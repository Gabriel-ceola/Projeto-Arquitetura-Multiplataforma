import { DomainError } from '@shared/domain/domain-error';
import {
  EmployeeConflictError,
  EmployeeNotFoundError,
} from './employee-errors';

describe('Employee errors', () => {
  it('EmployeeNotFoundError usa RESOURCE_NOT_FOUND e cita o id', () => {
    // Arrange & Act
    const erro = new EmployeeNotFoundError('id-1');

    // Assert
    expect(erro).toBeInstanceOf(DomainError);
    expect(erro.code).toBe('RESOURCE_NOT_FOUND');
    expect(erro.message).toBe('Funcionário id-1 não encontrado.');
  });

  it('EmployeeNotFoundError sem id usa mensagem genérica', () => {
    // Arrange & Act
    const erro = new EmployeeNotFoundError();

    // Assert
    expect(erro.message).toBe('Funcionário não encontrado.');
  });

  it('EmployeeConflictError usa RESOURCE_CONFLICT com mensagem padrão', () => {
    // Arrange & Act
    const erro = new EmployeeConflictError();

    // Assert
    expect(erro).toBeInstanceOf(DomainError);
    expect(erro.code).toBe('RESOURCE_CONFLICT');
    expect(erro.message).toContain('CPF');
  });

  it('EmployeeConflictError aceita mensagem do downstream', () => {
    // Arrange & Act
    const erro = new EmployeeConflictError('CPF duplicado');

    // Assert
    expect(erro.message).toBe('CPF duplicado');
  });
});
