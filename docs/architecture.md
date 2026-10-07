# GameServer-v2 — архитектура

> Статус: черновик плана. Масштаб: 50k+ одновременных игроков.
> Стек: .NET 10, PostgreSQL, RabbitMQ + Kafka, Redis, nginx → YARP.
> Базис: AuthService уже реализован (OIDC + Identity), остальные сервисы строятся
> по его слоям: `Domain / Contracts / Core / Infrastructure.Postgres / Web`.

## 1. Цели и принципы

Функциональные домены (из требований):

1. авторизация (есть);
2. профиль игрока;
3. battle pass (сезоны, уровни, награды, задания);
4. сезонные ивенты (летний, новогодний и т.п.);
5. кланы: версус между N кланами, клановые сезоны с выплатой наград, клановые
   награды и лидерборды за сезон;
6. инвентарь и кастомизация: одежда, оружие, машины, тюнинг машин деталями;
7. покупки за игровую валюту и за реальные деньги (донат).

Нефункциональные требования:

| Параметр | Цель |
| --- | --- |
| Одновременный онлайн | 50k+ CCU, пики x3 в дни контентных релизов |
| Сезонные финалы | «шторм» записи очков в последние часы сезона |
| p99 API | ≤ 300 ms для клиентских чтений, ≤ 1 s для записи |
| Доступность | 99.9%, деградация по сервисам, а не отказ платформы |
| Экономика | аудит каждой операции, нулевая потеря/дублирование валюты |
| События сезонов | выплаты идемпотентны и воспроизводимы из архива |

Принципы:

- **DDD-границы.** Сервис = bounded context, а не «слой» и не таблица.
  Отдельный сервис оправдан своим жизненным циклом данных, нагрузкой или
  командой; иначе это модуль внутри сервиса (см. §12).
- **Database per service.** Никаких общих таблиц, JOIN'ов и FK между
  сервисами. Ссылки на чужие id — без FK.
- **Async-first.** Межсервисные изменения выполняются событиями/командами;
  синхронные вызовы — только для чтений и «горячих» транзакционных шагов.
- **Идемпотентность везде.** Клиентские POST — `Idempotency-Key`, консьюмеры —
  dedup по `EventId` (inbox), операции кошелька — по `operation_id`.
- **Транзакционный outbox** в каждом сервисе: сначала пишем БД, потом
  публикуем, at-least-once + идемпотентные консьюмеры.
- **Деньги — только ledger.** Double-entry, целочисленные минорные единицы,
  append-only, баланс — материализация из проводок.

## 2. Карта сервисов

| # | Сервис | Зона ответственности | Владеет данными (БД/схема) | Фаза |
| --- | --- | --- | --- | --- |
| 1 | **AuthService** *(есть)* | Аккаунты, пароли, роли/permissions, OIDC, service-токены | `auth` | 0 |
| 2 | **PlayerService** | Профиль (ник, аватар, регион), уровень/XP, статистика, флаги (бан, право на лидерборд) | `players` | 1 |
| 3 | **CatalogService** | Контент: одежда, оружие, машины, детали, их характеристики, цены, совместимость деталей, окна доступности. Админ-версионирование | `catalog` + Redis-кэш | 1 |
| 4 | **InventoryService** | Владение: купленные предметы, надетый аутфит, арсенал, гараж, конфиги тюнинга машин | `inventory` | 1 |
| 5 | **WalletService** | Балансы (soft/hard/event-валюты), holds/reserves, double-entry ledger, история | `wallet` | 1 |
| 6 | **StoreService** | Витрина офферов/скидок, заказы, сага покупки, чеки. Оркестрация покупок за обе валюты | `store` | 1 |
| 7 | **PaymentService** | Реальные деньги: PSP-интеграция, checkout, вебхуки, возвраты/чарджбеки, идемпотентность | `payments` | 3 |
| 8 | **BattlePassService** | BP-сезоны, уровни/награды (free/premium), прогресс, задания (ежедневные/недельные/сезонные), клеймы | `battlepass` | 2 |
| 9 | **EventsService** | Сезонные ивенты: календарь и фазы, ивент-задания/очки, ивент-награды, feature flags для клиента | `events` | 2 |
| 10 | **ClanService** | Клан как соц. единица: создание, состав, роли (лидер/офицер/участник), заявки/инвайты, уровень и перки клана | `clans` + Redis | 2 |
| 11 | **ClanWarService** | Войны N кланов, очки (authoritative ledger), клан-сезоны, финальные standings, выплата наград | `clanwars` | 2 |
| 12 | **SessionService** | Приём результатов матчей от игровых серверов: валидация, дедуп, античит-эвристики, канонический `MatchCompleted` | `sessions` | 1 |
| 13 | **LeaderboardService** | Read-model лидербордов (игроки/кланы/сезоны) на Redis ZSET, «топ-N» и «моё место». Ребилд из Kafka | Redis + снапшоты в S3/PG | 2 |
| 14 | **PlayerHub BFF** *(опц.)* | Агрегация экрана входа: профиль + кошелёк + BP + клан + активные ивенты одним вызовом, кэш в Redis | — | 3 |
| 15 | **Notification/Mail** *(опц.)* | Внутриигровая почта (выдача наград/компенсаций), push/e-mail, шаблоны, локализация | `notifications` | 3 |

Прагматика: на старте `Wallet`+`Store` можно запустить одним сервисом
`CommerceService` и разделить по мере роста нагрузки/команды. `BattlePass` и
`Events` могут делить общий движок сезонов, но модели у них разные — это осознанная
развилка.

### Почему именно такие границы

- `ClanService` (социальный граф) отделён от `ClanWarService` (соревновательный
  цикл): у войн свой календарь, нагрузка и денежные выплаты; состав клана меняется
  независимо от сезона.
- `WalletService` отделён от `StoreService`: ledger — критичный инвариант
  (идемпотентность, аудит), витрина и саги меняются часто.
- `SessionService` — единственная точка контакта игровых серверов и ворота
  античита: всё остальное потребляет уже очищенный `MatchCompleted`.
- `LeaderboardService` — производная read-model: истина по очкам и standings
  живёт в `ClanWarService`/`PlayerService`, сортированные множества можно
  пересобрать реплеем Kafka.

## 3. Схема связей

```mermaid
flowchart LR
    subgraph CLIENTS["Клиенты"]
        GC["Игровой клиент"]
        WEB["Web UI / админка"]
        GS["Игровые серверы"]
    end

    subgraph EDGE["Edge"]
        GW["nginx / YARP Gateway<br/>TLS, rate limit, JWT"]
        BFF["PlayerHub BFF<br/>(агрегация, опц.)"]
    end

    subgraph CORE["Доменные сервисы"]
        AUTH["AuthService"]
        PLAYER["PlayerService"]
        CATALOG["CatalogService"]
        INV["InventoryService"]
        WALLET["WalletService"]
        STORE["StoreService"]
        PAY["PaymentService"]
        BP["BattlePassService"]
        EVENTS["EventsService"]
        CLAN["ClanService"]
        WAR["ClanWarService"]
        SESSION["SessionService"]
        LB["LeaderboardService"]
    end

    subgraph INFRA["Инфраструктура"]
        RMQ[("RabbitMQ")]
        KAFKA[("Kafka")]
        REDIS[("Redis")]
        PG[("PostgreSQL — БД на сервис")]
    end

    GC --> GW
    WEB --> GW
    GS -->|"gRPC: отчёт о матче (service token)"| SESSION
    GW --> BFF
    GW --> AUTH
    GW --> PLAYER
    GW --> CATALOG
    GW --> INV
    GW --> STORE
    GW --> BP
    GW --> EVENTS
    GW --> CLAN
    GW --> WAR
    GW --> LB
    BFF -.-> PLAYER
    BFF -.-> WALLET
    BFF -.-> INV
    BFF -.-> BP
    BFF -.-> EVENTS
    BFF -.-> CLAN
    CORE -.->|"команды и доменные события"| RMQ
    RMQ <-->|"outbox → CDC / bridge"| KAFKA
    KAFKA -.->|"read-модели, аналитика, реплей"| LB
    LB --- REDIS
    CORE --- PG
```

## 4. Коммуникации

### 4.1 Синхронные вызовы

| Вызов | Протокол | Зачем |
| --- | --- | --- |
| Клиент → Gateway → сервисы | HTTPS/REST | клиентские API |
| Игровой сервер → SessionService | gRPC (stream) + service token | отчёты о матчах, высокий RPS |
| Store → Wallet | gRPC | Reserve / Commit / Release — критичный путь покупки |
| Store → Catalog | REST (кэш Redis) | снапшот цены и состава оффера |
| BFF → сервисы | REST/gRPC параллельно | агрегация экрана входа |
| ClanWar → Player/Clan | REST (batch) | имена/теги для снапшотов (не хот-пас) |

Правила синхронного вызова: таймаут 1–2 s; retry только для идемпотентных
операций (GET, Reserve/Commit по operation_id); circuit breaker и bulkhead на
каждую зависимость (Polly / `Microsoft.Extensions.Http.Resilience`).

### 4.2 Асинхронные шины — разделение ролей

| Шина | Роль | Что идёт |
| --- | --- | --- |
| **RabbitMQ (MassTransit)** | Управление и координация | команды между сервисами, доменные события-триггеры, саги (state machine), retry/DLQ |
| **Kafka** | Журнал событий | `MatchCompleted` (высокий RPS), обновления standings, read-модели, аналитика, реплей, аудит |

Мост из RabbitMQ в Kafka не «двойная публикация», а **outbox + CDC**: сервис
пишет outbox-таблицу в своей БД; MassTransit EF-outbox публикует в RabbitMQ,
Debezium читает WAL и льёт те же события в Kafka-топики. Консьюмеры
идемпотентны по `EventId`, поэтому at-least-once на обеих шинах безопасен.

Kafka-топики ключуются по `player_id` / `clan_id` — порядок событий по
«сущности» гарантирован. Имена версионируются: `game.match.completed.v1`.
На MVP можно начать только с RabbitMQ и включить Kafka-поток на фазе 2, не меняя
контракты событий.

### 4.3 Конверт события

```json
{
  "event_id": "uuid",
  "event_type": "game.match.completed",
  "event_version": 1,
  "occurred_at": "2026-10-03T12:00:00Z",
  "producer": "SessionService",
  "correlation_id": "trace-id",
  "causation_id": "event-id-предка",
  "payload": { }
}
```

### 4.4 Каталог событий (основные)

| Событие | Продюсер | Шина | Консьюмеры |
| --- | --- | --- | --- |
| `identity.account.registered` | Auth | Rabbit | Player (провижининг профиля) |
| `identity.account.blocked` | Auth | Rabbit | Player, Clan, Leaderboard (исключение) |
| `game.match.completed` | Session | Rabbit + Kafka | Player, BattlePass, Events, ClanWar, Leaderboard, аналитика |
| `economy.purchase.completed` | Store | Rabbit | Inventory, BattlePass, Events, аналитика |
| `economy.wallet.credited/debited` | Wallet | Kafka | аналитика, антифрод |
| `payment.succeeded/failed/refunded` | Payment | Rabbit | Store, Wallet, аналитика |
| `inventory.items.granted` | Inventory | Kafka | аналитика, PlayerHub-кэш |
| `battlepass.tier.reached` | BattlePass | Rabbit | Inventory/Wallet (выдача), Gateway (пуш) |
| `liveops.event.started/ended` | Events | Rabbit | Gateway (флаги), Store, аналитика |
| `clan.war.points.updated` | ClanWar | Kafka | Leaderboard, аналитика |
| `clan.season.locked/payout.completed` | ClanWar | Rabbit + Kafka | Inventory/Wallet/Mail, Leaderboard, аналитика |

### 4.5 Переход на gRPC (план)

Принцип: **gRPC — только для внутренних service-to-service вызовов**; REST/JSON
остаётся для клиента, админки и внешних интеграций. Оба транспорта — тонкие
адаптеры над одними и теми же handler'ами (`Core`), логика, валидация и
идемпотентность не дублируются. Доменные `Error` конвертируются единым маппером
(HTTP-статус для REST / gRPC-статус для gRPC).

| Вызов | Сейчас | Целевой транспорт | Фаза |
| --- | --- | --- | --- |
| Store → Wallet `Reserve` / `Commit` / `Release` | REST, service-token | gRPC unary | 1 (Commerce) |
| Игровой сервер → SessionService (отчёты о матчах) | — | gRPC (server/bidi streaming) | 2 |
| BFF → сервисы (агрегация экрана входа) | — | gRPC unary (параллельно) | 3 |

Правила внедрения:

- контракт — отдельный проект/пакет (`.proto`), версионируется; номера полей не
  переиспользуются, старые методы не ломаются;
- аутентификация — тот же service-токен (`client_credentials`, scope на
  операцию) в gRPC-**metadata** (`authorization: Bearer …`);
- deadlines/таймауты 1–2 s; retry только для идемпотентных операций
  (`Reserve`/`Commit`/`Release` по `operation_id`); circuit breaker на каждую
  зависимость;
- gRPC **не выставляется в интернет** — только внутри compose/service mesh;
  наружу — REST, BFF или grpc-web; отдельный ingress-путь через nginx не
  создаётся;
- добавляется параллельно REST: существующие REST-эндпоинты не удаляются.

### 4.6 Realtime-оповещения клиентов (SignalR) — будущее

Решение: доставка серверных событий клиенту (старт/конец сезона, изменение
очков клана, личные награды/сообщения) — через **SignalR** (WebSocket с
автоматическим откатом на SSE / long polling). Транспорт выбран «на вырост»:
единый канал для браузера и Unity-клиента, с возможностью добавить
двусторонность (чат, если появится).

Поток:

```
бизнес-сервисы ── domain event ──► RabbitMQ/Kafka
                                        │
                                        ▼
                        RealtimeGateway (SignalR Hub)
                                        │  Redis backplane
                                        ▼
                        соединения клиентов
```

- Бизнес-сервисы **не держат клиентские соединения** — они публикуют события,
  рассылкой занимается отдельный **RealtimeGateway** (кандидат — расширение
  `Notification/Mail`).
- **Группы/каналы**: `player:{id}`, `clan:{id}`, `liveops` (и `chat:{room}` —
  если появится ChatService). Подписка авторизуется по OIDC-токену и правам на
  группу.
- **Масштабирование**: несколько инстансов gateway, событие раздаётся всем через
  **Redis backplane** (Redis уже в compose). Порядок по сущности — ключ
  Kafka/Rabbit по `player_id`/`clan_id`; дедуп на клиенте по `event_id`;
  высокочастотные события (очки клана) — coalesce/throttle.
- **Клиент**: паттерн «REST-снимок + подписка на дельты» — сначала `GET`
  текущего состояния, затем `subscribe` на группу (иначе события между
  загрузкой и подпиской теряются).
- **Инфраструктура**: nginx `location` для hub с заголовками `Upgrade`/
  `Connection`; для SSE-fallback отключить буферизацию. Токен — через SignalR
  `accessTokenFactory` (короткоживущий тикет), не в query-строке.

## 5. Данные

### 5.1 Владение

| Сервис | Ключевые сущности |
| --- | --- |
| Auth | Account, Credential, Role, Permission, Client, Token |
| Player | Player, PlayerStats, NicknameHistory, FeatureFlags |
| Catalog | Item, ItemStats, CompatRule, Price, AvailabilityWindow, CatalogVersion |
| Inventory | OwnedItem, Outfit, Loadout, Car, CarInstallation, GrantReceipt (inbox) |
| Wallet | Wallet, Balance, LedgerEntry, Hold, Operation |
| Store | Offer, Order, OrderItem, PurchaseSaga, Receipt |
| Payment | PaymentOrder, PspTransaction, WebhookEvent, Refund, Chargeback |
| BattlePass | BpSeason, BpTier, BpProgress, BpMission, BpMissionProgress, BpClaim |
| Events | Event, EventPhase, EventQuest, EventProgress, EventRewardGrant |
| Clan | Clan, ClanMember, ClanInvite, ClanJoinRequest, ClanLevel |
| ClanWar | WarSeason, War, WarParticipant, WarRound, WarPointEntry, SeasonStanding, RewardPayout |
| Session | GameSession, SessionParticipant, RawReport, Dedup |
| Leaderboard | Redis ZSET + периодические снапшоты (PG/S3) |

### 5.2 Консистентность и саги

- Локальные транзакции + outbox в одной БД-транзакции.
- Кросс-сервисные транзакции — только через саги с компенсациями
  (MassTransit state machine). Таймауты саг: hold живёт ≤ 5 минут.
- `player_id = account_id` (claim `sub`) — единая идентичность во всех сервисах,
  без маппинг-сервиса.
- Реферальные/статистические данные не шарятся, а проецируются событиями.
- Reconciliation-джобы: «orders ↔ wallet ledger», «grants ↔ purchases»,
  «season payouts ↔ reward receipts» — ежедневно.

### 5.3 Кэш и масштабирование данных

- Redis: сессии/лимиты, Catalog snapshot, лидерборды ZSET, BFF-кэш.
- PostgreSQL: одна managed-инсталляция с логической БД на сервис на старте;
  при росте — физическое разделение, затем шардирование по `player_id`.
- Чтения — из реплик; запись — только в primary своего сервиса.
- Catalog отдаётся клиенту через CDN/ETag, версии неизменяемы.

## 6. Ключевые сценарии

### 6.1 Покупка за игровую валюту (оружие, машина, деталь, одежда)

```mermaid
sequenceDiagram
    autonumber
    participant C as Клиент
    participant S as StoreService
    participant K as CatalogService
    participant W as WalletService
    participant I as InventoryService

    C->>S: POST /store/purchases (offer_id, Idempotency-Key)
    S->>K: снапшот оффера (цена, состав) из кэша
    S->>S: Order(Pending)
    S->>W: Reserve(order_id, currency, amount) [gRPC]
    W-->>S: hold_id
    S->>I: GrantItems(order_id, items) [Rabbit-команда]
    I->>I: проверка совместимости/лимитов, выдача, inbox-dedup
    I-->>S: ItemsGranted
    S->>W: Commit(hold_id) [gRPC]
    S->>S: Order(Completed)
    S-->>C: 200 + предметы/баланс
    Note over S,I: ошибка выдачи → W.Release(hold_id), Order(Failed)
    Note over S,W: таймаут саги → Release по operation_id, компенсация
```

Для машины «покупка» создаёт `Car`, детали покупаются отдельными офферами и
ставятся через Inventory (проверка совместимости по Catalog).

### 6.2 Донат (реальные деньги)

```mermaid
sequenceDiagram
    autonumber
    participant C as Клиент
    participant S as StoreService
    participant P as PaymentService
    participant PSP as Платёжный провайдер
    participant W as WalletService
    participant I as InventoryService

    C->>S: POST /store/donations (bundle_id)
    S->>P: CreatePayment(order_id, amount, currency)
    P->>PSP: создать checkout
    PSP-->>C: платёжная страница
    C->>PSP: оплата
    PSP->>P: webhook (проверка подписи)
    P->>P: идемпотентная запись (PSP event id)
    P->>S: PaymentSucceeded [Rabbit]
    S->>W: Credit(hard currency) [gRPC]
    S->>I: GrantItems(bundle) [Rabbit]
    S->>S: Order(Completed)
    Note over P: PaymentRefunded/Chargeback → отрицательная проводка,<br/>политика отзыва неиспользованных предметов, флаг аккаунта
```

### 6.3 Прогресс Battle Pass от матча

```mermaid
sequenceDiagram
    autonumber
    participant GS as Игровой сервер
    participant SS as SessionService
    participant BP as BattlePassService
    participant I as InventoryService
    participant LB as LeaderboardService

    GS->>SS: отчёт о матче (gRPC, service token)
    SS->>SS: валидация, дедуп, запись, античит-эвристики
    SS->>BP: game.match.completed [Rabbit]
    SS->>LB: game.match.completed [Kafka]
    BP->>BP: XP по правилам режима, дедуп по match_id
    BP->>BP: tier up → автоклейм или ручной клейм
    BP->>I: GrantReward(tier, track) [Rabbit-команда]
    I-->>BP: reward granted
```

Premium-трек активируется покупкой (`PurchaseCompleted`) в том же сезонном окне.

### 6.4 Жизненный цикл сезонного ивента

```mermaid
stateDiagram-v2
    [*] --> Scheduled: админ создал ивент
    Scheduled --> Announced: анонс в клиенте
    Announced --> Active: окно активности
    Active --> Grace: окно закрыто, прогресс заморожен
    Grace --> Closed: выдача/архив, выключение флагов
    Closed --> [*]
```

Правила: ивент-очки начисляются только в окне `Active` (дедуп по `match_id`);
смена валюты/множителей — через версию конфигурации, а не правку активного
ивента; закрытие идемпотентно (batch id на ивент).

### 6.5 Клан-сезон: войны N кланов и выплаты

```mermaid
stateDiagram-v2
    [*] --> Registration: старт сезона, набор кланов
    Registration --> Active: дивизионы зафиксированы (ростер-lock)
    Active --> Active: раунды войн N кланов, очки
    Active --> Locked: конец сезона (scheduler + distributed lock)
    Locked --> Payout: расчёт standings и наград (идемпотентный batch)
    Payout --> Archived: архив, снапшот лидерборда
    Archived --> [*]
```

Начисление очков за матч:

```mermaid
sequenceDiagram
    autonumber
    participant SS as SessionService
    participant CW as ClanWarService
    participant DB as Postgres (clanwars)
    participant K as Kafka
    participant LB as LeaderboardService

    SS->>CW: game.match.completed (участники + их кланы) [Rabbit]
    CW->>CW: война активна? оба клана — участники? ростер-lock? лимиты/анти-фрод?
    CW->>DB: WarPointEntry (идемпотентно: match_id + clan_id)
    CW->>DB: пересчёт SeasonStanding
    CW->>K: clan.war.points.updated
    K->>LB: обновление Redis ZSET сезонной таблицы
```

Выплата в конце сезона:

1. scheduler берёт distributed lock, переводит сезон в `Locked` — новые очки не
   принимаются, standings заморожены;
2. формируется `RewardPayout` на каждый клан: награда клану (казна/перки/эмблема)
   по занятому месту + награды участникам по вкладу (доля от очков сезона,
   зафиксированный ростер);
3. выплата идёт идемпотентными командами `GrantItems`/`Credit`/`Mail`
   (по `payout_id`), частичные сбои ретраятся до `payout.completed`;
4. архив сезона + снапшот лидерборда в S3; только после этого открывается
   регистрация следующего сезона.

Продуктовые параметры (в конфиге сезона): N кланов в войне, длительность раунда
и сезона, таблицы наград, веса вклада, политика выхода из клана во время сезона.

## 7. Доменные модели (кратко)

### 7.1 Инвентарь и кастомизация

- `OwnedItem(owner_id, item_id, kind, source, acquired_at)` — источник:
  `purchase | bp_reward | event_reward | clan_season_reward | admin`.
- `Outfit(owner_id, slot, item_id)` — слоты одежды (голова/торс/ноги/обувь/
  аксессуары); экипировка — идемпотентная замена слота.
- `Weapon(owner_id, item_id, skin_id?)`, `Loadout(owner_id, slot, weapon_id)`.
- `Car(owner_id, car_item_id, color, plate, mileage?)`.
- `CarInstallation(car_id, slot, part_item_id)` — слоты тюнинга (двигатель,
  турбина, подвеска, тормоза, шины, нитро, кузов). Перформанс машины —
  вычисляемая проекция из базы + установленных деталей (кэшируется).
- Совместимость деталей — правило каталога (`CompatRule`); Inventory проверяет
  её по снапшоту каталога, Catalog остаётся источником истины.

### 7.2 Battle Pass

`BpSeason(starts_at, ends_at, max_tier, premium_price)` → `BpTier(tier,
xp_required, free_rewards[], premium_rewards[])`; `BpProgress(player, season)`;
`BpMission(kind: daily|weekly|season, xp, conditions)`; `BpClaim(tier, track)`.
XP-правила — конфиг сезона (режим, место, киллы, задания). Клеймы
идемпотентны; неполученные награды закрытого сезона — политика «сгорают» или
авто-выдача через Mail (продуктовое решение, по умолчанию авто-выдача).

### 7.3 События

`Event(code, kind, phases[], currency_code, content_version)` →
`EventPhase(announce|active|grace)`; `EventQuest` + `EventProgress(player)`;
`EventRewardGrant`. Ивент-валюта — отдельный код валюты в Wallet ledger, не
отдельный сервис. Магазин ивента — офферы Store с окном доступности.

### 7.4 Кланы

`Clan(tag, name, emblem, level, xp, perks, is_open)`; `ClanMember(role:
leader|officer|member, joined_at, contribution)`; `ClanInvite`, `ClanJoinRequest`.
Лимит состава и перки зависят от уровня клана. История состава важна для
сезонных выплат (ростер-lock на момент старта войны).

### 7.5 Clan Wars

`WarSeason(starts_at, ends_at, reward_table, roster_lock_at)`;
`War(season_id, division, starts_at, ends_at, status)`;
`WarParticipant(war_id, clan_id, points)` — N кланов; `WarRound` — временные окна;
`WarPointEntry(war_id, clan_id, match_id, points, reason)` — append-only,
идемпотентно по `(match_id, clan_id)`; `SeasonStanding(season_id, clan_id,
rank, points)`; `RewardPayout(payout_id, season_id, clan_id, status)`.

Анти-абьюз: очки только из валидированных матчей, дневные капы,
клан-свитч-кулдаун, сравнение аномалий (вклад одного игрока > X% очков войны).

## 8. Безопасность и авторизация

| Актёр | Токен | Права |
| --- | --- | --- |
| Игрок | OIDC user token (Authorization Code + PKCE) | свои данные: профиль, инвентарь, покупки, клеймы |
| Игровой сервер | client_credentials, scope `session.write` | только отчёты о матчах |
| Сервис | client_credentials, scope на операцию (`wallet.write`, `inventory.write`, `catalog.read`, `clan.read`, …) | внутренние вызовы |
| Модератор/админ | user token + permissions | `users.manage`, `catalog.manage`, `liveops.manage`, `clanwars.manage`, `economy.audit` |

- Gateway валидирует JWT, лимитирует по IP/аккаунту, отсекает всё кроме
  известных префиксов; внутри — mTLS (service mesh) или подписанные токены.
- PSP-вебхуки: проверка подписи, идемпотентность по событию провайдера, отдельный
  ingress-путь без пользовательской авторизации.
- Аудит админских и экономических операций — обязательный, в отдельную таблицу
  с актором и корреляцией.

## 9. Отказоустойчивость

- Retry с экспоненциальным backoff и jitter; только идемпотентные операции.
- Circuit breaker: Store→Wallet, Inventory→Catalog, BFF→все; bulkhead на
  каждую зависимость.
- MassTransit: retry-политики, `_error`-очереди, инструмент повторной обработки,
  saga timeouts.
- Kafka: at-least-once, консьюмер-группы, DLQ-топики; реплей для ребилда
  read-моделей.
- Деградация: если Wallet недоступен — покупки ставятся в очередь с
  предварительной проверкой по кэшу; чтения профиля/каталога продолжают работать.
- Плановые maintenance-окна для сезонных выплат исключены: выплата — фоновая
  операция с идемпотентными батчами и чекпойнтами.

## 10. Наблюдаемость

- OpenTelemetry: trace через HTTP/gRPC/RabbitMQ/Kafka (`traceparent` в конверте
  события), экспорт в Tempo/Jaeger.
- Логи: Serilog → Loki, correlation/causation id в каждой записи.
- Метрики (Prometheus/Grafana): бизнес — покупки/мин, конверсия доната, лаг
  ingest матчей, длина очередей, успех выплат; технические — p50/p95/p99,
  ошибки по зависимостям, DLQ depth.
- Алерты: DLQ > 0 в критичных очередях, провал сезонного scheduler'а, всплеск
  webhook-ошибок PSP, расхождение reconciliation-джоб.

## 11. Деплой и масштабирование

- Dev: docker-compose (Postgres, RabbitMQ, Redis; Kafka — опционально).
- Prod (50k+): Kubernetes, HPA по CPU/RPS и длине очередей, PodDisruptionBudget,
  managed PostgreSQL (реплики для чтения), Kafka 3+ брокера, RabbitMQ quorum
  queues, Redis cluster/sentinel, CDN для каталога, S3 для снапшотов.
- Выкат: blue-green/canary, миграции EF отдельным контейнером (как сейчас),
  обратно-совместимые контракты событий (версия в топике/типе).
- Gateway: nginx остаётся на входе; при росте — YARP-шлюз с валидацией JWT,
  rate limit и BFF-маршрутизацией, чтобы не разрастался nginx.conf.

## 12. Этапы внедрения

| Фаза | Что делаем | Критерий готовности |
| --- | --- | --- |
| 0. Фундамент | Shared.Messaging (outbox/inbox, MassTransit), конверт событий, шаблон сервиса, Redis/RabbitMQ в compose, observability | новый сервис поднимается из шаблона, событие доходит end-to-end |
| 1. Игрок и экономика | Player, Catalog, Inventory, Wallet, Store (игровая валюта), Session-контракт (заглушка отчётов) | покупка одежды/оружия/машины/детали, гараж и тюнинг, профиль |
| 1.5 gRPC-транспорт | `.proto`-контракт Wallet `Reserve/Commit/Release`, gRPC-сервер рядом с REST, gRPC-клиент в Store; общий маппер ошибок | Store вызывает Wallet по gRPC, REST-эндпоинты кошелька продолжают работать |
| 2. Сезонный контент | BattlePass, Events, Clan, ClanWar, Leaderboard, реальные отчёты матчей | BP-сезон, летний/новогодний ивент, клан-войны и выплата сезона |
| 3. Донат и сервис-полировка | Payment, hard-валюта, premium BP, Notification/Mail, BFF, аналитика | донат end-to-end, возвраты, почта, агрегированный экран входа |
| 3.5 Realtime-оповещения | SignalR-hub (RealtimeGateway), Redis backplane, группы `player/clan/liveops`, консьюмеры событий, «снимок + подписка» на клиенте, nginx WebSocket | клиент получает старт сезона, изменение очков клана и личные события в реальном времени |
| 4. Масштаб | реплики/шардирование PG, Kafka-реплей, canary, anti-fraud ML-сигналы | нагрузочный тест 50k+ CCU, сезонный «шторм» выдерживается |

## 13. Открытые вопросы

1. **Игровые серверы**: какой движок/протокол, сервер-авторитативность, как
   передаются отчёты (gRPC-стрим или батчи)? От этого зависит SessionService.
2. **Мир один или регионы**: шардирование по `player_id`, кросс-региональные
   клан-войны (общий клан-сервис или федерация?).
3. **Продуктовые параметры кланов**: N кланов в войне, размер клана, длина
   сезона/раунда, политика выхода во время сезона, сгорают ли невыплаченные
   награды.
4. **Ники**: уникальность и правила смены (Player), фильтр мата, резервация.
5. **Чат/друзья/пати**: отдельный ChatService (WebSocket) не заложен — нужен ли
   в фазе 2–3.
6. **Торговля между игроками/аукцион**: нет в плане; сильно влияет на экономику
   и антифрод.
7. **PSP**: выбор провайдера (Stripe/Xsolla/Steam), регионы, налоги, возвраты.
8. **Premium BP**: политика при покупке в середине сезона (ретро-выдача наград).
9. **Тюнинг**: влияет ли на геймплей (баланс, античит) или только косметика.
10. **Лимиты**: количество машин/скинов на игрока, размер гаража, лимиты инвентаря.
