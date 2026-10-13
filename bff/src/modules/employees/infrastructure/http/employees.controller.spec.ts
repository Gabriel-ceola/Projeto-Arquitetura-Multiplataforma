import { Reflector } from '@nestjs/core';
import { Role } from '@modules/auth/domain/role.enum';
import { ROLES_KEY } from '@modules/auth/infrastructure/decorators/roles.decorator';
import { EmployeesController } from './employees.controller';
import { ListEmployeesUseCase } from '../../application/use-cases/list-employees.use-case';
import { GetEmployeeByIdUseCase } from '../../application/use-cases/get-employee-by-id.use-case';
import { CreateEmployeeUseCase } from '../../application/use-cases/create-employee.use-case';
import { UpdateEmployeeUseCase } from '../../application/use-cases/update-employee.use-case';
import { DeleteEmployeeUseCase } from '../../application/use-cases/delete-employee.use-case';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

function buildController(gateway = new FakeEmployeesGateway()) {
  const controller = new EmployeesController(
    new ListEmployeesUseCase(gateway),
    new GetEmployeeByIdUseCase(gateway),
    new CreateEmployeeUseCase(gateway),
    new UpdateEmployeeUseCase(gateway),
    new DeleteEmployeeUseCase(gateway),
  );
  return { controller, gateway };
}

describe('EmployeesController', () => {
  it('exige a role employ em todas as rotas (RBAC)', () => {
    // Arrange
    const reflector = new Reflector();

    // Act
    const roles = reflector.get<Role[]>(ROLES_KEY, EmployeesController);

    // Assert
    expect(roles).toEqual([Role.Employ]);
  });

  it('list devolve os funcionários no formato de resposta', async () => {
    // Arrange
    const { controller, gateway } = buildController();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);

    // Act
    const result = await controller.list();

    // Assert
    expect(result).toHaveLength(1);
    expect(result[0].createdAt).toBe(employee.createdAt.toISOString());
  });

  it('getById devolve o funcionário pedido', async () => {
    // Arrange
    const { controller, gateway } = buildController();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);

    // Act
    const result = await controller.getById(employee.id);

    // Assert
    expect(result.id).toBe(employee.id);
  });

  it('create repassa somente os campos do DTO ao caso de uso', async () => {
    // Arrange
    const { controller, gateway } = buildController();
    const dto = {
      name: 'Maria Souza',
      cpf: '12345678901',
      email: 'maria.souza@clinica.com',
      phone: '43999998888',
      role: EmployeeRole.Recepcionista,
      status: EmployeeStatus.Ativo,
    };

    // Act
    const result = await controller.create(dto);

    // Assert
    expect(gateway.calls.create).toEqual([[dto]]);
    expect(result.cpf).toBe('12345678901');
  });

  it('update repassa id e campos editáveis', async () => {
    // Arrange
    const { controller, gateway } = buildController();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);
    const dto = {
      name: 'Maria Souza Lima',
      email: 'maria.lima@clinica.com',
      phone: '4333334444',
      role: EmployeeRole.Administrativo,
      status: EmployeeStatus.Ferias,
    };

    // Act
    const result = await controller.update(employee.id, dto);

    // Assert
    expect(gateway.calls.update).toEqual([[employee.id, dto]]);
    expect(result.status).toBe(EmployeeStatus.Ferias);
  });

  it('delete remove o funcionário', async () => {
    // Arrange
    const { controller, gateway } = buildController();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);

    // Act
    await controller.delete(employee.id);

    // Assert
    expect(gateway.items.has(employee.id)).toBe(false);
  });
});
