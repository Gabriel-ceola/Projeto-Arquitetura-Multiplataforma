import { DownstreamValidationError } from '../../../../shared/domain/common-errors';
import { EmployeeConflictError } from '../../domain/employee-errors';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';
import { CreateEmployeeUseCase } from './create-employee.use-case';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

const input = () => ({
  name: 'Maria Souza',
  cpf: '12345678901',
  email: 'maria.souza@clinica.com',
  phone: '43999998888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
});

describe('CreateEmployeeUseCase', () => {
  it('encaminha os campos ao gateway e retorna o funcionário criado', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new CreateEmployeeUseCase(gateway);
    const dados = input();

    // Act
    const resultado = await sut.execute(dados);

    // Assert
    expect(gateway.calls.create).toEqual([[dados]]);
    expect(resultado).toMatchObject(dados);
    expect(resultado.id).toBeDefined();
  });

  it('propaga EmployeeConflictError quando o CPF já está cadastrado', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const existente = anEmployee({ cpf: '12345678901' });
    gateway.items.set(existente.id, existente);
    const sut = new CreateEmployeeUseCase(gateway);

    // Act
    const promessa = sut.execute(input());

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(EmployeeConflictError);
    expect(gateway.items.size).toBe(1);
  });

  it('propaga a mensagem de validação lançada pelo gateway', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    gateway.failNext(
      'create',
      new DownstreamValidationError('O CPF deve conter 11 dígitos.'),
    );
    const sut = new CreateEmployeeUseCase(gateway);

    // Act
    const promessa = sut.execute({ ...input(), cpf: '123' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamValidationError);
    await expect(promessa).rejects.toThrow('O CPF deve conter 11 dígitos.');
  });

  it.each(Object.values(EmployeeRole))('aceita o cargo %s', async (role) => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new CreateEmployeeUseCase(gateway);

    // Act
    const resultado = await sut.execute({ ...input(), role });

    // Assert
    expect(resultado.role).toBe(role);
  });
});
