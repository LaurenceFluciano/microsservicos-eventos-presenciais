
using Api.Application.Events;
using Api.Application.Ports;
using Api.Application.DTO.Client;
using MediatR;

namespace Api.Application.Listeners;

public class PortariaClientEventListener : INotificationHandler<IngressosGeradosEvent>
{

    private readonly IPortariaClient _portariaClient;

    public PortariaClientEventListener(IPortariaClient portariaClient)
    {
        _portariaClient = portariaClient;
    }

    public async Task Handle(IngressosGeradosEvent notification, CancellationToken cancellationToken)
    {
        foreach (var ingresso in notification.IngressoIds)
        {
            await _portariaClient.CriarCredencialAcesso(
                new CriarCredencialAcessoRequest(
                    notification.EventoId,
                    notification.PedidoId,
                    ingresso
                ),
                cancellationToken
            );
        }
    }
}