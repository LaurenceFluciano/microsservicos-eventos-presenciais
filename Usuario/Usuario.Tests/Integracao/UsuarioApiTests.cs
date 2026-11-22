using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTemplate;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Usuario.Tests.Suporte;
using Xunit;

namespace Usuario.Tests.Integracao;

public sealed class UsuarioApiTests
{
    [Fact]
    public async Task PostUsers_SemApiKey_DeveRetornarUnauthorized()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            nome = "Ana",
            email = "ana@example.com"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_ComApiKeyEEmailDuplicado_DeveCriarERetornarConflict()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", UsuarioApiFactory.ApiKey);
        var input = new
        {
            nome = "Ana Silva",
            email = "ana@example.com",
            telefone = "111-2222"
        };

        var created = await client.PostAsJsonAsync("/api/users", input);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var userId = createdBody.GetProperty("id").GetGuid();
        var duplicate = await client.PostAsJsonAsync("/api/users", input);
        var lookup = await client.GetAsync($"/api/users/{userId}");
        var lookupBody = await lookup.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
        Assert.Equal(userId, lookupBody.GetProperty("id").GetGuid());
        Assert.False(lookupBody.TryGetProperty("senhaHash", out _));
    }

    [Fact]
    public async Task PostUsers_ComApiKeyInvalida_DeveRetornarUnauthorized()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "chave-invalida");

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            nome = "Ana",
            email = "ana@example.com"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_ComIdInexistenteEApiKeyValida_DeveRetornarNotFound()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", UsuarioApiFactory.ApiKey);

        var response = await client.GetAsync($"/api/users/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_SemJwtOuSomenteComApiKey_DeveRetornarUnauthorized()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();

        var withoutCredentials = await client.GetAsync("/api/users/me");
        client.DefaultRequestHeaders.Add("X-API-Key", UsuarioApiFactory.ApiKey);
        var withServiceKeyOnly = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, withoutCredentials.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withServiceKeyOnly.StatusCode);
    }

    [Fact]
    public async Task LoginEAtualizacaoMe_ComJwtValido_DeveAtualizarSomenteProprioPerfil()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        var userId = await factory.CriarUsuarioComContaAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", UsuarioApiFactory.Credenciais());
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var profile = await client.GetAsync("/api/users/me");
        var profileBody = await profile.Content.ReadFromJsonAsync<JsonElement>();
        var update = await client.PatchAsJsonAsync("/api/users/me", new { nome = "Ana Atualizada" });
        var updatedBody = await update.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Bearer", loginBody.GetProperty("tokenType").GetString());
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal(userId, profileBody.GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Ana Atualizada", updatedBody.GetProperty("nome").GetString());
        Assert.Equal("111-2222", updatedBody.GetProperty("telefone").GetString());
    }

    [Fact]
    public async Task PatchMe_ComNomeInvalido_DeveRetornarBadRequestESemPersistirTelefone()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        await factory.CriarUsuarioComContaAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", UsuarioApiFactory.Credenciais());
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginBody.GetProperty("accessToken").GetString());

        var response = await client.PatchAsJsonAsync("/api/users/me", new
        {
            nome = "   ",
            telefone = "999-9999"
        });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = await context.Usuarios.SingleAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Ana Silva", user.Nome);
        Assert.Equal("111-2222", user.Telefone);
    }

    [Fact]
    public async Task Login_ComSenhaErradaUsuarioInexistenteOuContaNaoVerificada_DeveRetornarMesmo401Generico()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        await factory.CriarUsuarioComContaAsync(emailVerificado: false);

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            UsuarioApiFactory.Credenciais("Senha-Errada-123"));
        var nonexistentUser = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "naoexiste@example.com",
            senha = "Senha-Segura-123"
        });
        var unverifiedAccount = await client.PostAsJsonAsync(
            "/api/auth/login",
            UsuarioApiFactory.Credenciais());

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, nonexistentUser.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unverifiedAccount.StatusCode);
        Assert.Equal(
            await wrongPassword.Content.ReadAsStringAsync(),
            await nonexistentUser.Content.ReadAsStringAsync());
        Assert.Equal(
            await wrongPassword.Content.ReadAsStringAsync(),
            await unverifiedAccount.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_VerifyEmail_Login_DeveCompletarAtivacaoDaConta()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();
        var registration = new
        {
            nome = "Ana Silva",
            email = "ana@example.com",
            telefone = "111-2222",
            senha = "Senha-Segura-123"
        };

        var register = await client.PostAsJsonAsync("/api/auth/register", registration);
        var loginBeforeVerification = await client.PostAsJsonAsync(
            "/api/auth/login",
            UsuarioApiFactory.Credenciais());
        var verification = await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = factory.EmailSender.TokenEnviado
        });
        var loginAfterVerification = await client.PostAsJsonAsync(
            "/api/auth/login",
            UsuarioApiFactory.Credenciais());

        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginBeforeVerification.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginAfterVerification.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ComTokenInvalido_DeveRetornarBadRequest()
    {
        using var factory = new UsuarioApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = "token-invalido"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
