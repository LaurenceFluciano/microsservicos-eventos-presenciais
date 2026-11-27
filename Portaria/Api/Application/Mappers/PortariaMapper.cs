using Api.Model;
using Api.Application.DTO;


namespace Api.Application.Mappers;


public class PortariaMapper
{
    
    public CredencialAcesso ToEntity(
        CriarCredencialAcessoRequest request
    )
    {
        return new CredencialAcesso(
            request.IdIngresso,
            request.EventoId
        );
    }
}