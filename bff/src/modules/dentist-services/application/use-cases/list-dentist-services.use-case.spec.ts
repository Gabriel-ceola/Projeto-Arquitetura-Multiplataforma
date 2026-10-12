import { DownstreamUnavailableError } from '../../../../shared/domain/common-errors';
import { ListDentistServicesUseCase } from './list-dentist-services.use-case';
import { FakeDentistServicesGateway } from '../../../../../test/support/fakes/fake-dentist-services.gateway';
import { aDentistService } from '../../../../../test/support/builders/dentist-service.builder';

describe('ListDentistServicesUseCase', () => {
  it('retorna os 2 serviços e chama o gateway com o dentistId', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    gateway.items.set(
      'id-1',
      aDentistService({ id: 'id-1', dentistId: 'dentist-A' }),
    );
    gateway.items.set(
      'id-2',
      aDentistService({ id: 'id-2', dentistId: 'dentist-A' }),
    );
    const sut = new ListDentistServicesUseCase(gateway);

    // Act
    const resultado = await sut.execute({ dentistId: 'dentist-A' });

    // Assert
    expect(resultado).toHaveLength(2);
    expect(gateway.calls.list).toEqual([['dentist-A']]);
  });

  it('retorna lista vazia quando não há serviços', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new ListDentistServicesUseCase(gateway);

    // Act
    const resultado = await sut.execute({ dentistId: 'dentist-A' });

    // Assert
    expect(resultado).toEqual([]);
  });

  it('propaga DownstreamUnavailableError lançado pelo gateway', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    gateway.failNext('list', new DownstreamUnavailableError());
    const sut = new ListDentistServicesUseCase(gateway);

    // Act
    const promessa = sut.execute({ dentistId: 'dentist-A' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });
});
