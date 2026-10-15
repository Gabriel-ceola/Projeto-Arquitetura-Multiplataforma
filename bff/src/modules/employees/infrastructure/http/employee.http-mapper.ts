import { Employee } from '../../domain/employee';
import { EmployeeResponseDto } from './dto/employee.response.dto';

export function toResponseDto(entity: Employee): EmployeeResponseDto {
  const dto = new EmployeeResponseDto();
  dto.id = entity.id;
  dto.name = entity.name;
  dto.cpf = entity.cpf;
  dto.email = entity.email;
  dto.phone = entity.phone;
  dto.role = entity.role;
  dto.status = entity.status;
  dto.createdAt = entity.createdAt.toISOString();
  dto.updatedAt = entity.updatedAt.toISOString();
  return dto;
}
