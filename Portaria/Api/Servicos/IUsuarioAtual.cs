using System.Security.Claims;

namespace Usuario.Servicos
{
    public interface IUsuarioAtual
    {
        Guid? Id { get; }
    }

    public sealed class UsuarioAtual : IUsuarioAtual
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioAtual(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? Id
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                if (principal?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                var subject = principal.FindFirstValue("sub");
                return Guid.TryParse(subject, out var userId) ? userId : null;
            }
        }
    }
}
