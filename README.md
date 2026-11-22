# Microsserviço de Eventos Presenciais

Um projeto desenvolvido para o estudo da arquitetura de microsserviços. O sistema permite que pequenos organizadores publiquem eventos e comercializem ingressos para seus compradores.

## Arquitetura

O sistema é composto por quatro microsserviços independentes:

- **Usuários:** gerenciamento de contas, autenticação e informações dos usuários.
- **Eventos:** gerenciamento e publicação de eventos presenciais.
- **Comercialização:** gerenciamento de ofertas, pedidos e ingressos.
- **Portaria:** controle e validação de ingressos para acesso aos eventos.

## Pré-requisitos

Para executar o projeto localmente, é necessário ter instalado:

- .NET SDK 10
- OpenSSL
- Git

Cada microsserviço utiliza suas próprias configurações e dependências, documentadas individualmente.

## 1. Configuração inicial do projeto

Clone o repositório e acesse sua pasta principal.

```bash
git clone <URL_DO_REPOSITORIO>
cd microsservicos-eventos-presenciais
```

### 1.1. Gerar as chaves RSA

O sistema utiliza JWTs assinados com o algoritmo RS256 para autenticar usuários.

É necessário gerar um par de chaves RSA para o ambiente de desenvolvimento.

Na raiz do repositório, execute:

```bash
openssl genpkey \
  -algorithm RSA \
  -pkeyopt rsa_keygen_bits:2048 \
  -out private.pem
```

Em seguida, gere a chave pública correspondente:

```bash
openssl pkey \
  -in private.pem \
  -pubout \
  -out public.pem
```

Serão gerados os seguintes arquivos:

```text
microsservicos-eventos-presenciais/
├── private.pem
├── public.pem
├── README.md
├── Docs/
└── ...
```

**Distribuição das chaves:**

- `private.pem`: utilizada exclusivamente pelo microsserviço de Usuários para assinar JWTs.
- `public.pem`: utilizada pelos microsserviços que precisam validar os JWTs.

As chaves devem ser armazenadas nas configurações locais de cada microsserviço que necessite delas.


## 2. Configuração dos microsserviços

Cada microsserviço possui um documento próprio com instruções para configuração e execução em ambiente de desenvolvimento.

| Microsserviço | Documentação |
|---|---|
| Usuários | [Configurar Usuários](Docs/UsuarioConfiguracoes.md) |
| Eventos | [Configurar Eventos](Docs/EventosConfiguracoes.md) |
| Comercialização | [Configurar Comercialização](Docs/ComercializacaoConfiguracoes.md) |
| Portaria | [Configurar Portaria](Docs/PortariaConfiguracoes.md) |

## 3. Execução local

Para executar o sistema na sua máquina:

1. Instale os pré-requisitos.
2. Gere as chaves RSA conforme a seção 1.1.
3. Consulte a documentação de cada microsserviço na seção 2.
4. Configure os User Secrets e demais parâmetros necessários.
5. Execute cada microsserviço seguindo as instruções do respectivo documento.

Cada aplicação deve ser iniciada conforme sua configuração local, utilizando o ambiente `Development`.

Para testar a comunicação entre microsserviços, todos os serviços envolvidos devem estar em execução e utilizar endereços e credenciais compatíveis.

## Segurança

Este guia descreve exclusivamente a configuração de um ambiente local de desenvolvimento.

Não utilize as credenciais de desenvolvimento em produção. Em ambientes de produção, configure o armazenamento seguro dos segredos, a distribuição das chaves e os mecanismos de autenticação conforme os requisitos de segurança da implantação.