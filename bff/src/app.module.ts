import { MiddlewareConsumer, Module, NestModule } from '@nestjs/common';
import { APP_FILTER, APP_GUARD } from '@nestjs/core';
import { ThrottlerModule, ThrottlerGuard } from '@nestjs/throttler';
import { LoggerModule } from 'nestjs-pino';
import { AppConfigModule, APP_CONFIG } from '@shared/config/app-config.module';
import type { AppEnv } from '@shared/config/env.schema';
import { CorrelationIdMiddleware } from '@shared/infrastructure/http/correlation-id.middleware';
import { DomainExceptionFilter } from '@shared/infrastructure/http/domain-exception.filter';
import { HealthModule } from '@shared/infrastructure/health/health.module';
import { buildPinoHttpOptions } from '@shared/infrastructure/logging/pino-http.config';
import { MetricsModule } from '@shared/infrastructure/metrics/metrics.module';
import { HttpMetricsMiddleware } from '@shared/infrastructure/metrics/http-metrics.middleware';
import { AuthModule } from '@modules/auth/infrastructure/auth.module';
import { DentistServicesModule } from '@modules/dentist-services/infrastructure/dentist-services.module';
import { AppointmentsModule } from '@modules/appointments/infrastructure/appointments.module';
import { EmployeesModule } from '@modules/employees/infrastructure/employees.module';

@Module({
  imports: [
    AppConfigModule,
    LoggerModule.forRootAsync({
      useFactory: (config: AppEnv) => ({
        pinoHttp: buildPinoHttpOptions(config),
      }),
      inject: [APP_CONFIG],
    }),
    ThrottlerModule.forRootAsync({
      useFactory: (config: AppEnv) => [
        {
          ttl: config.THROTTLE_TTL_SECONDS * 1000,
          limit: config.THROTTLE_LIMIT,
        },
      ],
      inject: [APP_CONFIG],
    }),
    HealthModule,
    MetricsModule,
    AuthModule,
    DentistServicesModule,
    AppointmentsModule,
    EmployeesModule,
  ],
  providers: [
    {
      provide: APP_FILTER,
      useClass: DomainExceptionFilter,
    },
    {
      provide: APP_GUARD,
      useClass: ThrottlerGuard,
    },
  ],
})
export class AppModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    consumer
      .apply(CorrelationIdMiddleware, HttpMetricsMiddleware)
      .forRoutes('*');
  }
}
