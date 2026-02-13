/**
 * reCAPTCHA v3 Helper
 */

async function getRecaptchaToken(action) {
    if (typeof window.RECAPTCHA_ENABLED !== 'undefined' && !window.RECAPTCHA_ENABLED) {
        return '';
    }

    if (typeof grecaptcha === 'undefined' || typeof grecaptcha.execute !== 'function') {
        console.warn('reCAPTCHA yüklenmedi');
        return '';
    }

    try {
        return await grecaptcha.execute(window.SITE_KEY, { action: action });
    } catch (error) {
        console.error('reCAPTCHA token alınamadı:', error);
        return '';
    }
}

async function makeRequestWithRecaptcha(url, method, data, recaptchaAction) {
    if (recaptchaAction) {
        var token = await getRecaptchaToken(recaptchaAction);
        if (token) {
            if (data instanceof FormData) {
                data.append('recaptchaToken', token);
            } else if (typeof data === 'object' && data !== null) {
                data.recaptchaToken = token;
            } else {
                data = { recaptchaToken: token };
            }
        }
    }
    return await makeRequest(url, method, data);
}
