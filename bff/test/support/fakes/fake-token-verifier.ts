import type { TokenVerifierPort } from '../../../src/modules/auth/application/ports/token-verifier.port';
import type { AuthenticatedUser } from '../../../src/modules/auth/domain/authenticated-user';
import { InvalidTokenError } from '../../../src/modules/auth/domain/auth-errors';
import { anAuthenticatedUser } from '../builders/authenticated-user.builder';

/** Fake do verificador de token: devolve usuário configurado ou lança InvalidTokenError. */
export class FakeTokenVerifier implements TokenVerifierPort {
  readonly verifiedTokens: string[] = [];
  constructor(
    private user: AuthenticatedUser = anAuthenticatedUser(),
    private shouldFail = false,
  ) {}

  setUser(user: AuthenticatedUser): void {
    this.user = user;
    this.shouldFail = false;
  }

  failNext(): void {
    this.shouldFail = true;
  }

  async verify(token: string): Promise<AuthenticatedUser> {
    this.verifiedTokens.push(token);
    if (this.shouldFail) {
      this.shouldFail = false;
      throw new InvalidTokenError();
    }
    return this.user;
  }
}
