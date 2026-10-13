import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { Employee } from '../../domain/employee';
import { EMPLOYEES_GATEWAY } from '../ports/employees.gateway';
import type {
  EmployeesGateway,
  UpdateEmployeeInput,
} from '../ports/employees.gateway';

export interface UpdateEmployeeUseCaseInput extends UpdateEmployeeInput {
  id: string;
}

@Injectable()
export class UpdateEmployeeUseCase implements UseCase<
  UpdateEmployeeUseCaseInput,
  Employee
> {
  constructor(
    @Inject(EMPLOYEES_GATEWAY)
    private readonly gateway: EmployeesGateway,
  ) {}

  execute(input: UpdateEmployeeUseCaseInput): Promise<Employee> {
    const { id, ...updateData } = input;
    return this.gateway.update(id, updateData);
  }
}
