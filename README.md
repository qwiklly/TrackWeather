# КартаПогоды (TrackWeather)

TrackWeather - это приложение для мониторинга погодных условий и построения маршрутов с учетом погодной обстановки. Оно предназначено для путешественников и технических служб, таких как аварийные службы, которым необходимо быстро реагировать на изменения погоды.

# Основной функционал:
* Просмотр погоды в любой точке мира
* Построение маршрутов с учетом погодных условий
* Регистрация и авторизация пользователей
* Администрирование: просмотр и управление пользователями, управление транспортными запросами

# Используемые технологии:
.Net 8, C#, ASP Net, JavaScript, API, MS SQL, Swagger, Unit tests, Docker

## Примеры использования:
- **Для путешественников:** находите места с хорошей погодой для планирования поездок.
- **Для коммунальных служб:** отслеживайте районы с неблагоприятной погодой для отправки специального транспорта.

### Используемая технология аутентификации:
- Аутентификация пользователей осуществляется с использованием Web токенов (JWT).
- JWT хранится в куки, тем самым сессия пользователя сохраняется.

# Шаги для запуска проекта:
Выполните команду в консоли диспетчера пакетов для обновления базы данных:
```
update-database
```
В файле appsettings.json добавьте ключи разработчика для YandexApi и OpenWeatherApi.

![image](https://github.com/user-attachments/assets/b4e9c736-ce4b-4ab8-9194-008861fa3ca7)

**Сборка Docker**
Соберите Docker image, выполнив команду:
```
docker build -t trackweather/localtrackweather:v1 .
```
Затем раскомментируйте строку подключения к базе данных для Docker.

![image](https://github.com/user-attachments/assets/0fb95e4b-d5d0-4cfc-9113-1d34443167bd)

Создайте и запустите контейнер:
```
docker-compose up --build
```
 
## Изображения интерфейса 
### **Начальный экран**

![Screenshot 2024-09-09 132443](https://github.com/user-attachments/assets/e84515fa-115d-47c8-8762-09e90ce0ee90)

### **Окно карты для пользователей**

![Screenshot 2024-09-09 132754](https://github.com/user-attachments/assets/6a8d78c8-7723-4f70-8c7e-93e90f2106ce)

### **Окно карты для коммунальных служб**

![Screenshot 2024-09-09 132654](https://github.com/user-attachments/assets/45306f26-2dd5-4faa-a3bf-c2939bc604a2)

### **Список запросов транспортных средств с их координатами и удаление**

![Screenshot 2024-09-09 132512](https://github.com/user-attachments/assets/d3aa22d7-c46b-4c9c-bbce-96e9c55b9f1d)

### **Список пользователей и удаление**

![Screenshot 2024-09-09 132615](https://github.com/user-attachments/assets/656016ee-a34f-406b-9109-8ed1abcd6ae5)

### **Окно добавления пользователя**

![Screenshot 2024-09-09 132634](https://github.com/user-attachments/assets/e2cc27d2-794d-41cc-a1f9-4ce72ef928a2)

### **Окно авторизации**

![Screenshot 2024-09-09 132841](https://github.com/user-attachments/assets/73ac316b-97ce-4e38-ab69-0141d0ed1b7f)

### Профиль пользователя
**Авторизован**            |  **Не авторизован**
:-------------------------:|:-------------------------:
![Screenshot 2024-08-28 172831](https://github.com/user-attachments/assets/bf7bf5fd-83c5-474b-92e2-698d6c2748dd) |  ![Screenshot 2024-08-28 172904](https://github.com/user-attachments/assets/d7b757be-d8fc-4f4b-930a-fe0d665feb32)

### **Окно Swagger**

![image](https://github.com/user-attachments/assets/94ac7b50-6f50-455d-a86c-7b5e45d5c5db)

