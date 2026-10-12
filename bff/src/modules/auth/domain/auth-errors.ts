import { DomainError } from '@shared/domain/domain-error';

export class InvalidTokenError extends DomainError {
  readonly code = 'INVALID_TOKEN';
  constructor(message = 'Token inválido ou ausente.') {
    super(message);
  }
}

export class InsufficientRoleError extends DomainError {
  readonly code = 'INSUFFICIENT_ROLE';
  constructor(message = 'Permissão Negada!') {
    super(message);
  }
}
