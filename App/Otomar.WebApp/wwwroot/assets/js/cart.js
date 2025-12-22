// wwwroot/js/cart.js - Sepet işlemleri için örnek JavaScript
const CartManager = {
    apiUrl: '/api/cart',

    // Sepeti getir
    async getCart() {
        try {
            const response = await fetch(this.apiUrl);
            if (response.ok) {
                return await response.json();
            }
            return null;
        } catch (error) {
            console.error('Sepet getirme hatası:', error);
            return null;
        }
    },

    // Sepete ürün ekle
    async addToCart(productId, quantity = 1) {
        try {
            const response = await fetch(`${this.apiUrl}/items`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ productId, quantity })
            });

            if (response.ok) {
                const cart = await response.json();
                this.updateCartUI(cart);
                this.showNotification('Ürün sepete eklendi', 'success');
                return cart;
            } else {
                const error = await response.text();
                this.showNotification(error || 'Ürün eklenemedi', 'error');
                return null;
            }
        } catch (error) {
            console.error('Sepete ekleme hatası:', error);
            this.showNotification('Bir hata oluştu', 'error');
            return null;
        }
    },

    // Sepetteki ürün miktarını güncelle
    async updateCartItem(productId, quantity) {
        try {
            const response = await fetch(`${this.apiUrl}/items`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ productId, quantity })
            });

            if (response.ok) {
                const cart = await response.json();
                this.updateCartUI(cart);
                return cart;
            }
            return null;
        } catch (error) {
            console.error('Güncelleme hatası:', error);
            return null;
        }
    },

    // Sepetten ürün sil
    async removeFromCart(productId) {
        try {
            const response = await fetch(`${this.apiUrl}/items/${productId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                const cart = await response.json();
                this.updateCartUI(cart);
                this.showNotification('Ürün sepetten silindi', 'success');
                return cart;
            }
            return null;
        } catch (error) {
            console.error('Silme hatası:', error);
            return null;
        }
    },

    // Sepeti temizle
    async clearCart() {
        if (!confirm('Sepeti tamamen temizlemek istediğinizden emin misiniz?')) {
            return;
        }

        try {
            const response = await fetch(this.apiUrl, {
                method: 'DELETE'
            });

            if (response.ok) {
                this.updateCartUI({ Items: [], SubTotal: 0, ShippingCost: 0, Total: 0, ItemCount: 0 });
                this.showNotification('Sepet temizlendi', 'success');
                return true;
            }
            return false;
        } catch (error) {
            console.error('Temizleme hatası:', error);
            return false;
        }
    },

    // UI güncelleme
    updateCartUI(cart) {
        // Sepet badge'ini güncelle
        const badge = document.querySelector('.cart-badge');
        if (badge) {
            badge.textContent = cart.ItemCount || 0;
            badge.style.display = cart.ItemCount > 0 ? 'inline-block' : 'none';
        }

        // Sepet toplam tutarını güncelle
        const totalElement = document.querySelector('.cart-total');
        if (totalElement) {
            totalElement.textContent = cart.Total.toFixed(2) + ' ₺';
        }

        // Sepet sayfasındaysak listeyi güncelle
        const cartList = document.querySelector('#cart-items');
        if (cartList) {
            this.renderCartItems(cart);
        }
    },

    // Sepet öğelerini render et
    renderCartItems(cart) {
        const cartList = document.querySelector('#cart-items');
        if (!cartList) return;

        if (!cart.Items || cart.Items.length === 0) {
            cartList.innerHTML = '<div class="empty-cart">Sepetiniz boş</div>';
            return;
        }

        let html = '';
        cart.Items.forEach(item => {
            html += `
                <div class="cart-item" data-product-id="${item.ProductId}">
                    <img src="${item.ImagePath}" alt="${item.ProductName}">
                    <div class="item-details">
                        <h4>${item.ProductName}</h4>
                        <p class="item-code">${item.ProductCode}</p>
                        <p class="item-price">${item.UnitPrice.toFixed(2)} ₺</p>
                    </div>
                    <div class="item-quantity">
                        <button onclick="CartManager.decreaseQuantity(${item.ProductId})">-</button>
                        <input type="number" value="${item.Quantity}"
                               min="1" max="${item.StockQuantity || 999}"
                               onchange="CartManager.updateCartItem(${item.ProductId}, this.value)">
                        <button onclick="CartManager.increaseQuantity(${item.ProductId})">+</button>
                    </div>
                    <div class="item-total">
                        ${(item.UnitPrice * item.Quantity).toFixed(2)} ₺
                    </div>
                    <button class="remove-btn" onclick="CartManager.removeFromCart(${item.ProductId})">
                        <i class="fa fa-trash"></i>
                    </button>
                </div>
            `;
        });

        // Toplam bilgilerini ekle
        html += `
            <div class="cart-summary">
                <div class="summary-row">
                    <span>Ara Toplam:</span>
                    <span>${cart.SubTotal.toFixed(2)} ₺</span>
                </div>
                <div class="summary-row">
                    <span>Kargo:</span>
                    <span>${cart.ShippingCost.toFixed(2)} ₺</span>
                </div>
                <div class="summary-row total">
                    <span>Toplam:</span>
                    <span>${cart.Total.toFixed(2)} ₺</span>
                </div>
            </div>
        `;

        cartList.innerHTML = html;
    },

    // Miktar artır
    async increaseQuantity(productId) {
        const input = document.querySelector(`[data-product-id="${productId}"] input[type="number"]`);
        if (input) {
            const newQuantity = parseInt(input.value) + 1;
            await this.updateCartItem(productId, newQuantity);
        }
    },

    // Miktar azalt
    async decreaseQuantity(productId) {
        const input = document.querySelector(`[data-product-id="${productId}"] input[type="number"]`);
        if (input) {
            const newQuantity = Math.max(1, parseInt(input.value) - 1);
            await this.updateCartItem(productId, newQuantity);
        }
    },

    // Bildirim göster
    showNotification(message, type = 'info') {
        // Basit alert yerine daha güzel bir notification sistemi kullanabilirsiniz
        // Örn: Toastr, SweetAlert, vb.
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 3000);
    },

    // Sayfa yüklendiğinde sepeti getir
    async init() {
        const cart = await this.getCart();
        if (cart) {
            this.updateCartUI(cart);
        }
    }
};

// Sayfa yüklendiğinde çalıştır
document.addEventListener('DOMContentLoaded', () => {
    CartManager.init();
});

// jQuery ile kullanım örneği
$(document).ready(function () {
    // Sepete ekle butonları
    $('.add-to-cart-btn').on('click', function () {
        const productId = $(this).data('product-id');
        const quantity = $(this).closest('.product-card').find('.quantity-input').val() || 1;
        CartManager.addToCart(productId, parseInt(quantity));
    });

    // Hızlı sepete ekle
    $('.quick-add-btn').on('click', function () {
        const productId = $(this).data('product-id');
        CartManager.addToCart(productId, 1);
    });
});