import { toResponseDto } from './dentist-service.http-mapper';
import { DentistService } from '../../domain/dentist-service';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';

const entity = (overrides: Partial<DentistService> = {}): DentistService => ({
  id: '11111111-1111-4111-8111-111111111111',
  name: 'Limpeza',
  dentistId: 'dentist-1',
  price: 100,
  durationMinutes: 30,
  isPeriodic: false,
  status: DentistServiceStatus.Disponivel,
  createdAt: new Date('2026-01-01T10:00:00.000Z'),
  updatedAt: new Date('2026-02-01T10:00:00.000Z'),
  ...overrides,
});

describe('toResponseDto', () => {
  it('converte datas para ISO UTC', () => {
    const result = toResponseDto(entity());

    expect(result.createdAt).toBe('2026-01-01T10:00:00.000Z');
    expect(result.updatedAt).toBe('2026-02-01T10:00:00.000Z');
  });

  it('preserva todos os campos no shape do DentistServiceResponse', () => {
    const result = toResponseDto(entity());

    expect(result).toEqual({
      id: '11111111-1111-4111-8111-111111111111',
      name: 'Limpeza',
      dentistId: 'dentist-1',
      price: 100,
      durationMinutes: 30,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
      createdAt: '2026-01-01T10:00:00.000Z',
      updatedAt: '2026-02-01T10:00:00.000Z',
    });
  });
});
