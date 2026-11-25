using Api.Application.DTO;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Error;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class PortariaService
{
    private DataContext _dataContext;
    private PortariaMapper _portariaMapper;
    private IUsuarioAtual _usuarioAtual;
    private IComercializacaoClient _comercializacaoClient;

    public PortariaService(
        DataContext dataContext,
        PortariaMapper portariaMapper,
        IUsuarioAtual usuarioAtual,
        IComercializacaoClient comercializacaoClient
    )
    {
        _dataContext = dataContext;
        _portariaMapper = portariaMapper;
        _usuarioAtual = usuarioAtual;
        _comercializacaoClient = comercializacaoClient;
    }

    public async Task CriarCredencialAcesso(
        CriarCredencialAcessoRequest criarCredencialAcessoRequest,
        CancellationToken cancellationToken = default)
    {
        var credencial = _portariaMapper.ToEntity(criarCredencialAcessoRequest);

        _dataContext.CredenciaisAcesso.Add(credencial);
        await _dataContext.SaveChangesAsync(cancellationToken);
    }


    
    public async Task CheckIn(
        string codigoIngresso,
        CancellationToken cancellationToken = default)
    {
        var organizadorId = _usuarioAtual.GetId();

        var ingresso = await _comercializacaoClient
            .ObterIngressoPorCodigo(
                codigoIngresso,
                cancellationToken
            );

        if (ingresso.OrganizadorId != organizadorId)
        {
            throw new UnauthorizedAccessException(
                "Você não tem permissão para realizar check-in neste evento.");
        }


        var agora = DateTime.Now;

        if (agora.Date != ingresso.Inicio.Date ||
            agora >= ingresso.Fim)
        {
            throw new DomainException(
                "O check-in só pode ser realizado no dia do evento, antes do seu encerramento.");
        }

        var linhasAfetadas = await _dataContext.CredenciaisAcesso
            .Where(ca => ca.IngressoId == ingresso.Id)
            .Where(ca => ca.EventoId == ingresso.EventoId)
            .Where(ca => ca.Status == StatusCredencialAcesso.ATIVA)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        ca => ca.Status,
                        StatusCredencialAcesso.UTILIZADA)
                    .SetProperty(
                        ca => ca.DataUtilizacao,
                        agora),
                cancellationToken
            );

        if (linhasAfetadas == 1)
            return;

        if (linhasAfetadas > 1)
        {
            throw new InvalidOperationException(
                "Mais de uma credencial foi atualizada para o mesmo ingresso.");
        }

        var credencial = await _dataContext.CredenciaisAcesso
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ca => ca.IngressoId == ingresso.Id &&
                    ca.EventoId == ingresso.EventoId,
                cancellationToken
            );

        if (credencial is null)
        {
            throw new NotFoundException(
                "Credencial de acesso não encontrada.");
        }

        if (credencial.Status == StatusCredencialAcesso.UTILIZADA)
        {
            throw new DomainException(
                "Esse ingresso já foi utilizado!");
        }

        if (credencial.Status == StatusCredencialAcesso.CANCELADA)
        {
            throw new DomainException(
                "Esse ingresso foi cancelado!");
        }

        throw new DomainException(
            "Não foi possível realizar o check-in.");
    }

}

