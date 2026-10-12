import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../domain/dentist-service-errors';
import { DeleteDentistServiceUseCase } from './delete-dentist-service.use-case';
import { FakeDentistServicesGateway } from '../../../../../test/support/fakes/fake-dentist-services.gateway';
import { aDentistService } from '../../../../../test/support/builders/dentist-service.builder';

describe('DeleteDentistServiceUseCase', () => {
  it('chama delete e resolve void quando o serviço é próprio', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    const sut = new DeleteDentistServiceUseCase(gateway);

    // Act
    const resultado = await sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
    });

    // Assert
    expect(resultado).toBeUndefined();
    expect(gateway.calls.delete).toEqual([[service.id, 'dentist-A']]);
    expect(gateway.items.has(service.id)).toBe(false);
  });

  it('lança NotFound quando o serviço não existe', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new DeleteDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: 'inexistente',
      dentistId: 'dentist-A',
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceNotFoundError);
  });

  it('lança Forbidden e mantém o item quando pertence a outro dentista', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-B' });
    gateway.items.set(service.id, service);
    const sut = new DeleteDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceForbiddenError);
    expect(gateway.items.has(service.id)).toBe(true);
  });
});
