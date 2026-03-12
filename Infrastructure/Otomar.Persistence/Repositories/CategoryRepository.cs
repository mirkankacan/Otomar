using Dapper;
using Otomar.Application.Contracts.Persistence;
using Otomar.Shared.Dtos.Category;
using Otomar.Application.Contracts.Persistence.Repositories;

namespace Otomar.Persistence.Repositories
{
    public class CategoryRepository(IAppDbContext context) : ICategoryRepository
    {
        public async Task<IEnumerable<BrandDto>> GetBrandsAsync()
        {
            var query = "SELECT * FROM IdtMarkaTanim WITH (NOLOCK) WHERE AKTIF=1 ORDER BY MARKA_ADI ASC";
            return await context.Connection.QueryAsync<BrandDto>(query);
        }

        public async Task<(IEnumerable<BrandModelYearDto> Brands, IEnumerable<ModelDto> Models, IEnumerable<YearDto> Years)> GetBrandsModelsYearsRawAsync()
        {
            var brandQuery = @"
                SELECT MARKA_KODU, MARKA_ADI, AKTIF
                FROM IdtMarkaTanim WITH (NOLOCK)
                ORDER BY MARKA_ADI;";

            var modelQuery = @"
                SELECT MODEL_KODU, MODEL_ADI, MARKA_KODU, AKTIF
                FROM IdtAracModel WITH (NOLOCK)
                WHERE AKTIF = 1 OR AKTIF IS NULL
                ORDER BY MODEL_ADI;";

            var yearQuery = @"
                SELECT KASA_KODU, KASA_ADI, MODEL_ID, AKTIF
                FROM IdtAracKasaAdı WITH (NOLOCK)
                ORDER BY KASA_ADI;";

            var brands = await context.Connection.QueryAsync<BrandModelYearDto>(brandQuery);
            var models = await context.Connection.QueryAsync<ModelDto>(modelQuery);
            var years = await context.Connection.QueryAsync<YearDto>(yearQuery);

            return (brands, models, years);
        }

        public async Task<(IEnumerable<CategoryDto> Categories, IEnumerable<SubCategoryDto> SubCategories)> GetCategoriesRawAsync()
        {
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
            var subCategories = await context.Connection.QueryAsync<SubCategoryDto>(subQuery);

            return (categories, subCategories);
        }

        public async Task<IEnumerable<FeaturedCategoryDto>> GetFeaturedCategoriesAsync()
        {
            var query = "SELECT Name AS KATEGORI_ADI, ItemsCount AS TOPLAM_URUN_SAYISI, Icon AS IKON FROM IdvFeaturedCategories WITH (NOLOCK)";
            return await context.Connection.QueryAsync<FeaturedCategoryDto>(query);
        }

        public async Task<IEnumerable<ManufacturerDto>> GetManufacturersAsync()
        {
            var query = "SELECT * FROM IdvAktifStokMarka WITH (NOLOCK) ORDER BY MARKA_ADI ASC";
            return await context.Connection.QueryAsync<ManufacturerDto>(query);
        }

        public async Task<IEnumerable<ModelDto>> GetModelsByBrandAsync(int brandId)
        {
            var query = "SELECT * FROM IdtAracModel WITH (NOLOCK) WHERE MARKA_KODU=@brandId AND AKTIF=1 ORDER BY MODEL_ADI ASC";
            return await context.Connection.QueryAsync<ModelDto>(query, new { brandId });
        }

        public async Task<IEnumerable<YearDto>> GetYearsByModelAsync(int modelId)
        {
            var query = "SELECT * FROM IdtAracKasaAdı WITH (NOLOCK) WHERE AKTIF=1 AND MODEL_ID=@modelId ORDER BY KASA_ADI ASC";
            return await context.Connection.QueryAsync<YearDto>(query, new { modelId });
        }
    }
}
