import { ObjectsOverviewResult } from '../Types';
import { apiFetch, FetchError } from './ApiHelper';

export interface ObjectsAnalysisRequest {
  beatmapSetFolder: string;
}

const ObjectsAnalysisApi = {
  analyze: async function analyzeObjects(request: ObjectsAnalysisRequest) {
    return apiFetch('/beatmap-analysis/objects', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }).then(async (response) => {
      const raw = await response.text();
      let data: any = undefined;

      try {
        data = raw ? JSON.parse(raw) : undefined;
      } catch {
        /* ignore parse errors */
      }

      if (response.ok) {
        return data as ObjectsOverviewResult;
      }

      const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
      const stackTrace = data?.stackTrace;
      throw new FetchError(response, message, stackTrace);
    });
  },
};

export default ObjectsAnalysisApi;