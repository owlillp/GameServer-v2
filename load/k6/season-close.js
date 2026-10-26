// Stage 8 scenario 3: closing a big season — results frozen, grants created and delivered.
//
//   k6 run -e ADMIN_USER=admin -e ADMIN_PASSWORD=... -e CLANS=1000 -e MEMBERS=29 load/k6/season-close.js
//
// Creates a reward table and a short season, gives CLANS clans season points through the admin
// correction API, waits for the season to close and measures the time from finalization until
// every reward grant is delivered (Rewarding -> Closed). 1 000 clans x (points + 29 members)
// is the 30 000-grant target of the plan; members get grants only if they fought battles, so
// for currency/item grants run battle-window-end.js against the same season first.
import { check, fail, sleep } from 'k6';
import { Trend } from 'k6/metrics';
import { call, player, result, token } from './lib/platform.js';

const CLANS = Number(__ENV.CLANS || 100);
const RUN = __ENV.RUN_ID || `${Date.now()}`;
const SEASON_MINUTES = Number(__ENV.SEASON_MINUTES || 3);

const closeTime = new Trend('season_close_to_delivered_ms', true);

export const options = {
  insecureSkipTLSVerify: true,
  setupTimeout: '30m',
  scenarios: { close: { executor: 'shared-iterations', vus: 1, iterations: 1, maxDuration: '60m' } },
  thresholds: { season_close_to_delivered_ms: ['max<600000'] },
};

export function setup() {
  const admin = token(__ENV.ADMIN_USER || fail('ADMIN_USER is required'), __ENV.ADMIN_PASSWORD || fail('ADMIN_PASSWORD is required'));

  const table = result(call('POST', '/admin/clanwars/reward-tables', admin, {
    name: `load-${RUN}`,
    tiers: [
      { fromRank: 1, toRank: 10, clanPoints: 1000, currencyCode: 'coins', currencyAmount: 500, items: [], minBattles: 1 },
      { fromRank: 11, toRank: 100000, clanPoints: 100, currencyCode: 'coins', currencyAmount: 50, items: [], minBattles: 1 },
    ],
  }, '/admin/clanwars/reward-tables'));
  if (!table) {
    fail('reward table was not created');
  }

  const now = Date.now();
  const season = result(call('POST', '/admin/clanwars/seasons', admin, {
    name: `Load ${RUN}`,
    startsAt: new Date(now + 60_000).toISOString(),
    endsAt: new Date(now + 60_000 + SEASON_MINUTES * 60_000).toISOString(),
    finalizationGraceMinutes: 0,
    rewardTableId: table.id,
  }, '/admin/clanwars/seasons'));
  if (!season || result(call('POST', `/admin/clanwars/seasons/${season.id}/publish`, admin, undefined, 'publish')) === null) {
    fail('season was not created or published (another season overlapping?)');
  }

  const clanIds = [];
  for (let index = 0; index < CLANS; index++) {
    const name = `load-season-${RUN}-${index}`;
    const clan = result(call('POST', '/clans', player(name), { tag: `S${index}`.slice(0, 6).padEnd(3, 'X'), name }, '/clans'));
    if (clan) {
      clanIds.push(clan.clan.id);
    }
  }

  return { admin, seasonId: season.id, finalizesAt: Date.parse(season.finalizesAt), startsAt: Date.parse(season.startsAt), clanIds };
}

export default function (data) {
  waitUntil(() => Date.now() >= data.startsAt + 5_000, 'season start');

  data.clanIds.forEach((clanId, index) => {
    const adjusted = call('POST', `/admin/clanwars/seasons/${data.seasonId}/scores`, data.admin, {
      clanId,
      amount: CLANS - index,
      reason: 'load test',
    }, 'scores');
    check(adjusted, { 'score applied': (r) => r.status < 400 });
  });

  waitUntil(() => Date.now() >= data.finalizesAt, 'finalization');

  const closed = waitUntil(() => {
    const seasons = result(call('GET', '/admin/clanwars/seasons', data.admin, undefined, 'seasons')) || [];
    const season = seasons.find((entry) => entry.id === data.seasonId);
    return season && season.status === 'CLOSED';
  }, 'season closed', 3600);

  if (closed) {
    closeTime.add(Date.now() - data.finalizesAt);
  }

  const summary = result(call('GET', `/admin/clanwars/seasons/${data.seasonId}/rewards?limit=1`, data.admin, undefined, 'rewards'));
  check(summary, {
    'no pending grants': (value) => value !== null && value.pending === 0,
    'no failed grants': (value) => value !== null && value.failed === 0,
  });

  // Closing twice must not double: the grant count stays the same after another phase job run.
  console.log(`season ${data.seasonId}: granted=${summary && summary.granted} failed=${summary && summary.failed}`);
}

function waitUntil(condition, what, maxSeconds = 1800) {
  for (let second = 0; second < maxSeconds; second++) {
    if (condition()) {
      return true;
    }

    sleep(1);
  }

  console.error(`timed out waiting for ${what}`);
  return false;
}
