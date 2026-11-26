# Referência técnica de configurações

Este documento identifica as propriedades utilizadas pelos quatro microsserviços e seus requisitos de segurança. Consulte [Configuração local](ConfiguracaoLocal/) para Development e [Implantação](Implantacao.md) para a Sandbox remota.

## 1. Convenções

As aplicações ASP.NET Core utilizam `IConfiguration`. A propriedade `Jwt:Issuer`, por exemplo, corresponde à variável de ambiente `Jwt__Issuer`. Configurações podem vir de `appsettings.json`, arquivos específicos do ambiente, User Secrets (Development) e variáveis de ambiente (Render).

A classificação sensível indica que o valor deve ser protegido. A obrigatoriedade depende do ambiente e da funcionalidade correspondente. Não confundir `Production` com operação comercial real: a implantação atual é uma Sandbox acadêmica.

## 2. Microsserviço Usuários

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Emissor do JWT | Não |
| `Jwt:Audience` | Audiência do JWT | Não |
| `Jwt:AccessTokenMinutes` | Duração dos tokens | Não |
| `Jwt:PublicKeyPem` | Chave pública, se exigida pela implementação | Não |
| `Jwt:PrivateKeyPem` | Assinatura RSA dos tokens | **Sim** |
| `ConnectionStrings:DefaultConnection` | Banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens autorizadas | Não |
| `Authentication:ApiKey` | Autenticação interna, inclusive consulta por e-mail | **Sim** |

A chave privada permanece restrita ao emissor. Confirmar o uso efetivo da chave pública no serviço. O envio de e-mail utiliza FakeEmailSender; Brevo ainda é uma evolução proposta, não configuração obrigatória atual.

## 3. Microsserviço Eventos

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor | Não |
| `Jwt:Audience` | Validação da audiência | Não |
| `Jwt:PublicKeyPem` | Validação da assinatura JWT | Não |
| `Authentication:ApiKey` | Autenticação interna | **Sim** |
| `Services:Comercializacao:BaseUrl` | API de Comercialização | Não |
| `Eventos:PublicBaseUrl` | Composição das URLs públicas de eventos; confirmar nome no código atual | Não |
| `ConnectionStrings:DefaultConnection` | Banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens autorizadas | Não |
| `RabbitMQ:HostName` | Host do broker local | Não |
| `RabbitMQ:Port` | Porta AMQP local | Não |
| `RabbitMQ:UserName` | Usuário do broker local | **Sim** |
| `RabbitMQ:Password` | Senha do broker local | **Sim** |
| `RabbitMQ:Uri` | URI AMQPS do CloudAMQP (Production) | **Sim** |

## 4. Microsserviço Comercialização

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor | Não |
| `Jwt:Audience` | Validação da audiência | Não |
| `Jwt:PublicKeyPem` | Validação da assinatura | Não |
| `Authentication:ApiKey` | Autenticação interna | **Sim** |
| `Stripe:SecretKey` | Chave da API Stripe Sandbox | **Sim** |
| `Stripe:WebhookSecret` | Validação do webhook correspondente ao ambiente | **Sim** |
| `Stripe:SuccessUrl` | Retorno de Checkout | Não |
| `Stripe:CancelUrl` | Retorno ao sair de Checkout | Não |
| `Services:Eventos:BaseUrl` | Consulta de dados e estado temporal do evento | Não |
| `Services:Usuarios:BaseUrl` | Consulta do comprador por e-mail | Não |
| `Services:Portaria:BaseUrl` | API de Portaria para credenciais | Não |
| `Services:Portaria:PublicBaseUrl` | URLs públicas dos ingressos | Não |
| `ConnectionStrings:DefaultConnection` | Banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens autorizadas | Não |
| `RabbitMQ:HostName` | Host do broker local | Não |
| `RabbitMQ:Port` | Porta AMQP local | Não |
| `RabbitMQ:UserName` | Usuário do broker local | **Sim** |
| `RabbitMQ:Password` | Senha do broker local | **Sim** |
| `RabbitMQ:Uri` | URI AMQPS do CloudAMQP (Production) | **Sim** |

## 5. Microsserviço Portaria

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor | Não |
| `Jwt:Audience` | Validação da audiência | Não |
| `Jwt:PublicKeyPem` | Validação da assinatura | Não |
| `Authentication:ApiKey` | Autenticação interna | **Sim** |
| `Services:Comercializacao:BaseUrl` | Consulta de ingressos | Não |
| `ConnectionStrings:DefaultConnection` | Banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens autorizadas | Não |

Portaria não emite JWTs e não requer `Jwt:AccessTokenMinutes`.

## 6. Segurança e gerenciamento de segredos

Não versionar chaves privadas, API Keys, credenciais Stripe, segredos de webhooks, senhas e URIs RabbitMQ. O PEM público precisa corresponder à chave privada do emissor. Usar segredos separados entre ambientes sempre que possível; preservar quebras de linha dos PEM nas variáveis do Render. Não divulgar saídas de listagem de segredos.

## 7. Configurações por ambiente

| Ambiente | Fonte recomendada |
|---|---|
| Desenvolvimento | `appsettings.Development.json` e User Secrets |
| Testes automatizados | Configuração isolada |
| Sandbox remota (Production) | Variáveis de ambiente do Render e configurações gerais de `appsettings.json` |
| Recurso | Development | Production (Sandbox) |
|---|---|---|
| RabbitMQ | HostName, Port, UserName e Password | `RabbitMQ__Uri` (AMQPS, TLS e vhost) |
| SQLite | Arquivo local | `ConnectionStrings__DefaultConnection`, caminho absoluto em `/data` |
| Stripe | Chave de teste e segredo do CLI | Chave de teste e segredo do endpoint remoto |
| Integrações HTTP | localhost | URLs públicas HTTPS dos serviços |
| E-mail | FakeEmailSender | FakeEmailSender atualmente; Brevo planejado |

Em Production não configurar `RabbitMQ__Port` com valor vazio. A URI contém credenciais e deve permanecer secreta. Configurar explicitamente a conexão SQLite: configurações exclusivas de Development não são carregadas em Production. `/data` não possui persistência garantida no Render gratuito.

### 7.1. Endereços de referência

| Serviço | Local | Remoto |
|---|---|---|
| Usuários | `http://localhost:5296/` | `https://microsservicos-eventos-presenciais.onrender.com/` |
| Eventos | `http://localhost:5297/` | `https://microsservicos-eventos-presenciais-hctr.onrender.com/` |
| Comercialização | `http://localhost:5298/` | `https://comercializacao.onrender.com/` |
| Portaria | `http://localhost:5299/` | `https://portaria-gdve.onrender.com/` |

Valores JWT de referência: `Jwt:Issuer = Usuario`, `Jwt:Audience = EventosPresenciais`, duração de 60 minutos no emissor conforme configuração local documentada. A tabela completa de variáveis remotas está em [Implantação](Implantacao.md).

### 7.2. Configurações propostas — não implementadas

`Brevo:ApiKey` (sensível), `Brevo:SenderEmail` e `Brevo:SenderName` são propriedades propostas para o envio transacional de e-mails. Não são pré-requisitos da versão atual.

## 8. Documentação complementar

- [Usuários](ConfiguracaoLocal/Usuario.md)
- [Eventos](ConfiguracaoLocal/Eventos.md)
- [Comercialização](ConfiguracaoLocal/Comercializacao.md)
- [Portaria](ConfiguracaoLocal/Portaria.md)
- [Implantação](Implantacao.md)

Atualizar este inventário sempre que uma propriedade for adicionada, removida ou alterada.
