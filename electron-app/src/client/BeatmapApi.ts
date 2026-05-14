import {
  ApiBeatmapPage,
  ApiBeatmapInfo,
  ApiBeatmapSetCheckResult,
  ApiCategoryOverrideCheckResult,
  ApiLazerLookupResult,
} from '../Types.ts';
import { apiFetch, FetchError } from './ApiHelper.ts';

const BeatmapApi = {
  get: async function fetchGeneralDocumentation(params: URLSearchParams) {
    return apiFetch(`/beatmap?${params.toString()}`).then(async (response) => {
      const data = await response.json();

      if (response.ok) {
        return data;
      } else {
        throw new FetchError(response);
      }
    }) as Promise<ApiBeatmapPage>;
  },
  getInfo: async function getInfo(folder: string) {
    return apiFetch('/beatmap/info', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ folder }),
    }).then(async (response) => {
      const raw = await response.text();
      let data: any = undefined;
      try {
        data = raw ? JSON.parse(raw) : undefined;
      } catch {
        /* ignore parse errors */
      }

      if (response.ok) {
        return data as ApiBeatmapInfo;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
  getLazerCurrent: async function getLazerCurrent() {
    return apiFetch('/beatmap/lazer/current').then(async (response) => {
      const data = await response.json();

      if (response.ok) {
        return data as ApiLazerLookupResult;
      } else {
        throw new FetchError(response);
      }
    });
  },
  getStableCurrent: async function getStableCurrent(songFolder?: string) {
    const params = new URLSearchParams();
    if (songFolder) {
      params.set('songsFolder', songFolder);
    }

    const query = params.toString();
    const path = query ? `/beatmap/stable/current?${query}` : '/beatmap/stable/current';
    return apiFetch(path).then(async (response) => {
      const data = await response.json();

      if (response.ok) {
        return data as ApiLazerLookupResult;
      } else {
        throw new FetchError(response);
      }
    });
  },
  runChecks: async function runChecks(folder: string) {
    return apiFetch(`/beatmap/runChecks`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ folder }),
    }).then(async (response) => {
      const raw = await response.text();
      let data: any = undefined;
      try {
        data = raw ? JSON.parse(raw) : undefined;
      } catch {
        /* ignore parse errors */
      }

      if (response.ok) {
        return data as ApiBeatmapSetCheckResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        const details = data?.details;
        throw new FetchError(response, message, stackTrace, details);
      }
    });
  },
  runCheckOverride: async function runCheckOverride(
    folder: string,
    difficultyName: string,
    overrideDifficulty: string
  ) {
    const normalizedDifficulty = overrideDifficulty === 'Ultra' ? 'Expert' : overrideDifficulty;

    return apiFetch(`/beatmap/runCheck/override`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        folder,
        difficultyName,
        overrideDifficulty: normalizedDifficulty,
      }),
    }).then(async (response) => {
      const raw = await response.text();
      let data: any = undefined;
      try {
        data = raw ? JSON.parse(raw) : undefined;
      } catch {
        /* ignore parse errors */
      }

      if (response.ok) {
        return data as ApiCategoryOverrideCheckResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        const details = data?.details;
        throw new FetchError(response, message, stackTrace, details);
      }
    });
  },
};

export default BeatmapApi;
