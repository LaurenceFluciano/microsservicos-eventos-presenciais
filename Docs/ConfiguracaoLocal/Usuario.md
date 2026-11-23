# Configuração local — Usuários

Este documento apresenta os procedimentos necessários para configurar e executar o microsserviço **Usuários** em ambiente de desenvolvimento.

O microsserviço é responsável pelo gerenciamento de usuários, autenticação e emissão de tokens JWT utilizados pelos demais serviços do sistema.

Para informações complementares, consulte:

- [Pré-requisitos](../PreRequisitos.md) — ferramentas, dependências e geração das chaves RSA.
- [Referência técnica de configurações](../Configuracoes.md) — descrição das propriedades exigidas pela aplicação.

## 1. Acessar o diretório do projeto

A partir da raiz do repositório:

```bash
cd Usuario/Api
```

Os comandos seguintes devem ser executados nesse diretório.

## 2. Configurar os User Secrets

O projeto já possui um `UserSecretsId` definido em `Api.csproj`. Portanto, não é necessário executar `dotnet user-secrets init` novamente.

### 2.1. Configurar a API Key

A API Key é utilizada na autenticação de requisições internas entre microsserviços, conforme os endpoints disponibilizados pelo serviço.

```bash
dotnet user-secrets set "Authentication:ApiKey" "SUA_CHAVE_INTERNA"
```

Utilize a chave definida para a comunicação interna dos serviços.

Não utilize valores triviais, como `123456`, nem publique a credencial no repositório.

### 2.2. Configurar a chave privada RSA

Usuários utiliza a chave privada RSA para assinar os tokens JWT.

Considerando que o arquivo `private.pem` já tenha sido gerado conforme as instruções de pré-requisitos:

```bash
dotnet user-secrets set "Jwt:PrivateKeyPem" "$(cat /caminho/para/private.pem)"
```

Substitua o caminho pela localização real do arquivo.

**Importante:** a chave privada deve permanecer restrita ao microsserviço Usuários. Não a distribua para Eventos, Comercialização ou Portaria.

### 2.3. Configurar a chave pública RSA

A chave pública corresponde à chave privada utilizada para assinar os JWTs e pode ser distribuída aos microsserviços responsáveis pela validação.

```bash
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat /caminho/para/public.pem)"
```

As duas chaves devem pertencer ao mesmo par RSA.

Caso a implementação de `TokenJwtService` utilize ambas as propriedades, mantenha as duas configuradas.

## 3. Verificar as configurações locais

Execute:

```bash
dotnet user-secrets list
```

As propriedades esperadas são:

| Configuração | Finalidade |
|---|---|
| `Authentication:ApiKey` | Autenticação das chamadas internas |
| `Jwt:PrivateKeyPem` | Assinatura dos tokens JWT |
| `Jwt:PublicKeyPem` | Disponibilização ou utilização da chave pública RSA |

As seguintes propriedades já estão definidas em `appsettings.json`:

| Configuração | Valor padrão |
|---|---|
| `Jwt:Issuer` | `Usuario` |
| `Jwt:Audience` | `EventosPresenciais` |
| `Jwt:AccessTokenMinutes` | `60` |

A conexão com o banco SQLite está definida em `appsettings.Development.json`.

**Atenção:** `dotnet user-secrets list` exibe os valores armazenados. Não compartilhe sua saída contendo credenciais ou a chave privada RSA.

## 4. Executar o microsserviço

Restaure as dependências:

```bash
dotnet restore
```

Execute a aplicação no ambiente de desenvolvimento:

```bash
dotnet run --environment Development
```

O endereço HTTP utilizado pelo microsserviço no ambiente local é:

```text
http://localhost:5296
```

A interface Scalar pode ser acessada em:

```text
http://localhost:5296/scalar/v1
```

O endereço efetivo depende das configurações definidas em `Properties/launchSettings.json` ou de outros endpoints configurados para a aplicação.

## 5. Verificar a emissão de JWTs

Com o microsserviço em execução, utilize os endpoints de autenticação disponíveis na API.

O fluxo esperado consiste em:

1. Cadastrar um usuário, caso ainda não exista.
2. Autenticar-se utilizando as credenciais cadastradas.
3. Receber um token JWT.
4. Utilizar esse token nas operações protegidas dos demais microsserviços.

O JWT deve ser enviado no cabeçalho HTTP:

```http
Authorization: Bearer SEU_TOKEN_JWT
```

O microsserviço Usuários assina os tokens utilizando sua chave privada RSA.

Eventos, Comercialização e Portaria validam a assinatura utilizando a chave pública correspondente.

O emissor, a audiência e o tempo de expiração são definidos pela configuração JWT de Usuários.

## 6. Banco de dados

Usuários utiliza SQLite por meio do Entity Framework Core.

A conexão configurada para desenvolvimento é:

```text
Data Source=Nome_banco.db
```

O banco é criado ou atualizado pelas migrations durante a inicialização, conforme a configuração atual da aplicação.

Cada microsserviço possui seu próprio banco de dados.

## 7. Solução de problemas

| Problema | Verificação |
|---|---|
| `Authentication:ApiKey` ausente | Conferir os User Secrets de Usuários |
| `Jwt:PrivateKeyPem` ausente | Verificar a importação da chave privada |
| `Jwt:PublicKeyPem` ausente | Verificar a importação da chave pública |
| Erro ao importar chave RSA | Verificar o formato PEM e a integridade do arquivo |
| JWT com assinatura inválida | Verificar se as chaves privada e pública pertencem ao mesmo par |
| JWT rejeitado por outro serviço | Conferir `Issuer`, `Audience`, assinatura e expiração |
| Falha na inicialização do banco | Verificar o caminho SQLite e as migrations |
| Porta 5296 ocupada | Verificar processos que estejam utilizando a porta |

Para investigar falhas, consulte os logs da aplicação no terminal.

---

## Configuração mínima esperada

| Configuração | Origem |
|---|---|
| `Jwt:Issuer` | `appsettings.json` |
| `Jwt:Audience` | `appsettings.json` |
| `Jwt:AccessTokenMinutes` | `appsettings.json` |
| `Jwt:PrivateKeyPem` | User Secrets |
| `Jwt:PublicKeyPem` | User Secrets |
| `Authentication:ApiKey` | User Secrets |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` |
| `Cors:AllowedOrigins` | `appsettings.Development.json` |

Com essas configurações, o microsserviço Usuários estará preparado para emissão de JWTs e execução em ambiente local.