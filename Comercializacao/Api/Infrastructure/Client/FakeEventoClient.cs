
using Api.Error;
using Api.Application.DTO.Client;
using Api.Application.Ports;

namespace Api.Infrastructure.Client;

public sealed class FakeEventoClient : IEventoClient
{
    private readonly Guid _idEvento =
        Guid.Parse("01a1f4d0-b140-7372-84cd-f2ff6f1af9f5");

    private readonly Guid _idOrganizador =
        Guid.Parse("019a6f34-8c12-7000-8000-000000000001");

    public Task<EventoClientResponse> ObterEventoAsync(
        Guid idEvento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (idEvento != _idEvento)
        {
            throw new NotFoundException(
                "Evento não encontrado.");
        }

        return Task.FromResult(
            new EventoClientResponse(
                Id: _idEvento,
                OrganizadorId: _idOrganizador,
                Nome: "Evento Exemplo Fake",
                Status: "PLANEJAMENTO",
                StatusPublicacao: "PUBLICADO",
                Inicio: new DateTime(2026, 12, 10, 18, 0, 0),
                Fim: new DateTime(2026, 12, 10, 23, 0, 0)
            )
        );
    }
}
