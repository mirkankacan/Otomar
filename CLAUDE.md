# Otomar Projesi - Geliştirici Notları

## Proje Özeti
Otomar, oto yedek parça e-ticaret platformu. .NET 10.0, Clean Architecture.

## Mimari Yapı

```
Core/
├── Otomar.Contracts    → DTO, Enum, ServiceResult, PagedResult (sıfır bağımlılık)
├── Otomar.Domain       → Identity entity'leri (ApplicationUser, ApplicationRole, UserGlobalFilter)
├── Otomar.Application  → Service interface'leri + Refit ServiceResultExtensions
Infrastructure/
├── Otomar.Persistance  → Dapper ile service implementasyonları, EF Core sadece Identity için
Api/
├── Otomar.WebApi       → Carter Minimal API endpoints
App/
├── Otomar.WebApp       → MVC frontend, Refit HTTP client ile WebApi'ye bağlanır
Tests/
├── Otomar.UnitTests    → xUnit + Moq + FluentAssertions (226 test)
```

## Bağımlılık Zinciri
```
Otomar.Contracts (sıfır bağımlılık)
    ↑
Otomar.Domain (Identity)
    ↑
Otomar.Application (Domain + Contracts + Refit)
    ↑
Otomar.Persistance (Application + Dapper + MailKit + Redis)
    ↑
Otomar.WebApi (Persistance + Carter)

Otomar.Contracts
    ↑
Otomar.WebApp (Contracts + Refit) — WebApi'ye HTTP ile bağlanır
```

## Teknoloji Stack
- **ORM**: Dapper (iş tabloları), EF Core (sadece Identity)
- **API**: Carter Minimal APIs
- **Frontend**: MVC + Razor Views
- **API Client**: Refit (WebApp → WebApi)
- **Ödeme**: İş Bankası 3D Secure (IsBank)
- **Cache**: Redis (sepet), IMemoryCache (feed/sitemap)
- **Email**: MailKit + HTML template
- **Test**: xUnit, Moq, FluentAssertions

## Bilinen Mimari Eksiklikler (Refactoring Sırası)

### 1. Repository Pattern YOK (öncelikli)
- Service'ler SQL'i doğrudan Dapper ile çalıştırıyor
- Bu yüzden unit test'ler sadece input validation test edebiliyor
- Dapper extension method'ları (QueryAsync, ExecuteAsync) mock'lanamıyor
- **Çözüm**: IOrderRepository, IPaymentRepository vb. oluştur, SQL'leri oraya taşı

### 2. Use Case YOK
- Application katmanı sadece interface deposu, iş mantığı yok
- Tüm business logic Persistance/Services içinde
- **Çözüm**: Repository pattern eklendikten sonra Use Case'leri Application'a taşı
- Örnek: CompletePaymentUseCase, CreatePurchaseOrderUseCase

### 3. Anemic Domain Model
- Domain'de sadece 3 Identity entity var, iş entity'leri yok
- Tüm iş mantığı service'lerde (SRP ihlali)

### 4. EmailService Circular Dependency
- IServiceProvider kullanarak runtime'da diğer servisleri resolve ediyor
- Use Case pattern ile çözülecek

## Tamamlanan İyileştirmeler
- [x] Otomar.Contracts projesi oluşturuldu (DTO/Enum/Common duplication giderildi)
- [x] 226 unit test yazıldı (helper'lar, handler'lar, service validation'lar)
- [x] ServiceResult'tan Refit bağımlılığı ayrıldı (ServiceResultExtensions)

## Sonraki Adımlar
1. Repository Pattern ekle
2. Use Case'leri Application katmanına taşı
3. Domain entity'lerini zenginleştir

## Önemli Dosyalar
- Ödeme akışı: `Infrastructure/Otomar.Persistance/Services/PaymentService.cs` (~480 satır)
- Sipariş: `Infrastructure/Otomar.Persistance/Services/OrderService.cs` (~1230 satır)
- Ürün filtreleme: `Infrastructure/Otomar.Persistance/Services/ProductService.cs`
- 3D Secure helper: `Infrastructure/Otomar.Persistance/Helpers/IsBankHelper.cs`
- WebApp-only DTO'lar: `App/Otomar.WebApp/Dtos/Contact/`, `App/Otomar.WebApp/Dtos/Options/`

## Dil
Kullanıcı ile Türkçe iletişim kurulur.
