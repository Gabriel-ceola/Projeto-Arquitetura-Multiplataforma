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
  ApiCreatedResponse,
  ApiNoContentResponse,
  ApiNotFoundResponse,
  ApiOkResponse,
  ApiOperation,
  ApiTags,
} from '@nestjs/swagger';
import { ErrorResponseDto } from '@shared/infrastructure/http/error-response.dto';
import { Auth } from '@modules/auth/infrastructure/decorators/auth.decorator';
import { CurrentUser } from '@modules/auth/infrastructure/decorators/current-user.decorator';
import type { AuthenticatedUser } from '@modules/auth/domain/authenticated-user';
import { Role } from '@modules/auth/domain/role.enum';
import { ListDentistServicesUseCase } from '../../application/use-cases/list-dentist-services.use-case';
import { GetDentistServiceByIdUseCase } from '../../application/use-cases/get-dentist-service-by-id.use-case';
import { CreateDentistServiceUseCase } from '../../application/use-cases/create-dentist-service.use-case';
import { UpdateDentistServiceUseCase } from '../../application/use-cases/update-dentist-service.use-case';
import { DeleteDentistServiceUseCase } from '../../application/use-cases/delete-dentist-service.use-case';
import { CreateDentistServiceDto } from './dto/create-dentist-service.dto';
import { UpdateDentistServiceDto } from './dto/update-dentist-service.dto';
import { DentistServiceResponseDto } from './dto/dentist-service.response.dto';
import { toResponseDto } from './dentist-service.http-mapper';

@ApiTags('dentist-services')
@Controller('api/dentist-services')
@Auth(Role.Dentist)
export class DentistServicesController {
  constructor(
    private readonly listUseCase: ListDentistServicesUseCase,
    private readonly getByIdUseCase: GetDentistServiceByIdUseCase,
    private readonly createUseCase: CreateDentistServiceUseCase,
    private readonly updateUseCase: UpdateDentistServiceUseCase,
    private readonly deleteUseCase: DeleteDentistServiceUseCase,
  ) {}

  @Get()
  @ApiOperation({ summary: 'Lista todos os serviços do dentista autenticado' })
  @ApiOkResponse({ type: [DentistServiceResponseDto] })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  async list(
    @CurrentUser() user: AuthenticatedUser,
  ): Promise<DentistServiceResponseDto[]> {
    const services = await this.listUseCase.execute({ dentistId: user.id });
    return services.map(toResponseDto);
  }

  @Get(':id')
  @ApiOperation({ summary: 'Busca um serviço por ID' })
  @ApiOkResponse({ type: DentistServiceResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  @ApiNotFoundResponse({ type: ErrorResponseDto })
  async getById(
    @Param('id', ParseUUIDPipe) id: string,
    @CurrentUser() user: AuthenticatedUser,
  ): Promise<DentistServiceResponseDto> {
    const service = await this.getByIdUseCase.execute({
      id,
      dentistId: user.id,
    });
    return toResponseDto(service);
  }

  @Post()
  @HttpCode(HttpStatus.CREATED)
  @ApiOperation({ summary: 'Cria um novo serviço' })
  @ApiCreatedResponse({ type: DentistServiceResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  async create(
    @Body() dto: CreateDentistServiceDto,
    @CurrentUser() user: AuthenticatedUser,
  ): Promise<DentistServiceResponseDto> {
    const service = await this.createUseCase.execute({
      dentistId: user.id,
      name: dto.name,
      price: dto.price,
      isPeriodic: dto.isPeriodic,
      status: dto.status,
    });
    return toResponseDto(service);
  }

  @Put(':id')
  @ApiOperation({ summary: 'Atualiza um serviço (verifica ownership)' })
  @ApiOkResponse({ type: DentistServiceResponseDto })
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  @ApiNotFoundResponse({ type: ErrorResponseDto })
  async update(
    @Param('id', ParseUUIDPipe) id: string,
    @Body() dto: UpdateDentistServiceDto,
    @CurrentUser() user: AuthenticatedUser,
  ): Promise<DentistServiceResponseDto> {
    const service = await this.updateUseCase.execute({
      id,
      dentistId: user.id,
      name: dto.name,
      price: dto.price,
      isPeriodic: dto.isPeriodic,
      status: dto.status,
    });
    return toResponseDto(service);
  }

  @Delete(':id')
  @HttpCode(HttpStatus.NO_CONTENT)
  @ApiOperation({ summary: 'Remove um serviço' })
  @ApiNoContentResponse()
  async delete(
    @Param('id', ParseUUIDPipe) id: string,
    @CurrentUser() user: AuthenticatedUser,
  ): Promise<void> {
    await this.deleteUseCase.execute({ id, dentistId: user.id });
  }
}
