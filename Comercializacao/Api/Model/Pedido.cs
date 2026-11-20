
using System.Linq.Expressions;

namespace Api.Model;

public class Pedido
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CompradorId { get; set; }

    public Guid OfertaId { get; set; }

    public OfertaIngresso OfertaIngresso { get; set; } = null!;

    public int Quantidade { get; set; }

    public decimal ValorUnitario { get; set; }

    public StatusPedido Status { get; private set; }
        = StatusPedido.PENDENTE;

    public DateTimeOffset CriadoEm { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTime? ExpiraEm { get; set; }

    public ICollection<Pagamento> Pagamentos { get; set; }
        = new List<Pagamento>();

    public ICollection<Ingresso> Ingressos { get; set; }
        = new List<Ingresso>();


    public bool EstaExpirado(DateTime agora)
    {
        return Status == StatusPedido.EXPIRADO ||
            (
                Status == StatusPedido.PENDENTE &&
                ExpiraEm.HasValue &&
                ExpiraEm.Value <= agora
            );
    }

    public bool EstaCancelado()
    {
        return Status == StatusPedido.CANCELADO;
    }

    public static Expression<Func<Pedido, bool>> ComprometidosEm(
        DateTime agora)
    {
        return pedido =>
            pedido.Status == StatusPedido.PAGO ||
            (
                pedido.Status == StatusPedido.PENDENTE &&
                (
                    pedido.ExpiraEm == null ||
                    pedido.ExpiraEm > agora
                )
            );
    }
}

public enum StatusPedido
{
    PENDENTE,
    PAGO,
    EXPIRADO,
    CANCELADO
}
