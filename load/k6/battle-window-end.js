// Stage 8 scenario 2: end of a battle window — PAIRS clan battles finish within about a minute.
//
//   k6 run -e PAIRS=500 load/k6/battle-window-end.js
//
// Full path per pair: two clan leaders queue one-player squads → ClanWars pairs them →
// Matchmaking external match → SessionService allocates the fake game server → both players
// connect with their tickets → the server reports → ClanWars credits points.
//
// Prerequisites (docker-compose):
//   - an Active season with an open battle window (see season-close.js or the admin API);
//   - clanwars-service with ClanWars__MinSquadTenureHours=0 (fresh test clans);
//   - matchmaking-service with ResultsSource=Session (default in Docker);
//   - fake-game-server reachable at FAKE_SERVER_URL with enough Capacity.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';
import { FAKE_SERVER, accountId, call, player, result } from './lib/platform.js';

const PAIRS = Number(__ENV.PAIRS || 100);
const RUN = __ENV.RUN_ID || `${Date.now()}`;

const reportToPoints = new Trend('battle_report_to_points_ms', true);
const battlesFinished = new Counter('battles_finished');
const battlesFailed = new Counter('battles_failed');

export const options = {
  insecureSkipTLSVerify: true,
  setupTimeout: '20m',
  scenarios: {
    window_end: { executor: 'per-vu-iterations', vus: PAIRS, iterations: 1, maxDuration: '10m' },
  },
  thresholds: {
    battle_report_to_points_ms: ['p(95)<60000'],
    battles_failed: ['count<1'],
  },
};

export function setup() {
  const pairs = [];
  for (let index = 0; index < PAIRS; index++) {
    pairs.push([leader(`a${index}`), leader(`b${index}`)]);
  }

  return { pairs };
}

function leader(suffix) {
  const name = `load-clan-${RUN}-${suffix}`;
  const accessToken = player(name);
  call('POST', '/clans', accessToken, { tag: suffix.toUpperCase().padEnd(3, 'X').slice(0, 6), name }, '/clans');
  return { accessToken, accountId: accountId(accessToken) };
}

export default function (data) {
  const [first, second] = data.pairs[__VU - 1];

  // ClanWars builds its roster projection from clan events; give it a moment.
  sleep(2);
  for (const side of [first, second]) {
    const queued = call('POST', '/clanwars/battles/queue', side.accessToken, { accountIds: [side.accountId] }, '/clanwars/battles/queue');
    check(queued, { 'squad queued': (r) => r.status < 400 });
  }

  const connections = [first, second].map((side) => waitForTicket(side.accessToken));
  if (connections.some((connection) => connection === null)) {
    battlesFailed.add(1);
    return;
  }

  for (const connection of connections) {
    http.post(`${FAKE_SERVER}/connect`, JSON.stringify({ ticket: connection.ticket }), {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'fake/connect' },
    });
  }

  const reportedAt = Date.now();
  const report = http.post(`${FAKE_SERVER}/sessions/${connections[0].sessionId}/report`, JSON.stringify({ durationSeconds: 600 }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'fake/report' },
  });
  check(report, { 'report accepted': (r) => r.status === 200 });

  if (waitForFinished(first.accessToken)) {
    reportToPoints.add(Date.now() - reportedAt);
    battlesFinished.add(1);
  } else {
    battlesFailed.add(1);
  }
}

function waitForTicket(accessToken) {
  for (let attempt = 0; attempt < 120; attempt++) {
    const match = result(call('GET', '/matchmaking/matches/me', accessToken, undefined, '/matchmaking/matches/me'));
    if (match && match.connection) {
      return match.connection;
    }

    sleep(1);
  }

  return null;
}

function waitForFinished(accessToken) {
  for (let attempt = 0; attempt < 120; attempt++) {
    const battles = result(call('GET', '/clanwars/battles/me', accessToken, undefined, '/clanwars/battles/me'));
    if (battles && battles.battles.some((battle) => battle.status === 'FINISHED')) {
      return true;
    }

    sleep(0.5);
  }

  return false;
}
