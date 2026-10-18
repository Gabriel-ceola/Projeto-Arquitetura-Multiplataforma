import {
  Body,
  Controller,
  Get,
  Inject,
  Param,
  ParseArrayPipe,
  ParseUUIDPipe,
  Post,
  Put,
  Query,
} from '@nestjs/common';
import {
  ApiBadRequestResponse,
  ApiBody,
  ApiConflictResponse,
  ApiCreatedResponse,
  ApiOkResponse,
  ApiOperation,
  ApiQuery,
  ApiTags,
} from '@nestjs/swagger';
import { ErrorResponseDto } from '@shared/infrastructure/http/error-response.dto';
import { Auth } from '@modules/auth/infrastructure/decorators/auth.decorator';
import { CurrentUser } from '@modules/auth/infrastructure/decorators/current-user.decorator';
import type { AuthenticatedUser } from '@modules/auth/domain/authenticated-user';
import { Role } from '@modules/auth/domain/role.enum';
import { AppointmentsGateway } from './appointments.gateway';
import { CreateAppointmentDto } from './http/create-appointment.dto';
import { ScheduleExceptionDto } from './http/schedule-exception.dto';
import { WeeklyHoursItemDto } from './http/weekly-hours.dto';

@ApiTags('appointments')
@Controller('api')
export class AppointmentsController {
  constructor(
    @Inject(AppointmentsGateway) private readonly gateway: AppointmentsGateway,
  ) {}

  @Get('availability')
  @Auth(Role.Patient, Role.Dentist)
  @ApiOperation({
    summary: 'Consulta horários disponíveis nos próximos 30 dias',
  })
  @ApiQuery({ name: 'dentistId', required: true, format: 'uuid' })
  @ApiQuery({ name: 'serviceId', required: true, format: 'uuid' })
  @ApiOkResponse({
    description: 'Lista de datas ISO 8601 em UTC',
    schema: { type: 'array', items: { type: 'string', format: 'date-time' } },
  })
  availability(
    @Query('dentistId') dentistId: string,
    @Query('serviceId') serviceId: string,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'GET',
      `availability?dentistId=${encodeURIComponent(dentistId)}&serviceId=${encodeURIComponent(serviceId)}`,
      user.roles.includes(Role.Dentist)
        ? { dentistId: user.id }
        : { patientId: user.id },
    );
  }

  @Get('dentist-schedule/weekly')
  @Auth(Role.Dentist)
  weekly(@CurrentUser() user: AuthenticatedUser) {
    return this.gateway.request('GET', 'dentist-schedule/weekly', {
      dentistId: user.id,
    });
  }

  @Put('dentist-schedule/weekly')
  @Auth(Role.Dentist)
  @ApiOperation({ summary: 'Substitui expediente semanal; dias 0 a 6' })
  @ApiBody({
    schema: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          dayOfWeek: { type: 'integer', minimum: 0, maximum: 6 },
          startTime: { type: 'string', example: '08:00:00' },
          endTime: { type: 'string', example: '17:00:00' },
        },
        required: ['dayOfWeek', 'startTime', 'endTime'],
      },
    },
  })
  @ApiOkResponse()
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  setWeekly(
    @Body(new ParseArrayPipe({ items: WeeklyHoursItemDto }))
    body: WeeklyHoursItemDto[],
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'PUT',
      'dentist-schedule/weekly',
      { dentistId: user.id },
      body,
    );
  }

  @Get('dentist-schedule/exceptions')
  @Auth(Role.Dentist)
  exceptions(@CurrentUser() user: AuthenticatedUser) {
    return this.gateway.request('GET', 'dentist-schedule/exceptions', {
      dentistId: user.id,
    });
  }

  @Put('dentist-schedule/exceptions')
  @Auth(Role.Dentist)
  @ApiOperation({ summary: 'Cria ou substitui exceção por data' })
  @ApiOkResponse()
  @ApiBadRequestResponse({ type: ErrorResponseDto })
  setException(
    @Body() body: ScheduleExceptionDto,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'PUT',
      'dentist-schedule/exceptions',
      { dentistId: user.id },
      body,
    );
  }

  @Get('appointments')
  @Auth(Role.Dentist, Role.Patient)
  list(@CurrentUser() user: AuthenticatedUser) {
    return this.gateway.request('GET', 'appointments', this.identity(user));
  }

  @Post('appointments')
  @Auth(Role.Dentist, Role.Patient)
  @ApiOperation({ summary: 'Solicita consulta particular ou por convênio' })
  @ApiCreatedResponse()
  @ApiConflictResponse({ type: ErrorResponseDto })
  create(
    @Body() body: CreateAppointmentDto,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'POST',
      'appointments',
      this.identity(user),
      body,
    );
  }

  @Post('appointments/:id/confirm')
  @Auth(Role.Dentist)
  confirm(
    @Param('id', ParseUUIDPipe) id: string,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'POST',
      `appointments/${id}/confirm`,
      { dentistId: user.id },
      {},
    );
  }

  @Post('appointments/:id/reject')
  @Auth(Role.Dentist)
  reject(
    @Param('id', ParseUUIDPipe) id: string,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'POST',
      `appointments/${id}/reject`,
      { dentistId: user.id },
      {},
    );
  }

  @Post('appointments/:id/cancel')
  @Auth(Role.Dentist, Role.Patient)
  cancel(
    @Param('id', ParseUUIDPipe) id: string,
    @CurrentUser() user: AuthenticatedUser,
  ) {
    return this.gateway.request(
      'POST',
      `appointments/${id}/cancel`,
      this.identity(user),
      {},
    );
  }

  private identity(user: AuthenticatedUser) {
    return user.roles.includes(Role.Dentist)
      ? { dentistId: user.id }
      : { patientId: user.id };
  }
}