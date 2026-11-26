# Implantação — Sandbox pública

Os quatro serviços foram implantados; `Production` designa o ambiente ASP.NET Core, não uma operação comercial real.

## 1. Infraestrutura e acesso

Render gratuito hospeda as APIs; CloudAMQP Little Lemur hospeda RabbitMQ; Stripe Sandbox processa pagamentos simulados. GitHub mantém código e releases; Scalar/OpenAPI permite explorar os endpoints. Cada serviço possui SQLite próprio.

| Microsserviço | API pública | Acesso informado na release |
|---|---|---|
| Usuários | [Usuários](https://microsservicos-eventos-presenciais.onrender.com) | [Abrir](https://microsservicos-eventos-presenciais.onrender.com/scalar/) |
| Eventos | [Eventos](https://microsservicos-eventos-presenciais-hctr.onrender.com) | [Abrir](https://microsservicos-eventos-presenciais-hctr.onrender.com/scalar) |
| Comercialização | [Comercialização](https://comercializacao.onrender.com) | [Abrir](https://comercializacao.onrender.com) |
| Portaria | [Portaria](https://portaria-gdve.onrender.com) | [Abrir](https://portaria-gdve.onrender.com) |

As URLs base de Comercialização e Portaria foram fornecidas na release; confirmar o caminho do Scalar nesses serviços. Não foram realizados testes de disponibilidade nesta revisão.

```mermaid
flowchart LR
    subgraph Render
        U[Usuários] --- DU[(SQLite)]
        E[Eventos] --- DE[(SQLite)]
        C[Comercialização] --- DC[(SQLite)]
        P[Portaria] --- DP[(SQLite)]
    end
    C -->|GET comprador| U
    C -->|GET estado temporal| E
    E -->|GET ofertas| C
    C -->|Criar credenciais| P
    P -->|GET ingresso| C
    E -->|AMQPS| R[CloudAMQP]
    R -->|evento.iniciado| C
    C -->|Checkout HTTPS| S[Stripe Sandbox]
    S -->|Webhook HTTPS| C
```

## 2. Docker e banco

Imagens multi-stage `mcr.microsoft.com/dotnet/sdk:10.0` e `mcr.microsoft.com/dotnet/aspnet:10.0`, compilação Release, porta HTTP 8080, content root `/app`, usuário não root `app`, diretório `/data` gravável por esse usuário. O Dockerfile de Usuários seleciona explicitamente `Api/Api.csproj`, sem `find`; padronizar os demais é evolução, não alteração comprovada.

No Render, utilizar o Dockerfile e contexto de build correspondentes ao serviço. Configurar `ASPNETCORE_ENVIRONMENT=Production` e garantir escuta em `0.0.0.0:8080` conforme Dockerfile/configuração do host. As migrations executam via `Database.Migrate()` na inicialização.

| Serviço | Exemplo de `ConnectionStrings__DefaultConnection` |
|---|---|
| Usuários | `Data Source=/data/Nome_banco.db` |
| Eventos | `Data Source=/data/Eventos.db` |
| Comercialização | `Data Source=/data/Comercializacao.db` |
| Portaria | `Data Source=/data/Portaria.db` |

Os nomes são exemplos, exceto o de Usuários informado no incidente; cada serviço usa seu arquivo. `/data` não é disco persistente no plano gratuito. A migration recria estrutura, não recupera dados.

## 3. Variáveis de ambiente

Configurar no painel Environment do Render, sem versionar valores sensíveis. `:` torna-se `__`. Os valores comuns podem vir do `appsettings.json`, mas não depender do arquivo de Development.

| Variável | Serviços | Valor / finalidade |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Todos | `Production` |
| `ConnectionStrings__DefaultConnection` | Todos | Arquivo SQLite próprio em `/data` |
| `Authentication__ApiKey` | Todos | Segredo compartilhado conforme configuração |
| `Jwt__Issuer` | Todos | `Usuario` |
| `Jwt__Audience` | Todos | `EventosPresenciais` |
| `Jwt__PublicKeyPem` | Validadores; Usuários se exigido | PEM público correspondente |
| `Jwt__PrivateKeyPem` | Usuários somente | PEM privado |
| `Jwt__AccessTokenMinutes` | Usuários | `60`, conforme configuração |
| `Cors__AllowedOrigins__0` | Conforme necessidade | Origem autorizada; adicionar índices para outras origens |
| `Services__Comercializacao__BaseUrl` | Eventos, Portaria | https://comercializacao.onrender.com/ |
| `Services__Eventos__BaseUrl` | Comercialização | https://microsservicos-eventos-presenciais-hctr.onrender.com/ |
| `Services__Usuarios__BaseUrl` | Comercialização | https://microsservicos-eventos-presenciais.onrender.com/ |
| `Services__Portaria__BaseUrl` | Comercialização | https://portaria-gdve.onrender.com/ |
| `Services__Portaria__PublicBaseUrl` | Comercialização | https://portaria-gdve.onrender.com/ |
| `Eventos__PublicBaseUrl` | Eventos | URL pública de Eventos, se mantida a configuração do mapper |
| `RabbitMQ__Uri` | Eventos, Comercialização | URI AMQPS secreta do CloudAMQP |
| `Stripe__SecretKey` | Comercialização | Chave de teste da Sandbox |
| `Stripe__WebhookSecret` | Comercialização | Segredo do destino remoto |
| `Stripe__SuccessUrl` / `Stripe__CancelUrl` | Comercialização | URLs de navegação configuradas para a demonstração |


## 4. RabbitMQ gerenciado

A hospedagem própria foi descartada porque Private Services não possuem plano gratuito na configuração avaliada. CloudAMQP Little Lemur fornece o broker. Em Development, os quatro campos `HostName`, `Port`, `UserName`, `Password` permanecem; em Production utiliza-se `RabbitMQ__Uri` com formato `amqps://USUARIO:SENHA@HOST/VHOST` (exemplo fictício). A URI inclui TLS e virtual host; não a publique.

O `ConnectionFactory` utiliza `Uri`, `AutomaticRecoveryEnabled = true` e `NetworkRecoveryInterval = TimeSpan.FromSeconds(5)`. Uma variável de porta vazia causava erro de conversão; a falta de TLS/vhost gerava `BrokerUnreachableException` e timeout. Não manter `RabbitMQ__Port` vazio como alternativa à URI.

A exchange `eventos` é topic e durável; routing key `evento.iniciado`; fila `comercializacao.eventos.iniciados`. Comercialização declara fila e binding. Eventos verifica inícios pendentes a cada minuto enquanto ativo, publica com confirmação e registra `InicioProcessadoEm`; Comercialização encerra ofertas ativas e confirma via ACK. Manter validação HTTP síncrona do estado AGENDADO na criação de ofertas e pedidos.

Recuperação automática não cobre falha de conexão inicial. Exceção não tratada em BackgroundService pode parar o host. Retry/backoff inicial, Outbox e DLQ são evoluções. Não há atomicidade entre publicação e SQLite nem garantia exactly-once.

## 5. Stripe Sandbox

No Stripe Dashboard foi criado destino de eventos com escopo Sua conta, conteúdo Snapshot e tipo Webhook endpoint. URL: `https://comercializacao.onrender.com/api/webhooks/pagamentos`. Configurar os eventos tratados pelo código: `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`. Conferir a seleção efetiva.

Reaproveitar a chave de teste da Sandbox e configurar o novo `whsec_...` do destino remoto; não usar o segredo do Stripe CLI. Não é necessário `stripe listen`, `stripe login` ou computador local em execução. O retorno do navegador não confirma pagamento. Usar dados e cartões de teste somente. A configuração do webhook foi realizada; entrega e processamento completos ainda devem ser validados.

## 6. Operação e validação

Abrir as APIs para verificar disponibilidade e cold start. Conferir logs de startup e migrations; validar JWT entre serviços e API Keys; cadastrar comprador fictício, criar evento futuro e oferta, publicar, criar pedido com e-mail cadastrado, concluir Checkout de teste, consultar ingressos e validar check-in. Verificar também encerramento automático de oferta quando o evento iniciar. Inspecionar entregas no Stripe Dashboard e filas no CloudAMQP.

A dependência bidirecional HTTP Eventos–Comercialização permanece. Projeções locais e eventos de integração podem reduzir acoplamento futuramente, considerando a consistência eventual.

## 7. Referências

[Configurações](Configuracoes.md) · [Pré-requisitos](PreRequisitos.md) · [Arquitetura](Arquitetura.md) · [Configuração local](ConfiguracaoLocal/) · [Render](https://render.com/docs) · [CloudAMQP](https://www.cloudamqp.com/) · [Stripe](https://docs.stripe.com/webhooks)
