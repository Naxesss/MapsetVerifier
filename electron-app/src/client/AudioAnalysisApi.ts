import {
  AudioAnalysisResult,
  SpectralAnalysisResult,
  FrequencyAnalysisResult,
  HitSoundBatchResult,
} from '../Types';
import { apiFetch, FetchError } from './ApiHelper';

export interface AudioAnalysisRequest {
  beatmapSetFolder: string;
  audioFile?: string;
}

export interface SpectrogramRequest {
  beatmapSetFolder: string;
  audioFile?: string;
  fftSize?: number;
  timeResolutionMs?: number;
}

export interface FrequencyAnalysisRequest {
  beatmapSetFolder: string;
  audioFile?: string;
  fftSize?: number;
}

export interface HitSoundAnalysisRequest {
  beatmapSetFolder: string;
}

const AudioAnalysisApi = {
  analyze: async function analyzeAudio(request: AudioAnalysisRequest) {
    return apiFetch('/audio/analyze', {
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
        return data as AudioAnalysisResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },

  getSpectrogram: async function getSpectrogram(request: SpectrogramRequest) {
    return apiFetch('/audio/spectrogram', {
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
        return data as SpectralAnalysisResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },

  getFrequencyAnalysis: async function getFrequencyAnalysis(request: FrequencyAnalysisRequest) {
    return apiFetch('/audio/frequency', {
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
        return data as FrequencyAnalysisResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },

  analyzeHitSounds: async function analyzeHitSounds(request: HitSoundAnalysisRequest) {
    return apiFetch('/audio/hitsounds', {
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
        return data as HitSoundBatchResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
};

export default AudioAnalysisApi;

