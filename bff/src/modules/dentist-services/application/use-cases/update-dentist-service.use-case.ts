import { Inject, Injectable } from '@nestjs/common';
import { UseCase } from '@shared/application/use-case';
import { DentistService } from '../../domain/dentist-service';
import { DENTIST_SERVICES_GATEWAY } from '../ports/dentist-services.gateway';
import type {
  DentistServicesGateway,
  UpdateDentistServiceInput,
} from '../ports/dentist-services.gateway';

export interface UpdateDentistServiceUseCaseInput extends UpdateDentistServiceInput {
  id: string;
  dentistId: string;
}

@Injectable()
export class UpdateDentistServiceUseCase implements UseCase<
  UpdateDentistServiceUseCaseInput,
  DentistService
> {
  constructor(
    @Inject(DENTIST_SERVICES_GATEWAY)
    private readonly gateway: DentistServicesGateway,
  ) {}

  async execute(
    input: UpdateDentistServiceUseCaseInput,
  ): Promise<DentistService> {
    const { id, dentistId, ...updateData } = input;

    // Ownership check: verificar se o serviço pertence ao dentista ANTES de atualizar
    // Lança DentistServiceForbiddenError ou DentistServiceNotFoundError se não for dono
    await this.gateway.getById(id, dentistId);

    return this.gateway.update(id, dentistId, updateData);
  }
}
