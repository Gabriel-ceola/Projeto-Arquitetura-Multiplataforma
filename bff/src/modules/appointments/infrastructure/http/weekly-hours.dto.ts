import { ApiProperty } from '@nestjs/swagger';
import { Type } from 'class-transformer';
import {
  ArrayMaxSize,
  IsArray,
  IsInt,
  Matches,
  Max,
  Min,
  ValidateNested,
} from 'class-validator';

export class WeeklyHoursItemDto {
  @ApiProperty({ minimum: 0, maximum: 6, description: '0 domingo; 6 sábado.' })
  @IsInt()
  @Min(0)
  @Max(6)
  dayOfWeek!: number;

  @ApiProperty({ example: '08:00:00' })
  @Matches(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/)
  startTime!: string;

  @ApiProperty({ example: '17:00:00' })
  @Matches(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/)
  endTime!: string;
}

export class SetWeeklyHoursDto {
  @ApiProperty({ type: [WeeklyHoursItemDto] })
  @IsArray()
  @ArrayMaxSize(7)
  @ValidateNested({ each: true })
  @Type(() => WeeklyHoursItemDto)
  items!: WeeklyHoursItemDto[];
}
