import { BeatmapAnalysisResult } from '../Types';
import { apiFetch, FetchError } from './ApiHelper';

export interface BeatmapAnalysisRequest {
  beatmapSetFolder: string;
}

const BeatmapAnalysisApi = {
  analyze: async function analyzeBeatmap(request: BeatmapAnalysisRequest) {
    return apiFetch('/beatmap-analysis/analyze', {
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
        return data as BeatmapAnalysisResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
};

export default BeatmapAnalysisApi;

