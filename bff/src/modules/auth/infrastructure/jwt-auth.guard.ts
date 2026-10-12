import { CanActivate, ExecutionContext, Injectable } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { Request } from 'express';
import { AuthenticateUseCase } from '../application/authenticate.use-case';
import { IS_PUBLIC_KEY } from './decorators/public.decorator';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { AuthenticatedUser } from '../domain/authenticated-user';

@Injectable()
export class JwtAuthGuard implements CanActivate {
  constructor(
    private readonly reflector: Reflector,
    private readonly authenticateUseCase: AuthenticateUseCase,
  ) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const isPublic = this.reflector.getAllAndOverride<boolean>(IS_PUBLIC_KEY, [
      context.getHandler(),
      context.getClass(),
    ]);

    if (isPublic) return true;

    const request = context
      .switchToHttp()
      .getRequest<Request & { user?: AuthenticatedUser }>();
    const authHeader = request.headers[HEADERS.AUTHORIZATION];

    const user = await this.authenticateUseCase.execute({
      authorizationHeader: authHeader,
    });

    request.user = user;
    return true;
  }
}
