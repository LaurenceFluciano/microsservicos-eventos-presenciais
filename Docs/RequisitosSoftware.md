# Requisitos de Software

## 1. Introdução

Este documento apresenta os requisitos funcionais do sistema de gerenciamento de eventos presenciais e comercialização de ingressos digitais.

Os requisitos estão organizados por domínio de negócio e descrevem os comportamentos oferecidos pelo sistema aos seus atores e aos processos automatizados.

Cada requisito funcional possui um identificador único, utilizado para estabelecer a rastreabilidade entre as funcionalidades, as regras de negócio e as operações disponibilizadas pelas APIs REST dos microsserviços.

As condições e restrições aplicadas à execução dos requisitos funcionais são descritas no documento [Regras de Negócio](RegrasNegocio.md).

## 2. Atores do sistema

| Ator | Descrição |
|---|---|
| Visitante | Pessoa que consulta eventos publicados e ofertas de ingressos, sem necessariamente possuir uma conta de acesso. |
| Comprador | Pessoa cadastrada que realiza pedidos de compra, acompanha pagamentos e consulta os ingressos adquiridos, independentemente de possuir uma conta de acesso. |
| Organizador | Usuário autenticado responsável pelo cadastro, atualização, publicação e gerenciamento de seus eventos e ofertas de ingressos, bem como pela validação das entradas. |
| Sistema | Representa os processos automatizados, incluindo atualização de estados, processamento de pagamentos, emissão de ingressos e geração de credenciais de acesso. |

Um mesmo usuário pode desempenhar diferentes papéis conforme a operação realizada.

Todo usuário que possui uma conta de acesso pode atuar como organizador de eventos, sem necessidade de cadastro adicional.

## 3. Requisitos funcionais

### 3.1. Gestão de Eventos

| Código | Requisito funcional |
|---|---|
| RF-01 | O sistema deve permitir que o organizador cadastre eventos, informando nome, descrição, local, data e horário de início e término. |
| RF-02 | O sistema deve permitir que o organizador atualize as informações dos eventos sob sua responsabilidade. |
| RF-03 | O sistema deve permitir que o organizador publique eventos previamente cadastrados, tornando suas informações e ofertas de ingressos disponíveis para consulta pública. |
| RF-04 | O sistema deve disponibilizar ao organizador um endereço eletrônico próprio para o compartilhamento de cada evento publicado. |
| RF-16 | O sistema deve permitir que usuários autenticados atuem como organizadores de eventos, sem necessidade de cadastro adicional. |
| RF-21 | O sistema deve permitir que organizadores consultem individualmente seus eventos e obtenham uma listagem ordenada por data agendada, com possibilidade de filtragem pelo estado de publicação. |
| RF-23 | O sistema deve permitir que visitantes e compradores consultem a listagem de eventos publicados e obtenham suas informações por meio de um identificador ou endereço eletrônico público. |

### 3.2. Gestão de Usuários

| Código | Requisito funcional |
|---|---|
| RF-13 | O sistema deve permitir que pessoas criem uma conta de acesso mediante cadastro de seus dados, definição de senha e verificação da titularidade do endereço de e-mail. |
| RF-14 | O sistema deve permitir que usuários com uma conta de acesso realizem autenticação. |
| RF-15 | O sistema deve permitir que usuários autenticados consultem seus próprios dados cadastrais e atualizem seu nome e telefone. |
| RF-22 | O sistema deve permitir o cadastro de usuários sem a criação de uma conta de acesso. |

### 3.3. Comercialização

| Código | Requisito funcional |
|---|---|
| RF-05 | O sistema deve permitir que o organizador configure e consulte as ofertas de ingressos associadas aos eventos sob sua responsabilidade, definindo nome, preço unitário e quantidade total disponibilizada para comercialização. |
| RF-06 | O sistema deve permitir que visitantes e compradores consultem as ofertas de ingressos de eventos publicados, incluindo preço e quantidade disponível. |
| RF-07 | O sistema deve permitir que compradores realizem pedidos de ingressos, selecionando uma oferta e informando a quantidade desejada, reservando os ingressos correspondentes durante o prazo de pagamento. |
| RF-08 | O sistema deve permitir o processamento de pagamentos dos pedidos, disponibilizando os meios necessários para sua realização e registrando as alterações de situação comunicadas pelo provedor de pagamentos. |
| RF-09 | O sistema deve emitir os ingressos correspondentes aos pedidos após a confirmação do pagamento ou, no caso de pedidos gratuitos, após a confirmação de sua elegibilidade para emissão. |
| RF-10 | O sistema deve permitir a consulta individual de pedidos e ingressos adquiridos, incluindo suas informações e o QR Code associado ao ingresso digital. |
| RF-11 | O sistema deve permitir o cancelamento de pedidos elegíveis, liberando os ingressos reservados e invalidando as cobranças associadas conforme as regras de negócio. |
| RF-12 | O sistema deve permitir que visitantes realizem pedidos de ingressos sem a necessidade de criar uma conta de acesso, mediante identificação de um cadastro de usuário pelo endereço de e-mail. |
| RF-24 | O sistema deve permitir que o organizador atualize o nome, o preço unitário e a quantidade total de ingressos das ofertas associadas aos eventos sob sua responsabilidade. |

### 3.4. Portaria

| Código | Requisito funcional |
|---|---|
| RF-17 | O sistema deve gerar uma credencial de acesso única para cada ingresso emitido a partir de um pedido confirmado, independentemente de sua natureza gratuita ou paga. |
| RF-18 | O sistema deve permitir que o organizador valide ingressos digitais mediante o código de acesso representado pelo QR Code do ingresso. |
| RF-19 | O sistema deve registrar a utilização da credencial de acesso após a autorização de entrada, impedindo sua reutilização. |

## 4. Organização dos requisitos por domínio

| Domínio | Microsserviço responsável | Requisitos funcionais |
|---|---|---|
| Gestão de Eventos | Eventos | RF-01, RF-02, RF-03, RF-04, RF-16, RF-21, RF-23 |
| Gestão de Usuários | Usuários | RF-13, RF-14, RF-15, RF-22 |
| Comercialização | Comercialização | RF-05, RF-06, RF-07, RF-08, RF-09, RF-10, RF-11, RF-12, RF-24 |
| Portaria | Portaria | RF-17, RF-18, RF-19 |

A atribuição de um requisito a determinado microsserviço representa sua responsabilidade principal sobre a funcionalidade.

A execução de um requisito pode envolver a participação de outros microsserviços por meio de consultas, solicitações de alteração de dados e notificações assíncronas.

## 5. Rastreabilidade

Os identificadores dos requisitos funcionais são utilizados para estabelecer relações entre:

- Requisitos funcionais.
- Regras de negócio.
- Operações das APIs REST.
- Integrações entre microsserviços.
- Casos de teste.

A identificação dos requisitos é independente de sua posição neste documento, permitindo sua organização por domínio sem alterar as referências utilizadas na documentação do sistema.