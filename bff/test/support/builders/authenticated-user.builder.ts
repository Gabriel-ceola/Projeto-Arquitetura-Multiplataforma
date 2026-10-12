import { Role } from '../../../src/modules/auth/domain/role.enum';
import type { AuthenticatedUser } from '../../../src/modules/auth/domain/authenticated-user';

export function anAuthenticatedUser(
  overrides: Partial<AuthenticatedUser> = {},
): AuthenticatedUser {
  return {
    id: 'dentist-A',
    username: 'dentist.a',
    email: 'a@clinica.test',
    roles: [Role.Dentist],
    ...overrides,
  };
}
