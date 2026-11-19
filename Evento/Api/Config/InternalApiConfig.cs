
namespace Api.Config;

public class InternalApiConfig
{
    public const string SectionName = "InternalApi";
    public const string HeaderName = "X-API-Key";
    public string ApiKey { get; set; } = string.Empty;
}