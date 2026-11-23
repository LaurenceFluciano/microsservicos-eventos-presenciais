# Pendências temporárias — Eventos Presenciais

> **Documento provisório.** Registra funcionalidades que ainda precisam ser implementadas e ajustes feitos na especificação. Não substitui `Docs/RequisitosSoftware.md`.

## Requisitos funcionais pendentes

| Código | Requisito funcional | Situação |
|---|---|---|
| **RF-11** | O sistema deve permitir o cancelamento de pedidos elegíveis, conforme as regras de cancelamento estabelecidas. | **Pendente de implementação** |
| **RF-12** | O sistema deve permitir que visitantes realizem compras de ingressos sem a necessidade de criar uma conta de acesso, informando nome, e-mail e telefone. | **Pendente de implementação** |

### Checklist de implementação

- [ ] **RF-11 — Cancelamento de pedidos:** implementar o cancelamento de pedidos elegíveis, observando as regras de cancelamento do domínio; verificar liberação das reservas e atualização dos estados aplicáveis.
- [ ] **RF-12 — Compra sem conta:** permitir o pedido por visitante com nome, e-mail e telefone, sem exigir autenticação ou conta de acesso; verificar o vínculo e a recuperação dos pedidos conforme o escopo definido.
- [ ] Adicionar ou atualizar testes dos dois fluxos.
- [ ] Revisar a documentação e marcar as pendências como concluídas somente após validação.

## Alterações recentes nos requisitos

### Papel do organizador na Portaria

No escopo **atual**, o próprio **organizador do evento** é responsável pela validação dos ingressos na entrada. Não há papel obrigatório de operador de portaria distinto.

**RF-18 — redação atualizada:**

> O sistema deve permitir que o organizador valide ingressos por meio da leitura do QR Code do ingresso digital.

**Possibilidade futura, fora do escopo atual:** permitir que o organizador delegue a validação a colaboradores ou dispositivos de leitura autorizados.

### Ator Organizador — redação atualizada

> Usuário autenticado responsável por cadastrar, validar ingressos, publicar e administrar seus eventos e ofertas.

## Observações

- Este arquivo lista **expressamente como pendentes apenas RF-11 e RF-12**, conforme a revisão atual do projeto.
- A implementação efetiva de outras funcionalidades não foi auditada neste documento.
- Manter os identificadores RF originais nos documentos de requisitos e regras de negócio.
