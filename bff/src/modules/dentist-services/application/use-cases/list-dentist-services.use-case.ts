import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { DentistService } from '../../domain/dentist-service';
import { DENTIST_SERVICES_GATEWAY } from '../ports/dentist-services.gateway';
import type { DentistServicesGateway } from '../ports/dentist-services.gateway';

export interface ListDentistServicesInput {
  dentistId: string;
}

@Injectable()
export class ListDentistServicesUseCase implements UseCase<
  ListDentistServicesInput,
  DentistService[]
> {
  constructor(
    @Inject(DENTIST_SERVICES_GATEWAY)
    private readonly gateway: DentistServicesGateway,
  ) {}

  execute(input: ListDentistServicesInput): Promise<DentistService[]> {
    return this.gateway.list(input.dentistId);
  }
}
