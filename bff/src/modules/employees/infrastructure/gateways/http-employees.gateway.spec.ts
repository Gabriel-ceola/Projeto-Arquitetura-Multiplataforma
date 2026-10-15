import {
  DownstreamConflictError,
  DownstreamHttpClient,
  DownstreamHttpStatusError,
} from '@shared/infrastructure/http/downstream-http-client';
import { DownstreamUnavailableError } from '@shared/domain/common-errors';
import { HttpEmployeesGateway } from './http-employees.gateway';
import {
  EmployeeConflictError,
  EmployeeNotFoundError,
} from '../../domain/employee-errors';
import { EmployeeRole } from '../../domain/employee-role.enum';
import { EmployeeStatus } from '../../domain/employee-status.enum';

const ID = '11111111-1111-4111-8111-111111111111';

const downstreamDto = (overrides = {}) => ({
  id: ID,
  name: 'Maria Souza',
  cpf: '12345678901',
  email: 'maria.souza@clinica.com',
  phone: '43999998888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
  ...overrides,
});

const createInput = {
  name: 'Maria Souza',
  cpf: '123.456.789-01',
  email: 'maria.souza@clinica.com',
  phone: '(43) 99999-8888',
  role: EmployeeRole.Recepcionista,
  status: EmployeeStatus.Ativo,
};

const updateInput = {
  name: 'Maria Souza Lima',
  email: 'maria.lima@clinica.com',
  phone: '4333334444',
  role: EmployeeRole.Administrativo,
  status: EmployeeStatus.Ferias,
};

describe('HttpEmployeesGateway', () => {
  let request: jest.Mock;
  let sut: HttpEmployeesGateway;

  beforeEach(() => {
    request = jest.fn();
    sut = new HttpEmployeesGateway({
      request,
    } as unknown as DownstreamHttpClient);
  });

  it('list faz GET /api/employees sem headers extras', async () => {
    request.mockResolvedValue([downstreamDto()]);

    const result = await sut.list();

    expect(request).toHaveBeenCalledWith({
      method: 'GET',
      path: '/api/employees',
    });
    expect(result).toHaveLength(1);
    expect(result[0].createdAt).toBeInstanceOf(Date);
  });

  it('list retorna [] quando o downstream não envia corpo', async () => {
    request.mockResolvedValue(undefined);

    await expect(sut.list()).resolves.toEqual([]);
  });

  it('getById faz GET /api/employees/{id}', async () => {
    request.mockResolvedValue(downstreamDto());

    const result = await sut.getById(ID);

    expect(request).toHaveBeenCalledWith({
      method: 'GET',
      path: `/api/employees/${ID}`,
    });
    expect(result.id).toBe(ID);
  });

  it('create faz POST /api/employees com o body completo', async () => {
    request.mockResolvedValue(downstreamDto());

    await sut.create(createInput);

    expect(request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/api/employees',
      body: createInput,
    });
  });

  it('update faz PUT /api/employees/{id} sem enviar CPF', async () => {
    request.mockResolvedValue(downstreamDto(updateInput));

    await sut.update(ID, updateInput);

    expect(request).toHaveBeenCalledWith({
      method: 'PUT',
      path: `/api/employees/${ID}`,
      body: updateInput,
    });
    expect(request.mock.calls[0][0].body).not.toHaveProperty('cpf');
  });

  it('delete faz DELETE /api/employees/{id}', async () => {
    request.mockResolvedValue(undefined);

    await sut.delete(ID);

    expect(request).toHaveBeenCalledWith({
      method: 'DELETE',
      path: `/api/employees/${ID}`,
    });
  });

  it('404 traduz para EmployeeNotFoundError mantendo a mensagem do downstream', async () => {
    request.mockRejectedValue(
      new DownstreamHttpStatusError(
        404,
        `Funcionário com id '${ID}' não encontrado.`,
      ),
    );

    const err = await sut.getById(ID).catch((e) => e);

    expect(err).toBeInstanceOf(EmployeeNotFoundError);
    expect((err as Error).message).toContain(ID);
  });

  it('404 sem mensagem usa a mensagem padrão', async () => {
    request.mockRejectedValue(new DownstreamHttpStatusError(404, 'Erro 404.'));

    const err = await sut.delete(ID).catch((e) => e);

    expect(err).toBeInstanceOf(EmployeeNotFoundError);
    expect((err as Error).message).toBe('Funcionário não encontrado.');
  });

  it('409 (DownstreamConflictError) traduz para EmployeeConflictError mantendo a mensagem', async () => {
    request.mockRejectedValue(new DownstreamConflictError('CPF já cadastrado'));

    const err = await sut.create(createInput).catch((e) => e);

    expect(err).toBeInstanceOf(EmployeeConflictError);
    expect((err as Error).message).toBe('CPF já cadastrado');
  });

  it('propaga outros erros sem tradução', async () => {
    request.mockRejectedValue(new DownstreamUnavailableError());

    await expect(sut.list()).rejects.toBeInstanceOf(DownstreamUnavailableError);
    await expect(sut.update(ID, updateInput)).rejects.toBeInstanceOf(
      DownstreamUnavailableError,
    );
  });

  it('codifica o id na URL', async () => {
    const id = 'a b/c';
    request.mockResolvedValue(downstreamDto({ id }));

    await sut.getById(id);

    expect(request.mock.calls[0][0].path).toBe(
      `/api/employees/${encodeURIComponent(id)}`,
    );
  });
});
