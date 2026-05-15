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
    let activeLanguage = 'typescript';

    function getBaseName(path) {
        const normalizedPath = (path || '').replace(/\\/g, '/');
        const segments = normalizedPath.split('/').filter(Boolean);
        return segments.length > 0 ? segments[segments.length - 1] : normalizedPath;
    }

    function buildWorkspaceFileTree() {
        const source = document.getElementById('workspaceFileTreeSource');
        const root = document.getElementById('workspaceFileTreeRoot');

        if (!source || !root) {
            return;
        }

        const sourceButtons = Array.from(source.querySelectorAll('[data-file-button]'));
        if (sourceButtons.length === 0) {
            return;
        }

        const selectedButton = source.querySelector('[data-file-button].active');
        const selectedPath = selectedButton?.dataset.filePath || '';
        const tree = {
            path: '',
            name: '',
            directories: new Map(),
            files: []
        };

        sourceButtons.forEach(function (button) {
            const filePath = (button.dataset.filePath || '').replace(/\\/g, '/');
            const parts = filePath.split('/').filter(Boolean);

            if (parts.length === 0) {
                tree.files.push(button);
                return;
            }

            let currentNode = tree;
            let currentPath = '';

            parts.slice(0, -1).forEach(function (segment) {
                currentPath = currentPath ? `${currentPath}/${segment}` : segment;

                if (!currentNode.directories.has(segment)) {
                    currentNode.directories.set(segment, {
                        path: currentPath,
                        name: segment,
                        directories: new Map(),
                        files: []
                    });
                }

                currentNode = currentNode.directories.get(segment);
            });

            currentNode.files.push(button);
        });

        function renderFiles(container, files) {
            files.forEach(function (button) {
                const filePath = button.dataset.filePath || '';
                button.textContent = getBaseName(filePath);
                button.title = filePath;
                container.appendChild(button);
            });
        }

        function renderDirectory(node) {
            const details = document.createElement('details');
            details.className = 'workspace-dir-node';
            details.open = selectedPath.startsWith(`${node.path}/`);

            const summary = document.createElement('summary');
            summary.className = 'workspace-dir-summary';
            summary.setAttribute('role', 'treeitem');
            summary.innerHTML = `
                <span class="workspace-dir-chevron" aria-hidden="true"></span>
                <span class="workspace-dir-label">${node.name}</span>
            `;

            const children = document.createElement('div');
            children.className = 'workspace-dir-children';

            Array.from(node.directories.values()).forEach(function (childDirectory) {
                children.appendChild(renderDirectory(childDirectory));
            });

            renderFiles(children, node.files);

            details.appendChild(summary);
            details.appendChild(children);
            return details;
        }

        root.innerHTML = '';

        Array.from(tree.directories.values()).forEach(function (directory) {
            root.appendChild(renderDirectory(directory));
        });

        renderFiles(root, tree.files);
    }

    function setEditorLanguage(language) {
        activeLanguage = language || 'plaintext';

        if (monacoEditor && window.monaco?.editor) {
            window.monaco.editor.setModelLanguage(monacoEditor.getModel(), activeLanguage);
        }
    }

    function setEditorValue(value) {
        const editorContentField = document.getElementById('workspaceEditorContent');

        if (editorContentField) {
            editorContentField.value = value || '';
        }

        if (monacoEditor) {
            monacoEditor.setValue(value || '');
            monacoEditor.focus();
        }
    }

    function selectWorkspaceFile(button) {
        if (!button) {
            return;
        }

        const fileId = button.dataset.fileId;
        const filePath = button.dataset.filePath || 'No file selected';
        const fileLanguage = button.dataset.fileLanguage || 'plaintext';
        const fileSeed = fileId ? document.getElementById(`workspaceFileSeed_${fileId}`) : null;
        const fileInput = document.querySelector('input[name="SaveInput.FileId"]');
        const fileLabel = document.getElementById('workspaceActiveFileLabel');
        const saveButton = document.querySelector('.workspace-editor-actions button[type="submit"]');
        const nextValue = fileSeed ? fileSeed.value : '';

        document.querySelectorAll('[data-file-button]').forEach(function (item) {
            const isActive = item === button;
            item.classList.toggle('active', isActive);
            item.setAttribute('aria-pressed', isActive ? 'true' : 'false');
        });

        if (fileInput) {
            fileInput.value = fileId || '';
        }

        if (fileLabel) {
            fileLabel.textContent = filePath;
        }

        if (saveButton) {
            saveButton.disabled = !fileId;
        }

        let parentDetails = button.closest('details');
        while (parentDetails) {
            parentDetails.open = true;
            parentDetails = parentDetails.parentElement?.closest('details');
        }

        setEditorLanguage(fileLanguage);
        setEditorValue(nextValue);
    }

    window.initializeWorkspaceMonaco = function () {
        const editorElement = document.getElementById('workspaceMonacoEditor');
        const seedElement = document.getElementById('workspaceMonacoSeed');
        const editorContentField = document.getElementById('workspaceEditorContent');

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
                value: editorContentField?.value || seedElement.value,
                language: editorElement.dataset.language || activeLanguage,
                theme: 'vs-dark',
                automaticLayout: true,
                minimap: { enabled: false },
                fontSize: 14,
                scrollBeyondLastLine: false,
                readOnly: false
            });

            monacoEditor.onDidChangeModelContent(function () {
                if (editorContentField) {
                    editorContentField.value = monacoEditor.getValue();
                }
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

    buildWorkspaceFileTree();

    const fileButtons = document.querySelectorAll('[data-file-button]');
    const initiallySelectedFile = document.querySelector('[data-file-button].active') || fileButtons[0];

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

    fileButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            selectWorkspaceFile(button);
        });
    });

    if (initiallySelectedFile) {
        selectWorkspaceFile(initiallySelectedFile);
    }

    tabButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            activateTab(button.dataset.workspaceTab);
        });
    });

    activateTab(window.workspaceInitialTab || 'preview');
})();
