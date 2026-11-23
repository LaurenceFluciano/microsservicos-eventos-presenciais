# Configuração local — Portaria

Este documento apresenta os procedimentos necessários para configurar e executar o microsserviço **Portaria** em ambiente de desenvolvimento.

O microsserviço é responsável pela criação de credenciais de acesso e pela realização do check-in dos participantes de eventos presenciais.

Para informações complementares, consulte:

- [Pré-requisitos](../PreRequisitos.md) — ferramentas, dependências e geração das chaves RSA.
- [Referência técnica de configurações](../Configuracoes.md) — descrição das propriedades exigidas pela aplicação.

## 1. Acessar o diretório do projeto

A partir da raiz do repositório:

```bash
cd Portaria/Api
```

Os comandos seguintes devem ser executados nesse diretório.

## 2. Configurar os User Secrets

O projeto já possui um `UserSecretsId` definido em `Api.csproj`. Portanto, não é necessário executar `dotnet user-secrets init` novamente.

Antes de continuar, verifique se o identificador de User Secrets de Portaria é diferente daquele utilizado pelo microsserviço Usuários.

### 2.1. Configurar a API Key

A API Key é utilizada para autenticar chamadas internas realizadas por outros microsserviços, especialmente Comercialização.

```bash
dotnet user-secrets set "Authentication:ApiKey" "SUA_CHAVE_INTERNA"
```

Utilize a chave compartilhada definida para a comunicação entre os microsserviços.

Não utilize valores triviais, como `123456`, nem publique a chave no repositório.

### 2.2. Configurar a chave pública RSA

Portaria utiliza a chave pública RSA para validar os JWTs emitidos pelo microsserviço Usuários.

Considerando que o arquivo `public.pem` já tenha sido gerado conforme as instruções de pré-requisitos:

```bash
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat /caminho/para/public.pem)"
```

Substitua o caminho pela localização real do arquivo.

A chave pública deve corresponder à chave privada utilizada por Usuários para assinar os tokens.

**Importante:** Portaria não precisa da chave privada RSA, pois não realiza a emissão de JWTs.

## 3. Verificar as configurações locais

Execute:

```bash
dotnet user-secrets list
```

As propriedades esperadas são:

| Configuração | Finalidade |
|---|---|
| `Authentication:ApiKey` | Autenticação das chamadas internas |
| `Jwt:PublicKeyPem` | Validação da assinatura dos JWTs |

As configurações `Jwt:Issuer` e `Jwt:Audience` já estão definidas no `appsettings.json`.

O arquivo `appsettings.Development.json` contém a conexão SQLite e o endereço local do microsserviço Comercialização.

**Atenção:** o comando `dotnet user-secrets list` exibe os valores armazenados. Não compartilhe sua saída contendo credenciais.

## 4. Executar o microsserviço

Restaure as dependências:

```bash
dotnet restore
```

Execute a aplicação:

```bash
dotnet run
```

A aplicação utiliza as configurações definidas em `Properties/launchSettings.json`.

O endereço HTTP utilizado no ambiente de desenvolvimento é:

```text
http://localhost:5299
```

A interface Scalar está disponível em:

```text
http://localhost:5299/scalar/v1
```

## 5. Verificar as integrações

O microsserviço Portaria depende de Comercialização para consultar os dados de ingressos apresentados durante o check-in.

A URL base dessa integração é definida por:

```text
Services:Comercializacao:BaseUrl
```

No ambiente de desenvolvimento, o valor esperado é:

```text
http://localhost:5298/
```

Para testar o fluxo completo de check-in, também é necessário um JWT válido emitido por Usuários e uma credencial de acesso previamente criada em Portaria.

### Autenticação das operações

O microsserviço utiliza dois mecanismos:

| Mecanismo | Utilização |
|---|---|
| JWT Bearer | Autenticação dos organizadores responsáveis pelo check-in |
| API Key | Autenticação das requisições internas, como a criação de credenciais por Comercialização |

O JWT deve ser enviado no cabeçalho:

```http
Authorization: Bearer SEU_TOKEN_JWT
```

Portaria verifica a assinatura e a expiração do token, mas não determina sua duração.

## 6. Banco de dados

Portaria utiliza SQLite por meio do Entity Framework Core.

O arquivo de banco de dados configurado no desenvolvimento é:

```text
Portaria.db
```

As migrations são aplicadas automaticamente durante a inicialização, por meio de `Database.Migrate()`.

Não é necessário executar manualmente as migrations para uma inicialização normal do ambiente.

## 7. Solução de problemas

| Problema | Verificação |
|---|---|
| `Jwt:PublicKeyPem` ausente | Conferir os User Secrets de Portaria |
| HTTP 401 no check-in | Verificar JWT, assinatura e expiração |
| HTTP 403 | Verificar as permissões do organizador |
| Falha de autenticação interna | Conferir a API Key compartilhada |
| Erro ao consultar ingressos | Verificar Comercialização e sua URL base |
| Porta 5299 ocupada | Verificar se outro processo utiliza a porta |
| Falha no SQLite | Verificar a conexão e as migrations |

Para investigar falhas, consulte os logs da aplicação no terminal.

---

## Configuração mínima esperada

| Configuração | Origem |
|---|---|
| `Jwt:Issuer` | `appsettings.json` |
| `Jwt:Audience` | `appsettings.json` |
| `Jwt:PublicKeyPem` | User Secrets |
| `Authentication:ApiKey` | User Secrets |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` |
| `Services:Comercializacao:BaseUrl` | `appsettings.Development.json` |
| `Cors:AllowedOrigins` | `appsettings.Development.json` |

Com essas configurações, Portaria estará preparada para execução e testes em ambiente local.