using Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Usuario.DTO;
using Usuario.Models;

namespace Usuario.Servicos
{
	public sealed class AuthService
	{
		private const string InvalidCredentialsMessage = "Credenciais inválidas.";

		private readonly DataContext _dataContext;
		private readonly IPasswordHasher<ContaModel> _passwordHasher;
		private readonly ITokenJwtService _tokenJwtService;

		public AuthService(
			DataContext dataContext,
			IPasswordHasher<ContaModel> passwordHasher,
			ITokenJwtService tokenJwtService)
		{
			_dataContext = dataContext;
			_passwordHasher = passwordHasher;
			_tokenJwtService = tokenJwtService;
		}

		public async Task<LoginOutputDTO> LoginAsync(
			LoginInputDTO input,
			CancellationToken cancellationToken = default)
		{
			var emailNormalizado = input.Email.Trim().ToLowerInvariant();
			var conta = await _dataContext.Contas
				.Include(c => c.Usuario)
				.SingleOrDefaultAsync(
					c => c.Usuario.Email == emailNormalizado,
					cancellationToken);

			if (conta is null || !conta.EmailVerificado)
			{
				throw new UnauthorizedAccessException(InvalidCredentialsMessage);
			}

			var verificationResult = _passwordHasher.VerifyHashedPassword(
				conta,
				conta.SenhaHash,
				input.Senha);

			if (verificationResult == PasswordVerificationResult.Failed)
			{
				throw new UnauthorizedAccessException(InvalidCredentialsMessage);
			}

			if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
			{
				conta.SenhaHash = _passwordHasher.HashPassword(conta, input.Senha);
				await _dataContext.SaveChangesAsync(cancellationToken);
			}

			var generatedToken = _tokenJwtService.GerarToken(conta.UsuarioId, conta.Usuario.Email);
			return new LoginOutputDTO
			{
				AccessToken = generatedToken.Token,
				ExpiresAt = generatedToken.ExpiraEm,
				TokenType = "Bearer"
			};
		}
	}
}
