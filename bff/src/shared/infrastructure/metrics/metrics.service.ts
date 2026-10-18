import { Injectable } from '@nestjs/common';
import {
  Counter,
  Histogram,
  Registry,
  collectDefaultMetrics,
} from 'prom-client';

const UUID_REGEX =
  /\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

/** Normaliza ids dinâmicos para manter as labels de baixa cardinalidade. */
export function normalizeRoute(path: string): string {
  return path.replace(UUID_REGEX, '/:id').replace(/\/\d+/g, '/:id');
}

@Injectable()
export class MetricsService {
  private readonly registry = new Registry();

  private readonly httpRequests: Counter;
  private readonly httpDuration: Histogram;
  private readonly downstreamRequests: Counter;
  private readonly downstreamDuration: Histogram;

  constructor() {
    collectDefaultMetrics({ register: this.registry });

    this.httpRequests = new Counter({
      name: 'bff_http_requests_total',
      help: 'Total de requisições HTTP atendidas pelo BFF.',
      labelNames: ['method', 'route', 'status'],
      registers: [this.registry],
    });

    this.httpDuration = new Histogram({
      name: 'bff_http_request_duration_seconds',
      help: 'Duração das requisições HTTP no BFF.',
      labelNames: ['method', 'route', 'status'],
      buckets: [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5],
      registers: [this.registry],
    });

    this.downstreamRequests = new Counter({
      name: 'bff_downstream_requests_total',
      help: 'Total de chamadas aos serviços downstream.',
      labelNames: ['client', 'method', 'outcome', 'status'],
      registers: [this.registry],
    });

    this.downstreamDuration = new Histogram({
      name: 'bff_downstream_request_duration_seconds',
      help: 'Duração das chamadas aos serviços downstream.',
      labelNames: ['client', 'method', 'outcome'],
      buckets: [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5],
      registers: [this.registry],
    });
  }

  observeHttp(
    method: string,
    route: string,
    status: number,
    durationSeconds: number,
  ): void {
    const labels = {
      method,
      route: normalizeRoute(route),
      status: `${status}`,
    };
    this.httpRequests.inc(labels);
    this.httpDuration.observe(labels, durationSeconds);
  }

  observeDownstream(
    client: string,
    method: string,
    outcome: string,
    status: number,
    durationSeconds: number,
  ): void {
    this.downstreamRequests.inc({
      client,
      method,
      outcome,
      status: `${status}`,
    });
    this.downstreamDuration.observe(
      { client, method, outcome },
      durationSeconds,
    );
  }

  contentType(): string {
    return this.registry.contentType;
  }

  metrics(): Promise<string> {
    return this.registry.metrics();
  }
}
