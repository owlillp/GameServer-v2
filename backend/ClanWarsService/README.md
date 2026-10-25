# ClanWarsService

Сезоны клановых войн и рейтинг кланов. Схема БД — `clanwars`, лидерборд — Redis ZSET.
Битвы (этап 6) и награды (этап 7) строятся поверх этого.

## Сезон

`Draft → Scheduled → Active → Finalizing → (Rewarding) → Closed`

- Админ создаёт черновик (имя, `startsAt`, `endsAt`, `finalizationGraceMinutes`, `scoringRules`)
  и публикует его. Пересекаться с другим опубликованным сезоном нельзя.
- Дальше фазы идут по расписанию. На каждый сезон — durable Quartz-job с тремя триггерами:
  старт, конец, финализация (`endsAt + grace`). JobStore в PostgreSQL, кластерный — граница
  срабатывает на одной реплике; пропущенная при выключенном сервисе — сразу после запуска
  (`misfire = FireNow`). При старте сервис сверяет триггеры с базой.
- Переход выполняет `SeasonLifecycle` с блокировкой строки сезона: фаза вычисляется из часов,
  все пропущенные переходы применяются по порядку, повторный вызов ничего не меняет.
- `Finalizing` — окно для битв, начатых до конца сезона: очки ещё принимаются.
- При закрытии итоговые места замораживаются в `season_results` (из PostgreSQL),
  публикуется `season-ended.clanwars` с топом. С таблицей наград сезон ждёт в `Rewarding` (этап 7).

## Очки

- Только через `ScoreService`: чек `score_receipts` уникален по (сезон, клан, источник), очки —
  атомарный `points = points + delta` в той же транзакции, под блокировкой сезона (после
  заморозки итогов очки не принимаются).
- Источники: `battle:{id}` (этап 6), `admin:{operationId}` — ручная корректировка с аудитом.
- Redis получает абсолютное значение, прочитанное из PostgreSQL. Составной score
  (очки + время достижения) — при равенстве выше тот, кто набрал раньше.
- Расхождение ZSET и PostgreSQL проверяется по таймеру и после ошибок записи; пересборка — во
  временный ключ пачками + `RENAME`, под advisory lock сезона. Чтение при недоступном Redis — из PostgreSQL.

## Состав кланов

Проекция `clans` / `clan_roster` из `clan-events` (создание, роспуск, вступление, выход, смена роли).
Дубликаты отсекает inbox Wolverine, опоздавшие события — время события в строке.

## API

| Метод | Путь | Что делает |
| --- | --- | --- |
| `GET` | `/clanwars/seasons/current` | Идущий сезон или ближайший запланированный |
| `GET` | `/clanwars/seasons/{id}/leaderboard?limit=100` | Топ кланов |
| `GET` | `/clanwars/seasons/{id}/leaderboard/me?neighbors=5` | Место клана игрока с соседями |
| `GET` | `/clanwars/seasons/{id}/clans/{clanId}?neighbors=5` | Место клана с соседями |
| `GET` | `/clanwars/seasons/{id}/clans/{clanId}/members` | Вклад участников (заполняют битвы) |
| `GET` | `/admin/clanwars/seasons` | Все сезоны (`clanwars.view`) |
| `POST` | `/admin/clanwars/seasons` | Черновик (`clanwars.manage`) |
| `PUT` | `/admin/clanwars/seasons/{id}` | Изменить черновик |
| `POST` | `/admin/clanwars/seasons/{id}/publish` | Опубликовать |
| `GET` | `/admin/clanwars/seasons/{id}/scores?clanId=` | Чеки очков (аудит) |
| `POST` | `/admin/clanwars/seasons/{id}/scores` | Корректировка `{ clanId, amount, reason, operationId }` |
| `POST` | `/admin/clanwars/seasons/{id}/leaderboard/rebuild` | Пересобрать ZSET |

## События (exchange `clanwars-events`)

`season-started.clanwars`, `season-ended.clanwars` → RealtimeService (всем подключённым).
`clan-points-granted.clanwars` (этап 7) слушает ClanService.

## Конфигурация

`ConnectionStrings:ClanWarsDb`, `RabbitMQ`, `Redis`; `Authentication:Audiences = ["clanwars-service"]`
(scope `clanwars`); секция `ClanWars` (grace по умолчанию, размеры топа и выборок, Quartz).
Порт в dev — `5293`. Health: `/health/live`, `/health/ready` (Redis не входит в readiness).

## Проверка «сезон на час»

```bash
# черновик, который стартует через 2 минуты и длится час, затем публикация
curl -sk -X POST https://localhost:6101/admin/clanwars/seasons -H "Authorization: Bearer $ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","startsAt":"<now+2m>","endsAt":"<now+62m>","finalizationGraceMinutes":1}'
curl -sk -X POST https://localhost:6101/admin/clanwars/seasons/<id>/publish -H "Authorization: Bearer $ADMIN"
# можно остановить clanwars-service на границе и запустить снова — переход выполнится при старте
```
