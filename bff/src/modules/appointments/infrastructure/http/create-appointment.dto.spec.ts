import { plainToInstance } from 'class-transformer';
import { validate } from 'class-validator';
import { CreateAppointmentDto } from './create-appointment.dto';

const valid = {
  serviceId: '11111111-1111-4111-8111-111111111111',
  startAt: '2026-11-02T09:00:00-03:00',
  type: 'Insurance',
  insuranceName: 'Plano Odonto',
  insuranceCardNumber: '12345',
};

describe('CreateAppointmentDto', () => {
  it('accepts appointment requests with explicit ISO offset', async () => {
    await expect(
      validate(plainToInstance(CreateAppointmentDto, valid)),
    ).resolves.toHaveLength(0);
  });

  it('rejects a timestamp without offset', async () => {
    const dto = plainToInstance(CreateAppointmentDto, {
      ...valid,
      startAt: '2026-11-02T09:00:00',
    });
    await expect(validate(dto)).resolves.not.toHaveLength(0);
  });
});
