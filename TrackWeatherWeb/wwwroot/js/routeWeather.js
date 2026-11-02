// Weather along a built route: coloured stretches on the map and a list of what to expect, and when.
window.TrackWeatherRouteWeather = (function () {
    'use strict';

    const { t, escapeHtml, language } = TrackWeatherMap;

    const MAX_POINTS = 5000;
    const COLORS = { Low: '#1f9d55', Moderate: '#f59f00', High: '#d9253a', Unknown: '#8a94a6' };
    const LABELS = { Low: t('RiskLow'), Moderate: t('RiskModerate'), High: t('RiskHigh'), Unknown: t('RiskUnknown') };
    const OVERALL = { Low: t('OverallLow'), Moderate: t('OverallModerate'), High: t('OverallHigh'), Unknown: t('OverallUnknown') };

    const panel = document.getElementById('route-weather');
    const overallBox = document.getElementById('route-weather-overall');
    const list = document.getElementById('route-weather-points');
    const departureSelect = document.getElementById('route-departure');

    let map = null;
    let onRequestTransport = null;
    let overlay = null;
    let points = [];
    let latest = null;
    let requestNumber = 0;

    // The route line with the planned time to each vertex; the server decides where to check the forecast
    function extractPoints(route) {
        const result = [];
        let elapsed = 0;
        route.getPaths().each(path => {
            path.getSegments().forEach(segment => {
                const coords = segment.getCoordinates();
                const time = segment.getTime();
                coords.forEach((coord, index) => {
                    result.push({ lat: coord[0], lon: coord[1], seconds: elapsed + time * index / Math.max(coords.length - 1, 1) });
                });
                elapsed += time;
            });
        });
        if (result.length <= MAX_POINTS) {
            return result;
        }
        const step = result.length / (MAX_POINTS - 1);
        const thinned = Array.from({ length: MAX_POINTS - 1 }, (_, index) => result[Math.floor(index * step)]);
        thinned.push(result[result.length - 1]);
        return thinned;
    }

    function clearOverlay() {
        if (overlay) {
            map.geoObjects.remove(overlay);
            overlay = null;
        }
    }

    function drawStretches(data) {
        clearOverlay();
        overlay = new ymaps.GeoObjectCollection();
        data.points.forEach(point => {
            const line = points.slice(point.startIndex, point.endIndex + 1).map(p => [p.lat, p.lon]);
            if (line.length > 1) {
                overlay.add(new ymaps.Polyline(line, { hintContent: escapeHtml(hintFor(point)) }, {
                    strokeColor: COLORS[point.risk],
                    strokeWidth: 7,
                    strokeOpacity: 0.85
                }));
            }
        });
        map.geoObjects.add(overlay);
    }

    function hintFor(point) {
        const hazards = point.hazards.map(h => h.title).join(', ');
        return `${formatEta(point.etaUtc)}: ${hazards || point.summary || LABELS[point.risk]}`;
    }

    function formatEta(iso) {
        return new Date(iso).toLocaleString(language, { weekday: 'short', hour: '2-digit', minute: '2-digit' });
    }

    function renderPoint(point, index) {
        const temp = point.temp === null || point.temp === undefined ? '' : `${Math.round(point.temp)} °C`;
        const details = [temp, point.summary].filter(Boolean).join(', ') || LABELS.Unknown;
        const hazards = point.hazards.map(h =>
            `<span class="hazard hazard--${escapeHtml(h.level.toLowerCase())}">${escapeHtml(h.title)}</span>`).join('');
        const action = point.risk === 'Moderate' || point.risk === 'High'
            ? `<button type="button" class="btn btn-outline-danger btn-sm" data-route-request="${index}">${escapeHtml(t('RoadsideHelp'))}</button>`
            : '';
        return `<li class="route-point route-point--${escapeHtml(point.risk.toLowerCase())}">
                    <button type="button" class="route-point__main" data-route-point="${index}">
                        <span class="route-point__where">${escapeHtml(Math.round(point.distanceKm))} ${escapeHtml(t('KmShort'))} · ${escapeHtml(formatEta(point.etaUtc))}</span>
                        <span class="route-point__details">${escapeHtml(details)}</span>
                        <span class="route-point__hazards">${hazards}</span>
                    </button>
                    ${action}
                </li>`;
    }

    function render(data) {
        latest = data;
        const dangerous = data.points.filter(p => p.risk === 'High' || p.risk === 'Moderate').length;
        overallBox.className = `route-weather__overall route-weather__overall--${data.overallRisk.toLowerCase()}`;
        overallBox.textContent = dangerous > 0
            ? `${OVERALL[data.overallRisk]} ${t('DangerousStretches')}: ${dangerous} / ${data.points.length}`
            : OVERALL[data.overallRisk];
        list.innerHTML = data.points.map(renderPoint).join('');
        drawStretches(data);
    }

    function showMessage(message) {
        overallBox.className = 'route-weather__overall';
        overallBox.textContent = message;
        list.innerHTML = '';
    }

    async function load() {
        const number = ++requestNumber;
        showMessage(t('RouteWeatherLoading'));
        clearOverlay();
        const hours = Number(departureSelect.value);
        const departure = hours > 0 ? new Date(Date.now() + hours * 3600 * 1000).toISOString() : null;
        try {
            const response = await fetch('/api/weather/route', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ points, departure })
            });
            if (number !== requestNumber) {
                return;
            }
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            render(await response.json());
        } catch (error) {
            console.error(error);
            if (number === requestNumber) {
                showMessage(t('RouteWeatherFailed'));
            }
        }
    }

    function show(route) {
        points = extractPoints(route);
        departureSelect.value = '0';
        panel.hidden = false;
        load();
    }

    function clear() {
        requestNumber++;
        clearOverlay();
        points = [];
        latest = null;
        panel.hidden = true;
        list.innerHTML = '';
    }

    function init(mapInstance, requestTransportHandler) {
        map = mapInstance;
        onRequestTransport = requestTransportHandler;
        departureSelect.addEventListener('change', load);
        list.addEventListener('click', event => {
            const requestButton = event.target.closest('[data-route-request]');
            if (requestButton) {
                const point = latest.points[Number(requestButton.dataset.routeRequest)];
                const hazards = point.hazards.map(h => h.title).join(', ');
                onRequestTransport([point.lat, point.lon], `${hazards}, ${Math.round(point.distanceKm)} ${t('KmShort')}`);
                return;
            }
            const pointButton = event.target.closest('[data-route-point]');
            if (pointButton) {
                const point = latest.points[Number(pointButton.dataset.routePoint)];
                map.panTo([point.lat, point.lon], { flying: false });
                map.hint.open([point.lat, point.lon], escapeHtml(hintFor(point)));
            }
        });
    }

    return { init, show, clear };
})();
