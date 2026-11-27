using MediatR;

namespace Api.Application.Events;


public sealed record PagamentoRecusadoEvent(
    Guid PagamentoId,
    string IdSessaoStripe
) : INotification;