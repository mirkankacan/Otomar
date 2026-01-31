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

    // Sepeti temizle - DELETE /sepet/temizle (SweetAlert2 ile onay)
    async clearCart() {
        const confirmed = typeof Swal !== 'undefined'
            ? (await Swal.fire({
                title: 'Sepeti temizle',
                text: 'Sepeti tamamen temizlemek istediğinizden emin misiniz?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Evet, temizle',
                cancelButtonText: 'İptal'
            })).isConfirmed
            : confirm('Sepeti tamamen temizlemek istediğinizden emin misiniz?');
        if (!confirmed) return;
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
        if (!value && value !== 0) return '₺0,00';

        // Sayıyı string'e çevir ve ondalık kısmı ayır
        const numStr = value.toFixed(2);
        const parts = numStr.split('.');
        const integerPart = parts[0];
        const decimalPart = parts[1] || '00';

        // Binlik ayırıcı ekle (nokta)
        let formattedInteger = '';
        for (let i = integerPart.length - 1, j = 0; i >= 0; i--, j++)
        {
            if (j > 0 && j % 3 === 0)
            {
                formattedInteger = '.' + formattedInteger;
            }
            formattedInteger = integerPart[i] + formattedInteger;
        }

        // Türk Lirası formatı: ₺1.234,56
        return `₺${formattedInteger},${decimalPart}`;
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

        // Sepet sayfası (/sepet) tablo ve özet
        const cartPageTbody = document.getElementById('cart-page-tbody');
        if (cartPageTbody) {
            this.renderCartPage(c);
        }
        const pageSub = document.getElementById('cart-page-subtotal');
        const pageShip = document.getElementById('cart-page-shipping');
        const pageTotal = document.getElementById('cart-page-total');
        if (pageSub) pageSub.textContent = this.formatPrice(subTotal);
        if (pageShip) pageShip.textContent = this.formatPrice(shipping);
        if (pageTotal) pageTotal.textContent = this.formatPrice(total);

        // Ödeme sayfası (/odeme) sipariş özeti sidebar
        const paymentTbody = document.getElementById('payment-order-tbody');
        if (paymentTbody) {
            this.renderPaymentOrderSummary(c);
        }
        const paySub = document.getElementById('payment-order-subtotal');
        const payShip = document.getElementById('payment-order-shipping');
        const payTotal = document.getElementById('payment-order-total');
        if (paySub) paySub.textContent = this.formatPrice(subTotal);
        if (payShip) payShip.textContent = this.formatPrice(shipping);
        if (payTotal) payTotal.textContent = this.formatPrice(total);
    },

    // Ödeme sayfası sipariş özeti sidebar (read-only liste + tutarlar)
    renderPaymentOrderSummary(cart) {
        const tbody = document.getElementById('payment-order-tbody');
        if (!tbody) return;
        const items = cart?.Items ?? cart?.items ?? [];
        const emptyRow = document.getElementById('payment-order-empty-row');

        if (!items.length) {
            if (emptyRow) {
                emptyRow.style.display = '';
                emptyRow.innerHTML = '<td colspan="2" class="text-center py-4 text-muted">Sepetiniz boş. <a href="/magaza">Alışverişe devam et</a></td>';
            }
            tbody.querySelectorAll('tr.payment-order-item-row').forEach(function (r) { r.remove(); });
            return;
        }
        if (emptyRow) emptyRow.style.display = 'none';
        tbody.querySelectorAll('tr.payment-order-item-row').forEach(function (r) { r.remove(); });

        items.forEach(item => {
            const productId = item.productId ?? item.ProductId;
            const productName = (item.productName ?? item.ProductName ?? '').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            const productCode = item.productCode ?? item.ProductCode ?? '';
            const unitPrice = item.unitPrice ?? item.UnitPrice ?? 0;
            const quantity = item.quantity ?? item.Quantity ?? 1;
            const imagePath = item.imagePath ?? item.ImagePath ?? '';
            const lineTotal = unitPrice * quantity;
            const slug = generateSlug(productName)
            const lineTotalStr = this.formatPrice(lineTotal).replace(/^₺| ₺$/g, '').trim();

            const tr = document.createElement('tr');
            tr.className = 'cart__table--body__items payment-order-item-row';
            tr.setAttribute('data-product-id', productId);
            tr.innerHTML = `
                <td class="cart__table--body__list">
                    <div class="product__image two d-flex align-items-center">
                        <div class="product__thumbnail border-radius-5">
                            <a class="display-block" href="/urun/${slug}/${productCode}"><img class="display-block border-radius-5" src="${imagePath || '/assets/img/placeholder.webp'}" alt="${productName}"></a>
                            <span class="product__thumbnail--quantity">${quantity}</span>
                        </div>
                        <div class="product__description">
                            <h4 class="product__description--name"><a href="/urun/${slug}/${productCode}">${productName}</a></h4>
                            <span class="product__description--variant">Stok Kodu: ${productCode}</span>
                        </div>
                    </div>
                </td>
                <td class="cart__table--body__list">
                    <span class="cart__price">₺${lineTotalStr}</span>
                </td>
            `;
            tbody.appendChild(tr);
        });
    },

    // Sepet sayfası tablosunu doldur (Cart/Index)
    renderCartPage(cart) {
        const tbody = document.getElementById('cart-page-tbody');
        if (!tbody) return;
        const items = cart?.Items ?? cart?.items ?? [];
        const emptyRow = document.getElementById('cart-page-empty-row');

        if (!items.length) {
            if (emptyRow) emptyRow.style.display = '';
            tbody.querySelectorAll('tr.cart-page-item-row').forEach(function (r) { r.remove(); });
            return;
        }
        if (emptyRow) emptyRow.style.display = 'none';

        // Eski ürün satırlarını kaldır (boş satır hariç)
        tbody.querySelectorAll('tr.cart-page-item-row').forEach(function (r) { r.remove(); });

        items.forEach(item => {
            const productId = item.productId ?? item.ProductId;
            const productName = (item.productName ?? item.ProductName ?? '').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            const productCode = item.productCode ?? item.ProductCode ?? '';
            const unitPrice = item.unitPrice ?? item.UnitPrice ?? 0;
            const quantity = item.quantity ?? item.Quantity ?? 1;
            const imagePath = item.imagePath ?? item.ImagePath ?? '';
            const stockQuantity = item.stockQuantity ?? item.StockQuantity ?? 999;
            const lineTotal = unitPrice * quantity;
            const priceStr = this.formatPrice(unitPrice).replace(/^₺| ₺$/g, '').trim();
            const lineTotalStr = this.formatPrice(lineTotal).replace(/^₺| ₺$/g, '').trim();
            const slug = generateSlug(productName)

            const tr = document.createElement('tr');
            tr.className = 'cart__table--body__items cart-page-item-row';
            tr.setAttribute('data-product-id', productId);
            tr.innerHTML = `
                <td class="cart__table--body__list">
                    <div class="cart__product d-flex align-items-center">
                        <button class="cart__remove--btn" type="button" aria-label="Ürünü kaldır" onclick="CartManager.removeFromCart(${productId})">
                            <svg fill="currentColor" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16"><path d="M 4.7070312 3.2929688 L 3.2929688 4.7070312 L 10.585938 12 L 3.2929688 19.292969 L 4.7070312 20.707031 L 12 13.414062 L 19.292969 20.707031 L 20.707031 19.292969 L 13.414062 12 L 20.707031 4.7070312 L 19.292969 3.2929688 L 12 10.585938 L 4.7070312 3.2929688 z" /></svg>
                        </button>
                        <div class="cart__thumbnail">
                            <a href="/urun/${slug}/${productCode}"><img class="border-radius-5" src="${imagePath || '/assets/img/placeholder.webp'}" alt="${productName}"></a>
                        </div>
                        <div class="cart__content">
                            <h3 class="cart__content--title h4"><a href="/urun/${slug}/${productCode}">${productName}</a></h3>
                            <span class="cart__content--variant">Stok Kodu: ${productCode}</span>
                        </div>
                    </div>
                </td>
                <td class="cart__table--body__list"><span class="cart__price">₺${priceStr}</span></td>
                <td class="cart__table--body__list">
                    <div class="quantity__box">
                        <button type="button" class="quantity__value decrease" aria-label="Azalt" onclick="CartManager.decreaseQuantity(${productId})">-</button>
                        <label><input type="number" class="quantity__number" value="${quantity}" min="1" max="${stockQuantity}" onchange="CartManager.updateCartItem(${productId}, this.value)" data-counter /></label>
                        <button type="button" class="quantity__value increase" aria-label="Artır" onclick="CartManager.increaseQuantity(${productId})">+</button>
                    </div>
                </td>
                <td class="cart__table--body__list"><span class="cart__price end">₺${lineTotalStr}</span></td>
            `;
            tbody.appendChild(tr);
        });
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
            const priceStr = this.formatPrice(unitPrice).replace(/^₺| ₺$/g, '').trim();
            const slug = generateSlug(productName);
            const productCode = item.productCode ?? item.ProductCode ?? '';

            html += `
                <div class="minicart__product--items d-flex cart-item" data-product-id="${productId}">
                    <div class="minicart__thumb">
                        <a href="/urun/${slug}/${productCode}"><img src="${imagePath || '/assets/img/placeholder.webp'}" alt="${productName}"></a>
                    </div>
                    <div class="minicart__text">
                        <h4 class="minicart__subtitle"><a href="/urun/${slug}/${productCode}">${productName}</a></h4>
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
