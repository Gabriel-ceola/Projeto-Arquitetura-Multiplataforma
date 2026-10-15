import { randomUUID } from 'crypto';
import type { Employee } from '../../../src/modules/employees/domain/employee';
import { EmployeeRole } from '../../../src/modules/employees/domain/employee-role.enum';
import { EmployeeStatus } from '../../../src/modules/employees/domain/employee-status.enum';

export function anEmployee(overrides: Partial<Employee> = {}): Employee {
  const now = new Date('2026-01-01T00:00:00.000Z');
  return {
    id: randomUUID(),
    name: 'Maria Souza',
    cpf: '12345678901',
    email: 'maria.souza@clinica.com',
    phone: '43999998888',
    role: EmployeeRole.Recepcionista,
    status: EmployeeStatus.Ativo,
    createdAt: now,
    updatedAt: now,
    ...overrides,
  };
}
