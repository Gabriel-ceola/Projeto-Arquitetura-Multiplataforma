import { Module } from '@nestjs/common';
import { DownstreamHttpClient } from '@shared/infrastructure/http/downstream-http-client';
import { DownstreamHttpModule } from '@shared/infrastructure/http/downstream-http.module';
import { AppointmentsController } from './appointments.controller';
import { AppointmentsGateway } from './appointments.gateway';

export const APPOINTMENTS_HTTP_CLIENT = Symbol('APPOINTMENTS_HTTP_CLIENT');

@Module({
  imports: [DownstreamHttpModule.forClient(APPOINTMENTS_HTTP_CLIENT)],
  controllers: [AppointmentsController],
  providers: [
    {
      provide: AppointmentsGateway,
      useFactory: (http: DownstreamHttpClient) => new AppointmentsGateway(http),
      inject: [APPOINTMENTS_HTTP_CLIENT],
    },
  ],
  exports: [AppointmentsGateway],
})
export class AppointmentsModule {}
