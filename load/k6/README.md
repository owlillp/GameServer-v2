# Нагрузочные сценарии (k6)

Сценарии этапа 8 плана. Запускаются против docker-compose (API — `https://localhost:6101`,
заглушка сервера — `http://localhost:5291`), профиль `observability` даёт графики в Grafana
(`http://localhost:3001`) во время прогона.

| Скрипт | Что проверяет | Порог |
| --- | --- | --- |
| `session-join-storm.js` | Шторм входа в открытый мир: `POST /sessions/join` до `PEAK_RPS` в секунду | p95 < 300 мс, ошибок < 1 % |
| `battle-window-end.js` | `PAIRS` клановых битв проходят весь путь (очередь → матч → сервер → отчёт → очки) почти одновременно | p95 «отчёт → очки» < 60 с, ни одной потерянной битвы |
| `season-close.js` | Закрытие сезона на `CLANS` кланов: снимок, гранты, выдача до `Closed` | всё выдано за 10 мин, `pending = failed = 0` |

```bash
k6 run -e PLAYERS=2000 -e PEAK_RPS=300 load/k6/session-join-storm.js
```

```bash
k6 run -e PAIRS=500 load/k6/battle-window-end.js
```

```bash
k6 run -e ADMIN_USER=admin -e ADMIN_PASSWORD=... -e CLANS=1000 load/k6/season-close.js
```

Учётки создаются через `POST /auth/register` и password grant клиента `gameserver-v2-unity`.
Для `battle-window-end.js` нужен активный сезон с открытым окном битв и
`ClanWars__MinSquadTenureHours=0` у clanwars-service (иначе свежие тестовые кланы не пройдут
правило стажа). Для `season-close.js` нужен админ (роль Admin) и отсутствие других
опубликованных сезонов на это время.
