// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const frame = document.getElementById('appPreviewFrame');
    const addressInput = document.getElementById('previewAddressInput');
    const reloadButton = document.getElementById('previewReloadButton');
    const goButton = document.getElementById('previewGoButton');

    if (!frame || !addressInput) {
        return;
    }

    function normalizePreviewUrl(value) {
        const trimmed = (value || '').trim();

        if (!trimmed) {
            return '/';
        }

        if (trimmed.startsWith('http://') || trimmed.startsWith('https://') || trimmed.startsWith('/')) {
            return trimmed;
        }

        return `/${trimmed}`;
    }

    function navigatePreview() {
        const nextUrl = normalizePreviewUrl(addressInput.value);
        addressInput.value = nextUrl;
        frame.src = nextUrl;
    }

    goButton?.addEventListener('click', navigatePreview);

    reloadButton?.addEventListener('click', function () {
        frame.src = frame.src;
    });

    addressInput.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            navigatePreview();
        }
    });
})();
