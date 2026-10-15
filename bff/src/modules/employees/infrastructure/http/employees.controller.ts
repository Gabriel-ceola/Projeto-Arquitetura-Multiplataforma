import {
  Body,
  Controller,
  Delete,
  Get,
  HttpCode,
  HttpStatus,
  Param,
  ParseUUIDPipe,
  Post,
  Put,
} from '@nestjs/common';
import {
  ApiBadRequestResponse,
  ApiConflictResponse,
  ApiCreatedResponse,
  ApiNoContentResponse,
  ApiNotFoundResponse,
  ApiOkResponse,
  ApiOperation,
  ApiTags,
} from '@nestjs/swagger';
import { ErrorResponseDto } from '@shared/infrastructure/http/error-response.dto';
import { Auth } from '@modules/auth/infrastructure/decorators/auth.decorator';
import { Role } from '@modules/auth/domain/role.enum';
import { ListEmployeesUseCase } from '../../application/use-cases/list-employees.use-case';
import { GetEmployeeByIdUseCase } from '../../application/use-cases/get-employee-by-id.use-case';
import { CreateEmployeeUseCase } from '../../application/use-cases/create-employee.use-case';
import { UpdateEmployeeUseCase } from '../../application/use-cases/update-employee.use-case';
import { DeleteEmployeeUseCase } from '../../application/use-cases/delete-employee.use-case';
import { CreateEmployeeDto } from './dto/create-employee.dto';
import { UpdateEmployeeDto } from './dto/update-employee.dto';
import { EmployeeResponseDto } from './dto/employee.response.dto';
import { toResponseDto } from './employee.http-mapper';

@ApiTags('employees')
@Controller('api/employees')
@Auth(Role.Employ)
export class EmployeesController {
  constructor(
    private readonly listUseCase: ListEmployeesUseCase,
    private readonly getByIdUseCase: GetEmployeeByIdUseCase,
    private readonly createUseCase: CreateEmployeeUseCase,
    private readonly updateUseCase: UpdateEmployeeUseCase,
    private readonly deleteUseCase: DeleteEmployeeUseCase,
  ) {}

  @Get()
  @ApiOperation({ summary: 'Lista todos os funcionários da clínica' })
  @ApiOkResponse({ type: [EmployeeResponseDto] })
  async list(): Promise<EmployeeResponseDto[]> {
    const employees = await this.listUseCase.execute();
    return employees.map(toResponseDto);
  }

  @Get(':id')
  @ApiOperation({ summary: 'Busca um funcionário por ID' })
  @ApiOkResponse({ type: EmployeeResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  @ApiNotFoundResponse({ type: ErrorResponseDto })
  async getById(
    @Param('id', ParseUUIDPipe) id: string,
  ): Promise<EmployeeResponseDto> {
    const employee = await this.getByIdUseCase.execute({ id });
    return toResponseDto(employee);
  }

  @Post()
  @HttpCode(HttpStatus.CREATED)
  @ApiOperation({ summary: 'Cadastra um novo funcionário' })
  @ApiCreatedResponse({ type: EmployeeResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  @ApiConflictResponse({
    type: ErrorResponseDto,
    description: 'CPF já cadastrado',
  })
  async create(@Body() dto: CreateEmployeeDto): Promise<EmployeeResponseDto> {
    const employee = await this.createUseCase.execute({
      name: dto.name,
      cpf: dto.cpf,
      email: dto.email,
      phone: dto.phone,
      role: dto.role,
      status: dto.status,
    });
    return toResponseDto(employee);
  }

  @Put(':id')
  @ApiOperation({ summary: 'Edita os dados de um funcionário (CPF imutável)' })
  @ApiOkResponse({ type: EmployeeResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  @ApiNotFoundResponse({ type: ErrorResponseDto })
  async update(
    @Param('id', ParseUUIDPipe) id: string,
    @Body() dto: UpdateEmployeeDto,
  ): Promise<EmployeeResponseDto> {
    const employee = await this.updateUseCase.execute({
      id,
      name: dto.name,
      email: dto.email,
      phone: dto.phone,
      role: dto.role,
      status: dto.status,
    });
    return toResponseDto(employee);
  }

  @Delete(':id')
  @HttpCode(HttpStatus.NO_CONTENT)
  @ApiOperation({ summary: 'Remove um funcionário' })
  @ApiNoContentResponse()
  @ApiNotFoundResponse({ type: ErrorResponseDto })
  async delete(@Param('id', ParseUUIDPipe) id: string): Promise<void> {
    await this.deleteUseCase.execute({ id });
  }
}
