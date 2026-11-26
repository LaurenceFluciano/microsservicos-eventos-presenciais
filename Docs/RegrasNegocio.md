# Regras de Negócio

## 1. Introdução

Este documento reúne as regras de negócio do sistema de gerenciamento de eventos presenciais e comercialização de ingressos digitais. As regras estabelecem condições, restrições e comportamentos que orientam a execução dos requisitos funcionais.

Para os comportamentos solicitados ao sistema, consulte [Requisitos de Software](RequisitosSoftware.md). As decisões de comunicação entre serviços são descritas em [Arquitetura](Arquitetura.md).

## 2. Regras de negócio por domínio

### 2.1. Gestão de Eventos

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| **RN-01** | Um evento somente pode ser publicado se possuir pelo menos uma oferta de ingresso válida. | RF-03 |
| **RN-02** | Um organizador somente pode modificar eventos sob sua responsabilidade. | RF-02, RF-03, RF-16 |
| **RN-10** | Todo usuário com uma conta cadastrada está apto a atuar como organizador de eventos, sem necessidade de cadastro adicional. | RF-16 |
| **RN-11** | O sistema não deve permitir a alteração do organizador responsável por um evento. | RF-02 |
| **RN-12** | Um organizador somente pode consultar informações privadas de eventos sob sua responsabilidade. | RF-21 |
| **RN-18** | Somente eventos publicados podem ter suas informações disponibilizadas para consulta pública. | RF-03, RF-21, RF-23 |

### 2.2. Gestão de Usuários

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| **RN-13** | Um usuário pode existir no sistema sem possuir uma conta de acesso. | RF-13, RF-22 |
| **RN-14** | Um usuário pode possuir, no máximo, uma conta de acesso. | RF-13, RF-14 |
| **RN-15** | A criação de uma conta de acesso exige a verificação da titularidade do e-mail. | RF-13 |
| **RN-16** | O e-mail deve ser único entre os usuários. | RF-13, RF-15, RF-22 |
| **RN-17** | O usuário só pode consultar seus próprios dados cadastrais. | RF-15 |

### 2.3. Comercialização

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| **RN-03** | A quantidade de ingressos comprometidos não pode ultrapassar a quantidade disponibilizada. | RF-05, RF-07 |
| **RN-04** | Um pedido somente pode gerar ingressos válidos após a confirmação do pagamento. | RF-08, RF-09 |
| **RN-07** | Somente ofertas vinculadas a eventos publicados podem ser consultadas publicamente. | RF-04, RF-06, RF-23 |
| **RN-08** | A quantidade disponível de uma oferta deve considerar os ingressos vendidos e as reservas ativas. | RF-07, RF-09 |
| **RN-09** | Quando a quantidade disponível de ingressos de uma oferta atingir zero, o sistema deverá impedir automaticamente a realização de novas compras dessa oferta. | RF-07 |
| **RN-19** | Um organizador somente pode configurar ofertas de ingressos para eventos sob sua responsabilidade. | RF-05 |
| **RN-20** | O preço unitário de uma oferta de ingressos deve ser maior ou igual a zero. | RF-05 |
| **RN-21** | A quantidade total de ingressos de uma oferta deve ser maior que zero. | RF-05 |
| **RN-22** | Todo pedido de compra deverá possuir um prazo de pelo menos 30 minutos para a confirmação do pagamento, contado a partir de sua criação. | RF-07 |
| **RN-23** | Um pedido cujo prazo de pagamento tenha expirado sem a confirmação do pagamento deverá ser considerado expirado. | RF-07, RF-11 |
| **RN-24** | Um comprador somente poderá cancelar seus próprios pedidos enquanto estiverem pendentes de pagamento. | RF-07, RF-11 |
| **RN-25** | O cancelamento ou a expiração de um pedido deverá liberar os ingressos reservados, restabelecendo sua disponibilidade na oferta correspondente. | RF-07, RF-11 |
| **RN-26** | A quantidade total de ingressos de uma oferta não poderá ser reduzida para um valor inferior à soma dos ingressos vendidos e das reservas ativas. | RF-24 |
| **RN-27** | Somente eventos agendados permitem a criação de novas ofertas de ingressos e novos pedidos. | RF-05, RF-07 |
| **RN-28** | Ao atingir o horário de início do evento, suas ofertas ativas devem ser encerradas; ofertas esgotadas ou encerradas permanecem inalteradas. | RF-05, RF-07 |

### 2.4. Portaria

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| **RN-05** | Uma credencial de acesso pode autorizar uma única entrada no evento. | RF-19 |
| **RN-06** | Credenciais associadas a ingressos cancelados não podem autorizar entrada. | RF-20 |

## 3. Responsabilidades dos atores

No escopo atual do sistema, a validação dos ingressos é realizada pelo **organizador responsável pelo evento**. Não existe um papel independente de operador de portaria exigido nesta versão.

A possibilidade de delegar a validação a colaboradores ou dispositivos autorizados constitui uma evolução futura e não deve ser tratada como funcionalidade já contemplada.

## 4. Relação entre regras e requisitos

As regras de negócio complementam os requisitos funcionais: um requisito descreve o comportamento oferecido pelo sistema, enquanto a regra restringe as condições sob as quais esse comportamento pode ocorrer.

Por exemplo:

- **RF-03** descreve a publicação de eventos; **RN-01** determina a condição para publicar.
- **RF-07** descreve a realização de pedidos; **RN-03**, **RN-08**, **RN-09** e **RN-22** impõem limites de disponibilidade e prazo.
- **RF-19** descreve o registro da entrada; **RN-05** impede a reutilização da credencial.
- **RF-24** descreve a atualização de ofertas; **RN-26** impede a redução do estoque abaixo do comprometido.

