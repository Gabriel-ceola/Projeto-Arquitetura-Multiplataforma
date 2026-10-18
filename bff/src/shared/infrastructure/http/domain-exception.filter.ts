import {
  ArgumentsHost,
  Catch,
  ExceptionFilter,
  HttpException,
  HttpStatus,
  Logger,
} from '@nestjs/common';
import type { Request, Response } from 'express';
import { DomainError } from '@shared/domain/domain-error';
import {
  LOG_EVENTS,
  pathWithoutQuery,
} from '@shared/infrastructure/logging/log-events';
import { ErrorResponseDto } from './error-response.dto';

export const DOMAIN_ERROR_HTTP_STATUS: Record<string, number> = {
  INVALID_TOKEN: HttpStatus.UNAUTHORIZED,
  INSUFFICIENT_ROLE: HttpStatus.FORBIDDEN,
  RESOURCE_FORBIDDEN: HttpStatus.FORBIDDEN,
  RESOURCE_NOT_FOUND: HttpStatus.NOT_FOUND,
  RESOURCE_CONFLICT: HttpStatus.CONFLICT,
  DOWNSTREAM_BAD_REQUEST: HttpStatus.BAD_REQUEST,
  DOWNSTREAM_UNAVAILABLE: HttpStatus.BAD_GATEWAY,
  DOWNSTREAM_TIMEOUT: HttpStatus.GATEWAY_TIMEOUT,
  DOWNSTREAM_CONFLICT: HttpStatus.CONFLICT,
};

@Catch()
export class DomainExceptionFilter implements ExceptionFilter {
  private readonly logger = new Logger(DomainExceptionFilter.name);

  catch(exception: unknown, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const request = ctx.getRequest<Request>();

    let status: number;
    let message: string;

    if (exception instanceof DomainError) {
      status =
        DOMAIN_ERROR_HTTP_STATUS[exception.code] ??
        HttpStatus.INTERNAL_SERVER_ERROR;
      message = exception.message;

      if (!(exception.code in DOMAIN_ERROR_HTTP_STATUS)) {
        // Erros downstream mapeados (502/504) já são logados pelo DownstreamHttpClient.
        this.logger.error(
          { err: exception, path: request.url },
          'DomainError não mapeado',
        );
      } else {
        this.logSecurityEvent(status, request, exception.code);
      }
    } else if (exception instanceof HttpException) {
      status = exception.getStatus();
      this.logSecurityEvent(status, request);
      const exceptionResponse = exception.getResponse();

      if (typeof exceptionResponse === 'string') {
        message = exceptionResponse;
      } else if (
        typeof exceptionResponse === 'object' &&
        exceptionResponse !== null
      ) {
        const body = exceptionResponse as Record<string, unknown>;
        const raw = body['message'];
        if (Array.isArray(raw)) {
          message = (raw as unknown[]).map((item) => String(item)).join(', ');
        } else if (typeof raw === 'string') {
          message = raw;
        } else {
          message = exception.message;
        }
      } else {
        message = exception.message;
      }
    } else {
      status = HttpStatus.INTERNAL_SERVER_ERROR;
      message = 'Erro interno.';
      this.logger.error(
        { err: exception, path: request.url },
        'Erro não tratado',
      );
    }

    const body: ErrorResponseDto = { message };
    response.status(status).json(body);
  }

  private logSecurityEvent(
    status: number,
    request: Request,
    errorCode?: string,
  ): void {
    const isRateLimited = status === 429;
    if (!isRateLimited && status !== 401 && status !== 403) {
      return;
    }

    this.logger.warn(
      {
        event: {
          action: isRateLimited
            ? LOG_EVENTS.RATE_LIMITED
            : LOG_EVENTS.AUTH_DENIED,
        },
        http: { response: { status_code: status } },
        url: { path: pathWithoutQuery(request.url) },
        ...(errorCode ? { error: { code: errorCode } } : {}),
        ...(isRateLimited ? { client: { ip: request.ip } } : {}),
      },
      isRateLimited ? 'Limite de requisições excedido' : 'Acesso negado',
    );
  }
}
