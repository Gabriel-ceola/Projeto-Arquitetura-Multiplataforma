import { OmitType } from '@nestjs/swagger';
import { CreateEmployeeDto } from './create-employee.dto';

/** O CPF é imutável: não faz parte da edição. */
export class UpdateEmployeeDto extends OmitType(CreateEmployeeDto, [
  'cpf',
] as const) {}
