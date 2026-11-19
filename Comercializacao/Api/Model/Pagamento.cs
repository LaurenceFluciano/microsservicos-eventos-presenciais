namespace Api.Model;

public class Pagamento
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PedidoId { get; set; }

    public Pedido Pedido { get; set; } = null!;

    public decimal Valor { get; set; }

    public StatusPagamento Status { get; set; } = StatusPagamento.PENDENTE;

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    // Continua nulo enquanto o pagamento nao estiver confirmado.
    public DateTimeOffset? DataPagamento { get; set; }
}

public enum StatusPagamento
{
    PENDENTE,
    CONFIRMADO,
    RECUSADO,
    CANCELADO,
    ESTORNADO
}
