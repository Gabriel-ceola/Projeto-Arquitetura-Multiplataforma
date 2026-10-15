import type {
  CreateEmployeeInput,
  EmployeesGateway,
  UpdateEmployeeInput,
} from '../../../src/modules/employees/application/ports/employees.gateway';
import type { Employee } from '../../../src/modules/employees/domain/employee';
import {
  EmployeeConflictError,
  EmployeeNotFoundError,
} from '../../../src/modules/employees/domain/employee-errors';
import { randomUUID } from 'crypto';

type Method = keyof EmployeesGateway;

/** Fake em memória do gateway (simula as regras do Employee-service: 404 e CPF único). */
export class FakeEmployeesGateway implements EmployeesGateway {
  readonly items = new Map<string, Employee>();
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

  async list(): Promise<Employee[]> {
    this.record('list', []);
    return [...this.items.values()];
  }

  async getById(id: string): Promise<Employee> {
    this.record('getById', [id]);
    const item = this.items.get(id);
    if (!item) throw new EmployeeNotFoundError(id);
    return item;
  }

  async create(input: CreateEmployeeInput): Promise<Employee> {
    this.record('create', [input]);
    const cpfInUse = [...this.items.values()].some((e) => e.cpf === input.cpf);
    if (cpfInUse) throw new EmployeeConflictError();
    const now = new Date();
    const created: Employee = {
      id: randomUUID(),
      ...input,
      createdAt: now,
      updatedAt: now,
    };
    this.items.set(created.id, created);
    return created;
  }

  async update(id: string, input: UpdateEmployeeInput): Promise<Employee> {
    this.record('update', [id, input]);
    const item = this.items.get(id);
    if (!item) throw new EmployeeNotFoundError(id);
    const updated: Employee = { ...item, ...input, updatedAt: new Date() };
    this.items.set(id, updated);
    return updated;
  }

  async delete(id: string): Promise<void> {
    this.record('delete', [id]);
    if (!this.items.has(id)) throw new EmployeeNotFoundError(id);
    this.items.delete(id);
  }
}
