import { Controller, Get, Res } from '@nestjs/common';
import { ApiExcludeEndpoint, ApiTags } from '@nestjs/swagger';
import type { Response } from 'express';
import { Public } from '@modules/auth/infrastructure/decorators/public.decorator';
import { MetricsService } from './metrics.service';

@ApiTags('metrics')
@Controller('metrics')
@Public()
export class MetricsController {
  constructor(private readonly metrics: MetricsService) {}

  /** Endpoint de scrape do Prometheus. */
  @Get()
  @ApiExcludeEndpoint()
  async scrape(@Res() res: Response): Promise<void> {
    res.setHeader('Content-Type', this.metrics.contentType());
    res.send(await this.metrics.metrics());
  }
}
