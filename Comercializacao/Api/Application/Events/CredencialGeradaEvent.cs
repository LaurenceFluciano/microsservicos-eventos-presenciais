using MediatR;

namespace Api.Application.Events;

public record CredencialGeradaEvent(
    Guid PedidoId
): INotification;
