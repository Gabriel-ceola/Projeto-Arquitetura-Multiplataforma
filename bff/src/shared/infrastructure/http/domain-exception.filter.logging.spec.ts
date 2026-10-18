import {
  ArgumentsHost,
  ForbiddenException,
  Logger,
  NotFoundException,
} from '@nestjs/common';
import { ThrottlerException } from '@nestjs/throttler';
import { InvalidTokenError } from '@modules/auth/domain/auth-errors';
import { DownstreamTimeoutError } from '@shared/domain/common-errors';
import { DomainExceptionFilter } from './domain-exception.filter';

function mockHost(request: Record<string, unknown>): ArgumentsHost {
  const response = { status: jest.fn().mockReturnValue({ json: jest.fn() }) };
  return {
    switchToHttp: () => ({
      getResponse: () => response,
      getRequest: () => request,
    }),
  } as unknown as ArgumentsHost;
}

describe('DomainExceptionFilter - eventos de log', () => {
  const filter = new DomainExceptionFilter();
  let warn: jest.SpyInstance;
  let error: jest.SpyInstance;

  beforeEach(() => {
    warn = jest.spyOn(Logger.prototype, 'warn').mockImplementation();
    error = jest.spyOn(Logger.prototype, 'error').mockImplementation();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('deve registrar auth.denied com o código do erro em 401', () => {
    filter.catch(
      new InvalidTokenError('token ruim'),
      mockHost({ url: '/api/x?cpf=123' }),
    );

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({
        event: { action: 'auth.denied' },
        error: { code: 'INVALID_TOKEN' },
        url: { path: '/api/x' },
        http: { response: { status_code: 401 } },
      }),
      'Acesso negado',
    );
  });

  it('deve registrar auth.denied para ForbiddenException do framework', () => {
    filter.catch(new ForbiddenException(), mockHost({ url: '/api/x' }));

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({ event: { action: 'auth.denied' } }),
      'Acesso negado',
    );
  });

  it('deve registrar http.rate_limited com o IP do cliente em 429', () => {
    filter.catch(
      new ThrottlerException(),
      mockHost({ url: '/api/x', ip: '10.0.0.1' }),
    );

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({
        event: { action: 'http.rate_limited' },
        client: { ip: '10.0.0.1' },
      }),
      'Limite de requisições excedido',
    );
  });

  it('não deve registrar eventos de segurança para 404', () => {
    filter.catch(new NotFoundException(), mockHost({ url: '/api/x' }));

    expect(warn).not.toHaveBeenCalled();
    expect(error).not.toHaveBeenCalled();
  });

  it('não deve duplicar o log de erros downstream mapeados (já logados no cliente)', () => {
    filter.catch(new DownstreamTimeoutError(), mockHost({ url: '/api/x' }));

    expect(warn).not.toHaveBeenCalled();
    expect(error).not.toHaveBeenCalled();
  });

  it('deve registrar erro para exceção não tratada', () => {
    filter.catch(new Error('boom'), mockHost({ url: '/api/x' }));

    expect(error).toHaveBeenCalledWith(
      expect.objectContaining({ path: '/api/x' }),
      'Erro não tratado',
    );
  });
});
