
namespace Comercializacao.Model;

public class Pedido
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CompradorId { get; set; }

    public Guid OfertaId { get; set; }

    public OfertaIngresso OfertaIngresso { get; set; } = null!;

    public int Quantidade { get; set; }

    public decimal ValorUnitario { get; set; }

    public decimal ValorTotal { get; set; }

    public StatusPedido Status { get; set; }
        = StatusPedido.PENDENTE_PAGAMENTO;

    public DateTimeOffset CriadoEm { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiraEm { get; set; }

    /*public ICollection<Pagamento> Pagamentos { get; set; }
        = new List<Pagamento>();*/

    public ICollection<Ingresso> Ingressos { get; set; }
        = new List<Ingresso>();
}

public enum StatusPedido
{
    PENDENTE_PAGAMENTO,
    PAGO,
    EXPIRADO,
    CANCELADO
}
