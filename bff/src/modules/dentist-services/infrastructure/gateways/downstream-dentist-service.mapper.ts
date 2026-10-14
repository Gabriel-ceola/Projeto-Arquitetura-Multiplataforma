import { DentistService } from '../../domain/dentist-service';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';

export interface DownstreamDentistServiceDto {
  id: string;
  name: string;
  dentistId: string;
  price: number;
  durationMinutes?: number;
  isPeriodic: boolean;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export function fromDownstream(
  dto: DownstreamDentistServiceDto,
): DentistService {
  return {
    id: dto.id,
    name: dto.name,
    dentistId: dto.dentistId,
    price: dto.price,
    durationMinutes: dto.durationMinutes ?? 30,
    isPeriodic: dto.isPeriodic,
    status: dto.status as DentistServiceStatus,
    createdAt: new Date(dto.createdAt),
    updatedAt: new Date(dto.updatedAt),
  };
}
