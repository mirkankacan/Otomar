using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos;
using Otomar.WebApp.Dtos.Product;
using Otomar.WebApp.Helpers;
using Otomar.WebApp.Services.Refit;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("")]
    public class SeoController(IProductApi productApi, IOptionApi optionApi, ILogger<SeoController> logger) : Controller
    {
        private const string BaseUrl = "https://otomar.com.tr";

        /// <summary>
        /// Dinamik sitemap.xml - statik sayfalar + tum urunler
        /// </summary>
        [HttpGet("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var urlElements = new List<XElement>();

            // Statik sayfalar
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
                urlElements.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}{page.loc}"),
                    new XElement(ns + "changefreq", page.changefreq),
                    new XElement(ns + "priority", page.priority)));
            }

            // Dinamik urun sayfalari
            try
            {
                var pageNumber = 1;
                const int pageSize = 100;
                bool hasMore = true;

                while (hasMore)
                {
                    var result = await productApi.GetProductsAsync(new ProductFilterRequestDto
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    }, cancellationToken);

                    if (result?.Data != null)
                    {
                        foreach (var product in result.Data)
                        {
                            var slug = SlugHelper.Generate(product.STOK_ADI ?? "");
                            if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(product.STOK_KODU))
                                continue;

                            urlElements.Add(new XElement(ns + "url",
                                new XElement(ns + "loc", $"{BaseUrl}/urun/{slug}/{product.STOK_KODU}"),
                                new XElement(ns + "changefreq", "weekly"),
                                new XElement(ns + "priority", "0.8")));
                        }

                        hasMore = result.HasNext;
                        pageNumber++;
                    }
                    else
                    {
                        hasMore = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sitemap urun verisi alinamadi");
            }

            var document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "urlset", urlElements));

            return Content(document.ToString(), "application/xml", Encoding.UTF8);
        }

        /// <summary>
        /// Google Merchant Center urun feed'i - Google Shopping XML formati
        /// </summary>
        [HttpGet("feed.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Feed(CancellationToken cancellationToken)
        {
            XNamespace g = "http://base.google.com/ns/1.0";
            XNamespace atom = "http://www.w3.org/2005/Atom";

            var items = new List<XElement>();

            // Kargo bilgilerini al
            ShippingOptions? shippingOptions = null;
            try
            {
                shippingOptions = await optionApi.GetShippingOptionsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kargo bilgisi alinamadi");
            }

            try
            {
                var pageNumber = 1;
                const int pageSize = 100;
                bool hasMore = true;

                while (hasMore)
                {
                    var result = await productApi.GetProductsAsync(new ProductFilterRequestDto
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    }, cancellationToken);

                    if (result?.Data != null)
                    {
                        foreach (var product in result.Data)
                        {
                            var slug = SlugHelper.Generate(product.STOK_ADI ?? "");
                            if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(product.STOK_KODU))
                                continue;

                            var productUrl = $"{BaseUrl}/urun/{slug}/{product.STOK_KODU}";

                            // Ana gorsel
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

                            // Aciklama
                            var description = !string.IsNullOrEmpty(product.ACIKLAMA)
                                ? product.ACIKLAMA
                                : $"{product.STOK_ADI}, {product.URETICI_MARKA_ADI} marka, {product.URETICI_KODU} kodlu oto yedek parça.";

                            // Kategori
                            var category = product.ANA_GRUP_ADI?.Split(';')[0].Trim() ?? "Oto Yedek Parça";
                            var subCategory = product.ALT_GRUP_ADI?.Split(';')[0].Trim();
                            var fullCategory = !string.IsNullOrEmpty(subCategory)
                                ? $"Araçlar ve Parçalar > Araç Parçaları > {category} > {subCategory}"
                                : $"Araçlar ve Parçalar > Araç Parçaları > {category}";

                            // Kargo ucreti hesapla
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
                                new XElement(g + "availability", product.HasStock ? "in_stock" : "limited_availability"),
                                new XElement(g + "condition", "new"),
                                new XElement(g + "brand", product.URETICI_MARKA_ADI ?? ""),
                                new XElement(g + "mpn", product.URETICI_KODU?.Split(';')[0].Trim() ?? ""),
                                new XElement(g + "product_type", fullCategory),
                                new XElement(g + "google_product_category", "Araçlar ve Parçalar > Araç Parçaları"),
                                new XElement(g + "shipping",
                                    new XElement(g + "country", "TR"),
                                    new XElement(g + "service", "Kargo"),
                                    new XElement(g + "price", shippingPrice))
                            );

                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                itemElement.Add(new XElement(g + "image_link", imageUrl));
                            }

                            // Ek gorseller
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

                        hasMore = result.HasNext;
                        pageNumber++;
                    }
                    else
                    {
                        hasMore = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feed urun verisi alinamadi");
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

            return Content(rss.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
