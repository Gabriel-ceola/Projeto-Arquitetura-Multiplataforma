import { Injectable, NestMiddleware } from '@nestjs/common';
import type { NextFunction, Request, Response } from 'express';
import { MetricsService } from './metrics.service';

/**
 * Mede todas as requisições HTTP do BFF — inclusive as barradas por guards (ex.: 401)
 * ou sem rota correspondente (404). Roda antes dos guards, ao contrário de um interceptor.
 */
@Injectable()
export class HttpMetricsMiddleware implements NestMiddleware {
  constructor(private readonly metrics: MetricsService) {}

  use(req: Request, res: Response, next: NextFunction): void {
    if (req.path === '/metrics') {
      next();
      return;
    }

    const startedAt = process.hrtime.bigint();

    res.on('finish', () => {
      const durationSeconds =
        Number(process.hrtime.bigint() - startedAt) / 1_000_000_000;
      const routePath = (req.route as { path?: string } | undefined)?.path;
      const route = routePath ? `${req.baseUrl ?? ''}${routePath}` : req.path;
      this.metrics.observeHttp(
        req.method,
        route,
        res.statusCode,
        durationSeconds,
      );
    });

    next();
  }
}
