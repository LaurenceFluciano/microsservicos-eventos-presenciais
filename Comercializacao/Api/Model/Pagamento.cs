using Api.Error;

namespace Api.Model;

public class Pagamento
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid PedidoId { get; init; }

    public string IdExterno { get; set; } = string.Empty;
    public string UrlCheckout { get; set; } = string.Empty;

    public Pedido Pedido { get; set; } = null!;

    public decimal Valor { get; init; }

    public StatusPagamento Status { get; private set; } = StatusPagamento.PENDENTE;

    public DateTimeOffset CriadoEm { get; init; } = DateTimeOffset.UtcNow;
    public DateTime? DataConfirmacao { get; private set; }
    public DateTime? CobrancaGeradaEm { get; private set; }

    private Pagamento() { }

    public Pagamento(Guid pedidoId, decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("O valor do pagamento deve ser maior que zero.");

        PedidoId = pedidoId;
        Valor = valor;
        Status = StatusPagamento.PENDENTE;
        CriadoEm = DateTimeOffset.UtcNow;
    }


    #region Métodos que alteram status

    public bool RegistrarCobranca(string idSessao)
    {
        if (string.IsNullOrWhiteSpace(idSessao))
            throw new DomainException(
                "Sessão inválida."
            );

        if (Status != StatusPagamento.PENDENTE &&
            Status != StatusPagamento.RECUSADO)
            return false;

        IdExterno = idSessao;
        CobrancaGeradaEm = DateTime.UtcNow;
        Status = StatusPagamento.COBRANCA_GERADA;

        return true;
    }


    public bool Confirmar()
    {
        if (Status == StatusPagamento.CONFIRMADO)
            return false;

        if (string.IsNullOrWhiteSpace(IdExterno))
            throw new DomainException(
                "O pagamento não possui sessão de cobrança."
            );

        if (Status != StatusPagamento.PENDENTE &&
            Status != StatusPagamento.COBRANCA_GERADA &&
            Status != StatusPagamento.RECUSADO)
            throw new DomainException(
                "O pagamento não pode ser confirmado."
            );

        Status = StatusPagamento.CONFIRMADO;
        DataConfirmacao = DateTime.UtcNow;
        return true;
    }

    public bool Recusar()
    {
        if (Status == StatusPagamento.RECUSADO ||
            Status == StatusPagamento.CONFIRMADO)
            return false;

        if (Status != StatusPagamento.COBRANCA_GERADA)
            throw new DomainException(
                "O pagamento não possui cobrança válida."
            );

        Status = StatusPagamento.RECUSADO;
        return true;
    }

    #endregion



}

public enum StatusPagamento
{
    PENDENTE,
    COBRANCA_GERADA,
    CONFIRMADO,
    RECUSADO,
    CANCELADO,
    ESTORNADO
}