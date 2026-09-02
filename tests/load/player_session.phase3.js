import http from 'k6/http';
import exec from 'k6/execution';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.LOADTEST_BASE_URL || '').replace(/\/$/, '');
const password = __ENV.LOADTEST_USER_PASSWORD;
const leagueName = __ENV.LOADTEST_LEAGUE_NAME || 'PLAYPREDICT LOADTEST OFFICIAL';
const targetVus = Number.parseInt(__ENV.TARGET_VUS || '250', 10);
const rampSeconds = Number.parseInt(__ENV.RAMP_SECONDS || '60', 10);
const steadySeconds = Number.parseInt(__ENV.STEADY_SECONDS || '180', 10);

const loginInitial = new Trend('player_initial_login_duration', true);
const steadyDuration = new Trend('player_steady_duration', true);
const steadyRequests = new Counter('player_steady_requests');
const functionalErrors = new Rate('player_functional_errors');
const operationMetrics = Object.fromEntries(
  ['MATCHES', 'PREDICTION_GET', 'PREDICTION_SAVE', 'RANKING', 'LEAGUES']
    .map((name) => [name, new Trend(`player_${name.toLowerCase()}_duration`, true)]),
);

let session = null;

export const options = {
  scenarios: {
    authenticated_player: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: `${rampSeconds}s`, target: targetVus },
        { duration: `${steadySeconds}s`, target: targetVus },
        { duration: __ENV.RAMP_DOWN || '30s', target: 0 },
      ],
      gracefulRampDown: '15s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    player_functional_errors: ['rate<0.01'],
    player_steady_duration: ['p(95)<1000'],
  },
};

function inSteadyState() {
  const elapsedSeconds = (Date.now() - exec.scenario.startTime) / 1000;
  return elapsedSeconds >= rampSeconds && elapsedSeconds < rampSeconds + steadySeconds;
}

function request(method, path, body, token, operation, bootstrap = false, expected = [200]) {
  const steady = !bootstrap && inSteadyState();
  const response = http.request(method, `${baseUrl}/api${path}`, body === null ? null : JSON.stringify(body), {
    headers: {
      ...(body === null ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    tags: { operation, phase: bootstrap ? 'bootstrap' : (steady ? 'steady' : 'ramp') },
  });
  const ok = check(response, { [`${operation} status`]: (item) => expected.includes(item.status) });
  functionalErrors.add(!ok);
  if (steady) {
    steadyDuration.add(response.timings.duration);
    steadyRequests.add(1);
    operationMetrics[operation].add(response.timings.duration);
  }
  return response;
}

function initialize() {
  sleep(Math.random() * 1.5);
  const userIndex = ((exec.vu.idInTest - 1) % 1000) + 1;
  const email = `loadtest${String(userIndex).padStart(5, '0')}@playpredict.test`;
  const login = request('POST', '/auth/login', { email, password }, null, 'LOGIN', true);
  loginInitial.add(login.timings.duration);
  const token = login.json('token');
  if (!token) throw new Error(`Initial login failed for VU ${exec.vu.idInTest}.`);

  const leaguesResponse = request('GET', '/leagues/officials', null, token, 'LEAGUES', true);
  const league = leaguesResponse.json().find((item) => item.name === leagueName);
  if (!league) throw new Error(`League not found for VU ${exec.vu.idInTest}.`);
  request('POST', `/leagues/${league.id}/join`, null, token, 'LEAGUES', true);
  const matchesResponse = request('GET', `/leagues/${league.id}/matches`, null, token, 'MATCHES', true);
  const future = matchesResponse.json().filter((item) => item.status === 'Scheduled' && item.canPredict);
  if (!future.length) throw new Error('No predictable load-test match exists.');
  return { token, leagueId: league.id, future };
}

export default function () {
  if (!session) session = initialize();
  const target = session.future[(exec.vu.idInTest - 1) % session.future.length];
  const choice = Math.random() * 100;
  if (choice < 30) {
    request('GET', `/leagues/${session.leagueId}/matches`, null, session.token, 'MATCHES');
  } else if (choice < 55) {
    request('GET', `/predictions/rounds/${target.roundId}?leagueId=${session.leagueId}`, null, session.token, 'PREDICTION_GET');
  } else if (choice < 75) {
    const even = exec.vu.iterationInScenario % 2 === 0;
    request('POST', '/predictions/', {
      leagueId: session.leagueId,
      matchId: target.id,
      predictedHomeScore: even ? 2 : 1,
      predictedAwayScore: 1,
      preferredPlayerId: null,
      updatePreferredPlayer: false,
    }, session.token, 'PREDICTION_SAVE', false, [200, 201]);
  } else if (choice < 90) {
    request('GET', `/rankings/leagues/${session.leagueId}`, null, session.token, 'RANKING');
  } else {
    request('GET', '/leagues/officials', null, session.token, 'LEAGUES');
  }
  sleep(0.5 + Math.random());
}
