import { ExecutionContext } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { JwtAuthGuard } from './jwt-auth.guard';
import { RolesGuard } from './roles.guard';
import { Role } from '../domain/role.enum';
import {
  InvalidTokenError,
  InsufficientRoleError,
} from '../domain/auth-errors';
import { Public } from './decorators/public.decorator';
import { Roles } from './decorators/roles.decorator';

function fakeContext(
  handler: (...args: never[]) => unknown,
  clazz: new (...args: never[]) => unknown,
  request: Record<string, unknown>,
): ExecutionContext {
  return {
    getHandler: () => handler,
    getClass: () => clazz,
    switchToHttp: () => ({ getRequest: () => request }),
  } as unknown as ExecutionContext;
}

describe('JwtAuthGuard', () => {
  const reflector = new Reflector();

  class PrivadoController {
    handler() {
      return 'ok';
    }
  }

  class PublicoController {
    @Public()
    handler() {
      return 'ok';
    }
  }

  it('rota @Public libera sem chamar o use-case', async () => {
    const execute = jest.fn();
    const guard = new JwtAuthGuard(reflector, {
      execute,
    } as unknown as never);
    const request = { headers: {} };
    const ctx = fakeContext(
      new PublicoController().handler,
      PublicoController,
      request,
    );

    await expect(guard.canActivate(ctx)).resolves.toBe(true);
    expect(execute).not.toHaveBeenCalled();
  });

  it('sem token propaga InvalidTokenError (401)', async () => {
    const execute = jest.fn().mockRejectedValue(new InvalidTokenError());
    const guard = new JwtAuthGuard(reflector, {
      execute,
    } as unknown as never);
    const request = { headers: {} };
    const target = new PrivadoController();
    const ctx = fakeContext(target.handler, PrivadoController, request);

    await expect(guard.canActivate(ctx)).rejects.toBeInstanceOf(
      InvalidTokenError,
    );
  });

  it('token válido grava request.user e libera', async () => {
    const user = { id: 'dentist-1', roles: [Role.Dentist] };
    const execute = jest.fn().mockResolvedValue(user);
    const guard = new JwtAuthGuard(reflector, {
      execute,
    } as unknown as never);
    const request: Record<string, unknown> = {
      headers: { authorization: 'Bearer token-valido' },
    };
    const target = new PrivadoController();
    const ctx = fakeContext(target.handler, PrivadoController, request);

    await expect(guard.canActivate(ctx)).resolves.toBe(true);
    expect(execute).toHaveBeenCalledWith({
      authorizationHeader: 'Bearer token-valido',
    });
    expect(request['user']).toEqual(user);
  });
});

describe('RolesGuard', () => {
  const reflector = new Reflector();
  const guard = new RolesGuard(reflector);

  class SemRolesController {
    handler() {
      return 'ok';
    }
  }

  class DentistaController {
    @Roles(Role.Dentist)
    handler() {
      return 'ok';
    }
  }

  class OuController {
    @Roles(Role.Dentist, Role.Pacient)
    handler() {
      return 'ok';
    }
  }

  @Roles(Role.Dentist)
  class ClasseComOverride {
    @Roles(Role.Pacient)
    handler() {
      return 'ok';
    }
  }

  class PublicoSemRoles {
    @Public()
    handler() {
      return 'ok';
    }
  }

  it('sem metadata de roles nega (fail-closed) com InsufficientRoleError', () => {
    const target = new SemRolesController();
    const ctx = fakeContext(target.handler, SemRolesController, {
      user: { id: 'u', roles: [Role.Dentist] },
    });

    expect(() => guard.canActivate(ctx)).toThrow(InsufficientRoleError);
  });

  it('role correta permite', () => {
    const target = new DentistaController();
    const ctx = fakeContext(target.handler, DentistaController, {
      user: { id: 'u', roles: [Role.Dentist] },
    });

    expect(guard.canActivate(ctx)).toBe(true);
  });

  it('role errada lança InsufficientRoleError (403)', () => {
    const target = new DentistaController();
    const ctx = fakeContext(target.handler, DentistaController, {
      user: { id: 'u', roles: [Role.Pacient] },
    });

    let err: unknown;
    try {
      guard.canActivate(ctx);
    } catch (e) {
      err = e;
    }
    expect(err).toBeInstanceOf(InsufficientRoleError);
    expect((err as InsufficientRoleError).code).toBe('INSUFFICIENT_ROLE');
  });

  it('@Roles(a, b) com usuário só com b permite (OU)', () => {
    const target = new OuController();
    const ctx = fakeContext(target.handler, OuController, {
      user: { id: 'u', roles: [Role.Pacient] },
    });

    expect(guard.canActivate(ctx)).toBe(true);
  });

  it('metadata de método sobrepõe a de classe', () => {
    const target = new ClasseComOverride();
    const ctxPacient = fakeContext(target.handler, ClasseComOverride, {
      user: { id: 'u', roles: [Role.Pacient] },
    });
    const ctxDentista = fakeContext(target.handler, ClasseComOverride, {
      user: { id: 'u', roles: [Role.Dentist] },
    });

    expect(guard.canActivate(ctxPacient)).toBe(true);
    expect(() => guard.canActivate(ctxDentista)).toThrow(InsufficientRoleError);
  });

  it('@Public libera mesmo sem metadata de roles', () => {
    const target = new PublicoSemRoles();
    const ctx = fakeContext(target.handler, PublicoSemRoles, {});

    expect(guard.canActivate(ctx)).toBe(true);
  });
});
