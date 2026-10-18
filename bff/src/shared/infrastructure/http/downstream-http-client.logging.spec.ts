import { Logger } from '@nestjs/common';
import { DownstreamHttpClient } from './downstream-http-client';

describe('DownstreamHttpClient - eventos de log', () => {
  let warn: jest.SpyInstance;
  const fetchMock = jest.fn();
  const sut = new DownstreamHttpClient('http://downstream:5068', 5000);

  beforeEach(() => {
    global.fetch = fetchMock;
    warn = jest.spyOn(Logger.prototype, 'warn').mockImplementation();
  });

  afterEach(() => {
    fetchMock.mockReset();
    jest.restoreAllMocks();
  });

  it('deve registrar downstream.timeout com destino e duração', async () => {
    fetchMock.mockRejectedValue(
      Object.assign(new Error('timeout'), { name: 'TimeoutError' }),
    );

    await expect(
      sut.request({ method: 'GET', path: '/api/x?cpf=123' }),
    ).rejects.toThrow();

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({
        event: { action: 'downstream.timeout', duration: expect.any(Number) },
        destination: { address: 'downstream:5068' },
        url: { path: '/api/x' },
      }),
      expect.any(String),
    );
  });

  it('deve registrar downstream.unavailable em falha de conexão', async () => {
    fetchMock.mockRejectedValue(new TypeError('fetch failed'));

    await expect(
      sut.request({ method: 'GET', path: '/api/x' }),
    ).rejects.toThrow();

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({
        event: expect.objectContaining({ action: 'downstream.unavailable' }),
        error: { type: 'TypeError' },
      }),
      expect.any(String),
    );
  });

  it('deve registrar downstream.error com o status em resposta 5xx', async () => {
    fetchMock.mockResolvedValue(new Response('{}', { status: 503 }));

    await expect(
      sut.request({ method: 'GET', path: '/api/x' }),
    ).rejects.toThrow();

    expect(warn).toHaveBeenCalledWith(
      expect.objectContaining({
        event: expect.objectContaining({ action: 'downstream.error' }),
        http: {
          request: { method: 'GET' },
          response: { status_code: 503 },
        },
      }),
      expect.any(String),
    );
  });

  it('não deve registrar eventos em respostas de negócio (404)', async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ message: 'x' }), { status: 404 }),
    );

    await expect(
      sut.request({ method: 'GET', path: '/api/x' }),
    ).rejects.toThrow();

    expect(warn).not.toHaveBeenCalled();
  });
});
