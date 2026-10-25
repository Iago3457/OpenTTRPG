# Arquitetura e Engenharia de Software — OpenTTRPG

Projeto desenvolvido para **Algoritmos e Estruturas de Dados 1 (AED1 - UFSCar)** com separação estrita entre o **Motor Core Genérico em C++17** (agnóstico de sistema de RPG) e a **Camada de Regras e Interface em C#** (implementando **Tormenta20 — Edição Jogo do Ano**).

---

## 1. Princípio Arquitetural Central: Core Agnóstico vs. Regras em C#

A regra de ouro da arquitetura do **OpenTTRPG** é:
1. **Tudo que está no Core (`include/openttrpg/core/` e `src/core/`) é em C++17 e 100% agnóstico de sistema:**
   O Core não conhece nomes como *"Tormenta20"*, *"D&D 5e"*, *"Força"*, *"Ação Padrão"* ou *"Ataque Poderoso"*. Ele fornece os mecanismos universais e as estruturas de dados de AED1 para:
   - Gerenciar prioridades e conversões entre slots de ação (`ActionEconomy` + `PriorityQueue<T>`);
   - Avaliar árvores hierárquicas de concessão e uso de habilidades (`AbilityTree` + `RequirementTree` + `Tree<T>`);
   - Controlar iniciativa seletiva, turnos e rodadas (`InitiativeTracker` + `LinkedList<T>`);
   - Calcular expressões de dados (`DiceEngine`), recursos (`ResourcePool`), dano (`Damage`) e condições (`ConditionTracker`).
2. **Toda regra específica de um sistema de RPG (como Tormenta20 Jogo do Ano) é implementada em C# (`bindings/csharp/`):**
   A camada C# consome a biblioteca compartilhada (`libopenttrpg_core.so` / `openttrpg_core.dll`) via P/Invoke (`OpenTTRPG.cs`) e configura os tipos de ação, regras de conversão e árvores de poderes do sistema desejado (`Tormenta20Rules.cs`).

---

## 2. Estruturas de Dados Manuais de AED1 (`include/openttrpg/data_structures/`)

Todas as estruturas avaliadas na disciplina foram construídas manualmente com ponteiros e alocação dinâmica, respeitando a **Regra dos Cinco (*Rule of Five*)** (destrutor, construtor de cópia, atribuição por cópia, construtor de movimento e atribuição por movimento):

| Estrutura Manual | Arquivo | Onde é aplicada no Motor |
| :--- | :--- | :--- |
| **`LinkedList<T>`** | [`LinkedList.hpp`](../include/openttrpg/data_structures/LinkedList.hpp) | Lista de combatentes do encontro, ordem de iniciativa, recursos por combatente, atributos nomeados, condições ativas e listas de filhos na árvore. |
| **`Queue<T>`** | [`Queue.hpp`](../include/openttrpg/data_structures/Queue.hpp) | Fila dinâmica FIFO (First-In, First-Out) para fluxos sequenciais de combate. |
| **`PriorityQueue<T>`** | [`PriorityQueue.hpp`](../include/openttrpg/data_structures/PriorityQueue.hpp) | Fila de prioridades ordenada com desempate estável (FIFO). Ordena automaticamente as ações enfileiradas no turno de acordo com o peso de prioridade definido pelo sistema (ex.: em T20, `Livre [10] -> Movimento [20] -> Padrão [30]`). |
| **`Tree<T>`** | [`Tree.hpp`](../include/openttrpg/data_structures/Tree.hpp) | Árvore N-ária genérica com ponteiro para o pai e `LinkedList<TreeNode<T>*>` de filhos. Usada duplamente em `AbilityTree` (hierarquia de poderes) e `RequirementTree` (árvore de expressão lógica `AND`/`OR`/`NOT` de pré-requisitos). |

---

## 3. Subsistemas do Core Genérico (`include/openttrpg/core/`)

### 3.1. Iniciativa Seletiva e Turnos (`InitiativeTracker` e `Encounter`)
* **Seleção de Participantes (`rollInitiativeForList`):** Nem todos os personagens cadastrados precisam participar de uma luta. O método recebe uma `LinkedList<EntityId>` (ou array `int[]` via C API/C#) e insere na ordem de iniciativa apenas os combatentes escolhidos.
* **Operações por `EntityId`:** Causar dano, curar, gastar recursos ou aplicar condições exige o `EntityId` explícito do alvo e **nunca avança o turno automaticamente**. O avanço de turno ocorre exclusivamente via `nextTurn()`.

### 3.2. Economia de Ações e Fila de Prioridades (`ActionEconomy`)
* **Desacoplamento entre Prioridade e Conversão:**
  - `defineActionType(id, name, executionPriority, maxPerTurn)`: Registra um tipo de ação arbitrário. Quanto menor `executionPriority`, mais cedo a ação sai da `PriorityQueue`.
  - `addFallback(requestedType, fallbackType)`: Registra explicitamente que, se o slot `requestedType` estiver esgotado (`0`), o combatente pode pagar o custo consumindo `fallbackType`.
  - Isso permite modelar tanto **Tormenta20** (onde `Ação Padrão` pode ser convertida em `Ação de Movimento`, mas não o contrário) quanto sistemas com *Ação Bônus*, *Reação* ou *3 Ações Universais* sem alterar uma linha do Core C++.

### 3.3. Árvore de Habilidades e Pré-requisitos (`AbilityTree` e `RequirementTree`)
* **`AbilityTree` (`Tree<AbilityNode>`):** Cada habilidade registrada pode ter uma habilidade-pai (`parentKey`) na árvore. Para aprender uma habilidade-filha, o `AbilityTree::canLearn` verifica recursivamente na árvore se **todos os ancestrais** já foram desbloqueados pelo combatente.
* **`RequirementTree` (`Tree<RequirementNode>`):** Cada habilidade possui duas árvores de requisitos independentes:
  1. `grantRequirements`: Pré-requisitos para **aprender/desbloquear** a habilidade (ex.: `FOR >= 1 E Nivel >= 6`, ou `INT >= 1 OU CAR >= 1`).
  2. `usageRequirements`: Pré-requisitos para **usar/ativar** a habilidade em um turno de combate (ex.: exigir que a ação `"Agredir (Ataque Padrão)"` ou `"Movimentar-se"` já tenha sido executada neste turno via `ActionUsedInTurn`, ou exigir que uma condição esteja/não esteja ativa via `Not` + `ConditionActive`).

---

## 4. Integração com a Interface Gráfica (Godot Engine + C#)

Para consultar quais métodos C# devem ser chamados em cada tela e botão da interface gráfica (rolagem de dados, barra de iniciativa, fila de prioridades de ações e árvore de habilidades), consulte o documento dedicado:
* **[Guia de Integração de Interface Gráfica (`docs/ui_integration_guide.md`)](ui_integration_guide.md)**
