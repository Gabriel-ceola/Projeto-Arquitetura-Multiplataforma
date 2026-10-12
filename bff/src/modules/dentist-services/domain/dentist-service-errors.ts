import { DomainError } from '@shared/domain/domain-error';

export class DentistServiceNotFoundError extends DomainError {
  readonly code = 'RESOURCE_NOT_FOUND';
  constructor(id?: string) {
    super(id ? `Serviço ${id} não encontrado.` : 'Serviço não encontrado.');
  }
}

export class DentistServiceForbiddenError extends DomainError {
  readonly code = 'RESOURCE_FORBIDDEN';
  constructor(message = 'Acesso negado ao serviço.') {
    super(message);
  }
}
