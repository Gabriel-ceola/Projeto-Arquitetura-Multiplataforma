import { ApiProperty } from '@nestjs/swagger';
import { EmployeeRole } from '../../../domain/employee-role.enum';
import { EmployeeStatus } from '../../../domain/employee-status.enum';

export class EmployeeResponseDto {
  @ApiProperty({ format: 'uuid' })
  id!: string;

  @ApiProperty()
  name!: string;

  @ApiProperty({ description: 'Somente dígitos', example: '12345678901' })
  cpf!: string;

  @ApiProperty()
  email!: string;

  @ApiProperty({ description: 'Somente dígitos', example: '43999998888' })
  phone!: string;

  @ApiProperty({ enum: EmployeeRole })
  role!: EmployeeRole;

  @ApiProperty({ enum: EmployeeStatus })
  status!: EmployeeStatus;

  @ApiProperty({ format: 'date-time' })
  createdAt!: string;

  @ApiProperty({ format: 'date-time' })
  updatedAt!: string;
}
