import { DentistServiceStatus } from './dentist-service-status.enum';

export interface DentistService {
  id: string;
  name: string;
  dentistId: string;
  price: number;
  isPeriodic: boolean;
  status: DentistServiceStatus;
  createdAt: Date;
  updatedAt: Date;
}
