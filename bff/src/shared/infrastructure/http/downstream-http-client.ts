import {
  DownstreamUnavailableError,
  DownstreamTimeoutError,
  DownstreamValidationError,
} from '@shared/domain/common-errors';
import { HEADERS } from '@shared/infrastructure/constants/headers';
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
        throw new DownstreamTimeoutError();
      }
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
}
