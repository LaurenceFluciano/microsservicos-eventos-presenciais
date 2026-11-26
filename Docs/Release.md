# Sistema de Microsserviços para Eventos Presenciais — Sandbox pública

Ambiente acadêmico de demonstração e testes de integração. Os quatro microsserviços foram implantados em 26/11/2026; a validação completa dos fluxos permanece em andamento.

| Microsserviço | API pública | Acesso informado na release |
|---|---|---|
| Usuários | [Usuários](https://microsservicos-eventos-presenciais.onrender.com) | [Abrir](https://microsservicos-eventos-presenciais.onrender.com/scalar/) |
| Eventos | [Eventos](https://microsservicos-eventos-presenciais-hctr.onrender.com) | [Abrir](https://microsservicos-eventos-presenciais-hctr.onrender.com/scalar) |
| Comercialização | [Comercialização](https://comercializacao.onrender.com) | [Abrir](https://comercializacao.onrender.com) |
| Portaria | [Portaria](https://portaria-gdve.onrender.com) | [Abrir](https://portaria-gdve.onrender.com) |

Os dois últimos links são URLs base fornecidas na release; confirmar caminho do Scalar. Não é necessário instalar o projeto localmente para acessar as APIs.

## Como testar

Cadastre dados fictícios; obtenha JWT quando exigido; crie evento futuro e oferta, publique, identifique o comprador por e-mail cadastrado, crie pedido e utilize Checkout de teste. Consulte os ingressos e teste check-in. O encerramento de ofertas ao início do evento utiliza RabbitMQ.

## Tecnologias

.NET 10 / ASP.NET Core, EF Core, SQLite, Docker, Render, CloudAMQP (RabbitMQ/AMQP), Stripe Sandbox e webhooks, MediatR, JWT RS256, API Key, OpenAPI/Scalar e QRCoder.

## Limitações

Planos gratuitos, suspensão e cold start, bancos efêmeros, possíveis atrasos de webhooks e mensageria. Não use dados pessoais reais ou cartões reais. Envio real de e-mails (Brevo), rate limiting e validação integral remota permanecem pendentes; não há disponibilidade contínua garantida. Consulte `Docs/Implantacao.md` no repositório.
