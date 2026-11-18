
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

    public async Task<DetalheEventoResponse> Atualizar(
        Guid id,
        AtualizarEventoRequest request)
    {
        var organizadorId = _usuarioAtual.GetId();
        var evento = await _dataContext.Set<Evento>()
            .SingleOrDefaultAsync(e => e.Id == id);

        if (evento is null)
            throw new KeyNotFoundException("Evento não encontrado.");

        if (evento.OrganizadorId != organizadorId)
            throw new UnauthorizedAccessException(
                "Apenas o organizador pode alterar este evento."
            );

        var slugAnterior = evento.Slug;

        // Valida o período resultante e preserva os campos ausentes.
        evento.AtualizarInformacoes(
            request.Nome,
            request.Descricao,
            request.Local,
            request.Inicio,
            request.Fim
        );

        // Ao trocar o nome, gera um slug único sem contar o próprio evento.
        // Se o slug-base permanecer igual, mantém o número atual e a URL.
        if (evento.Slug != slugAnterior)
        {
            var ultimoNumero = await _dataContext.Set<Evento>()
                .AsNoTracking()
                .Where(e => e.Id != id && e.Slug == evento.Slug)
                .Select(e => (int?)e.SlugNumero)
                .MaxAsync();

            evento.DefinirNumeroSlug((ultimoNumero ?? 0) + 1);
        }

        await _dataContext.SaveChangesAsync();
        return EventoMapper.ToDetalheResponse(evento);
    }
}
