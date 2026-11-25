using MediatR;

namespace Api.Application.Events;

public sealed record PagamentoAprovadoEvent(
    Guid PagamentoId,
    string IdSessaoStripe
) : INotification;