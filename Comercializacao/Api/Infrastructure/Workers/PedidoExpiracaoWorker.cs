using Api.Application.Services;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Workers;

public class PedidoExpiracaoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PedidoExpiracaoWorker> _logger;

    public PedidoExpiracaoWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PedidoExpiracaoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(1)
        );

        do
        {
            try
            {
                await ProcessarPedidos(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao consultar pedidos expirados."
                );
            }

        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessarPedidos(
        CancellationToken cancellationToken)
    {
        var agora = DateTimeOffset.UtcNow;

        List<Guid> pedidosIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var dataContext = scope.ServiceProvider
                .GetRequiredService<DataContext>();

            pedidosIds = await dataContext.Pedidos
                .AsNoTracking()
                .Where(p =>
                    p.Status == StatusPedido.PENDENTE &&
                    p.ExpiraEm <= agora
                )
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        foreach (var pedidoId in pedidosIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<ExpirarPedido>();

                await service.Expirar(
                    pedidoId,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao expirar pedido {PedidoId}.",
                    pedidoId
                );
            }
        }
    }
}