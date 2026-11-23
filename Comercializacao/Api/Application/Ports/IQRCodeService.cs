namespace Api.Application.Ports;

public interface IQRCodeService
{
    string GerarQRCodeDataUri(string content, int pixelsPerModule = 20);
}
