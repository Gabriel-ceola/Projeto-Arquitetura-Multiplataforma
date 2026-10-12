import { INestApplication, ValidationPipe } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import helmet from 'helmet';
import { Logger } from 'nestjs-pino';
import { AppModule } from './app.module';
import { APP_CONFIG } from '@shared/config/app-config.module';
import type { AppEnv } from '@shared/config/env.schema';
import { DomainExceptionFilter } from '@shared/infrastructure/http/domain-exception.filter';

/** Aplica a configuração HTTP padrão (usada por main.ts e pelos testes E2E). */
export function configureApp(app: INestApplication): void {
  // Segurança
  app.use(helmet());
  app.useGlobalFilters(new DomainExceptionFilter());

  // Validação global (D-3: dentistId extra é descartado silenciosamente)
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      transform: true,
      forbidNonWhitelisted: false,
    }),
  );

  app.enableShutdownHooks();
}

export async function bootstrap() {
  const app = await NestFactory.create(AppModule, { bufferLogs: true });

  // Logger (pino)
  app.useLogger(app.get(Logger));

  const config = app.get<AppEnv>(APP_CONFIG);

  app.enableCors({
    origin: config.CORS_ORIGINS
      ? config.CORS_ORIGINS.split(',')
          .map((o) => o.trim())
          .filter(Boolean)
      : false,
  });

  configureApp(app);

  // Swagger
  if (config.SWAGGER_ENABLED) {
    const docBuilder = new DocumentBuilder()
      .setTitle('BFF Odontológico')
      .setDescription('Backend for Frontend para clínica odontológica')
      .setVersion('1.0')
      .addBearerAuth()
      .build();

    const document = SwaggerModule.createDocument(app, docBuilder);
    SwaggerModule.setup('docs', app, document);
  }

  await app.listen(config.PORT);
}

void bootstrap();
