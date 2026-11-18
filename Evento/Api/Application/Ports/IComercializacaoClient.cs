namespace Api.Application.Ports;

public interface IComercializacaoClient
{
    Task<bool> PossuiOfertaValidaAsync(
        Guid eventoId,
        CancellationToken cancellationToken = default);
}
