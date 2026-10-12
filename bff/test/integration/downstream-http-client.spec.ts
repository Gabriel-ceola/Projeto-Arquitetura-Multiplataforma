import { createServer, Server } from 'node:http';
import { AddressInfo } from 'node:net';
import { DownstreamHttpClient } from '../../src/shared/infrastructure/http/downstream-http-client';
import {
  DownstreamTimeoutError,
  DownstreamUnavailableError,
} from '../../src/shared/domain/common-errors';

describe('DownstreamHttpClient (integração + HTTP local)', () => {
  let server: Server;
  let baseUrl: string;
  let delayMs = 0;

  beforeAll(async () => {
    server = createServer((req, res) => {
      const respond = () => {
        res.writeHead(200, { 'content-type': 'application/json' });
        res.end(JSON.stringify({ ok: true, path: req.url }));
      };
      if (delayMs > 0) setTimeout(respond, delayMs);
      else respond();
    });
    await new Promise<void>((resolve) =>
      server.listen(0, '127.0.0.1', resolve),
    );
    baseUrl = `http://127.0.0.1:${(server.address() as AddressInfo).port}`;
  });

  afterAll(async () => {
    await new Promise<void>((resolve, reject) =>
      server.close((err) => (err ? reject(err) : resolve())),
    );
  });

  afterEach(() => {
    delayMs = 0;
  });

  it('retorna o corpo em 200', async () => {
    // Arrange
    const sut = new DownstreamHttpClient(baseUrl, 2000);

    // Act
    const body = await sut.request<{ ok: boolean }>({
      method: 'GET',
      path: '/ping',
    });

    // Assert
    expect(body).toMatchObject({ ok: true });
  });

  it('lança DownstreamTimeoutError quando o servidor atrasa além do timeout', async () => {
    // Arrange: delay (1500ms) > timeout (300ms)
    delayMs = 1500;
    const sut = new DownstreamHttpClient(baseUrl, 300);

    // Act / Assert
    await expect(
      sut.request({ method: 'GET', path: '/lento' }),
    ).rejects.toBeInstanceOf(DownstreamTimeoutError);
  }, 10000);

  it('lança DownstreamUnavailableError com conexão recusada', async () => {
    // Arrange: porta que foi aberta e fechada (conexão recusada)
    const probe = createServer();
    await new Promise<void>((resolve) => probe.listen(0, '127.0.0.1', resolve));
    const closedPort = (probe.address() as AddressInfo).port;
    await new Promise<void>((resolve, reject) =>
      probe.close((err) => (err ? reject(err) : resolve())),
    );
    const sut = new DownstreamHttpClient(
      `http://127.0.0.1:${closedPort}`,
      2000,
    );

    // Act / Assert
    await expect(
      sut.request({ method: 'GET', path: '/qualquer' }),
    ).rejects.toBeInstanceOf(DownstreamUnavailableError);
  });
});
