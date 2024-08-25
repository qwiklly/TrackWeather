ymaps.ready(function () {
	let myMap = new ymaps.Map('map-test', {
		center: [51.672, 39.1843],
		zoom: 10
	});
	function addPlacemark(coords) {
		// Инициализация метки без данных о погоде
		var placemark = new ymaps.Placemark(coords, {
			balloonContent: `<div style="padding: 10px;">
														<strong>Координаты:</strong> ${coords}<br>
														<div id="weather-info">Загрузка данных о погоде...</div>
														<button id="routeButton">Проложить маршрут</button>
													 </div>`
		});

		// Обработчик открытия балуна для метки
		placemark.events.add('balloonopen', function () {
			// Загрузка данных о погоде по координатам метки
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

					// Обновление информации о погоде в содержимом балуна
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

		// Обработчик клика на кнопку "Проложить маршрут" внутри балуна
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
						// Если не удалось получить местоположение, просим ввести адрес внутри балуна
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

	// Обработчик правого клика для добавления метки
	myMap.events.add('contextmenu', function (e) {
		const coords = e.get('coords');
		addPlacemark(coords);
	});

});