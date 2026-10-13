import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { EMPLOYEES_GATEWAY } from '../ports/employees.gateway';
import type { EmployeesGateway } from '../ports/employees.gateway';

export interface DeleteEmployeeInput {
  id: string;
}

@Injectable()
export class DeleteEmployeeUseCase implements UseCase<
  DeleteEmployeeInput,
  void
> {
  constructor(
    @Inject(EMPLOYEES_GATEWAY)
    private readonly gateway: EmployeesGateway,
  ) {}

  execute(input: DeleteEmployeeInput): Promise<void> {
    return this.gateway.delete(input.id);
  }
}
