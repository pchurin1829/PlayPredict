import http from 'k6/http';
import exec from 'k6/execution';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.LOADTEST_BASE_URL || '').replace(/\/$/, '');
const password = __ENV.LOADTEST_USER_PASSWORD;
const duration = new Trend('ranking_isolated_duration', true);
const errors = new Rate('ranking_isolated_errors');
let session = null;

export const options = {
  scenarios: {
    ranking: {
      executor: 'ramping-vus', startVUs: 0,
      stages: [{ duration: '10s', target: 50 }, { duration: '30s', target: 50 }, { duration: '10s', target: 0 }],
      gracefulRampDown: '10s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  thresholds: { ranking_isolated_errors: ['rate<0.01'] },
};

function initialize() {
  const index = ((exec.vu.idInTest - 1) % 1000) + 1;
  const email = `loadtest${String(index).padStart(5, '0')}@playpredict.test`;
  const login = http.post(`${baseUrl}/api/auth/login`, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' }, tags: { phase: 'bootstrap' },
  });
  const token = login.json('token');
  const leagues = http.get(`${baseUrl}/api/leagues/officials`, {
    headers: { Authorization: `Bearer ${token}` }, tags: { phase: 'bootstrap' },
  }).json();
  return { token, leagueId: leagues[0].id };
}

export default function () {
  if (!session) session = initialize();
  const response = http.get(`${baseUrl}/api/rankings/leagues/${session.leagueId}`, {
    headers: { Authorization: `Bearer ${session.token}` }, tags: { operation: 'RANKING_ISOLATED' },
  });
  duration.add(response.timings.duration);
  const ok = check(response, { 'ranking 200': (item) => item.status === 200 });
  errors.add(!ok);
  sleep(0.1);
}
