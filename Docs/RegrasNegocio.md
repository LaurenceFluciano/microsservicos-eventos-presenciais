# Regras de Negócio

## 1. Introdução

Este documento reúne as regras de negócio do sistema de gerenciamento de eventos presenciais e comercialização de ingressos digitais.

As regras de negócio estabelecem as condições, restrições, invariantes e comportamentos que orientam a execução dos requisitos funcionais e preservam a consistência dos respectivos domínios.

Os comportamentos oferecidos pelo sistema são descritos no documento [Requisitos de Software](RequisitosSoftware.md).

## 2. Regras de negócio por domínio

### 2.1. Gestão de Eventos

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-01 | Um evento somente pode ser publicado se possuir pelo menos uma oferta de ingresso válida. | RF-03 |
| RN-02 | Um organizador somente pode modificar eventos sob sua responsabilidade. | RF-02, RF-03 |
| RN-10 | Todo usuário com uma conta de acesso está apto a atuar como organizador de eventos, sem necessidade de cadastro adicional. | RF-01, RF-16 |
| RN-11 | O sistema não deve permitir a alteração do organizador responsável por um evento. | RF-02 |
| RN-12 | Um organizador somente pode consultar informações privadas dos eventos sob sua responsabilidade. | RF-21 |
| RN-18 | Somente eventos publicados podem ter suas informações disponibilizadas para consulta pública. | RF-03, RF-04, RF-23 |

### 2.2. Gestão de Usuários

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-13 | Um usuário pode existir no sistema sem possuir uma conta de acesso. | RF-13, RF-22 |
| RN-14 | Um usuário pode possuir, no máximo, uma conta de acesso. | RF-13, RF-14 |
| RN-15 | A criação de uma conta de acesso exige a verificação da titularidade do endereço de e-mail. | RF-13 |
| RN-16 | O endereço de e-mail deve ser único entre os usuários cadastrados. | RF-13, RF-22 |
| RN-17 | Um usuário autenticado somente pode consultar e atualizar seus próprios dados cadastrais nas operações de gerenciamento pessoal. | RF-15 |

### 2.3. Comercialização

#### 2.3.1. Ofertas de ingressos

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-03 | A quantidade de ingressos comprometidos de uma oferta não pode ultrapassar sua quantidade total disponibilizada. | RF-05, RF-07, RF-24 |
| RN-07 | Somente ofertas vinculadas a eventos publicados podem ser consultadas publicamente. | RF-03, RF-06, RF-23 |
| RN-08 | A quantidade disponível de uma oferta deve corresponder à sua quantidade total disponibilizada, descontando os ingressos associados a pedidos confirmados, em processamento e pendentes cujo prazo de reserva ainda não tenha expirado. | RF-05, RF-06, RF-07, RF-09, RF-24 |
| RN-09 | Quando a quantidade disponível de ingressos de uma oferta atingir zero, o sistema deverá impedir a realização de novos pedidos dessa oferta. | RF-07 |
| RN-19 | Um organizador somente pode configurar, consultar privadamente e atualizar ofertas de ingressos associadas aos eventos sob sua responsabilidade. | RF-05, RF-24 |
| RN-20 | O preço unitário de uma oferta de ingressos deve ser maior ou igual a zero. | RF-05, RF-24 |
| RN-21 | A quantidade total de ingressos de uma oferta deve ser maior que zero. | RF-05, RF-24 |
| RN-26 | A quantidade total de ingressos de uma oferta não poderá ser reduzida para um valor inferior à quantidade de ingressos comprometidos, conforme definido na RN-08. | RF-24 |
| RN-27 | Somente eventos agendados permitem a criação de novas ofertas de ingressos e novos pedidos. | RF-05, RF-07 |
| RN-28 | Ao atingir o horário de início do evento, suas ofertas ativas devem ser encerradas; ofertas esgotadas ou encerradas permanecem inalteradas. | RF-05, RF-07 |
| RN-29 | Somente ofertas de ingressos ativas e vinculadas a eventos publicados podem receber novos pedidos de compra. | RF-07 |

#### 2.3.2. Pedidos e reservas

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-22 | Todo pedido de compra deverá possuir um prazo de pelo menos 30 minutos para a confirmação do pagamento, contado a partir de sua criação. | RF-07 |
| RN-23 | Um pedido pendente cujo prazo de pagamento tenha terminado deverá ser considerado expirado. Pedidos em processamento ou confirmados não devem expirar pelo término do prazo de reserva. | RF-07, RF-08, RF-11 |
| RN-24 | Um comprador somente poderá cancelar seus próprios pedidos enquanto estiverem pendentes de pagamento e dentro do prazo de reserva. | RF-11 |
| RN-25 | O cancelamento ou a expiração de um pedido deverá liberar os ingressos reservados, removendo-os da quantidade comprometida da oferta correspondente, sem alterar automaticamente sua elegibilidade para comercialização. | RF-07, RF-11 |
| RN-30 | Todo pedido deve estar associado a um comprador previamente cadastrado no sistema, independentemente de possuir uma conta de acesso. | RF-07, RF-12, RF-22 |
| RN-31 | A quantidade de ingressos solicitada em um pedido deve ser maior que zero. | RF-07 |
| RN-32 | O preço unitário dos ingressos de um pedido deve corresponder ao preço da oferta no momento de sua criação, não sendo afetado por alterações posteriores. | RF-07, RF-24 |
| RN-33 | Pedidos confirmados, em processamento, cancelados ou expirados não podem ser cancelados diretamente pelo comprador. | RF-11 |

#### 2.3.3. Pagamentos e emissão de ingressos

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-04 | Um pedido somente pode gerar ingressos válidos após a confirmação do pagamento ou, quando gratuito, após a confirmação de sua elegibilidade para emissão. | RF-08, RF-09, RF-17 |
| RN-34 | Um pedido não pode ser cancelado caso possua um pagamento confirmado. | RF-08, RF-11 |
| RN-35 | O cancelamento de um pedido deve invalidar suas cobranças ainda disponíveis e cancelar os pagamentos associados elegíveis, impedindo a realização de pagamentos posteriores. | RF-08, RF-11 |
| RN-36 | Pedidos de ingressos gratuitos não exigem a realização de uma transação financeira para a emissão dos ingressos. | RF-08, RF-09, RF-17 |
| RN-37 | Pagamentos confirmados não podem ser estornados por meio do sistema. | RF-08, RF-11 |

### 2.4. Portaria

| Código | Regra de negócio | Requisitos relacionados |
|---|---|---|
| RN-05 | Uma credencial de acesso pode autorizar uma única entrada no evento. | RF-18, RF-19 |
| RN-38 | A validação dos ingressos e a autorização de entrada são de responsabilidade do organizador do evento. | RF-18, RF-19 |
| RN-39 | Cada ingresso emitido deve possuir uma credencial de acesso única. | RF-17 |

## 3. Formalização das regras de disponibilidade

### 3.1. Quantidade comprometida

A quantidade comprometida de uma oferta representa a soma dos ingressos associados aos pedidos confirmados, em processamento e pendentes com prazo de reserva vigente.

Para uma oferta de ingressos, considere:

- `Qt`: quantidade total disponibilizada.
- `Qf`: quantidade associada a pedidos confirmados.
- `Qe`: quantidade associada a pedidos em processamento.
- `Qr`: quantidade reservada por pedidos pendentes não expirados.
- `Qc`: quantidade total comprometida.
- `Qd`: quantidade disponível.

A quantidade comprometida é determinada por:

```math
Q_c = Q_f + Q_e + Q_r
```

A seguinte condição deve ser preservada:

```math
0 \leq Q_c \leq Q_t
```

**Regras relacionadas:** RN-03 e RN-08.

### 3.2. Quantidade disponível

A quantidade disponível é calculada pela diferença entre a quantidade total disponibilizada e a quantidade comprometida.

```math
Q_d = Q_t - Q_c
```

Consequentemente:

```math
Q_d = Q_t - (Q_f + Q_e + Q_r)
```

Pedidos cancelados e expirados não integram a quantidade comprometida.

**Regras relacionadas:** RN-08 e RN-25.

### 3.3. Criação de pedidos

Considere `Qp` como a quantidade de ingressos solicitada em um novo pedido.

A quantidade solicitada deve satisfazer:

```math
0 < Q_p \leq Q_d
```

Quando a quantidade disponível for igual a zero, nenhuma nova reserva poderá ser realizada para a oferta.

```math
Q_d = 0 \implies \text{novos pedidos não permitidos}
```

**Regras relacionadas:** RN-03, RN-09 e RN-31.

### 3.4. Atualização da quantidade total

Considere `Qt'` como a nova quantidade total disponibilizada de uma oferta.

A atualização somente poderá preservar a consistência do estoque quando:

```math
Q'_t > 0 \quad \land \quad Q'_t \geq Q_c
```

**Regras relacionadas:** RN-03, RN-21 e RN-26.

### 3.5. Valor do pedido

Considere:

- `P`: preço unitário da oferta no momento da criação do pedido.
- `Qp`: quantidade de ingressos solicitada.
- `V`: valor total do pedido.

O valor total é determinado por:

```math
V = P \cdot Q_p
```

O preço unitário deve satisfazer:

```math
P \geq 0
```

O preço unitário utilizado no pedido permanece associado a ele, independentemente de alterações posteriores no preço da oferta.

Quando o valor total for igual a zero, o pedido não exige uma transação financeira para a emissão dos ingressos.

**Regras relacionadas:** RN-20, RN-31, RN-32 e RN-36.

## 4. Condições para cancelamento de pedidos

O cancelamento de um pedido pelo comprador exige o atendimento simultâneo das seguintes condições:

| Condição | Regra relacionada |
|---|---|
| O pedido pertence ao comprador. | RN-24 |
| O pedido está pendente de pagamento. | RN-24, RN-33 |
| O prazo de reserva ainda está vigente. | RN-23, RN-24 |
| O pedido não possui pagamento confirmado. | RN-34 |

A realização do cancelamento produz os seguintes efeitos:

- Alteração da situação do pedido para cancelado.
- Liberação dos ingressos reservados.
- Invalidação das cobranças associadas ainda disponíveis.
- Cancelamento dos pagamentos associados elegíveis.

**Regras relacionadas:** RN-25 e RN-35.

## 5. Relação entre regras de negócio e requisitos funcionais

Os requisitos funcionais descrevem os comportamentos oferecidos pelo sistema, enquanto as regras de negócio estabelecem as condições e restrições aplicadas à execução desses comportamentos.

Uma mesma regra de negócio pode estar relacionada a diferentes requisitos funcionais, assim como um requisito pode depender do atendimento simultâneo de diversas regras.

Por exemplo:

| Requisito | Relação com as regras de negócio |
|---|---|
| RF-03 | A publicação de eventos depende da existência de uma oferta válida, da responsabilidade do organizador e das condições de disponibilização pública estabelecidas nas RN-01, RN-02 e RN-18. |
| RF-07 | A criação de pedidos depende das condições de disponibilidade, reserva, elegibilidade da oferta e identificação do comprador. |
| RF-09 | A emissão dos ingressos depende da confirmação do pagamento ou da elegibilidade de um pedido gratuito, conforme RN-04 e RN-36. |
| RF-11 | O cancelamento de pedidos depende das condições de estado, titularidade, prazo de reserva e situação dos pagamentos associados. |
| RF-19 | O registro da entrada deve impedir a reutilização da credencial, conforme RN-05. |
| RF-24 | A atualização das ofertas deve preservar a disponibilidade e a quantidade comprometida, conforme RN-03, RN-08 e RN-26. |

A identificação de cada regra de negócio é preservada ao longo da documentação e utilizada na rastreabilidade com os requisitos funcionais, as operações das APIs REST e os casos de teste.