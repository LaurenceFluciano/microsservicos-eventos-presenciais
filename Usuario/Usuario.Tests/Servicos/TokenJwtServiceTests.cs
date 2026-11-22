using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Usuario.Servicos;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class TokenJwtServiceTests
{
    [Fact]
    public void GerarToken_DeveAssinarComRsaEConterSubEmailIssuerEAudience()
    {
        using var rsa = RSA.Create(2048);
        var service = CriarService(rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
        var userId = Guid.CreateVersion7();

        var generated = service.GerarToken(userId, "ana@example.com");
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            generated.Token,
            service.CriarParametrosValidacao(),
            out var validatedToken);

        Assert.Equal(SecurityAlgorithms.RsaSha256, ((JwtSecurityToken)validatedToken).Header.Alg);
        Assert.Equal(userId.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal("ana@example.com", principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
        Assert.Equal("Usuario.Tests", ((JwtSecurityToken)validatedToken).Issuer);
        Assert.Contains("Usuario.Tests.Client", ((JwtSecurityToken)validatedToken).Audiences);
        Assert.True(generated.ExpiraEm > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Construtor_ComChavesQueNaoFormamPar_DeveFalhar()
    {
        using var privateRsa = RSA.Create(2048);
        using var differentRsa = RSA.Create(2048);

        Assert.Throws<InvalidOperationException>(() => CriarService(
            privateRsa.ExportPkcs8PrivateKeyPem(),
            differentRsa.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Construtor_SemIssuerConfigurado_DeveFalharExplicitamente()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => new TokenJwtService(configuration));

        Assert.Contains("Jwt:Issuer", error.Message);
    }

    private static TokenJwtService CriarService(string privateKey, string publicKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Usuario.Tests",
            ["Jwt:Audience"] = "Usuario.Tests.Client",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:PrivateKeyPem"] = privateKey,
            ["Jwt:PublicKeyPem"] = publicKey
        };

        return new TokenJwtService(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}
