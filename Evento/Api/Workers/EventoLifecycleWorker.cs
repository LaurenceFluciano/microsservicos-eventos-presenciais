using Api.Application.Events;
using Api.Application.Ports;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Workers;

public sealed class EventoLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventoLifecycleWorker> _logger;

    public EventoLifecycleWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EventoLifecycleWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(1)
        );

        do
        {
            try
            {
                await ProcessarEventos(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Erro ao processar início dos eventos."
                );
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessarEventos(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dataContext = scope.ServiceProvider
            .GetRequiredService<DataContext>();

        var publisher = scope.ServiceProvider
            .GetRequiredService<IEventoPublisher>();

        var agora = DateTimeOffset.UtcNow;

        var eventosPendentes = await dataContext
            .Set<Evento>()
            .Where(e => e.InicioProcessadoEm == null)
            .ToListAsync(cancellationToken);

        var eventosIniciados = eventosPendentes
            .Where(e => e.Inicio <= agora);

        foreach (var evento in eventosIniciados)
        {
            await publisher.Publicar(
                new EventoIniciadoEvent(
                    evento.Id,
                    evento.Inicio
                ),
                cancellationToken
            );

            evento.RegistrarInicioProcessado();

            await dataContext.SaveChangesAsync(
                cancellationToken
            );

            _logger.LogInformation(
                "Início do evento {EventoId} publicado.",
                evento.Id
            );
        }
    }
}