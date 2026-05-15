// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const frame = document.getElementById('appPreviewFrame');
    const addressInput = document.getElementById('previewAddressInput');
    const reloadButton = document.getElementById('previewReloadButton');
    const goButton = document.getElementById('previewGoButton');

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
        if (!frame || !addressInput) {
            return;
        }

        const nextUrl = normalizePreviewUrl(addressInput.value);
        addressInput.value = nextUrl;
        frame.src = nextUrl;
    }

    goButton?.addEventListener('click', navigatePreview);

    reloadButton?.addEventListener('click', function () {
        if (frame) {
            frame.src = frame.src;
        }
    });

    addressInput?.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            navigatePreview();
        }
    });
})();

(function () {
    let monacoEditor;
    let monacoLoadStarted = false;

    window.initializeWorkspaceMonaco = function () {
        const editorElement = document.getElementById('workspaceMonacoEditor');
        const seedElement = document.getElementById('workspaceMonacoSeed');

        if (!editorElement || !seedElement || monacoEditor || monacoLoadStarted) {
            if (monacoEditor) {
                window.setTimeout(function () {
                    monacoEditor.layout();
                }, 50);
            }
            return;
        }

        if (!window.require) {
            return;
        }

        monacoLoadStarted = true;
        window.require.config({ paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' } });
        window.require(['vs/editor/editor.main'], function () {
            monacoEditor = window.monaco.editor.create(editorElement, {
                value: seedElement.value,
                language: editorElement.dataset.language || 'typescript',
                theme: 'vs-dark',
                automaticLayout: true,
                minimap: { enabled: false },
                fontSize: 14,
                scrollBeyondLastLine: false,
                readOnly: false
            });

            window.setTimeout(function () {
                monacoEditor.layout();
            }, 50);
        });
    };

    const tabButtons = document.querySelectorAll('[data-workspace-tab]');
    const tabPanes = document.querySelectorAll('[data-workspace-pane]');

    if (tabButtons.length === 0 || tabPanes.length === 0) {
        return;
    }

    function activateTab(name) {
        tabButtons.forEach(function (button) {
            const isActive = button.dataset.workspaceTab === name;
            button.classList.toggle('active', isActive);
            button.setAttribute('aria-selected', isActive ? 'true' : 'false');
        });

        tabPanes.forEach(function (pane) {
            const isActive = pane.dataset.workspacePane === name;
            pane.classList.toggle('active', isActive);
            pane.hidden = !isActive;
        });

        if (name === 'code') {
            window.setTimeout(function () {
                window.initializeWorkspaceMonaco?.();
            }, 0);
        }
    }

    tabButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            activateTab(button.dataset.workspaceTab);
        });
    });

    activateTab('preview');
})();
