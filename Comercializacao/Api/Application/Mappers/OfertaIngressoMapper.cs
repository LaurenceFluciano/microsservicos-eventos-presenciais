using Api.Application.DTO;
using Api.Model;

namespace Api.Application.Mappers;

public class OfertaIngressoMapper
{
    public OfertaIngresso ToEntity(CriarOfertaIngressoRequest request, Guid idOrganizador)
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

    public OfertaIngressoResponse ToResponse(OfertaIngresso entity, int quantidadeComprometida = 0)
    {
        return new OfertaIngressoResponse(
            Id: entity.Id,
            IdEvento: entity.IdEvento,
            Nome: entity.Nome,
            PrecoUnitario: entity.Preco,
            QuantidadeTotal: entity.QuantidadeTotal,
            QuantidadeDisponivel: entity.QuantidadeTotal - quantidadeComprometida,
            Status: entity.Status,
            CriadoEm: entity.CriadoEm
        );
    }


    public void ToUpdate(
        AtualizarOfertaIngressoRequest request,
        OfertaIngresso oferta)
    {
        oferta.Nome = request.Nome;
        oferta.Preco = request.PrecoUnitario;
        oferta.QuantidadeTotal = request.QuantidadeTotal;
    }
}