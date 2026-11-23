# Requisitos de Software

## 1. Introdução

Este documento apresenta os requisitos funcionais do sistema de gerenciamento de eventos presenciais e comercialização de ingressos digitais.

O projeto foi desenvolvido com o objetivo de estudar a arquitetura de microsserviços, utilizando serviços independentes que se comunicam por meio de APIs REST.

A documentação está organizada em requisitos funcionais identificados de RF-01 a RF-24, agrupados por domínio de negócio.

As regras de negócio associadas serão documentadas separadamente em [Regras de Negócio](RegrasNegocio.md).

## 2. Propósito do sistema

O sistema tem como propósito permitir que pequenos organizadores de eventos presenciais publiquem seus eventos, disponibilizem ingressos para compra, acompanhem pedidos e pagamentos e realizem o controle de acesso dos participantes.

A solução busca reduzir a dependência de procedimentos manuais, especialmente nas etapas de comercialização, emissão de ingressos e conferência de entrada.

O projeto contempla o fluxo essencial de gerenciamento de eventos e comercialização de ingressos digitais, sem pretender substituir integralmente plataformas comerciais especializadas.

## 3. Problema identificado

A comercialização manual de ingressos pode envolver atendimento individual, confirmação de pagamentos, emissão de comprovantes e conferência presencial de participantes.

Esses procedimentos podem apresentar dificuldades como:

- Demora no atendimento e na conclusão das compras.
- Dificuldade para acompanhar pagamentos e pedidos.
- Falta de atualização automática da disponibilidade de ingressos.
- Risco de utilização repetida de um mesmo ingresso.
- Dificuldade para recuperar ingressos e consultar compras anteriores.
- Informações dos eventos distribuídas entre diferentes canais de comunicação.

A aplicação propõe centralizar e automatizar parte desses processos, utilizando APIs independentes para cada domínio.

Os problemas apresentados correspondem à análise inicial do domínio e não representam, por si só, resultados de pesquisa de mercado.

## 4. Público-alvo e atores

O público-alvo inicial é composto por pequenos organizadores de eventos presenciais, como apresentações musicais, palestras, espetáculos teatrais e encontros culturais.

Os principais atores identificados são:

| Ator | Descrição |
|---|---|
| Visitante | Pessoa que consulta eventos e ofertas sem necessariamente possuir uma conta de acesso. |
| Comprador | Pessoa que realiza pedidos, acompanha pagamentos e utiliza ingressos adquiridos. |
| Organizador | Usuário autenticado responsável por cadastrar, validar ingressos, publicar e administrar seus eventos e ofertas. |
| Sistema | Representa os processos automatizados, como confirmação de pagamentos, emissão de ingressos e atualização de estados. |

Um mesmo usuário autenticado pode desempenhar diferentes papéis conforme a operação realizada.

Nesse escopo foi definido que o organizador quem valida a entrada do ingresso, no futuro pode ter funcionalidades em que o organizador permita adicionar colaboradores e máquinas que escaneia o ingresso que também desempenha a função de validar o ingresso.

## 5. Requisitos funcionais

Os requisitos funcionais estão agrupados conforme os quatro domínios principais: Gestão de Eventos, Gestão de Usuários, Comercialização e Portaria.

### 5.1. Gestão de Eventos

| Código | Requisito funcional |
|---|---|
| RF-01 | O sistema deve permitir que o organizador cadastre eventos, informando nome, descrição, local, data e horário. |
| RF-02 | O sistema deve permitir que o organizador atualize as informações de seus eventos. |
| RF-03 | O sistema deve permitir que o organizador publique eventos previamente cadastrados, tornando-os acessíveis ao público por meio de um endereço eletrônico próprio e disponibilizando suas ofertas de ingressos para comercialização. |
| RF-04 | O sistema deve disponibilizar ao organizador um endereço eletrônico (URL) para o compartilhamento de cada evento publicado. |
| RF-16 | O sistema deve permitir que usuários autenticados atuem como organizadores de eventos, sem necessidade de cadastro adicional. |
| RF-21 | O sistema deve permitir que organizadores consultem seus próprios eventos filtrando por data agendada e estado de publicação. |
| RF-23 | O sistema deve permitir que visitantes e compradores consultem as informações de um evento publicado através de seu endereço eletrônico. |

### 5.2. Gestão de Usuários

| Código | Requisito funcional |
|---|---|
| RF-13 | O sistema deve permitir que pessoas criem uma conta de acesso, verificando seu e-mail e definindo uma senha. |
| RF-14 | O sistema deve permitir que usuários com conta realizem autenticação. |
| RF-15 | O sistema deve permitir que usuários autenticados consultem e atualizem seus dados cadastrais. |
| RF-22 | O sistema deve permitir que seja cadastrado um usuário sem uma conta de acesso. |

### 5.3. Comercialização

| Código | Requisito funcional |
|---|---|
| RF-05 | O sistema deve permitir que o organizador configure ofertas de ingressos para seus eventos, definindo o nome da oferta, o preço unitário e a quantidade total de ingressos disponibilizados para comercialização. |
| RF-06 | O sistema deve permitir que visitantes e compradores consultem as ofertas de ingressos disponíveis para eventos publicados, incluindo preço e quantidade disponível. |
| RF-07 | O sistema deve permitir que o comprador realize pedidos de compra de ingressos. |
| RF-08 | O sistema deve registrar e acompanhar a situação do pagamento de cada pedido. |
| RF-09 | O sistema deve emitir os ingressos correspondentes após a confirmação do pagamento. |
| RF-10 | O sistema deve permitir que o comprador consulte seus pedidos e ingressos adquiridos. |
| RF-11 | O sistema deve permitir o cancelamento de pedidos elegíveis, conforme as regras de cancelamento estabelecidas. |
| RF-12 | O sistema deve permitir que visitantes realizem compras de ingressos sem a necessidade de criar uma conta de acesso, informando nome, e-mail e telefone. |
| RF-24 | O sistema deve permitir que o organizador atualize o nome, o preço unitário e a quantidade total de ingressos de ofertas associadas aos eventos sob sua responsabilidade. |

### 5.4. Portaria

| Código | Requisito funcional |
|---|---|
| RF-17 | O sistema deve gerar uma credencial de acesso única para cada ingresso emitido após a confirmação do pagamento. |
| RF-18 | O sistema deve permitir que o organizador valide ingressos por meio da leitura do QR Code do ingresso digital. |
| RF-19 | O sistema deve registrar a utilização de um ingresso após a autorização de entrada, impedindo sua reutilização. |
| RF-20 | O sistema deve bloquear as credenciais de acesso associadas a ingressos cancelados. |

## 6. Visão geral dos microsserviços

O sistema é dividido em quatro microsserviços independentes.

| Microsserviço | Responsabilidade |
|---|---|
| Usuários | Cadastro de usuários, gerenciamento de contas e autenticação. |
| Eventos | Cadastro, atualização, publicação e consulta de eventos. |
| Comercialização | Gerenciamento de ofertas, pedidos, pagamentos e emissão de ingressos. |
| Portaria | Gerenciamento de credenciais de acesso e validação de entradas. |

Cada microsserviço possui seu próprio domínio e armazenamento de dados.

A descrição das integrações e das responsabilidades técnicas encontra-se em [Arquitetura](Arquitetura.md).

## 7. Escopo do projeto

O escopo inicial compreende o fluxo essencial de comercialização e utilização de ingressos digitais.

O processo esperado envolve:

1. Cadastro ou identificação de um organizador.
2. Cadastro de um evento.
3. Configuração das ofertas de ingressos.
4. Publicação do evento.
5. Consulta pública dos eventos e das ofertas disponíveis.
6. Criação de um pedido de compra.
7. Processamento e confirmação do pagamento.
8. Emissão dos ingressos digitais.
9. Geração das credenciais de acesso.
10. Validação dos ingressos na entrada do evento.

O desenvolvimento prioriza a aplicação dos conceitos de arquitetura de microsserviços, separação de responsabilidades, comunicação HTTP e consistência das regras de negócio.

Funcionalidades avançadas de gestão de eventos, logística e operação comercial não fazem parte do foco principal desta versão.

## 8. Rastreabilidade e implementação

Cada requisito funcional possui um identificador único, preservado ao longo da documentação.

As regras de negócio relacionadas serão descritas no documento `RegrasNegocio.md`, com referências aos requisitos correspondentes.

A presença de um requisito neste documento não significa, necessariamente, que sua implementação esteja concluída.

O atendimento dos requisitos deve ser verificado por meio da análise do código, dos testes automatizados e dos fluxos efetivamente executados.

## 9. Documentação complementar

- [Regras de Negócio](RegrasNegocio.md) — condições e restrições aplicadas ao domínio.
- [Arquitetura](Arquitetura.md) — estrutura dos microsserviços e suas integrações.
- [Pré-requisitos](PreRequisitos.md) — dependências e preparação do ambiente.
- [Configurações](Configuracoes.md) — referência das configurações utilizadas pelos serviços.
- [Configuração local](ConfiguracaoLocal/) — procedimentos de execução em desenvolvimento.

Este documento deve ser atualizado quando novos requisitos forem identificados ou quando o escopo funcional do sistema for alterado.