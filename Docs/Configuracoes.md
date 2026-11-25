# Referência técnica de configurações

Este documento descreve as configurações utilizadas pelos quatro microsserviços do sistema de gerenciamento de eventos presenciais.

Seu propósito é identificar as propriedades de configuração, suas responsabilidades e seus requisitos de segurança.

Para instruções de instalação e execução em ambiente de desenvolvimento, consulte os documentos do diretório `Docs/ConfiguracaoLocal/`.

## 1. Convenções

As configurações utilizam a estrutura de configuração do ASP.NET Core, sendo acessadas por meio de `IConfiguration`.

Exemplo:

```csharp
var issuer = configuration["Jwt:Issuer"];
```

As configurações podem ser fornecidas por arquivos `appsettings.json`, variáveis de ambiente e, durante o desenvolvimento, pelo .NET User Secrets.

Em variáveis de ambiente, o separador `:` é representado por `__`.

Exemplo: `Jwt:PublicKeyPem` corresponde a `Jwt__PublicKeyPem`.

**Classificação:**

- **Obrigatória:** necessária para o comportamento correspondente do microsserviço.
- **Sensível:** deve ser protegida e não deve ser publicada no repositório.
- **Não sensível:** pode ser documentada e armazenada em arquivos de configuração, desde que não contenha informações privadas.

## 2. Microsserviço Usuários

Responsável pelo gerenciamento de usuários, autenticação e emissão dos tokens JWT.

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Identificador do emissor dos JWTs | Não |
| `Jwt:Audience` | Identificador dos destinatários previstos dos JWTs | Não |
| `Jwt:AccessTokenMinutes` | Tempo de validade dos tokens de acesso emitidos | Não |
| `Jwt:PublicKeyPem` | Chave RSA pública para validação de assinaturas JWT | Não |
| `Jwt:PrivateKeyPem` | Chave RSA privada utilizada na assinatura dos tokens | **Sim** |
| `ConnectionStrings:DefaultConnection` | Conexão com o banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens permitidas para chamadas de navegadores | Não |

**Observação:** os nomes `Jwt:PrivateKeyPem` e `ConnectionStrings:DefaultConnection` devem ser conferidos no código do microsserviço Usuários. Esta seção descreve as configurações esperadas pela arquitetura, mas não constitui um inventário definitivo daquele projeto.

A chave privada RSA deve permanecer restrita ao serviço emissor.

## 3. Microsserviço Eventos

Responsável pelo cadastro, gerenciamento e publicação de eventos.

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor do JWT | Não |
| `Jwt:Audience` | Validação da audiência do JWT | Não |
| `Jwt:PublicKeyPem` | Chave RSA pública para validação de assinaturas JWT | Não |
| `Authentication:ApiKey` | Autenticação das requisições internas | **Sim** |
| `Services:Comercializacao:BaseUrl` | Endereço do microsserviço Comercialização | Não |
| `ConnectionStrings:DefaultConnection` | Conexão com o banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens permitidas para chamadas de navegadores | Não |
| `RabbitMQ:HostName` | Endereço do broker RabbitMQ | Não |
| `RabbitMQ:Port` | Porta de comunicação AMQP | Não |
| `RabbitMQ:UserName` | Usuário de autenticação no RabbitMQ | **Sim** |
| `RabbitMQ:Password` | Senha de autenticação no RabbitMQ | **Sim** |

**Valores de referência:**

- `Jwt:Audience`: `EventosPresenciais`
- `Services:ComercializacaoUrl`: `http://localhost:5298/` em desenvolvimento.

O valor de `Jwt:Issuer` deve ser exatamente igual ao configurado em Usuários.

## 4. Microsserviço Comercialização

Responsável pela administração de ofertas, criação de pedidos, processamento de pagamentos e emissão de ingressos.

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor do JWT | Não |
| `Jwt:Audience` | Validação da audiência do JWT | Não |
| `Jwt:PublicKeyPem` | Validação da assinatura JWT | Não |
| `Authentication:ApiKey` | Autenticação entre microsserviços | **Sim** |
| `Stripe:SecretKey` | Autenticação das chamadas à API da Stripe | **Sim** |
| `Stripe:WebhookSecret` | Verificação da assinatura dos eventos enviados pela Stripe | **Sim** |
| `Stripe:SuccessUrl` | URL de retorno após a conclusão do Checkout | Não |
| `Stripe:CancelUrl` | URL de retorno quando o comprador abandona o Checkout | Não |
| `Services:Eventos:BaseUrl` | Endereço da API de Eventos | Não |
| `Services:Portaria:BaseUrl` | Endereço interno da API de Portaria | Não |
| `Services:Portaria:PublicBaseUrl` | Endereço utilizado para compor URLs públicas presentes nos ingressos | Não |
| `ConnectionStrings:DefaultConnection` | Conexão com o banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens permitidas para chamadas de navegadores | Não |
| `RabbitMQ:HostName` | Endereço do broker RabbitMQ | Não |
| `RabbitMQ:Port` | Porta de comunicação AMQP | Não |
| `RabbitMQ:UserName` | Usuário de autenticação no RabbitMQ | **Sim** |
| `RabbitMQ:Password` | Senha de autenticação no RabbitMQ | **Sim** |

**Observações:**

A chave `Stripe:SecretKey` deve pertencer ao mesmo ambiente Stripe utilizado nas transações. O segredo `Stripe:WebhookSecret` deve corresponder ao destino ou listener que entrega os webhooks.

`Stripe:CancelUrl` não implementa cancelamento financeiro nem estorno. Trata-se apenas do destino de navegação ao sair do Checkout.

A confirmação de pagamento deve ser realizada por um mecanismo confiável de verificação, como o webhook com assinatura validada, e não pelo redirecionamento para `Stripe:SuccessUrl`.

## 5. Microsserviço Portaria

Responsável pela criação de credenciais de acesso e validação de ingressos na entrada dos eventos.

| Configuração | Finalidade | Sensível |
|---|---|---|
| `Jwt:Issuer` | Validação do emissor do JWT | Não |
| `Jwt:Audience` | Validação da audiência do JWT | Não |
| `Jwt:PublicKeyPem` | Validação da assinatura JWT | Não |
| `Authentication:ApiKey` | Autenticação das requisições internas | **Sim** |
| `Services:Comercializacao:BaseUrl` | Endereço da API de Comercialização | Não |
| `ConnectionStrings:DefaultConnection` | Conexão com o banco SQLite | Depende do conteúdo |
| `Cors:AllowedOrigins` | Origens permitidas para chamadas de navegadores | Não |

**Observação:** `Jwt:AccessTokenMinutes` não é exigida pela Portaria, pois ela não emite JWTs. O tempo de expiração é informado pelo próprio token e verificado durante sua validação.

## 6. Segurança e gerenciamento de segredos

As seguintes informações não devem ser publicadas no repositório:

- Chaves privadas RSA.
- Chaves de API internas.
- Chaves secretas da Stripe.
- Segredos de assinatura de webhooks.

A chave pública RSA pode ser compartilhada com os microsserviços validadores, mas deve corresponder à chave privada do serviço Usuários.

Em desenvolvimento, recomenda-se utilizar .NET User Secrets. Em produção, devem ser utilizadas variáveis de ambiente ou um mecanismo apropriado de gerenciamento de segredos.

## 7. Configurações por ambiente

| Ambiente | Fonte recomendada |
|---|---|
| Desenvolvimento | `appsettings.Development.json` e User Secrets |
| Testes automatizados | Configuração isolada e valores específicos de teste. |
| Produção | Variáveis de ambiente e gerenciamento seguro de segredos |

Os endereços `localhost` apresentados nesta documentação são referências de desenvolvimento e devem ser substituídos pelos endereços correspondentes na implantação.

## 8. Documentação complementar

Os procedimentos de preparação, execução e verificação local de cada serviço encontram-se em:

- `Docs/ConfiguracaoLocal/Usuario.md`
- `Docs/ConfiguracaoLocal/Eventos.md`
- `Docs/ConfiguracaoLocal/Comercializacao.md`
- `Docs/ConfiguracaoLocal/Portaria.md`

Este documento deve ser atualizado sempre que uma configuração obrigatória for adicionada, alterada ou removida do código.