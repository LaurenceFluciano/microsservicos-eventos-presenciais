namespace Api.Application.Events;

public sealed record EventoIniciadoEvent(
    Guid EventoId,
    DateTimeOffset Inicio
);