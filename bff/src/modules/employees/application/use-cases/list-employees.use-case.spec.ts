import { DownstreamUnavailableError } from '../../../../shared/domain/common-errors';
import { ListEmployeesUseCase } from './list-employees.use-case';
import { FakeEmployeesGateway } from '../../../../../test/support/fakes/fake-employees.gateway';
import { anEmployee } from '../../../../../test/support/builders/employee.builder';

describe('ListEmployeesUseCase', () => {
  it('retorna todos os funcionários da clínica', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    gateway.items.set('id-1', anEmployee({ id: 'id-1', cpf: '11111111111' }));
    gateway.items.set('id-2', anEmployee({ id: 'id-2', cpf: '22222222222' }));
    const sut = new ListEmployeesUseCase(gateway);

    // Act
    const resultado = await sut.execute();

    // Assert
    expect(resultado).toHaveLength(2);
    expect(gateway.calls.list).toHaveLength(1);
  });

  it('retorna lista vazia quando não há funcionários', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    const sut = new ListEmployeesUseCase(gateway);

    // Act
    const resultado = await sut.execute();

    // Assert
    expect(resultado).toEqual([]);
  });

  it('propaga DownstreamUnavailableError lançado pelo gateway', async () => {
    // Arrange
    const gateway = new FakeEmployeesGateway();
    gateway.failNext('list', new DownstreamUnavailableError());
    const sut = new ListEmployeesUseCase(gateway);

    // Act
    const promessa = sut.execute();

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });
});
