using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Otomar.WebApp.Dtos;
using Otomar.WebApp.Dtos.Product;
using Otomar.WebApp.Helpers;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Services
{
    public class FeedGeneratorService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<FeedGeneratorService> logger) : BackgroundService
    {
        public const string FeedCacheKey = "SeoFeedXml";
        public const string SitemapIndexCacheKey = "SeoSitemapIndexXml";
        public const string SitemapPartCacheKeyPrefix = "SeoSitemapPart_";
        private const string BaseUrl = "https://otomar.com.tr";
        private const int MaxUrlsPerSitemap = 45000;

        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(26);
        private static readonly TimeZoneInfo TurkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        private const int DailyRunHour = 23; // Her gün saat 23:00'te çalışır (Google Merchant 00:00'da çeker)

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // İlk çalıştırmada kısa bekleme (DI hazır olsun)
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            // Uygulama başladığında hemen bir kez oluştur
            await RunSafeAsync(stoppingToken);

            // Sonra her gün saat 23:00'te çalış
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRun();
                logger.LogInformation("Sonraki feed oluşturma: {Delay:hh\\:mm\\:ss} sonra", delay);
                await Task.Delay(delay, stoppingToken);
                await RunSafeAsync(stoppingToken);
            }
        }

        private async Task RunSafeAsync(CancellationToken ct)
        {
            try
            {
                logger.LogInformation("Feed/Sitemap oluşturma başladı");
                await GenerateAllAsync(ct);
                logger.LogInformation("Feed/Sitemap oluşturma tamamlandı");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Feed/Sitemap oluşturulurken hata oluştu");
            }
        }

        private static TimeSpan GetDelayUntilNextRun()
        {
            var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz);
            var nextRun = nowTurkey.Date.AddHours(DailyRunHour);

            // Eğer bugünkü saat geçtiyse yarına ayarla
            if (nowTurkey >= nextRun)
                nextRun = nextRun.AddDays(1);

            return nextRun - nowTurkey;
        }

        private async Task GenerateAllAsync(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var productApi = scope.ServiceProvider.GetRequiredService<IProductApi>();
            var optionApi = scope.ServiceProvider.GetRequiredService<IOptionApi>();

            // Tüm ürünleri bir kere çek, hem feed hem sitemap için kullan
            var allProducts = await FetchAllProductsAsync(productApi, ct);
            logger.LogInformation("Toplam {Count} ürün çekildi", allProducts.Count);

            // Kargo bilgisi
            ShippingOptions? shippingOptions = null;
            try
            {
                shippingOptions = await optionApi.GetShippingOptionsAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kargo bilgisi alınamadı");
            }

            // Feed XML oluştur
            var feedXml = BuildFeedXml(allProducts, shippingOptions);
            cache.Set(FeedCacheKey, feedXml, CacheExpiry);

            // Sitemap XML oluştur (index + parçalar)
            var sitemapParts = BuildSitemapParts(allProducts);
            for (var i = 0; i < sitemapParts.Count; i++)
            {
                cache.Set($"{SitemapPartCacheKeyPrefix}{i}", sitemapParts[i], CacheExpiry);
            }
            var sitemapIndexXml = BuildSitemapIndexXml(sitemapParts.Count);
            cache.Set(SitemapIndexCacheKey, sitemapIndexXml, CacheExpiry);
            logger.LogInformation("Sitemap oluşturuldu: {PartCount} parça", sitemapParts.Count);
        }

        private static async Task<List<ProductDto>> FetchAllProductsAsync(IProductApi productApi, CancellationToken ct)
        {
            var allProducts = new List<ProductDto>();
            var pageNumber = 1;
            const int pageSize = 1000;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var result = await productApi.GetProductsAsync(new ProductFilterRequestDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }, ct);

                if (result?.Data == null)
                    break;

                allProducts.AddRange(result.Data);

                if (!result.HasNext)
                    break;

                pageNumber++;
            }

            return allProducts;
        }

        private static readonly Dictionary<string, int> GoogleCategoryMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Filtre"] = 2820,                        // Motor Vehicle Engine Parts
            ["Kimyasal ve Yağ"] = 2788,               // Vehicle Fluids
            ["Silecek"] = 2534,                        // Motor Vehicle Window Parts & Accessories
            ["Debriyaj / Şanzıman"] = 2641,            // Motor Vehicle Transmission & Drivetrain Parts
            ["Aks / Tekerlek"] = 3020,                 // Motor Vehicle Wheel Systems
            ["Süspansiyon"] = 2935,                    // Motor Vehicle Suspension Parts
            ["Direksiyon"] = 8235,                     // Motor Vehicle Controls
            ["Fren"] = 2977,                           // Motor Vehicle Braking
            ["Motor"] = 2820,                          // Motor Vehicle Engine Parts
            ["İklimlendirme"] = 2805,                  // Motor Vehicle Climate Control
            ["Kayış / Rulman"] = 2820,                 // Motor Vehicle Engine Parts
            ["Kauçuk / Metal-Kauçuk"] = 899,           // Motor Vehicle Parts
            ["Elektrik / Aydınlatma"] = 3318,          // Motor Vehicle Lighting
            ["İç Aksam"] = 8233,                       // Motor Vehicle Interior Fittings
            ["Dış Aksam / Kaporta / Hasar"] = 8227,    // Motor Vehicle Frame & Body Parts
            ["Aksesuar"] = 5613,                       // Vehicle Parts & Accessories
            ["Diğer Ürünler"] = 899,                   // Motor Vehicle Parts
        };

        private static string BuildFeedXml(List<ProductDto> products, ShippingOptions? shippingOptions)
        {
            XNamespace g = "http://base.google.com/ns/1.0";
            XNamespace atom = "http://www.w3.org/2005/Atom";

            var items = new List<XElement>();

            foreach (var product in products)
            {
                var slug = SlugHelper.Generate(product.STOK_ADI ?? "");
                if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(product.STOK_KODU))
                    continue;

                var productUrl = $"{BaseUrl}/urun/{slug}/{product.STOK_KODU}";

                // Ana görsel
                var imageUrl = "";
                if (!string.IsNullOrEmpty(product.VITRIN_FOTO))
                {
                    imageUrl = product.VITRIN_FOTO.StartsWith("http")
                        ? product.VITRIN_FOTO
                        : $"{BaseUrl}{(product.VITRIN_FOTO.StartsWith("/") ? "" : "/")}{product.VITRIN_FOTO}";
                }
                else if (!string.IsNullOrEmpty(product.DOSYA_KONUM))
                {
                    var firstImage = product.DOSYA_KONUM.Split(';')[0].Trim();
                    imageUrl = firstImage.StartsWith("http")
                        ? firstImage
                        : $"{BaseUrl}{(firstImage.StartsWith("/") ? "" : "/")}{firstImage}";
                }

                // Açıklama
                var description = !string.IsNullOrEmpty(product.ACIKLAMA)
                    ? product.ACIKLAMA
                    : $"{product.STOK_ADI}, {product.URETICI_MARKA_ADI} marka, {product.URETICI_KODU} kodlu oto yedek parça.";

                // Kategori
                var category = product.ANA_GRUP_ADI?.Split(';')[0].Trim() ?? "Oto Yedek Parça";
                var subCategory = product.ALT_GRUP_ADI?.Split(';')[0].Trim();
                var fullCategory = !string.IsNullOrEmpty(subCategory)
                    ? $"Araçlar ve Parçalar > Araç Parçaları > {category} > {subCategory}"
                    : $"Araçlar ve Parçalar > Araç Parçaları > {category}";

                // Kargo ücreti
                var shippingPrice = "0 TRY";
                if (shippingOptions != null)
                {
                    shippingPrice = product.SATIS_FIYAT >= shippingOptions.Threshold
                        ? "0 TRY"
                        : $"{shippingOptions.Cost.ToString("F2", CultureInfo.InvariantCulture)} TRY";
                }

                var itemElement = new XElement("item",
                    new XElement(g + "id", product.STOK_KODU),
                    new XElement("title", product.STOK_ADI),
                    new XElement("description", description),
                    new XElement("link", productUrl),
                    new XElement(g + "price", $"{product.SATIS_FIYAT.ToString("F2", CultureInfo.InvariantCulture)} TRY"),
                    new XElement(g + "availability", product.HasStock ? "in_stock" : "out_of_stock"),
                    new XElement(g + "condition", "new"),
                    new XElement(g + "brand", product.URETICI_MARKA_ADI ?? ""),
                    new XElement(g + "mpn", product.URETICI_KODU?.Split(';')[0].Trim() ?? ""),
                    new XElement(g + "product_type", fullCategory),
                    new XElement(g + "google_product_category",
                        GoogleCategoryMap.GetValueOrDefault(category, 899)),
                    new XElement(g + "shipping",
                        new XElement(g + "country", "TR"),
                        new XElement(g + "service", "Kargo"),
                        new XElement(g + "price", shippingPrice))
                );

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    itemElement.Add(new XElement(g + "image_link", imageUrl));
                }

                // Ek görseller
                if (!string.IsNullOrEmpty(product.DOSYA_KONUM))
                {
                    var additionalImages = product.DOSYA_KONUM.Split(';')
                        .Select(f => f.Trim())
                        .Where(f => !string.IsNullOrEmpty(f))
                        .Skip(1)
                        .Take(9);

                    foreach (var img in additionalImages)
                    {
                        var imgUrl = img.StartsWith("http")
                            ? img
                            : $"{BaseUrl}{(img.StartsWith("/") ? "" : "/")}{img}";
                        itemElement.Add(new XElement(g + "additional_image_link", imgUrl));
                    }
                }

                items.Add(itemElement);
            }

            var rss = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XAttribute(XNamespace.Xmlns + "g", g),
                    new XAttribute(XNamespace.Xmlns + "atom", atom),
                    new XElement("channel",
                        new XElement("title", "OTOMAR Yedek Parça"),
                        new XElement("link", BaseUrl),
                        new XElement("description", "OTOMAR Yedek Parça - Oto yedek parça e-ticaret sitesi"),
                        new XElement(atom + "link",
                            new XAttribute("href", $"{BaseUrl}/feed.xml"),
                            new XAttribute("rel", "self"),
                            new XAttribute("type", "application/rss+xml")),
                        items)));

            return rss.ToString();
        }

        private static List<string> BuildSitemapParts(List<ProductDto> products)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            // Tüm URL elemanlarını hazırla
            var allUrlElements = new List<XElement>();

            // Statik sayfalar (ilk parçaya eklenir)
            var staticPages = new (string loc, string changefreq, string priority)[]
            {
                ("/ana-sayfa", "daily", "1.0"),
                ("/magaza", "daily", "0.9"),
                ("/iletisim", "monthly", "0.6"),
                ("/hakkimizda", "monthly", "0.5"),
                ("/kaynaklar/odeme-ve-teslimat", "monthly", "0.4"),
                ("/kaynaklar/gizlilik-ve-guvenlik", "monthly", "0.4"),
                ("/kaynaklar/gizlilik-politikasi", "monthly", "0.4"),
                ("/kaynaklar/iade-ve-degisim", "monthly", "0.4"),
                ("/kaynaklar/satis-sozlesmesi", "monthly", "0.4"),
                ("/kaynaklar/sartlar-ve-kosullar", "monthly", "0.4"),
            };

            foreach (var page in staticPages)
            {
                allUrlElements.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}{page.loc}"),
                    new XElement(ns + "changefreq", page.changefreq),
                    new XElement(ns + "priority", page.priority)));
            }

            foreach (var product in products)
            {
                var slug = SlugHelper.Generate(product.STOK_ADI ?? "");
                if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(product.STOK_KODU))
                    continue;

                allUrlElements.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/urun/{slug}/{product.STOK_KODU}"),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")));
            }

            // MaxUrlsPerSitemap'e göre parçalara böl
            var parts = new List<string>();
            var chunks = allUrlElements
                .Select((url, index) => new { url, index })
                .GroupBy(x => x.index / MaxUrlsPerSitemap)
                .Select(g => g.Select(x => x.url).ToList())
                .ToList();

            foreach (var chunk in chunks)
            {
                var document = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(ns + "urlset", chunk));
                parts.Add(document.ToString());
            }

            return parts;
        }

        private static string BuildSitemapIndexXml(int partCount)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var lastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var sitemapElements = new List<XElement>();
            for (var i = 0; i < partCount; i++)
            {
                sitemapElements.Add(new XElement(ns + "sitemap",
                    new XElement(ns + "loc", $"{BaseUrl}/sitemap-{i}.xml"),
                    new XElement(ns + "lastmod", lastMod)));
            }

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "sitemapindex", sitemapElements));

            return document.ToString();
        }
    }
}
