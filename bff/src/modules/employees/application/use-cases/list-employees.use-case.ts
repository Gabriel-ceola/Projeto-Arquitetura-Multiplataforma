import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { Employee } from '../../domain/employee';
import { EMPLOYEES_GATEWAY } from '../ports/employees.gateway';
import type { EmployeesGateway } from '../ports/employees.gateway';

@Injectable()
export class ListEmployeesUseCase implements UseCase<void, Employee[]> {
  constructor(
    @Inject(EMPLOYEES_GATEWAY)
    private readonly gateway: EmployeesGateway,
  ) {}

  execute(): Promise<Employee[]> {
    return this.gateway.list();
  }
}
