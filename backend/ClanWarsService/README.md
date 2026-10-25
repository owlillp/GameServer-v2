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

## Битвы

1. Окна битв — из шаблона сезона (`battleWindowStartUtc` / `battleWindowEndUtc`, например
   `19:00`–`23:00` UTC; окно через полночь тоже работает), нарезаются при публикации в
   `battle_windows`. Без шаблона битвы идут весь сезон.
2. Лидер или офицер ставит отряд: `POST /clanwars/battles/queue { accountIds }`. Проверки по
   проекции состава: роль, все в клане, окно открыто, сезон `Active`, клан не в очереди
   (ещё и уникальный индекс) и не в битве. Рейтинг — очки клана в сезоне на момент постановки.
3. `BattleMatcher` (фон, `SKIP LOCKED`) пока открыто окно: тот же размер отряда, разница
   рейтинга в пределах допуска, растущего с ожиданием (`BaseRatingTolerance` +
   `RatingTolerancePerMinute` × минуты), и не больше `MaxBattlesPerPairPerDay` встреч пары за сутки.
   Битва и команда `create-external-match.matchmaking` (ExternalRef = `clan-battle:{id}`,
   режим `CLAN-BATTLE`, две команды) пишутся в одной транзакции; кланы получают `battle-found`.
4. Matchmaking играет матч на игровом сервере как обычный (без пати, наград и рейтинга игроков)
   и сообщает `external-match-started` → битва `InProgress`, `external-match-completed` → итог,
   `external-match-cancelled` → битва отменяется.
5. Итог (`BattleResultService`) — одна транзакция под блокировкой сезона: условный переход
   `Scheduled/InProgress → Finished`, очки обоим кланам по `scoringRules` чеками
   `battle:{id}` (уникальны), вклад участников отрядов в `member_season_stats`. Повторная
   доставка ничего не задваивает; доставка, упавшая на полпути, дописывается следующей.
6. Битва без результата дольше `BattleTimeoutMinutes` отменяется. После конца сезона очередь
   снимается, а открытые битвы ещё могут прислать итог до `finalizesAt`; при закрытии
   оставшиеся отменяются.

| Метод | Путь | Что делает |
| --- | --- | --- |
| `GET` | `/clanwars/seasons/{id}/windows` | Окна битв |
| `POST` | `/clanwars/battles/queue` | Поставить отряд |
| `DELETE` | `/clanwars/battles/queue` | Снять отряд |
| `GET` | `/clanwars/battles/me` | Отряд в очереди и последние битвы клана |

## Награды сезона

1. Таблица наград (`POST /admin/clanwars/reward-tables`): диапазоны мест → поинты клану,
   валюта и предметы участникам с `minBattles` битвами за клан. Сезон ссылается на таблицу
   (`rewardTableId`), публикация проверяет, что она существует.
2. Закрытие сезона в одной транзакции под блокировкой: снимок `season_results`, переход
   `Finalizing → Rewarding`, создание `season_reward_grants` (клану — поинты, участникам —
   валюта/предметы). Участники берутся из вклада в битвах (`member_season_stats`), а не из
   текущего состава. Уникальный индекс (сезон, клан, игрок) `NULLS NOT DISTINCT` — повторное
   закрытие ничего не задваивает. Без таблицы наград сезон сразу `Closed`.
3. `SeasonRewardDispatcher`: пачки через `SKIP LOCKED`; поинты — `clan-points-granted.clanwars`
   (ledger ClanService, operation id = grant id), предметы — команда `grant-items.clanwars`
   в Inventory (receipt по grant id, ответ `reward-items-granted/failed.clanwars`), валюта —
   gRPC `Credit` в Wallet (operation id = grant id), параллельно до `RewardParallelism`.
4. Статусы `Pending → Granted / Failed`; ошибки Wallet — повтор с экспоненциальной задержкой
   (`RewardBackoffSeconds`, до часа), после `RewardMaxAttempts` — `Failed` и лог `ALERT`.
   Отказ Inventory — сразу `Failed`. `POST /admin/clanwars/seasons/{id}/rewards/retry-failed`
   возвращает их в очередь, выполненные шаги не повторяются.
5. `Rewarding → Closed`, когда все гранты `Granted`; пока есть `Failed` — сезон ждёт и пишет `ALERT`.
6. Сверка раз в сутки (Quartz, `ReconciliationCron`, одна реплика): каждый зачисленный за
   `ReconciliationDays` грант проверяется в ledger Wallet (`WalletCredit.GetOperation`), пропавший
   открывается и зачисляется заново (идемпотентно); предметы без подтверждения дольше
   `StuckItemsMinutes` отправляются повторно (Inventory дедуплицирует). Поинты идут через outbox
   в идемпотентный ledger ClanService и отдельной сверки не имеют.

| Метод | Путь | Что делает |
| --- | --- | --- |
| `GET` | `/clanwars/seasons/{id}/rewards/me` | Мои награды сезона |
| `GET` / `POST` | `/admin/clanwars/reward-tables` | Таблицы наград |
| `GET` | `/admin/clanwars/seasons/{id}/rewards?status=` | Сводка Pending/Granted/Failed и гранты |
| `POST` | `/admin/clanwars/seasons/{id}/rewards/retry-failed` | Повторить неудачные |

Для выдачи валюты сервис ходит в Wallet как `clanwars-service-client` (scope `wallet`).

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

`season-started.clanwars`, `season-ended.clanwars` → RealtimeService (всем подключённым);
`battle-found.clanwars`, `battle-finished.clanwars` → RealtimeService (группы обоих кланов).
Команда `create-external-match.matchmaking`; слушает `external-match-*.matchmaking`.
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
