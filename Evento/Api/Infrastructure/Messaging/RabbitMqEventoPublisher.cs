using System.Text.Json;
using Api.Application.Events;
using Api.Application.Ports;
using RabbitMQ.Client;

namespace Api.Infrastructure.Messaging;

public sealed class RabbitMqEventoPublisher : IEventoPublisher
{
    private readonly ConnectionFactory _factory;

    public RabbitMqEventoPublisher(
        ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task Publicar(
        EventoIniciadoEvent evento,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _factory.CreateConnectionAsync(
                cancellationToken
            );

        await using var channel =
            await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true
                ),
                cancellationToken
            );

        await channel.ExchangeDeclareAsync(
            exchange: "eventos",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        var body = JsonSerializer.SerializeToUtf8Bytes(evento);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true,
            MessageId = $"evento-iniciado-{evento.EventoId}"
        };

        await channel.BasicPublishAsync(
            exchange: "eventos",
            routingKey: "evento.iniciado",
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
    }
}