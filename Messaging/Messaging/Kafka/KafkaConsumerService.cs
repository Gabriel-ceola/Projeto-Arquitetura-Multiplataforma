using Confluent.Kafka;
using Messaging.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka;

public sealed class KafkaConsumerService : BackgroundService
{
    private readonly EventDispatcher _dispatcher;
    private readonly EventHandlerRegistry _registry;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(
        EventDispatcher dispatcher,
        EventHandlerRegistry registry,
        IOptions<KafkaOptions> options,
        ILogger<KafkaConsumerService> logger)
    {
        _dispatcher = dispatcher;
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topics = _options.Consumer.Topics;

        foreach (var registration in _registry.Registrations.Where(x => !topics.Contains(x.Topic)))
            _logger.LogWarning("O handler de {EventType} espera o tópico {Topic}, que não está em Kafka:Consumer:Topics.",
                registration.EventType, registration.Topic);

        if (topics.Count == 0)
        {
            _logger.LogInformation("Nenhum tópico em Kafka:Consumer:Topics. O consumidor não será iniciado.");
            return Task.CompletedTask;
        }

        return Task.Factory.StartNew(
            () => Run(topics, stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void Run(IReadOnlyCollection<string> topics, CancellationToken stoppingToken)
    {
        try
        {
            using var consumer = BuildConsumer();
            consumer.Subscribe(topics);
            _logger.LogInformation("Consumidor {GroupId} inscrito em {Topics}.", _options.Consumer.GroupId, string.Join(", ", topics));

            try
            {
                ConsumeLoop(consumer, stoppingToken);
            }
            finally
            {
                consumer.Close();
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "O consumidor Kafka parou.");
        }
    }

    private IConsumer<string?, string?> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            GroupId = _options.Consumer.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        return new ConsumerBuilder<string?, string?>(config)
            .SetErrorHandler((_, error) => _logger.LogWarning("Kafka consumer: {Reason}", error.Reason))
            .SetLogHandler((_, message) => _logger.LogDebug("Kafka consumer: {Message}", message.Message))
            .Build();
    }

    private void ConsumeLoop(IConsumer<string?, string?> consumer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string?, string?>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                var message = new ConsumedMessage(
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key,
                    result.Message.Value);

                _dispatcher.DispatchAsync(message, stoppingToken).GetAwaiter().GetResult();
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (ConsumeException exception) when (exception.Error.IsFatal)
            {
                _logger.LogCritical(exception, "Erro fatal no consumidor Kafka: {Reason}", exception.Error.Reason);
                return;
            }
            catch (ConsumeException exception)
            {
                _logger.LogWarning(exception, "Falha ao consumir do Kafka: {Reason}", exception.Error.Reason);
                Wait(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao processar a mensagem. Ela será lida novamente.");
                Rewind(consumer, result);
                Wait(stoppingToken);
            }
        }
    }

    private void Rewind(IConsumer<string?, string?> consumer, ConsumeResult<string?, string?>? result)
    {
        if (result is null)
            return;

        try
        {
            consumer.Seek(result.TopicPartitionOffset);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Não foi possível voltar para {TopicPartitionOffset}.", result.TopicPartitionOffset);
        }
    }

    private void Wait(CancellationToken stoppingToken) =>
        stoppingToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(_options.Consumer.RetryDelayMs));
}
