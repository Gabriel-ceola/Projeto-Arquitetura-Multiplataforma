export const HEADERS = {
  X_DENTIST_ID: 'x-dentist-id',
  X_CORRELATION_ID: 'x-correlation-id',
  AUTHORIZATION: 'authorization',
  CONTENT_TYPE: 'content-type',
} as const;

export type HeaderName = (typeof HEADERS)[keyof typeof HEADERS];
