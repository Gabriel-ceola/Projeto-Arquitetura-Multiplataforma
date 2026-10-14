import { DownstreamHttpClient } from '@shared/infrastructure/http/downstream-http-client';
import { DownstreamHttpStatusError } from '@shared/infrastructure/http/downstream-http-client';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { HttpDentistServicesGateway } from './http-dentist-services.gateway';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../domain/dentist-service-errors';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';

const downstreamDto = (overrides = {}) => ({
  id: '11111111-1111-4111-8111-111111111111',
  name: 'Limpeza',
  dentistId: 'dentist-1',
  price: 100,
  durationMinutes: 30,
  isPeriodic: false,
  status: DentistServiceStatus.Disponivel,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
  ...overrides,
});

describe('HttpDentistServicesGateway', () => {
  let request: jest.Mock;
  let sut: HttpDentistServicesGateway;

  beforeEach(() => {
    request = jest.fn();
    sut = new HttpDentistServicesGateway({
      request,
    } as unknown as DownstreamHttpClient);
  });

  it('list faz GET /api/dentist-services com X-Dentist-Id', async () => {
    request.mockResolvedValue([downstreamDto()]);

    const result = await sut.list('dentist-1');

    expect(request).toHaveBeenCalledWith({
      method: 'GET',
      path: '/api/dentist-services',
      headers: { [HEADERS.X_DENTIST_ID]: 'dentist-1' },
    });
    expect(result).toHaveLength(1);
    expect(result[0].createdAt).toBeInstanceOf(Date);
  });

  it('getById faz GET /api/dentist-services/{id} com X-Dentist-Id', async () => {
    const id = '11111111-1111-4111-8111-111111111111';
    request.mockResolvedValue(downstreamDto({ id }));

    await sut.getById(id, 'dentist-1');

    expect(request).toHaveBeenCalledWith({
      method: 'GET',
      path: `/api/dentist-services/${id}`,
      headers: { [HEADERS.X_DENTIST_ID]: 'dentist-1' },
    });
  });

  it('create faz POST com body contendo dentistId', async () => {
    const input = {
      dentistId: 'dentist-1',
      name: 'Limpeza',
      price: 100,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    };
    request.mockResolvedValue(downstreamDto());

    await sut.create(input);

    expect(request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/api/dentist-services',
      body: input,
    });
  });

  it('update faz PUT /api/dentist-services/{id} com header e body sem dentistId', async () => {
    const id = '11111111-1111-4111-8111-111111111111';
    const input = {
      name: 'Novo',
      price: 200,
      isPeriodic: true,
      status: DentistServiceStatus.Indisponivel,
    };
    request.mockResolvedValue(downstreamDto({ id }));

    await sut.update(id, 'dentist-1', input);

    expect(request).toHaveBeenCalledWith({
      method: 'PUT',
      path: `/api/dentist-services/${id}`,
      headers: { [HEADERS.X_DENTIST_ID]: 'dentist-1' },
      body: input,
    });
    expect(request.mock.calls[0][0].body).not.toHaveProperty('dentistId');
  });

  it('delete faz DELETE /api/dentist-services/{id} com X-Dentist-Id', async () => {
    const id = '11111111-1111-4111-8111-111111111111';
    request.mockResolvedValue(undefined);

    await sut.delete(id, 'dentist-1');

    expect(request).toHaveBeenCalledWith({
      method: 'DELETE',
      path: `/api/dentist-services/${id}`,
      headers: { [HEADERS.X_DENTIST_ID]: 'dentist-1' },
    });
  });

  it('403 traduz para DentistServiceForbiddenError', async () => {
    request.mockRejectedValue(new DownstreamHttpStatusError(403, 'negado'));

    await expect(sut.getById('abc', 'dentist-1')).rejects.toBeInstanceOf(
      DentistServiceForbiddenError,
    );
  });

  it('404 traduz para DentistServiceNotFoundError', async () => {
    request.mockRejectedValue(new DownstreamHttpStatusError(404, 'ausente'));

    await expect(sut.getById('abc', 'dentist-1')).rejects.toBeInstanceOf(
      DentistServiceNotFoundError,
    );
  });

  it('codifica id com espaços e barra na URL', async () => {
    const id = 'a b/c?d';
    request.mockResolvedValue(downstreamDto({ id }));

    await sut.getById(id, 'dentist-1');
    await sut.delete(id, 'dentist-1');
    await sut.update(id, 'dentist-1', {
      name: 'Limpeza',
      price: 100,
      isPeriodic: false,
      status: DentistServiceStatus.Disponivel,
    });

    const encoded = encodeURIComponent(id);
    expect(request.mock.calls[0][0].path).toBe(
      `/api/dentist-services/${encoded}`,
    );
    expect(request.mock.calls[1][0].path).toBe(
      `/api/dentist-services/${encoded}`,
    );
    expect(request.mock.calls[2][0].path).toBe(
      `/api/dentist-services/${encoded}`,
    );
    expect(encoded).not.toContain(' ');
    expect(encoded).not.toContain('/');
  });
});
