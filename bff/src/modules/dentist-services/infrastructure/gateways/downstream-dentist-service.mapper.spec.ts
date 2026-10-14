import {
  fromDownstream,
  DownstreamDentistServiceDto,
} from './downstream-dentist-service.mapper';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';

const dto = (overrides: Partial<DownstreamDentistServiceDto> = {}) => ({
  id: '11111111-1111-4111-8111-111111111111',
  name: 'Limpeza',
  dentistId: 'dentist-1',
  price: 100,
  durationMinutes: 30,
  isPeriodic: false,
  status: DentistServiceStatus.Disponivel,
  createdAt: '2026-01-01T10:00:00.000Z',
  updatedAt: '2026-02-01T10:00:00.000Z',
  ...overrides,
});

describe('fromDownstream', () => {
  it('converte createdAt/updatedAt para Date', () => {
    const result = fromDownstream(dto());

    expect(result.createdAt).toBeInstanceOf(Date);
    expect(result.updatedAt).toBeInstanceOf(Date);
    expect(result.createdAt.toISOString()).toBe('2026-01-01T10:00:00.000Z');
    expect(result.updatedAt.toISOString()).toBe('2026-02-01T10:00:00.000Z');
  });

  it('preserva todos os campos e o status', () => {
    const result = fromDownstream(dto());

    expect(result).toEqual({
      id: '11111111-1111-4111-8111-111111111111',
      name: 'Limpeza',
      dentistId: 'dentist-1',
      price: 100,
      durationMinutes: 30,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
      createdAt: new Date('2026-01-01T10:00:00.000Z'),
      updatedAt: new Date('2026-02-01T10:00:00.000Z'),
    });
  });

  it('ignora campos extras desconhecidos', () => {
    const withExtras = {
      ...dto(),
      extraDesconhecido: 'x',
      outro: 123,
    } as unknown as DownstreamDentistServiceDto;

    const result = fromDownstream(withExtras);

    expect(result).not.toHaveProperty('extraDesconhecido');
    expect(result).not.toHaveProperty('outro');
  });
});
