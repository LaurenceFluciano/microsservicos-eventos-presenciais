using Api.Application.Ports;
using Api.Application.DTO;
using Api.Model;

namespace Api.Application.Services;

public sealed class IngressoDigitalService
{
    private readonly IQRCodeService _qrCodeService;
    private readonly IConfiguration _configuration;

    public IngressoDigitalService(
        IQRCodeService qrCodeService,
        IConfiguration configuration)
    {
        _qrCodeService = qrCodeService;
        _configuration = configuration;
    }

    public IngressoDigitalResponse Gerar(Ingresso ingresso)
    {
        var baseUrl = _configuration[
            "Services:Portaria:PublicBaseUrl"
        ] ?? throw new InvalidOperationException(
            "URL pública da Portaria não configurada.");

        var raiz = new Uri(baseUrl);

        var caminho =
            $"/portaria/checkin/{Uri.EscapeDataString(ingresso.CodigoIngresso)}";

        var url = new Uri(raiz, caminho).ToString();

        return new IngressoDigitalResponse(
            ingresso.Id,
            ingresso.CodigoIngresso,
            url,
            _qrCodeService.GerarQRCodeDataUri(url)
        );
    }
}