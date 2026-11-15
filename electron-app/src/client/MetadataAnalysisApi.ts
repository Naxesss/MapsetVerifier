import { MetadataAnalysisResult, StoryboardDetailResult } from '../Types';
import { apiFetch, FetchError } from './ApiHelper';

export interface MetadataAnalysisRequest {
  beatmapSetFolder: string;
}

async function postJson<T>(path: string, request: MetadataAnalysisRequest): Promise<T> {
  return apiFetch(path, {
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
      return data as T;
    } else {
      const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
      const stackTrace = data?.stackTrace;
      throw new FetchError(response, message, stackTrace);
    }
  });
}

const MetadataAnalysisApi = {
  analyze: (request: MetadataAnalysisRequest) =>
    postJson<MetadataAnalysisResult>('/metadata/analyze', request),
  /** What the storyboard is made of: its images, samples and layers. */
  storyboard: (request: MetadataAnalysisRequest) =>
    postJson<StoryboardDetailResult>('/metadata/storyboard', request),
};

export default MetadataAnalysisApi;
