import { DownstreamValidationError } from '../../../../shared/domain/common-errors';
import { EmployeeNotFoundError } from '../../domain/employee-errors';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';
import { UpdateEmployeeUseCase } from './update-employee.use-case';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

const payload = () => ({
  name: 'Maria Souza Lima',
  email: 'maria.lima@clinica.com',
  phone: '4333334444',
  role: EmployeeRole.Administrativo,
  status: EmployeeStatus.Ferias,
});

describe('UpdateEmployeeUseCase', () => {
  it('encaminha id e payload (sem id) ao gateway e retorna o atualizado', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);
    const sut = new UpdateEmployeeUseCase(gateway);

    // Act
    const resultado = await sut.execute({ id: employee.id, ...payload() });

    // Assert
    expect(gateway.calls.update).toEqual([[employee.id, payload()]]);
    expect(resultado.name).toBe('Maria Souza Lima');
    expect(resultado.status).toBe(EmployeeStatus.Ferias);
  });

  it('preserva o CPF do funcionário', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const employee = anEmployee({ cpf: '98765432100' });
    gateway.items.set(employee.id, employee);
    const sut = new UpdateEmployeeUseCase(gateway);

    // Act
    const resultado = await sut.execute({ id: employee.id, ...payload() });

    // Assert
    expect(resultado.cpf).toBe('98765432100');
  });

  it('lança NotFound quando o funcionário não existe', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new UpdateEmployeeUseCase(gateway);

    // Act
    const promessa = sut.execute({ id: 'inexistente', ...payload() });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(EmployeeNotFoundError);
  });

  it('propaga erro de validação lançado pelo gateway', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const employee = anEmployee();
    gateway.items.set(employee.id, employee);
    gateway.failNext(
      'update',
      new DownstreamValidationError('O e-mail informado é inválido.'),
    );
    const sut = new UpdateEmployeeUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: employee.id,
      ...payload(),
      email: 'invalido',
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamValidationError);
  });
});
