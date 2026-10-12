import { Role } from '../domain/role.enum';

const ROLE_VALUES = new Set<string>(Object.values(Role));

export interface RoleExtractorOptions {
  source: 'realm' | 'client';
  clientId: string;
}

/**
 * Função pura: extrai roles do payload JWT conforme a fonte configurada.
 * Valores desconhecidos são ignorados. Resultado deduplicado.
 */
export function extractRoles(
  claims: Record<string, unknown>,
  options: RoleExtractorOptions,
): Role[] {
  let rawRoles: unknown;

  if (options.source === 'realm') {
    const realmAccess = claims['realm_access'];
    if (
      realmAccess !== null &&
      typeof realmAccess === 'object' &&
      !Array.isArray(realmAccess)
    ) {
      rawRoles = (realmAccess as Record<string, unknown>)['roles'];
    }
  } else {
    const resourceAccess = claims['resource_access'];
    if (
      resourceAccess !== null &&
      typeof resourceAccess === 'object' &&
      !Array.isArray(resourceAccess)
    ) {
      const clientData = (resourceAccess as Record<string, unknown>)[
        options.clientId
      ];
      if (
        clientData !== null &&
        typeof clientData === 'object' &&
        !Array.isArray(clientData)
      ) {
        rawRoles = (clientData as Record<string, unknown>)['roles'];
      }
    }
  }

  if (!Array.isArray(rawRoles)) {
    return [];
  }

  const seen = new Set<Role>();
  for (const r of rawRoles) {
    if (typeof r === 'string' && ROLE_VALUES.has(r)) {
      seen.add(r as Role);
    }
  }

  return Array.from(seen);
}
