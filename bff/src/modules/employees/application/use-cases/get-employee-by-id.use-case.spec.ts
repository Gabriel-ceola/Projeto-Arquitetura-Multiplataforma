import { EmployeeNotFoundError } from '../../domain/employee-errors';
import { GetEmployeeByIdUseCase } from './get-employee-by-id.use-case';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

describe('GetEmployeeByIdUseCase', () => {
  it('retorna o funcionário quando ele existe', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);
    const sut = new GetEmployeeByIdUseCase(gateway);

    // Act
    const resultado = await sut.execute({ id: employee.id });

    // Assert
    expect(resultado).toEqual(employee);
    expect(gateway.calls.getById).toEqual([[employee.id]]);
  });

  it('lança NotFound quando o funcionário não existe', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new GetEmployeeByIdUseCase(gateway);

    // Act
    const promessa = sut.execute({ id: 'inexistente' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(EmployeeNotFoundError);
  });
});
