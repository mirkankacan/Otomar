/**
 * Global Form Validation Utilities
 * Tüm sayfalarda kullanılabilecek validasyon fonksiyonları
 */

const Validator = {
    /**
     * Email formatını kontrol eder
     * @param {string} email - Kontrol edilecek email
     * @returns {boolean} - Geçerli ise true
     */
    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    },
    /**
 * Vergi numarası validasyonu (Türkiye)
 * @param {string} taxNumber - Kontrol edilecek vergi numarası
 * @returns {boolean} - Geçerli ise true
 */
    isValidTaxNumber(taxNumber)
    {
        if (!taxNumber || taxNumber.length !== 10 || !this.isNumeric(taxNumber))
        {
            return false;
        }

        return true;
    },

    /**
     * Kredi kartı numarası Luhn algoritması ile validasyon
     * @param {string} cardNumber - Kontrol edilecek kart numarası
     * @returns {boolean} - Geçerli ise true
     */
    isValidCreditCard(cardNumber)
    {
        const cleanNumber = cardNumber.replace(/\s/g, '');

        if (!this.isNumeric(cleanNumber) || cleanNumber.length < 13 || cleanNumber.length > 19)
        {
            return false;
        }

        // Luhn algoritması
        let sum = 0;
        let isEven = false;

        for (let i = cleanNumber.length - 1; i >= 0; i--)
        {
            let digit = parseInt(cleanNumber[i]);

            if (isEven)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            isEven = !isEven;
        }

        return sum % 10 === 0;
    },

    /**
     * Kredi kartı son kullanma tarihi validasyonu
     * @param {string} expiry - MM/YY formatında tarih
     * @returns {boolean} - Geçerli ise true
     */
    isValidCardExpiry(expiry)
    {
        if (!expiry || expiry.length !== 5)
        {
            return false;
        }

        const [month, year] = expiry.split('/').map(Number);

        if (month < 1 || month > 12)
        {
            return false;
        }

        const currentDate = new Date();
        const currentYear = currentDate.getFullYear() % 100; // Son 2 hane
        const currentMonth = currentDate.getMonth() + 1;

        if (year < currentYear || (year === currentYear && month < currentMonth))
        {
            return false;
        }

        return true;
    },

    /**
     * CVV validasyonu
     * @param {string} cvv - Kontrol edilecek CVV
     * @returns {boolean} - Geçerli ise true
     */
    isValidCVV(cvv)
    {
        return this.isNumeric(cvv) && (cvv.length === 3 || cvv.length === 4);
    },
    /**
     * Türkiye telefon numarası formatını kontrol eder
     * @param {string} phone - Kontrol edilecek telefon numarası
     * @returns {boolean} - Geçerli ise true
     */
    isValidPhone(phone) {
        // Türkiye telefon formatları: +90 5XX XXX XX XX, 05XX XXX XX XX, 5XX XXX XX XX
        const phoneRegex = /^(\+90|0)?5\d{9}$/;
        const cleanPhone = phone.replace(/[\s()-]/g, ''); // Boşluk, parantez ve tire temizle
        return phoneRegex.test(cleanPhone);
    },

    /**
     * String'in boş olup olmadığını kontrol eder
     * @param {string} value - Kontrol edilecek değer
     * @returns {boolean} - Boş ise true
     */
    isEmpty(value) {
        return !value || value.trim().length === 0;
    },

    /**
     * String uzunluğunu kontrol eder
     * @param {string} value - Kontrol edilecek değer
     * @param {number} min - Minimum uzunluk
     * @param {number} max - Maximum uzunluk
     * @returns {boolean} - Geçerli ise true
     */
    isValidLength(value, min = 0, max = Infinity) {
        const length = value ? value.trim().length : 0;
        return length >= min && length <= max;
    },

    /**
     * Sadece sayı karakteri içerip içermediğini kontrol eder
     * @param {string} value - Kontrol edilecek değer
     * @returns {boolean} - Sadece sayı ise true
     */
    isNumeric(value) {
        return /^\d+$/.test(value);
    },

    /**
     * Türkiye telefon numarası validasyonu (+90 5xx... veya 10 hane 5xx...; 90 sonrası 0 basılamaz)
     * @param {string} phone - Kontrol edilecek telefon (+90, boşluk/tire ile gelebilir)
     * @returns {boolean} - Geçerli ise true
     */
    isValidPhone(phone) {
        const digits = (phone || '').replace(/\D/g, '');
        // +90 5xx xxx xx xx → 12 hane (90 + 10), 3. hane 0 olamaz
        if (digits.length === 12 && digits.startsWith('90') && digits[2] !== '0') return true;
        // 10 hane 5xx... (0 ile başlamaz)
        if (digits.length === 10 && digits[0] !== '0') return true;
        // 11 hane 05xx...
        if (digits.length === 11 && digits[0] === '0' && digits[1] !== '0') return true;
        return false;
    },

    /**
     * TC Kimlik No validasyonu
     * @param {string} tcNo - Kontrol edilecek TC Kimlik No
     * @returns {boolean} - Geçerli ise true
     */
    /**
  * TC Kimlik No validasyonu (Doğru Algoritma)
  * @param {string} tcNo - Kontrol edilecek TC Kimlik No
  * @returns {boolean} - Geçerli ise true
  */
    isValidTCKN(tcNo)
    {
        if (!tcNo || tcNo.length !== 11 || !this.isNumeric(tcNo) || tcNo[0] === '0')
        {
            return false;
        }

        const digits = tcNo.split('').map(Number);

        // 1. Kural: 1, 3, 5, 7, 9. hanelerin toplamının 7 katından, 
        // 2, 4, 6, 8. hanelerin toplamının 9 katını çıkartıp 10'a böldüğümüzde 
        // kalan 10. haneyi vermelidir.
        const oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        const evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        const check10 = (oddSum * 7 - evenSum) % 10;

        if (check10 < 0)
        {
            // Negatif değer kontrolü
            if ((check10 + 10) !== digits[9])
            {
                return false;
            }
        } else
        {
            if (check10 !== digits[9])
            {
                return false;
            }
        }

        // 2. Kural: İlk 10 hanenin toplamının 10'a bölümünden kalan, 
        // 11. haneyi vermelidir.
        const sum10 = digits.slice(0, 10).reduce((a, b) => a + b, 0);
        if (sum10 % 10 !== digits[10])
        {
            return false;
        }

        return true;
    },

    /**
     * URL formatını kontrol eder
     * @param {string} url - Kontrol edilecek URL
     * @returns {boolean} - Geçerli ise true
     */
    isValidUrl(url) {
        try {
            new URL(url);
            return true;
        } catch {
            return false;
        }
    },

    /**
     * Güçlü şifre kontrolü
     * En az 8 karakter, 1 büyük harf, 1 küçük harf, 1 rakam
     * @param {string} password - Kontrol edilecek şifre
     * @returns {boolean} - Geçerli ise true
     */
    isStrongPassword(password) {
        const minLength = 8;
        const hasUpperCase = /[A-Z]/.test(password);
        const hasLowerCase = /[a-z]/.test(password);
        const hasNumber = /\d/.test(password);
        
        return password.length >= minLength && hasUpperCase && hasLowerCase && hasNumber;
    },

    /**
     * İki şifrenin eşleşip eşleşmediğini kontrol eder
     * @param {string} password - İlk şifre
     * @param {string} confirmPassword - Onay şifresi
     * @returns {boolean} - Eşleşiyor ise true
     */
    passwordsMatch(password, confirmPassword) {
        return password === confirmPassword && !this.isEmpty(password);
    },

    /**
     * Form elementini validate eder ve hata mesajı gösterir
     * @param {jQuery|HTMLElement} element - Validate edilecek element
     * @param {string} message - Hata mesajı
     * @returns {boolean} - Geçerli ise true
     */
    showError(element, message) {
        const $element = $(element);
        const $parent = $element.closest('.contact__form--list, .checkout__content--step__inner, .form-group');
        
        // Önceki hata mesajını kaldır
        $parent.find('.validation-error').remove();
        
        // Hata mesajı ekle
        if (message) {
            $element.addClass('is-invalid');
            const errorDiv = $('<div class="validation-error text-danger mt-1" style="font-size: 1.2rem;"></div>').text(message);
            $element.after(errorDiv);
        }
        
        return false;
    },

    /**
     * Form elementindeki hata mesajını temizler
     * @param {jQuery|HTMLElement} element - Temizlenecek element
     */
    clearError(element) {
        const $element = $(element);
        const $parent = $element.closest('.contact__form--list, .checkout__content--step__inner, .form-group');
        
        $element.removeClass('is-invalid');
        $parent.find('.validation-error').remove();
    },

    /**
     * Form'daki tüm hataları temizler
     * @param {jQuery|HTMLElement} form - Temizlenecek form
     */
    clearAllErrors(form) {
        const $form = $(form);
        $form.find('.is-invalid').removeClass('is-invalid');
        $form.find('.validation-error').remove();
    },

    /**
     * Input elementini gerçek zamanlı olarak validate eder
     * @param {jQuery|HTMLElement} input - Validate edilecek input
     * @param {Function} validationFn - Validasyon fonksiyonu
     * @param {string} errorMessage - Hata mesajı
     */
    validateOnInput(input, validationFn, errorMessage) {
        const $input = $(input);
        
        $input.on('blur', () => {
            const value = $input.val().trim();
            if (!this.isEmpty(value) && !validationFn(value)) {
                this.showError($input, errorMessage);
            } else {
                this.clearError($input);
            }
        });

        $input.on('input', () => {
            if ($input.hasClass('is-invalid')) {
                this.clearError($input);
            }
        });
    },

    /**
     * Formu validate eder
     * @param {Object} formData - Form verileri
     * @param {Object} rules - Validasyon kuralları
     * @returns {Object} - { isValid: boolean, errors: Object }
     */
    validateForm(formData, rules) {
        const errors = {};
        let isValid = true;

        for (const [field, rule] of Object.entries(rules)) {
            const value = formData[field];

            // Required kontrolü
            if (rule.required && this.isEmpty(value)) {
                errors[field] = rule.requiredMessage || `${field} alanı zorunludur.`;
                isValid = false;
                continue;
            }

            // Değer varsa diğer kontrolleri yap
            if (!this.isEmpty(value)) {
                // Min length kontrolü
                if (rule.minLength && value.length < rule.minLength) {
                    errors[field] = rule.minLengthMessage || `${field} en az ${rule.minLength} karakter olmalıdır.`;
                    isValid = false;
                }

                // Max length kontrolü
                if (rule.maxLength && value.length > rule.maxLength) {
                    errors[field] = rule.maxLengthMessage || `${field} en fazla ${rule.maxLength} karakter olabilir.`;
                    isValid = false;
                }

                // Email kontrolü
                if (rule.email && !this.isValidEmail(value)) {
                    errors[field] = rule.emailMessage || 'Geçerli bir e-posta adresi giriniz.';
                    isValid = false;
                }

                // Phone kontrolü
                if (rule.phone && !this.isValidPhone(value)) {
                    errors[field] = rule.phoneMessage || 'Geçerli bir telefon numarası giriniz.';
                    isValid = false;
                }

                // Custom validasyon fonksiyonu
                if (rule.custom && !rule.custom(value)) {
                    errors[field] = rule.customMessage || 'Geçersiz değer.';
                    isValid = false;
                }
            }
        }

        return { isValid, errors };
    },

    /**
     * Form validasyon hatalarını gösterir
     * @param {Object} errors - Hata objeleri { fieldId: errorMessage }
     * @param {string} formSelector - Form selector (opsiyonel)
     */
    displayErrors(errors, formSelector = null) {
        // Önce tüm hataları temizle
        if (formSelector) {
            this.clearAllErrors(formSelector);
        }

        // Her hata için mesaj göster
        for (const [field, message] of Object.entries(errors)) {
            const $element = $(`#txt${field.charAt(0).toUpperCase() + field.slice(1)}`);
            if ($element.length) {
                this.showError($element, message);
            }
        }
    }
};

// Export (modern JS için)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = Validator;
}
