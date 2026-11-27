using MediatR;

namespace Api.Application.Events;

public record IngressosGeradosEvent(
    Guid EventoId,
    Guid PedidoId,
    IReadOnlyList<Guid> IngressoIds
): INotification;
