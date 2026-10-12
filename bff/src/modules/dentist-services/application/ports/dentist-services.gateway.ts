import { DentistService } from '../../domain/dentist-service';
import { DentistServiceStatus } from '../../domain/dentist-service-status.enum';

export const DENTIST_SERVICES_GATEWAY = Symbol('DENTIST_SERVICES_GATEWAY');

export interface CreateDentistServiceInput {
  dentistId: string;
  name: string;
  price: number;
  isPeriodic: boolean;
  status: DentistServiceStatus;
}

export interface UpdateDentistServiceInput {
  name: string;
  price: number;
  isPeriodic: boolean;
  status: DentistServiceStatus;
}

export interface DentistServicesGateway {
  list(dentistId: string): Promise<DentistService[]>;
  getById(id: string, dentistId: string): Promise<DentistService>;
  create(input: CreateDentistServiceInput): Promise<DentistService>;
  update(
    id: string,
    dentistId: string,
    input: UpdateDentistServiceInput,
  ): Promise<DentistService>;
  delete(id: string, dentistId: string): Promise<void>;
}
