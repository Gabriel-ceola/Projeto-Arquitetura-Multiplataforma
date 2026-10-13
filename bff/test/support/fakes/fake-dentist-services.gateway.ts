import type {
  CreateDentistServiceInput,
  DentistServicesGateway,
  UpdateDentistServiceInput,
} from '../../../src/modules/dentist-services/application/ports/dentist-services.gateway';
import type { DentistService } from '../../../src/modules/dentist-services/domain/dentist-service';
import {
  DentistServiceForbiddenError,
  DentistServiceNotFoundError,
} from '../../../src/modules/dentist-services/domain/dentist-service-errors';
import { randomUUID } from 'crypto';

type Method = keyof DentistServicesGateway;

/** Fake em memória do gateway (verifica estado e chamadas; sobrevive a refactors). */
export class FakeDentistServicesGateway implements DentistServicesGateway {
  readonly items = new Map<string, DentistService>();
  readonly calls: Record<Method, unknown[][]> = {
    list: [],
    getById: [],
    create: [],
    update: [],
    delete: [],
  };
  private failures = new Map<Method, Error>();

  failNext(method: Method, error: Error): void {
    this.failures.set(method, error);
  }

  private record(method: Method, args: unknown[]): void {
    this.calls[method].push(args);
    const err = this.failures.get(method);
    if (err) {
      this.failures.delete(method);
      throw err;
    }
  }

  async list(dentistId: string): Promise<DentistService[]> {
    this.record('list', [dentistId]);
    return [...this.items.values()].filter((i) => i.dentistId === dentistId);
  }

  async getById(id: string, dentistId: string): Promise<DentistService> {
    this.record('getById', [id, dentistId]);
    const item = this.items.get(id);
    if (!item) throw new DentistServiceNotFoundError(id);
    if (item.dentistId !== dentistId) throw new DentistServiceForbiddenError();
    return item;
  }

  async create(input: CreateDentistServiceInput): Promise<DentistService> {
    this.record('create', [input]);
    const now = new Date();
    const created: DentistService = {
      id: randomUUID(),
      ...input,
      durationMinutes: input.durationMinutes ?? 30,
      createdAt: now,
      updatedAt: now,
    };
    this.items.set(created.id, created);
    return created;
  }

  async update(
    id: string,
    dentistId: string,
    input: UpdateDentistServiceInput,
  ): Promise<DentistService> {
    this.record('update', [id, dentistId, input]);
    const item = this.items.get(id);
    if (!item) throw new DentistServiceNotFoundError(id);
    if (item.dentistId !== dentistId) throw new DentistServiceForbiddenError();
    const updated: DentistService = {
      ...item,
      ...input,
      updatedAt: new Date(),
    };
    this.items.set(id, updated);
    return updated;
  }

  async delete(id: string, dentistId: string): Promise<void> {
    this.record('delete', [id, dentistId]);
    const item = this.items.get(id);
    if (!item) throw new DentistServiceNotFoundError(id);
    if (item.dentistId !== dentistId) throw new DentistServiceForbiddenError();
    this.items.delete(id);
  }
}
