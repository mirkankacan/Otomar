using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otomar.Contracts.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Contracts.Dtos.Product;
using Otomar.Persistance.Data;
using System.Data;
using System.Net;
using System.Web;

namespace Otomar.Persistance.Services
{
    public class ProductService(
        IAppDbContext context,
        ILogger<ProductService> logger,
        IIdentityService identityService,
        IHttpContextAccessor httpContextAccessor) : IProductService
    {
        private async Task<List<(string FilterType, string FilterValue)>> GetUserGlobalFiltersAsync()
        {
            if (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
                return [];

            var userId = identityService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return [];

            var filters = await context.Connection.QueryAsync<(string FilterType, string FilterValue)>(
                "SELECT FilterType, FilterValue FROM UserGlobalFilters WHERE UserId = @userId",
                new { userId });

            return filters.ToList();
        }

        private static void ApplyGlobalFilters(
            List<string> whereConditions,
            DynamicParameters parameters,
            List<(string FilterType, string FilterValue)> globalFilters)
        {
            if (globalFilters.Count == 0) return;

            var grouped = globalFilters.GroupBy(f => f.FilterType, StringComparer.OrdinalIgnoreCase);

            foreach (var group in grouped)
            {
                var columnName = group.Key;
                var values = group.Select(g => g.FilterValue).Where(v => !string.IsNullOrEmpty(v)).ToList();
                if (values.Count == 0) continue;

                var paramName = $"gf_{columnName}";
                if (values.Count == 1)
                {
                    whereConditions.Add($"{columnName} = @{paramName}");
                    parameters.Add(paramName, values[0]);
                }
                else
                {
                    whereConditions.Add($"{columnName} IN @{paramName}");
                    parameters.Add(paramName, values);
                }
            }
        }

        // Slug'ı normal metne çeviren helper metod
        private static string ConvertSlugToText(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            // Tire ve alt çizgileri boşluğa çevir
            var text = slug.Replace('-', ' ').Replace('_', ' ');

            // Her kelimenin ilk harfini büyük yap (Title Case)
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var titleCased = string.Join(' ', words.Select(w =>
                char.ToUpper(w[0]) + w.Substring(1).ToLower()));

            return titleCased;
        }

        public async Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync()
        {
            var selectColumns = @"
                      ID,
                     STOK_KODU,
                     OEM_KODU,
                     STOK_ADI,
                     URETICI_KODU,
                     URETICI_MARKA_ADI,
                     URETICI_MARKA_LOGO,
                     SATIS_FIYAT,
                     ANA_GRUP_ID,
                     ANA_GRUP_ADI,
                     ALT_GRUP_ID,
                     ALT_GRUP_ADI,
                     DOSYA_KONUM,
                     VITRIN_FOTO,
                     HASHTAG,
                     ACIKLAMA,
                     MARKA_ADI,
                     MODEL_ADI,
                     KASA_ADI,
                     STOK_BAKIYE";

            var baseWhere = "(ANA_GRUP_ID NOT LIKE '%17;%' AND ANA_GRUP_ID NOT LIKE '17;%' AND ANA_GRUP_ID NOT LIKE '%;17' AND ANA_GRUP_ID <> '17')";

            // Global filtreler
            var globalFilters = await GetUserGlobalFiltersAsync();
            var parameters = new DynamicParameters();
            var globalConditions = new List<string>();
            ApplyGlobalFilters(globalConditions, parameters, globalFilters);
            var globalWhere = globalConditions.Count > 0 ? " AND " + string.Join(" AND ", globalConditions) : "";

            var sql = $@"
                SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY WEB_GOSTER_TARIH DESC;
                SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} AND STOK_BAKIYE > 0 ORDER BY STOK_BAKIYE ASC;
                SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY SATIS_FIYAT ASC;
                SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY SATIS_FIYAT DESC;";

            using var multi = await context.Connection.QueryMultipleAsync(sql, parameters);
            var homePageProducts = new FeaturedProductDto
            {
                Latest = await multi.ReadAsync<ProductDto>(),
                BestSeller = await multi.ReadAsync<ProductDto>(),
                Lowestprice = await multi.ReadAsync<ProductDto>(),
                HighestPrice = await multi.ReadAsync<ProductDto>()
            };
            return ServiceResult<FeaturedProductDto>.SuccessAsOk(homePageProducts);
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return ServiceResult<ProductDto?>.Error("Geçersiz Stok Kodu", "Stok kodu boş geçilemez", HttpStatusCode.BadRequest);
            }
            var parameters = new DynamicParameters();
            parameters.Add("code", code.Trim());

            var query = $@"
                 SELECT TOP 1
                     ID,
                     STOK_KODU,
                     OEM_KODU,
                     STOK_ADI,
                     URETICI_KODU,
                     URETICI_MARKA_ADI,
                     URETICI_MARKA_LOGO,
                     SATIS_FIYAT,
                     ANA_GRUP_ID,
                     ANA_GRUP_ADI,
                     ALT_GRUP_ID,
                     ALT_GRUP_ADI,
                     DOSYA_KONUM,
                     VITRIN_FOTO,
                     HASHTAG,
                     ACIKLAMA,
                     MARKA_ADI,
                     MODEL_ADI,
                     KASA_ADI,
                     STOK_BAKIYE
                 FROM IdvStock WITH (NOLOCK)
                 WHERE STOK_KODU = @code";

            var result = await context.Connection.QueryFirstOrDefaultAsync<ProductDto>(query, parameters);
            if (result == null)
            {
                logger.LogWarning($"'{code}' stok kodlu ürün bulunamadı");
                return ServiceResult<ProductDto?>.Error("Ürün Bulunamadı", $"'{code}' stok kodlu ürün bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<ProductDto?>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id, IDbTransaction transaction = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("id", id);

            var query = $@"
                 SELECT TOP 1
                     ID,
                     STOK_KODU,
                     OEM_KODU,
                     STOK_ADI,
                     URETICI_KODU,
                     URETICI_MARKA_ADI,
                     URETICI_MARKA_LOGO,
                     SATIS_FIYAT,
                     ANA_GRUP_ID,
                     ANA_GRUP_ADI,
                     ALT_GRUP_ID,
                     ALT_GRUP_ADI,
                     DOSYA_KONUM,
                     VITRIN_FOTO,
                     HASHTAG,
                     ACIKLAMA,
                     MARKA_ADI,
                     MODEL_ADI,
                     KASA_ADI,
                     STOK_BAKIYE
                 FROM IdvStock WITH (NOLOCK)
                 WHERE ID = @id";

            var result = await context.Connection.QueryFirstOrDefaultAsync<ProductDto>(query, parameters, transaction);
            if (result == null)
            {
                logger.LogWarning($"'{id}' ID'li ürün bulunamadı");
                return ServiceResult<ProductDto?>.Error("Ürün Bulunamadı", $"'{id}' ID'li ürün bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<ProductDto?>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequestDto productFilterRequestDto)
        {
            var parameters = new DynamicParameters();
            var whereConditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.SearchTerm))
            {
                var decodedSearchTerm = HttpUtility.UrlDecode(productFilterRequestDto.SearchTerm.Trim());
                var searchParts = decodedSearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var searchConditions = new List<string>();
                for (int i = 0; i < searchParts.Length; i++)
                {
                    var paramName = $"searchTerm{i}";
                    parameters.Add(paramName, searchParts[i]);

                    if (searchParts.Length == 1)
                    {
                        searchConditions.Add($@"
                     (STOK_KODU = @{paramName}
                     OR URETICI_KODU = @{paramName}
                     OR OEM_KODU = @{paramName}
                     OR STOK_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR FATURA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR URETICI_MARKA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR HASHTAG COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR KASA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR MARKA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR MODEL_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%')");
                    }
                    else
                    {
                        // Multiple terms - focused search
                        searchConditions.Add($@"
                     (STOK_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR FATURA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR URETICI_MARKA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR HASHTAG COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'
                     OR KASA_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%')");
                    }
                }
                whereConditions.Add($"({string.Join(" AND ", searchConditions)})");
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.MainCategory))
            {
                var mainCategoryText = ConvertSlugToText(HttpUtility.UrlDecode(productFilterRequestDto.MainCategory.Trim()));
                var mainCatWords = mainCategoryText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var mainCatConditions = new List<string>();
                for (int i = 0; i < mainCatWords.Length; i++)
                {
                    var paramName = $"mainCat{i}";
                    mainCatConditions.Add($"ANA_GRUP_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'");
                    parameters.Add(paramName, mainCatWords[i]);
                }
                whereConditions.Add($"({string.Join(" AND ", mainCatConditions)})");
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.SubCategory))
            {
                var subCategoryText = ConvertSlugToText(HttpUtility.UrlDecode(productFilterRequestDto.SubCategory.Trim()));
                var subCatWords = subCategoryText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var subCatConditions = new List<string>();
                for (int i = 0; i < subCatWords.Length; i++)
                {
                    var paramName = $"subCat{i}";
                    subCatConditions.Add($"ALT_GRUP_ADI COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%'");
                    parameters.Add(paramName, subCatWords[i]);
                }
                whereConditions.Add($"({string.Join(" AND ", subCatConditions)})");
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Brand))
            {
                var brandText = ConvertSlugToText(HttpUtility.UrlDecode(productFilterRequestDto.Brand.Trim()));
                var brandWords = brandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var brandConditions = new List<string>();
                for (int i = 0; i < brandWords.Length; i++)
                {
                    var paramName = $"brand{i}";
                    brandConditions.Add($"EXISTS (SELECT 1 FROM STRING_SPLIT(MARKA_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%')");
                    parameters.Add(paramName, brandWords[i]);
                }
                whereConditions.Add($"({string.Join(" AND ", brandConditions)})");
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Model))
            {
                var modelText = ConvertSlugToText(HttpUtility.UrlDecode(productFilterRequestDto.Model.Trim()));
                var modelWords = modelText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var modelConditions = new List<string>();
                for (int i = 0; i < modelWords.Length; i++)
                {
                    var paramName = $"model{i}";
                    modelConditions.Add($"EXISTS (SELECT 1 FROM STRING_SPLIT(MODEL_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%')");
                    parameters.Add(paramName, modelWords[i]);
                }
                whereConditions.Add($"({string.Join(" AND ", modelConditions)})");
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Year))
            {
                var yearText = ConvertSlugToText(HttpUtility.UrlDecode(productFilterRequestDto.Year.Trim()));
                var yearWords = yearText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var yearConditions = new List<string>();
                for (int i = 0; i < yearWords.Length; i++)
                {
                    var paramName = $"year{i}";
                    yearConditions.Add($"EXISTS (SELECT 1 FROM STRING_SPLIT(KASA_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) COLLATE Latin1_General_CI_AI LIKE '%' + @{paramName} + '%')");
                    parameters.Add(paramName, yearWords[i]);
                }
                whereConditions.Add($"({string.Join(" AND ", yearConditions)})");
            }

            if (productFilterRequestDto.MinPrice.HasValue && productFilterRequestDto.MinPrice > 0)
            {
                whereConditions.Add("SATIS_FIYAT >= @minPrice");
                parameters.Add("minPrice", productFilterRequestDto.MinPrice.Value);
            }

            if (productFilterRequestDto.MaxPrice.HasValue && productFilterRequestDto.MaxPrice > 0)
            {
                whereConditions.Add("SATIS_FIYAT <= @maxPrice");
                parameters.Add("maxPrice", productFilterRequestDto.MaxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Manufacturer))
            {
                var decodedManufacturer = HttpUtility.UrlDecode(productFilterRequestDto.Manufacturer);
                var manufacturerList = decodedManufacturer.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => ConvertSlugToText(c.Trim()))
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                if (manufacturerList.Count > 0)
                {
                    var manufacturerConditions = new List<string>();
                    for (int i = 0; i < manufacturerList.Count; i++)
                    {
                        var paramName = $"manufacturer{i}";
                        manufacturerConditions.Add($"URETICI_KODU LIKE '%' + @{paramName} + '%' OR URETICI_MARKA_ADI LIKE '%' + @{paramName} + '%'");
                        parameters.Add(paramName, manufacturerList[i]);
                    }
                    whereConditions.Add($"({string.Join(" OR ", manufacturerConditions)})");
                }
            }

            // Global filtreler
            var globalFilters = await GetUserGlobalFiltersAsync();
            ApplyGlobalFilters(whereConditions, parameters, globalFilters);

            // Build final WHERE clause
            var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

            // Sorting
            var decodedSortBy = HttpUtility.UrlDecode(productFilterRequestDto.OrderBy ?? "");
            var orderByClause = decodedSortBy switch
            {
                "latest" => " ORDER BY WEB_GOSTER_TARIH DESC",
                "bestseller" => " ORDER BY STOK_BAKIYE DESC",
                "cheap" => " ORDER BY SATIS_FIYAT ASC",
                "expensive" => " ORDER BY SATIS_FIYAT DESC",
                _ => " ORDER BY ID DESC"
            };

            // Pagination
            var offset = (productFilterRequestDto.PageNumber - 1) * productFilterRequestDto.PageSize;
            parameters.Add("offset", offset);
            parameters.Add("pageSize", productFilterRequestDto.PageSize);

            // Get filtered count
            var totalCountQuery = $"SELECT COUNT(*) FROM IdvStock WITH (NOLOCK) {whereClause}";
            var totalCount = await context.Connection.QuerySingleAsync<int>(totalCountQuery, parameters);

            var query = $@"
                 SELECT
                     ID,
                     STOK_KODU,
                     OEM_KODU,
                     STOK_ADI,
                     URETICI_KODU,
                     URETICI_MARKA_ADI,
                     URETICI_MARKA_LOGO,
                     SATIS_FIYAT,
                     ANA_GRUP_ID,
                     ANA_GRUP_ADI,
                     ALT_GRUP_ID,
                     ALT_GRUP_ADI,
                     DOSYA_KONUM,
                     VITRIN_FOTO,
                     HASHTAG,
                     ACIKLAMA,
                     MARKA_ADI,
                     MODEL_ADI,
                     KASA_ADI,
                     STOK_BAKIYE
                 FROM IdvStock WITH (NOLOCK)
                 {whereClause}
                 {orderByClause}
                  OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var result = await context.Connection.QueryAsync<ProductDto>(query, parameters);

            return ServiceResult<PagedResult<ProductDto>>.SuccessAsOk(new PagedResult<ProductDto>(result, productFilterRequestDto.PageNumber, productFilterRequestDto.PageSize, totalCount));
        }

        public async Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("code", code.Trim());

            var query = @"
            WITH ProductInfo AS (
                SELECT KASA_ADI, ALT_GRUP_ADI, ANA_GRUP_ADI
                FROM [AA].[dbo].[IdvStock] WITH (NOLOCK)
                WHERE [STOK_KODU] = @code
            ),
            SimilarProducts AS (
                -- Birinci öncelik: KASA_ADI ve ALT_GRUP_ADI eşleşmesi
                SELECT s.*, 1 as Priority
                FROM [AA].[dbo].[IdvStock] s WITH (NOLOCK)
                CROSS JOIN ProductInfo p
                WHERE s.[STOK_KODU] <> @code
                  AND s.[KASA_ADI] LIKE '%' + SUBSTRING(p.KASA_ADI, 1, CHARINDEX(';', p.KASA_ADI + ';') - 1) + '%'
                  AND s.[ALT_GRUP_ADI] LIKE '%' + SUBSTRING(p.ALT_GRUP_ADI, 1, CHARINDEX(';', p.ALT_GRUP_ADI + ';') - 1) + '%'

                UNION ALL

                -- İkinci öncelik: KASA_ADI ve ANA_GRUP_ADI eşleşmesi
                SELECT s.*, 2 as Priority
                FROM [AA].[dbo].[IdvStock] s WITH (NOLOCK)
                CROSS JOIN ProductInfo p
                WHERE s.[STOK_KODU] <> @code
                  AND s.[KASA_ADI] LIKE '%' + SUBSTRING(p.KASA_ADI, 1, CHARINDEX(';', p.KASA_ADI + ';') - 1) + '%'
                  AND s.[ANA_GRUP_ADI] LIKE '%' + SUBSTRING(p.ANA_GRUP_ADI, 1, CHARINDEX(';', p.ANA_GRUP_ADI + ';') - 1) + '%'
                  AND NOT EXISTS (
                      SELECT 1 FROM [AA].[dbo].[IdvStock] s2 WITH (NOLOCK)
                      CROSS JOIN ProductInfo p2
                      WHERE s2.[STOK_KODU] = s.[STOK_KODU]
                        AND s2.[KASA_ADI] LIKE '%' + SUBSTRING(p2.KASA_ADI, 1, CHARINDEX(';', p2.KASA_ADI + ';') - 1) + '%'
                        AND s2.[ALT_GRUP_ADI] LIKE '%' + SUBSTRING(p2.ALT_GRUP_ADI, 1, CHARINDEX(';', p2.ALT_GRUP_ADI + ';') - 1) + '%'
                  )

                UNION ALL

                -- Üçüncü öncelik: Sadece KASA_ADI eşleşmesi
                SELECT s.*, 3 as Priority
                FROM [AA].[dbo].[IdvStock] s WITH (NOLOCK)
                CROSS JOIN ProductInfo p
                WHERE s.[STOK_KODU] <> @code
                  AND s.[KASA_ADI] LIKE '%' + SUBSTRING(p.KASA_ADI, 1, CHARINDEX(';', p.KASA_ADI + ';') - 1) + '%'
                  AND NOT EXISTS (
                      SELECT 1 FROM [AA].[dbo].[IdvStock] s2 WITH (NOLOCK)
                      CROSS JOIN ProductInfo p2
                      WHERE s2.[STOK_KODU] = s.[STOK_KODU]
                        AND (
                            (s2.[KASA_ADI] LIKE '%' + SUBSTRING(p2.KASA_ADI, 1, CHARINDEX(';', p2.KASA_ADI + ';') - 1) + '%'
                             AND s2.[ALT_GRUP_ADI] LIKE '%' + SUBSTRING(p2.ALT_GRUP_ADI, 1, CHARINDEX(';', p2.ALT_GRUP_ADI + ';') - 1) + '%')
                            OR
                            (s2.[KASA_ADI] LIKE '%' + SUBSTRING(p2.KASA_ADI, 1, CHARINDEX(';', p2.KASA_ADI + ';') - 1) + '%'
                             AND s2.[ANA_GRUP_ADI] LIKE '%' + SUBSTRING(p2.ANA_GRUP_ADI, 1, CHARINDEX(';', p2.ANA_GRUP_ADI + ';') - 1) + '%')
                        )
                  )
            )
            SELECT TOP 10
                ID, STOK_KODU, STOK_ADI, URETICI_KODU, SATIS_FIYAT, DOSYA_KONUM, URETICI_MARKA_LOGO
            FROM SimilarProducts
            ORDER BY Priority";
            var result = await context.Connection.QueryAsync<SimilarProductDto?>(query, parameters);

            return ServiceResult<IEnumerable<SimilarProductDto?>>.SuccessAsOk(result);
        }
    }
}