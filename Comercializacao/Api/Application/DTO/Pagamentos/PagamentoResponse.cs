namespace Api.Application.DTO.Pagamentos;

public record PagamentoResponse(
    Guid PagamentoId,
    string UrlPagamento
);