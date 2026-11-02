// Live updates about transport requests: a notification, and a refresh of any table on the page.
window.TrackWeatherLive = (function () {
    'use strict';

    const toasts = document.getElementById('live-toasts');
    const strings = JSON.parse(document.getElementById('live-strings').textContent);
    const isAdmin = toasts.dataset.isAdmin === 'true';
    const statusNames = {
        New: strings.StatusNew,
        InProgress: strings.StatusInProgress,
        Completed: strings.StatusCompleted,
        Cancelled: strings.StatusCancelled
    };

    function toast(message) {
        const element = document.createElement('div');
        element.className = 'toast';
        element.setAttribute('role', 'status');
        const body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = message;
        element.append(body);
        toasts.append(element);
        element.addEventListener('hidden.bs.toast', () => element.remove());
        new bootstrap.Toast(element, { delay: 6000 }).show();
    }

    async function refreshTable(container) {
        // Do not pull a dropdown out from under someone who is using it
        if (container.contains(document.activeElement) && document.activeElement.tagName === 'SELECT') {
            container.addEventListener('focusout', () => refreshTable(container), { once: true });
            return;
        }
        const response = await fetch(container.dataset.liveTable);
        if (response.ok) {
            container.innerHTML = await response.text();
            TrackWeatherSite.localizeTimes(container);
        }
    }

    function describe(change) {
        const number = `№ ${change.id}`;
        switch (change.kind) {
            case 'Created': return isAdmin ? `${strings.NewRequest} ${number}` : null;
            case 'Deleted': return `${strings.RequestDeleted} ${number}`;
            default: return `${strings.Request} ${number}: ${statusNames[change.status]}`;
        }
    }

    function onChanged(change) {
        const message = describe(change);
        if (message) {
            toast(message);
        }
        document.querySelectorAll('[data-live-table]').forEach(refreshTable);
        document.dispatchEvent(new CustomEvent('transport:changed', { detail: change }));
    }

    // The CDN script may be blocked; the site keeps working, just without live updates
    if (window.signalR) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/transport')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();
        connection.on('requestChanged', onChanged);
        connection.start().catch(error => console.warn('Live updates are unavailable', error));
    }

    return { toast };
})();
