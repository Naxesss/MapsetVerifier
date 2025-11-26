import { BACKEND_BASE_URL } from '../Constants.ts';

export function apiFetch(path: string, options?: RequestInit) {
  return fetch(`${BACKEND_BASE_URL}${path}`, options);
}

export class FetchError extends Error {
  constructor(
    public res: Response,
    message?: string,
    public stackTrace?: string
  ) {
    super(message);
    this.name = 'FetchError';
  }
}
