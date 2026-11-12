
namespace Comercializacao.Model;

public class OfertaIngresso
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid EventoId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int QuantidadeTotal { get; set; }

    public StatusOfertaIngresso Status { get; set; }
        = StatusOfertaIngresso.ATIVA;

    public DateTimeOffset CriadoEm { get; set; }
        = DateTimeOffset.UtcNow;

    public ICollection<Pedido> Pedidos { get; set; }
        = new List<Pedido>();
}

public enum StatusOfertaIngresso
{
    ATIVA,
    ENCERRADA
}
