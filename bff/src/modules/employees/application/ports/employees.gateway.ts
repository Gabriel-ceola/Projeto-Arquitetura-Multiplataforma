import { Employee } from '../../domain/employee';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';

export const EMPLOYEES_GATEWAY = Symbol('EMPLOYEES_GATEWAY');

export interface CreateEmployeeInput {
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: EmployeeRole;
  status: EmployeeStatus;
}

/** O CPF é imutável no Employee-service, por isso não faz parte da edição. */
export type UpdateEmployeeInput = Omit<CreateEmployeeInput, 'cpf'>;

/** Porta OUT: contrato com o Employee-service (adaptador HTTP na infraestrutura). */
export interface EmployeesGateway {
  list(): Promise<Employee[]>;
  getById(id: string): Promise<Employee>;
  create(input: CreateEmployeeInput): Promise<Employee>;
  update(id: string, input: UpdateEmployeeInput): Promise<Employee>;
  delete(id: string): Promise<void>;
}
