
using Api.Application.DTO;
using Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Api.Application.DTO.Pedidos;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly PagamentoService _pagamentoService;

    public PedidosController(
        PedidoService pedidoService,
        PagamentoService pagamentoService)
    {
        _pedidoService = pedidoService;
        _pagamentoService = pagamentoService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPedidoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _pedidoService.CriarPedido(
            request,
            cancellationToken
        );

        return CreatedAtAction(
            nameof(ObterPorId),
            resultado
        );
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
