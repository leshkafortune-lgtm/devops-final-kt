# WorkoutApp

REST API для управления спортивными тренировками. Микросервисная архитектура на Docker Compose: единая точка входа через Nginx, бизнес-логика на .NET 8, постоянное хранение в PostgreSQL и кэширование чтения в Redis.

## Какую задачу решает
Спортсмен/тренер ведёт список тренировок: смотрит все записи, добавляет новую, редактирует существующую, удаляет ненужную. Все данные переживают перезапуск системы.

## Сервисы (4 контейнера)
| Сервис | Папка | Порт наружу | Роль |
|---|---|---|---|
| Nginx (API Gateway) | `nginx/` | 80 | Reverse proxy, единственная точка входа |
| .NET 8 Web API | `api/` | — (только внутри сети, 8080) | CRUD-логика + кэш |
| PostgreSQL 16 | `db/` | 5432 | Персистентное хранение (volume `db_data`) |
| Redis 7 | `redis/` | 6379 | Кэш списка тренировок (TTL 5 мин) |

Наружу публикуется **только порт 80** шлюза; API, БД и кэш доступны исключительно внутри сети Compose по DNS-именам `api`, `db`, `redis`.

## Запуск
```bash
docker compose up --build -d
docker compose ps        # все 4 сервиса Up, db/redis healthy
```

## Проверка API
```bash
curl http://localhost/workouts
```
Или импортируй `postman/WorkoutApp.postman_collection.json` в Postman и выполни запросы. Примеры curl — в `scripts/api_examples.sh` (запускать из корня проекта).

## Персистентность данных
```bash
docker compose down      # БЕЗ флага -v
docker compose up -d
curl http://localhost/workouts   # данные сохранились (volume db_data)
```

## Архитектура C4
Диаграммы трёх уровней — в папке [`docs/`](./docs/):
- `context.png` — Level 1 (пользователь ↔ система);
- `container.png` — Level 2 (4 контейнера, порты, протоколы);
- `component.png` — Level 3 (внутренности API: Program.cs, AppDbContext, Cache Service, Endpoints);
- `architecture.drawio` — редактируемый исходник.

## Пользовательский сценарий
GET список → POST «Ноги» → PUT правка длительности → DELETE → `down`/`up` → данные целы.

## Обоснование дополнительных компонентов
**Redis** выбран как in-memory кэш: чтений списка много, писаний мало, асинхронных задач нет — поэтому кэш оптимален, а очереди (RabbitMQ/Kafka) и объектное хранилище (MinIO/S3) избыточны. **Nginx** — единая точка входа: скрывает внутренний порт API, готов к балансировке/масштабированию без правок кода. 