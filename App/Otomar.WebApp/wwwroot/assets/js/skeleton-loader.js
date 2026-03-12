/**
 * Skeleton Loader JavaScript
 * Tüm sayfalarda kullanılabilecek reusable skeleton loader fonksiyonları
 */

const SkeletonLoader = {
    /**
     * Kategori kartları için skeleton loader
     * @param {string} containerId - Container element ID
     * @param {number} count - Kaç adet skeleton gösterileceği
     */
    showCategorySkeleton: function(containerId, count = 6) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        container.innerHTML = '';
        
        for (let i = 0; i < count; i++) {
            const colDiv = document.createElement('div');
            colDiv.className = 'col-lg-2 col-md-4 col-sm-4 col-6 mb-25';
            
            const cardDiv = document.createElement('div');
            cardDiv.className = 'categories__card text-center';
            cardDiv.style.height = '100%';
            cardDiv.style.display = 'flex';
            cardDiv.style.flexDirection = 'column';
            cardDiv.style.padding = '20px';
            
            // Icon skeleton
            const iconSkeleton = document.createElement('div');
            iconSkeleton.className = 'skeleton skeleton-icon';
            
            // Title skeleton
            const titleSkeleton = document.createElement('div');
            titleSkeleton.className = 'skeleton skeleton-title';
            
            // Subtitle skeleton
            const subtitleSkeleton = document.createElement('div');
            subtitleSkeleton.className = 'skeleton skeleton-text';
            subtitleSkeleton.style.width = '60%';
            subtitleSkeleton.style.margin = '8px auto 0';
            
            cardDiv.appendChild(iconSkeleton);
            cardDiv.appendChild(titleSkeleton);
            cardDiv.appendChild(subtitleSkeleton);
            colDiv.appendChild(cardDiv);
            container.appendChild(colDiv);
        }
    },

    /**
     * Ürün kartları için skeleton loader
     * @param {string} containerId - Container element ID
     * @param {number} count - Kaç adet skeleton gösterileceği
     */
    showProductSkeleton: function(containerId, count = 8) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        container.innerHTML = '';
        
        for (let i = 0; i < count; i++) {
            const colDiv = document.createElement('div');
            colDiv.className = 'col-lg-4 col-md-4 col-sm-6 col-6 custom-col mb-30';
            
            const article = document.createElement('article');
            article.className = 'product__card';
            article.style.height = '100%';
            article.style.display = 'flex';
            article.style.flexDirection = 'column';
            
            // Image skeleton
            const imageSkeleton = document.createElement('div');
            imageSkeleton.className = 'skeleton skeleton-image';
            
            // Content div
            const contentDiv = document.createElement('div');
            contentDiv.style.padding = '15px';
            contentDiv.style.flex = '1';
            contentDiv.style.display = 'flex';
            contentDiv.style.flexDirection = 'column';
            
            // Title skeleton
            const titleSkeleton = document.createElement('div');
            titleSkeleton.className = 'skeleton skeleton-title';
            
            // OEM skeleton
            const oemSkeleton = document.createElement('div');
            oemSkeleton.className = 'skeleton skeleton-text';
            oemSkeleton.style.width = '70%';
            oemSkeleton.style.margin = '8px auto';
            
            // Brand logo skeleton
            const brandSkeleton = document.createElement('div');
            brandSkeleton.className = 'skeleton';
            brandSkeleton.style.width = '100px';
            brandSkeleton.style.height = '100px';
            brandSkeleton.style.margin = '8px auto';
            
            // Price skeleton
            const priceSkeleton = document.createElement('div');
            priceSkeleton.className = 'skeleton skeleton-price';
            
            // Button skeleton
            const buttonSkeleton = document.createElement('div');
            buttonSkeleton.className = 'skeleton skeleton-button';
            buttonSkeleton.style.marginTop = '12px';
            
            contentDiv.appendChild(titleSkeleton);
            contentDiv.appendChild(oemSkeleton);
            contentDiv.appendChild(brandSkeleton);
            contentDiv.appendChild(priceSkeleton);
            contentDiv.appendChild(buttonSkeleton);
            
            article.appendChild(imageSkeleton);
            article.appendChild(contentDiv);
            colDiv.appendChild(article);
            container.appendChild(colDiv);
        }
    },

    /**
     * Birden fazla ürün container'ı için skeleton loader
     * @param {Array<string>} containerIds - Container ID'leri dizisi
     * @param {number} count - Her container için kaç adet skeleton
     */
    showMultipleProductSkeletons: function(containerIds, count = 8) {
        containerIds.forEach(containerId => {
            this.showProductSkeleton(containerId, count);
        });
    },

    /**
     * Liste için skeleton loader (genel amaçlı)
     * @param {string} containerId - Container element ID
     * @param {number} count - Kaç adet skeleton gösterileceği
     * @param {Object} options - Özelleştirme seçenekleri
     */
    showListSkeleton: function(containerId, count = 5, options = {}) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        const defaults = {
            hasAvatar: false,
            hasImage: false,
            lineCount: 2,
            className: 'skeleton-list-item'
        };
        
        const config = { ...defaults, ...options };
        container.innerHTML = '';
        
        for (let i = 0; i < count; i++) {
            const item = document.createElement('div');
            item.className = config.className;
            
            if (config.hasAvatar) {
                const avatar = document.createElement('div');
                avatar.className = 'skeleton skeleton-avatar';
                item.appendChild(avatar);
            }
            
            if (config.hasImage) {
                const image = document.createElement('div');
                image.className = 'skeleton skeleton-image-sm';
                image.style.width = '80px';
                item.appendChild(image);
            }
            
            const textContainer = document.createElement('div');
            textContainer.style.flex = '1';
            
            for (let j = 0; j < config.lineCount; j++) {
                const line = document.createElement('div');
                line.className = 'skeleton skeleton-text';
                if (j === config.lineCount - 1) {
                    line.style.width = '70%'; // Son satır daha kısa
                }
                textContainer.appendChild(line);
            }
            
            item.appendChild(textContainer);
            container.appendChild(item);
        }
    },

    /**
     * Kart (card) için skeleton loader (genel amaçlı)
     * @param {string} containerId - Container element ID
     * @param {number} count - Kaç adet skeleton gösterileceği
     * @param {Object} options - Özelleştirme seçenekleri
     */
    showCardSkeleton: function(containerId, count = 4, options = {}) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        const defaults = {
            colClass: 'col-lg-3 col-md-6 col-12',
            hasImage: true,
            imageHeight: '200px',
            lineCount: 3,
            hasButton: false
        };
        
        const config = { ...defaults, ...options };
        container.innerHTML = '';
        
        for (let i = 0; i < count; i++) {
            const colDiv = document.createElement('div');
            colDiv.className = config.colClass + ' mb-4';
            
            const cardDiv = document.createElement('div');
            cardDiv.className = 'skeleton-card';
            
            if (config.hasImage) {
                const imageSkeleton = document.createElement('div');
                imageSkeleton.className = 'skeleton skeleton-image';
                imageSkeleton.style.height = config.imageHeight;
                cardDiv.appendChild(imageSkeleton);
            }
            
            for (let j = 0; j < config.lineCount; j++) {
                const line = document.createElement('div');
                line.className = 'skeleton skeleton-text';
                if (j === 0) {
                    line.className = 'skeleton skeleton-title';
                } else if (j === config.lineCount - 1) {
                    line.style.width = '60%';
                }
                cardDiv.appendChild(line);
            }
            
            if (config.hasButton) {
                const button = document.createElement('div');
                button.className = 'skeleton skeleton-button';
                button.style.marginTop = '12px';
                cardDiv.appendChild(button);
            }
            
            colDiv.appendChild(cardDiv);
            container.appendChild(colDiv);
        }
    },

    /**
     * Tablo için skeleton loader
     * @param {string} containerId - Container element ID (tbody)
     * @param {number} rows - Kaç satır gösterileceği
     * @param {number} columns - Kaç sütun olacağı
     */
    showTableSkeleton: function(containerId, rows = 5, columns = 4) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        container.innerHTML = '';
        
        for (let i = 0; i < rows; i++) {
            const tr = document.createElement('tr');
            
            for (let j = 0; j < columns; j++) {
                const td = document.createElement('td');
                const skeleton = document.createElement('div');
                skeleton.className = 'skeleton skeleton-text';
                td.appendChild(skeleton);
                tr.appendChild(td);
            }
            
            container.appendChild(tr);
        }
    },

    /**
     * Form için skeleton loader
     * @param {string} containerId - Container element ID
     * @param {number} fieldCount - Kaç form alanı gösterileceği
     */
    showFormSkeleton: function(containerId, fieldCount = 4) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container bulunamadı: ${containerId}`);
            return;
        }
        
        container.innerHTML = '';
        
        for (let i = 0; i < fieldCount; i++) {
            const formGroup = document.createElement('div');
            formGroup.className = 'mb-3';
            
            // Label skeleton
            const label = document.createElement('div');
            label.className = 'skeleton skeleton-text';
            label.style.width = '30%';
            label.style.marginBottom = '8px';
            
            // Input skeleton
            const input = document.createElement('div');
            input.className = 'skeleton skeleton-text-lg';
            input.style.height = '40px';
            
            formGroup.appendChild(label);
            formGroup.appendChild(input);
            container.appendChild(formGroup);
        }
    },

    /**
     * Tüm skeleton'ları temizle
     * @param {string} containerId - Container element ID
     */
    clear: function(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = '';
        }
    },

    /**
     * Hata mesajı göster
     * @param {string} containerId - Container element ID
     * @param {string} message - Gösterilecek hata mesajı
     */
    showError: function(containerId, message = 'Veriler yüklenirken bir hata oluştu.') {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = `<div class="col-12 text-center"><p class="text-danger">${message}</p></div>`;
        }
    },

    /**
     * Boş durum mesajı göster
     * @param {string} containerId - Container element ID
     * @param {string} message - Gösterilecek mesaj
     */
    showEmpty: function(containerId, message = 'Henüz veri bulunmamaktadır.') {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = `<div class="col-12 text-center"><p class="text-muted">${message}</p></div>`;
        }
    }
};

// Global scope'a ekle
window.SkeletonLoader = SkeletonLoader;
