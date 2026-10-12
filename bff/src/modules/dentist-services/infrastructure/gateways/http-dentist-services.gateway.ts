import { Injectable } from '@nestjs/common';
import {
  DownstreamHttpClient,
  DownstreamHttpStatusError,
} from '@shared/infrastructure/http/downstream-http-client';
import { HEADERS } from '@shared/infrastructure/constants/headers';
import {
  DentistServicesGateway,
  CreateDentistServiceInput,
  UpdateDentistServiceInput,
} from '../../application/ports/dentist-services.gateway';
import { DentistService } from '../../domain/dentist-service';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../domain/dentist-service-errors';
import {
  fromDownstream,
  DownstreamDentistServiceDto,
} from './downstream-dentist-service.mapper';

const BASE_PATH = '/api/dentist-services';

@Injectable()
export class HttpDentistServicesGateway implements DentistServicesGateway {
  constructor(private readonly http: DownstreamHttpClient) {}

  async list(dentistId: string): Promise<DentistService[]> {
    try {
      const data = await this.http.request<DownstreamDentistServiceDto[]>({
        method: 'GET',
        path: BASE_PATH,
        headers: { [HEADERS.X_DENTIST_ID]: dentistId },
      });
      return (data ?? []).map(fromDownstream);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async getById(id: string, dentistId: string): Promise<DentistService> {
    try {
      const data = await this.http.request<DownstreamDentistServiceDto>({
        method: 'GET',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
        headers: { [HEADERS.X_DENTIST_ID]: dentistId },
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async create(input: CreateDentistServiceInput): Promise<DentistService> {
    try {
      const data = await this.http.request<DownstreamDentistServiceDto>({
        method: 'POST',
        path: BASE_PATH,
        body: input,
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async update(
    id: string,
    dentistId: string,
    input: UpdateDentistServiceInput,
  ): Promise<DentistService> {
    try {
      const data = await this.http.request<DownstreamDentistServiceDto>({
        method: 'PUT',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
        headers: { [HEADERS.X_DENTIST_ID]: dentistId },
        body: input,
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async delete(id: string, dentistId: string): Promise<void> {
    try {
      await this.http.request<void>({
        method: 'DELETE',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
        headers: { [HEADERS.X_DENTIST_ID]: dentistId },
      });
    } catch (err) {
      throw this.translateError(err);
    }
  }

  private translateError(err: unknown): Error {
    if (err instanceof DownstreamHttpStatusError) {
      if (err.status === 403)
        return new DentistServiceForbiddenError(err.message);
      if (err.status === 404) {
        const notFound = new DentistServiceNotFoundError();
        if (err.message && err.message !== 'Erro 404.') {
          notFound.message = err.message;
        }
        return notFound;
      }
    }
    return err as Error;
  }
}
