# GameServer-v2

Монорепозиторий платформы v2: бекенд (несколько сервисов), фронтенд, nginx и
Docker. Создан на основе шаблонного сервиса TemplateService.

## Backend-сервисы

| Сервис            | Статус   | Описание                                                        |
| ----------------- | -------- | --------------------------------------------------------------- |
| `TemplateService` | шаблон   | Пример вертикального среза (CRUD `Item`), основа для копирования |
| `AuthService`     | заглушка | Минимальный скелет без Identity/OpenIddict: `/auth/service-info`, `/health` |

`AuthService` пока не является issuer'ом токенов — это заготовка структуры
(Domain / Contracts / Core / Infrastructure.Postgres / Web) с одним
query-endpoint'ом и пустой схемой `auth` в БД. OIDC/Identity добавятся позже.
Порт разработки — `5280` (`dotnet run --project backend/AuthService/src/AuthService.Web`).

## Состав

```
GameServer-v2/
├── backend/
│   ├── Directory.Build.props        # общие настройки сборки (net10.0, анализаторы)
│   ├── Directory.Packages.props     # центральные версии пакетов
│   ├── nuget.config                 # nuget.org + приватный feed Owlip.*
│   ├── AuthService/                 # заглушка: Domain/Contracts/Core/Infrastructure/Web
│   │   └── AuthService.slnx
│   └── TemplateService/            # шаблонный сервис с примером CRUD
│       ├── TemplateService.slnx
│       ├── Dockerfile               # стадии base / build / migrations / final
│       ├── src/
│       │   ├── TemplateService.Domain/                    # сущности, константы
│       │   ├── TemplateService.Contracts/                 # DTO запросов/ответов
│       │   ├── TemplateService.Core/                      # handlers, validators, permissions
│       │   ├── TemplateService.Infrastructure.Postgres/   # EF Core, репозиторий, миграции
│       │   └── TemplateService.Web/                       # Program, endpoints (IEndpointGroup), конфиги
│       └── tests/
│           └── TemplateService.IntegrationTests/          # Testcontainers + Respawn
├── frontend/                        # Next.js 16 (App Router), React 19, Tailwind v4
├── nginx.conf                       # 6000 — SPA, 6001 — API
├── nginx/certificates/              # self-signed сертификаты для localhost
├── scripts/
│   ├── create-service.sh            # генерация нового сервиса из шаблона
│   └── prepare-nginx-https.*        # перегенерация сертификатов
├── docker-compose.yml
└── .env.example
```

## Стек

| Слой      | Технологии                                                                                       |
| --------- | ------------------------------------------------------------------------------------------------ |
| Backend   | .NET 10, Minimal API, CQRS-handlers (`Shared.Core`), FluentValidation, EF Core + PostgreSQL, Serilog, Scalar |
| Auth      | JWT-валидация токенов AuthService (`Owlip.Shared.Authentication`), permissions по ролям           |
| Frontend  | Next.js 16, React 19, TypeScript, Tailwind CSS v4, TanStack Query, react-hook-form + zod, axios   |
| Infra     | Docker Compose, nginx с HTTPS, отдельный контейнер миграций EF                                    |
| Тесты     | xUnit, `WebApplicationFactory`, Testcontainers.PostgreSql, Respawn                               |

## Быстрый старт (Docker)

```bash
cp .env.example .env
# заполни NUGET_USERNAME / NUGET_PASSWORD (PAT с read:packages)
# и AUTH_AUTHORITY (issuer AuthService)
docker compose up -d --build
```

- UI: https://localhost:6000
- API: https://localhost:6001 (`/health`, в Development — Scalar `/scalar/v1`)
- PostgreSQL: `localhost:5436`

Контейнер `template-migrator` накатывает EF-миграции до старта сервиса.
Сертификаты для nginx можно перегенерировать: `scripts/prepare-nginx-https.sh`.

## Аутентификация

Сервис не выдаёт токены сам — он валидирует JWT, выданные AuthService:

- `Authentication:Authority` — OIDC issuer AuthService;
- `Authentication:Audiences` — audience, который обязан быть в токене
  (по умолчанию `template-service`);
- роли из claim `role` сопоставляются с permissions в
  `TemplateService.Core/Authorization/TemplatePermissions.cs`.

Чтобы токены AuthService подходили, добавь scope/resource `template-service` в
AuthService (по аналогии с `clan-service` в GameServer) **или** укажи
существующий audience:

```bash
# docker-compose.yml / .env
Authentication__Audiences__0=clan-service
```

В UI на странице **Items** есть панель: вставь access-токен — он сохранится в
localStorage и будет подставляться в `Authorization` интерсептором axios.

### Scalar OAuth (dev)

Scalar UI умеет выполнять вход по OAuth2 (`ConfigureOAuth2`). Flow включается,
только если одновременно заданы `Authentication:AuthorizationUrl`,
`Authentication:TokenUrl` и `Authentication:Scalar:ClientId` (или
`Authentication:Audience`). URL уже проставлены в Development/Docker —
остаётся зарегистрировать публичный PKCE-клиент в AuthService и указать его
`ClientId`.

## Конфигурация (options)

Секции appsettings, которые биндятся на options-классы Shared:

| Секция           | Options-класс                                          | Ключи                                                                     |
| ---------------- | ------------------------------------------------------ | ------------------------------------------------------------------------- |
| `Cors`           | `Shared.Framework.Cors.CorsSettings`                   | `AllowedOrigins`, `AllowCredentials`, `AllowedHeaders`, `AllowedMethods`  |
| `Authentication` | `Shared.Authentication...BearerTokenSettings` (JWT)    | `Authority`, `Audiences`                                                  |
| `Authentication` | `Shared.Authentication.AuthenticationOptions` (Scalar) | `AuthorizationUrl`, `TokenUrl`, `Audience`, `Scalar:ClientId/ClientSecret`|
| `ServiceToken`   | `...HttpServerClients.ServiceTokenOptions`             | `TokenUrl`, `ClientId`, `ClientSecret`, `Scopes`                          |

`BearerTokenSettings` по умолчанию читает секцию `Jwt`, но сервис явно передаёт
`sectionName: "Authentication"` — JWT и Scalar живут в одной секции.
`ServiceToken` — заготовка для service-to-service вызовов по client_credentials
(`AddServiceTokenForwarding`): в шаблоне не используется, заполни при
необходимости.

## Локальная разработка

Backend (Postgres поднимаем из compose):

```bash
docker compose up -d postgres
cd backend/TemplateService
dotnet run --project src/TemplateService.Web
# http://localhost:5279, Scalar: http://localhost:5279/scalar/v1
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
  --project src/TemplateService.Infrastructure.Postgres \
  --startup-project src/TemplateService.Web
```

Тесты (нужен запущенный Docker — Testcontainers поднимает Postgres):

```bash
cd backend/TemplateService
dotnet test
```

## API примера

| Метод  | Путь                | Permission                | Описание                                  |
| ------ | ------------------- | ------------------------- | ----------------------------------------- |
| GET    | `/items`            | `items.read`              | Список с поиском и пагинацией             |
| GET    | `/items/{itemId}`   | `items.read`              | Одна запись                               |
| POST   | `/items`            | `items.write`             | Создать запись (owner = текущий user)     |
| PUT    | `/items/{itemId}`   | `items.write`             | Обновить (владелец или `items.manage`)    |
| DELETE | `/items/{itemId}`   | `items.write`             | Удалить (владелец или `items.manage`)     |
| GET    | `/health`           | —                         | Health-check (Postgres)                   |

Роли: `User` — read/write, `Moderator`/`Admin` — плюс `items.manage`,
`Service` — read (service-to-service). Все ответы обёрнуты в общий
`Shared.SharedKernel.Envelope`.

## Организация эндпоинтов

Эндпоинты группируются по ресурсу в класс `IEndpointGroup` (файл
`Endpoints/<Resource>/<Resource>Endpoints.cs`). Группа создаёт `MapGroup` с
общим префиксом, тегом и `RequireAuthorization()`, а конкретные маршруты
уточняют permission, имя и summary:

```csharp
public sealed class ItemsEndpoints : IEndpointGroup
{
    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/items")
            .WithTags("Items")
            .RequireAuthorization();

        group.MapGet("", List)
            .WithName("Items.List")
            .WithSummary("Список записей с поиском и пагинацией")
            .RequirePermissions(TemplatePermissions.ITEMS_READ);

        return group;
    }
}
```

Группы регистрируются автоматически: `app.MapEndpointGroups(assembly)` в
`AppConfigurationExtensions` находит все `IEndpointGroup`. Handler'ы
разрешаются из DI по интерфейсам `ICommandHandler<,>` / `IQueryHandler<,>`
(регистрируются `AddCore`/`AddHandlers`) — эндпоинт не знает конкретный класс.

`AddPlatformAuthentication` ставит fallback-политику «требовать
аутентификацию», поэтому служебные эндпоинты (`/health`, OpenAPI, Scalar)
помечены `.AllowAnonymous()`.

## Новый сервис из шаблона

```bash
./scripts/create-service.sh BillingService ../BillingService
```

Скрипт копирует проект (без `.git`, `node_modules`, `bin`, `obj`) и заменяет
`TemplateService` / `template-service` / `template_service` / `template` в
именах файлов и содержимом. После создания:

1. `cd ../BillingService && cp .env.example .env` — заполнить `NUGET_*`,
   `AUTH_AUTHORITY`, пароли Postgres;
2. зарегистрировать audience `billing-service` в AuthService;
3. `docker compose up -d --build`;
4. заменить пример `Item` на свою доменную сущность:

   | Что                    | Где                                                        |
   | ---------------------- | ---------------------------------------------------------- |
   | Сущность и константы   | `backend/BillingService/src/BillingService.Domain`         |
   | DTO                    | `backend/BillingService/src/BillingService.Contracts`      |
   | Handlers и validators  | `.../BillingService.Core/Features`                         |
   | Permissions и роли     | `.../BillingService.Core/Authorization`                    |
   | EF-конфигурация        | `.../BillingService.Infrastructure.Postgres/Configurations`|
   | Репозиторий            | `.../BillingService.Infrastructure.Postgres`               |
   | Endpoints              | `.../BillingService.Web/Endpoints/<Resource>Endpoints.cs`  |
   | UI-страницы и запросы  | `frontend/src/entities` и `frontend/src/features`          |

## Соглашения

- Каждая фича — папка `Features/<Domain>/{Commands|Queries}/<UseCase>` с
  `Command/Query`, `Handler`, `Validator`.
- Эндпоинты — один `IEndpointGroup` на ресурс: общие префикс/теги/авторизация
  на группе, `.WithName`/`.WithSummary`/`RequirePermissions` — на маршрутах.
- Эндпоинт инжектит `ICommandHandler<,>` / `IQueryHandler<,>` из DI; конкретные
  handler'ы регистрируются `AddCore` (`AddHandlers` + FluentValidation).
- Handler возвращает `Result<T, Error>` из CSharpFunctionalExtensions; ошибки
  конвертируются в HTTP-статусы middleware из `Shared.Framework`.
- Данные читаются через `IReadDbContext` (no-tracking), пишутся через
  репозиторий.
- Валидация — FluentValidation + `GeneralErrors` из `Shared.SharedKernel`.
- Схема БД — своя (`template`), история миграций в `__EFMigrationsHistory`.
