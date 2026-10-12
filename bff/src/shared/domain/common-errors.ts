import { DomainError } from './domain-error';

export class DownstreamValidationError extends DomainError {
  readonly code = 'DOWNSTREAM_BAD_REQUEST';
  constructor(message: string) {
    super(message);
  }
}

export class DownstreamUnavailableError extends DomainError {
  readonly code = 'DOWNSTREAM_UNAVAILABLE';
  constructor(message = 'Serviço downstream indisponível.') {
    super(message);
  }
}

export class DownstreamTimeoutError extends DomainError {
  readonly code = 'DOWNSTREAM_TIMEOUT';
  constructor(message = 'Timeout ao chamar serviço downstream.') {
    super(message);
  }
}
