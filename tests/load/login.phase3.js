import http from 'k6/http';
import exec from 'k6/execution';
import { check } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.LOADTEST_BASE_URL || '').replace(/\/$/, '');
const password = __ENV.LOADTEST_USER_PASSWORD;
const targetVus = Number.parseInt(__ENV.TARGET_VUS || '25', 10);
const loginDuration = new Trend('login_duration', true);
const loginErrors = new Rate('login_errors');

export const options = {
  scenarios: {
    login_capacity: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: __ENV.RAMP_UP || '15s', target: targetVus },
        { duration: __ENV.STEADY || '60s', target: targetVus },
        { duration: __ENV.RAMP_DOWN || '15s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  thresholds: {
    login_errors: ['rate<0.01'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const userIndex = ((exec.vu.idInTest - 1) % 1000) + 1;
  const email = `loadtest${String(userIndex).padStart(5, '0')}@playpredict.test`;
  const response = http.post(`${baseUrl}/api/auth/login`, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { operation: 'LOGIN_ONLY' },
  });
  loginDuration.add(response.timings.duration);
  const ok = check(response, { 'login 200 with token': (item) => item.status === 200 && !!item.json('token') });
  loginErrors.add(!ok);
}
