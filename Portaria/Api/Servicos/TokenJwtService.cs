using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Usuario.Servicos
{
	public sealed record TokenGeradoDto(string Token, DateTimeOffset ExpiraEm);

	public interface ITokenJwtService
	{
		TokenGeradoDto GerarToken(Guid usuarioId, string email);

		TokenValidationParameters CriarParametrosValidacao();
	}

	public sealed class TokenJwtService : ITokenJwtService
	{
		private readonly string _issuer;
		private readonly string _audience;
		private readonly int _accessTokenMinutes;
		private readonly RsaSecurityKey _privateKey;
		private readonly RsaSecurityKey _publicKey;

		public TokenJwtService(IConfiguration configuration)
		{
			_issuer = configuration["Jwt:Issuer"]
				?? throw new InvalidOperationException("Configure Jwt:Issuer.");
			_audience = configuration["Jwt:Audience"]
				?? throw new InvalidOperationException("Configure Jwt:Audience.");

			if (!int.TryParse(configuration["Jwt:AccessTokenMinutes"], out _accessTokenMinutes) ||
				_accessTokenMinutes <= 0)
			{
				throw new InvalidOperationException(
					"Jwt:AccessTokenMinutes precisa ser um inteiro positivo.");
			}

			var privateKeyPem = configuration["Jwt:PrivateKeyPem"]
				?? throw new InvalidOperationException("Configure Jwt:PrivateKeyPem.");
			var publicKeyPem = configuration["Jwt:PublicKeyPem"]
				?? throw new InvalidOperationException("Configure Jwt:PublicKeyPem.");

			var privateRsa = RSA.Create();
			privateRsa.ImportFromPem(privateKeyPem);
			if (privateRsa.KeySize < 2048)
			{
				throw new InvalidOperationException("A chave RSA precisa ter pelo menos 2048 bits.");
			}

			var publicRsa = RSA.Create();
			publicRsa.ImportFromPem(publicKeyPem);
			if (publicRsa.KeySize < 2048)
			{
				throw new InvalidOperationException("A chave pública RSA precisa ter pelo menos 2048 bits.");
			}

			var privatePublicKey = privateRsa.ExportSubjectPublicKeyInfo();
			var configuredPublicKey = publicRsa.ExportSubjectPublicKeyInfo();
			if (!CryptographicOperations.FixedTimeEquals(privatePublicKey, configuredPublicKey))
			{
				throw new InvalidOperationException(
					"Jwt:PrivateKeyPem e Jwt:PublicKeyPem não formam o mesmo par RSA.");
			}

			_privateKey = new RsaSecurityKey(privateRsa);
			_publicKey = new RsaSecurityKey(publicRsa);
		}

		public TokenGeradoDto GerarToken(Guid usuarioId, string email)
		{
			var now = DateTimeOffset.UtcNow;
			var expiresAt = now.AddMinutes(_accessTokenMinutes);
			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var descriptor = new SecurityTokenDescriptor
			{
				Issuer = _issuer,
				Audience = _audience,
				Subject = new ClaimsIdentity(claims),
				IssuedAt = now.UtcDateTime,
				NotBefore = now.UtcDateTime,
				Expires = expiresAt.UtcDateTime,
				SigningCredentials = new SigningCredentials(
					_privateKey,
					SecurityAlgorithms.RsaSha256)
			};

			var handler = new JwtSecurityTokenHandler();
			var token = handler.CreateToken(descriptor);
			return new TokenGeradoDto(handler.WriteToken(token), expiresAt);
		}

		public TokenValidationParameters CriarParametrosValidacao()
		{
			return new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = _publicKey,
				ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
				ValidateIssuer = true,
				ValidIssuer = _issuer,
				ValidateAudience = true,
				ValidAudience = _audience,
				ValidateLifetime = true,
				RequireExpirationTime = true,
				RequireSignedTokens = true,
				ClockSkew = TimeSpan.FromSeconds(30),
				NameClaimType = JwtRegisteredClaimNames.Sub
			};
		}
	}
}
