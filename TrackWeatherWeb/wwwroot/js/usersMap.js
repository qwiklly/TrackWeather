TrackWeatherMap.ready(function () {
    'use strict';

    const { t, escapeHtml, formatCoords, showStatus } = TrackWeatherMap;

    const map = new ymaps.Map('map', {
        center: [51.672, 39.1843],
        zoom: 10,
        controls: ['zoomControl', 'searchControl', 'typeSelector', 'geolocationControl']
    });

    let openPlacemark = null;
    let activeRoute = null;

    function balloon(coords, body) {
        return `<div class="map-balloon">
                    <div class="map-balloon__row"><strong>${escapeHtml(t('Coordinates'))}:</strong> ${escapeHtml(formatCoords(coords))}</div>
                    ${body}
                    <div class="map-balloon__actions">
                        <button type="button" class="btn btn-primary btn-sm" data-map-action="route-from-me">${escapeHtml(t('RouteFromMe'))}</button>
                        <button type="button" class="btn btn-outline-primary btn-sm" data-map-action="ask-address">${escapeHtml(t('RouteFromAddress'))}</button>
                    </div>
                </div>`;
    }

    function weatherHtml(weather) {
        const rows = [];
        if (weather.city) {
            rows.push([t('City'), weather.city]);
        }
        if (weather.temp !== null && weather.temp !== undefined) {
            rows.push([t('Temperature'), `${Math.round(weather.temp)} °C`]);
        }
        if (weather.summary) {
            rows.push([t('Weather'), weather.summary]);
        }
        return rows.length === 0
            ? `<div class="map-balloon__row">${escapeHtml(t('WeatherIncomplete'))}</div>`
            : rows.map(([label, value]) =>
                `<div class="map-balloon__row"><strong>${escapeHtml(label)}:</strong> ${escapeHtml(value)}</div>`).join('');
    }

    async function loadWeather(placemark) {
        const coords = placemark.geometry.getCoordinates();
        let body;
        try {
            const response = await fetch(`/api/weather?lat=${coords[0]}&lon=${coords[1]}`);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            body = weatherHtml(await response.json());
            placemark.weatherLoaded = true;
        } catch (error) {
            console.error(error);
            body = `<div class="map-balloon__row text-danger">${escapeHtml(t('WeatherFailed'))}</div>`;
        }
        placemark.weatherBody = body;
        placemark.properties.set('balloonContent', balloon(coords, body));
    }

    function addPlacemark(coords) {
        const placemark = new ymaps.Placemark(coords, {
            balloonContent: balloon(coords, `<div class="map-balloon__row text-muted">${escapeHtml(t('WeatherLoading'))}</div>`)
        }, { preset: 'islands#blueCircleDotIcon' });

        placemark.events.add('balloonopen', function () {
            openPlacemark = placemark;
            if (!placemark.weatherLoaded) {
                loadWeather(placemark);
            }
        });

        map.geoObjects.add(placemark);
        placemark.balloon.open();
    }

    function clearRoute() {
        if (activeRoute) {
            map.geoObjects.remove(activeRoute);
            activeRoute = null;
        }
        showStatus('');
    }

    async function buildRoute(from, to) {
        showStatus(t('RouteBuilding'));
        try {
            const route = await ymaps.route([from, to]);
            clearRoute();
            activeRoute = route;
            map.geoObjects.add(route);
            map.setBounds(route.getPaths().getBounds(), { checkZoomRange: true, zoomMargin: 40 });
            map.balloon.close();
            // Yandex formats these for HTML ("19&#160;км"), the status line shows plain text
            const plain = value => value.replace(/&#160;|&nbsp;/g, ' ');
            showStatus(`${t('RouteSummary')}: ${plain(route.getHumanLength())}, ${plain(route.getHumanTime())}`, clearRoute);
        } catch (error) {
            console.error(error);
            showStatus(t('RouteFailed'));
        }
    }

    function askAddress(placemark, message) {
        const coords = placemark.geometry.getCoordinates();
        const note = message ? `<div class="map-balloon__row text-danger">${escapeHtml(message)}</div>` : '';
        placemark.properties.set('balloonContent', `
            <div class="map-balloon">
                <div class="map-balloon__row"><strong>${escapeHtml(t('Coordinates'))}:</strong> ${escapeHtml(formatCoords(coords))}</div>
                ${note}
                <label class="form-label small mb-1" for="route-address">${escapeHtml(t('AddressLabel'))}</label>
                <input type="text" class="form-control form-control-sm mb-2" id="route-address" placeholder="${escapeHtml(t('AddressPlaceholder'))}" />
                <button type="button" class="btn btn-primary btn-sm" data-map-action="route-from-address">${escapeHtml(t('BuildRoute'))}</button>
            </div>`);
    }

    async function routeFromMe(placemark) {
        showStatus(t('Locating'));
        try {
            const location = await ymaps.geolocation.get();
            await buildRoute(location.geoObjects.position, placemark.geometry.getCoordinates());
        } catch (error) {
            console.error(error);
            showStatus('');
            askAddress(placemark, t('GeolocationFailed'));
        }
    }

    async function routeFromAddress(placemark, address) {
        if (!address) {
            return;
        }
        try {
            const result = await ymaps.geocode(address, { results: 1 });
            const start = result.geoObjects.get(0);
            if (!start) {
                askAddress(placemark, t('AddressNotFound'));
                return;
            }
            await buildRoute(start.geometry.getCoordinates(), placemark.geometry.getCoordinates());
        } catch (error) {
            console.error(error);
            askAddress(placemark, t('AddressNotFound'));
        }
    }

    // Balloon markup is re-created on every update, so actions are handled in one place.
    // Capture phase: the map stops some click events from bubbling out of its balloons.
    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-map-action]');
        if (!button || !openPlacemark) {
            return;
        }
        switch (button.dataset.mapAction) {
            case 'route-from-me':
                routeFromMe(openPlacemark);
                break;
            case 'ask-address':
                askAddress(openPlacemark);
                break;
            case 'route-from-address':
                routeFromAddress(openPlacemark, document.getElementById('route-address').value.trim());
                break;
        }
    }, true);

    // Right click (long tap on touch screens) adds a placemark
    map.events.add('contextmenu', event => addPlacemark(event.get('coords')));
});
