import { Role } from './role.enum';

export interface AuthenticatedUser {
  id: string; // claim `sub` (UUID do Keycloak)
  username?: string; // preferred_username
  email?: string;
  roles: Role[];
}
