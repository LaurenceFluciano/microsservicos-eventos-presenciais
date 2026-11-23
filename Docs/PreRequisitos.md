# Pré-requisitos e dependências do projeto

Este documento descreve os requisitos necessários para executar os microsserviços do sistema de gerenciamento de eventos presenciais, incluindo ferramentas, dependências, autenticação e recursos compartilhados.

As dependências NuGet estão declaradas nos arquivos `.csproj` e são restauradas automaticamente pelo .NET. Não é necessário instalar manualmente cada pacote.

Para conhecer as propriedades de configuração exigidas, consulte [Configurações](Configuracoes.md). Para configurar cada microsserviço, consulte o diretório [Configuração local](ConfiguracaoLocal/).

## 1. Requisitos do ambiente

| Requisito | Finalidade | Obrigatório |
|---|---|---|
| .NET SDK 10 | Compilar, executar e testar os microsserviços | Sim |
| Git | Obter e versionar o código-fonte | Sim, para o fluxo com repositório |
| OpenSSL | Gerar as chaves RSA utilizadas na autenticação JWT | Sim, para a preparação inicial das chaves |
| SQLite | Persistência dos dados de cada microsserviço | Sim, por meio das dependências .NET |
| Stripe CLI | Encaminhar webhooks e testar pagamentos localmente | Para o teste local com Stripe |
| .NET User Secrets | Armazenar configurações locais sensíveis | Para o procedimento local documentado |

### 1.1. Observações

A ferramenta de linha de comando `sqlite3` é opcional, podendo ser utilizada para inspecionar os bancos de dados.

A ferramenta `dotnet-ef` também é opcional para executar as aplicações, pois os serviços aplicam as migrations automaticamente durante a inicialização. Ela é útil para criar e administrar migrations.

## 2. Portas dos microsserviços

| Microsserviço | Diretório | Porta HTTP de desenvolvimento |
|---|---|---|
| Usuários | `Usuario/Api` | 5296 | 
| Eventos | `Evento/Api` | 5297 | 
| Comercialização | `Comercializacao/Api` | 5298 | 
| Portaria | `Portaria/Api` | 5299 |

Cada microsserviço possui seu próprio banco de dados SQLite.

As portas representam a configuração adotada para o desenvolvimento local, não sendo obrigatórias para outros ambientes.

## 3. Dependências NuGet

Os pacotes utilizados estão definidos nos arquivos `Api.csproj` de cada microsserviço.

### 3.1. Dependências compartilhadas

| Pacote | Finalidade |
|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validação de tokens JWT |
| `Microsoft.AspNetCore.OpenApi` | Geração da especificação OpenAPI |
| `Microsoft.EntityFrameworkCore.Sqlite` | Persistência com Entity Framework Core e SQLite |
| `Microsoft.EntityFrameworkCore.Design` | Suporte às ferramentas de desenvolvimento do EF Core |
| `Microsoft.EntityFrameworkCore.Tools` | Ferramentas para migrations e gerenciamento do banco |
| `Scalar.AspNetCore` | Interface de documentação e testes da API |

Todos os microsserviços utilizam o framework `net10.0`.

### 3.2. Dependências específicas

| Microsserviço | Pacote | Versão declarada | Finalidade |
|---|---|---|---|
| Usuários | `System.IdentityModel.Tokens.Jwt` | 8.23.0 | Manipulação de tokens JWT |
| Eventos | `Microsoft.IdentityModel.Tokens` | 8.23.0 | Validação de assinaturas e parâmetros JWT |
| Comercialização | `Microsoft.IdentityModel.Tokens` | 8.19.2 | Validação JWT |
| Comercialização | `MediatR` | 15.0.0 | Publicação e tratamento de notificações internas |
| Comercialização | `Stripe.net` | 53.2.0 | Integração com a API de pagamentos Stripe |
| Comercialização | `QRCoder` | 1.9.0 | Geração de QR Codes dos ingressos |
| Portaria | `Microsoft.IdentityModel.Tokens` | 8.23.0 | Validação de assinaturas JWT |
| Portaria | `System.IdentityModel.Tokens.Jwt` | 8.23.0 | Suporte a tokens JWT |

As versões acima representam os arquivos `.csproj` fornecidos na preparação da documentação.

As dependências transitivas são resolvidas pelo NuGet durante a restauração do projeto.

## 4. Preparação das chaves RSA

O sistema utiliza autenticação JWT com assinatura assimétrica RSA.

O microsserviço Usuários é responsável pela emissão e assinatura dos tokens utilizando uma chave privada.

Os demais microsserviços validam os tokens utilizando a chave pública correspondente.

### 4.1. Gerar a chave privada

Execute em um diretório seguro:

```bash
openssl genpkey \
  -algorithm RSA \
  -pkeyopt rsa_keygen_bits:2048 \
  -out private.pem
```

O arquivo `private.pem` contém a chave privada e deve permanecer protegido.

### 4.2. Gerar a chave pública

A partir da chave privada:

```bash
openssl pkey \
  -in private.pem \
  -pubout \
  -out public.pem
```

O arquivo `public.pem` contém a chave pública correspondente.

### 4.3. Distribuição das chaves

| Microsserviço | Chave privada | Chave pública |
|---|---|---|
| Usuários | Utiliza para assinar JWTs | Pode utilizar, conforme sua implementação |
| Eventos | Não utiliza | Utiliza para validar JWTs |
| Comercialização | Não utiliza | Utiliza para validar JWTs |
| Portaria | Não utiliza | Utiliza para validar JWTs |

A chave privada não deve ser compartilhada com Eventos, Comercialização ou Portaria.

Os arquivos PEM não devem ser incluídos no repositório Git.

A configuração das chaves em cada projeto é descrita nos respectivos guias de configuração local.

## 5. Configurações compartilhadas de autenticação

Os microsserviços devem utilizar valores compatíveis para a validação dos JWTs.

| Configuração | Requisito |
|---|---|
| `Jwt:Issuer` | Deve corresponder ao emissor informado por Usuários |
| `Jwt:Audience` | Deve corresponder à audiência definida para os tokens |
| `Jwt:PublicKeyPem` | Deve conter a chave pública associada à chave privada utilizada por Usuários |

Os serviços também utilizam API Keys para autenticar determinadas chamadas internas.

As chaves utilizadas pelo cliente HTTP e pelo serviço de destino precisam ser compatíveis.

Uma API Key deve possuir valor suficientemente aleatório e não deve ser publicada em arquivos versionados.

## 6. Dependências entre microsserviços

| Origem | Destino | Operação |
|---|---|---|
| Eventos | Comercialização | Consultar a existência de ofertas válidas |
| Comercialização | Eventos | Consultar informações dos eventos |
| Comercialização | Portaria | Solicitar criação de credenciais de acesso |
| Portaria | Comercialização | Consultar ingressos pelo código |

As integrações utilizam HTTP REST.

O microsserviço Comercialização também se comunica com a API externa Stripe para operações de pagamento.

## 7. Dependência externa — Stripe

Para testar o fluxo completo de pagamentos, é necessária uma conta Stripe com acesso ao ambiente de teste.

A integração utiliza:

- Chave secreta de API de teste.
- Segredo de assinatura do webhook.
- Stripe Checkout.
- Stripe CLI para encaminhamento dos eventos ao ambiente local.

O Stripe CLI é uma ferramenta de desenvolvimento, não sendo necessário para receber webhooks em um endpoint HTTPS público configurado na Stripe.

Os procedimentos específicos encontram-se no guia de configuração local de Comercialização.

## 8. Projetos de testes

Os projetos de testes possuem dependências próprias declaradas em seus arquivos `.csproj`.

Entre as bibliotecas identificadas estão:

| Pacote | Finalidade |
|---|---|
| `Microsoft.NET.Test.Sdk` | Infraestrutura de execução de testes |
| `xunit` | Framework de testes |
| `xunit.runner.visualstudio` | Integração com ferramentas de execução |
| `Microsoft.AspNetCore.Mvc.Testing` | Testes de integração de aplicações ASP.NET Core |
| `Moq` | Criação de objetos simulados |
| `coverlet.collector` | Coleta de cobertura de testes |

As dependências variam conforme o projeto de testes.

Não é necessária instalação individual dos pacotes, desde que estejam declarados corretamente nos arquivos `.csproj`.

## 9. Gerenciamento das dependências

Para restaurar as dependências de um microsserviço:

```bash
dotnet restore
```

Para verificar a compilação:

```bash
dotnet build
```

Para executar os testes existentes:

```bash
dotnet test
```

Os comandos devem ser executados no diretório do projeto ou da solução correspondente.

A instalação manual dos pacotes NuGet não faz parte do processo normal de configuração de um clone do repositório.

## 10. Próximas etapas

Depois de atender aos pré-requisitos, configure individualmente os microsserviços:

1. [Usuários](ConfiguracaoLocal/Usuario.md)
2. [Eventos](ConfiguracaoLocal/Eventos.md)
3. [Comercialização](ConfiguracaoLocal/Comercializacao.md)
4. [Portaria](ConfiguracaoLocal/Portaria.md)

As configurações necessárias são descritas em [Referência técnica de configurações](Configuracoes.md).