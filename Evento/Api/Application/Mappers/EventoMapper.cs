using Api.Application.DTO;
using Api.Model;

namespace Api.Application.Mappers;

public static class EventoMapper
{
    public static Evento ToEntity(
        CriarEventoRequest request,
        Guid organizadorId)
    {
        if (!request.Inicio.HasValue ||
            !request.Fim.HasValue)
        {
            throw new ArgumentException(
                "As datas de início e término são obrigatórias."
            );
        }

        return new Evento(
            organizadorId,
            request.Nome,
            request.Inicio.Value,
            request.Fim.Value
        )
        {
            Descricao = request.Descricao ?? string.Empty,
            Local = request.Local ?? string.Empty
        };
    }

    public static EventoResponse ToEventoResponse(Evento evento, string? publicBaseUrl = null)
    {
        var caminhoPublico = $"/api/eventos/publicado/{Uri.EscapeDataString(evento.SlugPublico)}";

        return new EventoResponse
        {
            Id = evento.Id,
            OrganizadorId = evento.OrganizadorId,
            Nome = evento.Nome,
            Descricao = evento.Descricao,
            Local = evento.Local,
            Slug = evento.SlugPublico,
            Inicio = evento.Inicio,
            Fim = evento.Fim,
            StatusPublicacao = evento.GetStatusPublicacao(),
            Status = evento.GetStatus(),
            CriadoEm = evento.CriadoEm,
            PublicadoEm = evento.PublicadoEm,
            PublicacaoEncerradaEm = evento.PublicacaoEncerradaEm,
            UrlEvento = evento.PublicadoEm.HasValue
                ? string.IsNullOrWhiteSpace(publicBaseUrl)
                    ? caminhoPublico
                    : $"{publicBaseUrl.TrimEnd('/')}{caminhoPublico}"
                : null
        };
    }

    public static ResumoEventoResponse ToResumoResponse(
        Evento evento,
        string? publicBaseUrl = null)
    {
        var detalhe = ToEventoResponse(evento, publicBaseUrl);

        return new ResumoEventoResponse
        {
            Id = detalhe.Id,
            Nome = detalhe.Nome,
            Local = detalhe.Local,
            Slug = detalhe.Slug,
            Inicio = detalhe.Inicio,
            Fim = detalhe.Fim,
            Status = detalhe.Status,
            StatusPublicacao = detalhe.StatusPublicacao,
            UrlEvento = detalhe.UrlEvento
        };
    }

    public static DetalheEventoResponse ToDetalheResponse(
        Evento evento)
    {
        return new DetalheEventoResponse
        {
            Id = evento.Id,
            OrganizadorId = evento.OrganizadorId,

            Nome = evento.Nome,
            Descricao = evento.Descricao,
            Local = evento.Local,
            Slug = evento.SlugPublico,

            Inicio = evento.Inicio,
            Fim = evento.Fim,

            StatusPublicacao = evento.GetStatusPublicacao(),
            Status = evento.GetStatus(),

            CriadoEm = evento.CriadoEm,
            PublicadoEm = evento.PublicadoEm,
            PublicacaoEncerradaEm = evento.PublicacaoEncerradaEm
        };
    }
}