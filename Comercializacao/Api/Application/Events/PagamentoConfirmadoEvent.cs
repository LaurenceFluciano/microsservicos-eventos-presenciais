using MediatR;

namespace Api.Application.Events;
public sealed record PagamentoConfirmadoEvent(
    Guid PedidoId
) : INotification;