# SessionService

Единственная точка контакта с игровыми серверами: реестр живых серверов и сессий,
выдача join-тикетов игрокам, приём heartbeat и отчётов о матчах, публикация
канонического `match-completed.session`.

Схема БД — `sessions`, живой реестр — Redis.

## REST для игроков (через nginx / Gateway, JWT игрока, scope `session`)

| Метод | Путь | Что делает |
| --- | --- | --- |
| `POST` | `/sessions/join` | Войти в открытый мир: `{ "region": "eu-west", "mode": "FREE-ROAM" }` → адрес сервера + join-тикет |
| `POST` | `/sessions/join-friend/{accountId}` | Войти в сессию открытого мира, где сейчас друг |
| `GET` | `/sessions/me` | Текущая сессия игрока |
| `GET` | `/sessions/ticket-key` | Публичный ключ join-тикетов (анонимно) |
| `GET` | `/admin/sessions?openOnly=true&limit=100` | Сессии (`sessions.view`) |
| `GET` | `/admin/sessions/servers?region=eu-west` | Зарегистрированные серверы (`sessions.view`) |

Выбор сессии: игрок, который уже в подходящей сессии, возвращается в неё → самая
заполненная живая сессия региона со свободными местами → новая сессия на сервере
с наибольшим числом свободных слотов (`IGameServerAllocator`, в dev — `StaticPoolAllocator`
по зарегистрированным серверам).

## gRPC для игровых серверов (`Shared/Protos/session.proto`, только внутренний порт)

Токен client_credentials клиента из `GameServerAuth:AllowedClientIds` со scope
`session.write`. Токен игрока на gRPC не принимается.

| RPC | Что делает |
| --- | --- |
| `Register` | Регистрация (повторная — с тем же `server_id`), отдаёт интервал heartbeat и публичный ключ тикетов |
| `Heartbeat` (client streaming) | Продлевает `presence:server:{id}` и `session:{id}` для перечисленных сессий |
| `ValidateJoinTicket` | Проверка тикета игрока: подпись, срок, сервер, одноразовость |
| `PlayerJoined` / `PlayerLeft` | Состав сессии; жёсткий лимит мест проверяется здесь |
| `ReportMatch` | Итог матча; идемпотентен по `activity_id` |

Join-тикет — JWT RS256 на 60 с: `sub` (игрок), `session_id`, `server_id`, `jti`.
Повторное использование `jti` отклоняется через Redis.

## Отчёт о матче

1. Повтор с тем же `activity_id` → `duplicate = true`, событие повторно не публикуется
   (проверка перед записью + уникальный индекс на случай гонки).
2. Сервер должен владеть сессией, все игроки отчёта должны были в ней побывать.
3. Античит: длительность, очки и убийства в минуту (`AntiCheat:*`). Подозрительный
   отчёт принимается, но помечается флагами в `raw_reports.flags`.
4. `raw_reports` и `match-completed.session` (по событию на игрока) пишутся в одной
   транзакции через outbox Wolverine.

`MatchId` в событии: `match_id` сессии для матчей из очереди, иначе `activity_id`
(если это GUID) или стабильный GUID, вычисленный из него.

## Закрытие «мёртвых» сессий

`SessionLivenessSweeper` раз в `Sessions:LivenessSweepIntervalSeconds` забирает пачку
открытых сессий через `FOR UPDATE SKIP LOCKED` и закрывает те, у которых истёк
ключ `session:{id}` в Redis: `Active → Closed`, оставшиеся игроки отмечаются вышедшими.

## События (exchange `session-events`)

| Routing key | Контракт |
| --- | --- |
| `match-completed.session` | `MatchCompletedEvent` (существующий; добавлены необязательные `SessionId`, `ExternalRef`) |
| `player-joined.session` | `PlayerJoinedSessionEvent` |
| `player-left.session` | `PlayerLeftSessionEvent` |
| `session-closed.session` | `SessionClosedEvent` |

## Конфигурация

| Секция | Ключевые параметры |
| --- | --- |
| `ConnectionStrings` | `SessionDb`, `RabbitMQ`, `Redis` |
| `Authentication` | `Authority`, `Audiences: ["session-service"]` |
| `GameServerAuth` | `AllowedClientIds`, `RequiredScope` |
| `JoinTickets` | `SigningKeyPem` (из Lockbox), `KeyId`, `LifetimeSeconds`, `AllowEphemeralKey` (только dev) |
| `Sessions` | TTL сервера и сессии, интервал heartbeat, размер сессии открытого мира, параметры sweeper |
| `AntiCheat` | `MinMatchSeconds`, `MaxMatchMinutes`, `MaxScorePerMinute`, `MaxKillsPerMinute` |

Порты в dev: REST `5289`, gRPC `5290`. В Docker: REST `8080`, gRPC `8081`.
Health: `/health/live`, `/health/ready` (PostgreSQL, RabbitMQ, Redis).

## Локальная проверка с заглушкой сервера

`backend/tools/FakeGameServer` регистрируется, держит heartbeat-стрим и принимает
«игроков» по тикетам (в Docker — `http://localhost:5291`):

```bash
# 1. игрок получает тикет (TOKEN — access token игрока со scope session)
curl -sk -X POST https://localhost:6101/sessions/join \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"region":"eu-west"}'

# 2. «клиент» подключается к серверу с тикетом
curl -s -X POST http://localhost:5291/connect -H "Content-Type: application/json" \
  -d '{"ticket":"<ticket из шага 1>"}'

# 3. сервер отчитывается о матче; повтор с тем же activityId вернёт duplicate=true
curl -s -X POST http://localhost:5291/sessions/<sessionId>/report \
  -H "Content-Type: application/json" -d '{"activityId":"<guid>","durationSeconds":300}'
```
