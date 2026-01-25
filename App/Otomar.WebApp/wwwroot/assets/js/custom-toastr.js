function showToast(message, type = 'success', title = 'Bildirim', time = 'şimdi')
{
    const typeConfig = {
        success: { bgClass: 'bg-success', defaultTitle: 'Başarılı' },
        error: { bgClass: 'bg-danger', defaultTitle: 'Hata' },
        warning: { bgClass: 'bg-warning', defaultTitle: 'Uyarı' },
        info: { bgClass: 'bg-info', defaultTitle: 'Bilgi' }
    };

    const config = typeConfig[type] || typeConfig.success;
    const finalTitle = title || config.defaultTitle;

    const toastHtml = `
        <div class="toast fade show border-0 bg-white" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header ${config.bgClass} text-white">
                <img src="/theme/assets/images/toastrlogo.jpg" class="m-r-sm" alt="Kuşgöz İzmir Vinç Logo" height="18" width="18">
                <strong class="me-auto">${finalTitle}</strong>
                <small class="text-white">${time}</small>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;

    let container = document.querySelector('.toast-container');
    if (!container)
    {
        container = document.createElement('div');
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999'; // Container'a z-index
        document.body.appendChild(container);
    }

    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastElement = container.lastElementChild;

    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 5000
    });

    toast.show();

    toastElement.addEventListener('hidden.bs.toast', () =>
    {
        toastElement.remove();
    });
}

function extractErrorInfo(error)
{
    console.log('Error geldi:', error);
    console.log('Error type:', typeof error);

    // String ise
    if (typeof error === 'string')
    {
        try
        {
            const parsed = JSON.parse(error);
            console.log('Parsed error:', parsed);
            return {
                title: parsed.title || 'Hata',
                detail: parsed.detail || error
            };
        } catch (e)
        {
            return { title: 'Hata', detail: error };
        }
    }

    // Object ise
    if (typeof error === 'object' && error !== null)
    {
        console.log('Error properties:', Object.keys(error));

        // ResponseText veya data içinde olabilir
        if (error.responseText)
        {
            console.log('responseText bulundu:', error.responseText);
            return extractErrorInfo(error.responseText);
        }

        if (error.response)
        {
            console.log('response bulundu:', error.response);
            if (error.response.data)
            {
                console.log('response.data bulundu:', error.response.data);
                return extractErrorInfo(error.response.data);
            }
            return extractErrorInfo(error.response);
        }

        // Direkt property'leri kontrol et
        const title = error.title || error.ErrorTitle || 'Hata';
        const detail = error.detail || error.ErrorDetail || error.message || error.ErrorMessage || JSON.stringify(error);

        return { title, detail };
    }

    return { title: 'Hata', detail: 'Bir hata oluştu' };
}

function handleError(error, defaultMessage = 'Bir hata oluştu')
{
    if (!error)
    {
        showToast(defaultMessage, 'error', 'Hata');
        return;
    }

    const errorInfo = extractErrorInfo(error);

    showToast(
        errorInfo.detail || defaultMessage,
        'error',
        errorInfo.title
    );
}

// Toastr compatibility
const toastr = {
    success: (message, title) => showToast(message, 'success', title),
    error: (message, title) => handleError(message, title || 'Bir hata oluştu'),
    warning: (message, title) => showToast(message, 'warning', title),
    info: (message, title) => showToast(message, 'info', title)
};