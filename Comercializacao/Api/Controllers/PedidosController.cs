
using Api.Application.DTO;
using Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Api.Application.DTO.Pedidos;
using Api.Attributes;
using Api.Application.Ports;

namespace Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly PagamentoService _pagamentoService;
    private readonly IUsuarioClient _usuarioClient;

    

    public PedidosController(
        PedidoService pedidoService,
        PagamentoService pagamentoService,
        IUsuarioClient usuarioClient)
    {
        _pedidoService = pedidoService;
        _pagamentoService = pagamentoService;
        _usuarioClient = usuarioClient;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedido(
        [FromBody] CriarPedidoInputDTO dto,
        CancellationToken cancellationToken)
    {
        var comprador = await _usuarioClient.ObterPorEmailAsync(
            dto.EmailComprador,
            cancellationToken
        );

        if (comprador is null)
            return NotFound("Comprador não encontrado.");

        var request = new CriarPedidoRequest(
            OfertaId: dto.OfertaId,
            CompradorId: comprador.Id,
            Quantidade: dto.Quantidade
        );

        var pedido = await _pedidoService.CriarPedido(
            request,
            cancellationToken
        );

        return Ok(pedido);
    }


    [HttpGet("{pedidoId:guid}")]
    public async Task<IActionResult> ObterPorId(
        Guid pedidoId,
        CancellationToken cancellationToken)
    {
        var pedido = await _pedidoService.ObterPorId(
            pedidoId,
            cancellationToken
        );

        if (pedido is null)
            return NotFound();

        return Ok(pedido);
    }


    [HttpGet("{pedidoId:guid}/checkout")]
    public async Task<IActionResult> Checkout(
        Guid pedidoId,
        CancellationToken cancellationToken)
    {
        var url = await _pagamentoService.Checkout(
            pedidoId,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(url))
        {
            return Accepted(new
            {
                Status = "AGUARDANDO_COBRANCA"
            });
        }

        return Ok(new
        {
            UrlCheckout = url
        });
    }
}
