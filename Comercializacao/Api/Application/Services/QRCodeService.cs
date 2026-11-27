
using Api.Application.Ports;
using QRCoder;

namespace Api.Application.Services;

public class QRCodeService : IQRCodeService
{
    public string GerarQRCodeDataUri(string content, int pixelsPerModule = 20)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        
        var pngQr = new PngByteQRCode(qrCodeData);
        var pngBytes = pngQr.GetGraphic(pixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}