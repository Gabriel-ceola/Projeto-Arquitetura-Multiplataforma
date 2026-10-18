/** Valores de `event.action` dos logs do BFF (catálogo em elk/docs/logging-standard.md). */
export const LOG_EVENTS = {
  AUTH_DENIED: 'auth.denied',
  RATE_LIMITED: 'http.rate_limited',
  DOWNSTREAM_TIMEOUT: 'downstream.timeout',
  DOWNSTREAM_UNAVAILABLE: 'downstream.unavailable',
  DOWNSTREAM_ERROR: 'downstream.error',
} as const;

/** Remove a query string: ela pode carregar dados pessoais e não deve ir para os logs. */
export function pathWithoutQuery(url: string | undefined): string | undefined {
  return url?.split('?')[0];
}
