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
import { ErrorResponseDto } from './error-response.dto';

export const DOMAIN_ERROR_HTTP_STATUS: Record<string, number> = {
  INVALID_TOKEN: HttpStatus.UNAUTHORIZED,
  INSUFFICIENT_ROLE: HttpStatus.FORBIDDEN,
  RESOURCE_FORBIDDEN: HttpStatus.FORBIDDEN,
  RESOURCE_NOT_FOUND: HttpStatus.NOT_FOUND,
  DOWNSTREAM_BAD_REQUEST: HttpStatus.BAD_REQUEST,
  DOWNSTREAM_UNAVAILABLE: HttpStatus.BAD_GATEWAY,
  DOWNSTREAM_TIMEOUT: HttpStatus.GATEWAY_TIMEOUT,
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

      if (status >= 500) {
        this.logger.error(
          { err: exception, path: request.url },
          'DomainError não mapeado',
        );
      } else if (status === 401 || status === 403) {
        this.logger.warn(
          { code: exception.code, path: request.url },
          'Acesso negado',
        );
      }
    } else if (exception instanceof HttpException) {
      status = exception.getStatus();
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
}
