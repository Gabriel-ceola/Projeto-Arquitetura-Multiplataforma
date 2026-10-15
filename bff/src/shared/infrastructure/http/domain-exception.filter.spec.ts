import { ArgumentsHost, BadRequestException } from '@nestjs/common';
import {
  DomainExceptionFilter,
  DOMAIN_ERROR_HTTP_STATUS,
} from './domain-exception.filter';
import {
  InvalidTokenError,
  InsufficientRoleError,
} from '@modules/auth/domain/auth-errors';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '@modules/dentist-services/domain/dentist-service-errors';
import {
  DownstreamValidationError,
  DownstreamUnavailableError,
  DownstreamTimeoutError,
} from '@shared/domain/common-errors';
import { DomainError } from '@shared/domain/domain-error';

class TestConflictError extends DomainError {
  readonly code = 'RESOURCE_CONFLICT';
}

function mockHost() {
  const json = jest.fn();
  const status = jest.fn().mockReturnValue({ json });
  const response = { status };
  const request = { url: '/teste' };
  const host = {
    switchToHttp: () => ({
      getResponse: () => response,
      getRequest: () => request,
    }),
  } as unknown as ArgumentsHost;
  return { host, json, status };
}

describe('DomainExceptionFilter', () => {
  const filter = new DomainExceptionFilter();

  it.each([
    ['INVALID_TOKEN', new InvalidTokenError('token ruim'), 401],
    ['INSUFFICIENT_ROLE', new InsufficientRoleError(), 403],
    ['RESOURCE_FORBIDDEN', new DentistServiceForbiddenError(), 403],
    ['RESOURCE_NOT_FOUND', new DentistServiceNotFoundError('x'), 404],
    ['RESOURCE_CONFLICT', new TestConflictError('conflito'), 409],
    ['DOWNSTREAM_BAD_REQUEST', new DownstreamValidationError('inválido'), 400],
    ['DOWNSTREAM_UNAVAILABLE', new DownstreamUnavailableError(), 502],
    ['DOWNSTREAM_TIMEOUT', new DownstreamTimeoutError(), 504],
  ])(
    'code %s responde status %i com corpo { message }',
    (_code, err, httpStatus) => {
      const { host, json, status } = mockHost();

      filter.catch(err, host);

      expect(DOMAIN_ERROR_HTTP_STATUS[(err as { code: string }).code]).toBe(
        httpStatus,
      );
      expect(status).toHaveBeenCalledWith(httpStatus);
      expect(json).toHaveBeenCalledWith({ message: (err as Error).message });
    },
  );

  it('HttpException com message array responde string única', () => {
    const { host, json, status } = mockHost();

    filter.catch(new BadRequestException(['msg1', 'msg2']), host);

    expect(status).toHaveBeenCalledWith(400);
    expect(json).toHaveBeenCalledWith({ message: 'msg1, msg2' });
  });

  it('erro desconhecido responde 500 "Erro interno." sem stack no corpo', () => {
    const { host, json, status } = mockHost();

    filter.catch(new Error('boom'), host);

    expect(status).toHaveBeenCalledWith(500);
    expect(json).toHaveBeenCalledWith({ message: 'Erro interno.' });
    const body = json.mock.calls[0][0] as Record<string, unknown>;
    expect(body).not.toHaveProperty('stack');
    expect(Object.keys(body)).toEqual(['message']);
  });
});
