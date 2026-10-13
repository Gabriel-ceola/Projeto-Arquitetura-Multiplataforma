import {
  DownstreamEmployeeDto,
  fromDownstream,
} from './downstream-employee.mapper';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';

const dto = (overrides: Partial<DownstreamEmployeeDto> = {}) => ({
  id: '11111111-1111-4111-8111-111111111111',
  name: 'Maria Souza',
  cpf: '12345678901',
  email: 'maria.souza@clinica.com',
  phone: '43999998888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
  createdAt: '2026-01-01T10:00:00.000Z',
  updatedAt: '2026-02-01T10:00:00.000Z',
  ...overrides,
});

describe('fromDownstream (employees)', () => {
  it('converte createdAt/updatedAt para Date', () => {
    const result = fromDownstream(dto());

    expect(result.createdAt).toBeInstanceOf(Date);
    expect(result.updatedAt.toISOString()).toBe('2026-02-01T10:00:00.000Z');
  });

  it('preserva todos os campos, cargo e status', () => {
    const result = fromDownstream(dto());

    expect(result).toEqual({
      id: '11111111-1111-4111-8111-111111111111',
      name: 'Maria Souza',
      cpf: '12345678901',
      email: 'maria.souza@clinica.com',
      phone: '43999998888',
      role: EmployeeRole.Recepcionista,
      status: EmployeeStatus.Ativo,
      createdAt: new Date('2026-01-01T10:00:00.000Z'),
      updatedAt: new Date('2026-02-01T10:00:00.000Z'),
    });
  });

  it('ignora campos extras desconhecidos', () => {
    const withExtras = {
      ...dto(),
      deletedAt: null,
    } as unknown as DownstreamEmployeeDto;

    const result = fromDownstream(withExtras);

    expect(result).not.toHaveProperty('deletedAt');
  });
});
