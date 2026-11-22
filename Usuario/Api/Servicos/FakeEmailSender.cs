namespace Usuario.Servicos
{
    public sealed class FakeEmailSender : IEmailSender
    {
        private readonly ILogger<FakeEmailSender> _logger;

        public FakeEmailSender(ILogger<FakeEmailSender> logger)
        {
            _logger = logger;
        }

        public bool IsConfigured => true;

        public Task SendVerificationAsync(
            string email,
            string token,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogWarning(
                "Fake e-mail de confirmação para {Email}. Token de desenvolvimento: {Token}",
                email,
                token);

            return Task.CompletedTask;
        }
    }
}
