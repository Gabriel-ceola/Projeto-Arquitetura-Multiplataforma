import { DomainError } from './domain-error';
import {
  DownstreamTimeoutError,
  DownstreamUnavailableError,
  DownstreamValidationError,
} from './common-errors';
import {
  InsufficientRoleError,
  InvalidTokenError,
} from '../../modules/auth/domain/auth-errors';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../modules/dentist-services/domain/dentist-service-errors';

describe('DomainErrors', () => {
  it.each([
    {
      nome: 'InvalidTokenError',
      criar: () => new InvalidTokenError(),
      code: 'INVALID_TOKEN',
    },
    {
      nome: 'InsufficientRoleError',
      criar: () => new InsufficientRoleError(),
      code: 'INSUFFICIENT_ROLE',
    },
    {
      nome: 'DentistServiceNotFoundError',
      criar: () => new DentistServiceNotFoundError('id-1'),
      code: 'RESOURCE_NOT_FOUND',
    },
    {
      nome: 'DentistServiceForbiddenError',
      criar: () => new DentistServiceForbiddenError(),
      code: 'RESOURCE_FORBIDDEN',
    },
    {
      nome: 'DownstreamValidationError',
      criar: () => new DownstreamValidationError('rejeitado'),
      code: 'DOWNSTREAM_BAD_REQUEST',
    },
    {
      nome: 'DownstreamUnavailableError',
      criar: () => new DownstreamUnavailableError(),
      code: 'DOWNSTREAM_UNAVAILABLE',
    },
    {
      nome: 'DownstreamTimeoutError',
      criar: () => new DownstreamTimeoutError(),
      code: 'DOWNSTREAM_TIMEOUT',
    },
  ])(
    'expõe code correto e herda de DomainError e Error: $nome',
    ({ criar, code }) => {
      // Arrange
      const erro = criar();

      // Act
      const codigo = erro.code;

      // Assert
      expect(codigo).toBe(code);
      expect(erro).toBeInstanceOf(DomainError);
      expect(erro).toBeInstanceOf(Error);
    },
  );
});
