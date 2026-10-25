# Guia de Integração de Interface Gráfica (Godot Engine / C# UI) — OpenTTRPG

Este documento foi criado para guiar quem for programar a **Interface Gráfica (Godot Engine 4 com C#, WPF, Avalonia ou qualquer outro frontend)** do **OpenTTRPG**.

Aqui está mapeado exatamente **qual classe e qual função chamar em cada tela, botão ou componente visual da interface**, sem que a equipe de UI precise mexer em C++ ou ponteiros nativos.

---

## 1. Arquitetura para a Equipe de Interface

Toda a comunicação entre a interface gráfica e o motor em C++ já está pronta e encapsulada em dois arquivos C# dentro de [`bindings/csharp/`](file:///home/iago3457/%C3%81rea%20de%20trabalho/AED/OpenTTRPG/bindings/csharp):

1. **[`OpenTTRPG.cs`](file:///home/iago3457/%C3%81rea%20de%20trabalho/AED/OpenTTRPG/bindings/csharp/OpenTTRPG.cs)** (`namespace OpenTTRPG.Native`):
   Contém a classe principal **`EncounterSession`**, que gerencia a memória da biblioteca nativa (`libopenttrpg_core.so` no Linux / `openttrpg_core.dll` no Windows) de forma automática (`IDisposable`) e expõe métodos C# tipados.
2. **[`Tormenta20Rules.cs`](file:///home/iago3457/%C3%81rea%20de%20trabalho/AED/OpenTTRPG/bindings/csharp/Tormenta20Rules.cs)** (`namespace OpenTTRPG.Tormenta20`):
   Contém os utilitários prontos das regras de **Tormenta20 Jogo do Ano** construídos sobre o Core:
   - **`Tormenta20ActionRules`**: Configura e gerencia a Fila de Prioridades de Ações do turno (`Livre -> Movimento -> Padrão`) e a regra de converter Ação Padrão em Movimento.
   - **`Tormenta20AbilityTreeRules`**: Configura a Árvore de Poderes e Pré-requisitos de Tormenta20 e sincroniza os atributos da ficha com o Core.

---

## 2. Mapa Rápido: "Qual função eu chamo em cada parte da tela?"

| Parte da Interface (Tela / Componente no Godot) | Método C# Recomendado (`EncounterSession` / `Tormenta20Rules`) | Função C ABI Acionada (Interna) |
| :--- | :--- | :--- |
| **Inicializar Gerenciador de Jogo / Combate** | `new EncounterSession(seed)` | `ottrpg_encounter_create` |
| **Bandeja de Dados: Rolar expressão livre (`"2d6+3"`, `"1d20+5"`)** | `session.Roll(string expression)` | `ottrpg_dice_roll_expression` |
| **Bandeja de Dados: Rolar Teste de d20 (Normal / Vantagem / Desvantagem)** | `session.RollD20WithModifier(int mod, AdvantageMode mode)` | `ottrpg_dice_roll_d20` |
| **Cadastrar Personagem / Monstro na Sessão** | `session.AddCombatant(name, dexMod, maxHp, team)` | `ottrpg_combatant_add` |
| **Criar Barra de Recurso Extra (ex.: PM = ID `1`)** | `session.AddResource(id, resourceId: 1, "PM", maxPm, 0)` | `ottrpg_resource_add` |
| **Ler Barra de PV (`0`) ou PM (`1`) para atualizar `ProgressBar`** | `session.GetResource(id, resourceId)` | `ottrpg_resource_get` |
| **Adicionar PV Temporário (Escudo / Magia)** | `session.AddTemporaryResource(id, resourceId: 0, amount)` | `ottrpg_resource_add_temp` |
| **Gastar / Recuperar PM ou outro Recurso** | `session.SpendResource(...)` / `session.RestoreResource(...)` | `ottrpg_resource_spend` / `restore` |
| **Lobby de Combate: Rolar Iniciativa só dos Selecionados** | `session.RollInitiativeForList(int[] selectedIds, 1, 20)` | `ottrpg_initiative_roll_list` |
| **Botão "Iniciar Combate"** | `session.StartCombat()` | `ottrpg_combat_start` |
| **Barra Superior de Iniciativa: Obter ordem dos cards** | `session.GetInitiativeOrder()` | `ottrpg_initiative_get_order` |
| **Destacar de quem é a vez e qual a Rodada** | `session.CurrentTurn` e `session.Round` | `ottrpg_combat_current_turn` / `round` |
| **Botão "Atrasar Turno"** | `session.DelayTurn(combatantId, afterTargetId)` | `ottrpg_initiative_delay_turn` |
| **Barra de Ações: Enfileirar Ataque / Movimento / Poder** | `Tormenta20ActionRules.EnqueueTurnAction(...)` | `ottrpg_action_enqueue` |
| **Painel da Fila de Prioridades: Listar ações agendadas** | `session.GetActionQueue(combatantId)` | `ottrpg_action_queue_get` |
| **Botão "Desfazer / Cancelar Ação da Fila"** | `session.CancelQueuedAction(combatantId, actionId, true)` | `ottrpg_action_queue_cancel` |
| **Botão "Encerrar Turno" (Executa a fila e passa a vez)** | `Tormenta20ActionRules.DrainPriorityQueueInOrder(...)` + `session.NextTurn()` | `ottrpg_action_queue_pop` + `next_turn` |
| **Clicar em Alvo para Causar Dano (com RD/Resistência)** | `session.DealDamage(targetId, amount, damageType)` | `ottrpg_combat_deal_damage` |
| **Clicar em Alvo para Curar PV** | `session.Heal(targetId, amount)` | `ottrpg_combat_heal` |
| **Ícones de Buff/Debuff: Adicionar / Consultar / Remover Condição** | `session.AddCondition` / `HasCondition` / `RemoveCondition` | `ottrpg_condition_add` / `has` / `remove` |
| **Tela de Árvore de Poderes: Checar se poder está disponível** | `session.CanLearnAbility(id, key, out string unmetReasons)` | `ottrpg_ability_can_learn` |
| **Tela de Árvore de Poderes: Botão "Desbloquear Poder"** | `session.TryLearnAbility(id, key, out string unmetReasons)` | `ottrpg_ability_learn` |
| **Barra de Habilidades: Checar se poder pode ser usado no turno** | `session.CanUseAbility(id, key, out string unmetUsage)` | `ottrpg_ability_can_use` |

---

## 3. Exemplos Práticos por Componente da Interface (C# / Godot)

### 3.1. Gerenciador Global da Sessão (Autoload / Singleton no Godot)

No Godot, crie um Node Autoload (ex.: `CombatManager.cs`) que mantém a instância de [`EncounterSession`](file:///home/iago3457/%C3%81rea%20de%20trabalho/AED/OpenTTRPG/bindings/csharp/OpenTTRPG.cs#L487-L494):

```csharp
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

public class CombatManager
{
    public const int HealthResourceId = 0; // PV já vem criado por padrão no ID 0
    public const int ManaResourceId   = 1; // PM será registrado no ID 1

    public EncounterSession Session { get; private set; }

    public void InitializeNewSession()
    {
        Session?.Dispose();
        Session = new EncounterSession(); // Semente 0 = aleatória (relógio do sistema)

        // Registra a Árvore de Poderes de Tormenta20 no Core
        Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(Session);
    }

    public void Shutdown()
    {
        Session?.Dispose();
        Session = null;
    }
}
```

---

### 3.2. Componente: Rolador de Dados / Bandeja de Dados (Dice Tray)

Quando o jogador clicar em um botão de **"Rolar Dado"** (seja um teste de perícia `1d20+5`, um ataque com vantagem `2d20kh1+6` ou o dano de uma arma `2d6+4`):

```csharp
// Exemplo 1: Rolando qualquer expressão de dado digitada ou vinda de uma arma/magia
public void OnRollExpressionButtonPressed(string formula) // ex: "2d6+4", "1d20+7", "4d6dl1"
{
    RollResult resultado = Session.Roll(formula);

    // Atualize os labels na interface gráfica:
    // resultado.Total           -> Valor final somado (dados + modificador)
    // resultado.DiceTotal       -> Soma apenas dos dados rolados (sem o modificador fixo)
    // resultado.Modifier        -> Modificador numérico fixo da fórmula
    // resultado.NaturalMaxCount -> Quantos dados caíram no valor máximo (ex: 20 natural no d20!)
    // resultado.NaturalOneCount -> Quantos dados caíram em 1 natural (Falha Crítica!)

    if (resultado.NaturalMaxCount > 0)
    {
        // Tocar efeito sonoro/ partículas douradas de CRÍTICO NATURAL na UI!
    }
    else if (resultado.NaturalOneCount > 0)
    {
        // Tocar efeito vermelho de FALHA CRÍTICA na UI!
    }
}

// Exemplo 2: Botão específico de Teste de d20 (Normal, Vantagem ou Desvantagem)
public void OnSkillCheckButtonPressed(int modificadorPericia, AdvantageMode modo = AdvantageMode.Normal)
{
    RollResult teste = Session.RollD20WithModifier(modificadorPericia, modo);
    // Exibir teste.Total na tela
}
```

---

### 3.3. Componente: Sincronizando Ficha do Personagem com o Combate

Quando um personagem cadastrado é carregado na cena:

```csharp
public int RegisterCharacterInCore(CharacterSheetData ficha, int teamId = 1)
{
    // 1. Cria o combatente no Core (já cria o recurso de PV no ID 0)
    int entityId = Session.AddCombatant(ficha.Name, ficha.Dexterity, ficha.MaxHp, teamId);
    Session.SetResourceCurrent(entityId, CombatManager.HealthResourceId, ficha.CurrentHp);

    // 2. Adiciona a barra de Pontos de Mana (PM) no ID 1
    Session.AddResource(entityId, CombatManager.ManaResourceId, "PM", ficha.MaxPm, minValue: 0);
    Session.SetResourceCurrent(entityId, CombatManager.ManaResourceId, ficha.CurrentPm);

    // 3. Configura a Economia de Ações de Tormenta20 (1 Padrão, 1 Movimento, Livres ilimitadas, Padrão -> Movimento)
    Tormenta20ActionRules.ConfigureTurnActions(
        Session, entityId, standardActions: 1, movementActions: 1, freeActions: -1);

    // 4. Sincroniza Nível, Atributos (FOR, DES, CON, INT, SAB, CAR) e Poderes já aprendidos
    Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
        Session, entityId,
        ficha.Level,
        ficha.Strength, ficha.Dexterity, ficha.Constitution,
        ficha.Intelligence, ficha.Wisdom, ficha.Charisma,
        ficha.UnlockedTreeKeys);

    return entityId;
}
```

---

### 3.4. Componente: Modal de Seleção de Participantes da Iniciativa

Na tela antes de começar a batalha, o mestre/jogador marca em *Checkboxes* quais personagens/monstros vão participar daquele encontro (para que personagens fora da cena não entrem na iniciativa):

```csharp
public void OnStartEncounterButtonPressed(int[] idsMarcadosNosCheckboxes)
{
    // 1. Rola 1d20 + Modificador de Iniciativa de uma vez só para a lista selecionada
    Session.RollInitiativeForList(idsMarcadosNosCheckboxes, diceCount: 1, diceSides: 20);

    // 2. Inicia o combate (posiciona o cursor no 1º da fila e seta Rodada = 1)
    Session.StartCombat();

    // 3. Atualiza a barra visual de iniciativa na tela
    RefreshInitiativeBarUI();
}
```

---

### 3.5. Componente: Barra Superior de Iniciativa e Controle de Turnos

Para desenhar a fila de retratos/cards no topo da tela (estilo *Baldur's Gate 3* / *FoundryVTT*):

```csharp
public void RefreshInitiativeBarUI()
{
    int rodadaAtual = Session.Round;
    int idDeQuemEstaNaVez = Session.CurrentTurn;

    // Retorna os IDs já ordenados do maior para o menor na iniciativa
    int[] ordemIniciativa = Session.GetInitiativeOrder();

    foreach (int id in ordemIniciativa)
    {
        string nome = Session.GetCombatantName(id);
        CombatantInfo info = Session.GetCombatantInfo(id);
        ResourceInfo pv = Session.GetResource(id, CombatManager.HealthResourceId);
        ResourceInfo pm = Session.GetResource(id, CombatManager.ManaResourceId);

        bool ehAVezDele = (id == idDeQuemEstaNaVez);
        bool estaCaidoOuFora = info.Defeated; // true se PV == 0 ou se foi marcado como fora de combate

        // Crie/atualize o Card na UI com:
        // - Nome, Iniciativa (info.Initiative)
        // - Barra de PV (pv.Current / pv.Max + pv.Temporary)
        // - Barra de PM (pm.Current / pm.Max)
        // - Borda brilhante se 'ehAVezDele' for true
        // - Ícone de caveira/desmaiado se 'estaCaidoOuFora' for true
    }
}
```

---

### 3.6. Componente: Barra de Ações do Turno e Fila de Prioridades

No turno de um personagem, a UI mostra quantas ações ele ainda tem (`Padrão`, `Movimento`, `Livre`) e a **Fila de Prioridades** (`Livre -> Movimento -> Padrão`):

```csharp
// 1. Quando o jogador clica em "Ataque Padrão", "Movimentar" ou em uma Habilidade:
public void OnActionSlotClicked(int actorId, T20ActionType tipoAcao, string nomeAcao, int alvoId = 0, int custoPm = 0)
{
    ActionUseResult res = Tormenta20ActionRules.EnqueueTurnAction(
        Session, actorId, tipoAcao, nomeAcao, alvoId, custoPm);

    if (!res.Success)
    {
        // Exibir popup/toast de aviso na UI: res.Message
        // (ex.: "[BLOQUEADO] Sem Ação Padrão disponível" ou "PM insuficiente")
        return;
    }

    // Se res.UsedFallback == true, a UI pode mostrar um ícone indicando que a Ação de Movimento
    // foi paga convertendo a Ação Padrão do turno!
    RefreshActionQueueUI(actorId);
}

// 2. Para desenhar os ícones da Fila de Prioridades na tela:
public void RefreshActionQueueUI(int actorId)
{
    // Texto resumo pronto (ex: "Padrão: 1/1 | Movimento: 0/1 | Livre: Ilimitadas")
    string resumoSlots = Tormenta20ActionRules.FormatRemainingActions(Session, actorId);

    // Lista já ordenada por prioridade (1º Ações Livres, 2º Movimento, 3º Padrão)
    QueuedActionEntry[] fila = Session.GetActionQueue(actorId);
    foreach (QueuedActionEntry item in fila)
    {
        // item.ActionId     -> ID único para o botão de cancelar ("X") ao lado da ação
        // item.Name         -> Nome da ação/habilidade
        // item.RequiredType -> Tipo original da ação
        // item.UsedFallback -> Se usou conversão (ex: Padrão -> Movimento)
    }
}

// 3. Botão "X" (Desfazer/Cancelar uma ação enfileirada antes de executar):
public void OnCancelQueuedActionPressed(int actorId, int actionId)
{
    // Remove da fila e devolve o slot de ação para o personagem!
    Session.CancelQueuedAction(actorId, actionId, refundSlot: true);
    RefreshActionQueueUI(actorId);
}

// 4. Botão "Encerrar Turno" (Executa a fila na ordem correta e avança para o próximo):
public void OnEndTurnButtonPressed()
{
    int actorId = Session.CurrentTurn;

    // Drena a fila na ordem Livre -> Movimento -> Padrão
    var executadas = Tormenta20ActionRules.DrainPriorityQueueInOrder(Session, actorId);
    foreach (var acao in executadas)
    {
        // Pode disparar a animação de cada ação no log de combate
    }

    // Avança para o próximo combatente ativo na iniciativa (e reseta as ações do próximo)
    Session.NextTurn();
    RefreshInitiativeBarUI();
}
```

---

### 3.7. Componente: Causar Dano, Curar e Aplicar Condições em Alvos

Quando o jogador seleciona um alvo na tela (pelo `targetId` do card ou do token) e aplica dano ou cura:

```csharp
// Causar Dano (passa automaticamente pelo pipeline de RD, Resistência, Vulnerabilidade e PV Temporário)
public void OnApplyDamageToTarget(int targetId, int danoBruto, int tipoDano = 0)
{
    DamageResult resultado = Session.DealDamage(targetId, danoBruto, tipoDano);

    // resultado.FinalAmount         -> Dano real descontado após RD/Resistências
    // resultado.AbsorbedByTemporary -> Quanto foi absorvido por PV Temporário
    // resultado.Depleted            -> true se o alvo caiu a 0 PV neste golpe!

    RefreshInitiativeBarUI();
}

// Curar PV de um Alvo
public void OnHealTarget(int targetId, int cura)
{
    int pvRecuperados = Session.Heal(targetId, cura);
    RefreshInitiativeBarUI();
}

// Adicionar uma Condição (ex.: "Abalado" por 3 turnos, diminuindo no fim de cada turno)
public void OnApplyCondition(int targetId, string nomeCondicao, int turnosDuracao)
{
    Session.AddCondition(
        targetId,
        nomeCondicao,
        intensity: 1,
        durationTurns: turnosDuracao,
        tickTiming: TickTiming.EndOfTurn,
        policy: StackPolicy.RefreshDuration);
}
```

---

### 3.8. Componente: Tela da Árvore de Habilidades e Pré-requisitos (`AbilityTree`)

Para desenhar a janela de **Árvore de Poderes** (com linhas ligando cada poder-pai `ParentKey` aos poderes-filhos) e validar se uma habilidade pode ser aprendida ou usada:

```csharp
public void RefreshSkillTreeWindowUI(int combatantId)
{
    foreach (T20PowerDefinition poder in Tormenta20AbilityTreeRules.CanonicalPowers)
    {
        bool jaAprendeu = Session.HasAbility(combatantId, poder.Key);
        bool podeAprender = Session.CanLearnAbility(combatantId, poder.Key, out string motivosPendentes);

        if (jaAprendeu)
        {
            // Pintar o nó de DOURADO (Desbloqueado)
        }
        else if (podeAprender)
        {
            // Pintar o nó de VERDE (Disponível para clicar e aprender)
        }
        else
        {
            // Pintar o nó de CINZA (Bloqueado)
            // Colocar 'motivosPendentes' no Tooltip ao passar o mouse!
            // Exemplo do que vem em 'motivosPendentes':
            // "Requer habilidade ancestral na árvore: 'Ataque Poderoso'; Requer atributo/estatística 'Nivel' >= 6 (atual: 1)"
        }
    }
}

// Quando o jogador clica no botão "Aprender Poder" na Árvore:
public void OnLearnPowerButtonPressed(int combatantId, string powerKey)
{
    if (Session.TryLearnAbility(combatantId, powerKey, out string erro))
    {
        // Sucesso! Poder desbloqueado no personagem.
        RefreshSkillTreeWindowUI(combatantId);
    }
    else
    {
        // Mostrar alerta na UI com a string 'erro'
    }
}

// Na Barra de Habilidades durante o Combate (para saber se o botão da habilidade deve estar habilitado agora):
public bool IsAbilityButtonEnabledInCombat(int combatantId, string powerKey, out string tooltipMotivo)
{
    // Valida pré-requisitos de uso no turno (ex.: Trespassar exige ter usado 'Agredir (Ataque Padrão)' neste turno)
    return Session.CanUseAbility(combatantId, powerKey, out tooltipMotivo);
}
```

