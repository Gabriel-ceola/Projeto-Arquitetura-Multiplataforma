import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import {
  IsIn,
  IsISO8601,
  IsOptional,
  IsString,
  IsUUID,
  Length,
  Matches,
  ValidateIf,
} from 'class-validator';

export class CreateAppointmentDto {
  @ApiProperty({ format: 'uuid' })
  @IsUUID()
  serviceId!: string;

  @ApiProperty({ format: 'date-time', example: '2026-11-02T09:00:00-03:00' })
  @IsISO8601({ strict: true, strictSeparator: true })
  @Matches(/(Z|[+-]\d{2}:\d{2})$/i, {
    message: 'startAt deve incluir offset ISO 8601.',
  })
  startAt!: string;

  @ApiProperty({ enum: ['Private', 'Insurance'] })
  @IsIn(['Private', 'Insurance'])
  type!: 'Private' | 'Insurance';

  @ApiPropertyOptional({ maxLength: 120 })
  @ValidateIf((value: CreateAppointmentDto) => value.type === 'Insurance')
  @IsString()
  @Length(1, 120)
  insuranceName?: string;

  @ApiPropertyOptional({ maxLength: 80 })
  @ValidateIf((value: CreateAppointmentDto) => value.type === 'Insurance')
  @IsString()
  @Length(1, 80)
  insuranceCardNumber?: string;

  @ApiPropertyOptional({
    format: 'uuid',
    description: 'Dentista pode solicitar em nome do paciente.',
  })
  @IsOptional()
  @IsUUID()
  patientId?: string;
}
