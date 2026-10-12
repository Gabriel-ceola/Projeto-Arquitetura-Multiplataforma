import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { DentistService } from '../../domain/dentist-service';
import { DENTIST_SERVICES_GATEWAY } from '../ports/dentist-services.gateway';
import type { DentistServicesGateway } from '../ports/dentist-services.gateway';

export interface GetDentistServiceByIdInput {
  id: string;
  dentistId: string;
}

@Injectable()
export class GetDentistServiceByIdUseCase implements UseCase<
  GetDentistServiceByIdInput,
  DentistService
> {
  constructor(
    @Inject(DENTIST_SERVICES_GATEWAY)
    private readonly gateway: DentistServicesGateway,
  ) {}

  execute(input: GetDentistServiceByIdInput): Promise<DentistService> {
    return this.gateway.getById(input.id, input.dentistId);
  }
}
