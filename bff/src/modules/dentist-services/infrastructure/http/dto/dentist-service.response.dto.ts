import { ApiProperty } from '@nestjs/swagger';
import { DentistServiceStatus } from '../../../domain/dentist-service-status.enum';

export class DentistServiceResponseDto {
  @ApiProperty({ format: 'uuid' })
  id!: string;

  @ApiProperty()
  name!: string;

  @ApiProperty({ format: 'uuid' })
  dentistId!: string;

  @ApiProperty({ minimum: 1 })
  price!: number;

  @ApiProperty({ minimum: 5, maximum: 480 })
  durationMinutes!: number;

  @ApiProperty()
  isPeriodic!: boolean;

  @ApiProperty({ enum: DentistServiceStatus })
  status!: DentistServiceStatus;

  @ApiProperty({ format: 'date-time' })
  createdAt!: string;

  @ApiProperty({ format: 'date-time' })
  updatedAt!: string;
}
