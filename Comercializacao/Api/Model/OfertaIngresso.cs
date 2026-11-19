using Api.Error;

namespace Api.Model;

public class OfertaIngresso
{
    public Guid Id { get; init; }

    public Guid IdEvento { get; init; }

    public Guid IdOrganizador { get; init; }

    public string Nome
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(
                    "O nome da oferta é obrigatório."
                );
            }

            var nomeNormalizado = value.Trim();

            field = nomeNormalizado;
        }
    } = string.Empty;

    public decimal Preco
    {
        get;
        set
        {
            if (value < 0)
                throw new DomainException(
                    "O preço unitário deve ser maior ou igual a zero."
                );

            field = value;
        }
    }
    public bool IsGratis => Preco == 0m;

    public int QuantidadeTotal { 
        get; 
        set
        {
            if (value <= 0)
                throw new DomainException("A quantidade total de ingressos deve ser maior que zero.");
        
            field = value;
        } 
        
    }

    public StatusOfertaIngresso Status { get; private set; }

    public DateTimeOffset CriadoEm { get; init; }

    public ICollection<Pedido> Pedidos { get; set; }
        = new List<Pedido>();

    
    // Construtores
    private OfertaIngresso()
    {
        
    }

    public OfertaIngresso(string nome, decimal preco, int quantidade, Guid idEvento, Guid idOrganizador)
    {
        if (idEvento == Guid.Empty)
            throw new DomainException("O evento é obrigatório.");

        if (idOrganizador == Guid.Empty)
            throw new DomainException("O organizador é obrigatório.");

        Id = Guid.CreateVersion7();
        Nome = nome;
        Preco = preco;
        IdEvento = idEvento;
        IdOrganizador = idOrganizador;
        QuantidadeTotal = quantidade;
        Status = StatusOfertaIngresso.ATIVA;
        CriadoEm = DateTimeOffset.UtcNow;
    }


    public void Encerrar()
    {
        if (Status == StatusOfertaIngresso.ESGOTADO)
            throw new DomainException("Os ingressos esgotados da oferta não podem ser encerrados.");

        Status = StatusOfertaIngresso.ENCERRADA;
    }

    public void Esgotado()
    {
        if (Status == StatusOfertaIngresso.ENCERRADA)
            throw new DomainException("Ofertas encerradas não podem ser esgotadas!");

        Status = StatusOfertaIngresso.ESGOTADO;
    }
}

public enum StatusOfertaIngresso
{
    ATIVA,
    ESGOTADO,
    ENCERRADA
}
