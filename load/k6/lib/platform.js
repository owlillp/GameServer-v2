// Shared helpers: accounts, tokens and the API envelope.
import http from 'k6/http';
import { check, fail } from 'k6';
import encoding from 'k6/encoding';

export const API = __ENV.API_URL || 'https://localhost:6101';
export const FAKE_SERVER = __ENV.FAKE_SERVER_URL || 'http://localhost:5291';
export const CLIENT_ID = __ENV.CLIENT_ID || 'gameserver-v2-unity';
export const PASSWORD = __ENV.LOAD_PASSWORD || 'LoadTest-Passw0rd!';
export const SCOPES = 'openid offline_access session matchmaking clans clanwars';

const JSON_HEADERS = { 'Content-Type': 'application/json' };

// Registers a load-test player (ignores "already exists") and returns an access token.
export function player(name) {
  http.post(`${API}/auth/register`, JSON.stringify({ email: `${name}@load.test`, userName: name, password: PASSWORD }), {
    headers: JSON_HEADERS,
    tags: { name: 'auth/register' },
  });

  return token(name, PASSWORD);
}

// Resource owner password grant of the Unity client.
export function token(username, password) {
  const response = http.post(
    `${API}/connect/token`,
    { grant_type: 'password', client_id: CLIENT_ID, username, password, scope: SCOPES },
    { tags: { name: 'connect/token' } },
  );

  if (response.status !== 200) {
    fail(`token for ${username}: ${response.status} ${response.body}`);
  }

  return response.json('access_token');
}

export function accountId(accessToken) {
  const payload = JSON.parse(decodeBase64Url(accessToken.split('.')[1]));
  return payload.sub;
}

export function call(method, path, accessToken, body, name) {
  const response = http.request(method, `${API}${path}`, body === undefined ? null : JSON.stringify(body), {
    headers: { ...JSON_HEADERS, Authorization: `Bearer ${accessToken}` },
    tags: { name: name || path },
  });

  return response;
}

// Unwraps { result, errors, isError }; returns null on an error envelope.
export function result(response) {
  if (response.status >= 400) {
    return null;
  }

  const envelope = response.json();
  return envelope && !envelope.isError ? envelope.result : null;
}

export function expectOk(response, what) {
  check(response, { [`${what} ok`]: (r) => r.status < 400 });
  return result(response);
}

function decodeBase64Url(value) {
  return encoding.b64decode(value, 'rawurl', 's');
}
