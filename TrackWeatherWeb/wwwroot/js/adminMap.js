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

    function addPlaceMark(coords, content) {
        let placeMark = new ymaps.Placemark(coords, {
            balloonContent: content
        });
        myMap.geoObjects.add(placeMark);
    }

    async function loadCoordinates() {
        try {
            let response = await fetch('/api/application/getAllTransportRequests');
            let data = await response.json();

            data.forEach(request => {
                let content = `
                                    <strong>Email:</strong> ${request.email}<br>
                                    <strong>Comment:</strong> ${request.comment}<br>
                                    <strong>Timestamp:</strong> ${new Date(request.timestamp).toLocaleString()}<br>
                                    <strong>Coordinates:</strong> [${request.coordinate_x}, ${request.coordinate_y}]
                                `;
                addPlaceMark([request.coordinate_x, request.coordinate_y], content);
            });
        } catch (error) {
            console.error('Ошибка загрузки координат:', error);
        }
    }

    await loadCoordinates();
});