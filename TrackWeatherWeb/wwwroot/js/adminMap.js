TrackWeatherMap.ready(async function () {
    'use strict';

    const { t, escapeHtml, formatCoords, showStatus, language } = TrackWeatherMap;

    const map = new ymaps.Map('map', {
        center: [51.672, 39.1843],
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

    function row(label, value) {
        return `<div class="map-balloon__row"><strong>${escapeHtml(label)}:</strong> ${escapeHtml(value)}</div>`;
    }

    function addRequest(request) {
        const coords = [request.latitude, request.longitude];
        map.geoObjects.add(new ymaps.Placemark(coords, {
            hintContent: escapeHtml(request.comment),
            balloonContent: `<div class="map-balloon">
                ${row(t('Email'), request.email)}
                ${row(t('Comment'), request.comment)}
                ${row(t('Time'), new Date(request.timestamp).toLocaleString(language))}
                ${row(t('Coordinates'), formatCoords(coords))}
            </div>`
        }, { preset: 'islands#redAutoIcon' }));
    }

    try {
        const response = await fetch('/api/transport-requests');
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        (await response.json()).forEach(addRequest);
    } catch (error) {
        console.error(error);
        showStatus(t('RequestsFailed'));
    }
});
