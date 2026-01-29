// wwwroot/assets/js/cart.js - CartController (/sepet) ile uyumlu, http-client.js kullanır
const CartManager = {
    baseUrl: '/sepet',

    // makeRequest (http-client.js) yoksa fetch ile yedek
    async _request(url, method, data) {
        if (typeof makeRequest === 'function') {
            return makeRequest(url, method, data);
        }
        const options = { method, headers: { 'Content-Type': 'application/json' } };
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token && ['POST', 'PUT', 'DELETE'].includes(method)) options.headers['X-CSRF-TOKEN'] = token;
        if (data && ['POST', 'PUT'].includes(method)) options.body = JSON.stringify(data);
        const res = await fetch(url, options);
        if (!res.ok) throw { detail: await res.text() };
        const ct = res.headers.get('content-type');
        return ct && ct.includes('application/json') ? res.json() : null;
    },

    // Sepeti getir - GET /sepet/getir
    async getCart() {
        try {
            return await this._request(`${this.baseUrl}/getir`, 'GET');
        } catch (e) {
            console.error('Sepet getirme hatası:', e);
            return null;
        }
    },

    // Sepete ürün ekle - POST /sepet/ekle
    async addToCart(productId, quantity = 1) {
        try {
            const cart = await this._request(`${this.baseUrl}/ekle`, 'POST', { productId, quantity });
            this.updateCartUI(cart);
            this.showNotification('Ürün sepete eklendi', 'success');
            return cart;
        } catch (e) {
            const msg = (e && (e.detail || e.title)) || 'Ürün eklenemedi';
            this.showNotification(typeof msg === 'string' ? msg : 'Ürün eklenemedi', 'error');
            return null;
        }
    },

    // Sepetteki ürün miktarını güncelle - PUT /sepet/guncelle
    async updateCartItem(productId, quantity) {
        try {
            const cart = await this._request(`${this.baseUrl}/guncelle`, 'PUT', { productId, quantity });
            this.updateCartUI(cart);
            return cart;
        } catch (e) {
            console.error('Güncelleme hatası:', e);
            return null;
        }
    },

    // Sepetten ürün sil - DELETE /sepet/sil/{productId}
    async removeFromCart(productId) {
        try {
            const cart = await this._request(`${this.baseUrl}/sil/${productId}`, 'DELETE');
            this.updateCartUI(cart);
            this.showNotification('Ürün sepetten silindi', 'success');
            return cart;
        } catch (e) {
            console.error('Silme hatası:', e);
            return null;
        }
    },

    // Sepeti temizle - DELETE /sepet/temizle
    async clearCart() {
        if (!confirm('Sepeti tamamen temizlemek istediğinizden emin misiniz?')) return;
        try {
            await this._request(`${this.baseUrl}/temizle`, 'DELETE');
            this.updateCartUI({ items: [], subTotal: 0, shippingCost: 0, total: 0, itemCount: 0 });
            this.showNotification('Sepet temizlendi', 'success');
            return true;
        } catch (e) {
            console.error('Temizleme hatası:', e);
            return false;
        }
    },

    // Sepeti yenile - POST /sepet/yenile
    async refreshCart() {
        try {
            const cart = await this._request(`${this.baseUrl}/yenile`, 'POST');
            this.updateCartUI(cart);
            return cart;
        } catch (e) {
            console.error('Sepet yenileme hatası:', e);
            return null;
        }
    },

    // API yanıtındaki alan adları (PascalCase veya camelCase) için uyumlu erişim
    normalizeCart(cart) {
        if (!cart) return cart;
        return {
            Items: cart.items ?? cart.Items ?? [],
            SubTotal: cart.subTotal ?? cart.SubTotal ?? 0,
            ShippingCost: cart.shippingCost ?? cart.ShippingCost ?? 0,
            Total: cart.total ?? cart.Total ?? 0,
            ItemCount: cart.itemCount ?? cart.ItemCount ?? 0
        };
    },

    // Türkçe tutar formatı: layout'taki formatTurkishLira varsa onu kullan, yoksa basit format
    formatPrice(value) {
        const num = value ?? 0;
        if (typeof formatTurkishLira === 'function') {
            return formatTurkishLira(num);
        }
        return num.toFixed(2).replace('.', ',') + ' ₺';
    },

    // UI güncelleme (header + offcanvas minicart)
    updateCartUI(cart) {
        const c = this.normalizeCart(cart);
        const count = c.ItemCount ?? 0;
        const total = c.Total ?? 0;
        const subTotal = c.SubTotal ?? 0;
        const shipping = c.ShippingCost ?? 0;

        // Sepet adet badge'leri (header + sticky)
        document.querySelectorAll('.cart-badge, .items__count').forEach(el => {
            el.textContent = count;
            el.style.display = count > 0 ? '' : 'none';
        });

        // Header'daki toplam (minicart__btn--text__price)
        document.querySelectorAll('.minicart__btn--text__price.cart-total, .cart-total').forEach(el => {
            el.textContent = this.formatPrice(total);
        });

        // Offcanvas minicart tutarları
        const subEl = document.getElementById('cart-amount-subtotal');
        const shipEl = document.getElementById('cart-amount-shipping');
        const totalEl = document.getElementById('cart-amount-total');
        if (subEl) subEl.textContent = this.formatPrice(subTotal);
        if (shipEl) shipEl.textContent = this.formatPrice(shipping);
        if (totalEl) totalEl.textContent = this.formatPrice(total);

        // Sepet listesi (offcanvas #cart-items)
        const cartList = document.querySelector('#cart-items');
        if (cartList) {
            this.renderCartItems(c);
        }
    },

    // Sepet öğelerini render et (Layout minicart yapısı)
    renderCartItems(cart) {
        const cartList = document.querySelector('#cart-items');
        if (!cartList) return;

        const items = cart?.Items ?? cart?.items ?? [];

        if (!items.length) {
            cartList.innerHTML = '<div class="minicart__empty text-center py-4" data-cart-empty><p class="mb-0">Sepetiniz boş</p></div>';
            return;
        }

        let html = '';
        items.forEach(item => {
            const productId = item.productId ?? item.ProductId;
            const productName = (item.productName ?? item.ProductName ?? '').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            const unitPrice = item.unitPrice ?? item.UnitPrice ?? 0;
            const quantity = item.quantity ?? item.Quantity ?? 1;
            const imagePath = item.imagePath ?? item.ImagePath ?? '';
            const stockQuantity = item.stockQuantity ?? item.StockQuantity ?? 999;
            // formatPrice "₺1.234,56" veya "123,45 ₺" döner; template'te ₺ zaten var
            const priceStr = this.formatPrice(unitPrice).replace(/^₺| ₺$/g, '').trim();

            html += `
                <div class="minicart__product--items d-flex cart-item" data-product-id="${productId}">
                    <div class="minicart__thumb">
                        <a href="/magaza/urun/${productId}"><img src="${imagePath || '/assets/img/placeholder.webp'}" alt="${productName}"></a>
                    </div>
                    <div class="minicart__text">
                        <h4 class="minicart__subtitle"><a href="/magaza/urun/${productId}">${productName}</a></h4>
                        <div class="minicart__price">
                            <span class="minicart__current--price">₺${priceStr}</span>
                        </div>
                        <div class="minicart__text--footer d-flex align-items-center">
                            <div class="quantity__box minicart__quantity">
                                <button type="button" class="quantity__value decrease" aria-label="azalt" onclick="CartManager.decreaseQuantity(${productId})">-</button>
                                <label>
                                    <input type="number" class="quantity__number" value="${quantity}" min="1" max="${stockQuantity}" onchange="CartManager.updateCartItem(${productId}, this.value)" data-counter />
                                </label>
                                <button type="button" class="quantity__value increase" aria-label="artır" onclick="CartManager.increaseQuantity(${productId})">+</button>
                            </div>
                            <button class="minicart__product--remove" type="button" onclick="CartManager.removeFromCart(${productId})">KALDIR</button>
                        </div>
                    </div>
                </div>
            `;
        });

        cartList.innerHTML = html;
    },

    async increaseQuantity(productId) {
        const input = document.querySelector(`[data-product-id="${productId}"] input[type="number"]`);
        if (input) {
            const newQuantity = parseInt(input.value, 10) + 1;
            await this.updateCartItem(productId, newQuantity);
        }
    },

    async decreaseQuantity(productId) {
        const input = document.querySelector(`[data-product-id="${productId}"] input[type="number"]`);
        if (input) {
            const newQuantity = Math.max(1, parseInt(input.value, 10) - 1);
            await this.updateCartItem(productId, newQuantity);
        }
    },

    showNotification(message, type = 'info') {
        if (typeof toastr === 'undefined') return;
        if (type === 'success') toastr["success"](message);
        else if (type === 'error') toastr["error"](message);
        else if (type === 'warning') toastr["warning"](message);
        else toastr["info"](message);
    },

    async init() {
        const cart = await this.getCart();
        if (cart) {
            this.updateCartUI(cart);
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    CartManager.init();
});

// jQuery ile kullanım (jQuery yüklüyse)
if (typeof $ !== 'undefined') {
    $(document).ready(function () {
        $('.add-to-cart-btn').on('click', function () {
            const productId = $(this).data('product-id');
            const quantity = $(this).closest('.product-card').find('.quantity-input').val() || 1;
            CartManager.addToCart(productId, parseInt(quantity, 10));
        });

        $('.quick-add-btn').on('click', function () {
            const productId = $(this).data('product-id');
            CartManager.addToCart(productId, 1);
        });
    });
}
