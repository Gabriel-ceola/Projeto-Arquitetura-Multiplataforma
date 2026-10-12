import { DownstreamValidationError } from '../../../../shared/domain/common-errors';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';
import { CreateDentistServiceUseCase } from './create-dentist-service.use-case';
import { FakeDentistServicesGateway } from '../../../../../test/support/fakes/fake-dentist-services.gateway';

describe('CreateDentistServiceUseCase', () => {
  it('encaminha dentistId e campos ao gateway e retorna o criado', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new CreateDentistServiceUseCase(gateway);
    const input = {
      dentistId: 'dentist-A',
      name: 'Limpeza completa',
      price: 150,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    };

    // Act
    const resultado = await sut.execute(input);

    // Assert
    expect(gateway.calls.create).toEqual([[input]]);
    expect(resultado).toMatchObject(input);
  });

  it('propaga a mensagem de validação lançada pelo gateway', async () => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    gateway.failNext('create', new DownstreamValidationError('preço inválido'));
    const sut = new CreateDentistServiceUseCase(gateway);

    // Act
    const promessa = sut.execute({
      dentistId: 'dentist-A',
      name: 'Limpeza',
      price: -1,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    // Assert
    await expect(promessa).rejects.toThrow('preço inválido');
    await expect(promessa).rejects.toBeInstanceOf(DownstreamValidationError);
  });

  it.each([
    { status: DentistServiceStatus.Disponivel },
    { status: DentistServiceStatus.Indisponivel },
    { status: DentistServiceStatus.EmBreve },
  ])('aceita o status $status', async ({ status }) => {
    // Arrange
    const gateway = new FakeDentistServicesGateway();
    const sut = new CreateDentistServiceUseCase(gateway);

    // Act
    const resultado = await sut.execute({
      dentistId: 'dentist-A',
      name: 'Serviço',
      price: 100,
      isPeriodic: false,
      status,
    });

    // Assert
    expect(resultado.status).toBe(status);
  });
});
