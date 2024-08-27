ymaps.ready(async function () {
    let myMap = new ymaps.Map('map-test', {
        center: [51.672, 39.1843],
        zoom: 10
    });
    async function addPlacemark(coords) {
        // Initialising a tag without weather information
        var placemark = new ymaps.Placemark(coords, {
            balloonContent: `<div style="padding: 10px;">
														<strong>Координаты:</strong> ${coords}<br>
														<div id="weather-info">Загрузка данных о погоде...</div>
														<button id="routeButton">Проложить маршрут</button>
													 </div>`
        });

        // Balun opening handler for tag
        placemark.events.add('balloonopen', function () {
            // Load weather data by tag coordinates
            fetch(`https://localhost:7118/api/application/getWeather/${coords[0]},${coords[1]}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    console.log('API response data:', data);
                    let weatherInfo = '';

                    if (data.city.length != 0) {
                        weatherInfo = `
													<strong>Город:</strong> ${data.city}<br>
													<strong>Температура:</strong> ${data.temp} °C<br>
													<strong>Погода:</strong> ${data.summary}
												`;
                    } else {
                        weatherInfo = 'Получены неполные данные.';
                    }

                    // Updating weather information in balun
                    placemark.properties.set({
                        balloonContent: `<div style="padding: 10px;">
																	<strong>Координаты:</strong> ${coords}<br>
																	${weatherInfo}
																	<br><button id="routeButton">Проложить маршрут</button>
																 </div>`
                    });
                })
                .catch(error => {
                    console.error('Ошибка получения данных о погоде:', error);
                    placemark.properties.set({
                        balloonContent: `<div style="padding: 10px;">
																	<strong>Координаты:</strong> ${coords}<br>
																	Не удалось загрузить данные о погоде.
																	<br><button id="routeButton">Проложить маршрут</button>
																 </div>`
                    });
                });
        });

        // Handler for clicking the ‘Route’ button inside the balun
        placemark.events.add('balloonopen', function () {
            document.addEventListener('click', function (event) {
                if (event.target && event.target.id === 'routeButton') {
                    // Попытка получить текущее местоположение пользователя
                    let location = ymaps.geolocation.get();
                    location.then(function (res) {
                        const userCoords = res.geoObjects.position;
                        const endPoint = placemark.geometry.getCoordinates();
                        ymaps.route([userCoords, endPoint]).then(function (route) {
                            myMap.geoObjects.add(route);
                        });
                    }).catch(function () {
                        // If the location could not be retrieved, ask to enter the address inside the balun
                        placemark.properties.set({
                            balloonContent: `<div style="padding: 10px;">
																		<strong>Координаты:</strong> ${coords}<br>
																		Не удалось определить местоположение.<br>
																		<label for="address">Введите начальный адрес:</label>
																		<input type="text" id="address" placeholder="Введите адрес">
																		<br>
																		<button id="build-route">Проложить маршрут</button>
																	 </div>`
                        });

                        document.addEventListener('click', function (event) {
                            if (event.target && event.target.id === 'build-route') {
                                const address = document.getElementById('address').value;
                                if (address) {
                                    ymaps.geocode(address).then(function (res) {
                                        const startPoint = res.geoObjects.get(0).geometry.getCoordinates();
                                        const endPoint = placemark.geometry.getCoordinates();
                                        ymaps.route([startPoint, endPoint]).then(function (route) {
                                            myMap.geoObjects.add(route);
                                        });
                                    });
                                }
                            }
                        });
                    });
                }
            });
        });

        myMap.geoObjects.add(placemark);
    }

    // Right-click handler for adding a tag
    myMap.events.add('contextmenu', function (e) {
        const coords = e.get('coords');
        addPlacemark(coords);
    });
});