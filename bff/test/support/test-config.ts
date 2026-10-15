import type { AppEnv } from '../../src/shared/config/env.schema';

let counter = 0;

/** Config válida de teste; sobrescreva o necessário por teste/E2E. */
export function buildTestConfig(overrides: Partial<AppEnv> = {}): AppEnv {
  counter += 1;
  return {
    PORT: 0,
    NODE_ENV: 'test',
    KEYCLOAK_ISSUER: 'https://auth.test/realms/clinica',
    KEYCLOAK_JWKS_URI: 'https://auth.test/realms/clinica/certs',
    KEYCLOAK_CLIENT_ID: 'bff-clinica',
    KEYCLOAK_AUDIENCE: undefined,
    KEYCLOAK_ROLES_SOURCE: 'realm',
    JWT_CLOCK_TOLERANCE_SECONDS: 5,
    JWKS_CACHE_MAX_AGE_MS: 600000,
    JWKS_COOLDOWN_MS: 30000,
    DENTIST_SERVICE_BASE_URL: `http://127.0.0.1:9${String(counter).padStart(3, '0')}`,
    EMPLOYEE_SERVICE_BASE_URL: `http://127.0.0.1:8${String(counter).padStart(3, '0')}`,
    DOWNSTREAM_TIMEOUT_MS: 2000,
    CORS_ORIGINS: '',
    SWAGGER_ENABLED: false,
    THROTTLE_TTL_SECONDS: 60,
    THROTTLE_LIMIT: 1000,
    ...overrides,
  };
}
