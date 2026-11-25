using System.Text.RegularExpressions;
using Usuario.Servicos;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class TokenVerificacaoEmailServiceTests
{
    private readonly TokenVerificacaoEmailService _service = new();

    [Fact]
    public void GerarToken_DeveGerarTokenBase64UrlCom32Bytes()
    {
        var token = _service.GerarToken();

        Assert.Equal(43, token.Length);
        Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), token);
    }

    [Fact]
    public void CalcularHash_DeveSerDeterministicoEProduzirSha256()
    {
        var token = _service.GerarToken();

        var primeiroHash = _service.CalcularHash(token);
        var segundoHash = _service.CalcularHash(token);

        Assert.Equal(primeiroHash, segundoHash);
        Assert.Matches(new Regex("^[A-F0-9]{64}$"), primeiroHash);
    }

    [Fact]
    public void GerarTokensDiferentes_DeveProduzirHashesDiferentes()
    {
        var primeiroToken = _service.GerarToken();
        var segundoToken = _service.GerarToken();

        Assert.NotEqual(primeiroToken, segundoToken);
        Assert.NotEqual(
            _service.CalcularHash(primeiroToken),
            _service.CalcularHash(segundoToken));
    }
}
