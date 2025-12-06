import { ApiSnapshotResult } from '../Types.ts';
import { apiFetch, FetchError } from './ApiHelper.ts';

const SnapshotApi = {
  getSnapshots: async function getSnapshots(folder: string) {
    return apiFetch(`/snapshot`, {
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
        return data as ApiSnapshotResult;
      } else {
        const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
        const stackTrace = data?.stackTrace;
        throw new FetchError(response, message, stackTrace);
      }
    });
  },
};

export default SnapshotApi;

