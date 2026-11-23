# Guia da Interface Gráfica (Godot Engine 4.7 .NET + C#) — OpenTTRPG

Este documento detalha a implementação real da **Interface Gráfica do OpenTTRPG em Godot Engine 4.7 (.NET / C#)**, mapeando suas cenas, componentes visuais, controles do Tabletop e as chamadas entre **Godot UI $\leftrightarrow$ Regras T20 (C#) $\leftrightarrow$ Core C++ (C ABI)**.

---

## 1. Estrutura da Camada Gráfica e Integração Nativa

O projeto gráfico em [`godot_ui/open-ttrpg/`](../godot_ui/open-ttrpg/) importa diretamente os módulos compartilhados de [`bindings/csharp/Native/`](../bindings/csharp/Native/) e [`bindings/csharp/Tormenta20/`](../bindings/csharp/Tormenta20/) através de [`OpenTTRPG.csproj`](../godot_ui/open-ttrpg/OpenTTRPG.csproj), sem código duplicado.

* **Autoload Global:** [`scripts/CombatManager.cs`](../godot_ui/open-ttrpg/scripts/CombatManager.cs) — Inicializa a [`EncounterSession`](../bindings/csharp/Native/EncounterSession.cs), configura o resolver da biblioteca nativa (`libopenttrpg_core.so` / `openttrpg_core.dll`) e mantém as listas persistentes de Heróis (`saves/characters.json`), Inimigos (`saves/enemies.json`) e Snapshot de Combate (`saves/encounter_snapshot.json`).
* **Cenas Principais:**
  - [`Cenas/MainMenu.tscn`](../godot_ui/open-ttrpg/Cenas/MainMenu.tscn) / [`MainMenu.cs`](../godot_ui/open-ttrpg/Cenas/MainMenu.cs) — Menu inicial do sistema.
  - [`Cenas/MainUi.tscn`](../godot_ui/open-ttrpg/Cenas/MainUi.tscn) / [`MainUi.cs`](../godot_ui/open-ttrpg/Cenas/MainUi.cs) — Interface principal com 4 áreas de trabalho (**Ficha**, **Inimigos**, **Combate**, **Mapa Tático**) + **Pop-up Bandeja de Dados** + **Modal Árvore de Poderes**.
* **Renderizadores Customizados (`CanvasItem._Draw`):**
  - [`scripts/DiceVisualCanvas.cs`](../godot_ui/open-ttrpg/scripts/DiceVisualCanvas.cs) — Animação procedural de dados poliedrais (`d4` a `d100`), `Tween` e partículas para acertos e falhas críticas.
  - [`scripts/AbilityTreeCanvas.cs`](../godot_ui/open-ttrpg/scripts/AbilityTreeCanvas.cs) — Grafo interativo dos **5 ramos / 17 poderes** da árvore N-ária com cores de estado (aprendido, disponível, bloqueado) e tooltip de pré-requisitos.
  - [`scripts/TacticalGridCanvas.cs`](../godot_ui/open-ttrpg/scripts/TacticalGridCanvas.cs) — Tabuleiro tático 2D com câmera suave (zoom interpolado centrado no cursor e pan fluido), destaque de alcance, rota A\*, cobertura, névoa de guerra e tokens animados.

---

## 2. Telas e Funcionalidades Implementadas na `MainUi`

1. **Aba Ficha (Heróis):**
   - **Partições Redimensionáveis:** `HSplitFicha` (divide lista/atributos da ficha detalhada) e `VSplitLeftCol` (divide a lista de personagens dos painéis de atributos/recursos).
   - **Criação Automática por Seletores:** `OptionButton` ligados aos enums de **14 Classes**, **17 Raças**, **34 Origens** e **20 Deuses Maiores (+ Não Devoto)**, recalculando PV, PM, Defesa e deslocamento automaticamente via [`Tormenta20CharacterCreationRules.cs`](../bindings/csharp/Tormenta20/Tormenta20CharacterCreationRules.cs).
   - **Recursos, Estado de Morrendo e Habilidades:** Edição de PV/PM/PV Temp, indicação clara do Estado de Morrendo (`-max(10, PV Máx / 2)`), gasto direto de habilidades/magias na ficha com desconto de PM e controle de descanso.
   - **Perícias, Inventário e Árvore:** Rolagem rápida das **29 perícias**, compra/gerenciamento de itens com `T$` e cálculo automático de espaços de carga.
2. **Aba Inimigos (Bestiário & Ficha de Ameaças):**
   - Criação a partir dos **8 presets canônicos** de [`Tormenta20Bestiary.cs`](../bindings/csharp/Tormenta20/Tormenta20Bestiary.cs) (`Goblin Salteador` ND 1/4 até `Dragão Jovem` ND 7) ou ameaça em branco.
   - Edição de ND, Tipo, Tamanho, Papel, Defesa Fixa, RD, Deslocamento (`m`), Ataques com `AttackBonus` e overrides de perícias (*Iniciativa*, *Percepção*, *Fortitude*, *Reflexos*, *Vontade*), salvos em `saves/enemies.json`.
3. **Aba Combate:**
   - **Lobby de Iniciativa:** Seleção dos heróis participantes e adição rápida de inimigos com multiplicador de clones (**`x1..x8`**) em times distintos.
   - **Controle de Turno e Fila de Prioridades:** Barra superior de iniciativa (`GetInitiativeOrder`), destaque do turno atual e rodada, enfileiramento ordenado (`Reação -> Livre -> Movimento -> Padrão -> Completa`), botão desfazer com reembolso e resolução de ataques/dano/condições.
4. **Aba Mapa Tático (Tabletop 2D) + Aba Ajuda:**
   - **Controles de Câmera Suave (`TacticalGridCanvas.cs`):**
     - `Roda do mouse` (ou botões `+` / `-` / `Reset`): Zoom exponencial interpolado mantendo o ponto sob o cursor fixo.
     - `Botão direito`, `Botão do meio` ou `Espaço + arrastar`: Pan fluido pelo tabuleiro.
     - `Botão esquerdo`: Seleciona célula/token e move ou ataca sem disparar movimento acidental após um arrasto.
   - **Recursos Táticos:** Editor de terrenos (Normal, Terreno Difícil, Parede/Obstáculo, Cobertura), 4 presets de arena, imagem de fundo opcional, alternância de Névoa de Guerra (*Visão dos Heróis* vs *Visão do Mestre*) e **aba Ajuda** embutida no painel lateral com todos os comandos.
5. **Pop-up Bandeja de Dados (`_popupDadosOverlay`):**
   - Acessível de qualquer aba sem trocar de tela (`ToggleDiceTrayPopup`), aceitando fórmulas (`"1d20+5"`, `"2d20kh1+6"`, `"4d6dl1"`) ou cliques nos dados com feedback visual em `DiceVisualCanvas`.

---

## 3. Mapeamento: `Interface (Godot) -> Regras (C#) -> Core (C ABI)`

| Ação na Interface (`MainUi` / `CombatManager`) | Camada C# (`EncounterSession` / `Tormenta20*Rules`) | Função C ABI Acionada |
| :--- | :--- | :--- |
| **Inicializar sessão de combate** | `new EncounterSession()` + `RegisterTormenta20PowerTrees` | `ottrpg_encounter_create` |
| **Rolar dados na Bandeja / Perícias** | `session.Roll(expr)` / `Tormenta20SkillRules.RollSkillCheck(...)` | `ottrpg_dice_roll_expression` / `d20` |
| **Registrar Herói / Clone de Inimigo no combate** | `session.AddCombatant(...)` + `AddResource(Mana)` + RD | `ottrpg_combatant_add` / `resource_add` |
| **Iniciar encontro com selecionados** | `session.RollInitiativeForList(...)` + `session.StartCombat()` | `ottrpg_initiative_roll_list` / `combat_start` |
| **Atualizar barra de iniciativa e turno** | `session.GetInitiativeOrder()`, `session.CurrentTurn`, `session.Round` | `ottrpg_initiative_get_order` / `current_turn` |
| **Enfileirar / Cancelar / Executar ações do turno** | `Tormenta20ActionRules.EnqueueTurnAction(...)`, `CancelQueuedAction`, `DrainPriorityQueueInOrder` | `ottrpg_action_enqueue` / `cancel` / `pop` |
| **Atacar alvo (com Flanqueamento, Cobertura e Crítico)** | `Tormenta20TacticalRules.EvaluateAttackContext(...)` + `Tormenta20CombatRules.ResolveWeaponAttack(...)` | `ottrpg_damage_deal` / `combatant_get_resource` |
| **Aplicar condição / Testar Sangramento (`CD 15`)** | `Tormenta20ConditionRules.ApplyCondition(...)` / `ProcessStartOfTurnBleeding(...)` | `ottrpg_condition_add` / `remove` |
| **Abrir e interagir com a Árvore de Poderes** | `session.CanLearnAbility(...)` / `TryLearnAbility(...)` / `CanUseAbility(...)` | `ottrpg_ability_can_learn` / `learn` / `can_use` |
| **Configurar grid, mover token e consultar raio** | `session.ResetMap(...)`, `SetMapCell(...)`, `MoveToken(...)`, `QueryTokensInRadius(...)` | `ottrpg_map_reset` / `set_cell` / `move_token` / `query_radius` |
| **Salvar / Restaurar fichas, bestiário e combate+mapa** | `Tormenta20Persistence.Save*` / `Load*` + reconstrução na sessão | Recomposição via `AddCombatant` + `SetInitiative` + `ottrpg_map_*` |

---

## 4. Diretrizes Arquiteturais e Visuais Mantidas na UI

1. **Zero P/Invoke dentro de `_Process()` ou `_Draw()`:** Os scripts visuais (`DiceVisualCanvas`, `AbilityTreeCanvas`, `TacticalGridCanvas`) desenham exclusivamente a partir de snapshots em cache C#, evitando chamadas nativas a cada frame.
2. **Textos limpos voltados ao jogador:** Os rótulos, botões, logs e abas da UI do Godot não utilizam emojis (para evitar falhas de renderização de glifos) nem comentários técnicos de implementação direcionados ao programador.
3. **Resolução portátil de bibliotecas e saves:** `CombatManager.cs` resolve dinamicamente `libopenttrpg_core.so` / `openttrpg_core.dll` e a pasta `saves/` para que a aplicação funcione de forma idêntica dentro do editor, no CLI e nas releases Linux/Windows.
