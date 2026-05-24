import {
  ApiBeatmapPage,
  ApiBeatmapInfo,
  ApiBeatmapSetCheckResult,
  ApiBeatmapStructure,
  ApiCategoryOverrideCheckResult,
  ApiLazerLookupResult,
  CheckProgress,
} from '../Types.ts';
import { apiFetch, FetchError } from './ApiHelper.ts';
import { parseSseChunk } from './parseSse.ts';

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
    return BeatmapApi.runChecksStream(folder);
  },
  runChecksStream: async function runChecksStream(
    folder: string,
    options?: {
      signal?: AbortSignal;
      onProgress?: (progress: CheckProgress) => void;
      onStructure?: (structure: ApiBeatmapStructure) => void;
    }
  ) {
    const signal = options?.signal;

    if (signal?.aborted) {
      throw new DOMException('The check stream was aborted.', 'AbortError');
    }

    const response = await apiFetch('/beatmap/runChecks/stream', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
      },
      body: JSON.stringify({ folder }),
      signal,
    });

    if (!response.ok) {
      const raw = await response.text();
      let data: any = undefined;
      try {
        data = raw ? JSON.parse(raw) : undefined;
      } catch {
        /* ignore parse errors */
      }

      const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
      const stackTrace = data?.stackTrace;
      const details = data?.details;
      throw new FetchError(response, message, stackTrace, details);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error('Streaming response body unavailable');
    }

    const abortReader = () => {
      void reader.cancel();
    };
    signal?.addEventListener('abort', abortReader);

    const decoder = new TextDecoder();
    let buffer = '';
    let result: ApiBeatmapSetCheckResult | undefined;
    let lastLabel: string | null = null;

    try {
      for (;;) {
        if (signal?.aborted) {
          throw new DOMException('The check stream was aborted.', 'AbortError');
        }

        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const { messages, remaining } = parseSseChunk(buffer);
        buffer = remaining;

        for (const message of messages) {
          if (message.event === 'structure') {
            options?.onStructure?.(JSON.parse(message.data) as ApiBeatmapStructure);
          } else if (message.event === 'progress') {
            const progress = JSON.parse(message.data) as CheckProgress;
            if (progress.label) {
              lastLabel = progress.label;
            }
            options?.onProgress?.({
              ...progress,
              label: progress.label ?? lastLabel,
            });
          } else if (message.event === 'complete') {
            result = JSON.parse(message.data) as ApiBeatmapSetCheckResult;
          } else if (message.event === 'error') {
            const errorPayload = JSON.parse(message.data) as {
              message?: string;
              stackTrace?: string;
              details?: string;
            };
            throw new FetchError(
              response,
              errorPayload.message ?? 'An error occurred while running beatmap checks.',
              errorPayload.stackTrace,
              errorPayload.details
            );
          }
        }
      }
    } finally {
      signal?.removeEventListener('abort', abortReader);
    }

    if (!result) {
      throw new Error('Check stream ended without a result');
    }

    return result;
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
