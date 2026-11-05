# Arquitetura e Representação Visual do Sistema — OpenTTRPG

Este documento apresenta a engenharia de software e a **representação visual detalhada** de cada subsistema do **Core Genérico em C++17**, das **Estruturas de Dados Manuais de AED1**, da **Fronteira C ABI** e do **Pacote Modular de Regras de Tormenta20 (Jogo do Ano) em C#**.

---

## 1. Visão Macro: Arquitetura em 4 Camadas e Fronteira Nativa (P/Invoke)

O **OpenTTRPG** separa estritamente **mecanismos universais de RPG** (em C++17) de **regras específicas de um sistema** (em C#). Nenhum arquivo C++ conhece o que é "Tormenta20", "Pontos de Mana", "Luta" ou "Ataque Poderoso" — tudo no C++ opera sobre identificadores genéricos (`EntityId`, `ResourceId`, `DamageTypeId`, `ActionTypeId`) e árvores/filas abstratas:

```mermaid
flowchart TD
    subgraph Camada4 ["4. Camada de Aplicação e Regras do Sistema (C# / .NET Modular)"]
        UI["Interface Gráfica (Godot Engine 4)\nou CLI (bindings/csharp/CLI/)"]

        subgraph T20Package ["Pacote Modular Tormenta20 (bindings/csharp/Tormenta20/)"]
            T20Mod["Tormenta20Models & Tormenta20Enums\n(Ficha, Atributos, Espaços, Armas, Magias)"]
            T20Act["Tormenta20ActionRules\n(Ações, Prioridades e Conversões)"]
            T20Tree["Tormenta20AbilityTreeRules\n(Árvore de Poderes e Pré-requisitos)"]
            T20Skill["Tormenta20SkillRules\n(30 Perícias, Treino e Pen. Armadura)"]
            T20Cond["Tormenta20ConditionRules\n(13 Condições, Escalonamento e Sangramento)"]
            T20Comb["Tormenta20CombatRules\n(Ataque vs. Defesa, Crítico Base e RD)"]
            T20Mag["Tormenta20MagicRules & Tormenta20RestRules\n(Aprimoramentos, Limite PM e Descanso)"]
        end

        subgraph NativePkg ["Pacote P/Invoke (bindings/csharp/Native/)"]
            Session["EncounterSession + RequirementBuilder\n(NativeTypes.cs / NativeMethods.cs)"]
        end
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
        LL["LinkedList&lt;T&gt; (Lista Encadeada Cadastral)"]
        Q["Queue&lt;T&gt; (Fila FIFO Dinâmica)"]
        PQ["PriorityQueue&lt;T&gt; (Fila de Prioridades Estável)"]
        TR["Tree&lt;T&gt; (Árvore N-ária Genérica)"]
    end

    UI --> T20Package
    UI --> Session
    T20Package --> Session
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

Todas as 4 estruturas foram implementadas manualmente do zero com ponteiros (`new` / `delete`), obedecem à **Regra dos Cinco (*Rule of Five*)** (destrutor virtual/explícito, construtor de cópia, operador de atribuição por cópia, construtor de movimento e atribuição por movimento) e são verificadas contra vazamentos de memória via `make test_asan` (`-fsanitize=address,leak`).

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
    HEAD["head_ (dequeue / peek)"] --> P1["[Prio 10 | 1º Livre]\nAtaque Especial"]
    P1 --> P2["[Prio 10 | 2º Livre]\nGrito de Guerra"]
    P2 --> P3["[Prio 20 | Movimento]\nDeslocar 9m"]
    P3 --> P4["[Prio 30 | Padrão]\nAgredir"]
    P4 --> NULL["nullptr"]
```
* **Inserção (`enqueue(value, priority)`)**: Percorre a lista enquanto `curr->priority <= priority` e insere o novo nó logo após o último elemento de mesma prioridade ($O(N)$), garantindo estabilidade sem reordenar elementos antigos.
* **Remoção seletiva (`remove_if`)**: Permite cancelar uma ação específica no meio da fila (usado quando o jogador desfaz uma ação agendada no turno e recebe o slot de volta).

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

## 3. Como Cada Subsistema do Core C++ Funciona

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

### 3.2. `ActionEconomy` — Slots de Ação, Conversão Direcional (`Fallback`), Custos Compostos e Fila de Prioridades
O `ActionEconomy` é **100% agnóstico de sistema**: ele gerencia uma lista de `ActionSlot`s, uma lista de regras direcionais `ActionFallbackRule` (`requestedType -> fallbackType`) e uma `PriorityQueue<QueuedAction>`:

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

* **Ações Compostas (`enqueueCompoundAction`)**: Usadas para ações que exigem múltiplos slots simultaneamente (como a **Ação Completa** em Tormenta20, que consome `1 Ação Padrão + 1 Ação de Movimento`). Se o jogador cancelar a ação composta na fila, todos os slots consumidos são reembolsados atomicamente.

---

### 3.3. `AbilityTree` & `RequirementTree` — Árvore de Habilidades + Árvore Lógica de Pré-requisitos
Este subsistema combina **duas árvores (`Tree<T>`) trabalhando em conjunto**:

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

---

## 4. Como os Módulos de Tormenta20 em C# Funcionam (`bindings/csharp/Tormenta20/`)

Toda a lógica específica de **Tormenta20 — Edição Jogo do Ano** está dividida em 9 arquivos modulares dentro de `bindings/csharp/Tormenta20/` (`namespace OpenTTRPG.Tormenta20`):

### 4.1. `Tormenta20ActionRules.cs` — Economia de Ações e Fila de Prioridades
* Registra os slots de ação no Core:
  - `Free` (`ID 0`, Prioridade `10`, Ilimitada `-1`)
  - `Movement` (`ID 1`, Prioridade `20`, `1` por turno)
  - `Standard` (`ID 2`, Prioridade `30`, `1` por turno)
  - `Full` (`ID 3`, Prioridade `40`, consome `Standard + Movement` via `EnqueueCompoundAction`)
  - `Reaction` (`ID 4`, Prioridade `5`, Ilimitada `-1`)
* Registra a regra oficial `AddActionFallback(Movement, Standard)`: permite gastar a **Ação Padrão** para realizar uma **Ação de Movimento** adicional, mas bloqueia o inverso.
* Antes de enfileirar qualquer ação, consulta `Tormenta20ConditionRules.CanExecuteActionType(...)` para verificar se o combatente está sob condições restritivas (ex.: *Imóvel* ou *Lento* bloqueiam Ações Completas; *Enjoado* permite apenas 1 Ação Padrão **ou** 1 de Movimento por turno).

### 4.2. `Tormenta20AbilityTreeRules.cs` — Árvore de Poderes Canônicos
Constrói os 4 ramos na `AbilityTree` do Core:
1. **Estilo de Uma Arma** (`DES >= 1`) $\rightarrow$ **Ataque Preciso**
2. **Ataque Poderoso** (`FOR >= 1`) $\rightarrow$ **Trespassar** (exige ter executado `"Agredir (Ataque Padrão)"` no turno atual)
3. **Encouraçado** (`CON >= 1`) $\rightarrow$ **Inexpugnável** (`Nivel >= 6`) $\rightarrow$ **Fanático** (`Nivel >= 12`)
4. **Magia Ilimitada** (`INT >= 1 OU CAR >= 1`) $\rightarrow$ **Surto Arcano** (exige condição `Foco_Arcano` ativa **ou** ter lançado `"Mísseis Mágicos"` no turno)

### 4.3. `Tormenta20SkillRules.cs` — Sistema Completo de Perícias
Implementa a tabela oficial de 30 perícias de Tormenta20 Jogo do Ano:
$$\text{Total da Perícia} = \left\lfloor \frac{\text{Nível}}{2} \right\rfloor + \text{AtributoEfetivo} + \text{BônusDeTreino} + \text{PenalidadeDeArmadura} + \text{PenalidadeDeCondição} + \text{Outros}$$
* **Bônus de Treinamento por Patamar:** `+2` (Níveis 1 a 6), `+4` (Níveis 7 a 14) e `+6` (Níveis 15 a 20).
* **Somente Treinada (`TrainedOnly`):** Bloqueia testes de perícias como *Ladinagem*, *Misticismo*, *Cura* e *Ofício* se o personagem não for treinado.
* **Penalidade de Armadura (`ArmorPenaltyApplies`):** Aplica automaticamente a penalidade de armaduras/escudos equipados (e `–5` adicional se estiver **Sobrecarregado**) em *Acrobacia*, *Furtividade* e *Ladinagem*.

### 4.4. `Tormenta20CombatRules.cs` — Resolução de Ataques vs. Defesa e Multiplicação Crítica
```mermaid
flowchart TD
    Atk["ResolveWeaponAttack(session, attackerId, targetId, weapon)"] --> Roll20["Rola 1d20 + Modificador de Luta/Pontaria\n(+2 na margem de ameaça se tiver Ataque Preciso)"]
    Roll20 --> CheckNat{"Dado Natural d20"}
    CheckNat -- "1 Natural" --> Miss["Falha Crítica Automática!"]
    CheckNat -- "20 Natural ou Total >= Defesa do Alvo" --> CritCheck{"Dado Natural >= Margem de Ameaça?\n(Ex: >= 19 ou >= 17 com Ataque Preciso)"}
    CheckNat -- "Total < Defesa" --> Miss

    CritCheck -- "NÃO (Acerto Normal)" --> NormalDmg["Rola Dados Base da Arma (ex: 1d8)\n+ FOR/DES + Bônus Fixos (ex: Ataque Poderoso +5)"]
    CritCheck -- "SIM (Acerto Crítico!)" --> CritDmg["Multiplica APENAS os Dados Base da Arma!\n(Ex: 1d8 com x2 vira 2d8; com Ataque Preciso vira 3d8)\nSoma FOR/DES e Bônus Fixos UMA única vez"]

    NormalDmg --> Pipeline["Envia ao Core: session.DealDamage(targetId, total, damageType)\nAplica RD, Vulnerabilidade, Resistência e PV Temporário"]
    CritDmg --> Pipeline
```

### 4.5. `Tormenta20ConditionRules.cs` — Catálogo de 13 Condições Canônicas
Gerencia os efeitos mecânicos automáticos de condições no combate:
* **Escalonamento de Medo:** Aplicar **Abalado** em quem já está **Abalado** converte automaticamente a condição para **Apavorado**.
* **Modificadores de Defesa e Atributos:** *Caído* (`–5` Defesa e ataque corpo a corpo), *Desprevenido* (`–5` Defesa e Reflexos), *Vulnerável* (`–2` Defesa), *Cego* (`–5` Defesa e `–2` em FOR/DES), *Fraco* (`–2` em FOR/DES/CON).
* **Sangramento:** No início do turno (`ProcessStartOfTurnBleeding`), rola um teste de Constituição (`1d20 + CON >= CD 15`). Se passar, estanca o sangramento; se falhar, perde `1d6` PV.

### 4.6. `Tormenta20MagicRules.cs` & `Tormenta20RestRules.cs` — Magias e Descanso
* **Limite de PM (`Tormenta20MagicRules`):** Em Tormenta20, o máximo de PM que um personagem pode gastar em uma única magia ou habilidade com aprimoramentos é igual ao seu **Nível**. Se o personagem possuir o poder **Magia Ilimitada**, esse limite passa a ser $\text{Nível} + \text{Atributo-Chave}$.
* **Recuperação de Descanso (`Tormenta20RestRules`):** Calcula e aplica a recuperação de PV e PM por noite de descanso:
  - *Ruim*: $\max(1, \lfloor \text{Nível} / 2 \rfloor)$
  - *Normal*: $1 \times \text{Nível}$
  - *Confortável*: $2 \times \text{Nível}$
  - *Luxuoso*: $3 \times \text{Nível}$

---

## 5. Documentos Relacionados

* **[Guia de Implementação da Persistência de Dados JSON (`docs/data_persistence_guide.md`)](data_persistence_guide.md):** Instruções detalhadas e código de referência para salvar/carregar fichas e snapshots de combate em JSON.
* **[Guia de Integração com a Interface Gráfica (`docs/ui_integration_guide.md`)](ui_integration_guide.md):** Mostra exatamente quais métodos de `EncounterSession` e dos módulos `Tormenta20*Rules` chamar em cada botão e tela do Godot Engine 4 + C#.
* **[Regras de Tormenta20 (`docs/tormenta20_rules.md`)](tormenta20_rules.md):** Referência das fórmulas e tabelas oficiais de Tormenta20 Jogo do Ano.
* **[To-Do List (`To_Do_List.md`)](../To_Do_List.md):** Roadmap atualizado do projeto.
