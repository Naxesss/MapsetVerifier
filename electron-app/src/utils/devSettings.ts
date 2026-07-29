/** True only in dev builds (`npm run dev`), never in packaged/production builds. */
export const isDevBuild = import.meta.env.DEV;

/**
 * Resolves a dev-only setting's effective value. Settings persist to disk and are shared
 * across dev/prod builds pointing at the same profile, so a value saved while running in dev
 * would otherwise leak into production even though its toggle UI is hidden there.
 */
export function resolveDevOnlySetting(value: boolean): boolean {
  return isDevBuild && value;
}
