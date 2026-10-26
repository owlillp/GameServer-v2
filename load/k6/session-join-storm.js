// Stage 8 scenario 1: login storm into the open world (SessionService POST /sessions/join).
//
//   k6 run -e PLAYERS=2000 -e PEAK_RPS=300 load/k6/session-join-storm.js
//
// Needs at least one registered game server (fake-game-server in docker-compose) with enough
// capacity; otherwise joins fail with "no server available", which the thresholds will show.
import { check } from 'k6';
import { Trend } from 'k6/metrics';
import { call, player, result } from './lib/platform.js';

const PLAYERS = Number(__ENV.PLAYERS || 1000);
const PEAK_RPS = Number(__ENV.PEAK_RPS || 200);
const REGION = __ENV.REGION || 'eu-west';

const joinTime = new Trend('session_join_ms', true);

export const options = {
  insecureSkipTLSVerify: true,
  setupTimeout: '10m',
  scenarios: {
    storm: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 200,
      maxVUs: 1000,
      stages: [
        { target: PEAK_RPS, duration: '1m' },
        { target: PEAK_RPS, duration: '3m' },
        { target: 0, duration: '30s' },
      ],
    },
  },
  thresholds: {
    'http_req_failed{name:/sessions/join}': ['rate<0.01'],
    session_join_ms: ['p(95)<300', 'p(99)<800'],
  },
};

export function setup() {
  const tokens = [];
  for (let index = 0; index < PLAYERS; index++) {
    tokens.push(player(`load-join-${index}`));
  }

  return { tokens };
}

export default function (data) {
  const accessToken = data.tokens[Math.floor(Math.random() * data.tokens.length)];
  const response = call('POST', '/sessions/join', accessToken, { region: REGION }, '/sessions/join');

  joinTime.add(response.timings.duration);
  const ticket = result(response);

  check(ticket, {
    'got server address': (value) => value !== null && value.serverAddress.length > 0,
    'got join ticket': (value) => value !== null && value.ticket.length > 0,
  });
}
