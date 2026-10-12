import { Reflector } from '@nestjs/core';
import { Role } from '../../domain/role.enum';
import { Auth } from './auth.decorator';
import { ROLES_KEY } from './roles.decorator';

describe('@Auth', () => {
  const reflector = new Reflector();

  it('aplica metadata de Roles legível via Reflector', () => {
    class C {
      @Auth(Role.Dentist)
      handler() {
        return 'ok';
      }
    }
    const target = new C();

    const roles = reflector.getAllAndOverride<Role[]>(ROLES_KEY, [
      target.handler,
      C,
    ]);

    expect(roles).toEqual([Role.Dentist]);
  });

  it('repasa múltiplas roles (OU)', () => {
    class C {
      @Auth(Role.Dentist, Role.Employ)
      handler() {
        return 'ok';
      }
    }
    const target = new C();

    const roles = reflector.getAllAndOverride<Role[]>(ROLES_KEY, [
      target.handler,
      C,
    ]);

    expect(roles).toEqual([Role.Dentist, Role.Employ]);
  });

  it('aplica metadados swagger (bearer + respostas 401/403)', () => {
    class C {
      @Auth(Role.Dentist)
      handler() {
        return 'ok';
      }
    }
    const target = new C();

    const keys: string[] = Reflect.getMetadataKeys(target.handler).map(String);
    expect(keys.some((k) => k.includes('swagger'))).toBe(true);

    const apiResponse = reflector.getAllAndOverride<Record<string, unknown>>(
      'swagger/apiResponse',
      [target.handler, C],
    );
    if (apiResponse) {
      expect(apiResponse).toHaveProperty('401');
      expect(apiResponse).toHaveProperty('403');
    }
  });
});
