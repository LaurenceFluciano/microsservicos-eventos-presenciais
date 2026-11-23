# Configuração local — Eventos

Este documento apresenta os procedimentos necessários para configurar os segredos do microsserviço **Eventos** no ambiente de desenvolvimento.

Antes de continuar, consulte:

- [Pré-requisitos](../PreRequisitos.md) — ferramentas, dependências e geração das chaves RSA.
- [Referência técnica de configurações](../Configuracoes.md) — propriedades exigidas pelo microsserviço.

## 1. Acessar o projeto

A partir da raiz do repositório:

```bash
cd Evento/Api
```

Os comandos seguintes devem ser executados nesse diretório.

## 2. Configurar o .NET User Secrets

O projeto já possui `UserSecretsId` definido em `Api.csproj`. Portanto, não é necessário executar `dotnet user-secrets init` novamente.

### 2.1. Configurar a API Key

A API Key é utilizada para autenticar requisições internas entre microsserviços.

```bash
dotnet user-secrets set "Authentication:ApiKey" "SUA_CHAVE_INTERNA"
```

Utilize a chave compartilhada definida para a comunicação entre os microsserviços.

Não utilize valores triviais, como `123456`, nem publique a credencial no repositório.

### 2.2. Configurar a chave pública RSA

O microsserviço Eventos utiliza a chave pública para validar os JWTs emitidos por Usuários.

Considerando que o arquivo `public.pem` já foi gerado conforme os pré-requisitos:

```bash
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat /caminho/para/public.pem)"
```

Substitua o caminho pelo local real do arquivo.

A chave deve corresponder à chave privada utilizada pelo microsserviço Usuários.

**Não configure a chave privada RSA em Eventos.**

## 3. Verificar as configurações

Execute:

```bash
dotnet user-secrets list
```

As propriedades esperadas são:

| Configuração | Finalidade |
|---|---|
| `Authentication:ApiKey` | Autenticação das chamadas internas |
| `Jwt:PublicKeyPem` | Validação da assinatura dos JWTs |

As configurações `Jwt:Issuer` e `Jwt:Audience` já estão definidas no `appsettings.json`, portanto não precisam ser adicionadas aos User Secrets.

**Atenção:** o comando de listagem exibe os valores das configurações. Não compartilhe sua saída contendo credenciais.

## 4. Executar o microsserviço

Restaure as dependências e execute a aplicação:

```bash
dotnet restore
dotnet run
```

O endereço HTTP de desenvolvimento é:

```text
http://localhost:5297
```

A documentação interativa pode ser acessada em:

```text
http://localhost:5297/scalar/v1
```

Para realizar testes de publicação de eventos, o microsserviço Comercialização também deve estar em execução.

## 5. Solução de problemas

| Problema | Verificação |
|---|---|
| `Jwt:PublicKeyPem` ausente | Conferir o User Secrets do projeto Eventos |
| API Key rejeitada | Conferir se a chave corresponde à utilizada pelo serviço de destino |
| JWT rejeitado | Conferir chave pública, emissor, audiência e expiração |
| Falha de comunicação com Comercialização | Conferir `Services:Comercializacao:BaseUrl` no `appsettings.Development.json` |
| Porta ocupada | Verificar processos utilizando a porta 5297 |

Para consultar todas as configurações exigidas pela aplicação, consulte [Configuracoes.md](../Configuracoes.md).