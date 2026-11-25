using Api.Application.Ports;

namespace Api.Infrastructure.Authentication;

// Classe que retorna o usuário atual
// utilizada em cenários de teste.
public class FakeUsuarioAtual : IUsuarioAtual
{
    public Guid UsuarioId { get; set; } = Guid.NewGuid();

    public Guid GetId()
    {
        return UsuarioId;
    }

    public void AlterarUsuario(Guid usuarioId)
    {
        UsuarioId = usuarioId;
    }
}
