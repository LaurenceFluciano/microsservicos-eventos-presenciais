
using System.Linq.Expressions;
using Api.Error;

namespace Api.Model;

public class Pedido
{
    public Guid Id { get; private init; }

    public Guid CompradorId { get; private init; }

    public Guid OfertaId { get; private init; }

    public OfertaIngresso OfertaIngresso { get; set; } = null!;

    public int Quantidade 
    { 
        get; 
        set
        {
            if (value <= 0)
                throw new DomainException("A quantidade total de ingressos deve ser maior que zero.");
        
            field = value;
        } 
    }

    public decimal ValorUnitario 
    { 
        get; 
        set
        {
            if (value < 0)
                throw new DomainException("O valor unitário deve ser maior ou igual a zero.");

            field = value;
        }
    }

    public bool IsGratis => ValorUnitario == 0m;

    public decimal ValorTotal()
    {
        return ValorUnitario * Quantidade;
    }

    public StatusPedido Status { get; private set; } = StatusPedido.PENDENTE;

    public DateTimeOffset CriadoEm { get; private init; }

    public DateTimeOffset ExpiraEm { get; init; }

    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();

    public ICollection<Ingresso> Ingressos { get; set; } = new List<Ingresso>();

    #region Construtores

    private Pedido() { }

    public Pedido(
        Guid idOferta,
        Guid idComprador,
        int quantidade,
        decimal valorUnitario)
    {
        Id = Guid.CreateVersion7();
        OfertaId = idOferta;
        CompradorId = idComprador;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        CriadoEm = DateTimeOffset.UtcNow;
        ExpiraEm = DateTimeOffset.UtcNow.AddMinutes(60);
        Status = StatusPedido.PENDENTE;
    }

    #endregion

    public void Cancelar()
    {

        if (EstaEmProcessamento())
            throw new DomainException("Pedidos em processamento não podem ser cancelados.");

        if (EstaCancelado())
            throw new DomainException("Pedidos cancelados não podem ser cancelados novamente.");

        if (EstaConfirmado())
            throw new DomainException("Pedidos confirmados não podem ser cancelados diretamente (solicite estorno).");

        if (EstaExpirado(DateTime.UtcNow))
            throw new DomainException("Pedidos expirados não podem ser cancelados.");

        Status = StatusPedido.CANCELADO;
    }

    public void Confirmar()
    {
        if (!EstaEmProcessamento())
            throw new DomainException(
                "Apenas pedidos em processamento podem ser confirmados."
            );

        Status = StatusPedido.CONFIRMADO;
    }

    public void MarcarEmProcessamento()
    {
        if (!EstaPendente() || EstaExpirado(DateTimeOffset.UtcNow))
            throw new DomainException(
                "O pedido não está disponível para processamento."
            );

        Status = StatusPedido.EM_PROCESSAMENTO;
    }
    
    public void Expirar(DateTimeOffset agora)
    {
        if (!EstaPendente())
            throw new DomainException(
                "Apenas pedidos pendentes podem expirar."
            );

        if (!EstaExpirado(agora))
            throw new DomainException(
                "O prazo de reserva ainda não expirou."
            );

        Status = StatusPedido.EXPIRADO;
    }

    public bool EstaExpirado(DateTimeOffset agora)
    {
        return Status == StatusPedido.EXPIRADO ||
            (
                Status == StatusPedido.PENDENTE &&
                ExpiraEm <= agora
            );
    }

    public bool EstaConfirmado() => Status == StatusPedido.CONFIRMADO;

    public bool EstaCancelado() => Status == StatusPedido.CANCELADO;

    public bool EstaPendente() => Status == StatusPedido.PENDENTE;

    public bool EstaEmProcessamento() => Status == StatusPedido.EM_PROCESSAMENTO;


    public static Expression<Func<Pedido, bool>> ComprometidosEm(DateTimeOffset agora)
    {
        return pedido =>
            pedido.Status == StatusPedido.CONFIRMADO || pedido.Status == StatusPedido.EM_PROCESSAMENTO ||
            (
                pedido.Status == StatusPedido.PENDENTE &&
                (
                    pedido.ExpiraEm > agora
                )
            );
    }
}

public enum StatusPedido
{
    PENDENTE,
    EM_PROCESSAMENTO,
    CONFIRMADO,
    EXPIRADO,
    CANCELADO
}
