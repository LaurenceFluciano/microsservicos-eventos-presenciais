using MediatR;

namespace Api.Application.Events;

public sealed record CobrancaGeradaEvent(
    Guid PagamentoId,
    string IdSessaoStripe
) : INotification;