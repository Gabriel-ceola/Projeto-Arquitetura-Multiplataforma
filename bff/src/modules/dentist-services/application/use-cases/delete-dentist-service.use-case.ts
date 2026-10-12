import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { DENTIST_SERVICES_GATEWAY } from '../ports/dentist-services.gateway';
import type { DentistServicesGateway } from '../ports/dentist-services.gateway';

export interface DeleteDentistServiceInput {
  id: string;
  dentistId: string;
}

@Injectable()
export class DeleteDentistServiceUseCase implements UseCase<
  DeleteDentistServiceInput,
  void
> {
  constructor(
    @Inject(DENTIST_SERVICES_GATEWAY)
    private readonly gateway: DentistServicesGateway,
  ) {}

  execute(input: DeleteDentistServiceInput): Promise<void> {
    return this.gateway.delete(input.id, input.dentistId);
  }
}
