// ProblemDetails: detail bazen iç içe JSON string döner (örn. 404); title ve detail'i düzgün çıkarır
function normalizeProblemDetails(errorData, fallbackStatus)
{
    let title = errorData.title || 'Hata';
    let detail = errorData.detail || errorData.message || `Bir hata oluştu. (HTTP ${fallbackStatus})`;

    const rawDetail = errorData.detail;
    if (typeof rawDetail === 'string' && rawDetail.trim().startsWith('{'))
    {
        try
        {
            const inner = JSON.parse(rawDetail);
            if (inner && (inner.title || inner.detail))
            {
                title = inner.title || title;
                detail = (typeof inner.detail === 'string' ? inner.detail : (inner.detail ? String(inner.detail) : detail));
            }
        }
        catch (_)
        {
            // Parse edilemezse dış title/detail kullanılır
        }
    }

    return { title, detail };
}

// HTTP isteklerini yöneten merkezi fonksiyon
async function makeRequest(url, method = 'GET', data = null)
{
    const options = {
        method: method,
        headers: {}
    };

    // FormData kontrolü
    const isFormData = data instanceof FormData;

    // Content-Type header'ı sadece FormData değilse ekle
    if (!isFormData)
    {
        options.headers['Content-Type'] = 'application/json';
    }

    // POST, PUT, PATCH, DELETE için CSRF token ekle
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase()))
    {
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (csrfToken)
        {
            // FormData için token'ı FormData'ya ekle
            if (isFormData)
            {
                // FormData'ya token eklenmemişse ekle
                if (!data.has('__RequestVerificationToken'))
                {
                    data.append('__RequestVerificationToken', csrfToken);
                }
            }
            else
            {
                options.headers['X-CSRF-TOKEN'] = csrfToken;
            }
        }
    }

    // Body ekle
    if (data && ['POST', 'PUT', 'PATCH'].includes(method.toUpperCase()))
    {
        if (isFormData)
        {
            options.body = data;
        }
        else
        {
            options.body = JSON.stringify(data);
        }
    }

    try
    {
        const response = await fetch(url, options);

        // 401 Unauthorized hatası için özel işlem
        if (response.status === 401)
        {
            let message = 'Oturum süresi doldu. Lütfen tekrar giriş yapın.';
            let redirectUrl = '/giris-yap';

            // Backend'den gelen mesajı kontrol et (ProblemDetails formatı, iç içe detail olabilir)
            try
            {
                const responseText = await response.text();
                if (responseText)
                {
                    const errorData = JSON.parse(responseText);
                    const normalized = normalizeProblemDetails(errorData, 401);
                    message = normalized.detail || normalized.title || message;
                    if (errorData.loginUrl)
                        redirectUrl = errorData.loginUrl;
                }
            }
            catch (parseError)
            {
                // JSON parse hatası - varsayılan mesaj kullan
            }

            // Direkt logout ve yönlendirme yap
            await handleUnauthorizedError(message, redirectUrl);

            // Bu noktaya gelmemeli ama yine de hata fırlat
            throw {
                title: 'Oturum Sonlandı',
                detail: message
            };
        }

        if (!response.ok)
        {
            let errorObj = {
                title: 'Hata',
                detail: `Bir hata oluştu. (HTTP ${response.status})`
            };

            try
            {
                const responseText = await response.text();
                const errorData = JSON.parse(responseText);
                errorObj = normalizeProblemDetails(errorData, response.status);
            } catch (parseError)
            {
                // JSON parse edilemezse varsayılan mesaj kullan
            }

            // Hata mesajını toastr ile göster
            showErrorToastr(errorObj);

            // Hatayı fırlat ki catch bloğu çalışsın
            throw errorObj;
        }

        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json'))
        {
            return await response.json();
        }
        return null;
    } catch (error)
    {
        // Eğer hata zaten işlenmediyse (title/detail yoksa), toastr göster
        if (!error.title || !error.detail)
        {
            const errorObj = {
                title: 'Bağlantı Hatası',
                detail: error.message || 'Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin.'
            };
            showErrorToastr(errorObj);
            throw errorObj;
        }
        // Hata zaten işlenmişse direkt fırlat
        throw error;
    }
}

// 401 hatası için logout ve yönlendirme fonksiyonu
async function handleUnauthorizedError(message, redirectUrl)
{
    // Toastr mesajı göster (non-blocking)
    if (typeof toastr !== 'undefined')
    {
        toastr.error(message, 'Oturum Sonlandı', {
            timeOut: 2000,
            closeButton: false,
            progressBar: true
        });
    }

    // Kısa bir gecikme sonrası logout yap (toastr görünsün diye)
    await new Promise(resolve => setTimeout(resolve, 500));

    // Logout işlemini gerçekleştir
    performLogout(redirectUrl);
}

// Logout işlemi ve yönlendirme (401 vb. için - makeRequest kullanmadan)
function performLogout(redirectUrl)
{
    // Session storage ve local storage'ı temizle
    if (typeof sessionStorage !== 'undefined')
    {
        sessionStorage.clear();
    }
    if (typeof localStorage !== 'undefined')
    {
        localStorage.clear();
    }

    // Logout endpoint'ine async istek gönder ama yönlendirmeyi engelleme
    fetch('/cikis-yap', {
        method: 'POST',
        keepalive: true // Sayfa kapansa bile isteği tamamla
    }).catch(() =>
    {
        // Hata olsa bile devam et
    });

    // Her durumda hemen login sayfasına yönlendir
    window.location.href = redirectUrl || '/giris-yap';
}

// Çıkış: makeRequest ile POST /cikis-yap (CSRF token dahil), sonra yönlendir
async function logout(redirectUrl)
{
    try
    {
        await makeRequest('/cikis-yap', 'POST');
    }
    catch (_)
    {
        // Hata olsa bile çıkış yap (örn. zaten oturum kapanmış)
    }
    if (typeof sessionStorage !== 'undefined') sessionStorage.clear();
    if (typeof localStorage !== 'undefined') localStorage.clear();
    window.location.href = redirectUrl || '/giris-yap';
}

// Toastr ile hata gösterme helper fonksiyonu
function showErrorToastr(error)
{
    if (typeof toastr !== 'undefined')
    {
        const title = error.title || 'Hata';
        const detail = error.detail || 'Bir hata oluştu.';

        toastr.error(detail, title, {
            timeOut: 5000,
            closeButton: true,
            progressBar: true
        });
    }
}