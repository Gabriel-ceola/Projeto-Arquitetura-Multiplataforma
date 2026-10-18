import {
  DownstreamUnavailableError,
  DownstreamTimeoutError,
  DownstreamValidationError,
} from '@shared/domain/common-errors';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { DomainError } from '@shared/domain/domain-error';
import { MetricsService } from '@shared/infrastructure/metrics/metrics.service';
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
  constructor(
    private readonly baseUrl: string,
    private readonly timeoutMs: number,
    private readonly defaultHeaders: Record<string, string> = {},
    private readonly metrics?: MetricsService,
    private readonly clientName: string = 'unknown',
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

    const startedAt = process.hrtime.bigint();
    let response: Response;
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
        this.recordDownstream(method, path, 'timeout', 0, startedAt);
        throw new DownstreamTimeoutError();
      }
      this.recordDownstream(method, path, 'unavailable', 0, startedAt);
      throw new DownstreamUnavailableError();
    }

    this.recordDownstream(
      method,
      path,
      response.ok ? 'ok' : 'error',
      response.status,
      startedAt,
    );

    if (response.status === 204) {
      return undefined;
    }

    let responseBody: unknown;
    try {
      responseBody = await response.json();
    } catch {
      if (response.ok) {
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
      throw new DownstreamUnavailableError();
    }

    return responseBody as T;
  }

  private recordDownstream(
    method: string,
    path: string,
    outcome: string,
    status: number,
    startedAt: bigint,
  ): void {
    if (!this.metrics) {
      return;
    }
    const durationSeconds =
      Number(process.hrtime.bigint() - startedAt) / 1_000_000_000;
    this.metrics.observeDownstream(
      this.clientName,
      method,
      outcome,
      status,
      durationSeconds,
    );
  }
}
