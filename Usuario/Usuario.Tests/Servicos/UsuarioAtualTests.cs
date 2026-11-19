using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Usuario.Servicos;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class UsuarioAtualTests
{
    [Fact]
    public void Id_ComUsuarioAutenticadoEDoTipoGuid_DeveRetornarId()
    {
        var expectedId = Guid.CreateVersion7();
        var accessor = CriarAccessor(isAuthenticated: true, subject: expectedId.ToString());
        var service = new UsuarioAtual(accessor);

        Assert.Equal(expectedId, service.Id);
    }

    [Theory]
    [InlineData(false, "0d0a6ce7-eaa1-4ccd-a6b7-5072c81b949e")]
    [InlineData(true, "nao-e-um-guid")]
    [InlineData(true, null)]
    public void Id_ComIdentidadeAusenteOuSubjectInvalido_DeveRetornarNulo(
        bool isAuthenticated,
        string? subject)
    {
        var service = new UsuarioAtual(CriarAccessor(isAuthenticated, subject));

        Assert.Null(service.Id);
    }

    private static IHttpContextAccessor CriarAccessor(bool isAuthenticated, string? subject)
    {
        var claims = subject is null
            ? Array.Empty<Claim>()
            : [new Claim("sub", subject)];
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "test" : null);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return new HttpContextAccessor { HttpContext = context };
    }
}
