import { Injectable } from '@nestjs/common';
import {
  DownstreamHttpClient,
  DownstreamHttpStatusError,
} from '@shared/infrastructure/http/downstream-http-client';
import {
  CreateEmployeeInput,
  EmployeesGateway,
  UpdateEmployeeInput,
} from '../../application/ports/employees.gateway';
import { Employee } from '../../domain/employee';
import {
  EmployeeConflictError,
  EmployeeNotFoundError,
} from '../../domain/employee-errors';
import {
  DownstreamEmployeeDto,
  fromDownstream,
} from './downstream-employee.mapper';

const BASE_PATH = '/api/employees';

/** Adaptador OUT: implementa EmployeesGateway chamando o Employee-service via HTTP. */
@Injectable()
export class HttpEmployeesGateway implements EmployeesGateway {
  constructor(private readonly http: DownstreamHttpClient) {}

  async list(): Promise<Employee[]> {
    try {
      const data = await this.http.request<DownstreamEmployeeDto[]>({
        method: 'GET',
        path: BASE_PATH,
      });
      return (data ?? []).map(fromDownstream);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async getById(id: string): Promise<Employee> {
    try {
      const data = await this.http.request<DownstreamEmployeeDto>({
        method: 'GET',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async create(input: CreateEmployeeInput): Promise<Employee> {
    try {
      const data = await this.http.request<DownstreamEmployeeDto>({
        method: 'POST',
        path: BASE_PATH,
        body: input,
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async update(id: string, input: UpdateEmployeeInput): Promise<Employee> {
    try {
      const data = await this.http.request<DownstreamEmployeeDto>({
        method: 'PUT',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
        body: input,
      });
      return fromDownstream(data!);
    } catch (err) {
      throw this.translateError(err);
    }
  }

  async delete(id: string): Promise<void> {
    try {
      await this.http.request<void>({
        method: 'DELETE',
        path: `${BASE_PATH}/${encodeURIComponent(id)}`,
      });
    } catch (err) {
      throw this.translateError(err);
    }
  }

  private translateError(err: unknown): Error {
    if (err instanceof DownstreamHttpStatusError) {
      if (err.status === 404) {
        const notFound = new EmployeeNotFoundError();
        if (err.message && err.message !== 'Erro 404.') {
          notFound.message = err.message;
        }
        return notFound;
      }
      if (err.status === 409) {
        return err.message && err.message !== 'Erro 409.'
          ? new EmployeeConflictError(err.message)
          : new EmployeeConflictError();
      }
    }
    return err as Error;
  }
}
