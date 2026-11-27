# Arquitetura do sistema

Este documento descreve a arquitetura do back-end de gerenciamento de eventos presenciais e comercialização de ingressos digitais. Apresenta os limites dos quatro microsserviços, sua organização interna, comunicação, autenticação, persistência e integração com a Stripe.

> **Escopo e precisão:** este texto documenta a organização e os fluxos identificados no código e nas configurações discutidas. Diagramas são visões lógicas simplificadas: não representam todos os endpoints nem asseguram, por si só, que todos os fluxos estejam concluídos. Para nomes exatos de rotas e contratos HTTP, consulte os controllers e DTOs de cada serviço.

## Sumário

1. [Estilo arquitetural](#1-estilo-arquitetural)
2. [Microsserviços e responsabilidades](#2-microsserviços-e-responsabilidades)
3. [Organização interna das APIs](#3-organização-interna-das-apis)
4. [Comunicação e integrações](#4-comunicação-e-integrações)
5. [Referências entre microsserviços](#5-referências-entre-microsserviços)
6. [Persistência e propriedade dos dados](#6-persistência-e-propriedade-dos-dados)
7. [Autenticação e autorização](#7-autenticação-e-autorização)
8. [Fluxos principais](#8-fluxos-principais)
9. [Consistência e tratamento de falhas](#9-consistência-e-tratamento-de-falhas)
10. [Tecnologias e decisões](#10-tecnologias-e-decisões)
11. [Limitações e evolução](#11-limitações-e-evolução)
12. [Documentação complementar](#12-documentação-complementar)

---

## 1. Estilo arquitetural

A solução utiliza **arquitetura de microsserviços**, organizada em quatro aplicações ASP.NET Core independentes. Cada aplicação possui responsabilidades de negócio próprias, configurações próprias e um banco SQLite separado.

A comunicação entre serviços ocorre por meio de APIs REST sobre HTTP, para operações síncronas, e RabbitMQ, para a propagação assíncrona de acontecimentos relacionados ao ciclo de vida dos eventos.

O projeto foi concebido para cumprir os requisitos acadêmicos de quatro microsserviços, pelo menos duas integrações de leitura e uma integração que provoque alteração de dados em outro microsserviço.

```mermaid
flowchart LR
    Cliente["Cliente HTTP / Scalar / futura interface"]

    U["Usuários\n:5296"]
    E["Eventos\n:5297"]
    C["Comercialização\n:5298"]
    P["Portaria\n:5299"]
    S["Stripe\nAPI e webhooks"]
    R["RabbitMQ\n:5672"]

    Cliente --> U
    Cliente --> E
    Cliente --> C
    Cliente --> P

    E -->|"GET: ofertas válidas"| C
    C -->|"GET: dados e estado do evento"| E
    C -->|"GET: comprador por e-mail"| U
    C -->|"Criação de credenciais"| P
    P -->|"Consulta de ingressos"| C

    C <-->|"Checkout / webhook"| S

    E -->|"EventoIniciadoEvent"| R
    R -->|"Encerramento de ofertas"| C
```

As portas representam o ambiente local. **Usuários é o emissor de identidades e JWTs**, mas não é necessário consultá-lo a cada requisição autenticada: os outros serviços podem verificar a assinatura do token com a chave pública correspondente.

**Não se trata de um monólito distribuído por definição:** a intenção é estabelecer limites de domínio e propriedade dos dados. Isso não elimina acoplamentos operacionais, como dependência de disponibilidade nas chamadas HTTP síncronas.

## 2. Microsserviços e responsabilidades

| Microsserviço | Responsabilidade principal | Dados sob sua responsabilidade |
|---|---|---|
| **Usuários** | Cadastro, contas, verificação de cadastro e autenticação | Usuários, contas, solicitações de cadastro |
| **Eventos** | Cadastro, atualização, publicação e consulta de eventos | Eventos, períodos, slugs e informações de publicação |
| **Comercialização** | Ofertas, pedidos, pagamentos e emissão de ingressos | Ofertas, pedidos, pagamentos, tentativas e ingressos |
| **Portaria** | Credenciais de acesso e controle de utilização | Credenciais, estado e momento da utilização |

A separação por serviço evita que um módulo controle diretamente as tabelas de outro. Ações que dependem de informações remotas utilizam contratos de integração.

**Escopo da Portaria:** na versão documentada, o **organizador do evento** realiza a validação dos ingressos. Delegação a colaboradores ou dispositivos autorizados é uma possibilidade futura, não uma funcionalidade assumida como implementada.

## 3. Organização interna das APIs

### 3.1. Padrão adotado

Internamente, as aplicações apresentam uma **organização por responsabilidades** e uma **arquitetura em camadas**, com componentes típicos de uma API ASP.NET Core:

- **Controllers / camada HTTP:** recebem requisições e expõem endpoints REST.
- **Serviços de aplicação:** coordenam casos de uso, regras e integrações.
- **Models / entidades de domínio:** representam os dados e, em vários casos, encapsulam validações e mudanças de estado.
- **Infraestrutura:** oferece persistência via EF Core, autenticação, clientes HTTP e integrações externas.
- **DTOs e mapeadores:** definem representações trocadas nas APIs e sua conversão, quando presentes.

Em partes do projeto, as dependências externas são acessadas por **interfaces** (ports) e implementações concretas (adapters). Exemplos identificados:

| Abstração | Implementação | Responsabilidade |
|---|---|---|
| `IUsuarioAtual` | `UsuarioAtualHttp` | Obtém a identidade do usuário a partir do contexto HTTP |
| `IEventoClient` | `HttpEventoClient` | Consulta Eventos a partir de Comercialização |
| `IPortariaClient` | `HttpPortariaClient` | Comunica Comercialização com Portaria |
| `IGatewayPagamento` | `StripeGatewayPagamento` | Encapsula a integração com a Stripe |
| `IQRCodeService` | `QRCodeService` | Geração de QR Code |
| `IUsuarioClient` | `HttpUsuarioClient` | Consulta compradores por e-mail a partir de Comercialização |
| `IEventoPublisher` | `RabbitMqEventoPublisher` | Publica acontecimentos do ciclo de vida dos eventos no RabbitMQ |

> **Terminologia:** o emprego dessas interfaces e adaptadores **não comprova a adoção integral de arquitetura hexagonal (Ports and Adapters)**, Clean Architecture ou Onion Architecture. Essas classificações exigiriam verificar a direção de dependências entre projetos/camadas, os limites do domínio e o grau de isolamento das tecnologias. A descrição adequada aqui é **organização em camadas/responsabilidades com uso pontual do padrão Ports and Adapters**.

### 3.2. Fluxo interno típico

```mermaid
flowchart TD
    HTTP["Requisição HTTP"] --> Controller["Controller"]
    Controller --> Service["Serviço de aplicação"]
    Service --> Domain["Modelo / regras de domínio"]
    Service --> Database["DataContext / EF Core / SQLite"]
    Service --> Port["Interface para serviço externo"]
    Port --> Adapter["Adaptador HTTP / Stripe"]
    Adapter --> External["Serviço remoto"]
```

O diagrama representa **possíveis caminhos de dependência**, não uma sequência obrigatória para todos os endpoints. Em particular, cada microsserviço pode organizar suas classes em diretórios diferentes.

### 3.3. Injeção de dependências e clientes tipados

No ASP.NET Core, dependências são registradas no contêiner de DI. Comercialização registra clientes HTTP tipados utilizando `AddHttpClient<IEventoClient, HttpEventoClient>()`, `AddHttpClient<IPortariaClient, HttpPortariaClient>()` e `AddHttpClient<IUsuarioClient, HttpUsuarioClient>()`, configurando URL base, timeout e um handler para a API Key interna.

Essa abordagem evita construir `HttpClient` repetidamente nos serviços de aplicação e concentra aspectos de comunicação nas implementações de infraestrutura.

### 3.4. MediatR

O MediatR é utilizado exclusivamente para comunicação interna da aplicação. A comunicação assíncrona entre microsserviços é realizada separadamente pelo RabbitMQ, utilizando eventos de integração.

O projeto, conforme documentado, não deve ser descrito como uma arquitetura de eventos distribuídos baseada em RabbitMQ, Kafka ou filas persistentes. As integrações entre aplicações continuam baseadas em HTTP.

### 3.5. RabbitMQ e serviços em segundo plano

O sistema utiliza RabbitMQ como broker de mensagens para comunicação assíncrona entre Eventos e Comercialização.

Eventos utiliza um `BackgroundService` para identificar, a cada minuto, eventos que atingiram seu horário de início e cujo processamento ainda não foi registrado.

Ao identificar um evento elegível, o serviço publica uma mensagem `EventoIniciadoEvent`.

Comercialização consome essa mensagem e encerra as ofertas de ingressos ativas relacionadas ao evento.

| Componente | Responsabilidade |
|---|---|
| `EventoLifecycleWorker` | Detectar eventos iniciados |
| `RabbitMqEventoPublisher` | Publicar mensagens de integração |
| `EventoIniciadoConsumer` | Consumir mensagens de integração |
| `OfertaIngressoService` | Encerrar as ofertas ativas |

```mermaid
flowchart LR
    E["EventoLifecycleWorker"] --> P["Publisher"]
    P --> X["Exchange: eventos"]
    X -->|"evento.iniciado"| Q["Queue: comercializacao.eventos.iniciados"]
    Q --> C["EventoIniciadoConsumer"]
    C --> S["OfertaIngressoService"]
```

A atualização das ofertas ocorre de maneira assíncrona e idempotente. As validações síncronas do estado temporal do evento permanecem necessárias para impedir operações inválidas antes do processamento da mensagem.

## 4. Comunicação e integrações

As comunicações entre microsserviços utilizam APIs REST, com representações JSON e identificadores GUID. As operações internas relevantes são:

| Origem | Destino | Tipo | Protocolo | Finalidade |
|---|---|---|---|---|
| Eventos | Comercialização | Consulta | HTTP REST | Verificar se existem ofertas válidas para publicar um evento |
| Comercialização | Eventos | Consulta | HTTP REST | Verificar a existência, propriedade e estado temporal do evento durante a criação de ofertas e pedidos |
| Comercialização | Usuários | Consulta | HTTP REST | Identificar e verificar a existência do comprador pelo e-mail durante a criação de pedidos |
| Comercialização | Portaria | Alteração | HTTP REST | Solicitar a criação de credenciais de acesso dos ingressos emitidos |
| Portaria | Comercialização | Consulta | HTTP REST | Obter informações de um ingresso pelo código durante a validação |
| Eventos | Comercialização | Notificação assíncrona | AMQP | Comunicar o início do evento para encerrar suas ofertas de ingressos |

O sistema possui quatro fluxos de consulta síncrona entre microsserviços, um fluxo de alteração remota via HTTP e um fluxo de comunicação assíncrona via AMQP.

```mermaid
flowchart TB
    subgraph Q["Consultas síncronas"]
        E["Eventos"] -->|"Verificar ofertas válidas"| C["Comercialização"]
        C -->|"Verificar estado temporal e dados do evento"| E
        C -->|"Consultar comprador por e-mail"| U["Usuários"]
        P["Portaria"] -->|"Consultar ingresso"| C
    end

    subgraph M["Alteração remota síncrona"]
        C2["Comercialização"] -->|"Criar credenciais"| P2["Portaria"]
    end

    subgraph A["Comunicação assíncrona"]
        E3["Eventos"] -->|"EventoIniciadoEvent"| R["RabbitMQ"]
        R -->|"Encerrar ofertas"| C3["Comercialização"]
    end
```

**Natureza das chamadas:** uma requisição HTTP enviada de um serviço para outro é uma integração *síncrona* do ponto de vista do cliente, mesmo quando o código C# usa `async`/`await`. Essa sintaxe não transforma a comunicação em mensageria.

**Contratos:** os nomes de interfaces (`IEventoClient`, `IPortariaClient` etc.) não determinam por si só a rota pública. Os endpoints precisos devem ser conferidos nos controllers e nas implementações HTTP, evitando documentar caminhos incorretos.

### 4.1. Integração externa com a Stripe

Comercialização utiliza `Stripe.net` para criar sessões do **Stripe Checkout**. A Stripe comunica resultados por **webhook HTTP**, recebido em Comercialização. O sistema deve validar a assinatura com `Stripe:WebhookSecret` e relacionar o evento ao pagamento/pedido correto.

```mermaid
sequenceDiagram
    participant B as Comprador
    participant C as Comercialização
    participant S as Stripe
    B->>C: Solicitar pedido / checkout
    C->>S: Criar sessão de Checkout
    S-->>C: Identificador e URL da sessão
    C-->>B: URL para pagamento
    B->>S: Concluir Checkout
    S-->>C: Webhook assinado
    C->>C: Validar assinatura e processar pagamento
```

O redirecionamento do navegador para `Stripe:SuccessUrl` **não é comprovação suficiente** de pagamento confirmado. A confirmação precisa ser baseada em evidência confiável do provedor, como webhook validado e o estado da transação.

## 5. Referências entre microsserviços

### 5.1. Identificadores externos não são FKs locais

Cada serviço armazena identificadores de recursos de outros serviços como valores GUID. Esses campos **não formam chaves estrangeiras SQL entre os bancos SQLite independentes**.

| Serviço que armazena | Campo | Entidade de origem |
|---|---|---|
| Eventos — `Evento` | `OrganizadorId` | Usuários — `UsuarioModel` |
| Comercialização — `OfertaIngresso` | `IdEvento` | Eventos — `Evento` |
| Comercialização — `OfertaIngresso` | `IdOrganizador` | Usuários — `UsuarioModel` |
| Comercialização — `Pedido` | `CompradorId` | Usuários — `UsuarioModel` |
| Portaria — `CredencialAcesso` | `IngressoId` | Comercialização — `Ingresso` |
| Portaria — `CredencialAcesso` | `EventoId` | Eventos — `Evento` |

Durante a criação de um pedido, Comercialização consulta Usuários pelo e-mail informado no contrato HTTP. O identificador GUID retornado é utilizado para construir o contrato interno de criação do pedido.

O pedido permanece associado ao comprador por meio de `CompradorId`, não pelo endereço de e-mail. Comercialização não possui responsabilidade sobre o cadastro ou gerenciamento dos usuários.

```mermaid
flowchart LR
    U["Usuários\nUsuarioModel"]
    E["Eventos\nEvento"]
    O["Comercialização\nOfertaIngresso"]
    R["Comercialização\nPedido"]
    I["Comercialização\nIngresso"]
    P["Portaria\nCredencialAcesso"]
    E -. "OrganizadorId" .-> U
    O -. "IdEvento" .-> E
    O -. "IdOrganizador" .-> U
    R -. "CompradorId" .-> U
    P -. "EventoId" .-> E
    P -. "IngressoId" .-> I
```

**Consequências arquiteturais:**

1. Um banco não pode garantir, por FK, que o GUID remoto ainda existe.
2. A validação de existência, propriedade e elegibilidade precisa ocorrer por contratos e regras de aplicação, de acordo com cada caso de uso.
3. Se o serviço de origem estiver indisponível, uma operação dependente da consulta poderá falhar ou precisar de política de recuperação.
4. Alterações entre serviços **não participam da mesma transação SQLite**; salvar dados em Comercialização não faz rollback automático de alterações já concluídas em Portaria.

### 5.2. Separação entre modelo de dados e integração

As relações **internas** — por exemplo, `Pedido` → `Pagamento` e `Pedido` → `Ingresso` em Comercialização — pertencem à [Modelagem](Modelagem.md). As referências **entre serviços**, assim como os seus riscos de consistência, são explicadas aqui.

## 6. Persistência e propriedade dos dados

Cada microsserviço utiliza **Entity Framework Core com SQLite**, mantendo um arquivo de banco próprio no ambiente de desenvolvimento.

```mermaid
flowchart LR
    U["Usuários"] --> DBU[("SQLite\nUsuários")]
    E["Eventos"] --> DBE[("SQLite\nEventos")]
    C["Comercialização"] --> DBC[("SQLite\nComercialização")]
    P["Portaria"] --> DBP[("SQLite\nPortaria")]
```

Não existe um banco relacional compartilhado entre os quatro serviços. A separação preserva a autonomia dos modelos, mas exige coordenação por API em operações interdependentes.

As aplicações utilizam migrations do EF Core; no fluxo local documentado, elas são aplicadas durante a inicialização. A estrutura exata das tabelas, índices e constraints deve ser conferida nos `DbContext` e nas migrations, e não inferida apenas das classes C#.

**Produção:** SQLite exige atenção à persistência do volume, concorrência, backups e características da hospedagem. A existência de `Data Source=...db` no desenvolvimento não garante persistência em ambientes com sistema de arquivos efêmero.

## 7. Autenticação e autorização

### 7.1. Autenticação do usuário — JWT RS256

O microsserviço **Usuários** emite JWTs assinados com uma **chave privada RSA**. Eventos, Comercialização e Portaria validam esses tokens com a **chave pública** correspondente, verificando assinatura, emissor, audiência e validade temporal.

```mermaid
sequenceDiagram
    participant U as Usuários
    participant B as Cliente
    participant E as Serviço protegido
    B->>U: Credenciais de acesso
    U-->>B: JWT assinado (RS256)
    B->>E: Authorization: Bearer JWT
    E->>E: Validar assinatura e claims
    E-->>B: Resultado autorizado ou erro
```

Na configuração exibida de Comercialização, `MapInboundClaims = false` preserva os nomes originais dos claims e a identidade é lida de `sub`. A autorização de uma ação exige mais que um token válido: deve conferir as permissões e a propriedade dos recursos envolvidos.

### 7.2. Autenticação entre serviços — API Key

Endpoints internos selecionados utilizam **API Key**, enviada pelos clientes HTTP internos e verificada no serviço de destino. O código de Comercialização registra um `InternalApiKeyHandler` e um esquema `ApiKeyAuthenticationHandler`, com políticas separadas para JWT de usuários e API Key de serviço.

As API Keys são segredos compartilhados de aplicação, não identidades de usuários finais. Elas devem ser armazenadas externamente aos arquivos versionados e transmitidas em conexões protegidas em produção.

> JWT e API Key **têm propósitos distintos**: a primeira autentica o usuário; a segunda autentica uma chamada de integração. Uma API Key válida não deve autorizar indiscriminadamente operações de usuário.

### 7.3. Operações públicas

A criação de pedidos pode ser realizada por visitantes sem conta de acesso, desde que possuam um cadastro em Usuários.

A identificação do comprador ocorre pelo endereço de e-mail informado na criação do pedido. Comercialização consulta internamente Usuários utilizando autenticação por API Key.

**Limitação da versão acadêmica:** o endereço de e-mail informado não passa por verificação de titularidade. Sua utilização permite identificar um cadastro existente, mas não representa autenticação do comprador nem comprovação de identidade.

As consultas públicas de pedidos e ingressos constituem uma simplificação acadêmica e deverão receber mecanismos adequados de autorização antes de uma implantação em produção.

## 8. Fluxos principais

### 8.1. Publicação de evento

```mermaid
sequenceDiagram
    actor O as Organizador
    participant E as Eventos
    participant C as Comercialização
    O->>E: Solicitar publicação
    E->>C: Consultar existência de oferta válida
    C-->>E: Resultado da consulta
    alt Existe oferta válida e demais critérios atendidos
        E->>E: Registrar publicação
        E-->>O: Evento publicado
    else Oferta não atende aos critérios
        E-->>O: Publicação recusada
    end
```

### 8.2. Compra e emissão de ingresso

```mermaid
sequenceDiagram
    actor B as Comprador
    participant C as Comercialização
    participant U as Usuários
    participant E as Eventos
    participant S as Stripe
    participant P as Portaria

    B->>C: Criar pedido informando e-mail
    C->>U: Consultar comprador por e-mail

    alt Comprador inexistente
        U-->>C: 404 Not Found
        C-->>B: Comprador não encontrado
    else Comprador existente
        U-->>C: Identificador do comprador
        C->>E: Consultar evento
        E-->>C: Dados e estado temporal do evento

        alt Evento agendado, publicado e oferta elegível
            C->>C: Validar disponibilidade e criar pedido
            C->>S: Criar sessão Checkout
            S-->>C: URL e ID da sessão
            C-->>B: Dados para pagamento
            B->>S: Realizar pagamento
            S-->>C: Webhook de pagamento
            C->>C: Confirmar pagamento e emitir ingressos
            C->>P: Solicitar criação de credenciais
            P-->>C: Resultado da operação
        else Evento ou oferta inelegível
            C-->>B: Solicitação recusada
        end
    end
```

Esse desenho representa o **fluxo pretendido**. A implementação de cada transição e sua confiabilidade devem ser verificadas nos serviços e handlers. Pedidos gratuitos podem seguir um caminho diferente, sem sessão Stripe.

Além disso, esse fluxo ele não cobre cenários de inconsistências, como falta de energia no meio da requisição, concorrência ou algum erro interno. Nesse projeto, ele é esperado acontecer sempre sem nenhum erro, ou seja possa ser que exista inconsistência de dados.

### 8.3. Validação de entrada

```mermaid
sequenceDiagram
    actor O as Organizador
    participant P as Portaria
    participant C as Comercialização
    O->>P: Solicitar check-in com código do ingresso
    P->>C: Consultar ingresso pelo código
    C-->>P: Dados do ingresso
    P->>P: Verificar autorização, elegibilidade e estado da credencial
    alt Credencial pode ser utilizada
        P->>P: Registrar utilização
        P-->>O: Entrada autorizada
    else Credencial inválida ou utilizada
        P-->>O: Entrada negada
    end
```

O QR Code pode transportar um identificador/código de ingresso. A **leitura física ou por câmera** depende de uma interface ou equipamento apropriado; o endpoint de check-in, isoladamente, não comprova a existência de um leitor implementado.

### 8.4. Criação de oferta de ingresso

```mermaid
sequenceDiagram
    actor O as Organizador
    participant C as Comercialização
    participant E as Eventos

    O->>C: Solicitar criação de oferta
    C->>E: Consultar evento
    E-->>C: Dados, organizador e estado temporal

    alt Evento agendado e pertencente ao organizador
        C->>C: Validar e persistir oferta
        C-->>O: Oferta criada
    else Evento inelegível ou organizador sem permissão
        C-->>O: Solicitação recusada
    end
```

A criação de ofertas não exige que o evento esteja publicado, pois a existência de uma oferta válida é uma das condições para sua publicação.

Entretanto, somente eventos agendados permitem a criação de novas ofertas.

### 8.5. Encerramento automático de ofertas

```mermaid
sequenceDiagram
    participant E as Eventos
    participant R as RabbitMQ
    participant C as Comercialização
    participant DB as Banco de Comercialização

    loop A cada minuto
        E->>E: Identificar eventos iniciados não processados
        E->>R: Publicar EventoIniciadoEvent
        R-->>E: Confirmar publicação
        E->>E: Registrar InicioProcessadoEm
    end

    R->>C: Entregar mensagem
    C->>DB: Encerrar ofertas ativas
    DB-->>C: Atualização concluída
    C->>R: ACK
```

O encerramento das ofertas ocorre quando o evento atinge seu horário de início, e não quando termina.

Ofertas esgotadas ou previamente encerradas permanecem inalteradas.

A validação síncrona do estado do evento impede a criação de novos pedidos e ofertas durante o intervalo entre o início do evento e o processamento da mensagem.

## 9. Consistência e tratamento de falhas

A arquitetura distribuída introduz falhas que não ocorrem dentro de uma única transação local:

| Situação | Risco | Consideração arquitetural |
|---|---|---|
| Pagamento confirmado e Portaria indisponível | Ingresso emitido sem credencial disponível | Reprocessamento seguro da emissão de credenciais |
| Webhook recebido repetidamente | Emissão duplicada ou transições incorretas | Idempotência baseada na identidade do evento/sessão/pagamento |
| Timeout após envio de credencial | Não saber se a operação remota foi concluída | Consultar estado e evitar duplicação antes de repetir |
| Duas tentativas simultâneas de check-in | Uso duplicado do mesmo ingresso | Atualização atômica condicionada ao estado da credencial |
| Consulta remota indisponível | Falha ao verificar evento ou ingresso | Tratamento explícito de indisponibilidade e timeout |
| Dados de data/hora inconsistentes | Estado incorreto do evento | Padronização de instantes e conversão de fuso |
| Usuários indisponível | Impossibilidade de identificar o comprador | Impedir a criação do pedido sem validação do cadastro |
| RabbitMQ indisponível | Atraso no encerramento automático das ofertas | Manter o processamento do início pendente para nova tentativa |
| Mensagem de integração duplicada | Repetição do encerramento de ofertas | Atualização idempotente condicionada ao status ATIVA |
| Atraso no processamento da mensagem | Ofertas permanecem temporariamente ativas após o início | Validar sincronamente o estado temporal do evento |
| Falha após publicação e antes da persistência | Possibilidade de republicação da mensagem | Consumidor idempotente e futura adoção de Transactional Outbox |

O encerramento automático das ofertas utiliza consistência eventual por meio da mensageria. A implementação utiliza mensagens persistentes, confirmações de publicação, ACK, NACK e operações idempotentes.

Esses mecanismos reduzem os riscos de perda e duplicação de mensagens, mas não garantem processamento exatamente uma vez nem atomicidade entre o banco de Eventos e o RabbitMQ.

Transactional Outbox, Dead Letter Queue, Saga e Circuit Breaker permanecem como possibilidades de evolução.


## 10. Tecnologias e decisões

| Tecnologia / técnica | Utilização |
|---|---|
| **.NET 10 / ASP.NET Core** | Implementação das APIs |
| **REST e HTTP/JSON** | Contratos de comunicação |
| **Entity Framework Core / SQLite** | Persistência independente por serviço |
| **JWT RS256** | Autenticação de usuários e validação descentralizada |
| **API Key** | Autenticação de integrações internas selecionadas |
| **HttpClientFactory / clientes tipados** | Comunicação HTTP e configuração dos clientes |
| **MediatR** | Notificações e eventos internos de Comercialização |
| **Stripe.net / Checkout / webhooks** | Integração com pagamentos |
| **QRCoder** | Geração de QR Codes de ingressos |
| **OpenAPI / Scalar** | Documentação e exploração das APIs em desenvolvimento |
| **RabbitMQ / AMQP** | Comunicação assíncrona entre microsserviços |
| **RabbitMQ.Client** | Publicação e consumo de mensagens AMQP |
| **BackgroundService** | Detecção periódica de eventos iniciados e consumo de mensagens |
| **Docker Compose** | Provisionamento local do broker RabbitMQ |
| **Publisher Confirms / ACK / NACK** | Controle de publicação e processamento das mensagens |
| **Consistência eventual** | Propagação do início dos eventos para encerramento das ofertas |

A documentação local utiliza `appsettings.json`, `appsettings.Development.json` e .NET User Secrets. Ambientes publicados devem fornecer configurações apropriadas por variáveis de ambiente ou outros mecanismos seguros.

## 11. Limitações e evolução

A separação arquitetural foi dimensionada para o escopo acadêmico e para eventos presenciais simples. Não se deve interpretar a presença de quatro serviços como prova de alta disponibilidade, escalabilidade horizontal automática ou transações distribuídas garantidas.

Possíveis evoluções, sujeitas a necessidades reais, incluem:

- Tratamento completo de cancelamento, estorno, expiração e liberação de estoque.
- Reprocessamento persistente de integrações e webhooks, com idempotência verificável.
- Autorização de colaboradores e dispositivos de check-in pelo organizador.
- Revisão de armazenamento para hospedagem com persistência e concorrência adequadas.
- Verificação de titularidade do e-mail dos compradores.
- Autorização para consulta e gerenciamento de pedidos e ingressos.
- Implementação do Transactional Outbox Pattern.
- Configuração de Dead Letter Queues e políticas limitadas de novas tentativas.
- Reutilização de conexões e canais de publicação do RabbitMQ.

**Decisão documental:** a descrição detalhada de campos, entidades e relacionamentos locais pertence a `Modelagem.md`. O modelo de colaboração, referências externas, políticas de comunicação e consistência pertence a este documento.

## 12. Documentação complementar

- [Requisitos de Software](RequisitosSoftware.md) — propósito, atores e requisitos funcionais.
- [Regras de Negócio](RegrasNegocio.md) — restrições e condições de operação.
- [Modelagem de Dados](Modelagem.md) — entidades e relacionamentos internos de cada microsserviço.
- [Pré-requisitos](PreRequisitos.md) — dependências e preparação do ambiente.
- [Referência de Configurações](Configuracoes.md) — propriedades de configuração e requisitos de segurança.
- [Configuração Local](ConfiguracaoLocal/) — execução de cada microsserviço.

