import { ApiBeatmapPage, ApiBeatmapSetCheckResult, ApiCategoryCheckResult } from '../Types.ts';
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
        throw new FetchError(response, message, stackTrace);
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
        return data as ApiCategoryCheckResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
};

export default BeatmapApi;
