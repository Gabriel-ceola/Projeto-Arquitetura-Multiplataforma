import pino from 'pino';
import { correlationIdStorage } from '@shared/infrastructure/http/correlation-id.storage';
import { buildPinoHttpOptions } from './pino-http.config';

type LogLine = Record<string, unknown>;

interface AccessLogOptions extends pino.LoggerOptions {
  customSuccessObject: (
    req: unknown,
    res: unknown,
    val: { responseTime: number },
  ) => object;
  customErrorObject: (
    req: unknown,
    res: unknown,
    err: Error,
    val: { responseTime: number },
  ) => object;
}

function getOptions(): AccessLogOptions {
  const [options] = buildPinoHttpOptions({
    NODE_ENV: 'production',
    LOG_DIR: undefined,
  }) as [AccessLogOptions, unknown];
  return options;
}

function createLogger(): { logger: pino.Logger; lines: LogLine[] } {
  const lines: LogLine[] = [];
  const stream = {
    write: (chunk: string) => {
      lines.push(JSON.parse(chunk) as LogLine);
    },
  };

  return { logger: pino(getOptions(), stream), lines };
}

describe('buildPinoHttpOptions', () => {
  it('deve gravar logs no formato ECS com service.name bff', () => {
    const { logger, lines } = createLogger();

    logger.info('Olá');

    expect(lines[0]).toMatchObject({
      message: 'Olá',
      'log.level': 'info',
      'service.name': 'bff',
      'service.environment': 'production',
    });
    expect(lines[0]['@timestamp']).toEqual(expect.any(String));
  });

  it('deve incluir labels.correlation_id quando há correlation id no contexto', () => {
    const { logger, lines } = createLogger();

    correlationIdStorage.run('teste-123', () => logger.info('Com contexto'));

    expect(lines[0].labels).toEqual({ correlation_id: 'teste-123' });
  });

  it('deve manter o correlation_id ao informar labels adicionais na chamada', () => {
    const { logger, lines } = createLogger();

    correlationIdStorage.run('teste-123', () =>
      logger.warn({ labels: { motivo: 'x' } }, 'Com labels'),
    );

    expect(lines[0].labels).toEqual({
      correlation_id: 'teste-123',
      motivo: 'x',
    });
  });

  it('não deve incluir labels quando não há correlation id', () => {
    const { logger, lines } = createLogger();

    logger.info('Sem contexto');

    expect(lines[0]).not.toHaveProperty('labels');
  });

  it('deve ocultar authorization e cookie caso headers sejam logados', () => {
    const { logger, lines } = createLogger();

    logger.info({
      req: {
        headers: { authorization: 'Bearer segredo', cookie: 'sessao=segredo' },
      },
    });

    expect(JSON.stringify(lines[0])).not.toContain('segredo');
  });

  it('deve montar o log de acesso em campos ECS sem headers e sem query string', () => {
    const { customSuccessObject } = getOptions();
    const req = {
      method: 'GET',
      url: '/api/x?cpf=12345678901',
      headers: { authorization: 'Bearer segredo', 'user-agent': 'jest' },
    };

    const fields = customSuccessObject(
      req,
      { statusCode: 200 },
      {
        responseTime: 12,
      },
    );

    expect(fields).toEqual({
      event: { duration: 12_000_000 },
      http: { request: { method: 'GET' }, response: { status_code: 200 } },
      url: { path: '/api/x' },
      user_agent: { original: 'jest' },
    });
  });

  it('deve incluir o erro no log de acesso de requisições com falha', () => {
    const { customErrorObject } = getOptions();
    const error = new Error('falha');

    const fields = customErrorObject(
      { method: 'GET', url: '/api/x', headers: {} },
      { statusCode: 500 },
      error,
      { responseTime: 1 },
    );

    expect(fields).toMatchObject({ err: error });
  });

  it('não deve serializar req e res completos', () => {
    const { logger, lines } = createLogger();

    logger.info({ req: { headers: { host: 'x' } }, res: { statusCode: 200 } });

    expect(lines[0]).not.toHaveProperty('req');
    expect(lines[0]).not.toHaveProperty('res');
  });
});
