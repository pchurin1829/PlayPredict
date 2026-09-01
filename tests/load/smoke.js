import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const baseUrl = (__ENV.LOADTEST_BASE_URL || '').replace(/\/$/, '');
const leagueName = __ENV.LOADTEST_LEAGUE_NAME || 'PLAYPREDICT LOADTEST OFFICIAL';
const userIndex = Number.parseInt(__ENV.LOADTEST_USER_INDEX || '1', 10);
const password = __ENV.LOADTEST_USER_PASSWORD;

const durations = {
  LOGIN: new Trend('playpredict_login_duration', true),
  LEAGUES: new Trend('playpredict_leagues_duration', true),
  MATCHES: new Trend('playpredict_matches_duration', true),
  PREDICTION_GET: new Trend('playpredict_prediction_get_duration', true),
  PREDICTION_SAVE: new Trend('playpredict_prediction_save_duration', true),
  RANKING: new Trend('playpredict_ranking_duration', true),
};

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

function requireConfiguration() {
  if (!baseUrl) throw new Error('LOADTEST_BASE_URL is required.');
  if (!password) throw new Error('LOADTEST_USER_PASSWORD is required.');
  if (!Number.isInteger(userIndex) || userIndex < 1 || userIndex > 10000) {
    throw new Error('LOADTEST_USER_INDEX must be an integer between 1 and 10000.');
  }
}

function request(method, path, body, token, metric, expectedStatuses = [200]) {
  const params = {
    headers: {
      ...(body === null ? {} : { 'Content-Type': 'application/json' }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    tags: { operation: metric },
  };
  const payload = body === null ? null : JSON.stringify(body);
  const response = http.request(method, `${baseUrl}/api${path}`, payload, params);
  durations[metric].add(response.timings.duration);
  const accepted = check(response, {
    [`${metric} returned ${expectedStatuses.join(' or ')}`]: (item) => expectedStatuses.includes(item.status),
  });
  if (!accepted) {
    throw new Error(`${metric} failed with HTTP ${response.status}: ${response.body}`);
  }
  return response;
}

function json(response, label) {
  try {
    return response.json();
  } catch (error) {
    throw new Error(`${label} did not return valid JSON: ${error.message}`);
  }
}

export default function () {
  requireConfiguration();
  const email = `loadtest${String(userIndex).padStart(5, '0')}@playpredict.test`;

  const auth = group('LOGIN', () => {
    const response = request('POST', '/auth/login', { email, password }, null, 'LOGIN');
    const data = json(response, 'LOGIN');
    if (!data.token) throw new Error('LOGIN response did not contain a token.');
    return data;
  });

  const league = group('LEAGUES', () => {
    const listResponse = request('GET', '/leagues/officials', null, auth.token, 'LEAGUES');
    const leagues = json(listResponse, 'LEAGUES');
    const selected = leagues.find((item) => item.name === leagueName);
    if (!selected) throw new Error(`Official league '${leagueName}' was not found.`);

    request('POST', `/leagues/${selected.id}/join`, null, auth.token, 'LEAGUES');
    return selected;
  });

  const target = group('MATCHES', () => {
    const response = request('GET', `/leagues/${league.id}/matches`, null, auth.token, 'MATCHES');
    const matches = json(response, 'MATCHES');
    const future = matches.find((match) => match.status === 'Scheduled' && match.canPredict);
    if (!future) throw new Error('No future predictable match was found.');
    return future;
  });

  group('PREDICTION_GET', () => {
    const response = request(
      'GET',
      `/predictions/rounds/${target.roundId}?leagueId=${league.id}`,
      null,
      auth.token,
      'PREDICTION_GET',
    );
    const matches = json(response, 'PREDICTION_GET');
    if (!matches.some((match) => match.id === target.id)) {
      throw new Error('Target match was not returned by the prediction query.');
    }
  });

  const preferredPlayerId = target.homePlayers?.[0]?.id ?? null;
  const prediction = group('PREDICTION_SAVE', () => {
    const createResponse = request(
      'POST',
      '/predictions/',
      {
        leagueId: league.id,
        matchId: target.id,
        predictedHomeScore: 1,
        predictedAwayScore: 0,
        preferredPlayerId,
        updatePreferredPlayer: preferredPlayerId !== null,
      },
      auth.token,
      'PREDICTION_SAVE',
      [200, 201],
    );
    const saved = json(createResponse, 'PREDICTION_SAVE create');
    if (!saved.id) throw new Error('Saved prediction did not contain an id.');

    const updateResponse = request(
      'PUT',
      `/predictions/${saved.id}`,
      {
        leagueId: league.id,
        predictedHomeScore: 2,
        predictedAwayScore: 1,
        preferredPlayerId,
        updatePreferredPlayer: preferredPlayerId !== null,
      },
      auth.token,
      'PREDICTION_SAVE',
    );
    const updated = json(updateResponse, 'PREDICTION_SAVE update');
    check(updated, {
      'prediction was modified to 2-1': (item) => item.predictedHomeScore === 2 && item.predictedAwayScore === 1,
    });
    return updated;
  });

  if (!prediction?.id) throw new Error('Prediction flow did not complete.');

  group('RANKING', () => {
    const response = request('GET', `/rankings/leagues/${league.id}`, null, auth.token, 'RANKING');
    const ranking = json(response, 'RANKING');
    if (!Array.isArray(ranking) || ranking.length === 0) {
      throw new Error('RANKING returned no entries.');
    }
  });
}
