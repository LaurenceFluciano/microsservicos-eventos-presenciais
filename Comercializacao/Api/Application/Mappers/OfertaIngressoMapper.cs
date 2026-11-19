using Api.Application.DTO;
using Api.Model;

namespace Api.Application.Mappers;

public class OfertaIngressoMapper
{
    

    public OfertaIngresso toEntity(CriarOfertaIngressoRequest request, Guid idOrganizador)
    {
        var ofertaIngresso = new OfertaIngresso(
            request.Nome,
            request.PrecoUnitario,
            request.QuantidadeTotal,
            request.IdEvento,
            idOrganizador
        );

        return ofertaIngresso;
    }

    public OfertaIngressoResponse toResponse(OfertaIngresso entity)
    {
        return new OfertaIngressoResponse(
            Id: entity.Id,
            IdEvento: entity.IdEvento,
            Nome: entity.Nome,
            PrecoUnitario: entity.Preco,
            QuantidadeTotal: entity.QuantidadeTotal,
            QuantidadeDisponivel: entity.QuantidadeTotal,
            Status: entity.Status,
            CriadoEm: entity.CriadoEm
        );
    }
}