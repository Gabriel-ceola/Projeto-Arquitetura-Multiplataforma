import { randomUUID } from 'crypto';
import type { DentistService } from '../../../src/modules/dentist-services/domain/dentist-service';
import { DentistServiceStatus } from '../../../src/modules/dentist-services/domain/dentist-service-status.enum';

export function aDentistService(
  overrides: Partial<DentistService> = {},
): DentistService {
  const now = new Date('2026-01-01T00:00:00.000Z');
  return {
    id: randomUUID(),
    name: 'Limpeza completa',
    dentistId: 'dentist-A',
    price: 150,
    isPeriodic: false,
    status: DentistServiceStatus.Disponivel,
    createdAt: now,
    updatedAt: now,
    ...overrides,
  };
}
