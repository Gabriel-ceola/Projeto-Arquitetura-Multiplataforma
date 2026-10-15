import { MiddlewareConsumer, Module, NestModule } from '@nestjs/common';
import { APP_FILTER, APP_GUARD } from '@nestjs/core';
import { ThrottlerModule, ThrottlerGuard } from '@nestjs/throttler';
import { LoggerModule } from 'nestjs-pino';
import { AppConfigModule, APP_CONFIG } from '@shared/config/app-config.module';
import type { AppEnv } from '@shared/config/env.schema';
import { CorrelationIdMiddleware } from '@shared/infrastructure/http/correlation-id.middleware';
import { DomainExceptionFilter } from '@shared/infrastructure/http/domain-exception.filter';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import { HealthModule } from '@shared/infrastructure/health/health.module';
import { AuthModule } from '@modules/auth/infrastructure/auth.module';
import { DentistServicesModule } from '@modules/dentist-services/infrastructure/dentist-services.module';
import { AppointmentsModule } from '@modules/appointments/infrastructure/appointments.module';
import { EmployeesModule } from '@modules/employees/infrastructure/employees.module';
import { correlationIdStorage } from '@shared/infrastructure/http/correlation-id.storage';

@Module({
  imports: [
    AppConfigModule,
    LoggerModule.forRootAsync({
      useFactory: (config: AppEnv) => ({
        pinoHttp: {
          level: config.NODE_ENV === 'production' ? 'info' : 'debug',
          redact: [`req.headers.${HEADERS.AUTHORIZATION}`],
          mixin: () => ({
            // correlationId disponível via AsyncLocalStorage (CorrelationIdMiddleware)
            correlationId: correlationIdStorage.getStore() ?? undefined,
          }),
          transport:
            config.NODE_ENV !== 'production'
              ? { target: 'pino-pretty', options: { colorize: true } }
              : undefined,
        },
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
    consumer.apply(CorrelationIdMiddleware).forRoutes('*');
  }
}
