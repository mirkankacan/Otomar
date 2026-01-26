# Global Skeleton Loader Sistemi

## 📋 Genel Bakış

Tüm sayfalarda kullanılabilen, performanslı ve özelleştirilebilir skeleton loader sistemi.

## 🎯 Neden Harici Kütüphane Değil?

### ✅ Kendi Çözümümüzün Avantajları:
1. **Sıfır Bağımlılık**: Ekstra HTTP isteği yok, harici sunuculara bağımlılık yok
2. **Tam Kontrol**: İhtiyacınıza göre özelleştirebilirsiniz
3. **Performans**: Minimal CSS/JS, hızlı yükleme
4. **Bakım Kolaylığı**: Harici güncellemelere bağımlı değilsiniz
5. **Proje Tutarlılığı**: Tasarımınıza tam uyumlu
6. **Türkçe Destek**: Hata mesajları Türkçe

### ❌ Harici Kütüphane Dezavantajları:
- Ekstra HTTP isteği (performans kaybı)
- Kullanmadığınız özellikler için gereksiz kod
- Güncelleme zorunluluğu
- Özelleştirme sınırlamaları
- Bağımlılık riski

## 📁 Dosya Yapısı

```
wwwroot/assets/
├── css/
│   └── skeleton-loader.css          # Global CSS stilleri
└── js/
    ├── skeleton-loader.js            # Global JavaScript fonksiyonları
    ├── skeleton-loader-examples.md   # Kullanım örnekleri
    └── skeleton-loader-template.cshtml # Hızlı başlangıç template'i
```

## 🚀 Hızlı Başlangıç

### 1. Dosyalar Hazır
CSS ve JS dosyaları `_Layout.cshtml` içinde otomatik yüklenir. Hiçbir şey eklemenize gerek yok.

### 2. Herhangi Bir Sayfada Kullanın

```cshtml
<!-- YourPage.cshtml -->
<div class="container">
    <div class="row" id="my-content">
        <!-- Dinamik içerik buraya gelecek -->
    </div>
</div>

@section Scripts {
    <script>
        $(document).ready(async function() {
            // Skeleton göster
            SkeletonLoader.showProductSkeleton('my-content', 8);
            
            // Veri yükle
            const data = await makeRequest('/api/data', 'GET');
            
            // Skeleton temizle
            SkeletonLoader.clear('my-content');
            
            // Verileri render et
            renderData(data);
        });
    </script>
}
```

## 🎨 Renk ve Animasyon

Skeleton loader modern ve yumuşak renkler kullanır:
- **Light Mode**: Açık gri (#f0f0f0, #e6e6e6)
- **Shimmer Effect**: Beyaz gradient geçişi ile parlak animasyon
- **Animation Duration**: 1.5-2 saniye yumuşak geçişler
- **Dark Mode**: Otomatik tema desteği (#2a2a2a)

### Animasyon Stilleri
1. **Default**: Yumuşak gradient shimmer (önerilen)
2. **Pulse**: Opacity tabanlı nabız efekti
3. **Shimmer**: Yoğun ışıltı efekti

## 🎨 Mevcut Skeleton Tipleri

| Fonksiyon | Kullanım Alanı | Parametreler |
|-----------|---------------|--------------|
| `showCategorySkeleton()` | Kategori kartları | containerId, count |
| `showProductSkeleton()` | Ürün kartları | containerId, count |
| `showMultipleProductSkeletons()` | Çoklu ürün container'ları | containerIds[], count |
| `showCardSkeleton()` | Genel kartlar | containerId, count, options |
| `showListSkeleton()` | Liste elemanları | containerId, count, options |
| `showTableSkeleton()` | Tablo satırları | containerId, rows, columns |
| `showFormSkeleton()` | Form alanları | containerId, fieldCount |
| `clear()` | Skeleton temizleme | containerId |
| `showError()` | Hata mesajı | containerId, message |
| `showEmpty()` | Boş durum mesajı | containerId, message |

## 🎭 CSS Class'ları

### Temel Class'lar
- `.skeleton` - Temel skeleton sınıfı (default animasyon)
- `.skeleton-pulse` - Nabız animasyonu
- `.skeleton-shimmer` - Işıltı animasyonu

### Boyut Class'ları
- `.skeleton-text` - Normal metin (16px)
- `.skeleton-text-sm` - Küçük metin (12px)
- `.skeleton-text-lg` - Büyük metin (20px)
- `.skeleton-title` - Başlık
- `.skeleton-subtitle` - Alt başlık
- `.skeleton-icon` - Icon (50x50px)
- `.skeleton-icon-sm` - Küçük icon (30x30px)
- `.skeleton-icon-lg` - Büyük icon (80x80px)
- `.skeleton-image` - Resim (200px yükseklik)
- `.skeleton-image-sm` - Küçük resim (120px)
- `.skeleton-image-lg` - Büyük resim (300px)
- `.skeleton-avatar` - Avatar (40x40px yuvarlak)
- `.skeleton-price` - Fiyat
- `.skeleton-button` - Buton (40px)
- `.skeleton-button-sm` - Küçük buton (32px)
- `.skeleton-button-lg` - Büyük buton (48px)

### Utility Class'lar
- `.skeleton-w-25` / `.skeleton-w-50` / `.skeleton-w-75` / `.skeleton-w-100`
- `.skeleton-h-100` / `.skeleton-h-200` / `.skeleton-h-300`

## 🌓 Dark Mode Desteği

Skeleton loader otomatik olarak sistem dark mode tercihini algılar ve stilleri buna göre ayarlar.

## 💡 İpuçları

1. **Doğru Sayıyı Seçin**: Genelde görünen alan için yeterli skeleton gösterin (8-12 ürün)
2. **Hızlı Göster**: Skeleton'ı sayfa yüklenir yüklenmez gösterin
3. **Temizlemeyi Unutmayın**: Veri geldiğinde `SkeletonLoader.clear()` çağırın
4. **Hata Yönetimi**: Her zaman try-catch kullanın ve `showError()` ile kullanıcıyı bilgilendirin
5. **Lazy Loading**: Tüm resimlere `loading="lazy"` ekleyin

## 📱 Responsive Davranış

Skeleton'lar Bootstrap grid sistemi ile uyumludur:
- Desktop: 4 kolon (col-lg-3)
- Tablet: 3 kolon (col-md-4)
- Mobile: 2 kolon (col-sm-6, col-6)

## 🔧 Özelleştirme

### CSS'i Özelleştirmek
`assets/css/skeleton-loader.css` dosyasını düzenleyin:

```css
/* Örnek: Animasyon hızını değiştir */
@keyframes skeleton-loading {
    /* ... */
    animation-duration: 0.8s; /* 1s yerine 0.8s */
}

/* Örnek: Yeni bir skeleton tipi ekle */
.skeleton-card-header {
    width: 100%;
    height: 60px;
    margin-bottom: 16px;
    border-radius: 8px;
}
```

### JavaScript'i Özelleştirmek
`assets/js/skeleton-loader.js` dosyasına yeni fonksiyon ekleyin:

```javascript
SkeletonLoader.showCustomSkeleton = function(containerId, options) {
    // Özel skeleton implementasyonunuz
};
```

## 📊 Kullanım İstatistikleri

Mevcut kullanım yerleri:
- ✅ Home/Index.cshtml - Kategoriler ve ürünler
- 🔜 Diğer sayfalar (eklediğinizde buraya ekleyin)

## 🆘 Sorun Giderme

### Skeleton Görünmüyor
1. `_Layout.cshtml` içinde CSS ve JS dosyalarının yüklendiğinden emin olun
2. Tarayıcı konsolunda hata olup olmadığını kontrol edin
3. Container ID'sinin doğru olduğundan emin olun

### Animasyon Çalışmıyor
1. CSS dosyasının yüklendiğini kontrol edin
2. Tarayıcı uyumluluğunu kontrol edin (modern tarayıcılarda çalışır)

### Container Bulunamadı
```javascript
// Container'ın DOM'da olduğundan emin olun
if (!document.getElementById('my-container')) {
    console.error('Container bulunamadı!');
}
```

## 📝 Lisans

Bu sistem Otomar projesi için geliştirilmiştir ve proje içinde serbestçe kullanılabilir.
