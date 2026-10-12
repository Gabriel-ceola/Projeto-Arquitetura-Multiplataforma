import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { DentistService } from '../../domain/dentist-service';
import { DENTIST_SERVICES_GATEWAY } from '../ports/dentist-services.gateway';
import type {
  DentistServicesGateway,
  CreateDentistServiceInput,
} from '../ports/dentist-services.gateway';

@Injectable()
export class CreateDentistServiceUseCase implements UseCase<
  CreateDentistServiceInput,
  DentistService
> {
  constructor(
    @Inject(DENTIST_SERVICES_GATEWAY)
    private readonly gateway: DentistServicesGateway,
  ) {}

  execute(input: CreateDentistServiceInput): Promise<DentistService> {
    return this.gateway.create(input);
  }
}
