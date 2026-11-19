using System.Security.Claims;
using Api.Application.Ports;

namespace Api.Infrastructure.Authentication;

// Classe que retorna o usuário atual
// quando usada cenários com requisições HTTP reais.
public sealed class UsuarioAtualHttp : IUsuarioAtual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtualHttp(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetId()
    {
        var id = _httpContextAccessor.HttpContext?
            .User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(id, out var usuarioId))
            throw new UnauthorizedAccessException(
                "Usuário não autenticado."
            );

        return usuarioId;
    }
}