using AppTemplate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Usuario.DTO;
using Usuario.Models;

namespace Usuario.Servicos
{
    public sealed class CadastroContaService
    {
        private static readonly TimeSpan ValidadeToken = TimeSpan.FromHours(24);

        private readonly DataContext _dataContext;
        private readonly IPasswordHasher<ContaModel> _passwordHasher;
        private readonly TokenVerificacaoEmailService _tokenService;
        private readonly IEmailSender _emailSender;

        public CadastroContaService(
            DataContext dataContext,
            IPasswordHasher<ContaModel> passwordHasher,
            TokenVerificacaoEmailService tokenService,
            IEmailSender emailSender)
        {
            _dataContext = dataContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        public async Task SolicitarCadastroAsync(
            CriarContaInputDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_emailSender.IsConfigured)
            {
                throw new EmailSenderNotConfiguredException();
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            var agora = DateTime.UtcNow;

            var usuarioExistente = await _dataContext.Usuarios
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (usuarioExistente?.Conta is not null ||
                (usuarioExistente is not null && await _dataContext.Contas
                    .AnyAsync(c => c.UsuarioId == usuarioExistente.Id, cancellationToken)))
            {
                throw new InvalidOperationException(
                    "Este e-mail já possui uma conta de acesso.");
            }

            var solicitacoesPendentes = await _dataContext.SolicitacoesCadastro
                .Where(s => s.Email == email && s.UtilizadoEm == null)
                .ToListAsync(cancellationToken);

            foreach (var solicitacao in solicitacoesPendentes)
            {
                solicitacao.UtilizadoEm = agora;
            }

            var token = _tokenService.GerarToken();
            var contaParaHash = new ContaModel();
            var senhaHash = _passwordHasher.HashPassword(contaParaHash, dto.Senha);

            var novaSolicitacao = new SolicitacaoCadastro
            {
                Id = Guid.CreateVersion7(),
                Nome = dto.Nome.Trim(),
                Email = email,
                Telefone = string.IsNullOrWhiteSpace(dto.Telefone)
                    ? null
                    : dto.Telefone.Trim(),
                SenhaHash = senhaHash,
                TokenHash = _tokenService.CalcularHash(token),
                CriadoEm = agora,
                ExpiraEm = agora.Add(ValidadeToken)
            };

            _dataContext.SolicitacoesCadastro.Add(novaSolicitacao);
            await _dataContext.SaveChangesAsync(cancellationToken);

            await _emailSender.SendVerificationAsync(email, token, cancellationToken);
        }

        public async Task<(Guid UsuarioId, Guid ContaId)> VerificarEmailAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            string tokenHash;
            try
            {
                tokenHash = _tokenService.CalcularHash(token.Trim());
            }
            catch (FormatException)
            {
                throw new InvalidVerificationTokenException();
            }

            await using var transaction = await _dataContext.Database
                .BeginTransactionAsync(cancellationToken);

            var solicitacao = await _dataContext.SolicitacoesCadastro
                .SingleOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

            var agora = DateTime.UtcNow;
            if (solicitacao is null ||
                solicitacao.UtilizadoEm is not null ||
                solicitacao.ExpiraEm <= agora)
            {
                throw new InvalidVerificationTokenException();
            }

            var usuario = await _dataContext.Usuarios
                .SingleOrDefaultAsync(u => u.Email == solicitacao.Email, cancellationToken);

            if (usuario is null)
            {
                usuario = new UsuarioModel
                {
                    Id = Guid.CreateVersion7(),
                    Nome = solicitacao.Nome,
                    Email = solicitacao.Email,
                    Telefone = solicitacao.Telefone,
                    CriadoEm = agora
                };

                _dataContext.Usuarios.Add(usuario);
            }

            var contaExistente = await _dataContext.Contas
                .AnyAsync(c => c.UsuarioId == usuario.Id, cancellationToken);

            if (contaExistente)
            {
                solicitacao.UtilizadoEm = agora;
                await _dataContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new InvalidOperationException(
                    "Este usuário já possui uma conta de acesso.");
            }

            var conta = new ContaModel
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = usuario.Id,
                Usuario = usuario,
                SenhaHash = solicitacao.SenhaHash,
                EmailVerificado = true,
                CriadoEm = agora
            };

            _dataContext.Contas.Add(conta);
            solicitacao.UtilizadoEm = agora;

            await _dataContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (usuario.Id, conta.Id);
        }
    }

    public sealed class InvalidVerificationTokenException : Exception
    {
        public InvalidVerificationTokenException()
            : base("Token de confirmação inválido, expirado ou já utilizado.")
        {
        }
    }
}
