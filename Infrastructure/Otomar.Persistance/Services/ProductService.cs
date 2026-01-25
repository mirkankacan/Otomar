using Dapper;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Product;
using Otomar.Persistance.Data;
using System.Net;
using System.Web;

namespace Otomar.Persistance.Services
{
    public class ProductService(IAppDbContext context, ILogger<ProductService> logger) : IProductService
    {
        public async Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync()
        {
            try
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

                var tasks = new[]
                  {
                        context.Connection.QueryAsync<ProductDto>($"SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) ORDER BY WEB_GOSTER_TARIH DESC"),
                        context.Connection.QueryAsync<ProductDto>($"SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) ORDER BY ASGARI_STOK DESC"),
                        context.Connection.QueryAsync<ProductDto>($"SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) ORDER BY SATIS_FIYAT DESC"),
                        context.Connection.QueryAsync<ProductDto>($"SELECT TOP 8 {selectColumns} FROM IdvStock WITH (NOLOCK) ORDER BY URETICI_KODU DESC")
                    };
                var results = await Task.WhenAll(tasks);
                var homePageProducts = new FeaturedProductDto
                {
                    Recent = results[0],
                    BestSeller = results[1],
                    Top = results[2],
                    ByRate = results[3]
                };
                return ServiceResult<FeaturedProductDto>.SuccessAsOk(homePageProducts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetFeaturedProductsAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, "GetProductByCodeAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id)
        {
            try
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

                var result = await context.Connection.QueryFirstOrDefaultAsync<ProductDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"'{id}' ID'li ürün bulunamadı");
                    return ServiceResult<ProductDto?>.Error("Ürün Bulunamadı", $"'{id}' ID'li ürün bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<ProductDto?>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetProductByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequestDto productFilterRequestDto)
        {
            try
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
                     OR STOK_ADI LIKE '%' + @{paramName} + '%'
                     OR URETICI_MARKA_ADI LIKE '%' + @{paramName} + '%'
                     OR HASHTAG LIKE '%' + @{paramName} + '%'
                     OR KASA_ADI LIKE '%' + @{paramName} + '%'
                     OR MARKA_ADI LIKE '%' + @{paramName} + '%'
                     OR MODEL_ADI LIKE '%' + @{paramName} + '%')");
                        }
                        else
                        {
                            // Multiple terms - focused search
                            searchConditions.Add($@"
                     (STOK_ADI LIKE '%' + @{paramName} + '%'
                     OR URETICI_MARKA_ADI LIKE '%' + @{paramName} + '%'
                     OR HASHTAG LIKE '%' + @{paramName} + '%'
                     OR KASA_ADI LIKE '%' + @{paramName} + '%'
                     OR MARKA_ADI LIKE '%' + @{paramName} + '%')");
                        }
                    }
                    whereConditions.Add($"({string.Join(" AND ", searchConditions)})");
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.MainCategory))
                {
                    whereConditions.Add("ANA_GRUP_ADI LIKE '%' + @mainCategory + '%'");
                    parameters.Add("mainCategory", HttpUtility.UrlDecode(productFilterRequestDto.MainCategory.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.SubCategory))
                {
                    whereConditions.Add("ALT_GRUP_ADI LIKE '%' + @subCategory + '%'");
                    parameters.Add("subCategory", HttpUtility.UrlDecode(productFilterRequestDto.SubCategory.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Brand))
                {
                    whereConditions.Add("EXISTS (SELECT 1 FROM STRING_SPLIT(MARKA_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) = @brand)");
                    parameters.Add("brand", HttpUtility.UrlDecode(productFilterRequestDto.Brand.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Model))
                {
                    whereConditions.Add("EXISTS (SELECT 1 FROM STRING_SPLIT(MODEL_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) = @model)");
                    parameters.Add("model", HttpUtility.UrlDecode(productFilterRequestDto.Model.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Year))
                {
                    whereConditions.Add("EXISTS (SELECT 1 FROM STRING_SPLIT(KASA_ADI, ';') WHERE LTRIM(RTRIM(REPLACE(value, CHAR(160), ''))) = @year)");
                    parameters.Add("year", HttpUtility.UrlDecode(productFilterRequestDto.Year.Trim()));
                }

                if (productFilterRequestDto.MinPrice.HasValue && productFilterRequestDto.MinPrice > 0)
                {
                    whereConditions.Add("SATIS_FIYATI >= @minPrice");
                    parameters.Add("minPrice", productFilterRequestDto.MinPrice.Value);
                }

                if (productFilterRequestDto.MaxPrice.HasValue && productFilterRequestDto.MaxPrice > 0)
                {
                    whereConditions.Add("SATIS_FIYATI <= @maxPrice");
                    parameters.Add("maxPrice", productFilterRequestDto.MaxPrice.Value);
                }

                if (!string.IsNullOrWhiteSpace(productFilterRequestDto.Manufacturer))
                {
                    var decodedManufacturer = HttpUtility.UrlDecode(productFilterRequestDto.Manufacturer);
                    var manufacturerList = decodedManufacturer.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
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

                // Build final WHERE clause
                var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

                // Sorting
                var decodedSortBy = HttpUtility.UrlDecode(productFilterRequestDto.OrderBy ?? "");
                var orderByClause = decodedSortBy switch
                {
                    "latest" => " ORDER BY WEB_GOSTER_TARIH DESC",
                    "popularity" => " ORDER BY ASGARI_STOK DESC",
                    "cheap" => " ORDER BY SATIS_FIYAT ASC",
                    "expensive" => " ORDER BY SATIS_FIYAT DESC",
                    _ => " ORDER BY ID DESC"
                };

                // Pagination
                var offset = (productFilterRequestDto.PageNumber - 1) * productFilterRequestDto.PageSize;
                parameters.Add("offset", offset);
                parameters.Add("pageSize", productFilterRequestDto.PageSize);

                // Get total count first (for all records)
                var totalCountQuery = "SELECT COUNT(*) FROM IdvStock WITH (NOLOCK)";
                var totalCount = await context.Connection.QuerySingleAsync<int>(totalCountQuery);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "GetProductsAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code)
        {
            try
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
                STOK_KODU, STOK_ADI, SATIS_FIYAT, DOSYA_KONUM,URETICI_MARKA_LOGO
            FROM SimilarProducts
            ORDER BY Priority";
                var result = await context.Connection.QueryAsync<SimilarProductDto?>(query, parameters);

                return ServiceResult<IEnumerable<SimilarProductDto?>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetSimilarProductsByCodeAsync işleminde hata");
                throw;
            }
        }
    }
}