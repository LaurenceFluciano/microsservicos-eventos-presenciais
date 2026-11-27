using Api.Application.Events;

namespace Api.Application.Ports;

public interface IEventoPublisher
{
    Task Publicar(
        EventoIniciadoEvent evento,
        CancellationToken cancellationToken = default
    );
}