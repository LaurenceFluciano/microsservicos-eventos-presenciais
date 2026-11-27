
namespace Api.Model;

public class PagamentoTentativa
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid PagamentoId { get; private set; }

    public StatusPagamento Status { get; private set; }

    public DateTime TentadoEm { get; private set; }

    private PagamentoTentativa() { }

    public PagamentoTentativa(
        Guid pagamentoId,
        StatusPagamento status)
    {
        if (status != StatusPagamento.RECUSADO &&
            status != StatusPagamento.CONFIRMADO)
        {
            throw new ArgumentException(
                "Uma tentativa deve ser recusada ou confirmada.",
                nameof(status)
            );
        }

        PagamentoId = pagamentoId;
        Status = status;
        TentadoEm = DateTime.UtcNow;
    }
}
