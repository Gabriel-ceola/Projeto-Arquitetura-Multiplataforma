import { Module } from '@nestjs/common';
import { DownstreamHttpClient } from '@shared/infrastructure/http/downstream-http-client';
import { DownstreamHttpModule } from '@shared/infrastructure/http/downstream-http.module';
import { HttpDentistServicesGateway } from './gateways/http-dentist-services.gateway';
import { DENTIST_SERVICES_GATEWAY } from '../application/ports/dentist-services.gateway';
import { ListDentistServicesUseCase } from '../application/use-cases/list-dentist-services.use-case';
import { GetDentistServiceByIdUseCase } from '../application/use-cases/get-dentist-service-by-id.use-case';
import { CreateDentistServiceUseCase } from '../application/use-cases/create-dentist-service.use-case';
import { UpdateDentistServiceUseCase } from '../application/use-cases/update-dentist-service.use-case';
import { DeleteDentistServiceUseCase } from '../application/use-cases/delete-dentist-service.use-case';
import { DentistServicesController } from './http/dentist-services.controller';

/** Token for the downstream HTTP client targeting the dentist-service microservice. */
export const DENTIST_HTTP_CLIENT = Symbol('DENTIST_HTTP_CLIENT');

@Module({
  imports: [
    DownstreamHttpModule.forClient(
      DENTIST_HTTP_CLIENT,
      'DENTIST_SERVICE_BASE_URL',
      'DOWNSTREAM_TIMEOUT_MS',
      'dentist-services',
    ),
  ],
  controllers: [DentistServicesController],
  providers: [
    {
      provide: DENTIST_SERVICES_GATEWAY,
      useFactory: (http: DownstreamHttpClient) =>
        new HttpDentistServicesGateway(http),
      inject: [DENTIST_HTTP_CLIENT],
    },
    ListDentistServicesUseCase,
    GetDentistServiceByIdUseCase,
    CreateDentistServiceUseCase,
    UpdateDentistServiceUseCase,
    DeleteDentistServiceUseCase,
  ],
})
export class DentistServicesModule {}
