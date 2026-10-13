import { EmployeeNotFoundError } from '../../domain/employee-errors';
import { DeleteEmployeeUseCase } from './delete-employee.use-case';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

describe('DeleteEmployeeUseCase', () => {
  it('remove o funcionário e resolve void', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);
    const sut = new DeleteEmployeeUseCase(gateway);

    // Act
    const resultado = await sut.execute({ id: employee.id });

    // Assert
    expect(resultado).toBeUndefined();
    expect(gateway.calls.delete).toEqual([[employee.id]]);
    expect(gateway.items.has(employee.id)).toBe(false);
  });

  it('lança NotFound quando o funcionário não existe', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new DeleteEmployeeUseCase(gateway);

    // Act
    const promessa = sut.execute({ id: 'inexistente' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(EmployeeNotFoundError);
  });
});
