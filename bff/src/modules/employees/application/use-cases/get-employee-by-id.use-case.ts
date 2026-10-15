import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { Employee } from '../../domain/employee';
import { EMPLOYEES_GATEWAY } from '../ports/employees.gateway';
import type { EmployeesGateway } from '../ports/employees.gateway';

export interface GetEmployeeByIdInput {
  id: string;
}

@Injectable()
export class GetEmployeeByIdUseCase implements UseCase<
  GetEmployeeByIdInput,
  Employee
> {
  constructor(
    @Inject(EMPLOYEES_GATEWAY)
    private readonly gateway: EmployeesGateway,
  ) {}

  execute(input: GetEmployeeByIdInput): Promise<Employee> {
    return this.gateway.getById(input.id);
  }
}
