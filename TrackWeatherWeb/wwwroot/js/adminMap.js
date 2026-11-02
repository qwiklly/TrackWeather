TrackWeatherMap.ready(async function () {
    'use strict';

    const { t, escapeHtml, formatCoords, showStatus, language } = TrackWeatherMap;

    const STATUS_NAMES = {
        New: t('StatusNew'),
        InProgress: t('StatusInProgress'),
        Completed: t('StatusCompleted'),
        Cancelled: t('StatusCancelled')
    };
    const STATUS_ICONS = { New: 'islands#redAutoIcon', InProgress: 'islands#orangeAutoIcon' };

    const map = new ymaps.Map('map', {
        center: [55.7558, 37.6173],
        zoom: 7,
        controls: ['zoomControl', 'typeSelector', 'routePanelControl']
    });

    // The M4 highway, where the utility services work
    map.controls.get('routePanelControl').routePanel.state.set({
        type: 'auto',
        fromEnabled: true,
        from: [44.50497296854422, 38.90903090603487],
        toEnabled: true,
        to: [55.57697610501588, 37.68869270398412],
    });

    const markers = new ymaps.GeoObjectCollection();
    map.geoObjects.add(markers);

    function row(label, value) {
        return `<div class="map-balloon__row"><strong>${escapeHtml(label)}:</strong> ${escapeHtml(value)}</div>`;
    }

    function addRequest(request) {
        const coords = [request.latitude, request.longitude];
        markers.add(new ymaps.Placemark(coords, {
            hintContent: escapeHtml(request.comment),
            balloonContent: `<div class="map-balloon">
                ${row(t('Number'), request.id)}
                ${row(t('Status'), STATUS_NAMES[request.status])}
                ${request.assignedToEmail ? row(t('Assignee'), request.assignedToEmail) : ''}
                ${row(t('Email'), request.email)}
                ${row(t('Comment'), request.comment)}
                ${row(t('Time'), new Date(request.timestamp).toLocaleString(language))}
                ${row(t('Coordinates'), formatCoords(coords))}
            </div>`
        }, { preset: STATUS_ICONS[request.status] }));
    }

    // Only open requests belong on the dispatcher's map
    async function loadRequests() {
        try {
            const response = await fetch('/api/transport-requests');
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            const requests = await response.json();
            markers.removeAll();
            requests.filter(request => request.status in STATUS_ICONS).forEach(addRequest);
            showStatus('');
        } catch (error) {
            console.error(error);
            showStatus(t('RequestsFailed'));
        }
    }

    document.addEventListener('transport:changed', loadRequests);
    await loadRequests();
});
