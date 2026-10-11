(function () {
    function csrfToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    async function request(url, options = {}) {
        const method = (options.method || 'GET').toUpperCase();
        const headers = new Headers(options.headers || {});
        if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
            headers.set('Content-Type', 'application/json');
        }
        if (!['GET', 'HEAD', 'OPTIONS'].includes(method) && !headers.has('Authorization')) {
            const token = csrfToken();
            if (token) headers.set('RequestVerificationToken', token);
        }
        const response = await fetch(url, {
            ...options,
            method,
            headers,
            credentials: 'same-origin',
            cache: options.cache || 'no-store'
        });
        const type = response.headers.get('content-type') || '';
        const payload = type.includes('application/json') ? await response.json() : await response.text();
        if (!response.ok) {
            const message = payload && typeof payload === 'object'
                ? payload.message || payload.detail || payload.title
                : payload;
            throw new Error(message || `Yêu cầu thất bại (${response.status}).`);
        }
        return payload;
    }

    window.CustomerApi = Object.freeze({
        get: (url) => request(url),
        post: (url, data) => request(url, { method: 'POST', body: data instanceof FormData ? data : JSON.stringify(data) }),
        put: (url, data) => request(url, { method: 'PUT', body: data instanceof FormData ? data : JSON.stringify(data) }),
        delete: (url) => request(url, { method: 'DELETE' }),
        request,
        escapeHtml: (value) => String(value ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]))
    });
})();
