using System.Text.Json;
using Api.Application.Events;
using Api.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Api.Infrastructure.Messaging;

public sealed class EventoIniciadoConsumer : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventoIniciadoConsumer> _logger;

    private const string Exchange = "eventos";
    private const string Queue = "comercializacao.eventos.iniciados";
    private const string RoutingKey = "evento.iniciado";

    public EventoIniciadoConsumer(
        ConnectionFactory factory,
        IServiceScopeFactory scopeFactory,
        ILogger<EventoIniciadoConsumer> logger)
    {
        _factory = factory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await using var connection =
            await _factory.CreateConnectionAsync(stoppingToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: stoppingToken
            );

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var notification =
                    JsonSerializer.Deserialize<EventoIniciadoEvent>(
                        args.Body.Span
                    );

                if (notification is null ||
                    notification.EventoId == Guid.Empty)
                {
                    throw new JsonException("Evento inválido.");
                }

                using var scope = _scopeFactory.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<OfertaIngressoService>();

                await service.Encerrar(
                    notification,
                    stoppingToken
                );

                await channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken
                );

                _logger.LogInformation(
                    "Ofertas do evento {EventoId} encerradas.",
                    notification.EventoId
                );
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Mensagem de integração inválida."
                );

                await channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken
                );
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // A mensagem não confirmada poderá ser entregue novamente.
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Erro ao encerrar ofertas de ingresso."
                );


                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken
                );

                await channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken
                );
            }
        };

        await channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation(
            "Escutando mensagens na fila {Queue}.",
            Queue
        );

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken
        );
    }
}