import {
  DownstreamTimeoutError,
  DownstreamValidationError,
} from '../../../../shared/domain/common-errors';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../domain/dentist-service-errors';
import { UpdateDentistServiceUseCase } from './update-dentist-service.use-case';
import { FakeDentistServicesGateway } from '../../../../../test/support/fakes/fake-dentist-services.gateway';
import { aDentistService } from '../../../../../test/support/builders/dentist-service.builder';

describe('UpdateDentistServiceUseCase', () => {
  it('chama getById antes de update e retorna o serviço atualizado', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    const sut = new UpdateDentistServiceUseCase(gateway);

    // Act
    const resultado = await sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
      name: 'Novo nome',
      price: 200,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    // Assert
    expect(gateway.calls.getById).toHaveLength(1);
    expect(gateway.calls.update).toHaveLength(1);
    expect(resultado.name).toBe('Novo nome');
  });

  it('não chama update quando o serviço pertence a outro dentista', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-B' });
    gateway.items.set(service.id, service);
    const sut = new UpdateDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
      name: 'Novo',
      price: 200,
      isPeriodic: false,
      status: DentistServiceStatus.Indisponivel,
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceForbiddenError);
    expect(gateway.calls.update).toHaveLength(0);
  });

  it('não chama update quando o serviço não existe', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new UpdateDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: 'inexistente',
      dentistId: 'dentist-A',
      name: 'Novo',
      price: 200,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DentistServiceNotFoundError);
    expect(gateway.calls.update).toHaveLength(0);
  });

  it('propaga erro de validação quando getById passa mas update falha', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    gateway.failNext(
      'update',
      new DownstreamValidationError('dados inválidos'),
    );
    const sut = new UpdateDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
      name: 'Novo',
      price: 200,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamValidationError);
    await expect(promessa).rejects.toThrow('dados inválidos');
  });

  it('propaga timeout lançado pelo update', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    gateway.failNext('update', new DownstreamTimeoutError());
    const sut = new UpdateDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
      name: 'Novo',
      price: 200,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(DownstreamTimeoutError);
  });

  it('encaminha id, dentistId e payload corretos ao update', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-A' });
    gateway.items.set(service.id, service);
    const sut = new UpdateDentistServiceUseCase(gateway);
    const payload = {
      name: 'Canal',
      price: 300,
      isPeriodic: true,
      status: DentistServiceStatus.EmBreve,
    };

    // Act
    await sut.execute({
      id: service.id,
      dentistId: 'dentist-A',
      ...payload,
    });

    // Assert
    expect(gateway.calls.update).toEqual([[service.id, 'dentist-A', payload]]);
  });
});
