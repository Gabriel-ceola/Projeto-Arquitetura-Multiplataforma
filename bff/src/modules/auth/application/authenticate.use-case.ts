import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { InvalidTokenError } from '../domain/auth-errors';
import { AuthenticatedUser } from '../domain/authenticated-user';
import { TOKEN_VERIFIER } from './ports/token-verifier.port';
import type { TokenVerifierPort } from './ports/token-verifier.port';

export interface AuthenticateInput {
  authorizationHeader: string | undefined;
}

@Injectable()
export class AuthenticateUseCase implements UseCase<
  AuthenticateInput,
  AuthenticatedUser
> {
  constructor(
    @Inject(TOKEN_VERIFIER)
    private readonly tokenVerifier: TokenVerifierPort,
  ) {}

  async execute(input: AuthenticateInput): Promise<AuthenticatedUser> {
    const { authorizationHeader } = input;

    if (!authorizationHeader) {
      throw new InvalidTokenError();
    }

    const parts = authorizationHeader.split(' ');
    if (
      parts.length !== 2 ||
      parts[0].toLowerCase() !== 'bearer' ||
      !parts[1]
    ) {
      throw new InvalidTokenError();
    }

    const token = parts[1];
    return this.tokenVerifier.verify(token);
  }
}
