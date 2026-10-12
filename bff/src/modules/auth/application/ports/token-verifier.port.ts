import { AuthenticatedUser } from '../../domain/authenticated-user';

export const TOKEN_VERIFIER = Symbol('TOKEN_VERIFIER');

export interface TokenVerifierPort {
  /** Lança InvalidTokenError se o token for inválido. */
  verify(token: string): Promise<AuthenticatedUser>;
}
