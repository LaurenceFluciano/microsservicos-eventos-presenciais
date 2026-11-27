using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Usuario.Servicos;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class BrevoEmailSenderTests
{
    [Fact]
    public async Task SendVerificationAsync_DeveEnviarPayloadPelaApiBrevoComLinkTokenizado()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.Created);
        using var client = new HttpClient(handler);
        var sender = new BrevoEmailSender(client, CriarConfiguracao(
            verificationUrl: "https://site.example/confirmar-email?origem=cadastro"));

        await sender.SendVerificationAsync("ana@example.com", "token-de-teste");

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.RequestUri?.ToString());
        Assert.Equal("api-key-de-teste", handler.ApiKey);

        using var payload = JsonDocument.Parse(handler.Body!);
        var root = payload.RootElement;
        Assert.Equal("no-reply@example.com", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Eventos Presenciais", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("ana@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains("token=token-de-teste", root.GetProperty("htmlContent").GetString());
        Assert.Contains("token-de-teste", root.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task SendVerificationAsync_ComFalhaDaBrevo_DeveExporStatusHttp()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.Unauthorized);
        using var client = new HttpClient(handler);
        var sender = new BrevoEmailSender(client, CriarConfiguracao());

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendVerificationAsync("ana@example.com", "token-de-teste"));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
    }

    [Fact]
    public async Task SendVerificationAsync_SemConfiguracao_DeveFalharSemFazerRequisicao()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.Created);
        using var client = new HttpClient(handler);
        var sender = new BrevoEmailSender(client, CriarConfiguracao(includeApiKey: false));

        Assert.False(sender.IsConfigured);
        await Assert.ThrowsAsync<EmailSenderNotConfiguredException>(() =>
            sender.SendVerificationAsync("ana@example.com", "token-de-teste"));
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task SendVerificationAsync_ComUrlDeVerificacaoInvalida_DeveFalharAntesDaRequisicao()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.Created);
        using var client = new HttpClient(handler);
        var sender = new BrevoEmailSender(client, CriarConfiguracao("file:///local"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendVerificationAsync("ana@example.com", "token-de-teste"));
        Assert.Null(handler.RequestUri);
    }

    private static IConfiguration CriarConfiguracao(
        string? verificationUrl = null,
        bool includeApiKey = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["Brevo:ApiKey"] = includeApiKey ? "api-key-de-teste" : null,
            ["Brevo:SenderEmail"] = "no-reply@example.com",
            ["Brevo:SenderName"] = "Eventos Presenciais",
            ["Brevo:VerificationUrl"] = verificationUrl
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public CapturingHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            ApiKey = request.Headers.GetValues("api-key").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_statusCode);
        }
    }
}
