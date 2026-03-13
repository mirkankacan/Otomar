using Dapper;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Product;
using System.Web;

namespace Otomar.Persistence.Repositories
{
    /// <summary>
    /// Dapper-based product data access.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private const string SelectColumns = @"
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

        /// <inheritdoc />
        public async Task<List<(string FilterType, string FilterValue)>> GetUserGlobalFiltersAsync(string userId)
        {
            var filters = await _unitOfWork.Connection.QueryAsync<(string FilterType, string FilterValue)>(
                "SELECT FilterType, FilterValue FROM UserGlobalFilters WHERE UserId = @userId",
                new { userId });

            return filters.ToList();
        }

        /// <inheritdoc />
        public async Task<FeaturedProductDto> GetFeaturedProductsAsync(
            List<(string FilterType, string FilterValue)> globalFilters)
        {
            var baseWhere = "(ANA_GRUP_ID NOT LIKE '%17;%' AND ANA_GRUP_ID NOT LIKE '17;%' AND ANA_GRUP_ID NOT LIKE '%;17' AND ANA_GRUP_ID <> '17')";

            var parameters = new DynamicParameters();
            var globalConditions = new List<string>();
            ApplyGlobalFilters(globalConditions, parameters, globalFilters);
            var globalWhere = globalConditions.Count > 0 ? " AND " + string.Join(" AND ", globalConditions) : "";

            var sql = $@"
                SELECT TOP 8 {SelectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY WEB_GOSTER_TARIH DESC;
                SELECT TOP 8 {SelectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} AND STOK_BAKIYE > 0 ORDER BY STOK_BAKIYE ASC;
                SELECT TOP 8 {SelectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY SATIS_FIYAT ASC;
                SELECT TOP 8 {SelectColumns} FROM IdvStock WITH (NOLOCK) WHERE {baseWhere}{globalWhere} ORDER BY SATIS_FIYAT DESC;";

            using var multi = await _unitOfWork.Connection.QueryMultipleAsync(sql, parameters);
            return new FeaturedProductDto
            {
                Latest = await multi.ReadAsync<ProductDto>(),
                BestSeller = await multi.ReadAsync<ProductDto>(),
                Lowestprice = await multi.ReadAsync<ProductDto>(),
                HighestPrice = await multi.ReadAsync<ProductDto>()
            };
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetByCodeAsync(string code)
        {
            var query = $@"
                SELECT TOP 1 {SelectColumns}
                FROM IdvStock WITH (NOLOCK)
                WHERE STOK_KODU = @code";

            return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<ProductDto>(query, new { code = code.Trim() });
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetByIdAsync(int id, IUnitOfWork? unitOfWork = null)
        {
            var query = $@"
                SELECT TOP 1 {SelectColumns}
                FROM IdvStock WITH (NOLOCK)
                WHERE ID = @id";

            var uow = unitOfWork ?? _unitOfWork;
            var connection = uow.Connection;
            var transaction = uow.Transaction;

            return await connection.QueryFirstOrDefaultAsync<ProductDto>(query, new { id }, transaction);
        }

        /// <inheritdoc />
        public async Task<PagedResult<ProductDto>> GetFilteredAsync(
            ProductFilterRequestDto filter,
            List<(string FilterType, string FilterValue)> globalFilters)
        {
            var parameters = new DynamicParameters();
            var whereConditions = new List<string>();

            BuildSearchTermConditions(whereConditions, parameters, filter.SearchTerm);
            BuildCategoryConditions(whereConditions, parameters, filter.MainCategory, filter.SubCategory);
            BuildBrandConditions(whereConditions, parameters, filter.Brand);
            BuildModelConditions(whereConditions, parameters, filter.Model);
            BuildYearConditions(whereConditions, parameters, filter.Year);
            BuildPriceConditions(whereConditions, parameters, filter.MinPrice, filter.MaxPrice);
            BuildManufacturerConditions(whereConditions, parameters, filter.Manufacturer);
            ApplyGlobalFilters(whereConditions, parameters, globalFilters);

            var whereClause = whereConditions.Count > 0
                ? $"WHERE {string.Join(" AND ", whereConditions)}"
                : "";

            var decodedSortBy = HttpUtility.UrlDecode(filter.OrderBy ?? "");
            var orderByClause = decodedSortBy switch
            {
                "latest" => " ORDER BY WEB_GOSTER_TARIH DESC",
                "bestseller" => " ORDER BY STOK_BAKIYE DESC",
                "cheap" => " ORDER BY SATIS_FIYAT ASC",
                "expensive" => " ORDER BY SATIS_FIYAT DESC",
                _ => " ORDER BY ID DESC"
            };

            var offset = (filter.PageNumber - 1) * filter.PageSize;
            parameters.Add("offset", offset);
            parameters.Add("pageSize", filter.PageSize);

            var totalCountQuery = $"SELECT COUNT(*) FROM IdvStock WITH (NOLOCK) {whereClause}";
            var totalCount = await _unitOfWork.Connection.QuerySingleAsync<int>(totalCountQuery, parameters);

            var query = $@"
                SELECT {SelectColumns}
                FROM IdvStock WITH (NOLOCK)
                {whereClause}
                {orderByClause}
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var result = await _unitOfWork.Connection.QueryAsync<ProductDto>(query, parameters);

            return new PagedResult<ProductDto>(result, filter.PageNumber, filter.PageSize, totalCount);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SimilarProductDto?>> GetSimilarByCodeAsync(string code)
        {
            var query = @"
            WITH ProductInfo AS (
                SELECT KASA_ADI, ALT_GRUP_ADI, ANA_GRUP_ADI
                FROM [AA].[dbo].[IdvStock] WITH (NOLOCK)
                WHERE [STOK_KODU] = @code
            ),
            SimilarProducts AS (
                -- Birinci oncelik: KASA_ADI ve ALT_GRUP_ADI eslesmesi
                SELECT s.*, 1 as Priority
                FROM [AA].[dbo].[IdvStock] s WITH (NOLOCK)
                CROSS JOIN ProductInfo p
                WHERE s.[STOK_KODU] <> @code
                  AND s.[KASA_ADI] LIKE '%' + SUBSTRING(p.KASA_ADI, 1, CHARINDEX(';', p.KASA_ADI + ';') - 1) + '%'
                  AND s.[ALT_GRUP_ADI] LIKE '%' + SUBSTRING(p.ALT_GRUP_ADI, 1, CHARINDEX(';', p.ALT_GRUP_ADI + ';') - 1) + '%'

                UNION ALL

                -- Ikinci oncelik: KASA_ADI ve ANA_GRUP_ADI eslesmesi
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

                -- Ucuncu oncelik: Sadece KASA_ADI eslesmesi
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

            return await _unitOfWork.Connection.QueryAsync<SimilarProductDto?>(query, new { code = code.Trim() });
        }

        #region Private Helpers

        /// <summary>
        /// Converts a URL slug back to title-cased text.
        /// </summary>
        private static string ConvertSlugToText(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            var text = slug.Replace('-', ' ').Replace('_', ' ');
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var titleCased = string.Join(' ', words.Select(w =>
                char.ToUpper(w[0]) + w.Substring(1).ToLower()));

            return titleCased;
        }

        /// <summary>
        /// Applies user-level global filters as additional WHERE conditions.
        /// </summary>
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

        private static void BuildSearchTermConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return;

            var decodedSearchTerm = HttpUtility.UrlDecode(searchTerm.Trim());
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

        private static void BuildCategoryConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? mainCategory,
            string? subCategory)
        {
            if (!string.IsNullOrWhiteSpace(mainCategory))
            {
                var mainCategoryText = ConvertSlugToText(HttpUtility.UrlDecode(mainCategory.Trim()));
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

            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                var subCategoryText = ConvertSlugToText(HttpUtility.UrlDecode(subCategory.Trim()));
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
        }

        private static void BuildBrandConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? brand)
        {
            if (string.IsNullOrWhiteSpace(brand)) return;

            var brandText = ConvertSlugToText(HttpUtility.UrlDecode(brand.Trim()));
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

        private static void BuildModelConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return;

            var modelText = ConvertSlugToText(HttpUtility.UrlDecode(model.Trim()));
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

        private static void BuildYearConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? year)
        {
            if (string.IsNullOrWhiteSpace(year)) return;

            var yearText = ConvertSlugToText(HttpUtility.UrlDecode(year.Trim()));
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

        private static void BuildPriceConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            decimal? minPrice,
            decimal? maxPrice)
        {
            if (minPrice.HasValue && minPrice > 0)
            {
                whereConditions.Add("SATIS_FIYAT >= @minPrice");
                parameters.Add("minPrice", minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice > 0)
            {
                whereConditions.Add("SATIS_FIYAT <= @maxPrice");
                parameters.Add("maxPrice", maxPrice.Value);
            }
        }

        private static void BuildManufacturerConditions(
            List<string> whereConditions,
            DynamicParameters parameters,
            string? manufacturer)
        {
            if (string.IsNullOrWhiteSpace(manufacturer)) return;

            var decodedManufacturer = HttpUtility.UrlDecode(manufacturer);
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

        #endregion
    }
}
