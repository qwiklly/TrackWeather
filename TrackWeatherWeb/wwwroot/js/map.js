ymaps.ready(async function () {
    let myMap = new ymaps.Map('map-test', {
        center: [51.672, 39.1843],
        zoom: 10,
        controls: ['routePanelControl']
    });

    let control = myMap.controls.get('routePanelControl');

    control.routePanel.state.set({
        type: 'auto',
        fromEnabled: true,
        from: [44.50497296854422, 38.90903090603487],
        toEnabled: true,
        to: [55.57697610501588, 37.68869270398412],
    });

    // add markers on map function
    function addPlaceMark(coords) {
        let placeMark = new ymaps.Placemark(coords);
        myMap.geoObjects.add(placeMark);
    }

    // load and display coordinates on map
    async function loadCoordinates() {
        try {
            let response = await fetch('/api/application/getAllTransportRequests');
            let data = await response.json();

            data.forEach(request => {
                addPlaceMark([request.coordinate_x, request.coordinate_y]);
            });
        } catch (error) {
            console.error('Ошибка загрузки координат:', error);
        }
    }

    // first load coordinates
    await loadCoordinates();
});