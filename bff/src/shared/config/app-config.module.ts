import { Global, Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { envSchema, AppEnv } from './env.schema';

export const APP_CONFIG = Symbol('APP_CONFIG');

@Global()
@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      validate: (config: Record<string, unknown>) => {
        const result = envSchema.safeParse(config);
        if (!result.success) {
          const errors = result.error.flatten().fieldErrors;
          const missing = Object.entries(errors)
            .map(([k, v]) => `${k}: ${v.join(', ')}`)
            .join('\n');
          throw new Error(
            `Variáveis de ambiente inválidas ou ausentes:\n${missing}`,
          );
        }
        return result.data;
      },
    }),
  ],
  providers: [
    {
      provide: APP_CONFIG,
      useFactory: (cs: ConfigService<AppEnv, true>): AppEnv => ({
        PORT: cs.get('PORT', { infer: true }),
        NODE_ENV: cs.get('NODE_ENV', { infer: true }),
        KEYCLOAK_ISSUER: cs.get('KEYCLOAK_ISSUER', { infer: true }),
        KEYCLOAK_JWKS_URI: cs.get('KEYCLOAK_JWKS_URI', { infer: true }),
        KEYCLOAK_CLIENT_ID: cs.get('KEYCLOAK_CLIENT_ID', { infer: true }),
        KEYCLOAK_AUDIENCE: cs.get('KEYCLOAK_AUDIENCE', { infer: true }),
        KEYCLOAK_ROLES_SOURCE: cs.get('KEYCLOAK_ROLES_SOURCE', { infer: true }),
        JWT_CLOCK_TOLERANCE_SECONDS: cs.get('JWT_CLOCK_TOLERANCE_SECONDS', {
          infer: true,
        }),
        JWKS_CACHE_MAX_AGE_MS: cs.get('JWKS_CACHE_MAX_AGE_MS', { infer: true }),
        JWKS_COOLDOWN_MS: cs.get('JWKS_COOLDOWN_MS', { infer: true }),
        DENTIST_SERVICE_BASE_URL: cs.get('DENTIST_SERVICE_BASE_URL', {
          infer: true,
        }),
        EMPLOYEE_SERVICE_BASE_URL: cs.get('EMPLOYEE_SERVICE_BASE_URL', {
          infer: true,
        }),
        DOWNSTREAM_TIMEOUT_MS: cs.get('DOWNSTREAM_TIMEOUT_MS', { infer: true }),
        CORS_ORIGINS: cs.get('CORS_ORIGINS', { infer: true }),
        SWAGGER_ENABLED: cs.get('SWAGGER_ENABLED', { infer: true }),
        THROTTLE_TTL_SECONDS: cs.get('THROTTLE_TTL_SECONDS', { infer: true }),
        THROTTLE_LIMIT: cs.get('THROTTLE_LIMIT', { infer: true }),
      }),
      inject: [ConfigService],
    },
  ],
  exports: [APP_CONFIG, ConfigModule],
})
export class AppConfigModule {}
