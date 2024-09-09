ymaps.ready(async function () {
    let myMap = new ymaps.Map('map-test', {
        center: [51.672, 39.1843],
        zoom: 10
    });

    async function addPlacemark(coords) {
        // Initialize the placemark without weather information
        let placemark = new ymaps.Placemark(coords, {
            balloonContent: `<div style="padding: 10px;">
                                <strong>Координаты:</strong> ${coords}<br>
                                <div id="weather-info">Загрузка данных о погоде...</div>
                                <button id="routeButton">Проложить маршрут</button>
                            </div>`
        });

        // Event handler for opening the balloon
        placemark.events.add('balloonopen', function () {
            // Fetch weather data
            fetch(`/api/application/getWeather/${coords[0]},${coords[1]}`)
                .then(response => response.json())
                .then(data => {
                    let weatherInfo = data.city.length ? `
                        <strong>Город:</strong> ${data.city}<br>
                        <strong>Температура:</strong> ${data.temp} °C<br>
                        <strong>Погода:</strong> ${data.summary}` : 'Получены неполные данные.';

                    placemark.properties.set('balloonContent', `
                        <div style="padding: 10px;">
                            <strong>Координаты:</strong> ${coords}<br>
                            ${weatherInfo}
                            <br><button id="routeButton">Проложить маршрут</button>
                        </div>`
                    );

                    // Wait until the balloon content is updated, then add the event listener
                    placemark.balloon.open().then(() => {
                        let routeButton = document.querySelector('#routeButton');
                        if (routeButton) {
                            routeButton.addEventListener('click', function () {
                                // Get user's current location
                                ymaps.geolocation.get().then(function (res) {
                                    const userCoords = res.geoObjects.position;
                                    const endPoint = placemark.geometry.getCoordinates();
                                    ymaps.route([userCoords, endPoint]).then(function (route) {
                                        myMap.geoObjects.add(route);
                                    });
                                }).catch(function () {
                                    // If location can't be obtained, prompt for address input
                                    placemark.properties.set('balloonContent', `
                                        <div style="padding: 10px;">
                                            <strong>Координаты:</strong> ${coords}<br>
                                            Не удалось определить местоположение.<br>
                                            <label for="address">Введите начальный адрес:</label>
                                            <input type="text" id="address" placeholder="Введите адрес">
                                            <br><button id="buildRoute">Проложить маршрут</button>
                                        </div>`
                                    );

                                    let buildRouteButton = document.querySelector('#buildRoute');
                                    if (buildRouteButton) {
                                        buildRouteButton.addEventListener('click', function () {
                                            const address = document.querySelector('#address').value;
                                            if (address) {
                                                ymaps.geocode(address).then(function (res) {
                                                    const startPoint = res.geoObjects.get(0).geometry.getCoordinates();
                                                    const endPoint = placemark.geometry.getCoordinates();
                                                    ymaps.route([startPoint, endPoint]).then(function (route) {
                                                        myMap.geoObjects.add(route);
                                                    });
                                                });
                                            }
                                        });
                                    }
                                });
                            });
                        }
                    });
                })
                .catch(error => {
                    console.error('Ошибка получения данных о погоде:', error);
                    placemark.properties.set('balloonContent', `
                        <div style="padding: 10px;">
                            <strong>Координаты:</strong> ${coords}<br>
                            Не удалось загрузить данные о погоде.
                            <br><button id="routeButton">Проложить маршрут</button>
                        </div>`
                    );
                });
        });

        // Add the placemark to the map
        myMap.geoObjects.add(placemark);
    }

    // Right-click event to add a placemark
    myMap.events.add('contextmenu', function (e) {
        const coords = e.get('coords');
        addPlacemark(coords);
    });
});
