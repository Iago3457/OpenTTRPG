# Lista de Tarefas (To-Do List) — OpenTTRPG

## 1. Melhorias de Combate e Turnos (Concluído)
- [x] **Fix: Pula turno ao executar alguma ação, como causar dano** — Resolvido no Core: ações não avançam turno automaticamente; `nextTurn` é explícito.
- [x] **Feat: Selecionar alvo de ataque e ações por ID** — Resolvido no Core: `dealDamage(targetId, ...)`, `heal(targetId, ...)` e demais operações atuam diretamente sobre o `EntityId` do alvo.
- [x] **Feat: Verificação de ações e custos com Fila de Prioridades** — Resolvido no Core (`ActionEconomy` + `PriorityQueue<T>`) e em C# (`Tormenta20ActionRules`): ordem de execução `Livre -> Movimento -> Comum/Padrão` e conversão direcional `Padrão -> Movimento` (sem permitir o inverso).
- [x] **Fix: Seleção de participantes na lista de iniciativa** — Resolvido permitiendo escolher quais personagens cadastrados entram no combate via `Encounter::rollInitiativeForList` (C++), `ottrpg_initiative_roll_list` (C API), `EncounterSession.RollInitiativeForList` (C#) e seleção em lote no `CLIController`.
- [x] **Feat/Fix: Escolher habilidades e descontar custo de PM dinamicamente** — Resolvido com `spendResource(target, resourceId, amount)` integrado às habilidades da ficha.
- [x] **FIX ERRO CRÍTICO: Dano caindo no próximo integrante da fila** — Resolvido no Core desacoplando o alvo da ação da cabeça da fila de iniciativa.
- [x] **Feat: Estado de MORRIDO / Fora de Combate** — Resolvido com `isDefeated()`, `setForcedOut()` e pulo automático na iniciativa (`autoSkipDefeated`).

---

## 2. Core Genérico em C++ & Interop C ABI / DLL (Concluído)
- [x] **Estruturas de Dados Manuais de AED1 (`include/openttrpg/data_structures/`)**:
  - [x] Lista Duplamente/Simplesmente Encadeada Cadastral (`LinkedList<T>`)
  - [x] Fila Dinâmica FIFO (`Queue<T>`)
  - [x] Fila de Prioridades Ordenada (`PriorityQueue<T>`)
  - [x] Árvore N-ária Genérica (`Tree<T>`)
- [x] **Motor de Dados Genérico (`DiceEngine`)** com parsing de expressões (`"2d20kh1+5"`, `"4d6dl1"`, dados explosivos `"3d6!"`, vantagem/desvantagem).
- [x] **Gestor de Recursos Genérico (`ResourcePool`)** com mínimo, máximo e absorção automática de pontos temporários.
- [x] **Pipeline de Dano Agnóstico (`Damage`)** com resistências, vulnerabilidades, imunidades e Redução de Dano (RD).
- [x] **Rastreador de Condições e Status (`ConditionTracker`)** com políticas de empilhamento, durações e ticks de início/fim de turno.
- [x] **Fila de Iniciativa Ordenada Dinâmica (`InitiativeTracker`)** com desempates por modificador/empate e suporte a atrasar turno (`delayTurn`).
- [x] **Economia de Ações de Turno 100% Genérica (`ActionEconomy`)** com prioridades e regras de conversão (`addFallback`) desacopladas.
- [x] **Sistema de Árvores de Habilidades e Pré-requisitos (`AbilityTree` / `RequirementTree`)** com validação de cadeia ancestral na árvore e nós lógicos (`AllOf`, `AnyOf`, `Not`, `AtLeastN`) e folhas (`AbilityUnlocked`, `StatAtLeast`, `ConditionActive`, `ActionUsedInTurn`, `ResourceAtLeast`).
- [x] **Fachada de Encontro (`Encounter`)** orquestrando combatentes, turnos, ações, árvore de habilidades e dados.
- [x] **C ABI Estável (`openttrpg_api.h` / `openttrpg_api.cpp`)** compilada em biblioteca compartilhada (`bin/libopenttrpg_core.so` / `.dll`).
- [x] **Bindings P/Invoke e Regras Modularizadas em C#** (`bindings/csharp/Native/`, `bindings/csharp/Tormenta20/`, `bindings/csharp/CLI/`, `bindings/csharp/Tests/`, `OpenTTRPG.CLI.csproj`).
- [x] **Bateria de Testes Automatizados do Core e DLL** (`tests/test_generic_core.cpp`).

---

## 3. Refatoração Arquitetural: Migrar Código Não-Agnóstico (Tormenta20) para C# (Concluído)
> **Objetivo:** Garantir que o C++ contenha **exclusivamente** estruturas de dados de AED1, o Core 100% agnóstico de sistema e a C API, movendo toda modelagem específica de Tormenta20 para a camada C#.

- [x] **Migrar Modelos de Ficha de T20 (`src/models/` e `include/openttrpg/models/`) para C#**:
  - [x] Consolidar `CharacterSheetData`, `Attribute`, `CharacterItem` (com regra de espaços e penalidade de armadura) e `CharacterSkill` inteiramente no projeto C# (`bindings/csharp/Tormenta20/Tormenta20Models.cs` e `Tormenta20Enums.cs`), removendo as classes específicas de Tormenta20 do C++.
- [x] **Remover Sistemas Legados de T20 em C++ (`src/systems/` e `include/openttrpg/systems/`)**:
  - [x] Substituir `DiceRoller` e `InitiativeSystem` legados pelo uso exclusivo do `DiceEngine` e `InitiativeTracker` do Core genérico via C#.
- [x] **Limpar `include/openttrpg/core/Types.hpp`**:
  - [x] Manter no C++ apenas identificadores agnósticos (`EntityId`, `ResourceId`, `DamageTypeId`, `ActionTypeId`), movendo enums específicos de Tormenta20 (`AttributeType`, `ItemType`, `T20DamageType`) para o namespace `OpenTTRPG.Tormenta20` em C#.
- [x] **Desacoplar o Build da DLL (`libopenttrpg_core.so` / `.dll`)**:
  - [x] Fazer com que a biblioteca compartilhada compile apenas `src/core/*.cpp` e `src/api/openttrpg_api.cpp`, mantendo o `CLIController.cs` como cliente oficial via P/Invoke.

---

## 4. Passos Restantes para um Programa Funcional Completo (Regras, Persistência e Qualidade)
- [x] **Persistência de Dados (Salvar e Carregar em JSON no C#)** *(Ver guia detalhado em [`docs/data_persistence_guide.md`](docs/data_persistence_guide.md))*:
  - [x] Serializar e desserializar Fichas de Personagens (atributos, inventário, poderes desbloqueados na árvore) em arquivos `.json` (`System.Text.Json`).
  - [x] Salvar e restaurar o estado de um combate em andamento (`Encounter` snapshot: rodada, turno atual, PV/PM e condições ativas).
- [x] **Sistema Completo de Perícias e Testes de Tormenta20 (C#)**:
  - [x] Implementar a tabela de perícias de T20 JdA (`Tormenta20SkillRules`) com cálculo automático ($\text{Metade do Nível} + \text{Atributo} + \text{Bônus de Treinamento} + \text{Outros}$).
  - [x] Aplicar automaticamente a Penalidade de Armadura em perícias baseadas em Força/Destreza (Acrobacia, Furtividade, Ladinagem) e exigir treinamento em perícias *Somente Treinadas*.
- [x] **Resolução Automática de Ataques vs. Defesa e Pipeline de Dano (C# + Core)**:
  - [x] Automatizar o fluxo de ataque (`Tormenta20CombatRules.ResolveWeaponAttack`): rolagem de teste de ataque (`1d20 + Luta/Pontaria`) comparada contra a `Defesa` do alvo.
  - [x] Verificar automaticamente a margem de ameaça da arma (ex.: `19/x2`, `18/x3`), multiplicar apenas os dados base da arma no crítico (regra de T20) e enviar o total ao `session.DealDamage(...)` respeitando RD, vulnerabilidades e imunidades configuradas no alvo.
- [x] **Catálogo de Condições Canônicas de Tormenta20 (C#)**:
  - [x] Mapear condições oficiais de T20 (*Abalado*, *Apavorado*, *Caído*, *Cego*, *Desprevenido*, *Enjoado*, *Fraco*, *Imóvel*, *Lento*, *Ofuscado*, *Sangrando*, *Sobrecarregado*, *Vulnerável*) em `Tormenta20ConditionRules` usando `ConditionTracker`, `Stat` e bloqueios de ações no Core.
- [x] **Aprimoramentos de Magias e Limite de Gasto de PM (C#)**:
  - [x] Validar a regra de limite de gasto de PM por nível de personagem (ou nível + atributo-chave caso possua o poder *Magia Ilimitada* na árvore) em `Tormenta20MagicRules`.
- [x] **Mecânica de Descanso e Recuperação Entre Encontros (C#)**:
  - [x] Implementar recuperação de PV e PM por nível de acordo com a qualidade do descanso (*Ruim*: metade do nível; *Normal*: igual ao nível; *Confortável*: dobro do nível; *Luxuoso*: triplo do nível) em `Tormenta20RestRules`.
- [x] **Qualidade, Testes em C# e Auditoria de Memória (Opcional)**:
  - [x] Criar suíte de testes automatizados em C# (`bindings/csharp/Tests/Tormenta20Tests.cs`) testando `Tormenta20ActionRules`, `Tormenta20AbilityTreeRules`, `Tormenta20SkillRules`, `Tormenta20CombatRules`, `Tormenta20ConditionRules`, `Tormenta20MagicRules`, `Tormenta20RestRules` e `EncounterSession`.
  - [x] Validar ausência de *memory leaks* nas estruturas manuais (`LinkedList`, `Queue`, `PriorityQueue`, `Tree`) com `AddressSanitizer` (`make test_asan` com `-fsanitize=address,leak`).

---

## 5. Interface Gráfica do Projeto (Fase 2 — Godot Engine 4 + C#) (Concluído)
- [x] **Configuração do Projeto Godot (.NET) e Integração Nativa**:
  - [x] Importar as pastas `bindings/csharp/Native/` e `bindings/csharp/Tormenta20/` no projeto Godot C# (`godot_ui/open-ttrpg/OpenTTRPG.csproj`) e configurar o carregamento automático de `libopenttrpg_core.so` (Linux) / `openttrpg_core.dll` (Windows) em `CombatManager.cs`.
- [x] **Tela de Criação e Gerenciamento de Fichas**:
  - [x] Interface visual para distribuição de atributos, barras de PV/PM (incluindo PV Temporário `+Temp`), carteira de Tibares (`T$`) com compra automática no inventário, cálculo dinâmico de Defesa, inventário com barra de espaços de carga, descanso e perícias clicáveis.
- [x] **Visualizador Interativo da Árvore de Poderes (`AbilityTree`) (Opcional)**:
  - [x] Janela interativa da árvore de poderes (`AbilityTreeCanvas`) exibindo os 5 ramos em layout N-ário gráfico com nós aprendidos, disponíveis e bloqueados (detalhando os pré-requisitos pendentes retornados por `CanLearnAbility`).
- [x] **HUD de Combate por Turnos e Bandeja de Dados (Dice Tray)**:
  - [x] Modal de seleção de participantes para a iniciativa (`RollInitiativeForList`), incluindo escolha de Time (Heróis vs. Inimigos) e geração rápida de oponentes.
  - [x] Barra superior de ordem de iniciativa (`GetInitiativeOrder`) com destaque para o combatente do turno atual (`CurrentTurn`), contador de rodada (`Round`) e salvamento/restauração de snapshot de combate em JSON (`SaveEncounterSnapshotToFile` / `LoadEncounterSnapshotFromFile`).
  - [x] Painel da Fila de Prioridades de Ações do turno (`GetActionQueue`, `EnqueueTurnAction`, Ação Completa, botão de desfazer com `CancelQueuedAction` e reembolso de slot/PM, e botão `PopNextAction` de execução em ordem de prioridade).
  - [x] Bandeja de rolagem de dados em **Pop-up sobreposto** consumindo `session.Roll(...)` com renderizador visual de dados poliedrais (`DiceVisualCanvas`: `CanvasItem._Draw()`, `Tween` e `CpuParticles2D`) e destaque animado para Críticos Naturais e Falhas Críticas.

---
## 6. Opcional / Última Etapa: Grid Tático Espacial e Mapa de Batalha (Concluído)
> **Nota:** Todas as etapas anteriores do sistema de fichas, regras em C#, HUD de combate e agora o Tabletop Tático estão 100% implementadas e testadas.

- [x] **Estrutura de Dados Espacial no Core C++ (`QuadTree` / Matriz Esparsa)**:
  - [x] `SpatialTypes.hpp`, `SparseGrid<T>` e `QuadTree<T>` manuais de AED1 integrados em `TacticalMap` + `Encounter` + C ABI (`ottrpg_map_*`) com auditoria `AddressSanitizer` (`0 memory leaks`).
- [x] **Movimentação e Alcance no Grid (Godot + Core)**:
  - [x] Tabuleiro interativo em grid quadrado no Godot (`TacticalGridCanvas.cs`, zoom/pan, imagem de fundo opcional, animações `Tween` e Editor de Cenários), cálculo oficial de diagonais alternadas `1-2-1-2` (`1 quadrado = 1,5m`), pathfinding Dijkstra/A\* e validação de alcance corpo a corpo / à distância em `Tormenta20TacticalRules.cs`.
- [x] **Linha de Visão (*Line of Sight*) e *Fog of War***:
  - [x] Raycasting Supercover em grid avaliando paredes bloqueadas, Cobertura Leve (`+5 Defesa`), Flanqueamento (`+2 Ataque`) e Névoa de Guerra (*Unseen / Explored / Visible*) com alternância entre Visão do Mestre e Visão dos Heróis e persistência em JSON.

---

## 7. Polimento de UX, Bestiário e Experiência de Mesa (Concluído)
- [x] **Ficha Própria de Inimigos / Ameaças e Bestiário (`Tormenta20Bestiary.cs`)**:
  - [x] Aba dedicada **Inimigos** na `MainUi` com 8 presets canônicos de Tormenta20 JdA (`Goblin Salteador` ND 1/4 até `Dragão Jovem` ND 7), criação de ameaça em branco, Defesa Fixa, Redução de Dano (RD), bônus de ataque (`AttackBonus` sem somar Força duplicada), overrides de perícias (Iniciativa, Percepção, Fortitude, Reflexos, Vontade) e persistência dedicada em `saves/enemies.json`.
  - [x] Suporte a seleção de múltiplos clones de inimigos no Combate (`x1..x8`) com snapshot inline (`InlineSheet`) preservado ao salvar e restaurar encontros.
- [x] **Limpeza Visual e Redimensionamento de Interface**:
  - [x] Remoção completa de emojis dos textos visíveis da UI Godot e remoção de comentários técnicos de programador visíveis para o jogador.
  - [x] Partições redimensionáveis na Ficha de Personagens (`HSplitFicha` entre coluna esquerda/direita e `VSplitLeftCol` entre lista de personagens e atributos/recursos).
- [x] **Aba de Ajuda e Câmera Suave no Tabletop (`TacticalGridCanvas.cs`)**:
  - [x] Aba **Ajuda** integrada ao painel lateral do Mapa Tático com atalhos de câmera, cliques de movimentação/ataque, leitura de marcadores e regras táticas (`1q = 1,5m`, `1-2-1-2`, cobertura e flanqueamento).
  - [x] Câmera tática refinada com zoom interpolado exponencialmente centrado no cursor (`ZoomBy` / `TargetZoom`), pan fluido (botão direito/meio ou `Espaço + arrastar`) e clique esquerdo inteligente sem deslocar tokens acidentalmente ao arrastar.
- [x] **Empacotamento e Validação Multiplataforma (Linux & Windows)**:
  - [x] Geração e validação de releases em `make release-all` (Linux `x86_64` + Windows `win-x64` com `openttrpg_core.dll` estática e fallback offline `.NET 8.0.32`), passando `make test`, `make test_asan` (`0` leaks) e `--self-test` (**8/8** etapas).

