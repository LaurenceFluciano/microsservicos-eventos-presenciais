
using System.Security.Claims;
using Api.Application.Ports;

namespace Api.Infrastructure.Authentication;

public sealed class UsuarioAtualHttp : IUsuarioAtual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtualHttp(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetId()
    {
        var usuario = _httpContextAccessor.HttpContext?.User;

        if (usuario?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "Usuário não autenticado.");
        }

        var id = usuario.FindFirstValue("sub");

        if (!Guid.TryParse(id, out var usuarioId))
        {
            throw new UnauthorizedAccessException(
                "Identificador do usuário inválido no JWT.");
        }

        return usuarioId;
    }
}
