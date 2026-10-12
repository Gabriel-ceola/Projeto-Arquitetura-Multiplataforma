import { Inject, Injectable, Logger } from '@nestjs/common';
import { createRemoteJWKSet, jwtVerify, JWTPayload } from 'jose';
import { APP_CONFIG } from '@shared/config/app-config.module';
import type { AppEnv } from '@shared/config/env.schema';
import { TokenVerifierPort } from '../application/ports/token-verifier.port';
import { AuthenticatedUser } from '../domain/authenticated-user';
import { InvalidTokenError } from '../domain/auth-errors';
import { extractRoles } from '../application/role-extractor';

@Injectable()
export class JoseJwksTokenVerifier implements TokenVerifierPort {
  private readonly logger = new Logger(JoseJwksTokenVerifier.name);
  private readonly jwks: ReturnType<typeof createRemoteJWKSet>;

  constructor(@Inject(APP_CONFIG) private readonly config: AppEnv) {
    this.jwks = createRemoteJWKSet(new URL(config.KEYCLOAK_JWKS_URI), {
      cacheMaxAge: config.JWKS_CACHE_MAX_AGE_MS,
      cooldownDuration: config.JWKS_COOLDOWN_MS,
      timeoutDuration: config.DOWNSTREAM_TIMEOUT_MS,
    });
  }

  async verify(token: string): Promise<AuthenticatedUser> {
    try {
      const audience =
        this.config.KEYCLOAK_AUDIENCE &&
        this.config.KEYCLOAK_AUDIENCE.trim() !== ''
          ? this.config.KEYCLOAK_AUDIENCE
          : undefined;
      const { payload } = await jwtVerify(token, this.jwks, {
        issuer: this.config.KEYCLOAK_ISSUER,
        algorithms: ['RS256'],
        clockTolerance: this.config.JWT_CLOCK_TOLERANCE_SECONDS,
        ...(audience ? { audience } : {}),
      });

      // Se KEYCLOAK_AUDIENCE não estiver definido, validar azp
      if (!audience) {
        const azp = (payload as JWTPayload & { azp?: string }).azp;
        if (azp !== this.config.KEYCLOAK_CLIENT_ID) {
          this.logger.warn('Token com azp inválido rejeitado');
          throw new InvalidTokenError();
        }
      }

      if (!payload.sub || typeof payload.sub !== 'string') {
        this.logger.warn('Token sem claim sub rejeitado');
        throw new InvalidTokenError();
      }

      const roles = extractRoles(payload, {
        source: this.config.KEYCLOAK_ROLES_SOURCE,
        clientId: this.config.KEYCLOAK_CLIENT_ID,
      });

      const claims = payload as Record<string, unknown>;
      return {
        id: payload.sub,
        username: claims['preferred_username'] as string | undefined,
        email: claims['email'] as string | undefined,
        roles,
      };
    } catch (err) {
      if (err instanceof InvalidTokenError) throw err;
      this.logger.warn(
        { err: (err as Error).message },
        'Falha na verificação do token',
      );
      throw new InvalidTokenError();
    }
  }
}
