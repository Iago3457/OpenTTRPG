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
- [x] **Bindings P/Invoke e Regras em C#** (`bindings/csharp/OpenTTRPG.cs`, `Tormenta20Rules.cs`, `CLIController.cs`, `OpenTTRPG.CLI.csproj`).
- [x] **Bateria de Testes Automatizados do Core e DLL** (`tests/test_generic_core.cpp`).

---

## 3. Refatoração Arquitetural: Migrar Código Não-Agnóstico (Tormenta20) para C#
> **Objetivo:** Garantir que o C++ contenha **exclusivamente** estruturas de dados de AED1, o Core 100% agnóstico de sistema e a C API, movendo toda modelagem específica de Tormenta20 para a camada C#.

- [ ] **Migrar Modelos de Ficha de T20 (`src/models/` e `include/openttrpg/models/`) para C#**:
  - [ ] Consolidar `CharacterSheet`, `Character`, `Attribute`, `Item` (com regra de espaços e penalidade de armadura) e `Skill` inteiramente no projeto C# (`bindings/csharp/`), removendo as classes específicas de Tormenta20 do C++.
- [ ] **Remover Sistemas Legados de T20 em C++ (`src/systems/` e `include/openttrpg/systems/`)**:
  - [ ] Substituir `DiceRoller` e `InitiativeSystem` legados pelo uso exclusivo do `DiceEngine` e `InitiativeTracker` do Core genérico via C#.
- [ ] **Limpar `include/openttrpg/core/Types.hpp`**:
  - [ ] Manter no C++ apenas identificadores agnósticos (`EntityId`, `ResourceId`, `DamageTypeId`, `ActionTypeId`), movendo enums específicos de Tormenta20 (`AttributeType`, `ItemType`) para o namespace `OpenTTRPG.Tormenta20` em C#.
- [ ] **Desacoplar o Build da DLL (`libopenttrpg_core.so` / `.dll`)**:
  - [ ] Fazer com que a biblioteca compartilhada compile apenas `src/core/*.cpp` e `src/api/openttrpg_api.cpp`, mantendo o `CLIController.cs` como cliente oficial via P/Invoke.

---

## 4. Passos Restantes para um Programa Funcional Completo (Regras, Persistência e Qualidade)
- [ ] **Persistência de Dados (Salvar e Carregar em JSON no C#)**:
  - [ ] Serializar e desserializar Fichas de Personagens (atributos, inventário, poderes desbloqueados na árvore) em arquivos `.json` (`System.Text.Json`).
  - [ ] Salvar e restaurar o estado de um combate em andamento (`Encounter` snapshot: rodada, turno atual, PV/PM e condições ativas).
- [ ] **Sistema Completo de Perícias e Testes de Tormenta20 (C#)**:
  - [ ] Implementar a tabela de perícias de T20 JdA com cálculo automático ($\text{Metade do Nível} + \text{Atributo} + \text{Bônus de Treinamento} + \text{Outros}$).
  - [ ] Aplicar automaticamente a Penalidade de Armadura em perícias baseadas em Força/Destreza (Acrobacia, Furtividade, Ladinagem) e exigir treinamento em perícias *Somente Treinadas*.
- [ ] **Resolução Automática de Ataques vs. Defesa e Pipeline de Dano (C# + Core)**:
  - [ ] Automatizar o fluxo de ataque: rolagem de teste de ataque (`1d20 + Luta/Pontaria`) comparada contra a `Defesa` do alvo.
  - [ ] Verificar automaticamente a margem de ameaça da arma (ex.: `19/x2`, `18/x3`), multiplicar apenas os dados base da arma no crítico (regra de T20) e enviar o total ao `session.DealDamage(...)` respeitando RD, vulnerabilidades e imunidades configuradas no alvo.
- [ ] **Catálogo de Condições Canônicas de Tormenta20 (C#)**:
  - [ ] Mapear condições oficiais de T20 (*Abalado*, *Apavorado*, *Caído*, *Cego*, *Desprevenido*, *Enjoado*, *Fraco*, *Imóvel*, *Lento*, *Ofuscado*, * Sangrando*, *Sobrecarregado*, *Vulnerável*) usando `ConditionTracker`, `Stat` e bloqueios de ações no Core.
- [ ] **Aprimoramentos de Magias e Limite de Gasto de PM (C#)**:
  - [ ] Validar a regra de limite de gasto de PM por nível de personagem (ou nível + atributo-chave caso possua o poder *Magia Ilimitada* na árvore).
- [ ] **Mecânica de Descanso e Recuperação Entre Encontros (C#)**:
  - [ ] Implementar recuperação de PV e PM por nível de acordo com a qualidade do descanso (*Ruim*: metade do nível; *Normal*: igual ao nível; *Confortável*: dobro do nível; *Luxuoso*: triplo do nível).
- [ ] **Qualidade, Testes em C# e Auditoria de Memória(Opcional)**:
  - [ ] Criar suíte de testes automatizados em C# (`xUnit` ou `NUnit`) testando `Tormenta20ActionRules`, `Tormenta20AbilityTreeRules` e `EncounterSession`.
  - [ ] Validar ausência de *memory leaks* nas estruturas manuais (`LinkedList`, `Queue`, `PriorityQueue`, `Tree`) com `AddressSanitizer` (`-fsanitize=address`) / `Valgrind`.

---

## 5. Interface Gráfica do Projeto (Fase 2 — Godot Engine 4 + C#)
- [ ] **Configuração do Projeto Godot (.NET) e Integração Nativa**:
  - [ ] Importar `OpenTTRPG.cs` e `Tormenta20Rules.cs` no projeto Godot C# e configurar o carregamento automático de `libopenttrpg_core.so` (Linux) / `openttrpg_core.dll` (Windows).
- [ ] **Tela de Criação e Gerenciamento de Fichas**:
  - [ ] Interface visual para distribuição de atributos, barras de PV/PM, cálculo dinâmico de Defesa, inventário com barra de espaços de carga e Tibares (T$).
- [ ] **Visualizador Interativo da Árvore de Poderes (`AbilityTree`) (Opcional)**:
  - [ ] Grafo/árvore visual de poderes exibindo nós aprendidos, disponíveis e bloqueados (com *tooltip* detalhando os pré-requisitos pendentes retornados por `CanLearnAbility`).
- [ ] **HUD de Combate por Turnos e Bandeja de Dados (Dice Tray)**:
  - [ ] Modal de seleção de participantes para a iniciativa (`RollInitiativeForList`).
  - [ ] Barra superior de ordem de iniciativa (`GetInitiativeOrder`) com destaque para o combatente do turno atual (`CurrentTurn`) e contador de rodada (`Round`).
  - [ ] Painel da Fila de Prioridades de Ações do turno (`GetActionQueue`, `EnqueueTurnAction`, botão de desfazer com `CancelQueuedAction` e execução em ordem).
  - [ ] Bandeja de rolagem de dados animada(?) consumindo `session.Roll(...)` com destaque para Críticos Naturais e Falhas Críticas.

---)
## 6. Opcional / Última Etapa: Grid Tático Espacial e Mapa de Batalha
> **Nota:** Funcionalidade opcional a ser desenvolvida somente após todas as etapas anteriores do sistema de fichas, regras em C# e HUD de combate estarem completas.

- [ ] *(Opcional)* **Estrutura de Dados Espacial no Core C++ (`QuadTree` / Matriz Esparsa)**:
  - [ ] Representação agnóstica de coordenadas e células do mapa tático no Core C++.
- [ ] *(Opcional)* **Movimentação e Alcance no Grid (Godot + Core)**:
  - [ ] Renderização de tabuleiro em grid quadrado ou hexagonal no Godot, validação de deslocamento por ação de movimento e cálculo de distância para ataques corpo a corpo e à distância.
- [ ] *(Opcional)* **Linha de Visão (*Line of Sight*) e *Fog of War***:
  - [ ] Cálculo de obstáculos/cobertura e visibilidade de tokens no mapa tático.
