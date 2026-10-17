import {
  DownstreamHttpClient,
  DownstreamHttpStatusError,
} from '@shared/infrastructure/http/downstream-http-client';
import { DomainError } from '@shared/domain/domain-error';

class AppointmentResourceError extends DomainError {
  readonly code: string;
  constructor(code: string, message: string) {
    super(message);
    this.code = code;
  }
}

export class AppointmentsGateway {
  constructor(private readonly http: DownstreamHttpClient) {}
  async request<T>(
    method: 'GET' | 'POST' | 'PUT',
    path: string,
    identity: { dentistId?: string; patientId?: string },
    body?: unknown,
  ): Promise<T | undefined> {
    const headers: Record<string, string> = {};
    if (identity.dentistId) headers['x-dentist-id'] = identity.dentistId;
    if (identity.patientId) headers['x-patient-id'] = identity.patientId;
    try {
      return await this.http.request<T>({
        method,
        path: `/api/${path}`,
        headers,
        body,
      });
    } catch (error) {
      if (error instanceof DownstreamHttpStatusError && error.status === 403) {
        throw new AppointmentResourceError('RESOURCE_FORBIDDEN', error.message);
      }
      if (error instanceof DownstreamHttpStatusError && error.status === 404) {
        throw new AppointmentResourceError('RESOURCE_NOT_FOUND', error.message);
      }
      throw error;
    }
  }
}
