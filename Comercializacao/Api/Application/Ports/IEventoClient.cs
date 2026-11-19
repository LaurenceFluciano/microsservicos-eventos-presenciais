
using Api.Application.DTO.Client;

namespace Api.Application.Ports;

public interface IEventoClient
{
    Task<EventoClientResponse> ObterEventoAsync(
        Guid idEvento,
        CancellationToken cancellationToken = default
    );
}