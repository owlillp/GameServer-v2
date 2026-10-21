# GameServer-v2

Монорепозиторий игровой платформы v2: бекенд (AuthService), фронтенд, nginx и
Docker. Единый вход (OIDC) и разделение интерфейса по ролям пользователя.

## Backend-сервисы

| Сервис        | Описание                                                                 |
| ------------- | ------------------------------------------------------------------------ |
| `AuthService` | OIDC-провайдер (OpenIddict + ASP.NET Identity) и JSON API аккаунтов      |
| `ProfileService` | Игровой профиль игрока (ник, регион) и identity-поля из access-токена |
| `WalletService` | Балансы валют и ledger-журнал операций                                 |
| `CatalogService` | Каталог контента: предметы, цены, характеристики, совместимость деталей, неизменяемые версии |
| `InventoryService` | Владение игрока: предметы, аутфит, гараж и тюнинг машин, выдача/отзыв |
| `StoreService` | Витрина офферов, заказы и сага покупки: резерв в Wallet → выдача в Inventory → commit |
| `EventService` | Сезонные ивенты: календарь и фазы, ивент-задания и очки, награды за очки, feature-события для клиента |
| `MatchmakingService` | Пати, простая очередь, длительные матчи 2 пати, результаты, награды и глобальный лидерборд |
| `RealtimeService` | WebSocket/SignalR push для клиентов: `liveops`-события и персональные уведомления |

AuthService выдаёт токены (Authorization Code + PKCE, refresh и
client_credentials) и отдаёт админские данные:

- `/connect/*`, `/.well-known/*` — OIDC endpoints;
- `/auth/register`, `/auth/login` — регистрация и вход (Identity-cookie);
- `/admin/users`, `/admin/stats` — админ-API (permission `users.view`).

ProfileService отдаёт профиль текущего игрока по Bearer-токену:

- `/profiles/me` — объединённый профиль: игровые поля (ник, регион) плюс
  `email`/`userName` из claims access-токена.

WalletService ведёт балансы и журнал операций:

- `/wallets/me`, `/wallets/me/transactions` — балансы и история игрока;
- `/wallets/currencies` — динамический список доступных (активных) валют;
- `/wallets/{id}/credit`, `/wallets/{id}/debit` — внутренние операции
  (service-токен со scope `wallet`);
- `/admin/wallets/{id}/credit`, `/admin/wallets/{id}/debit` — ручные операции
  Admin/Moderator (permission `wallets.manage`, обязательный `comment`);
- `/admin/wallets/{id}`, `/admin/wallets/{id}/transactions` — карточка игрока
  (балансы и история, `wallets.view`);
- `/admin/wallets/currencies` — создание/удаление валют (`wallets.manage`).

CatalogService хранит контент каталога с неизменяемыми версиями (снапшоты в
`jsonb`), кэширует чтения в Redis (`HybridCache`) и отдаёт ETag по номеру версии:

- `/catalog/items`, `/catalog/items/{id}`, `/catalog/version` — опубликованный
  каталог;
- `/catalog/items/{carItemId}/compatible-parts` — совместимые детали машины
  (способности машины + `FitTags` детали);
- `/admin/catalog/items` — CRUD предметов, включая черновики (`catalog.manage`);
- `/admin/catalog/versions` — публикация версии, `/versions/rollback` — откат.

Порт разработки CatalogService — `5281`
(`dotnet run --project backend/CatalogService/src/CatalogService.Web`).

InventoryService ведёт владение игрока и предоставляет админ-методы редактирования:

- `/inventory/me` — инвентарь текущего игрока;
- `/inventory/outfit/{slot}` (PUT/DELETE), `/inventory/cars*` — аутфит и гараж;
- `/inventory/{accountId}/grant`, `/inventory/{accountId}/revoke` — внутренние
  выдача/отзыв (service-токен, идемпотентно по `operationId`);
- `/admin/inventory/{accountId}` — карточка инвентаря (`inventory.view`);
- `/admin/inventory/{accountId}/grant|revoke|outfit|cars...` — редактирование
  инвентаря игрока админом (`inventory.manage`).

Порт разработки InventoryService — `5283`
(`dotnet run --project backend/InventoryService/src/InventoryService.Web`).

StoreService публикует витрину и проводит покупки за игровую валюту:

- `/store/offers`, `/store/offers/{id}` — активные офферы;
- `/store/purchases` — покупка оффера (`Idempotency-Key`, ответ — заказ);
- `/store/orders`, `/store/orders/{id}` — история и статус покупок;
- `/admin/store/offers` — CRUD и активация офферов (`store.view`/`store.manage`).

Покупка идёт сагой: Store резервирует средства в Wallet по gRPC
(`Reserve`), публикует команду выдачи в RabbitMQ, Inventory выдаёт предметы
(для `CAR` автоматически создаётся машина в гараже) и отвечает событием;
Store делает `Commit` и завершает заказ. При ошибке — `Release`, при
таймауте hold освобождается фоновыми sweeper'ами.

gRPC-контракт лежит в `backend/Shared/Grpc` (`Shared.Grpc.Contracts` +
`Shared/Protos/wallet.proto`) и переиспользуется сервером Wallet и клиентом
Store. Вызовы идут по HTTP/2 на внутренний порт `8081`, с service-токеном
(`store-service-client`) в metadata, дедлайном 3 с и одним retry на
`Unavailable`/`DeadlineExceeded`.

HTTP- и gRPC-клиенты других сервисов живут в их contracts-проектах и
переиспользуются по образцу DirectoryService:

- `CatalogService.Contracts/Communication` — `ICatalogClient`, `CatalogHttpClient`,
  `CatalogServiceOptions`, `AddCatalogServiceHttpCommunication`;
- `WalletService.Contracts/Communication` — `IWalletClient`, `WalletGrpcClient`,
  `WalletServiceOptions`, `AddWalletServiceGrpcCommunication`;
- `Shared.Communication` — общий `IServiceTokenProvider` (client_credentials)
  и `ServiceAuthOptions`.

Конфиг Store: `CatalogService:Url` и `WalletService:GrpcAddress`. Для REST-вызовов
в Catalog Store использует контракты `CatalogService.Contracts`, без локальных
копий DTO.

Порт разработки StoreService — `5285`, gRPC Wallet в dev — `5284`
(`dotnet run --project backend/StoreService/src/StoreService.Web`).

EventService ведёт сезонные ивенты и прогресс игрока:

- `/events`, `/events/active`, `/events/{code}` — опубликованные ивенты, текущая
  фаза (`SCHEDULED`/`ANNOUNCED`/`ACTIVE`/`GRACE`/`CLOSED`), задания, награды и
  прогресс игрока;
- `/events/{code}/rewards/{rewardCode}/claim` — обмен ивент-очков на награду
  (`Idempotency-Key`), выдача предметов идёт командой в `InventoryService`;
- `/events/me/claims` — история обменов;
- `/admin/events` — CRUD черновиков, публикация и архивация ивентов
  (`events.view`/`events.manage`), ручная корректировка очков.

Фазы вычисляются из расписания (`AnnounceAt`/`StartsAt`/`EndsAt`/`GraceEndsAt`),
а планировщик Quartz держит триггеры на каждый переход в PostgreSQL (таблицы
`QRTZ_*` в `public`): при старте расписание пересобирается из БД, пропущенные
границы догоняются по `misfire = FireNow`. Job публикует `started.event`/
`ended.event` один раз на переход, дневной cron чистит устаревшие
daily/weekly-прогрессы. Прогресс заданий обновляется по `match-completed.session`
от MatchmakingService/SessionService (дедуп по `(event, account, match)`); очки
начисляются только в фазе `ACTIVE`, обмены доступны в `ACTIVE` и `GRACE`. Если
выдача награды не удалась, Inventory присылает `reward-failed.event`, и
EventService возвращает очки. Порт разработки — `5286`
(`dotnet run --project backend/EventService/src/EventService.Web`). UI: `/events`
для игрока и `/admin/events` для админки.

RealtimeService доставляет клиентам live-уведомления через SignalR (WebSocket) с
Redis backplane:

- `POST /realtime/ticket` — одноразовый тикет для WebSocket (Bearer-токен);
- `/realtime` — SignalR hub: после подключения клиент вызывает `Authenticate(ticket)`
  и автоматически попадает в `liveops` и `player:{accountId}`;
- события: `event.started`, `event.ended`, `event.reward.granted`,
  `wallet.credited`, `wallet.debited`, `inventory.items.granted`.

Gateway ничего не хранит: он потребляет RabbitMQ-события (`event-events`,
`wallet-events`, `inventory-events`) и пересылает их в группы. Wallet-события
маршрутизируются по `WalletId`, который совпадает с `accountId` (`Wallet.Id`).
Клиент дедуплицирует по `Id`, при переподключении заново берёт снапшот
(`/events`, `/wallets/me`, `/inventory/me`). Порт разработки — `5287`
(`dotnet run --project backend/RealtimeService/src/RealtimeService.Web`). Пример
Unity-клиента — `clients/unity/RealtimeClient.cs`.

MatchmakingService собирает пати и проводит длительные матчи:

- `POST /matchmaking/parties`, `POST /matchmaking/parties/join`,
  `POST /matchmaking/parties/leave`, `PUT /matchmaking/parties/ready`,
  `POST /matchmaking/parties/kick/{accountId}`, `GET /matchmaking/parties/me` —
  пати на 2–5 игроков с инвайт-кодом и готовностью;
- `POST /matchmaking/queue`, `DELETE /matchmaking/queue` — простая FIFO-очередь
  (пары пати одного размера и режима);
- `GET /matchmaking/matches/me`, `GET /matchmaking/matches/{id}` — активный
  матч; матч длится дни (`Matchmaking:DefaultMatchDurationHours`);
- `GET /matchmaking/leaderboard`, `GET /matchmaking/leaderboard/me` — глобальный
  лидерборд: истина в PostgreSQL, ускорение ранга через Redis ZSET;
- админ-API `/admin/matchmaking/*` (`matchmaking.view`/`matchmaking.manage`):
  создание матча из двух пати, ввод результатов, завершение и отмена,
  пересборка кэша лидерборда.

Фоновый `MatchmakingCoordinator` каждые `PairingIntervalSeconds` парит
очередь, закрывает матчи по `EndsAt` (с результатами — начисляет награды,
без результатов — отменяет) и ретраит незавершённые награды. При завершении
матча очки идут в лидерборд, валюта — в Wallet (REST + service-токен),
предметы — командой `grant-reward.matchmaking` в Inventory, а
`match-completed.session` уходит в EventService для прогресса ивентов.
Порт разработки — `5288`
(`dotnet run --project backend/MatchmakingService/src/MatchmakingService.Web`).
UI: `/matchmaking` для игрока.

Порт разработки AuthService — `5280`
(`dotnet run --project backend/AuthService/src/AuthService.Web`).

## Состав

```
GameServer-v2/
├── backend/
│   ├── Directory.Build.props        # общие настройки сборки (net10.0, анализаторы)
│   ├── Directory.Packages.props     # центральные версии пакетов
│   ├── nuget.config                 # nuget.org + приватный feed Owlip.*
│   ├── AuthService/
│       ├── AuthService.slnx
│       ├── Dockerfile               # стадии base / build / migrations / final
│       ├── src/
│       │   ├── AuthService.Domain/                    # Account, роли
│       │   ├── AuthService.Contracts/                 # DTO запросов/ответов
│       │   ├── AuthService.Core/                      # handlers, validators, permissions
│       │   ├── AuthService.Infrastructure.Postgres/   # EF Core + Identity, миграции
│       │   └── AuthService.Web/                       # Program, endpoints, OIDC, конфиги
│       └── tests/
│           ├── AuthService.UnitTests/                 # домен, валидаторы
│           └── AuthService.IntegrationTests/          # Testcontainers + OIDC-флоу
│   ├── MatchmakingService/
│   │   ├── MatchmakingService.slnx
│   │   ├── Dockerfile
│   │   └── src/                     # Domain / Contracts / Core / Postgres / Redis / Web
├── frontend/                        # Next.js 16 (App Router), React 19, Tailwind v4
├── nginx.conf                       # 6100 — SPA, 6101 — API (единый origin)
├── nginx/certificates/              # self-signed сертификаты для localhost
├── scripts/
│   └── prepare-nginx-https.*        # перегенерация сертификатов
├── docker-compose.yml
└── .env.example
```

## Стек

| Слой     | Технологии                                                                                       |
| -------- | ------------------------------------------------------------------------------------------------ |
| Backend  | .NET 10, Minimal API, CQRS-handlers (`Shared.Core`), FluentValidation, EF Core + PostgreSQL, Quartz (Events), Wolverine/RabbitMQ, Serilog, Scalar |
| Auth     | OpenIddict (OIDC), ASP.NET Identity, JWT-валидация, permissions по ролям                          |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS v4, TanStack Query, react-hook-form + zod, axios, zustand |
| Infra    | Docker Compose, nginx с HTTPS, отдельный контейнер миграций EF                                    |
| Тесты    | xUnit, `WebApplicationFactory`, Testcontainers.PostgreSql, Respawn                                |

## Быстрый старт (Docker)

```bash
cp .env.example .env
# заполни NUGET_USERNAME / NUGET_PASSWORD (PAT с read:packages)
docker compose up -d --build
```

- UI: https://localhost:6100
- API (OIDC + JSON): https://localhost:6101 (`/health`, в Docker — Scalar `/scalar/v1`)
- PostgreSQL: `localhost:5436`

Контейнер `gameserver-v2-auth-migrator` накатывает EF-миграции до старта
сервиса. Сертификаты для nginx можно перегенерировать:
`scripts/prepare-nginx-https.sh`.

## Аутентификация и роли

Фронтенд ходит только на один origin (`https://localhost:6101`): и OIDC, и
JSON API идут через nginx. Поток входа:

1. `/login` — email и пароль уходят в `POST /auth/login`, AuthService ставит
   Identity-cookie;
2. браузер редиректится на `/connect/authorize` (Authorization Code + PKCE),
   AuthService возвращает код на `/auth/callback`;
3. SPA меняет код на токены в `/connect/token`, кладёт их в zustand-store и
   подставляет access-токен как `Bearer` через интерсептор axios;
4. `/profiles/me` отдаёт профиль игрока (ProfileService), `/admin/*` доступны по
   permission `users.view`.

Роли (`AuthService.Domain.AuthRoles`) и permissions:

| Роль        | Permissions                              | UI                                     |
| ----------- | ---------------------------------------- | -------------------------------------- |
| `User`      | —                                        | личный кабинет, профиль                |
| `Moderator` | `users.view`, `users.manage`, `platform.manage` | админ-панель: статистика, пользователи |
| `Admin`     | все                                      | админ-панель                           |
| `Service`   | —                                        | service-to-service (client_credentials) |

После входа `resolveHomeRoute` уводит персонал в `/admin`, остальных — в
`/dashboard`. Маршруты админки дополнительно закрыты клиентским
`RequireAuth roles={[Admin, Moderator]}`.

Чтобы выдать роль первому администратору:

```sql
INSERT INTO auth.account_roles (account_id, role_id)
SELECT u.id, r.id FROM auth.accounts u, auth.roles r
WHERE u.email = 'admin@example.com' AND r.name = 'Admin';
```

## Конфигурация (options)

Секции appsettings, которые биндятся на options-классы:

| Секция           | Options-класс                                          | Ключи                                                                     |
| ---------------- | ------------------------------------------------------ | ------------------------------------------------------------------------- |
| `Cors`           | `Shared.Framework.Cors.CorsSettings`                   | `AllowedOrigins`, `AllowCredentials`, `AllowedHeaders`, `AllowedMethods`  |
| `Authentication` | `Shared.Authentication...BearerTokenSettings` (JWT)    | `Authority`, `Audiences`                                                  |
| `OpenIddict`     | `AuthService.Core.Configuration.OidcOptions`           | `Issuer`, `AccessTokenLifetimeMinutes`, `RefreshTokenLifetimeDays`, `Clients:*` |
| `AuthService`    | `AuthService.Web.Configurations.Options.AuthServiceOptions` | `FrontendLoginUrl` (куда вести неавторизованный `/connect/authorize`) |

`OpenIddict__Issuer` в Docker — `https://localhost:6101/` (внешний адрес API
через nginx), `AuthService__FrontendLoginUrl` — `https://localhost:6100/login`.

## Локальная разработка

Backend (Postgres поднимаем из compose):

```bash
docker compose up -d postgres
cd backend/AuthService
dotnet run --project src/AuthService.Web
# http://localhost:5280, Scalar: http://localhost:5280/scalar/v1
```

Frontend:

```bash
cd frontend
npm install
npm run dev
# http://localhost:3000, API берётся из frontend/.env.development
```

Миграции:

```bash
dotnet ef migrations add <Name> \
  --project src/AuthService.Infrastructure.Postgres \
  --startup-project src/AuthService.Web
```

Тесты (нужен запущенный Docker — Testcontainers поднимает Postgres):

```bash
cd backend/AuthService
dotnet test
```

## API

| Метод | Путь             | Permission     | Описание                                   |
| ----- | ---------------- | -------------- | ------------------------------------------ |
| POST  | `/auth/register` | —              | Регистрация (роль `User`)                  |
| POST  | `/auth/login`    | —              | Вход, ставит Identity-cookie               |
| GET   | `/profiles/me`   | аутентификация | Профиль текущего игрока (ProfileService)   |
| GET   | `/wallets/me`    | аутентификация | Балансы текущего игрока (WalletService)    |
| GET   | `/wallets/currencies` | аутентификация | Список доступных валют                |
| GET   | `/wallets/me/transactions` | аутентификация | История операций игрока        |
| POST  | `/wallets/{id}/credit` | `wallet` scope | Начисление на кошелёк               |
| POST  | `/wallets/{id}/debit`  | `wallet` scope | Списание с кошелька                 |
| POST  | `/admin/wallets/{id}/credit` | `wallets.manage` | Ручное начисление (Admin/Moderator) |
| POST  | `/admin/wallets/{id}/debit`  | `wallets.manage` | Ручное списание (Admin/Moderator)   |
| GET   | `/admin/wallets/{id}`        | `wallets.view`   | Карточка кошелька игрока            |
| GET   | `/admin/wallets/{id}/transactions` | `wallets.view` | История операций игрока         |
| POST  | `/admin/wallets/currencies`  | `wallets.manage` | Создание валюты                     |
| DELETE | `/admin/wallets/currencies/{code}` | `wallets.manage` | Удаление (деактивация) валюты |
| GET   | `/catalog/items` | аутентификация | Список опубликованных предметов (CatalogService) |
| GET   | `/catalog/items/{id}` | аутентификация | Карточка опубликованного предмета |
| GET   | `/catalog/items/{carId}/compatible-parts` | аутентификация | Совместимые детали машины |
| GET   | `/catalog/version` | аутентификация | Текущая опубликованная версия каталога |
| GET   | `/admin/catalog/items` | `catalog.view` | Список предметов (включая черновики) |
| POST  | `/admin/catalog/items` | `catalog.manage` | Создать предмет |
| PUT   | `/admin/catalog/items/{id}` | `catalog.manage` | Обновить предмет |
| DELETE | `/admin/catalog/items/{id}` | `catalog.manage` | Архивировать предмет |
| POST  | `/admin/catalog/versions` | `catalog.manage` | Опубликовать новую версию каталога |
| POST  | `/admin/catalog/versions/rollback` | `catalog.manage` | Откатить каталог к версии |
| GET   | `/admin/users`   | `users.view`   | Список аккаунтов с поиском и пагинацией    |
| GET   | `/admin/stats`   | `users.view`   | Сводка: всего/админы/модераторы/заблокированы |
| GET   | `/store/offers`  | аутентификация | Активные офферы магазина (StoreService) |
| GET   | `/store/offers/{id}` | аутентификация | Карточка активного оффера |
| POST  | `/store/purchases` | аутентификация | Купить оффер (идемпотентно по `Idempotency-Key`) |
| GET   | `/store/orders`  | аутентификация | История покупок игрока |
| GET   | `/store/orders/{id}` | аутентификация | Статус покупки |
| GET   | `/admin/store/offers` | `store.view` | Все офферы |
| POST  | `/admin/store/offers` | `store.manage` | Создать оффер |
| PUT   | `/admin/store/offers/{id}` | `store.manage` | Обновить оффер |
| POST  | `/admin/store/offers/{id}/activate` | `store.manage` | Активировать оффер |
| DELETE | `/admin/store/offers/{id}` | `store.manage` | Архивировать оффер |
| GET   | `/events`        | аутентификация | Опубликованные ивенты (фильтр по фазе)     |
| GET   | `/events/active` | аутентификация | Ивенты в активной фазе                     |
| GET   | `/events/{code}` | аутентификация | Ивент: задания, награды, прогресс игрока   |
| POST  | `/events/{code}/rewards/{rewardCode}/claim` | аутентификация | Обмен очков на награду |
| GET   | `/events/me/claims` | аутентификация | История обменов наград                  |
| GET   | `/admin/events`  | `events.view`  | Все ивенты, включая черновики              |
| GET   | `/admin/events/{id}` | `events.view` | Ивент по id                             |
| POST  | `/admin/events`  | `events.manage` | Создать черновик ивента                  |
| PUT   | `/admin/events/{id}` | `events.manage` | Обновить черновик ивента              |
| POST  | `/admin/events/{id}/publish` | `events.manage` | Опубликовать ивент            |
| DELETE | `/admin/events/{id}` | `events.manage` | Архивировать ивент                  |
| POST  | `/admin/events/{id}/accounts/{accountId}/points` | `events.manage` | Скорректировать очки |
| POST  | `/matchmaking/parties` | аутентификация | Создать пати |
| POST  | `/matchmaking/parties/join` | аутентификация | Вступить в пати по коду |
| GET   | `/matchmaking/parties/me` | аутентификация | Моя пати, очередь и активный матч |
| POST  | `/matchmaking/queue` | аутентификация | Поставить пати в очередь |
| DELETE | `/matchmaking/queue` | аутентификация | Выйти из очереди |
| GET   | `/matchmaking/matches/me` | аутентификация | Активный матч игрока |
| GET   | `/matchmaking/matches/{id}` | аутентификация | Матч по id (участник) |
| GET   | `/matchmaking/leaderboard` | аутентификация | Топ лидерборда |
| GET   | `/matchmaking/leaderboard/me` | аутентификация | Моё место и очки |
| GET   | `/admin/matchmaking/matches` | `matchmaking.view` | Все матчи |
| GET   | `/admin/matchmaking/queue` | `matchmaking.view` | Очередь пати |
| GET   | `/admin/matchmaking/parties/{id}` | `matchmaking.view` | Пати по id |
| GET   | `/admin/matchmaking/parties/by-code/{code}` | `matchmaking.view` | Пати по коду |
| POST  | `/admin/matchmaking/matches` | `matchmaking.manage` | Создать матч из двух пати |
| PUT   | `/admin/matchmaking/matches/{id}/results` | `matchmaking.manage` | Ввести результаты |
| POST  | `/admin/matchmaking/matches/{id}/finish` | `matchmaking.manage` | Завершить и выдать награды |
| POST  | `/admin/matchmaking/matches/{id}/cancel` | `matchmaking.manage` | Отменить матч |
| POST  | `/admin/matchmaking/leaderboard/rebuild` | `matchmaking.manage` | Пересобрать Redis-лидерборд |
| POST  | `/realtime/ticket` | аутентификация | Одноразовый тикет для SignalR/WebSocket |
| GET   | `/health`        | —              | Health-check (Postgres)                    |

Все ответы обёрнуты в общий `Shared.SharedKernel.Envelope`.

## Организация эндпоинтов

Эндпоинты группируются по ресурсу в класс `IEndpointGroup` (файл
`Endpoints/<Resource>/<Resource>Endpoints.cs`). Группа создаёт `MapGroup` с
общим префиксом, тегом и `RequireAuthorization()`, а конкретные маршруты
уточняют permission, имя и summary:

```csharp
public sealed class AdminEndpoints : IEndpointGroup
{
    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        group.MapGet("/users", ListUsers)
            .WithName("Admin.Users.List")
            .RequirePermissions(AuthPermissions.Users.VIEW);

        return group;
    }
}
```

Группы регистрируются автоматически: `app.MapEndpointGroups(assembly)` в
`AppConfigurationExtensions` находит все `IEndpointGroup`. Handler'ы
разрешаются из DI по интерфейсам `ICommandHandler<,>` / `IQueryHandler<,>`
(регистрируются `AddCore`/`AddHandlers`).

## Новый доменный сервис

1. Создай проект по образцу `backend/AuthService` (Domain / Contracts / Core /
   Infrastructure.Postgres / Web) и добавь сервис и migrator в
   `docker-compose.yml`;
2. создай отдельную БД через `POSTGRES_DB`/init-скрипт и свою схему;
3. добавь nginx `location` с префиксом домена (как `/clans` в GameServer) на
   сервере `6101` и отдельный `upstream`;
4. зарегистрируй scope/resource в `OidcScopes`/`PlatformSyncService` AuthService
   и укажи audience нового сервиса в `Authentication:Audiences`.

## Соглашения

- Каждая фича — папка `Features/<Domain>/{Commands|Queries}/<UseCase>` с
  `Command/Query`, `Handler`, `Validator`.
- Эндпоинты — один `IEndpointGroup` на ресурс: общие префикс/теги/авторизация
  на группе, `.WithName`/`.WithSummary`/`RequirePermissions` — на маршрутах.
- Handler возвращает `Result<T, Error>` из CSharpFunctionalExtensions; ошибки
  конвертируются в HTTP-статусы middleware из `Shared.Framework`.
- Данные читаются через `IReadDbContext` (no-tracking), пишутся через
  репозиторий/менеджеры Identity.
- Валидация — FluentValidation + `GeneralErrors` из `Shared.SharedKernel`.
- Схема БД — своя (`auth`), история миграций в `__EFMigrationsHistory`.
- Фронтенд: `entities` (API/DTO), `features` (model/ui), `shared` (auth, api,
  ui); роли и guard'ы — в `shared/auth`.
