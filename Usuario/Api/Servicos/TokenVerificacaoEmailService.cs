using System.Security.Cryptography;

namespace Usuario.Servicos
{
    public sealed class TokenVerificacaoEmailService
    {
        public string GerarToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(tokenBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public string CalcularHash(string token)
        {
            var tokenBytes = Convert.FromBase64String(
                token.Replace('-', '+').Replace('_', '/') +
                new string('=', (4 - token.Length % 4) % 4));

            return Convert.ToHexString(SHA256.HashData(tokenBytes));
        }
    }
}
