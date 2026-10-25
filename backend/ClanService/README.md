# ClanService

Социальная часть кланов: кланы, состав и роли, заявки и приглашения, клановые поинты.
Схема БД — `clans`.

## REST (через nginx / Gateway, JWT игрока, scope `clans`)

| Метод | Путь | Кто | Что делает |
| --- | --- | --- | --- |
| `POST` | `/clans` | игрок без клана | Создать клан `{ tag, name, description }`, игрок становится лидером |
| `GET` | `/clans?query=` | все | Поиск по префиксу тега или части имени |
| `GET` | `/clans/me` | участник | Клан, моя роль, состав (404 — не в клане) |
| `POST` | `/clans/me/leave` | участник | Выйти; лидер должен сначала передать лидерство, лидер в одиночку — распускает клан |
| `GET` | `/clans/{id}` | все | Клан и состав |
| `PUT` | `/clans/{id}` | офицер+ | Имя и описание |
| `DELETE` | `/clans/{id}` | лидер | Распустить |
| `POST` | `/clans/{id}/requests` | игрок без клана | Заявка на вступление |
| `GET` | `/clans/{id}/requests` | офицер+ | Ожидающие заявки и приглашения |
| `POST` | `/clans/{id}/invites` | офицер+ | Пригласить `{ accountId }` |
| `GET` | `/clans/requests/me` | игрок | Мои приглашения и заявки |
| `POST` | `/clans/requests/{id}/accept` | решающая сторона | Приглашение — приглашённый, заявку — офицер+ |
| `POST` | `/clans/requests/{id}/decline` | решающая сторона / автор | Отклонить; автор заявки может её отозвать |
| `PUT` | `/clans/{id}/members/{accountId}/role` | лидер | `{ role: "Officer" \| "Member" }` |
| `DELETE` | `/clans/{id}/members/{accountId}` | старшая роль | Исключить |
| `POST` | `/clans/{id}/transfer-leadership` | лидер | `{ accountId }`, прежний лидер становится офицером |
| `GET` | `/admin/clans?query=` | `clans.view` | Поиск, включая распущенные |
| `GET` | `/admin/clans/{id}` | `clans.view` | Клан и состав |
| `GET` | `/admin/clans/{id}/points` | `clans.view` | Ledger поинтов |
| `POST` | `/admin/clans/{id}/points` | `clans.manage` | Корректировка `{ amount, reason, comment, operationId }`; в ledger пишется, кто и почему |

## Правила

- Один клан на игрока — первичный ключ `clan_members.account_id`.
- Лимит участников (`Clans:MaxMembers`) — атомарный
  `UPDATE clans SET member_count = member_count + 1 WHERE member_count < @max`.
- Кулдаун после добровольного выхода — `Clans:JoinCooldownHours` (по умолчанию 24 ч);
  исключение и роспуск кулдаун не запускают.
- Права по ролям — `ClanPolicy` в домене: Лидер > Офицер > Участник, действовать можно
  только на младшую роль, роль «Лидер» меняется только передачей.
- Поинты меняются только через `clan_points_ledger` (уникальный `operation_id`) и
  `clan_points = clan_points + @amount` в одной транзакции.
- Гонки решают ограничения БД: уникальный тег, первичный ключ участника, одна ожидающая
  заявка на пару клан–игрок, row version (`xmin`) у кланов, участников и заявок.
- Создание может стоить валюту (`Clans:CreationFee`, по умолчанию 0): резерв в Wallet →
  создание → commit; при ошибке создания резерв снимается, неудачный commit повторяет
  `ClanFeeSweeper`.

## События (exchange `clan-events`)

| Routing key | Контракт |
| --- | --- |
| `clan-created.clan` | `ClanCreatedEvent` |
| `clan-disbanded.clan` | `ClanDisbandedEvent` (с составом на момент роспуска) |
| `member-joined.clan` | `ClanMemberJoinedEvent` |
| `member-left.clan` | `ClanMemberLeftEvent` (`left` / `kicked`) |
| `member-role-changed.clan` | `ClanMemberRoleChangedEvent` |

Слушает `clan-points-granted.clanwars` (`ClanPointsGrantedEvent`) — начисление поинтов, идемпотентно.

## Realtime

RealtimeService строит проекцию «игрок → клан» из этих событий и держит группу
`clan:{id}`: при `Authenticate` соединение добавляется в группу клана, при вступлении/выходе
— все соединения игрока добавляются/удаляются. Участникам приходят `clan.member.joined`,
`clan.member.left`, `clan.member.role_changed`, `clan.disbanded`.

## Конфигурация

`ConnectionStrings:ClanDb`, `RabbitMQ`; `Authentication:Audiences = ["clan-service"]`;
`ServiceAuth` (клиент `clan-service-client`, scope `wallet`); `WalletService:GrpcAddress`;
секция `Clans`. Порт в dev — `5292`. Health: `/health/live`, `/health/ready`.
