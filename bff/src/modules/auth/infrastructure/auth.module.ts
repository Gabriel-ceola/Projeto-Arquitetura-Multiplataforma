import { Module } from '@nestjs/common';
import { APP_GUARD } from '@nestjs/core';
import { JoseJwksTokenVerifier } from './jose-jwks-token-verifier.adapter';
import { JwtAuthGuard } from './jwt-auth.guard';
import { RolesGuard } from './roles.guard';
import { AuthenticateUseCase } from '../application/authenticate.use-case';
import { TOKEN_VERIFIER } from '../application/ports/token-verifier.port';

@Module({
  providers: [
    {
      provide: TOKEN_VERIFIER,
      useClass: JoseJwksTokenVerifier,
    },
    AuthenticateUseCase,
    {
      provide: APP_GUARD,
      useClass: JwtAuthGuard,
    },
    {
      provide: APP_GUARD,
      useClass: RolesGuard,
    },
  ],
  exports: [AuthenticateUseCase],
})
export class AuthModule {}
