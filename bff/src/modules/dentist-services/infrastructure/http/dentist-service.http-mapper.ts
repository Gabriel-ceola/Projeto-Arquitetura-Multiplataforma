import { DentistService } from '../../domain/dentist-service';
import { DentistServiceResponseDto } from './dto/dentist-service.response.dto';

export function toResponseDto(
  entity: DentistService,
): DentistServiceResponseDto {
  const dto = new DentistServiceResponseDto();
  dto.id = entity.id;
  dto.name = entity.name;
  dto.dentistId = entity.dentistId;
  dto.price = entity.price;
  dto.isPeriodic = entity.isPeriodic;
  dto.status = entity.status;
  dto.createdAt = entity.createdAt.toISOString();
  dto.updatedAt = entity.updatedAt.toISOString();
  return dto;
}
