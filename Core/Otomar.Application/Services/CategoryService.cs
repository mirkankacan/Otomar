using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Dtos.Category;
using Otomar.Application.Contracts.Persistence.Repositories;
using System.Net;

namespace Otomar.Application.Services
{
    public class CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger) : ICategoryService
    {
        public async Task<ServiceResult<IEnumerable<BrandDto>>> GetBrandsAsync()
        {
            var result = await categoryRepository.GetBrandsAsync();
            if (!result.Any())
            {
                logger.LogWarning("Markalar bulunamadı");
                return ServiceResult<IEnumerable<BrandDto>>.Error("Markalar Bulunamadı", "Sistemde aktif marka bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<IEnumerable<BrandDto>>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<BrandModelYearDto>>> GetBrandsModelsYearsAsync()
        {
            var (brands, models, years) = await categoryRepository.GetBrandsModelsYearsRawAsync();

            if (brands == null || !brands.Any())
            {
                logger.LogWarning("Markalar bulunamadı");
                return ServiceResult<IEnumerable<BrandModelYearDto>>.Error("Markalar Bulunamadı", "Sistemde aktif marka bulunamadı", HttpStatusCode.NotFound);
            }

            if (models == null)
            {
                logger.LogWarning("Modeller bulunamadı");
                return ServiceResult<IEnumerable<BrandModelYearDto>>.Error("Modeller Bulunamadı", "Sistemde aktif model bulunamadı", HttpStatusCode.NotFound);
            }

            var yearLookup = years.ToLookup(y => y.MODEL_ID);

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

        public async Task<ServiceResult<IEnumerable<CategoryDto>>> GetCategoriesAsync()
        {
            var (categories, subCategories) = await categoryRepository.GetCategoriesRawAsync();

            if (!categories.Any())
            {
                logger.LogWarning("Kategoriler bulunamadı");
                return ServiceResult<IEnumerable<CategoryDto>>.Error("Kategoriler Bulunamadı", "Sistemde kategori bulunamadı", HttpStatusCode.NotFound);
            }

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

        public async Task<ServiceResult<IEnumerable<FeaturedCategoryDto>>> GetFeaturedCategoriesAsync()
        {
            var result = await categoryRepository.GetFeaturedCategoriesAsync();
            if (!result.Any())
            {
                logger.LogWarning("Öne çıkarılmış kategoriler bulunamadı");
                return ServiceResult<IEnumerable<FeaturedCategoryDto>>.Error("Öne Çıkarılmış Kategoriler Bulunamadı", "Sistemde öne çıkarılmış kategori bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<IEnumerable<FeaturedCategoryDto>>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<ManufacturerDto>>> GetManufacturersAsync()
        {
            var result = await categoryRepository.GetManufacturersAsync();
            if (!result.Any())
            {
                logger.LogWarning("Üretici markalar bulunamadı");
                return ServiceResult<IEnumerable<ManufacturerDto>>.Error("Üretici Markalar Bulunamadı", "Sistemde aktif üretici marka bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<IEnumerable<ManufacturerDto>>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<ModelDto>>> GetModelsByBrandAsync(int brandId)
        {
            var result = await categoryRepository.GetModelsByBrandAsync(brandId);
            if (!result.Any())
            {
                logger.LogWarning("{BrandId} kodlu markaya ait modeller bulunamadı", brandId);
                return ServiceResult<IEnumerable<ModelDto>>.Error("Modeller Bulunamadı", $"{brandId} kodlu markaya ait aktif model bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<IEnumerable<ModelDto>>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<YearDto>>> GetYearsByModelAsync(int modelId)
        {
            var result = await categoryRepository.GetYearsByModelAsync(modelId);
            if (!result.Any())
            {
                logger.LogWarning("{ModelId} kodlu modele ait kasalar bulunamadı", modelId);
                return ServiceResult<IEnumerable<YearDto>>.Error("Kasalar Bulunamadı", $"{modelId} kodlu modele ait aktif kasa bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<IEnumerable<YearDto>>.SuccessAsOk(result);
        }
    }
}
