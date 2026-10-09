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

Порт разработки AuthService — `5280`
(`dotnet run --project backend/AuthService/src/AuthService.Web`).

## Состав

```
GameServer-v2/
├── backend/
│   ├── Directory.Build.props        # общие настройки сборки (net10.0, анализаторы)
│   ├── Directory.Packages.props     # центральные версии пакетов
│   ├── nuget.config                 # nuget.org + приватный feed Owlip.*
│   └── AuthService/
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
| Backend  | .NET 10, Minimal API, CQRS-handlers (`Shared.Core`), FluentValidation, EF Core + PostgreSQL, Serilog, Scalar |
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
