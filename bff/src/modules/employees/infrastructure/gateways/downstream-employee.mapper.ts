import { Employee } from '../../domain/employee';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';

/** Formato devolvido pelo Employee-service (.NET). */
export interface DownstreamEmployeeDto {
  id: string;
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export function fromDownstream(dto: DownstreamEmployeeDto): Employee {
  return {
    id: dto.id,
    name: dto.name,
    cpf: dto.cpf,
    email: dto.email,
    phone: dto.phone,
    role: dto.role as EmployeeRole,
    status: dto.status as EmployeeStatus,
    createdAt: new Date(dto.createdAt),
    updatedAt: new Date(dto.updatedAt),
  };
}
