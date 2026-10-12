import { Injectable, NestMiddleware } from '@nestjs/common';
import type { NextFunction, Request, Response } from 'express';
import { randomUUID } from 'crypto';
import { HEADERS } from '../constants/headers';
import { correlationIdStorage } from './correlation-id.storage';

@Injectable()
export class CorrelationIdMiddleware implements NestMiddleware {
  use(req: Request, res: Response, next: NextFunction): void {
    const correlationId =
      (req.headers[HEADERS.X_CORRELATION_ID] as string | undefined) ??
      randomUUID();

    res.setHeader(HEADERS.X_CORRELATION_ID, correlationId);

    correlationIdStorage.run(correlationId, () => {
      next();
    });
  }
}
