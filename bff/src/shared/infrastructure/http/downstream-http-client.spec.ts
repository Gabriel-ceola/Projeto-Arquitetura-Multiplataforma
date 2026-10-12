import {
  DownstreamHttpClient,
  DownstreamHttpStatusError,
} from './downstream-http-client';
import {
  DownstreamUnavailableError,
  DownstreamTimeoutError,
  DownstreamValidationError,
} from '@shared/domain/common-errors';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { correlationIdStorage } from './correlation-id.storage';

const BASE_URL = 'http://downstream:5068';

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });

function mockFetch() {
  const fn = jest.fn();
  global.fetch = fn;
  return fn;
}

describe('DownstreamHttpClient', () => {
  let fetchMock: jest.Mock;
  let sut: DownstreamHttpClient;

  beforeEach(() => {
    fetchMock = mockFetch();
    sut = new DownstreamHttpClient(BASE_URL, 5000);
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('200 com JSON retorna o corpo parseado', async () => {
    fetchMock.mockResolvedValue(json({ id: '1', name: 'Limpeza' }, 200));

    const result = await sut.request({ method: 'GET', path: '/x' });

    expect(result).toEqual({ id: '1', name: 'Limpeza' });
  });

  it('201 com JSON retorna o corpo parseado', async () => {
    fetchMock.mockResolvedValue(json({ id: '2' }, 201));

    const result = await sut.request({ method: 'POST', path: '/x', body: {} });

    expect(result).toEqual({ id: '2' });
  });

  it('204 sem corpo retorna undefined sem erro de parse', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

    const result = await sut.request({ method: 'DELETE', path: '/x' });

    await expect(result).toBeUndefined();
  });

  it('400 com { message } lança DownstreamValidationError com a mensagem', async () => {
    fetchMock.mockResolvedValue(json({ message: 'nome inválido' }, 400));

    await expect(
      sut.request({ method: 'POST', path: '/x', body: {} }),
    ).rejects.toMatchObject({
      message: 'nome inválido',
    });
    await expect(
      sut.request({ method: 'POST', path: '/x', body: {} }),
    ).rejects.toBeInstanceOf(DownstreamValidationError);
  });

  it('403 lança DownstreamHttpStatusError com status 403', async () => {
    fetchMock.mockResolvedValue(json({ message: 'negado' }, 403));

    const err = await sut
      .request({ method: 'GET', path: '/x' })
      .catch((e) => e);

    expect(err).toBeInstanceOf(DownstreamHttpStatusError);
    expect((err as DownstreamHttpStatusError).status).toBe(403);
  });

  it('404 lança DownstreamHttpStatusError com status 404', async () => {
    fetchMock.mockResolvedValue(json({ message: 'não achado' }, 404));

    const err = await sut
      .request({ method: 'GET', path: '/x' })
      .catch((e) => e);

    expect(err).toBeInstanceOf(DownstreamHttpStatusError);
    expect((err as DownstreamHttpStatusError).status).toBe(404);
  });

  it('500/503 lança DownstreamUnavailableError', async () => {
    fetchMock.mockResolvedValueOnce(json({ message: 'boom' }, 500));
    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamUnavailableError);

    fetchMock.mockResolvedValueOnce(json({ message: 'busy' }, 503));
    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });

  it('falha de rede (TypeError) lança DownstreamUnavailableError', async () => {
    fetchMock.mockRejectedValue(new TypeError('fetch failed'));

    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });

  it('AbortError lança DownstreamTimeoutError', async () => {
    const abort = new Error('aborted');
    abort.name = 'AbortError';
    fetchMock.mockRejectedValue(abort);

    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamTimeoutError);
  });

  it('TimeoutError lança DownstreamTimeoutError', async () => {
    const timeout = new Error('timed out');
    timeout.name = 'TimeoutError';
    fetchMock.mockRejectedValue(timeout);

    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamTimeoutError);
  });

  it('resposta 2xx com JSON inválido lança DownstreamUnavailableError', async () => {
    fetchMock.mockResolvedValue(
      new Response('não-é-json{{{', {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );

    await expect(
      sut.request({ method: 'GET', path: '/x' }),
    ).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });

  it('envia X-Correlation-Id do contexto ao downstream', async () => {
    fetchMock.mockResolvedValue(json({ ok: true }));

    await correlationIdStorage.run('corr-123', () =>
      sut.request({ method: 'GET', path: '/x' }),
    );

    const init = fetchMock.mock.calls[0][1] as RequestInit & {
      headers: Record<string, string>;
    };
    expect(init.headers[HEADERS.X_CORRELATION_ID]).toBe('corr-123');
  });

  it('envia Content-Type apenas quando há body', async () => {
    fetchMock.mockResolvedValue(json({ ok: true }));

    await sut.request({ method: 'GET', path: '/sem-body' });
    const semBody = fetchMock.mock.calls[0][1] as RequestInit & {
      headers: Record<string, string>;
    };
    expect(semBody.headers[HEADERS.CONTENT_TYPE]).toBeUndefined();

    fetchMock.mockResolvedValue(json({ ok: true }));
    await sut.request({ method: 'POST', path: '/com-body', body: { a: 1 } });
    const comBody = fetchMock.mock.calls[1][1] as RequestInit & {
      headers: Record<string, string>;
    };
    expect(comBody.headers[HEADERS.CONTENT_TYPE]).toBe('application/json');
    expect(comBody.body).toBe(JSON.stringify({ a: 1 }));
  });
});
