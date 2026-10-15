import { EmployeeRole } from './employee-role.enum';
import { EmployeeStatus } from './employee-status.enum';

export interface Employee {
  id: string;
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: EmployeeRole;
  status: EmployeeStatus;
  createdAt: Date;
  updatedAt: Date;
}
