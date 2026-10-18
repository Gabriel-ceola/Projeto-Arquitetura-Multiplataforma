import { z } from 'zod';

export const envSchema = z.object({
  PORT: z.coerce.number().default(3000),
  NODE_ENV: z
    .enum(['development', 'production', 'test'])
    .default('development'),

  // Keycloak
  KEYCLOAK_ISSUER: z.string().min(1),
  KEYCLOAK_JWKS_URI: z.string().url(),
  KEYCLOAK_CLIENT_ID: z.string().min(1),
  KEYCLOAK_AUDIENCE: z
    .string()
    .optional()
    .transform((v) => (v && v.trim() !== '' ? v : undefined)),
  KEYCLOAK_ROLES_SOURCE: z.enum(['realm', 'client']).default('realm'),
  JWT_CLOCK_TOLERANCE_SECONDS: z.coerce.number().default(5),
  JWKS_CACHE_MAX_AGE_MS: z.coerce.number().default(600000),
  JWKS_COOLDOWN_MS: z.coerce.number().default(30000),

  // Downstream
  DENTIST_SERVICE_BASE_URL: z.string().url(),
  EMPLOYEE_SERVICE_BASE_URL: z.string().url().default('http://localhost:5070'),
  DOWNSTREAM_TIMEOUT_MS: z.coerce.number().default(5000),

  // HTTP
  CORS_ORIGINS: z.string().default(''),
  SWAGGER_ENABLED: z
    .union([z.string(), z.boolean()])
    .transform((v) => v === true || v === 'true')
    .default('false'),

  // Rate limit
  THROTTLE_TTL_SECONDS: z.coerce.number().default(60),
  THROTTLE_LIMIT: z.coerce.number().default(100),

  // Logs: pasta raiz dos logs JSON (ELK). Vazio = apenas stdout.
  LOG_DIR: z
    .string()
    .optional()
    .transform((v) => (v && v.trim() !== '' ? v.trim() : undefined)),
});

export type AppEnv = z.infer<typeof envSchema>;
