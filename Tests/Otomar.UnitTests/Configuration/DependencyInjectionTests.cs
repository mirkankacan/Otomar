using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Contracts.Providers;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Extensions;
using Otomar.Persistence.Extensions;
using Otomar.Persistence.Options;

namespace Otomar.UnitTests.Configuration
{
    /// <summary>
    /// DI container'ın tüm servis ve repository kayıtlarını doğrulayan testler.
    /// Eksik kayıtları runtime yerine test aşamasında yakalar.
    /// </summary>
    public class DependencyInjectionTests
    {
        private readonly IConfiguration _configuration;

        public DependencyInjectionTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnection"] = "Server=.;Database=TestDb;Trusted_Connection=True;TrustServerCertificate=True;",
                    ["JwtOptions:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long-for-hmac",
                    ["JwtOptions:Issuer"] = "test-issuer",
                    ["JwtOptions:Audience"] = "test-audience",
                    ["JwtOptions:AccessTokenExpiration"] = "60",
                    ["JwtOptions:RefreshTokenExpiration"] = "1440",
                    ["RedisOptions:ConnectionString"] = "localhost:6379",
                    ["RedisOptions:InstanceName"] = "TestInstance",
                    ["UiOptions:WebRootPath"] = Path.GetTempPath(),
                    ["EmailOptions:Host"] = "smtp.test.com",
                    ["EmailOptions:Port"] = "587",
                    ["EmailOptions:EnableSsl"] = "true",
                    ["EmailOptions:From"] = "test@test.com",
                    ["PaymentOptions:MerchantId"] = "test",
                    ["PaymentOptions:MerchantKey"] = "test",
                    ["PaymentOptions:MerchantSalt"] = "test",
                    ["ShippingOptions:FreeShippingThreshold"] = "1000",
                    ["ShippingOptions:DefaultShippingFee"] = "50",
                })
                .Build();
        }

        #region Application Layer - Service Registrations

        [Theory]
        [InlineData(typeof(IAuthService))]
        [InlineData(typeof(ICartService))]
        [InlineData(typeof(ICategoryService))]
        [InlineData(typeof(IClientService))]
        [InlineData(typeof(IListSearchService))]
        [InlineData(typeof(INotificationService))]
        [InlineData(typeof(IOrderService))]
        [InlineData(typeof(IPaymentService))]
        [InlineData(typeof(IProductService))]
        public void ApplicationServices_AreRegistered(Type serviceType)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddApplicationServices();

            // Act & Assert
            services.Should().Contain(
                sd => sd.ServiceType == serviceType,
                $"{serviceType.Name} Application katmanında kayıtlı olmalı");
        }

        #endregion

        #region Persistence Layer - Repository Registrations

        [Theory]
        [InlineData(typeof(ICategoryRepository))]
        [InlineData(typeof(IClientRepository))]
        [InlineData(typeof(IListSearchRepository))]
        [InlineData(typeof(INotificationRepository))]
        [InlineData(typeof(IOrderRepository))]
        [InlineData(typeof(IPanelUserRepository))]
        [InlineData(typeof(IPaymentRepository))]
        [InlineData(typeof(IProductRepository))]
        public void PersistenceRepositories_AreRegistered(Type serviceType)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddPersistenceServices(_configuration);

            // Act & Assert
            services.Should().Contain(
                sd => sd.ServiceType == serviceType,
                $"{serviceType.Name} Persistence katmanında kayıtlı olmalı");
        }

        #endregion

        #region Persistence Layer - Infrastructure Service Registrations

        [Theory]
        [InlineData(typeof(IJwtProvider))]
        [InlineData(typeof(IIdentityService))]
        [InlineData(typeof(IEmailService))]
        [InlineData(typeof(IFileService))]
        public void PersistenceInfrastructureServices_AreRegistered(Type serviceType)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddPersistenceServices(_configuration);

            // Act & Assert
            services.Should().Contain(
                sd => sd.ServiceType == serviceType,
                $"{serviceType.Name} Persistence katmanında kayıtlı olmalı");
        }

        #endregion

        #region Full DI Graph Validation

        [Fact]
        public void FullDiGraph_CanBeValidated_WithoutMissingDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddSingleton<IConfiguration>(_configuration);

            // Options — Program.cs'de AddOptionsExtensions() ile kayıt ediliyor
            services.AddOptionsExtensions();

            services.AddApplicationServices();
            services.AddPersistenceServices(_configuration);

            // WebApi katmanında kayıtlı olan IRealtimeNotifier'ı mock olarak ekle
            services.AddScoped<IRealtimeNotifier>(_ => Mock.Of<IRealtimeNotifier>());

            // Act
            var buildAction = () => services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

            // Assert — Eksik kayıt varsa burada patlar
            buildAction.Should().NotThrow(
                "Tüm servis bağımlılıkları DI container'da kayıtlı olmalı. " +
                "Eksik kayıt varsa ilgili servisin ServiceRegistration'ına ekleyin.");
        }

        #endregion
    }
}
