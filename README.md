# WorkoutApp

Веб-приложение для управления спортивными тренировками. Микросервисная архитектура на Docker Compose: единая точка входа через Nginx (хостинг фронтенда + reverse proxy), бизнес-логика на .NET 8, постоянное хранение в PostgreSQL, кэширование чтения в Redis.

## Какую задачу решает
Спортсмен/тренер ведёт список тренировок: смотрит все записи, добавляет новую (с указанием подходов и повторений), редактирует существующую, удаляет ненужную. Интерфейс — веб-страница на `http://localhost`, данные переживают перезапуск системы.

## Сервисы (4 контейнера)
| Сервис | Папка | Порт наружу | Роль |
|---|---|---|---|
| Nginx | `nginx/` | 80 | API Gateway + хостинг фронтенда (`index.html`), reverse proxy на API |
| .NET 8 Web API | `api/` | — (только внутри сети, 8080) | CRUD-логика, кэш, модель тренировки |
| PostgreSQL 16 | `db/` | 5432 | Персистентное хранение (volume `db_data`) |
| Redis 7 | `redis/` | 6379 | Кэш списка тренировок (TTL 5 мин) |

Наружу публикуется **только порт 80** шлюза; API, БД и кэш доступны исключительно внутри сети Compose по DNS-именам `api`, `db`, `redis`.

## Модель данных
Поле `Workout`: `Id`, `Name`, `Description`, `MuscleGroup`, `Sets` (подходы), `Reps` (повторения), `Difficulty`.

## Запуск
```bash
docker compose up --build -d
docker compose ps        # все 4 сервиса Up, db/redis healthy
```

## Проверка
Открой в браузере **http://localhost** — веб-интерфейс со списком тренировок и формой добавления.

API напрямую (JSON):
```bash
curl http://localhost/workouts
```

Или импортируй `postman/WorkoutApp.postman_collection.json` в Postman. Примеры curl — в `scripts/api_examples.sh` (запускать из корня проекта).

## Персистентность данных
```bash
docker compose down      # БЕЗ флага -v
docker compose up -d
curl http://localhost/workouts   # данные сохранились (volume db_data)
```

## Архитектура C4
Диаграммы трёх уровней (Context, Container, Component) — в одном файле:

![C4 Architecture](docs/architecture.png)

Редактируемый исходник — [`docs/architecture.drawio`](docs/architecture.drawio).

## Пользовательский сценарий
Открыть `http://localhost` → GET список → через форму POST «Ноги» (5 подходов × 8 повторений) → PUT правка подходов/повторений → DELETE → `down`/`up` → данные целы.

## Обоснование дополнительных компонентов
**Redis** — in-memory кэш: чтений списка много, писаний мало, асинхронных задач нет, поэтому кэш оптимален, а очереди (RabbitMQ/Kafka) и объектное хранилище (MinIO/S3) избыточны. **Nginx** — единая точка входа: раздаёт фронтенд, проксирует API, скрывает внутренний порт сервиса, готов к балансировке/масштабированию без правок кода.

## Структура репозитория
```text
WorkoutApp/
├── api/                  # .NET 8 Web API (Program.cs, Models, Data, Dockerfile)
├── nginx/                # Gateway + фронтенд (Dockerfile, nginx.conf, index.html)
├── db/                   # PostgreSQL (Dockerfile)
├── redis/                # Redis (Dockerfile)
├── docs/                 # C4-схемы (architecture.drawio, architecture.png)
├── postman/              # Postman-коллекция API
├── scripts/              # Примеры curl-запросов
├── docker-compose.yml    # Оркестрация 4 сервисов + volume db_data
├── README.md             # Настоящий файл
└── .gitignore
```