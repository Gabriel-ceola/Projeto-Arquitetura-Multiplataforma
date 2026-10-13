// Carregado via `setupFiles` do Jest (antes de qualquer import dos testes).
// O AppConfigModule valida process.env no boot; estes fallbacks garantem que
// importar adapters/módulos nunca quebre por falta de env. Cada teste/E2E
// continua usando buildTestConfig() + overrideProvider(APP_CONFIG).
process.env.PORT ??= '3000';
process.env.NODE_ENV ??= 'test';
process.env.KEYCLOAK_ISSUER ??= 'https://auth.test/realms/clinica';
process.env.KEYCLOAK_JWKS_URI ??= 'http://127.0.0.1:1/certs';
process.env.KEYCLOAK_CLIENT_ID ??= 'bff-clinica';
process.env.DENTIST_SERVICE_BASE_URL ??= 'http://127.0.0.1:1';
process.env.EMPLOYEE_SERVICE_BASE_URL ??= 'http://127.0.0.1:1';
