using Dapper;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Category;
using Otomar.Persistance.Data;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class CategoryService(IAppDbContext context, ILogger<CategoryService> logger) : ICategoryService
    {
        public async Task<ServiceResult<IEnumerable<BrandDto>>> GetBrandsAsync()
        {
            try
            {
                var query = $@"SELECT * FROM IdtMarkaTanim  WITH (NOLOCK) WHERE AKTIF=1 ORDER BY MARKA_ADI ASC";

                var result = await context.Connection.QueryAsync<BrandDto>(query);
                if (result.Count() == 0)
                {
                    logger.LogWarning("Markalar bulunamadı");
                    return ServiceResult<IEnumerable<BrandDto>>.Error("Markalar Bulunamadı", "Sistemde aktif marka bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<IEnumerable<BrandDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetBrandsAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<BrandModelYearDto>>> GetBrandsModelsYearsAsync()
        {
            try
            {
                // Markaları çek
                var brandQuery = @"
        SELECT
            MARKA_KODU,
            MARKA_ADI,
            AKTIF
        FROM IdtMarkaTanim WITH (NOLOCK)
        ORDER BY MARKA_ADI;";

                // Modelleri çek
                var modelQuery = @"
        SELECT
            MODEL_KODU,
            MODEL_ADI,
            MARKA_KODU,
            AKTIF
        FROM IdtAracModel WITH (NOLOCK)
        WHERE AKTIF = 1 OR AKTIF IS NULL
        ORDER BY MODEL_ADI;";

                // Yılları/Kasaları çek
                var yearQuery = @"
        SELECT
            KASA_KODU,
            KASA_ADI,
            MODEL_ID,
            AKTIF
        FROM IdtAracKasaAdı WITH (NOLOCK)
        ORDER BY KASA_ADI;";

                var brands = await context.Connection.QueryAsync<BrandModelYearDto>(brandQuery);
                if (brands == null || !brands.Any())
                {
                    logger.LogWarning("Markalar bulunamadı");

                    return ServiceResult<IEnumerable<BrandModelYearDto>>.Error("Markalar Bulunamadı", "Sistemde aktif marka bulunamadı", HttpStatusCode.NotFound);
                }

                var models = await context.Connection.QueryAsync<ModelDto>(modelQuery);
                if (models == null)
                {
                    logger.LogWarning("Modeller bulunamadı");

                    return ServiceResult<IEnumerable<BrandModelYearDto>>.Error("Modeller Bulunamadı", "Sistemde aktif model bulunamadı", HttpStatusCode.NotFound);
                }

                var years = await context.Connection.QueryAsync<YearDto>(yearQuery);
                var yearLookup = years.ToLookup(y => y.MODEL_ID);

                // Class kullanırken yeni instance oluşturuyoruz
                var modelsWithYears = models.Select(model => new ModelYearDto
                {
                    MODEL_KODU = model.MODEL_KODU,
                    MODEL_ADI = model.MODEL_ADI,
                    MARKA_KODU = model.MARKA_KODU,
                    AKTIF = model.AKTIF,
                    Years = yearLookup[model.MODEL_KODU].AsEnumerable()
                }).AsEnumerable();

                var modelLookup = modelsWithYears.ToLookup(m => m.MARKA_KODU);

                var result = brands.Select(brand => new BrandModelYearDto
                {
                    MARKA_KODU = brand.MARKA_KODU,
                    MARKA_ADI = brand.MARKA_ADI,
                    AKTIF = brand.AKTIF,
                    ModelsYears = modelLookup[brand.MARKA_KODU].AsEnumerable()
                });

                return ServiceResult<IEnumerable<BrandModelYearDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetBrandsModelsYearsAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<CategoryDto>>> GetCategoriesAsync()
        {
            try
            {
                // Ana kategorileri çek
                var mainQuery = @"
SELECT
    ID AS ANA_ID,
    GRUP_ID AS GRUP_ID,
    ANA_GRUP_ADI AS ANA_GRUP_ADI,
    GRUP_IKON AS GRUP_IKON
FROM IdtStokAnaGrup WITH (NOLOCK)
ORDER BY SIRA;";

                var subQuery = @"
SELECT
    ID AS ALT_ID,
    ALT_GRUP_ID AS ALT_GRUP_ID,
    ALT_GRUP_ADI AS ALT_GRUP_ADI,
    ANA_GRUP_ID AS ANA_GRUP_ID
FROM IdtStokAltGrup WITH (NOLOCK)
ORDER BY SIRA;";

                var categories = await context.Connection.QueryAsync<CategoryDto>(mainQuery);
                if (!categories.Any())
                {
                    logger.LogWarning("Kategoriler bulunamadı");

                    return ServiceResult<IEnumerable<CategoryDto>>.Error("Kategoriler Bulunamadı", "Sistemde kategori bulunamadı", HttpStatusCode.NotFound);
                }

                var subCategories = await context.Connection.QueryAsync<SubCategoryDto>(subQuery);
                var subCategoryLookup = subCategories.ToLookup(x => x.ANA_GRUP_ID);

                var result = categories.Select(category => new CategoryDto
                {
                    ANA_ID = category.ANA_ID,
                    GRUP_ID = category.GRUP_ID,
                    ANA_GRUP_ADI = category.ANA_GRUP_ADI,
                    GRUP_IKON = category.GRUP_IKON,
                    SubCategories = subCategoryLookup[category.GRUP_ID]
                }).AsEnumerable();

                return ServiceResult<IEnumerable<CategoryDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetCategoriesAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<FeaturedCategoryDto>>> GetFeaturedCategoriesAsync()
        {
            try
            {
                var query = $@"SELECT Name AS KATEGORI_ADI, ItemsCount AS TOPLAM_URUN_SAYISI, Icon AS IKON FROM IdvFeaturedCategories WITH (NOLOCK)";

                var result = await context.Connection.QueryAsync<FeaturedCategoryDto>(query);
                if (result.Count() == 0)
                {
                    logger.LogWarning("Öne çıkarılmış kategoriler bulunamadı");

                    return ServiceResult<IEnumerable<FeaturedCategoryDto>>.Error("Öne Çıkarılmış Kategoriler Bulunamadı", "Sistemde öne çıkarılmış kategori bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<IEnumerable<FeaturedCategoryDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetFeaturedCategoriesAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ManufacturerDto>>> GetManufacturersAsync()
        {
            try
            {
                var query = $@"SELECT * FROM IdvAktifStokMarka WITH (NOLOCK) ORDER BY MARKA_ADI ASC";

                var result = await context.Connection.QueryAsync<ManufacturerDto>(query);
                if (result.Count() == 0)
                {
                    logger.LogWarning("Üretici markalar bulunamadı");
                    return ServiceResult<IEnumerable<ManufacturerDto>>.Error("Üretici Markalar Bulunamadı", "Sistemde aktif üretici marka bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<IEnumerable<ManufacturerDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetManufacturersAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ModelDto>>> GetModelsByBrandAsync(int brandId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("brandId", brandId);

                var query = $@"SELECT * FROM IdtAracModel WITH (NOLOCK) WHERE MARKA_KODU=@brandId AND AKTIF=1 ORDER BY MODEL_ADI ASC";

                var result = await context.Connection.QueryAsync<ModelDto>(query, parameters);

                if (result.Count() == 0)
                {
                    logger.LogWarning($"{brandId} kodlu markaya ait modeller bulunamadı");
                    return ServiceResult<IEnumerable<ModelDto>>.Error("Modeller Bulunamadı", $"{brandId} kodlu markaya ait aktif model bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<IEnumerable<ModelDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetModelsByBrandAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<YearDto>>> GetYearsByModelAsync(int modelId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("modelId", modelId);

                var query = $@"SELECT * FROM IdtAracKasaAdı WITH (NOLOCK) WHERE AKTIF=1 AND MODEL_ID=@modelId ORDER BY KASA_ADI ASC";

                var result = await context.Connection.QueryAsync<YearDto>(query, parameters);

                if (result.Count() == 0)
                {
                    logger.LogWarning($"{modelId} kodlu modele ait kasalar bulunamadı");
                    return ServiceResult<IEnumerable<YearDto>>.Error("Kasalar Bulunamadı", $"{modelId} kodlu modele ait aktif kasa bulunamadı", HttpStatusCode.NotFound);
                }

                return ServiceResult<IEnumerable<YearDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetYearsByModelAsync işleminde hata");
                throw;
            }
        }
    }
}