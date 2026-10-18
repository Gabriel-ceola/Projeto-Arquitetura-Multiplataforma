import { Logger } from '@nestjs/common';
import {
  DownstreamUnavailableError,
  DownstreamTimeoutError,
  DownstreamValidationError,
} from '@shared/domain/common-errors';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { DomainError } from '@shared/domain/domain-error';
import {
  LOG_EVENTS,
  pathWithoutQuery,
} from '@shared/infrastructure/logging/log-events';
import { correlationIdStorage } from './correlation-id.storage';

export class DownstreamHttpStatusError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'DownstreamHttpStatusError';
  }
}

export class DownstreamConflictError extends DomainError {
  readonly code = 'DOWNSTREAM_CONFLICT';
  constructor(message: string) {
    super(message);
    this.name = 'DownstreamConflictError';
  }
}

export interface RequestOptions {
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  path: string;
  headers?: Record<string, string>;
  body?: unknown;
}

export class DownstreamHttpClient {
  private readonly logger = new Logger(DownstreamHttpClient.name);

  constructor(
    private readonly baseUrl: string,
    private readonly timeoutMs: number,
    private readonly defaultHeaders: Record<string, string> = {},
  ) {}

  async request<T>(options: RequestOptions): Promise<T | undefined> {
    const { method, path, headers = {}, body } = options;
    const url = `${this.baseUrl}${path}`;

    const correlationId = correlationIdStorage.getStore();
    const requestHeaders: Record<string, string> = {
      ...this.defaultHeaders,
      ...headers,
    };

    if (correlationId) {
      requestHeaders[HEADERS.X_CORRELATION_ID] = correlationId;
    }

    if (body !== undefined) {
      requestHeaders[HEADERS.CONTENT_TYPE] = 'application/json';
    }

    let response: Response;
    const startedAt = performance.now();
    try {
      response = await fetch(url, {
        method,
        headers: requestHeaders,
        body: body !== undefined ? JSON.stringify(body) : undefined,
        signal: AbortSignal.timeout(this.timeoutMs),
      });
    } catch (err) {
      const error = err as Error;
      if (error.name === 'AbortError' || error.name === 'TimeoutError') {
        this.logFailure(LOG_EVENTS.DOWNSTREAM_TIMEOUT, options, startedAt, {
          message: `Timeout de ${this.timeoutMs}ms ao chamar o serviço downstream`,
        });
        throw new DownstreamTimeoutError();
      }
      this.logFailure(LOG_EVENTS.DOWNSTREAM_UNAVAILABLE, options, startedAt, {
        message: 'Falha de conexão com o serviço downstream',
        errorType: error.name,
      });
      throw new DownstreamUnavailableError();
    }

    if (response.status === 204) {
      return undefined;
    }

    let responseBody: unknown;
    try {
      responseBody = await response.json();
    } catch {
      if (response.ok) {
        this.logFailure(LOG_EVENTS.DOWNSTREAM_ERROR, options, startedAt, {
          message: 'Resposta inválida do serviço downstream',
          statusCode: response.status,
        });
        throw new DownstreamUnavailableError(
          'Resposta inválida do serviço downstream.',
        );
      }
      responseBody = {};
    }

    if (response.status === 400) {
      const message =
        ((responseBody as Record<string, unknown>)?.['message'] as string) ||
        'Requisição inválida.';
      throw new DownstreamValidationError(message);
    }

    if (response.status === 409) {
      const message =
        ((responseBody as Record<string, unknown>)?.['message'] as string) ||
        'Conflito de agenda.';
      throw new DownstreamConflictError(message);
    }

    if (response.status === 403 || response.status === 404) {
      const message =
        ((responseBody as Record<string, unknown>)?.['message'] as string) ||
        `Erro ${response.status}.`;
      throw new DownstreamHttpStatusError(response.status, message);
    }

    if (!response.ok) {
      this.logFailure(LOG_EVENTS.DOWNSTREAM_ERROR, options, startedAt, {
        message: 'Serviço downstream respondeu com erro',
        statusCode: response.status,
      });
      throw new DownstreamUnavailableError();
    }

    return responseBody as T;
  }

  private logFailure(
    action: string,
    options: RequestOptions,
    startedAt: number,
    details: { message: string; statusCode?: number; errorType?: string },
  ): void {
    const { host } = new URL(this.baseUrl);
    const elapsedNanoseconds = Math.round(
      (performance.now() - startedAt) * 1_000_000,
    );

    this.logger.warn(
      {
        event: { action, duration: elapsedNanoseconds },
        destination: { address: host },
        http: {
          request: { method: options.method },
          ...(details.statusCode
            ? { response: { status_code: details.statusCode } }
            : {}),
        },
        url: { path: pathWithoutQuery(options.path) },
        ...(details.errorType ? { error: { type: details.errorType } } : {}),
      },
      details.message,
    );
  }
}
