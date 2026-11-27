using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace Usuario.Servicos;

public sealed class BrevoEmailSender : IEmailSender
{
    private const string SendEmailEndpoint = "https://api.brevo.com/v3/smtp/email";
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _senderEmail;
    private readonly string _senderName;
    private readonly string? _verificationUrl;

    public BrevoEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Brevo:ApiKey"];
        _senderEmail = configuration["Brevo:SenderEmail"];
        _senderName = configuration["Brevo:SenderName"] ?? "Eventos Presenciais";
        _verificationUrl = configuration["Brevo:VerificationUrl"];
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) &&
        !string.IsNullOrWhiteSpace(_senderEmail);

    public async Task SendVerificationAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new EmailSenderNotConfiguredException();
        }

        var verificationLink = CriarLinkVerificacao(token);
        var encodedToken = HtmlEncoder.Default.Encode(token);
        var linkHtml = verificationLink is null
            ? string.Empty
            : $"<p><a href=\"{HtmlEncoder.Default.Encode(verificationLink)}\">Confirmar meu e-mail</a></p>";
        var linkText = verificationLink is null
            ? string.Empty
            : $"\nConfirme seu e-mail: {verificationLink}\n";

        var message = new BrevoEmailRequest(
            new BrevoSender(_senderEmail!, _senderName),
            [new BrevoRecipient(email)],
            "Confirme seu e-mail - Eventos Presenciais",
            $"<p>Olá!</p><p>Use o código abaixo para confirmar seu endereço de e-mail:</p><p><strong>{encodedToken}</strong></p>{linkHtml}<p>Este código expira em 24 horas. Se você não solicitou a criação da conta, ignore esta mensagem.</p>",
            $"Olá!\n\nUse este código para confirmar seu endereço de e-mail: {token}\n{linkText}\nEste código expira em 24 horas. Se você não solicitou a criação da conta, ignore esta mensagem.");

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEmailEndpoint)
        {
            Content = JsonContent.Create(message)
        };
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Brevo recusou o envio do e-mail de confirmação (HTTP {(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }
    }

    private string? CriarLinkVerificacao(string token)
    {
        if (string.IsNullOrWhiteSpace(_verificationUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(_verificationUrl, UriKind.Absolute, out var verificationUri) ||
            (verificationUri.Scheme != Uri.UriSchemeHttps &&
             verificationUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "Brevo:VerificationUrl deve ser uma URL absoluta HTTP ou HTTPS.");
        }

        var builder = new UriBuilder(verificationUri);
        var query = builder.Query.TrimStart('?');
        if (query.Length > 0)
        {
            query += "&";
        }

        builder.Query = $"{query}token={Uri.EscapeDataString(token)}";
        return builder.Uri.AbsoluteUri;
    }

    private sealed record BrevoEmailRequest(
        [property: JsonPropertyName("sender")] BrevoSender Sender,
        [property: JsonPropertyName("to")] IReadOnlyList<BrevoRecipient> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("htmlContent")] string HtmlContent,
        [property: JsonPropertyName("textContent")] string TextContent);

    private sealed record BrevoSender(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);

    private sealed record BrevoRecipient(
        [property: JsonPropertyName("email")] string Email);
}
