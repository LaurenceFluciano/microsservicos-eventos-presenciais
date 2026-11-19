namespace Api.Application.DTO.Client;

public sealed record EventoClientResponse
(
    Guid IdEvento,
    Guid IdOrganizador,
    string Nome
);