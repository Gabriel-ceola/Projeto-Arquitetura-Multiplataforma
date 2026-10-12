import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../domain/dentist-service-errors';
import { GetDentistServiceByIdUseCase } from './get-dentist-service-by-id.use-case';
import { FakeDentistServicesGateway } from '../../../../../test/support/fakes/fake-dentist-services.gateway';
import { aDentistService } from '../../../../../test/support/builders/dentist-service.builder';

describe('GetDentistServiceByIdUseCase', () => {
  it('retorna o serviço quando pertence ao dentista', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    const sut = new GetDentistServiceByIdUseCase(gateway);

    // Act
    const resultado = await sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
    });

    // Assert
    expect(resultado).toEqual(service);
  });

  it('lança NotFound quando o serviço não existe', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new GetDentistServiceByIdUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: 'inexistente',
      dentistId: 'dentist-A',
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceNotFoundError);
  });

  it('lança Forbidden quando o serviço pertence a outro dentista', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-B' });
    gateway.items.set(service.id, service);
    const sut = new GetDentistServiceByIdUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceForbiddenError);
  });
});
