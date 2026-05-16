// Existing workspace code omitted for brevity...

(function () {
    // Existing workspace initialization code...

    // --- New SignalR integration for workspace logs ---
    const projectIdElement = document.getElementById('selectedProjectId');
    const projectId = projectIdElement?.value;

    if (projectId && window.signalR) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/workspace-log-hub')
            .withAutomaticReconnect()
            .build();

        connection.on('workspaceLog', function (payload) {
            if (!payload || payload.projectId !== projectId) return;

            const container = document.querySelector('.workspace-log-lines');
            if (!container) return;

            const line = document.createElement('div');
            line.innerHTML = `<span>[${payload.level}]</span> ${payload.message}`;
            container.appendChild(line);
            container.scrollTop = container.scrollHeight;
        });

        connection.start().catch(err => console.error(err));
    }
})();
