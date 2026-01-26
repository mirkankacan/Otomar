# Skeleton Loader Kullanım Kılavuzu

## Kurulum

Skeleton loader dosyaları `_Layout.cshtml` içinde otomatik olarak yüklenmiştir:

```html
<!-- CSS (head bölümünde) -->
<link rel="stylesheet" href="~/assets/css/skeleton-loader.css">

<!-- JavaScript (body sonunda) -->
<script src="~/assets/js/skeleton-loader.js"></script>
```

## Kullanım Örnekleri

### 1. Kategori Skeleton Loader

```javascript
// Sayfa yüklenirken
$(document).ready(function() {
    // 6 adet kategori skeleton göster
    SkeletonLoader.showCategorySkeleton('categories-container', 6);
    
    // Verileri yükle
    loadCategories();
});

async function loadCategories() {
    try {
        const response = await makeRequest('/api/categories', 'GET');
        
        if (!response || !Array.isArray(response)) {
            SkeletonLoader.showError('categories-container', 'Kategoriler yüklenemedi.');
            return;
        }
        
        if (response.length === 0) {
            SkeletonLoader.showEmpty('categories-container', 'Henüz kategori bulunmamaktadır.');
            return;
        }
        
        // Skeleton'ı temizle
        SkeletonLoader.clear('categories-container');
        
        // Gerçek verileri render et
        renderCategories(response);
        
    } catch (error) {
        SkeletonLoader.showError('categories-container');
    }
}
```

### 2. Ürün Skeleton Loader

```javascript
// Tek bir container için
SkeletonLoader.showProductSkeleton('products-container', 8);

// Birden fazla container için (tab'lar)
SkeletonLoader.showMultipleProductSkeletons(
    ['products-recommended', 'products-bestseller', 'products-latest'],
    8
);
```

### 3. Genel Amaçlı Kart Skeleton

```javascript
// Varsayılan ayarlar
SkeletonLoader.showCardSkeleton('cards-container', 4);

// Özelleştirilmiş
SkeletonLoader.showCardSkeleton('cards-container', 6, {
    colClass: 'col-lg-4 col-md-6 col-12',
    hasImage: true,
    imageHeight: '250px',
    lineCount: 3,
    hasButton: true
});
```

### 4. Liste Skeleton

```javascript
// Basit liste
SkeletonLoader.showListSkeleton('list-container', 5);

// Avatar'lı liste (kullanıcı listesi gibi)
SkeletonLoader.showListSkeleton('users-list', 10, {
    hasAvatar: true,
    lineCount: 2
});

// Resimli liste (blog yazıları gibi)
SkeletonLoader.showListSkeleton('posts-list', 5, {
    hasImage: true,
    lineCount: 3,
    className: 'post-item'
});
```

### 5. Tablo Skeleton

```javascript
// 5 satır, 4 sütun
SkeletonLoader.showTableSkeleton('table-body', 5, 4);

// 10 satır, 6 sütun
SkeletonLoader.showTableSkeleton('data-table-body', 10, 6);
```

### 6. Form Skeleton

```javascript
// 4 form alanı
SkeletonLoader.showFormSkeleton('form-container', 4);

// 8 form alanı
SkeletonLoader.showFormSkeleton('checkout-form', 8);
```

## Utility Fonksiyonları

```javascript
// Skeleton'ı temizle
SkeletonLoader.clear('container-id');

// Hata mesajı göster
SkeletonLoader.showError('container-id', 'Özel hata mesajı');

// Boş durum mesajı göster
SkeletonLoader.showEmpty('container-id', 'Veri bulunamadı');
```

## Sadece CSS Kullanımı (HTML içinde)

Eğer JavaScript kullanmak istemiyorsanız, doğrudan HTML'de skeleton yapısı oluşturabilirsiniz:

```html
<!-- Basit skeleton -->
<div class="skeleton skeleton-text"></div>

<!-- Resim skeleton -->
<div class="skeleton skeleton-image"></div>

<!-- Buton skeleton -->
<div class="skeleton skeleton-button"></div>

<!-- Avatar skeleton -->
<div class="skeleton skeleton-avatar"></div>

<!-- Özel boyut -->
<div class="skeleton" style="width: 200px; height: 50px;"></div>

<!-- Utility class'lar ile -->
<div class="skeleton skeleton-w-50 skeleton-h-100"></div>

<!-- Shimmer animasyonu -->
<div class="skeleton-shimmer" style="width: 100%; height: 200px;"></div>

<!-- Pulse animasyonu -->
<div class="skeleton skeleton-pulse" style="width: 100%; height: 50px;"></div>
```

## Tam Örnek: Dinamik Ürün Listesi

```javascript
$(document).ready(async function() {
    const containerId = 'products-grid';
    
    // 1. Skeleton göster
    SkeletonLoader.showProductSkeleton(containerId, 12);
    
    // 2. Verileri yükle
    try {
        const products = await makeRequest('/api/products', 'GET');
        
        // 3a. Boş kontrol
        if (!products || products.length === 0) {
            SkeletonLoader.showEmpty(containerId, 'Henüz ürün bulunmamaktadır.');
            return;
        }
        
        // 3b. Skeleton'ı temizle
        SkeletonLoader.clear(containerId);
        
        // 3c. Gerçek verileri render et
        products.forEach(product => {
            // Ürün kartı oluştur ve ekle
            const card = createProductCard(product);
            document.getElementById(containerId).appendChild(card);
        });
        
    } catch (error) {
        // 4. Hata durumunda
        SkeletonLoader.showError(containerId, 'Ürünler yüklenirken bir hata oluştu.');
    }
});
```

## Avantajları

1. **Tek Merkez**: Tüm skeleton stilleri ve fonksiyonları tek yerde
2. **Tutarlılık**: Tüm sayfalarda aynı görünüm
3. **Kolay Bakım**: Tek dosyada değişiklik yaparsınız
4. **Performans**: Harici kütüphane yok, hafif ve hızlı
5. **Özelleştirme**: İstediğiniz gibi düzenleyebilirsiniz
6. **Lazy Loading**: Tüm resimler otomatik lazy load
7. **Dark Mode**: Otomatik dark mode desteği

## Animasyon ve Renk Seçenekleri

### Renk Paleti
- **Light Mode**: 
  - Base: `#f0f0f0` (çok açık gri)
  - Shimmer: `#f8f8f8` (beyaza yakın)
  - Card: `#fafafa` (arka plan)
  - Border: `#e6e6e6` (çok açık border)
- **Dark Mode**: Otomatik geçiş (#2a2a2a)

### Animasyon Stilleri

CSS'te 3 farklı animasyon stili mevcuttur:

1. **Default (skeleton)**: Yumuşak gradient shimmer (1.5s + 2s çift animasyon)
2. **Pulse (skeleton-pulse)**: Nabız gibi opacity animasyonu (2s)
3. **Shimmer (skeleton-shimmer)**: Yoğun ışıltı efekti (1.5s)

Kullanım:
```html
<div class="skeleton"></div>           <!-- Default - Önerilen -->
<div class="skeleton-pulse"></div>     <!-- Pulse -->
<div class="skeleton-shimmer"></div>   <!-- Shimmer -->
```

**Not**: Default shimmer efekti hem gradient hem de sweep animasyonu kullanarak en modern görünümü sağlar.

## CSS Utility Class'lar

- `.skeleton-w-25` - Width 25%
- `.skeleton-w-50` - Width 50%
- `.skeleton-w-75` - Width 75%
- `.skeleton-w-100` - Width 100%
- `.skeleton-h-100` - Height 100px
- `.skeleton-h-200` - Height 200px
- `.skeleton-h-300` - Height 300px

## Başka Sayfalarda Kullanım

Herhangi bir Razor view'da kullanmak için:

```cshtml
@section Scripts {
    <script>
        $(document).ready(async function() {
            // Skeleton göster
            SkeletonLoader.showProductSkeleton('my-container', 10);
            
            // Veri yükle
            await loadMyData();
        });
    </script>
}
```

CSS ve JS dosyaları zaten global olarak yüklendiği için ekstra import gerekmez!
