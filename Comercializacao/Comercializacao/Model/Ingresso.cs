namespace Comercializacao.Model;

public class Ingresso
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PedidoId { get; set; }

    public string CodigoIngresso { get; set; } = string.Empty;

    public DateTimeOffset EmitidoEm { get; set; } = DateTimeOffset.UtcNow;


    public Pedido Pedido { get; set; } = null!;
}
