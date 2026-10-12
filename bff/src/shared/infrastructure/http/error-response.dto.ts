import { ApiProperty } from '@nestjs/swagger';

export class ErrorResponseDto {
  @ApiProperty({
    description: 'Mensagem de erro',
    example: 'Recurso não encontrado.',
  })
  message!: string;
}
