import { DomainError } from '@shared/domain/domain-error';

export class EmployeeNotFoundError extends DomainError {
  readonly code = 'RESOURCE_NOT_FOUND';
  constructor(id?: string) {
    super(
      id ? `Funcionário ${id} não encontrado.` : 'Funcionário não encontrado.',
    );
  }
}

export class EmployeeConflictError extends DomainError {
  readonly code = 'RESOURCE_CONFLICT';
  constructor(
    message = 'Já existe um funcionário cadastrado com o CPF informado.',
  ) {
    super(message);
  }
}
