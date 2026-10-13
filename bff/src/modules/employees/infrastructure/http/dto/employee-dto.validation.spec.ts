import { validate } from 'class-validator';
import { plainToInstance } from 'class-transformer';
import { CreateEmployeeDto } from './create-employee.dto';
import { UpdateEmployeeDto } from './update-employee.dto';
import { EmployeeRole } from '../../../domain/employee-role.enum';
import { EmployeeStatus } from '../../../domain/employee-status.enum';

const valido = () => ({
  name: 'Maria Souza',
  cpf: '123.456.789-01',
  email: 'maria.souza@clinica.com',
  phone: '(43) 99999-8888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
});

const semCpf = () => ({
  name: 'Maria Souza',
  email: 'maria.souza@clinica.com',
  phone: '(43) 99999-8888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
});

const errorsOf = (plain: Record<string, unknown>) =>
  validate(plainToInstance(CreateEmployeeDto, plain));

describe('CreateEmployeeDto (validação)', () => {
  it('payload completo e mascarado é válido', async () => {
    await expect(errorsOf(valido())).resolves.toHaveLength(0);
  });

  it('CPF e telefone sem máscara também são válidos', async () => {
    await expect(
      errorsOf({ ...valido(), cpf: '12345678901', phone: '4333334444' }),
    ).resolves.toHaveLength(0);
  });

  it.each([['ab'], ['a'.repeat(101)]])('name %p é inválido', async (name) => {
    await expect(errorsOf({ ...valido(), name })).resolves.not.toHaveLength(0);
  });

  it.each([['123'], ['123.456.789-0'], ['abc.def.ghi-jk']])(
    'cpf %p é inválido',
    async (cpf) => {
      await expect(errorsOf({ ...valido(), cpf })).resolves.not.toHaveLength(0);
    },
  );

  it.each([['email-invalido'], [`${'a'.repeat(145)}@x.com`]])(
    'email %p é inválido',
    async (email) => {
      await expect(errorsOf({ ...valido(), email })).resolves.not.toHaveLength(
        0,
      );
    },
  );

  it.each([['999'], ['99999-8888']])('phone %p é inválido', async (phone) => {
    await expect(errorsOf({ ...valido(), phone })).resolves.not.toHaveLength(0);
  });

  it('role fora do enum é inválida', async () => {
    await expect(
      errorsOf({ ...valido(), role: 'dentista' }),
    ).resolves.not.toHaveLength(0);
  });

  it('status fora do enum é inválido', async () => {
    await expect(
      errorsOf({ ...valido(), status: 'disponivel' }),
    ).resolves.not.toHaveLength(0);
  });
});

describe('UpdateEmployeeDto (validação)', () => {
  it('payload sem CPF é válido', async () => {
    const errors = await validate(plainToInstance(UpdateEmployeeDto, semCpf()));

    expect(errors).toHaveLength(0);
  });

  it('cpf enviado na edição é descartado com whitelist', async () => {
    const dto = plainToInstance(UpdateEmployeeDto, valido());

    const errors = await validate(dto, { whitelist: true });

    expect(errors).toHaveLength(0);
    expect(dto).not.toHaveProperty('cpf');
  });

  it('herda as validações do cadastro', async () => {
    const errors = await validate(
      plainToInstance(UpdateEmployeeDto, { ...semCpf(), email: 'invalido' }),
    );

    expect(errors).not.toHaveLength(0);
  });
});
