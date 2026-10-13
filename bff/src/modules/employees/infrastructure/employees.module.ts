import { Module } from '@nestjs/common';
import { DownstreamHttpClient } from '@shared/infrastructure/http/downstream-http-client';
import { DownstreamHttpModule } from '@shared/infrastructure/http/downstream-http.module';
import { HttpEmployeesGateway } from './gateways/http-employees.gateway';
import { EMPLOYEES_GATEWAY } from '../application/ports/employees.gateway';
import { ListEmployeesUseCase } from '../application/use-cases/list-employees.use-case';
import { GetEmployeeByIdUseCase } from '../application/use-cases/get-employee-by-id.use-case';
import { CreateEmployeeUseCase } from '../application/use-cases/create-employee.use-case';
import { UpdateEmployeeUseCase } from '../application/use-cases/update-employee.use-case';
import { DeleteEmployeeUseCase } from '../application/use-cases/delete-employee.use-case';
import { EmployeesController } from './http/employees.controller';

/** Token for the downstream HTTP client targeting the employee-service microservice. */
export const EMPLOYEE_HTTP_CLIENT = Symbol('EMPLOYEE_HTTP_CLIENT');

@Module({
  imports: [
    DownstreamHttpModule.forClient(
      EMPLOYEE_HTTP_CLIENT,
      'EMPLOYEE_SERVICE_BASE_URL',
    ),
  ],
  controllers: [EmployeesController],
  providers: [
    {
      provide: EMPLOYEES_GATEWAY,
      useFactory: (http: DownstreamHttpClient) =>
        new HttpEmployeesGateway(http),
      inject: [EMPLOYEE_HTTP_CLIENT],
    },
    ListEmployeesUseCase,
    GetEmployeeByIdUseCase,
    CreateEmployeeUseCase,
    UpdateEmployeeUseCase,
    DeleteEmployeeUseCase,
  ],
})
export class EmployeesModule {}
