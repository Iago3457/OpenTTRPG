# Arquitetura e Representação Visual do Sistema — OpenTTRPG

Este documento apresenta a engenharia de software e a **representação visual detalhada** de cada subsistema do **Core Genérico em C++17**, das **Estruturas de Dados Manuais de AED1** e do **Pacote de Regras de Tormenta20 (Jogo do Ano) em C#**.

---

## 1. Visão Macro: Arquitetura em 4 Camadas e Fronteira Nativa (P/Invoke)

O **OpenTTRPG** separa estritamente **mecanismos universais de RPG** (em C++17) de **regras específicas de um sistema** (em C#):

```mermaid
flowchart TD
    subgraph Camada4 ["4. Camada de Aplicação e Regras do Sistema (C# / .NET 10)"]
        UI["Interface Gráfica (Godot Engine 4) / CLIController.cs"]
        T20Act["Tormenta20ActionRules (Ações e Prioridades T20)"]
        T20Tree["Tormenta20AbilityTreeRules (Árvore de Poderes T20)"]
        Session["EncounterSession (Wrapper Gerenciado IDisposable)"]
    end

    subgraph Camada3 ["3. Fronteira C ABI (include/openttrpg/api/openttrpg_api.h)"]
        CABI["Funções C Puras (extern 'C' / cdecl)\nStructs Blittable (int32_t) + ottrpg_last_error()"]
    end

    subgraph Camada2 ["2. Motor Core Genérico em C++17 (100% Agnóstico de Sistema)"]
        Enc["Encounter (Fachada Central do Combate)"]
        Init["InitiativeTracker (Iniciativa Seletiva e Turnos)"]
        ActEcon["ActionEconomy (Slots, Fallbacks e Fila do Turno)"]
        AbTree["AbilityTree & RequirementTree (Concessão e Uso)"]
        Comb["Combatant (Recursos, Status, Atributos, Histórico)"]
        DmgCond["Damage Pipeline & ConditionTracker"]
        Dice["DiceEngine (Parser de Fórmulas e RNG)"]
    end

    subgraph Camada1 ["1. Estruturas de Dados Manuais de AED1 (C++17 Puro)"]
        LL["LinkedList&lt;T&gt; (Lista Duplamente/Simplesmente Encadeada)"]
        Q["Queue&lt;T&gt; (Fila FIFO)"]
        PQ["PriorityQueue&lt;T&gt; (Fila de Prioridades Estável)"]
        TR["Tree&lt;T&gt; (Árvore N-ária Genérica)"]
    end

    UI --> T20Act
    UI --> T20Tree
    UI --> Session
    T20Act --> Session
    T20Tree --> Session
    Session -- "P/Invoke (DllImport libopenttrpg_core)" --> CABI
    CABI --> Enc

    Enc --> Init
    Enc --> ActEcon
    Enc --> AbTree
    Enc --> Comb
    Enc --> Dice
    Comb --> DmgCond

    Init --> LL
    Comb --> LL
    DmgCond --> LL
    ActEcon --> PQ
    ActEcon --> LL
    AbTree --> TR
    AbTree --> LL
```

---

## 2. Representação Visual das Estruturas de Dados de AED1 (`include/openttrpg/data_structures/`)

Todas as 4 estruturas foram implementadas manualmente do zero com ponteiros (`new` / `delete`) e obedecem à **Regra dos Cinco (*Rule of Five*)**.

### 2.1. `LinkedList<T>` e `Queue<T>`
```mermaid
flowchart LR
    subgraph LinkedList ["LinkedList&lt;T&gt; (Acesso indexado, find_if, remove_at, insert_at)"]
        H["head_"] --> N0["Node 0\n[data | next]"]
        N0 --> N1["Node 1\n[data | next]"]
        N1 --> N2["Node 2\n[data | nullptr]"]
        T["tail_"] --> N2
    end

    subgraph Queue ["Queue&lt;T&gt; (FIFO: First-In, First-Out)"]
        F["front_ (dequeue)"] --> Q0["Node A"]
        Q0 --> Q1["Node B"]
        Q1 --> Q2["Node C"]
        R["rear_ (enqueue)"] --> Q2
    end
```

### 2.2. `PriorityQueue<T>` (Fila de Prioridades Ordenada Estável)
Elementos com **menor valor numérico de `priority`** saem primeiro na frente da fila. Em caso de **empate de prioridade**, preserva-se a ordem exata de chegada (**estabilidade FIFO**):

```mermaid
flowchart LR
    HEAD["head_ (pop / front)"] --> P1["[Prio 10 | 1º Livre]\nAtaque Especial"]
    P1 --> P2["[Prio 10 | 2º Livre]\nGrito de Guerra"]
    P2 --> P3["[Prio 20 | Movimento]\nDeslocar 9m"]
    P3 --> P4["[Prio 30 | Padrão]\nAgredir"]
    P4 --> NULL["nullptr"]
```
* **Inserção (`push(value, priority)`)**: Percorre a lista enquanto `curr->priority <= priority` e insere o novo nó logo após o último elemento de mesma prioridade ($O(N)$), garantindo estabilidade sem reordenar elementos antigos.
* **Remoção seletiva (`remove_if`)**: Permite cancelar uma ação específica no meio da fila (usado quando o jogador desfaz uma ação agendada no turno).

### 2.3. `Tree<T>` (Árvore N-ária Genérica)
Cada `TreeNode<T>` possui um ponteiro para seu `parent` (permitindo subir até a raiz em $O(h)$ para validar ancestralidade) e uma `LinkedList<TreeNode<T>*> children` (permitindo um número ilimitado de filhos por nó):

```mermaid
flowchart TD
    Root["root_ (TreeNode&lt;T&gt;)\nparent = nullptr"] --> C1["Filho 1 (TreeNode&lt;T&gt;)\nparent = root_"]
    Root --> C2["Filho 2 (TreeNode&lt;T&gt;)\nparent = root_"]
    Root --> C3["Filho 3 (TreeNode&lt;T&gt;)\nparent = root_"]
    C1 --> GC1["Neto 1.1\nparent = Filho 1"]
    C1 --> GC2["Neto 1.2\nparent = Filho 1"]
    C2 --> GC3["Neto 2.1\nparent = Filho 2"]
```

---

## 3. Como Cada Parte do Core C++ Funciona (Representação Visual Detalhada)

### 3.1. `InitiativeTracker` & `Encounter` — Seleção de Participantes e Ciclo de Turnos
No Core, **cadastrar um combatente não o força a entrar na lista de iniciativa**. O método `rollInitiativeForList` recebe apenas os IDs escolhidos para participar daquele combate, rola a iniciativa e insere cada um na posição ordenada correta:

```mermaid
flowchart TD
    A["Combatentes Cadastrados no Encounter\n(Ex: ID 1 = Valeros, ID 2 = Sillith, ID 3 = NPC Fora da Luta)"] --> B["Seleção de Participantes\nrollInitiativeForList([ID 1, ID 2], 1d20)"]
    B --> C["Para cada ID selecionado:\nTotal = 1d20 + initiativeModifier"]
    C --> D["Inserção Ordenada no InitiativeTracker\nCritérios de Desempate:\n1º Maior Total\n2º Maior Modificador\n3º Maior TieBreaker\n4º Menor EntityId"]
    D --> E["startCombat()\nround_ = 1 | currentIndex_ = 1º Ativo"]
    E --> F["Turno Atual (currentTurn)\nReseta ActionEconomy e Histórico do Turno\nTick de Condições: StartOfTurn"]
    F --> G["Ações durante o turno (Dano, Cura, Habilidades)\nOperam por EntityId do Alvo\n(NÃO avançam o turno!)"]
    G --> H["nextTurn() explícito\nTick de Condições do Ator: EndOfTurn\nAvança cursor pulando isDefeated()"]
    H -- "Cursor voltou ao início da fila" --> I["Nova Rodada (round_++)\nTick Global: RoundStart"]
    I --> F
    H -- "Próximo da mesma rodada" --> F
```

---

### 3.2. `ActionEconomy` — Slots de Ação, Conversão Direcional (`Fallback`) e Fila de Prioridades
O `ActionEconomy` é **100% agnóstico de sistema**: ele não sabe o que é "Ação Padrão" ou "Ação de Movimento". Ele gerencia uma lista de `ActionSlot`s, uma lista de regras direcionais `ActionFallbackRule` (`requestedType -> fallbackType`) e uma `PriorityQueue<QueuedAction>`:

```mermaid
flowchart TD
    Start["enqueueAction(requiredType, name, targetId)"] --> CheckDirect{"Slot[requiredType]\ntem saldo > 0\nou é ilimitado (-1)?"}

    CheckDirect -- "SIM" --> SpendDirect["Consome 1 de Slot[requiredType]\nconsumedType = requiredType"]
    CheckDirect -- "NÃO (Saldo == 0)" --> CheckFallback{"Existe regra em fallbacks_\nonde requested == requiredType\ne Slot[fallbackType] tem saldo?"}

    CheckFallback -- "SIM (Conversão Permitida)" --> SpendFallback["Consome 1 de Slot[fallbackType]\nconsumedType = fallbackType"]
    CheckFallback -- "NÃO" --> Blocked["Retorna 0 (Ação Bloqueada!\nNão há saldo nem fallback válido)"]

    SpendDirect --> PushPQ["Insere QueuedAction na PriorityQueue\nusando a prioridade de requiredType"]
    SpendFallback --> PushPQ

    PushPQ --> Record["Registra o nome da ação em\nCombatant::turnActionHistory_"]
    Record --> Options{"O que acontece depois?"}

    Options -- "popNextAction()" --> Pop["Remove e executa na ordem de\nmenor executionPriority"]
    Options -- "cancelQueuedAction(actionId, refund=true)" --> Cancel["Remove da PriorityQueue e\ndevolve +1 ao Slot[consumedType]"]
```

> **Detalhe Importante (Reembolso Exato):** Como cada `QueuedAction` guarda tanto o `requiredType` (que define a prioridade na fila) quanto o `consumedType` (qual slot realmente pagou a conta), se o jogador cancelar uma ação que usou *fallback*, o Core devolve o ponto exatamente para o slot que foi gasto!

---

### 3.3. `AbilityTree` & `RequirementTree` — Árvore de Habilidades + Árvore Lógica de Pré-requisitos
Este subsistema combina **duas árvores (`Tree<T>`) trabalhando em conjunto** para validar qualquer regra de concessão ou uso de habilidade:

1. **`AbilityTree` (`Tree<AbilityNode>`)**: Modela a hierarquia entre habilidades (qual poder é filho de qual poder).
2. **`RequirementTree` (`Tree<RequirementNode>`)**: Dentro de cada `AbilityNode`, existem **duas** árvores de expressão lógica avaliadas em **pós-ordem** (filhos avaliados antes do pai):
   - **`grantRequirements`**: Avaliada em `canLearn()` / `learn()` para saber se o personagem pode **adquirir/desbloquear** a habilidade.
   - **`usageRequirements`**: Avaliada em `canUse()` durante o combate para saber se o personagem pode **ativar** a habilidade naquele momento do turno.

```mermaid
flowchart TD
    subgraph ValidacaoCompleta ["Fluxo de Validação: AbilityTree + RequirementTree"]
        CallLearn["canLearn(combatant, abilityKey)"] --> FindNode["Busca AbilityNode na AbilityTree"]
        FindNode --> WalkUp["1. Sobe pelos ponteiros 'parent' na AbilityTree\naté a raiz verificando se todos os\nancestrais estão em combatant.unlockedAbilities_"]
        WalkUp --> EvalGrant["2. Avalia grantRequirements.isSatisfied(combatant)\nem Pós-Ordem (Filhos -> Pai)"]
        EvalGrant --> ResultLearn{"Ancestrais OK\nE grantRequirements OK?"}
        ResultLearn -- "SIM" --> CanLearnTrue["Retorna true (Disponível!)"]
        ResultLearn -- "NÃO" --> CanLearnFalse["Retorna false + Lista de motivos\npendentes em outUnmetReasons"]
    end

    subgraph TiposDeNos ["Tipos de Nós da RequirementTree (RequirementKind)"]
        Logic["Nós Lógicos (Ramos Internos)\n• AllOf (AND: todos os filhos verdadeiros)\n• AnyOf (OR: >= 1 filho verdadeiro)\n• Not (NOT: nenhum filho verdadeiro)\n• AtLeastN (>= N filhos verdadeiros)"]
        Leaves["Nós Folha (Predicados no Combatant)\n• AbilityUnlocked (possui habilidade 'key')\n• StatAtLeast (stat 'key' >= threshold)\n• ConditionActive (condição 'key' ativa)\n• ActionUsedInTurn (usou ação 'key' neste turno)\n• ResourceAtLeast (recurso ID >= threshold)"]
        Logic --> Leaves
    end
```

---

### 3.4. `ResourcePool`, Pipeline de `Damage` e `ConditionTracker`

#### Pipeline de Dano e Absorção por Recurso Temporário (`Damage.cpp` + `Resource.cpp`)
Quando `Encounter::dealDamage(targetId, rawAmount, damageType)` é chamado, o dano passa por um pipeline determinístico antes de tocar nos pontos de vida:

```mermaid
flowchart LR
    Raw["Dano Bruto\n(rawAmount, type)"] --> Imm{"Modificador\n== Immune?"}
    Imm -- "SIM" --> Zero["Dano Final = 0"]
    Imm -- "NÃO" --> Order{"CalculationOrder\ndo Combatant"}

    Order -- "ReductionFirst (Padrão)" --> Step1A["1. Subtrai RD\n(globalFlatReduction + RD do tipo)"]
    Step1A --> Step2A["2. Aplica Multiplicador\n• Vulnerable: × 2\n• Resistant: ÷ 2"]

    Order -- "MultiplierFirst" --> Step1B["1. Aplica Multiplicador\n(× 2 ou ÷ 2)"]
    Step1B --> Step2B["2. Subtrai RD Fixa"]

    Step2A --> Temp["ResourcePool::subtract(finalAmount)"]
    Step2B --> Temp
    Temp --> Absorb["1º Desconta de temporary_ (PV Temp)\n2º O excedente desconta de current_\n3º Respeita limite min_ (0)"]
    Absorb --> Out["Retorna DamageResult\n{ finalAmount, absorbedByTemp, depleted }"]
```

#### Ciclo de Vida e Empilhamento de Condições (`ConditionTracker`)
```mermaid
stateDiagram-v2
    [*] --> RecebeCondicao: add(condition, StackPolicy)
    RecebeCondicao --> RefreshDuration: Policy = RefreshDuration\n(Mantém maior duração e maior intensidade)
    RecebeCondicao --> StackIntensity: Policy = StackIntensity\n(Soma intensidades e renova duração)
    RecebeCondicao --> KeepHighest: Policy = KeepHighest\n(Substitui só se intensidade for maior)
    RecebeCondicao --> Independent: Policy = Independent\n(Adiciona instância separada na LinkedList)

    RefreshDuration --> AtivaNoCombatente
    StackIntensity --> AtivaNoCombatente
    KeepHighest --> AtivaNoCombatente
    Independent --> AtivaNoCombatente

    AtivaNoCombatente --> TickTurno: tick(StartOfTurn / EndOfTurn / RoundStart)
    TickTurno --> AtivaNoCombatente: durationTurns > 0 (--durationTurns)
    TickTurno --> Removida: durationTurns chega a 0 (remove_if)
    Removida --> [*]
```

---

### 3.5. `DiceEngine` — Parser e Avaliador de Expressões de Dados
O `DiceEngine` processa fórmulas complexas sem depender de regras fixas de um único RPG:

```mermaid
flowchart LR
    Expr["Expressão String\nEx: '2d20kh1+5' ou '4d6!dl1-1'"] --> Parser["Regex / Tokenizador\n• Quantidade (N)\n• Faces (dS)\n• Explosivo (!)\n• Seletor (kh / kl / dh / dl)\n• Modificador (+/- M)"]
    Parser --> Roll["Rola N dados no std::mt19937_64\n(Repete rolagem se '!' e cair no valor máximo)"]
    Roll --> Filter["Ordena cópia e marca 'kept = false'\nnos dados descartados por kh/kl/dh/dl"]
    Filter --> Sum["Soma dados mantidos + Modificador\nConta NaturalMax (críticos) e NaturalOne (falhas)"]
    Sum --> Res["DiceRollResult"]
```

---

## 4. Representação Visual do Pacote de Regras de Tormenta20 em C# (`bindings/csharp/Tormenta20Rules.cs`)

Toda a lógica específica de **Tormenta20 — Edição Jogo do Ano** está isolada em C# dentro do namespace `OpenTTRPG.Tormenta20`, configurando o Core C++ via P/Invoke.

### 4.1. `Tormenta20ActionRules` — Fila de Prioridades e Conversão Unidirecional de Ações
Em Tormenta20 Jogo do Ano:
1. As ações agendadas no turno são executadas na ordem de prioridade: **1º Ação Livre $\rightarrow$ 2º Ação de Movimento $\rightarrow$ 3º Ação Padrão/Comum**.
2. Um personagem pode **gastar sua Ação Padrão para realizar uma Ação de Movimento adicional**, mas **NUNCA pode gastar uma Ação de Movimento para pagar uma Ação Padrão**.

Veja como `Tormenta20ActionRules.ConfigureTurnActions` configura isso no Core:

```mermaid
flowchart TD
    subgraph SlotsT20 ["Slots Configurados por Turno em Tormenta20 (C# -> Core)"]
        Reac["T20ActionType.Reaction (ID 4)\nPrioridade = 5 | Máx = Ilimitada (-1)"]
        Free["T20ActionType.Free (ID 0)\nPrioridade = 10 | Máx = Ilimitada (-1)"]
        Move["T20ActionType.Movement (ID 1)\nPrioridade = 20 | Máx = 1 por turno"]
        Std["T20ActionType.Standard (ID 2)\nPrioridade = 30 | Máx = 1 por turno"]
        Full["T20ActionType.Full (ID 3)\nPrioridade = 40 | Máx = 0 (Consome Padrão + Movimento)"]
    end

    subgraph RegraConversao ["Regras de Conversão (AddActionFallback)"]
        Move -- "1º Se Movimento == 0, pode consumir Padrão!\nAddActionFallback(Movement, Standard)" --> Std
        Std -. "BLOQUEADO! Movimento NÃO paga Padrão" .-> Move
    end

    subgraph OrdemFila ["Ordem de Execução na PriorityQueue do Turno"]
        Q1["1º Ações Livres\n(Prioridade 10)"] --> Q2["2º Ações de Movimento\n(Prioridade 20 — mesmo se pagas com Padrão!)"]
        Q2 --> Q3["3º Ações Padrão / Comuns\n(Prioridade 30)"]
    end
```

---

### 4.2. `Tormenta20AbilityTreeRules` — Topologia Completa da Árvore de Poderes de Tormenta20
O método `Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session)` constrói no Core C++ os **4 ramos canônicos de poderes de Tormenta20**, combinando hierarquia de árvore (`parentKey`), requisitos de aprendizado (`grantRequirements`) e requisitos de ativação no turno (`usageRequirements`):

```mermaid
flowchart TD
    Root(("Raiz da AbilityTree\n(__root__)"))

    %% Ramo 1: Estilo de Uma Arma
    P1["Estilo de Uma Arma\n[key: estilo_uma_arma]\n• Aprender (AllOf): DES >= 1"]
    P2["Ataque Preciso\n[key: ataque_preciso]\n• Pai na Árvore: Estilo de Uma Arma\n• Aprender: Ter aprendido o nó pai"]

    %% Ramo 2: Ataque Poderoso -> Trespassar
    P3["Ataque Poderoso\n[key: ataque_poderoso]\n• Aprender (AllOf): FOR >= 1"]
    P4["Trespassar\n[key: trespassar]\n• Pai na Árvore: Ataque Poderoso\n• Usar no Turno (AllOf):\n  ActionUsedInTurn('Agredir (Ataque Padrão)')"]

    %% Ramo 3: Encouraçado -> Inexpugnável -> Fanático
    P5["Encouraçado\n[key: encouracado]\n• Aprender (AllOf): CON >= 1"]
    P6["Inexpugnável\n[key: inexpugnavel]\n• Pai na Árvore: Encouraçado\n• Aprender (AllOf): Nivel >= 6"]
    P7["Fanático\n[key: fanatico]\n• Pai na Árvore: Inexpugnável\n  (Exige cadeia Encouraçado -> Inexpugnável)\n• Aprender (AllOf): Nivel >= 12"]

    %% Ramo 4: Magia Ilimitada -> Surto Arcano
    P8["Magia Ilimitada\n[key: magia_ilimitada]\n• Aprender (AnyOf / OU):\n  INT >= 1 OU CAR >= 1"]
    P9["Surto Arcano\n[key: surto_arcano]\n• Pai na Árvore: Magia Ilimitada\n• Usar no Turno (AnyOf / OU):\n  ConditionActive('Foco_Arcano') OU\n  ActionUsedInTurn('Mísseis Mágicos')"]

    Root --> P1
    P1 --> P2

    Root --> P3
    P3 --> P4

    Root --> P5
    P5 --> P6
    P6 --> P7

    Root --> P8
    P8 --> P9
```

#### Exemplo Visual da Árvore de Requisitos (`RequirementTree`) de **Surto Arcano** e **Magia Ilimitada**
Para ilustrar como o `Tormenta20AbilityTreeRules` monta as `RequirementTree`s internas de um poder com lógica `OR` (`AnyOf`):

```mermaid
flowchart TD
    subgraph GrantMagia ["grantRequirements de 'magia_ilimitada'"]
        G_Root["Nó Raiz (id=1)\nKind = AnyOf (OR)"]
        G_Leaf1["Nó Folha (id=2)\nKind = StatAtLeast\nkey = 'INT' | threshold = 1"]
        G_Leaf2["Nó Folha (id=3)\nKind = StatAtLeast\nkey = 'CAR' | threshold = 1"]
        G_Root --> G_Leaf1
        G_Root --> G_Leaf2
    end

    subgraph UseSurto ["usageRequirements de 'surto_arcano'"]
        U_Root["Nó Raiz (id=1)\nKind = AnyOf (OR)"]
        U_Leaf1["Nó Folha (id=2)\nKind = ConditionActive\nkey = 'Foco_Arcano' | threshold = 1"]
        U_Leaf2["Nó Folha (id=3)\nKind = ActionUsedInTurn\nkey = 'Mísseis Mágicos'"]
        U_Root --> U_Leaf1
        U_Root --> U_Leaf2
    end
```

---

### 4.3. Diagrama de Sequência: Um Turno Completo de Combate (C# $\leftrightarrow$ C ABI $\leftrightarrow$ Core C++)

O diagrama abaixo mostra o que acontece nos bastidores durante um turno em que **Valeros** tenta usar *Trespassar* antes de atacar (sendo bloqueado pela árvore), realiza um *Ataque Padrão* (desbloqueando *Trespassar*), usa *Ataque Especial* (Ação Livre) e encerra o turno executando a Fila de Prioridades na ordem correta:

```mermaid
sequenceDiagram
    autonumber
    actor Jogador
    participant Rules as Tormenta20Rules (C#)
    participant Session as EncounterSession (C#)
    participant Core as Encounter + ActionEconomy + AbilityTree (C++)

    Jogador->>Session: CanUseAbility(Valeros, "trespassar")
    Session->>Core: ottrpg_ability_can_use("trespassar")
    Core-->>Session: false ("Exige ter executado 'Agredir (Ataque Padrão)' neste turno")
    Session-->>Jogador: [BLOQUEADO PELA ÁRVORE DE PRÉ-REQUISITOS]

    Jogador->>Rules: EnqueueTurnAction(Valeros, Standard, "Agredir (Ataque Padrão)")
    Rules->>Session: EnqueueAction(Valeros, requiredType=2, "Agredir (Ataque Padrão)")
    Session->>Core: Consome Slot[Standard] (1->0), insere na PriorityQueue (prio=30) e grava no turnActionHistory_
    Core-->>Jogador: [FILA] 'Agredir (Ataque Padrão)' enfileirada!

    Jogador->>Session: CanUseAbility(Valeros, "trespassar")
    Session->>Core: ottrpg_ability_can_use("trespassar")
    Core-->>Session: true ('Agredir (Ataque Padrão)' consta no histórico do turno!)

    Jogador->>Rules: EnqueueTurnAction(Valeros, Free, "Ataque Especial", pmCost=1)
    Rules->>Session: SpendResource(PM, 1) + EnqueueAction(Valeros, requiredType=0, "Ataque Especial")
    Session->>Core: Desconta 1 PM e insere na PriorityQueue (prio=10 -> vai para a FRENTE da fila!)

    Jogador->>Rules: Encerrar Turno -> DrainPriorityQueueInOrder(Valeros)
    Rules->>Core: PopNextAction() -> 1º Sai "Ataque Especial" [Prio 10 - Livre]
    Rules->>Core: PopNextAction() -> 2º Sai "Agredir (Ataque Padrão)" [Prio 30 - Padrão]
    Jogador->>Session: NextTurn()
    Session->>Core: Avança para o próximo da iniciativa e reseta slots e histórico do turno
```

---

## 5. Documentos Relacionados

* **[Guia de Integração com a Interface Gráfica (`docs/ui_integration_guide.md`)](ui_integration_guide.md):** Mostra exatamente quais métodos de `EncounterSession` e `Tormenta20Rules` chamar em cada botão e tela do Godot Engine + C#.
* **[Regras de Tormenta20 (`docs/tormenta20_rules.md`)](tormenta20_rules.md):** Referência das fórmulas e tabelas oficiais de Tormenta20 Jogo do Ano.
* **[To-Do List (`To_Do_List.md`)](../To_Do_List.md):** Roadmap atualizado do projeto.
