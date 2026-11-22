# Configuração local — Microsserviço de Usuários

Este documento descreve os procedimentos necessários para configurar e executar o microsserviço de Usuários em uma máquina de desenvolvimento.

## 1. Pré-requisitos

Antes de continuar:

- Instale o .NET SDK 10.
- Execute a geração das chaves RSA descrita no [README principal](../README.md).
- Verifique se os arquivos `private.pem` e `public.pem` existem na raiz do repositório.

## 2. Configurar User Secrets

Acesse o diretório `Usuario/Api`.

Inicialize o gerenciador de segredos:

```bash
dotnet user-secrets init
```

O comando precisa ser executado apenas uma vez por projeto.

### 2.1. Configurar a API Key

O microsserviço utiliza a propriedade `Authentication:ApiKey` para autenticar requisições internas.

Configure a chave:

```bash
dotnet user-secrets set "Authentication:ApiKey" "123456"
```

O valor `123456` é exclusivamente ilustrativo. Para testes locais mais seguros, gere uma chave aleatória:

```bash
openssl rand -hex 32
```

Utilize o valor gerado nos serviços que compartilham essa credencial.

### 2.2. Configurar JWT

Configure o emissor, a audiência e a duração dos tokens:

```bash
dotnet user-secrets set "Jwt:Issuer" "Usuario"

dotnet user-secrets set "Jwt:Audience" "EventosPresenciais"

dotnet user-secrets set "Jwt:AccessTokenMinutes" "60"
```

### 2.3. Importar as chaves RSA

A partir do diretório que contém o `.csproj`, utilize os caminhos dos arquivos PEM gerados na raiz do repositório.

Nos comandos abaixo, substitua os caminhos indicados pelos caminhos reais dos arquivos:

```bash
dotnet user-secrets set \
  "Jwt:PrivateKeyPem" \
  "$(cat /CAMINHO/DO/REPOSITORIO/private.pem)"
```

```bash
dotnet user-secrets set \
  "Jwt:PublicKeyPem" \
  "$(cat /CAMINHO/DO/REPOSITORIO/public.pem)"
```

Essas propriedades são utilizadas pelo `TokenJwtService` para assinar e validar tokens JWT.

Após a importação, os arquivos PEM não precisam permanecer na pasta do microsserviço.

## 3. Verificar configurações

Execute:

```bash
dotnet user-secrets list
```

As seguintes propriedades devem estar configuradas:

```text
Authentication:ApiKey
Jwt:Issuer
Jwt:Audience
Jwt:AccessTokenMinutes
Jwt:PrivateKeyPem
Jwt:PublicKeyPem
```

**Atenção:** o comando também pode exibir os valores secretos. Não compartilhe esses valores.

## 4. Banco de dados

O microsserviço utiliza SQLite e Entity Framework Core.

A conexão padrão está configurada no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Nome_banco.db"
  }
}
```

O banco será criado ou atualizado pelas migrations durante a inicialização, conforme a configuração da aplicação.

## 5. Executar o microsserviço

Na pasta do projeto `.csproj`, execute:

```bash
dotnet run --environment Development
```

A aplicação carregará os User Secrets e inicializará os serviços configurados.

Se as configurações obrigatórias estiverem ausentes ou inválidas, a inicialização poderá ser interrompida.

## 6. Solução de problemas

**Erro: `Configure Authentication:ApiKey`**

Verifique se a propriedade `Authentication:ApiKey` foi cadastrada no User Secrets do projeto correto.

**Erro: `Configure Jwt:PrivateKeyPem`**

Verifique se a chave privada foi importada corretamente.

**Erro: as chaves RSA não formam o mesmo par**

Gere novamente o par RSA conforme o README principal e atualize as duas propriedades.

**Erro de assinatura JWT inválida**

Verifique se o serviço responsável pela validação está utilizando a chave pública correspondente à chave privada do serviço emissor.