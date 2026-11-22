using MediatR;

namespace Api.Application.Events;

public record PedidoCriadoEvent(
    Guid PedidoId
): INotification;
