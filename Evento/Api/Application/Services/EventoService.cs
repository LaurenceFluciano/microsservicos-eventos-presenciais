
using Api.Application.DTO;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class EventoService
{
    private readonly DataContext _dataContext;
    private readonly IUsuarioAtual _usuarioAtual;

    public EventoService(
        DataContext dataContext,
        IUsuarioAtual usuarioAtual)
    {
        _dataContext = dataContext;
        _usuarioAtual = usuarioAtual;
    }

    public async Task<DetalheEventoResponse> Cadastrar(
        CriarEventoRequest request)
    {
        var organizadorId = _usuarioAtual.GetId();

        var evento = EventoMapper.ToEntity(
            request,
            organizadorId
        );

        var ultimoNumero = await _dataContext
            .Set<Evento>()
            .AsNoTracking()
            .Where(e => e.Slug == evento.Slug)
            .Select(e => (int?)e.SlugNumero)
            .MaxAsync();

        evento.DefinirNumeroSlug(
            (ultimoNumero ?? 0) + 1
        );

        _dataContext.Set<Evento>().Add(evento);

        await _dataContext.SaveChangesAsync();

        return EventoMapper.ToDetalheResponse(evento);
    }
}
