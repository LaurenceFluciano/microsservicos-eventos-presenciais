
using Api.Application.DTO;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Error;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class EventoService
{
    private readonly DataContext _dataContext;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IComercializacaoClient _comercializacaoClient;

    public EventoService(
        DataContext dataContext,
        IUsuarioAtual usuarioAtual,
        IComercializacaoClient comercializacaoClient)
    {
        _dataContext = dataContext;
        _usuarioAtual = usuarioAtual;
        _comercializacaoClient = comercializacaoClient;
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

    public async Task<IReadOnlyList<ResumoEventoResponse>> ListarMeus(
        string? ordem = "asc",
        bool? publicado = null,
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        if (!string.Equals(ordem, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ordem, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A ordem deve ser 'asc' ou 'desc'.",
                nameof(ordem)
            );
        }

        var organizadorId = _usuarioAtual.GetId();

        IQueryable<Evento> consulta = _dataContext.Set<Evento>()
            .AsNoTracking()
            .Where(e => e.OrganizadorId == organizadorId);

        if (publicado.HasValue)
            consulta = publicado.Value
                ? consulta.Where(e => e.PublicadoEm != null)
                : consulta.Where(e => e.PublicadoEm == null);


        var eventos = await consulta.ToListAsync(cancellationToken);

        // APENAS PARA ESCOPO ACADEMICO!
        var eventosOrdenados = string.Equals(
            ordem,
            "desc",
            StringComparison.OrdinalIgnoreCase)
            ? eventos.OrderByDescending(e => e.Inicio).ThenBy(e => e.Id)
            : eventos.OrderBy(e => e.Inicio).ThenBy(e => e.Id);

        return eventosOrdenados
            .Select(e => EventoMapper.ToResumoResponse(e, publicBaseUrl))
            .ToList();
    }

    public async Task<EventoIntegracaoResponse> ObterParaIntegracaoAsync(
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Set<Evento>()
            .AsNoTracking()
            .Where(e => e.Id == eventoId)
            .Select(e => new EventoIntegracaoResponse(
                e.Id,
                e.OrganizadorId,
                e.Nome,
                e.GetStatus(),
                e.GetStatusPublicacao(),
                e.Inicio,
                e.Fim
            ))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Evento não encontrado.");
    }

    public async Task<EventoResponse> ConsultarMeu(
        Guid id,
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        var organizadorId = _usuarioAtual.GetId();
        var evento = await _dataContext.Set<Evento>()
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evento is null)
            throw new KeyNotFoundException("Evento nao encontrado.");

        if (evento.OrganizadorId != organizadorId)
            throw new UnauthorizedAccessException(
                "Apenas o organizador pode consultar este evento.");

        return EventoMapper.ToEventoResponse(evento, publicBaseUrl);
    }

    public async Task<EventoResponse> Publicar(
        Guid id,
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        var organizadorId = _usuarioAtual.GetId();
        var evento = await _dataContext.Set<Evento>()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evento is null)
            throw new KeyNotFoundException("Evento nao encontrado.");

        if (evento.OrganizadorId != organizadorId)
            throw new UnauthorizedAccessException(
                "Apenas o organizador pode publicar este evento.");

        // Publicacao repetida e idempotente: nao consulta Comercializacao novamente.
        if (evento.PublicadoEm.HasValue)
            return EventoMapper.ToEventoResponse(evento, publicBaseUrl);



        bool possuiOfertaValida = await _comercializacaoClient
            .PossuiOfertaValidaAsync(id, cancellationToken);


        if (!possuiOfertaValida)
            throw new Api.Error.DomainException(
                "O evento precisa ter pelo menos uma oferta de ingresso valida para ser publicado.");

        evento.Publicar();
        await _dataContext.SaveChangesAsync(cancellationToken);
        return EventoMapper.ToEventoResponse(evento, publicBaseUrl);
    }


    public async Task<IReadOnlyList<ResumoEventoResponse>> ListarPublicadosAsync(
        string ordem = "asc",
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        if (!string.Equals(ordem, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ordem, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A ordem deve ser 'asc' ou 'desc'.",
                nameof(ordem));
        }

        IQueryable<Evento> consulta = _dataContext.Set<Evento>()
            .AsNoTracking()
            .Where(e => e.PublicadoEm != null);

        consulta = string.Equals(
            ordem,
            "desc",
            StringComparison.OrdinalIgnoreCase)
            ? consulta.OrderByDescending(e => e.Inicio).ThenBy(e => e.Id)
            : consulta.OrderBy(e => e.Inicio).ThenBy(e => e.Id);

        var eventos = await consulta.ToListAsync(cancellationToken);

        return eventos
            .Select(e => EventoMapper.ToResumoResponse(e, publicBaseUrl))
            .ToList();
    }

    public async Task<EventoResponse> ListarPublicadosPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        var evento = await _dataContext.Set<Evento>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == id &&
                    e.PublicadoEm != null &&
                    e.PublicacaoEncerradaEm == null,
                cancellationToken
            );

        if (evento is null)
        {
            throw new NotFoundException(
                "Evento publicado não encontrado.");
        }

        return EventoMapper.ToEventoResponse(
            evento,
            publicBaseUrl);
    }


}
