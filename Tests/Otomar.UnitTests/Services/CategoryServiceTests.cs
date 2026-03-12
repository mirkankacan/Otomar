using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Services;
using Otomar.Shared.Dtos.Category;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// CategoryService testleri - Tum 7 metod icin happy path ve not-found senaryolari.
/// </summary>
public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ILogger<CategoryService>> _loggerMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _sut = new CategoryService(_categoryRepositoryMock.Object, _loggerMock.Object);
    }

    #region GetBrandsAsync

    [Fact]
    public async Task GetBrandsAsync_BrandsExist_ReturnsSuccess()
    {
        // Arrange
        var brands = new List<BrandDto>
        {
            new() { MARKA_KODU = 1, MARKA_ADI = "Toyota" },
            new() { MARKA_KODU = 2, MARKA_ADI = "Honda" }
        };
        _categoryRepositoryMock
            .Setup(x => x.GetBrandsAsync())
            .ReturnsAsync(brands);

        // Act
        var result = await _sut.GetBrandsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBrandsAsync_NoBrands_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetBrandsAsync())
            .ReturnsAsync(Enumerable.Empty<BrandDto>());

        // Act
        var result = await _sut.GetBrandsAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetBrandsModelsYearsAsync

    [Fact]
    public async Task GetBrandsModelsYearsAsync_DataExists_ReturnsHierarchicalResult()
    {
        // Arrange
        var brands = new List<BrandModelYearDto>
        {
            new() { MARKA_KODU = 1, MARKA_ADI = "Toyota", AKTIF = true },
            new() { MARKA_KODU = 2, MARKA_ADI = "Honda", AKTIF = true }
        };
        var models = new List<ModelDto>
        {
            new() { MODEL_KODU = 10, MODEL_ADI = "Corolla", MARKA_KODU = 1, AKTIF = true },
            new() { MODEL_KODU = 20, MODEL_ADI = "Civic", MARKA_KODU = 2, AKTIF = true }
        };
        var years = new List<YearDto>
        {
            new() { KASA_KODU = 100, KASA_ADI = "2020", MODEL_ID = 10, AKTIF = true },
            new() { KASA_KODU = 101, KASA_ADI = "2021", MODEL_ID = 10, AKTIF = true },
            new() { KASA_KODU = 200, KASA_ADI = "2022", MODEL_ID = 20, AKTIF = true }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetBrandsModelsYearsRawAsync())
            .ReturnsAsync((brands, models, years));

        // Act
        var result = await _sut.GetBrandsModelsYearsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultList = result.Data!.ToList();
        resultList.Should().HaveCount(2);

        // Toyota should have Corolla with 2 years
        var toyota = resultList.First(b => b.MARKA_KODU == 1);
        toyota.ModelsYears.Should().HaveCount(1);
        toyota.ModelsYears.First().MODEL_ADI.Should().Be("Corolla");
        toyota.ModelsYears.First().Years.Should().HaveCount(2);

        // Honda should have Civic with 1 year
        var honda = resultList.First(b => b.MARKA_KODU == 2);
        honda.ModelsYears.Should().HaveCount(1);
        honda.ModelsYears.First().MODEL_ADI.Should().Be("Civic");
        honda.ModelsYears.First().Years.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBrandsModelsYearsAsync_NoBrands_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetBrandsModelsYearsRawAsync())
            .ReturnsAsync((
                Enumerable.Empty<BrandModelYearDto>(),
                new List<ModelDto>(),
                new List<YearDto>()
            ));

        // Act
        var result = await _sut.GetBrandsModelsYearsAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBrandsModelsYearsAsync_NullModels_ReturnsNotFound()
    {
        // Arrange
        var brands = new List<BrandModelYearDto>
        {
            new() { MARKA_KODU = 1, MARKA_ADI = "Toyota", AKTIF = true }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetBrandsModelsYearsRawAsync())
            .ReturnsAsync((brands, (IEnumerable<ModelDto>)null!, new List<YearDto>()));

        // Act
        var result = await _sut.GetBrandsModelsYearsAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBrandsModelsYearsAsync_ModelWithNoYears_ReturnsEmptyYearsCollection()
    {
        // Arrange
        var brands = new List<BrandModelYearDto>
        {
            new() { MARKA_KODU = 1, MARKA_ADI = "Toyota", AKTIF = true }
        };
        var models = new List<ModelDto>
        {
            new() { MODEL_KODU = 10, MODEL_ADI = "Corolla", MARKA_KODU = 1, AKTIF = true }
        };
        var years = Enumerable.Empty<YearDto>();

        _categoryRepositoryMock
            .Setup(x => x.GetBrandsModelsYearsRawAsync())
            .ReturnsAsync((brands, models, years));

        // Act
        var result = await _sut.GetBrandsModelsYearsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var brand = result.Data!.First();
        brand.ModelsYears.First().Years.Should().BeEmpty();
    }

    #endregion

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_CategoriesExist_ReturnsHierarchicalResult()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new() { ANA_ID = 1, GRUP_ID = 10, ANA_GRUP_ADI = "Motor", GRUP_IKON = "icon-motor" },
            new() { ANA_ID = 2, GRUP_ID = 20, ANA_GRUP_ADI = "Elektrik", GRUP_IKON = "icon-elektrik" }
        };
        var subCategories = new List<SubCategoryDto>
        {
            new() { ALT_ID = 1, ALT_GRUP_ID = 100, ALT_GRUP_ADI = "Yakit Sistemi", ANA_GRUP_ID = 10 },
            new() { ALT_ID = 2, ALT_GRUP_ID = 101, ALT_GRUP_ADI = "Sogutma", ANA_GRUP_ID = 10 },
            new() { ALT_ID = 3, ALT_GRUP_ID = 200, ALT_GRUP_ADI = "Aku", ANA_GRUP_ID = 20 }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetCategoriesRawAsync())
            .ReturnsAsync((categories, subCategories));

        // Act
        var result = await _sut.GetCategoriesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultList = result.Data!.ToList();
        resultList.Should().HaveCount(2);

        // Motor should have 2 sub-categories
        var motor = resultList.First(c => c.GRUP_ID == 10);
        motor.SubCategories.Should().HaveCount(2);

        // Elektrik should have 1 sub-category
        var elektrik = resultList.First(c => c.GRUP_ID == 20);
        elektrik.SubCategories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCategoriesAsync_NoCategories_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetCategoriesRawAsync())
            .ReturnsAsync((
                Enumerable.Empty<CategoryDto>(),
                new List<SubCategoryDto>()
            ));

        // Act
        var result = await _sut.GetCategoriesAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetFeaturedCategoriesAsync

    [Fact]
    public async Task GetFeaturedCategoriesAsync_CategoriesExist_ReturnsSuccess()
    {
        // Arrange
        var featuredCategories = new List<FeaturedCategoryDto>
        {
            new() { KATEGORI_ADI = "Motor Yedek Parca", TOPLAM_URUN_SAYISI = "150", IKON = "icon-motor" },
            new() { KATEGORI_ADI = "Elektrik", TOPLAM_URUN_SAYISI = "80", IKON = "icon-elektrik" }
        };
        _categoryRepositoryMock
            .Setup(x => x.GetFeaturedCategoriesAsync())
            .ReturnsAsync(featuredCategories);

        // Act
        var result = await _sut.GetFeaturedCategoriesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFeaturedCategoriesAsync_NoCategories_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetFeaturedCategoriesAsync())
            .ReturnsAsync(Enumerable.Empty<FeaturedCategoryDto>());

        // Act
        var result = await _sut.GetFeaturedCategoriesAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetManufacturersAsync

    [Fact]
    public async Task GetManufacturersAsync_ManufacturersExist_ReturnsSuccess()
    {
        // Arrange
        var manufacturers = new List<ManufacturerDto>
        {
            new() { MARKA_ID = 1, MARKA_ADI = "Bosch", MARKA_KODU = "BSH", MARKA_LOGO = "bosch.png" },
            new() { MARKA_ID = 2, MARKA_ADI = "Denso", MARKA_KODU = "DNS", MARKA_LOGO = "denso.png" }
        };
        _categoryRepositoryMock
            .Setup(x => x.GetManufacturersAsync())
            .ReturnsAsync(manufacturers);

        // Act
        var result = await _sut.GetManufacturersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetManufacturersAsync_NoManufacturers_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(x => x.GetManufacturersAsync())
            .ReturnsAsync(Enumerable.Empty<ManufacturerDto>());

        // Act
        var result = await _sut.GetManufacturersAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetModelsByBrandAsync

    [Fact]
    public async Task GetModelsByBrandAsync_ModelsExist_ReturnsSuccess()
    {
        // Arrange
        var brandId = 1;
        var models = new List<ModelDto>
        {
            new() { MODEL_KODU = 10, MODEL_ADI = "Corolla", MARKA_KODU = brandId, AKTIF = true },
            new() { MODEL_KODU = 20, MODEL_ADI = "Yaris", MARKA_KODU = brandId, AKTIF = true }
        };
        _categoryRepositoryMock
            .Setup(x => x.GetModelsByBrandAsync(brandId))
            .ReturnsAsync(models);

        // Act
        var result = await _sut.GetModelsByBrandAsync(brandId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetModelsByBrandAsync_NoModels_ReturnsNotFound()
    {
        // Arrange
        var brandId = 999;
        _categoryRepositoryMock
            .Setup(x => x.GetModelsByBrandAsync(brandId))
            .ReturnsAsync(Enumerable.Empty<ModelDto>());

        // Act
        var result = await _sut.GetModelsByBrandAsync(brandId);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetYearsByModelAsync

    [Fact]
    public async Task GetYearsByModelAsync_YearsExist_ReturnsSuccess()
    {
        // Arrange
        var modelId = 10;
        var years = new List<YearDto>
        {
            new() { KASA_KODU = 100, KASA_ADI = "2020 Sedan", MODEL_ID = modelId, AKTIF = true },
            new() { KASA_KODU = 101, KASA_ADI = "2021 Sedan", MODEL_ID = modelId, AKTIF = true }
        };
        _categoryRepositoryMock
            .Setup(x => x.GetYearsByModelAsync(modelId))
            .ReturnsAsync(years);

        // Act
        var result = await _sut.GetYearsByModelAsync(modelId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetYearsByModelAsync_NoYears_ReturnsNotFound()
    {
        // Arrange
        var modelId = 999;
        _categoryRepositoryMock
            .Setup(x => x.GetYearsByModelAsync(modelId))
            .ReturnsAsync(Enumerable.Empty<YearDto>());

        // Act
        var result = await _sut.GetYearsByModelAsync(modelId);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
