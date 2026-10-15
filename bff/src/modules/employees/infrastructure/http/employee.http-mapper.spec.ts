import { toResponseDto } from './employee.http-mapper';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

describe('toResponseDto (employees)', () => {
  it('converte datas para ISO UTC e preserva os campos', () => {
    const entity = anEmployee({
      id: '11111111-1111-4111-8111-111111111111',
      createdAt: new Date('2026-01-01T10:00:00.000Z'),
      updatedAt: new Date('2026-02-01T10:00:00.000Z'),
    });

    const result = toResponseDto(entity);

    expect({ ...result }).toEqual({
      id: '11111111-1111-4111-8111-111111111111',
      name: entity.name,
      cpf: entity.cpf,
      email: entity.email,
      phone: entity.phone,
      role: entity.role,
      status: entity.status,
      createdAt: '2026-01-01T10:00:00.000Z',
      updatedAt: '2026-02-01T10:00:00.000Z',
    });
  });
});
