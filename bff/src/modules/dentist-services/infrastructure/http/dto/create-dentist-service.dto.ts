import { ApiProperty } from '@nestjs/swagger';
import {
  IsBoolean,
  IsEnum,
  IsInt,
  IsString,
  Max,
  Length,
  Min,
} from 'class-validator';
import { DentistServiceStatus } from '../../../domain/dentist-service-status.enum';

export class CreateDentistServiceDto {
  @ApiProperty({ minLength: 3, maxLength: 100 })
  @IsString()
  @Length(3, 100)
  name!: string;

  @ApiProperty({ description: 'Preço em centavos (inteiro >= 1)', minimum: 1 })
  @IsInt()
  @Min(1)
  price!: number;

  @ApiProperty({ description: 'Duração em minutos', minimum: 5, maximum: 480 })
  @IsInt()
  @Min(5)
  @Max(480)
  durationMinutes!: number;

  @ApiProperty()
  @IsBoolean()
  isPeriodic!: boolean;

  @ApiProperty({ enum: DentistServiceStatus })
  @IsEnum(DentistServiceStatus)
  status!: DentistServiceStatus;
}
