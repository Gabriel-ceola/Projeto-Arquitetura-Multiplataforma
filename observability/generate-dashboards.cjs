/* Gera os dashboards Grafana provisionados como código. */
const fs = require('fs');
const path = require('path');

const DS = { type: 'prometheus', uid: 'prometheus' };
const outDir = path.join(__dirname, 'grafana', 'dashboards');

let id = 0;
const nextId = () => ++id;

function ts(title, exprs, gridPos, opts = {}) {
  return {
    id: nextId(),
    type: opts.type || 'timeseries',
    title,
    datasource: DS,
    gridPos,
    fieldConfig: {
      defaults: { custom: {}, unit: opts.unit, ...(opts.defaults || {}) },
      overrides: [],
    },
    options: {
      legend: { displayMode: 'table', placement: 'bottom', showLegend: true },
      tooltip: { mode: 'multi', sort: 'desc' },
      ...(opts.options || {}),
    },
    targets: exprs.map((expr, i) => ({
      refId: String.fromCharCode(65 + i),
      datasource: DS,
      expr,
      legendFormat: opts.legends ? opts.legends[i] : undefined,
      instant: opts.instant || false,
    })),
  };
}

function stat(title, expr, gridPos, opts = {}) {
  return {
    id: nextId(),
    type: 'stat',
    title,
    datasource: DS,
    gridPos,
    fieldConfig: {
      defaults: {
        unit: opts.unit,
        color: { mode: 'thresholds' },
        thresholds: {
          mode: 'absolute',
          steps: [
            { color: 'green', value: null },
            { color: opts.warnColor || 'orange', value: opts.warn ?? 1 },
            { color: 'red', value: opts.crit ?? 5 },
          ],
        },
      },
      overrides: [],
    },
    options: {
      colorMode: 'value',
      graphMode: 'area',
      reduceOptions: { calcs: ['lastNotNull'], fields: '', values: false },
    },
    targets: [
      { refId: 'A', datasource: DS, expr, instant: true, legendFormat: opts.legend },
    ],
  };
}

const dashboards = {};

// ─────────────────────────────────────────────────────────────
// 1. Agendamentos + mensageria (outbox/consumer)
// ─────────────────────────────────────────────────────────────
dashboards['agendamentos.json'] = {
  uid: 'odontologia-agendamentos',
  title: 'Agendamentos — Fluxo completo',
  tags: ['odontologia', 'agendamento'],
  timezone: 'browser',
  editable: true,
  schemaVersion: 39,
  version: 1,
  refresh: '30s',
  time: { from: 'now-6h', to: 'now' },
  templating: { list: [] },
  panels: [
    stat('Criados (5m)', 'sum(increase(appointment_creation_total{result="created"}[5m]))', { h: 4, w: 6, x: 0, y: 0 }, { legend: 'created' }),
    stat('Confirmados (5m)', 'sum(increase(appointment_confirmation_total{result="ok"}[5m]))', { h: 4, w: 6, x: 6, y: 0 }, { legend: 'confirmed' }),
    stat('Cancelados (5m)', 'sum(increase(appointment_cancellation_total{result="ok"}[5m]))', { h: 4, w: 6, x: 12, y: 0 }, { legend: 'cancelled' }),
    stat('Rejeitados (5m)', 'sum(increase(appointment_creation_total{result="rejected"}[5m]))', { h: 4, w: 6, x: 18, y: 0 }, { legend: 'rejected' }),

    ts('Criações por resultado', [
      'sum by (result) (rate(appointment_creation_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 4 }, { legends: ['{{result}}'] }),
    ts('Motivos de rejeição na criação', [
      'sum by (reason) (rate(appointment_creation_total{result="rejected"}[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 4 }, { legends: ['{{reason}}'] }),

    ts('Transições: confirmação e rejeição', [
      'sum by (result) (rate(appointment_confirmation_total[$__rate_interval]))',
      'sum by (result) (rate(appointment_rejection_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 12 }, { legends: ['confirm {{result}}', 'reject {{result}}'] }),
    ts('Cancelamentos por ator', [
      'sum by (actor, result) (rate(appointment_cancellation_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 12 }, { legends: ['{{actor}} {{result}}'] }),

    ts('Agendamentos ativos por status', [
      'sum by (status) (appointment_active)',
    ], { h: 8, w: 8, x: 0, y: 20 }, { legends: ['{{status}}'], type: 'timeseries' }),
    ts('Disponibilidade consultada', [
      'sum by (result) (rate(appointment_availability_total[$__rate_interval]))',
    ], { h: 8, w: 8, x: 8, y: 20 }, { legends: ['{{result}}'] }),
    ts('Atualizações de agenda', [
      'sum by (kind, result) (rate(appointment_schedule_update_total[$__rate_interval]))',
    ], { h: 8, w: 8, x: 16, y: 20 }, { legends: ['{{kind}} {{result}}'] }),

    ts('Outbox: publicados por tópico', [
      'sum by (event_type) (rate(messaging_outbox_published_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 28 }, { legends: ['{{event_type}}'] }),
    ts('Outbox: pendentes e falhas', [
      'max(messaging_outbox_pending)',
      'sum(rate(messaging_outbox_publish_failures_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 28 }, { legends: ['pending', 'falhas/s'] }),

    ts('Consumer: mensagens por resultado', [
      'sum by (topic, result) (rate(messaging_consumer_messages_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 36 }, { legends: ['{{topic}} {{result}}'] }),
    ts('Consumer: DLQ e retries', [
      'sum by (topic) (rate(messaging_consumer_dlq_total[$__rate_interval]))',
      'sum by (topic) (rate(messaging_consumer_retries_total[$__rate_interval]))',
      'sum(rate(messaging_consumer_fatal_errors_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 36 }, { legends: ['dlq {{topic}}', 'retry {{topic}}', 'fatal'] }),

    ts('HTTP: latência p95 por rota', [
      'histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket{job="dentist-service"}[5m])))',
    ], { h: 8, w: 12, x: 0, y: 44 }, { unit: 's', legends: ['{{http_route}}'] }),
    ts('HTTP: requisições por status', [
      'sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count{job="dentist-service"}[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 44 }, { legends: ['{{http_response_status_code}}'] }),
  ],
};

// ─────────────────────────────────────────────────────────────
// 2. Pagamentos
// ─────────────────────────────────────────────────────────────
dashboards['pagamentos.json'] = {
  uid: 'odontologia-pagamentos',
  title: 'Pagamentos — Fluxo completo',
  tags: ['odontologia', 'pagamento'],
  timezone: 'browser',
  editable: true,
  schemaVersion: 39,
  version: 1,
  refresh: '30s',
  time: { from: 'now-6h', to: 'now' },
  templating: { list: [] },
  panels: [
    stat('Criados (5m)', 'sum(increase(payment_creation_total{result="created"}[5m]))', { h: 4, w: 6, x: 0, y: 0 }),
    stat('Pagos (5m)', 'sum(increase(payment_status_update_total{to="paid",result="ok"}[5m]))', { h: 4, w: 6, x: 6, y: 0 }),
    stat('Falhados (5m)', 'sum(increase(payment_status_update_total{to="failed"}[5m]))', { h: 4, w: 6, x: 12, y: 0 }, { warn: 1 }),
    stat('Rejeições de criação (5m)', 'sum(increase(payment_creation_total{result="rejected"}[5m]))', { h: 4, w: 6, x: 18, y: 0 }, { warn: 1 }),

    ts('Criações por resultado/motivo', [
      'sum by (result, reason) (rate(payment_creation_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 4 }, { legends: ['{{result}} {{reason}}'] }),
    ts('Transições de status', [
      'sum by (from, to) (rate(payment_status_update_total{result="ok"}[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 4 }, { legends: ['{{from}} → {{to}}'] }),

    ts('Pagamentos ativos por status', [
      'sum by (status) (payment_active)',
    ], { h: 8, w: 8, x: 0, y: 12 }, { legends: ['{{status}}'] }),
    ts('Leituras (list/get)', [
      'sum by (operation, result) (rate(payment_read_total[$__rate_interval]))',
    ], { h: 8, w: 8, x: 8, y: 12 }, { legends: ['{{operation}} {{result}}'] }),
    ts('Valor dos pagamentos (p50/p95)', [
      'histogram_quantile(0.5, sum by (le) (rate(payment_amount_bucket[$__rate_interval])))',
      'histogram_quantile(0.95, sum by (le) (rate(payment_amount_bucket[$__rate_interval])))',
    ], { h: 8, w: 8, x: 16, y: 12 }, { legends: ['p50', 'p95'] }),

    ts('HTTP: latência p95 por rota', [
      'histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket{job="payment-service"}[5m])))',
    ], { h: 8, w: 12, x: 0, y: 20 }, { unit: 's', legends: ['{{http_route}}'] }),
    ts('HTTP: requisições por status', [
      'sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count{job="payment-service"}[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 20 }, { legends: ['{{http_response_status_code}}'] }),
  ],
};

// ─────────────────────────────────────────────────────────────
// 3. Kafka + Infraestrutura
// ─────────────────────────────────────────────────────────────
dashboards['kafka-infra.json'] = {
  uid: 'odontologia-kafka-infra',
  title: 'Kafka & Infraestrutura',
  tags: ['odontologia', 'kafka', 'infra'],
  timezone: 'browser',
  editable: true,
  schemaVersion: 39,
  version: 1,
  refresh: '30s',
  time: { from: 'now-6h', to: 'now' },
  templating: { list: [] },
  panels: [
    stat('Broker Kafka', 'sum(kafka_brokers)', { h: 4, w: 6, x: 0, y: 0 }, { instant: true }),
    stat('DLQ total (5m)', 'sum(increase(messaging_consumer_dlq_total[5m]))', { h: 4, w: 6, x: 6, y: 0 }, { warn: 1 }),
    stat('Outbox pendente', 'sum(messaging_outbox_pending)', { h: 4, w: 6, x: 12, y: 0 }, { warn: 50, crit: 200 }),
    stat('Serviços up', 'sum(up{job=~"dentist-service|payment-service|bff"})', { h: 4, w: 6, x: 18, y: 0 }, { warn: 3, crit: 2 }),

    ts('Lag de consumo por grupo/tópico', [
      'sum by (consumergroup, topic) (kafka_consumergroup_lag)',
    ], { h: 8, w: 12, x: 0, y: 4 }, { legends: ['{{consumergroup}} {{topic}}'] }),
    ts('Offset atualizado por tópico', [
      'sum by (topic) (rate(kafka_consumergroup_current_offset[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 4 }, { legends: ['{{topic}}'] }),

    ts('Throughput do outbox e consumer', [
      'sum(rate(messaging_outbox_published_total[$__rate_interval]))',
      'sum(rate(messaging_consumer_messages_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 0, y: 12 }, { legends: ['publicados', 'consumidos'] }),
    ts('DLQ e falhas', [
      'sum(rate(messaging_consumer_dlq_total[$__rate_interval]))',
      'sum(rate(messaging_outbox_publish_failures_total[$__rate_interval]))',
      'sum(rate(messaging_consumer_fatal_errors_total[$__rate_interval]))',
    ], { h: 8, w: 12, x: 12, y: 12 }, { legends: ['dlq', 'publish failures', 'fatal'] }),

    ts('BFF: requisições por status', [
      'sum by (status) (rate(bff_http_requests_total[$__rate_interval]))',
    ], { h: 8, w: 8, x: 0, y: 20 }, { legends: ['{{status}}'] }),
    ts('BFF: downstream por resultado', [
      'sum by (client, outcome) (rate(bff_downstream_requests_total[$__rate_interval]))',
    ], { h: 8, w: 8, x: 8, y: 20 }, { legends: ['{{client}} {{outcome}}'] }),
    ts('BFF: latência downstream p95', [
      'histogram_quantile(0.95, sum by (le, client) (rate(bff_downstream_request_duration_seconds_bucket[5m])))',
    ], { h: 8, w: 8, x: 16, y: 20 }, { unit: 's', legends: ['{{client}}'] }),

    ts('Runtime .NET: heap', [
      'sum by (job) (dotnet_gc_heap_total_allocated_bytes_total)',
    ], { h: 8, w: 12, x: 0, y: 28 }, { unit: 'bytes', legends: ['{{job}}'] }),
    ts('Runtime .NET: thread pool', [
      'sum by (job) (dotnet_thread_pool_queue_length_total)',
    ], { h: 8, w: 12, x: 12, y: 28 }, { legends: ['{{job}}'] }),

    ts('Alvos raspados (up)', [
      'up',
    ], { h: 8, w: 24, x: 0, y: 36 }, { legends: ['{{job}} {{instance}}'] }),
  ],
};

for (const [file, dashboard] of Object.entries(dashboards)) {
  const target = path.join(outDir, file);
  fs.mkdirSync(outDir, { recursive: true });
  fs.writeFileSync(target, JSON.stringify(dashboard, null, 2) + '\n');
  console.log(`wrote ${target} (${dashboard.panels.length} panels)`);
}
