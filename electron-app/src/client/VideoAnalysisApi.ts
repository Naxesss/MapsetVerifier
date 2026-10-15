import { VideoAnalysisResult } from '../Types';
import { apiFetch, FetchError } from './ApiHelper';

export interface VideoAnalysisRequest {
  beatmapSetFolder: string;
}

const VideoAnalysisApi = {
  analyze: async function analyzeVideos(request: VideoAnalysisRequest) {
    return apiFetch('/video/analyze', {
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
        return data as VideoAnalysisResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
};

export default VideoAnalysisApi;
