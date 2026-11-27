# Configuração local — Comercialização

Este documento apresenta os procedimentos necessários para configurar e executar o microsserviço **Comercialização** em ambiente de desenvolvimento.

O microsserviço é responsável pelo gerenciamento de ofertas de ingressos, pedidos, pagamentos e emissão de ingressos digitais.

A integração de pagamentos utiliza a API da Stripe, com Stripe Checkout e webhooks para processamento das confirmações.

Para informações complementares, consulte:

- [Pré-requisitos](../PreRequisitos.md) — ferramentas, dependências e geração das chaves RSA.

- [Referência técnica de configurações](../Configuracoes.md) — propriedades exigidas pela aplicação.

Para a Sandbox remota, consulte [Implantação](../Implantacao.md). As configurações abaixo são exclusivamente locais; no Render utiliza-se `RabbitMQ__Uri` e o segredo do webhook remoto.

## Sumário

1. [Acessar o diretório do projeto](#1-acessar-o-diretório-do-projeto)

2. [Configurar os User Secrets](#2-configurar-os-user-secrets)

   - [Configurar a API Key](#21-configurar-a-api-key)

   - [Configurar a chave pública RSA](#22-configurar-a-chave-pública-rsa)

   - [Configurar a chave secreta da Stripe](#23-configurar-a-chave-secreta-da-stripe)

   - [Configurar o segredo do webhook](#24-configurar-o-segredo-do-webhook)
   - [Configurar o RabbitMQ](#25-configurar-o-rabbitmq)

3. [Verificar as configurações da aplicação](#3-verificar-as-configurações-da-aplicação)

4. [Executar o microsserviço](#4-executar-o-microsserviço)

5. [Stripe CLI — Configuração e comandos](#5-stripe-cli--configuração-e-comandos)

   - [Verificar a instalação](#51-verificar-a-instalação)

   - [Autenticar o Stripe CLI](#52-autenticar-o-stripe-cli)

   - [Verificar a conta conectada](#53-verificar-a-conta-conectada)

   - [Iniciar o listener de webhooks](#54-iniciar-o-listener-de-webhooks)

   - [Escutar apenas os eventos utilizados pelo projeto](#55-escutar-apenas-os-eventos-utilizados-pelo-projeto)

   - [Simular um evento](#56-simular-um-evento)

6. [Consultar sessões e eventos da Stripe](#6-consultar-sessões-e-eventos-da-stripe)

7. [Testar um pagamento completo](#7-testar-um-pagamento-completo)

8. [Solução de problemas](#8-solução-de-problemas)

9. [Configuração mínima esperada](#9-configuração-mínima-esperada)

10. [Referências](#10-referências)

## 1. Acessar o diretório do projeto

A partir da raiz do repositório:

```bash
cd Comercializacao/Api
```

Os comandos .NET apresentados nas próximas seções devem ser executados nesse diretório.

## 2. Configurar os User Secrets

O projeto já possui um `UserSecretsId` definido em `Api.csproj`. Portanto, não é necessário executar `dotnet user-secrets init` novamente.

### 2.1. Configurar a API Key

A API Key é utilizada para autenticar as chamadas internas entre Comercialização e os demais microsserviços.

```bash
dotnet user-secrets set "Authentication:ApiKey" "SUA_CHAVE_INTERNA"
```

Utilize a chave compartilhada definida para a comunicação entre os serviços.

Não utilize valores triviais nem publique essa credencial no repositório.

### 2.2. Configurar a chave pública RSA

Comercialização utiliza a chave pública RSA para validar os tokens JWT emitidos pelo microsserviço Usuários.

Considerando que `public.pem` já tenha sido gerado conforme os pré-requisitos:

```bash
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat /caminho/para/public.pem)"
```

Substitua o caminho pela localização real do arquivo.

**Importante:** Comercialização não utiliza a chave privada RSA para emitir JWTs.

### 2.3. Configurar a chave secreta da Stripe

Para criar pagamentos, é necessário obter uma chave de API da Stripe.

Acesse o [Stripe Dashboard — API Keys](https://dashboard.stripe.com/test/apikeys), utilizando o ambiente de teste ou sandbox correspondente.

Localize a chave secreta, normalmente identificada pelo prefixo `sk_test_`.

Configure-a no projeto:

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_SUA_CHAVE"
```

Essa chave permite que o backend realize chamadas autenticadas à API da Stripe, incluindo a criação de sessões Checkout.

**Não utilize chaves de produção (`sk_live_`) nos testes locais.**

### 2.4. Configurar o segredo do webhook

Além da chave de API, Comercialização precisa verificar a autenticidade dos eventos recebidos da Stripe.

Essa verificação utiliza a propriedade:

`Stripe:WebhookSecret`

O valor é identificado pelo prefixo `whsec_` e será obtido por meio do Stripe CLI, conforme a seção 5.

Exemplo de configuração:

```bash
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_SEU_SEGREDO"
```

**Atenção:** a chave `sk_test_` e o segredo `whsec_` possuem responsabilidades diferentes e não são intercambiáveis.

### 2.5. Configurar o RabbitMQ

O microsserviço Comercialização utiliza RabbitMQ para consumir mensagens de integração publicadas pelo microsserviço Eventos.

Ao receber uma mensagem `EventoIniciadoEvent`, Comercialização encerra automaticamente todas as ofertas de ingressos ativas associadas ao evento.

As configurações de conexão podem ser definidas no arquivo `appsettings.Development.json`:

```json
"RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672
}
```

Configure as credenciais utilizando o .NET User Secrets:

```bash
dotnet user-secrets set "RabbitMQ:UserName" "SEU_USUARIO"
dotnet user-secrets set "RabbitMQ:Password" "SUA_SENHA"
```

O microsserviço deve utilizar o mesmo broker configurado em Eventos.

#### Topologia de mensageria

| Componente | Valor |
|---|---|
| Exchange | `eventos` |
| Tipo da exchange | `topic` |
| Routing Key | `evento.iniciado` |
| Queue | `comercializacao.eventos.iniciados` |
| Papel do microsserviço | Consumer |

A fila e seu vínculo com a exchange são declarados automaticamente pelo consumidor durante a inicialização.

As mensagens são confirmadas utilizando ACK somente após a conclusão bem-sucedida do encerramento das ofertas.

A operação de encerramento é idempotente: mensagens repetidas não modificam ofertas que já estejam encerradas ou esgotadas.

**Importante:** o RabbitMQ deve estar em execução antes de iniciar Comercialização.

## 3. Verificar as configurações da aplicação

As seguintes configurações gerais já estão definidas em `appsettings.json`:

| Configuração | Valor |
|---|---|
| `Jwt:Issuer` | `Usuario` |
| `Jwt:Audience` | `EventosPresenciais` |

O arquivo `appsettings.Development.json` contém as configurações locais, incluindo:

| Configuração | Valor de desenvolvimento |
|---|---|
| `ConnectionStrings:DefaultConnection` | `Data Source=Comercializacao.db` |
| `Services:Eventos:BaseUrl` | `http://localhost:5297/` |
| `Services:Portaria:BaseUrl` | `http://localhost:5299/` |
| `Services:Portaria:PublicBaseUrl` | `http://localhost:5299/` |
| `Stripe:SuccessUrl` | URL de retorno após concluir o Checkout |
| `Stripe:CancelUrl` | URL de retorno ao sair do Checkout |
| `Cors:AllowedOrigins` | Origens autorizadas no desenvolvimento |
| `Services:Usuarios:BaseUrl` | `http://localhost:5296/` |
| `RabbitMQ:HostName` | `localhost` |
| `RabbitMQ:Port` | `5672` |

A configuração `Services:Usuarios:BaseUrl` é utilizada para consultar compradores pelo endereço de e-mail durante a criação de pedidos. O comprador deve estar previamente cadastrado em Usuários, mas não precisa possuir uma conta de acesso.

Comercialização não é responsável pelo cadastro de usuários.

As URLs de sucesso e cancelamento do Stripe Checkout são configurações de navegação.

A `CancelUrl` não representa o cancelamento financeiro de um pedido nem o estorno de um pagamento.

Caso o frontend ainda não esteja implementado, as URLs de retorno podem apontar para páginas inexistentes durante os testes. Isso não impede o envio dos webhooks para Comercialização.

O endereço `Services:Portaria:PublicBaseUrl` deve ser acessível pelos dispositivos que utilizarão os links dos ingressos digitais. `localhost` serve apenas para testes realizados no próprio computador.

### 3.1. Verificar os User Secrets

Execute:

```bash
dotnet user-secrets list
```

As propriedades esperadas são:

| Configuração | Finalidade |
|---|---|
| `Authentication:ApiKey` | Comunicação interna entre microsserviços |
| `Jwt:PublicKeyPem` | Validação de JWTs |
| `Stripe:SecretKey` | Chamadas autenticadas à API Stripe |
| `Stripe:WebhookSecret` | Validação da assinatura dos webhooks |
| `RabbitMQ:UserName` | Usuário de autenticação no RabbitMQ |
| `RabbitMQ:Password` | Senha de autenticação no RabbitMQ |

O comando exibe os valores armazenados. Não compartilhe sua saída contendo credenciais.

## 4. Executar o microsserviço

Restaure as dependências:

```bash
dotnet restore
```

Execute a aplicação:

```bash
dotnet run
```

O endereço HTTP de desenvolvimento é:

`http://localhost:5298`

A interface de documentação Scalar pode ser acessada em:

`http://localhost:5298/scalar/v1`

A aplicação utiliza as configurações definidas em `Properties/launchSettings.json`.

O banco SQLite é gerenciado pelo Entity Framework Core, com migrations aplicadas durante a inicialização.

Para testar a criação de ofertas e pedidos, Eventos deve estar em execução, pois Comercialização consulta o estado temporal do evento.

Para criar pedidos, Usuários também deve estar em execução, permitindo a identificação do comprador pelo e-mail informado.

Portaria deve estar disponível durante a emissão das credenciais de acesso dos ingressos.

O RabbitMQ deve estar em execução antes da inicialização de Comercialização.

**---**

## 5. Stripe CLI — Configuração e comandos

O Stripe CLI permite interagir com a API da Stripe diretamente pelo terminal, inspecionar recursos e encaminhar notificações de webhook para uma aplicação local.

Ele é utilizado neste projeto para testar pagamentos sem precisar publicar Comercialização em um servidor acessível pela internet.

### 5.1. Verificar a instalação

```bash
stripe --version
```

Exibe a versão instalada do Stripe CLI.

Caso não esteja instalado, consulte a [documentação oficial de instalação](https://docs.stripe.com/cli/install).

### 5.2. Autenticar o Stripe CLI

```bash
stripe login
```

Inicia a autenticação do CLI com a conta Stripe.

O comando fornece um procedimento de autorização que deve ser concluído no navegador.

A autenticação do CLI é independente das configurações do projeto .NET.

Mesmo autenticando o CLI, Comercialização ainda precisa de `Stripe:SecretKey` para realizar suas próprias chamadas.

### 5.3. Verificar a conta conectada

```bash
stripe whoami
```

Exibe informações sobre a conta ou o contexto de autenticação utilizado pelo Stripe CLI.

Esse comando é útil para confirmar que as operações estão sendo realizadas na conta esperada.

### 5.4. Iniciar o listener de webhooks

Com Comercialização em execução na porta `5298`, abra outro terminal e execute:

```bash
stripe listen \\
  --all-snapshot \\
  --forward-to http://localhost:5298/api/webhooks/pagamentos
```

Esse comando inicia um listener que recebe eventos da Stripe e os encaminha ao endpoint HTTP local.

**Rota utilizada:**

```http
POST /api/webhooks/pagamentos
```

O terminal exibirá um segredo de assinatura semelhante a:

```text
Your webhook signing secret is whsec_...
```

Copie esse valor e configure-o nos User Secrets:

```bash
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_SEU_SEGREDO"
```

Reinicie Comercialização após atualizar o segredo.

**Importante:** o segredo apresentado pelo Stripe CLI deve corresponder ao listener utilizado. Ele não deve ser confundido com o segredo de um endpoint de produção cadastrado no Stripe Dashboard.

### 5.5. Escutar apenas os eventos utilizados pelo projeto

O webhook de Comercialização processa os seguintes eventos:

| Evento | Significado |
|---|---|
| `checkout.session.completed` | Sessão Checkout concluída |
| `checkout.session.async_payment_succeeded` | Pagamento assíncrono confirmado |
| `checkout.session.async_payment_failed` | Pagamento assíncrono não concluído com sucesso |

Para escutar somente esses eventos:

```bash
stripe listen \\
  --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed \\
  --forward-to http://localhost:5298/api/webhooks/pagamentos
```

Essa opção reduz a quantidade de eventos exibidos no terminal durante os testes.

**Observação:** uma sessão concluída não significa necessariamente pagamento aprovado. O processamento deve considerar também `payment_status` e as regras definidas para o meio de pagamento utilizado.

### 5.6. Simular um evento

```bash
stripe trigger checkout.session.completed
```

O comando cria objetos de teste e dispara um evento simulado de conclusão de Checkout.

É útil para verificar se o webhook está recebendo requisições e validando assinaturas.

Entretanto, o evento simulado normalmente não corresponde a um pagamento previamente registrado no banco SQLite de Comercialização.

Por isso, a aplicação pode rejeitar ou não encontrar a sessão associada.

**Esse comando não substitui um teste completo de compra.**

**---**

## 6. Consultar sessões e eventos da Stripe

Os comandos seguintes são úteis para investigar pagamentos, sessões Checkout e falhas de webhook.

### 6.1. Listar sessões Checkout

```bash
stripe checkout sessions list --limit 5
```

Lista sessões Checkout recentes.

Os identificadores normalmente começam com `cs_test_` no ambiente de teste.

Esse comando é útil para localizar uma sessão criada pelo microsserviço Comercialização.

### 6.2. Consultar uma sessão específica

```bash
stripe checkout sessions retrieve cs_test_ID_DA_SESSAO
```

Recupera os dados de uma sessão Checkout.

Verifique principalmente:

- `id`: identificador da sessão.

- `status`: estado da sessão.

- `payment_status`: situação do pagamento.

Para um pagamento confirmado por cartão, normalmente esperamos:

```json
{
  "status": "complete",
  "payment_status": "paid"
}
```

A consulta é especialmente útil quando o Checkout foi concluído, mas o pedido permanece pendente na aplicação.

### 6.3. Listar eventos

```bash
stripe events list --limit 20
```

Lista os eventos recentes registrados na Stripe.

Cada evento possui um identificador iniciado por `evt_`.

### 6.4. Consultar um evento específico

```bash
stripe events retrieve evt_ID_DO_EVENTO
```

Recupera os detalhes de um evento.

Para relacionar um evento `checkout.session.completed` a um pedido, compare o identificador da sessão presente em `data.object.id` com o identificador externo registrado no pagamento em Comercialização.

### 6.5. Consultar opções de reenvio

```bash
stripe events resend --help
```

Exibe os parâmetros disponíveis para reenviar eventos a destinos registrados na Stripe.

Um exemplo para um endpoint de webhook previamente cadastrado seria:

```bash
stripe events resend evt_ID_DO_EVENTO \\
  --webhook-endpoint we_ID_DO_ENDPOINT
```

Esse procedimento exige um identificador real de endpoint (`we_...`).

**Atenção:** o comando não representa um reenvio direto para o `stripe listen` local. Não é possível simplesmente inventar um identificador de endpoint.

Quando um pagamento foi concluído, mas seu webhook local falhou, não crie outro pagamento automaticamente. Primeiro consulte a sessão original e determine como reprocessar sua confirmação com segurança.

**---**

## 7. Testar um pagamento completo

Para validar a integração, execute o seguinte fluxo:

1. Inicie o RabbitMQ.

2. Inicie Usuários, Portaria, Comercialização e Eventos, respectivamente.

3. Inicie Portaria, caso deseje validar também a emissão de credenciais.

4. Execute o listener do Stripe CLI.

5. Crie um evento e uma oferta de ingresso ativa.

6. Publique o evento.

7. Cadastre um comprador em Usuários, caso ainda não exista, e crie um pedido em Comercialização informando o e-mail do comprador, a oferta de ingresso e a quantidade desejada.

8. Consulte o pedido e obtenha a URL de Checkout.

9. Abra a URL de Checkout no navegador.

10. Realize um pagamento utilizando os dados de teste fornecidos pela Stripe.

11. Observe o webhook e verifique o processamento do pedido.

Exemplo de corpo da requisição para criação de um pedido:

```json
{
    "ofertaId": "UUID_DA_OFERTA",
    "emailComprador": "comprador@example.com",
    "quantidade": 2
}
```

O comprador não precisa possuir uma conta de acesso, mas deve estar previamente cadastrado em Usuários. Caso o e-mail não corresponda a um cadastro existente, a solicitação retornará HTTP 404.

### 7.1. Cartão de teste

Para simular um pagamento aprovado, utilize:

| Campo | Valor |
|---|---|
| Número | `4242 4242 4242 4242` |
| Validade | Uma data futura válida |
| CVC | Qualquer número de três dígitos |

Os dados são destinados exclusivamente ao ambiente de teste da Stripe.

Não utilize cartões reais durante esses testes.

### 7.2. Verificar o webhook

No terminal do Stripe CLI, procure uma entrega semelhante a:

```text
checkout.session.completed
POST http://localhost:5298/api/webhooks/pagamentos
```

O resultado esperado é uma resposta HTTP de sucesso, como `202 Accepted`, desde que a aplicação tenha processado o evento.

### 7.3. Verificar o pedido

Consulte o pedido utilizando:

```http
GET /api/pedidos/{pedidoId}
```

Verifique se o pagamento foi registrado e se os ingressos foram emitidos.

Quando a integração com Portaria estiver funcionando, o sistema também deverá registrar as credenciais de acesso correspondentes.

O redirecionamento para a página de sucesso do Checkout não comprova, sozinho, que o pagamento foi confirmado pelo backend.

**---**

## 8. Solução de problemas

| Problema | Possível causa |
|---|---|
| `Stripe:SecretKey não configurada` | Chave de API ausente dos User Secrets |
| `Stripe:WebhookSecret` ausente | Segredo do listener não configurado |
| `Stripe:SuccessUrl não configurada` | URL de retorno não definida |
| HTTP 400 no webhook | Assinatura inválida, erro de interpretação ou incompatibilidade de versão da API |
| HTTP 404 em uma integração | Rota incorreta ou recurso inexistente |
| HTTP 503 no webhook | Falha ao localizar pagamento ou configuração ausente, conforme o controller atual |
| Pedido permanece pendente | Pagamento não confirmado ou webhook não processado |
| Ingresso emitido, mas pedido em processamento | Falha no fluxo de confirmação das credenciais |
| Falha ao criar credencial | Verificar URL, API Key e disponibilidade de Portaria |
| Erro após redirecionamento para `localhost:3000` | Frontend de retorno não implementado ou não iniciado |
| Comprador não encontrado (HTTP 404) | E-mail informado não está cadastrado em Usuários |
| Falha de comunicação com Usuários | Verificar `Services:Usuarios:BaseUrl`, API Key e disponibilidade do microsserviço |
| Pedido ou oferta recusada | Verificar se o evento está com status `AGENDADO` |
| Ofertas não encerradas automaticamente | Verificar a disponibilidade do RabbitMQ e a execução do consumidor |

### 8.1. Incompatibilidade de versões Stripe

Durante o desenvolvimento, foi identificado um problema de incompatibilidade entre a versão de API utilizada em um evento e a versão esperada pelo SDK Stripe.net.

O erro pode apresentar uma mensagem semelhante a:

```text
Received event with API version ...,
but Stripe.net expects API version ...
```

Isso não significa necessariamente que a assinatura esteja incorreta.

O ideal é utilizar eventos com uma versão de API compatível com o SDK instalado.

Durante o desenvolvimento, também é possível configurar `EventUtility.ConstructEvent` com `throwOnApiVersionMismatch: false`, mas essa alternativa exige cuidado: objetos produzidos em versões diferentes podem não ser desserializados corretamente.

Essa opção não desativa a validação criptográfica da assinatura.

### 8.2. Falhas após a confirmação do pagamento

O processamento do pagamento e a emissão de ingressos envolvem múltiplos componentes.

Por exemplo, um pagamento pode ser confirmado e persistido em Comercialização enquanto a comunicação com Portaria falha.

Não considere esses problemas resolvidos simplesmente repetindo operações indiscriminadamente.

O processamento deve ser idempotente e permitir recuperação de falhas sem duplicar pagamentos, ingressos ou credenciais.

**---**

## 9. Configuração mínima esperada

| Configuração | Origem |
|---|---|
| `Jwt:Issuer` | `appsettings.json` |
| `Jwt:Audience` | `appsettings.json` |
| `Jwt:PublicKeyPem` | User Secrets |
| `Authentication:ApiKey` | User Secrets |
| `Stripe:SecretKey` | User Secrets |
| `Stripe:WebhookSecret` | User Secrets |
| `Stripe:SuccessUrl` | `appsettings.Development.json` |
| `Stripe:CancelUrl` | `appsettings.Development.json` |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` |
| `Services:Eventos:BaseUrl` | `appsettings.Development.json` |
| `Services:Portaria:BaseUrl` | `appsettings.Development.json` |
| `Services:Portaria:PublicBaseUrl` | `appsettings.Development.json` |
| `Cors:AllowedOrigins` | `appsettings.Development.json` |
| `Services:Usuarios:BaseUrl` | `appsettings.Development.json` |
| `RabbitMQ:HostName` | `appsettings.Development.json` |
| `RabbitMQ:Port` | `appsettings.Development.json` |
| `RabbitMQ:UserName` | User Secrets |
| `RabbitMQ:Password` | User Secrets |

Com essas configurações, o microsserviço Comercialização estará preparado para execução e testes de pagamentos em ambiente de desenvolvimento.

**---**

## 10. Referências

- [Stripe CLI — documentação oficial](https://docs.stripe.com/stripe-cli)

- [Stripe CLI — instalação](https://docs.stripe.com/cli/install)

- [Stripe — webhooks](https://docs.stripe.com/webhooks)

- [Stripe — cartões e pagamentos de teste](https://docs.stripe.com/testing)

- [Stripe — Checkout](https://docs.stripe.com/payments/checkout)

- [Stripe Dashboard — API Keys](https://dashboard.stripe.com/test/apikeys)

Para informações sobre as demais configurações do projeto, consulte [Configuracoes.md](../Configuracoes.md).
