import { DynamicModule, Module } from '@nestjs/common';
import { APP_CONFIG } from '@shared/config/app-config.module';
import type { AppEnv } from '@shared/config/env.schema';
import { DownstreamHttpClient } from './downstream-http-client';

export interface DownstreamHttpModuleOptions {
  providerToken: symbol | string;
  baseUrlEnvKey: string;
  timeoutMsEnvKey?: string;
}

@Module({})
export class DownstreamHttpModule {
  static forFeature(
    token: symbol | string,
    factory: (...args: unknown[]) => DownstreamHttpClient,
    inject: unknown[],
  ): DynamicModule {
    return {
      module: DownstreamHttpModule,
      providers: [
        {
          provide: token,
          useFactory: factory,
          inject: inject as never[],
        },
      ],
      exports: [token],
    };
  }

  /**
   * Atalho de uma linha para um downstream novo:
   * `DownstreamHttpModule.forClient(TOKEN, 'BASE_URL_ENV', 'TIMEOUT_ENV?')`
   */
  static forClient(
    token: symbol | string,
    baseUrlKey: keyof AppEnv = 'DENTIST_SERVICE_BASE_URL',
    timeoutKey: keyof AppEnv = 'DOWNSTREAM_TIMEOUT_MS',
  ): DynamicModule {
    return {
      module: DownstreamHttpModule,
      providers: [
        {
          provide: token,
          useFactory: (config: AppEnv) =>
            new DownstreamHttpClient(
              String(config[baseUrlKey]),
              Number(config[timeoutKey]),
            ),
          inject: [APP_CONFIG],
        },
      ],
      exports: [token],
    };
  }
}
