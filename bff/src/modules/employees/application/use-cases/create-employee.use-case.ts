import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { Employee } from '../../domain/employee';
import { EMPLOYEES_GATEWAY } from '../ports/employees.gateway';
import type {
  CreateEmployeeInput,
  EmployeesGateway,
} from '../ports/employees.gateway';

@Injectable()
export class CreateEmployeeUseCase implements UseCase<
  CreateEmployeeInput,
  Employee
> {
  constructor(
    @Inject(EMPLOYEES_GATEWAY)
    private readonly gateway: EmployeesGateway,
  ) {}

  execute(input: CreateEmployeeInput): Promise<Employee> {
    return this.gateway.create(input);
  }
}
