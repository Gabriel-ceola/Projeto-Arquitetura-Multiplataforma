import { validate } from 'class-validator';
import { plainToInstance } from 'class-transformer';
import { CreateDentistServiceDto } from './create-dentist-service.dto';
import { DentistServiceStatus } from '../../../domain/dentist-service-status.enum';

const valido = () => ({
  name: 'Limpeza completa',
  price: 100,
  durationMinutes: 30,
  isPeriodic: false,
  status: DentistServiceStatus.Disponivel,
});

const errorsOf = (plain: Record<string, unknown>) =>
  validate(plainToInstance(CreateDentistServiceDto, plain));

describe('CreateDentistServiceDto (validação)', () => {
  it('name com 2 caracteres é inválido', async () => {
    await expect(
      errorsOf({ ...valido(), name: 'ab' }),
    ).resolves.not.toHaveLength(0);
  });

  it('name com 101 caracteres é inválido', async () => {
    await expect(
      errorsOf({ ...valido(), name: 'a'.repeat(101) }),
    ).resolves.not.toHaveLength(0);
  });

  it('name com 3 e 100 caracteres é válido', async () => {
    await expect(errorsOf({ ...valido(), name: 'abc' })).resolves.toHaveLength(
      0,
    );
    await expect(
      errorsOf({ ...valido(), name: 'a'.repeat(100) }),
    ).resolves.toHaveLength(0);
  });

  it.each([[0], [-5], [10.5], ['100']])(
    'price %p é inválido',
    async (price) => {
      await expect(errorsOf({ ...valido(), price })).resolves.not.toHaveLength(
        0,
      );
    },
  );

  it('isPeriodic não-booleano é inválido', async () => {
    await expect(
      errorsOf({ ...valido(), isPeriodic: 'true' }),
    ).resolves.not.toHaveLength(0);
  });

  it('status fora do enum é inválido', async () => {
    await expect(
      errorsOf({ ...valido(), status: 'ativo' }),
    ).resolves.not.toHaveLength(0);
  });

  it('dentistId extra é descartado com whitelist', async () => {
    const dto = plainToInstance(CreateDentistServiceDto, {
      ...valido(),
      dentistId: 'outro-dentista',
    });

    const errors = await validate(dto, { whitelist: true });

    expect(errors).toHaveLength(0);
    expect(dto).not.toHaveProperty('dentistId');
  });
});
