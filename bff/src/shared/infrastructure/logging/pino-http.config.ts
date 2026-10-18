import { join } from 'node:path';
import type { IncomingMessage, ServerResponse } from 'node:http';
import { ecsFormat } from '@elastic/ecs-pino-format';
import type { Params } from 'nestjs-pino';
import pino from 'pino';
import pinoPretty from 'pino-pretty';
import type { AppEnv } from '@shared/config/env.schema';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { correlationIdStorage } from '@shared/infrastructure/http/correlation-id.storage';
import { pathWithoutQuery } from './log-events';

export const LOG_SERVICE_NAME = 'bff';

type LoggingEnv = Pick<AppEnv, 'NODE_ENV' | 'LOG_DIR'>;
type RequestWithOriginalUrl = IncomingMessage & { originalUrl?: string };

const NANOSECONDS_PER_MILLISECOND = 1_000_000;

/**
 * Configuração do pino-http: logs ECS (JSON) no stdout e, quando LOG_DIR está definido,
 * também em {LOG_DIR}/bff/bff.log para o Filebeat enviar ao ELK.
 * Em desenvolvimento o stdout continua legível (pino-pretty).
 *
 * Request e response não são serializados por inteiro (carregariam headers como authorization):
 * o log de acesso traz apenas os campos ECS montados em `buildAccessFields`.
 */
export function buildPinoHttpOptions(config: LoggingEnv): Params['pinoHttp'] {
  const options = {
    ...ecsFormat({
      serviceName: LOG_SERVICE_NAME,
      serviceEnvironment: config.NODE_ENV,
    }),
    level: config.NODE_ENV === 'production' ? 'info' : 'debug',
    serializers: { req: () => undefined, res: () => undefined },
    // Rede de segurança caso algum log inclua headers do request.
    redact: [
      `req.headers.${HEADERS.AUTHORIZATION}`,
      'req.headers.cookie',
      `http.request.headers.${HEADERS.AUTHORIZATION}`,
      'http.request.headers.cookie',
    ],
    autoLogging: { ignore: (req: { url?: string }) => isHealthCheck(req.url) },
    customLogLevel: (
      _req: unknown,
      res: { statusCode: number },
      err?: Error,
    ) =>
      err || res.statusCode >= 500
        ? 'error'
        : res.statusCode >= 400
          ? 'warn'
          : 'info',
    customSuccessObject: (
      req: RequestWithOriginalUrl,
      res: ServerResponse,
      val: { responseTime: number },
    ) => buildAccessFields(req, res, val.responseTime),
    customErrorObject: (
      req: RequestWithOriginalUrl,
      res: ServerResponse,
      err: Error,
      val: { responseTime: number },
    ) => ({ ...buildAccessFields(req, res, val.responseTime), err }),
    mixin: () => {
      const correlationId = correlationIdStorage.getStore();
      return correlationId ? { labels: { correlation_id: correlationId } } : {};
    },
    mixinMergeStrategy: mergeKeepingLabels,
  };

  return [options, pino.multistream(buildStreams(config))];
}

function buildAccessFields(
  req: RequestWithOriginalUrl,
  res: ServerResponse,
  responseTimeMs: number,
): object {
  return {
    event: { duration: responseTimeMs * NANOSECONDS_PER_MILLISECOND },
    http: {
      request: { method: req.method },
      response: { status_code: res.statusCode },
    },
    url: { path: pathWithoutQuery(req.originalUrl ?? req.url) },
    user_agent: { original: req.headers['user-agent'] },
  };
}

type Labels = { labels?: Record<string, unknown> };

/** Mescla `labels` do mixin (correlation_id) com os `labels` informados na chamada de log. */
function mergeKeepingLabels(mergeObject: object, mixinObject: object): object {
  const { labels: callLabels } = mergeObject as Labels;
  const { labels: mixinLabels } = mixinObject as Labels;
  return {
    ...mixinObject,
    ...mergeObject,
    ...(callLabels || mixinLabels
      ? { labels: { ...mixinLabels, ...callLabels } }
      : {}),
  };
}

function isHealthCheck(url: string | undefined): boolean {
  return url === '/health' || (url?.startsWith('/health?') ?? false);
}

function buildStreams(config: LoggingEnv): pino.StreamEntry[] {
  const streams: pino.StreamEntry[] = [
    {
      stream:
        config.NODE_ENV === 'production'
          ? process.stdout
          : pinoPretty({
              colorize: true,
              messageKey: 'message',
              levelKey: 'log\\.level',
              timestampKey: '@timestamp',
              ignore:
                'ecs\\.version,process\\.pid,host\\.hostname,service\\.name,service\\.environment,event\\.dataset',
            }),
    },
  ];

  if (config.LOG_DIR) {
    streams.push({
      stream: pino.destination({
        dest: join(config.LOG_DIR, LOG_SERVICE_NAME, `${LOG_SERVICE_NAME}.log`),
        mkdir: true,
        sync: false,
      }),
    });
  }

  return streams;
}
