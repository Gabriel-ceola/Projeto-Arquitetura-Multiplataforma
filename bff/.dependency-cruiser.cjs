/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'domain-not-import-application',
      comment: 'domain não pode importar application',
      severity: 'error',
      from: { path: 'src/.*/domain/' },
      to: { path: 'src/.*/application/' },
    },
    {
      name: 'domain-not-import-infrastructure',
      comment: 'domain não pode importar infrastructure',
      severity: 'error',
      from: { path: 'src/.*/domain/' },
      to: { path: 'src/.*/infrastructure/' },
    },
    {
      name: 'domain-not-import-nestjs',
      comment: 'domain não pode importar NestJS',
      severity: 'error',
      from: { path: 'src/.*/domain/' },
      to: { path: '@nestjs/' },
    },
    {
      name: 'domain-not-import-jose',
      comment: 'domain não pode importar jose',
      severity: 'error',
      from: { path: 'src/.*/domain/' },
      to: { path: 'jose' },
    },
    {
      name: 'application-not-import-jose',
      comment: 'application não pode importar jose nem http direto',
      severity: 'error',
      from: { path: 'src/.*/application/' },
      to: { path: '(jose|^node:.*|^undici)' },
    },
    {
      name: 'application-not-import-infrastructure',
      comment: 'application não pode importar infrastructure',
      severity: 'error',
      from: { path: 'src/.*/application/' },
      to: { path: 'src/.*/infrastructure/' },
    },
    {
      name: 'modules-not-cross-import-infrastructure',
      comment:
        'módulos não podem importar infrastructure de outros módulos (exceto decorators de auth, que são o mecanismo oficial de RBAC compartilhado)',
      severity: 'error',
      from: { path: 'src/modules/([^/]+)/' },
      to: {
        path: 'src/modules/([^/]+)/infrastructure/',
        pathNot: ['src/modules/$1/', 'src/modules/auth/infrastructure/decorators/'],
      },
    },
    {
      name: 'no-circular',
      comment: 'sem dependências circulares',
      severity: 'error',
      from: {},
      to: { circular: true },
    },
  ],
  options: {
    doNotFollow: {
      path: 'node_modules',
    },
    tsConfig: {
      fileName: 'tsconfig.json',
    },
    reporterOptions: {
      text: {
        highlightFocused: true,
      },
    },
  },
};
