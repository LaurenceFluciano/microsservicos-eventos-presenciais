using Api.Application.Ports;

namespace Api.Infrastructure.Integration;

// Somente para desenvolvimento/testes: nao consulta o servico real.
public sealed class FakeComercializacaoClient : IComercializacaoClient
{
    // No ambiente de desenvolvimento, simulamos uma oferta valida.
    public bool PossuiOfertaValida { get; set; } = true;

    public bool SimularIndisponibilidade { get; set; }

    public Task<bool> PossuiOfertaValidaAsync(
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (SimularIndisponibilidade)
            throw new HttpRequestException("Comercializacao indisponivel.");

        return Task.FromResult(PossuiOfertaValida);
    }
}
