using System.Security.Cryptography;

namespace Api.Application.Services;

public static class CodigoIngressoGenerator
{
    public static string Gerar(int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 6);

        var bytes = RandomNumberGenerator.GetBytes((tamanho + 1) / 2);
        var codigo = Convert.ToHexString(bytes)[..tamanho];

        return $"ING-{codigo}";
    }
}
