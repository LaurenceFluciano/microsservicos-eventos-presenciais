using MediatR;

namespace Api.Application.Events;

public sealed record PagamentoCriadoEvent(
    Guid PagamentoId
) : INotification;