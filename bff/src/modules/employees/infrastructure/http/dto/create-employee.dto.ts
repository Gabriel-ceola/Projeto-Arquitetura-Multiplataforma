import { ApiProperty } from '@nestjs/swagger';
import {
  IsEmail,
  IsEnum,
  IsString,
  Length,
  Matches,
  MaxLength,
} from 'class-validator';
import { EmployeeRole } from '../../../domain/employee-role.enum';
import { EmployeeStatus } from '../../../domain/employee-status.enum';

/** Aceita CPF com ou sem máscara (o Employee-service normaliza para 11 dígitos). */
const CPF_PATTERN = /^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/;
/** Aceita telefone com DDD, com ou sem máscara: (43) 99999-8888, 4333334444... */
const PHONE_PATTERN = /^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$/;

export class CreateEmployeeDto {
  @ApiProperty({ minLength: 3, maxLength: 100, example: 'Maria Souza' })
  @IsString()
  @Length(3, 100)
  name!: string;

  @ApiProperty({
    description: 'CPF com ou sem máscara',
    example: '123.456.789-01',
  })
  @IsString()
  @Matches(CPF_PATTERN, { message: 'cpf deve conter 11 dígitos' })
  cpf!: string;

  @ApiProperty({ maxLength: 150, example: 'maria.souza@clinica.com' })
  @IsEmail()
  @MaxLength(150)
  email!: string;

  @ApiProperty({
    description: 'Telefone com DDD, com ou sem máscara',
    example: '(43) 99999-8888',
  })
  @IsString()
  @Matches(PHONE_PATTERN, {
    message: 'phone deve conter DDD e 8 ou 9 dígitos',
  })
  phone!: string;

  @ApiProperty({ enum: EmployeeRole })
  @IsEnum(EmployeeRole)
  role!: EmployeeRole;

  @ApiProperty({ enum: EmployeeStatus })
  @IsEnum(EmployeeStatus)
  status!: EmployeeStatus;
}
