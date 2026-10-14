import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { IsBoolean, IsISO8601, IsOptional, Matches } from 'class-validator';

export class ScheduleExceptionDto {
  @ApiProperty({ format: 'date' })
  @IsISO8601({ strict: true, strictSeparator: true })
  date!: string;

  @ApiProperty()
  @IsBoolean()
  isClosed!: boolean;

  @ApiPropertyOptional({ example: '09:00:00' })
  @IsOptional()
  @Matches(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/)
  startTime?: string;

  @ApiPropertyOptional({ example: '12:00:00' })
  @IsOptional()
  @Matches(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/)
  endTime?: string;
}
