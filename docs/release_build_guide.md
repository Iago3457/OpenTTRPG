# Guia Completo de Build e Release Multiplataforma (Linux & Windows) — OpenTTRPG

Este documento explica como as três camadas do **OpenTTRPG** (**Interface Gráfica Godot 4**, **Regras de Tormenta20 em C#** e **Core Agnóstico em C++17 com Estruturas de Dados de AED1**) são compiladas, empacotadas e interagem em tempo de execução tanto no **Linux (`x86_64`)** quanto no **Windows (`win-x64`)**, além do passo a passo para gerar novas releases no futuro.

---

## 1. Como a UI (Godot), o C# (.NET 8) e o C++17 (DLL / .SO) Interagem na Release

Quando o usuário abre o executável final (`OpenTTRPG.x86_64` no Linux ou `OpenTTRPG.exe` no Windows), três subsistemas rodam acoplados no mesmo processo:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ 1. CAMADA VISUAL — GODOT ENGINE 4.7 (.NET)                                   │
│    Executável: OpenTTRPG.x86_64 (Linux) / OpenTTRPG.exe (Windows)            │
│    Pacote de Cenas e Recursos: OpenTTRPG.pck                                 │
│    • Cenas: MainMenu.tscn, MainUi.tscn                                       │
│    • Renderizadores Procedurais (CanvasItem._Draw + Tween + CpuParticles2D): │
│      - DiceVisualCanvas.cs (Dados poliedrais d4..d100 animados)              │
│      - AbilityTreeCanvas.cs (Grafo visual da Árvore N-ária de Habilidades)   │
│      - TacticalGridCanvas.cs (Tabuleiro 2D, tokens animados e Névoa)         │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       │ Eventos de UI ( Sinais / Callbacks C# )
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 2. CAMADA DE REGRAS E PERSISTÊNCIA — C# (.NET 8)                             │
│    Assembly compilado: data_OpenTTRPG_*/OpenTTRPG.dll                        │
│    • Autoload Singleton: CombatManager.cs                                    │
│      - Configura NativeLibrary.SetDllImportResolver com caminhos absolutos   │
│      - Gerencia a sessão nativa (EncounterSession) e saves/characters.json   │
│    • Módulos OpenTTRPG.Tormenta20 (bindings/csharp/Tormenta20/):             │
│      - Tormenta20CharacterCreationRules (Enums + PV/PM/Defesa automáticos)   │
│      - Tormenta20Bestiary (8 ameaças canônicas + Ficha de Inimigo)           │
│      - Tormenta20SkillRules (29 perícias), Tormenta20CombatRules, MagicRules │
│      - Tormenta20ConditionRules (14 condições, Morrendo/Sangrando CON CD 15) │
│      - Tormenta20ActionRules, Tormenta20AbilityTreeRules (5 ramos / 17 nós)  │
│      - Tormenta20RestRules, Tormenta20TacticalRules (1-2-1-2, A*, LoS, Fog)  │
│      - Tormenta20Persistence (characters.json, enemies.json, snapshot+mapa)  │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       │ P/Invoke (C ABI: ottrpg_api.h)
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 3. CAMADA CORE AGNÓSTICA & AED1 — C++17 NATIVO                               │
│    Biblioteca Dinâmica: libopenttrpg_core.so (Linux) / openttrpg_core.dll    │
│    • Estruturas de Dados Manuais de AED1 (include/openttrpg/data_structures):│
│      - LinkedList<T>    -> Cadastro de combatentes, condições e recursos     │
│      - Queue<T>         -> Fila FIFO circular dinâmica de turnos             │
│      - PriorityQueue<T> -> Fila de Prioridades das ações planejadas no turno │
│      - Tree<T>          -> Árvore N-ária de habilidades e RequirementTree    │
│      - SparseGrid<T>    -> Matriz esparsa hash de terrenos/obstáculos do mapa│
│      - QuadTree<T>      -> Árvore quaternária espacial de tokens no tabuleiro│
│    • Sistemas Core (src/core/):                                              │
│      - DiceEngine, ResourcePool (PV/PM/Temp), DamagePipeline (RD/Imunidade), │
│      - InitiativeTracker (RollInitiativeForList), ActionEconomy, Encounter   │
│      - TacticalMap (grid agnóstico e posicionamento/busca espacial de tokens)│
└──────────────────────────────────────────────────────────────────────────────┘
```

### Mapa das Interações em Tempo de Execução
1. **Bandeja de Dados e Testes de Perícia (`MainUi` → `CombatManager.RollTest` → C++ `DiceEngine`)**:
   - O clique em qualquer botão de dado (`d4`..`d100`) ou perícia na ficha envia a expressão (ex.: `"2d20kh1+5"`) via P/Invoke para `ottrpg_dice_roll`, que executa o parser em C++ e devolve `RollResult` para o `DiceVisualCanvas` animar o poliedro e as partículas.
2. **Árvore N-ária de Habilidades (`AbilityTreeCanvas` → `Tormenta20AbilityTreeRules` → C++ `Tree<T>` & `RequirementTree`)**:
   - Ao abrir a janela **Árvore de Habilidades**, o C# sincroniza nível, atributos e poderes do personagem com o C++ (`ottrpg_combatant_set_stat` / `ottrpg_combatant_unlock_ability`) e consulta `ottrpg_ability_can_learn` e `ottrpg_ability_can_use` para cada um dos 17 nós nos 5 ramos da árvore N-ária.
3. **Seleção de Iniciativa e Turnos (`BuildSelecaoIniciativaDialog` → `StartCombatWithSelectedCharacters` → C++ `InitiativeTracker`)**:
   - Os participantes marcados e seus respectivos times são registrados no C++ via `ottrpg_combatant_add`, e a iniciativa é rolada exclusivamente para a lista selecionada com `ottrpg_initiative_roll_list` (`RollInitiativeForList`).
4. **Fila de Prioridades de Ações do Turno (`RefreshActionQueuePanel` → `Tormenta20ActionRules` → C++ `PriorityQueue<T>`)**:
   - Ataques, movimentos, poderes/magias e ações completas entram na `PriorityQueue<T>` do C++ (`ottrpg_action_enqueue` / `ottrpg_action_enqueue_compound`), com conversão automática `Padrão → Movimento`, execução ordenada (`ottrpg_action_queue_pop`) e botão de desfazer com reembolso de slot e PM (`ottrpg_action_queue_cancel`).
5. **Ataques, Dano, PV Temporário e Estado de Morrendo (`Tormenta20CombatRules` → C++ `DamagePipeline` & `ResourcePool`)**:
   - Ataques calculam margem de ameaça e multiplicador de dados base em C#, aplicam o dano no `ResourcePool` C++ (`ottrpg_damage_deal`, que desconta automaticamente o PV Temporário antes do PV real e respeita o limite negativo de morte $-\max(10, \text{PV Máx}/2)$) e atualizam as condições `Inconsciente` e `Sangrando` no `ConditionTracker` C++.

---

## 2. Como Gerar as Releases (Linux e Windows) da Próxima Vez

### Pré-requisitos (Em um PC Novo / Clone Limpo do Git)
1. **Compilador C++17**: `g++` (Linux) e `x86_64-w64-mingw32-g++` (`mingw-w64-gcc`) caso deseje cross-compilar a DLL de Windows no Linux;
2. **SDK .NET** (`dotnet`);
3. **Godot Engine 4.7 (.NET / Mono)** (`godot-mono` ou `godot` no `PATH`) **com os Export Templates (.NET) instalados**:
   - No Godot Editor: **Editor → Gerenciar Modelos de Exportação → Baixar e Instalar** (instala os templates da mesma versão `.mono` em `~/.local/share/godot/export_templates/`);
   - Sem os Export Templates instalados, os binários standalone `OpenTTRPG.x86_64` e `OpenTTRPG.exe` (que ficam fora do Git porque `bin/` está no `.gitignore`) não conseguem ser exportados pelo Godot, embora o `Makefile` gere o `.pck` e os scripts `OpenTTRPG.sh` / `OpenTTRPG_GodotUI.sh` como fallback.

### Opção A: Comando Único no Linux (Gera Linux + Windows + Executa Auto-Teste)

A partir da raiz do repositório, basta executar:

```bash
make release-all
```

O comando `make release-all` executa automaticamente:
1. **`make release` (Linux x86_64)**:
   - Compila o Core C++17 em `bin/libopenttrpg_core.so`;
   - Copia `libopenttrpg_core.so` para `godot_ui/open-ttrpg/`, `bin/release/linux-x64/` e `bin/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64/`;
   - Copia as fichas padrão (`saves/characters.json` e `saves/enemies.json`, se existentes) para `bin/release/linux-x64/saves/`;
   - Executa o Godot em modo headless (`--export-release "Linux"`, com fallback para `--export-pack "Linux"` caso os Export Templates não estejam instalados) para gerar `OpenTTRPG.x86_64`, `OpenTTRPG.pck` e publicar os assemblies C# (`.NET`) em `bin/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64/`;
   - Gera automaticamente os launchers executáveis `OpenTTRPG.sh` e `OpenTTRPG_GodotUI.sh` em `bin/release/linux-x64/`;
   - Publica também o executável de terminal `OpenTTRPG.CLI` em `bin/release/linux-x64/`.
2. **`make release-windows` (Windows x86_64 via Cross-Compilation no Linux)**:
   - Cross-compila o Core C++17 para Windows usando `x86_64-w64-mingw32-g++` gerando `bin/release/win-x64/openttrpg_core.dll` com linkagem estática do runtime MinGW (`-static -static-libgcc -static-libstdc++`);
   - Copia `openttrpg_core.dll` para `godot_ui/open-ttrpg/`, `bin/release/win-x64/` e `bin/release/win-x64/data_OpenTTRPG_windows_x86_64/`;
   - Copia `saves/characters.json` e `saves/enemies.json` (se existentes) para `bin/release/win-x64/saves/`;
   - Executa o Godot em modo headless (`--export-release "Windows Desktop"`, com fallback para `--export-pack`) para gerar `OpenTTRPG.exe`, `OpenTTRPG.pck` e publicar os assemblies C# de Windows em `bin/release/win-x64/data_OpenTTRPG_windows_x86_64/`.
3. **Auto-Teste End-to-End (`--self-test`)**:
   - Executa `./bin/release/linux-x64/OpenTTRPG.x86_64 --headless -- --self-test` (ou via `godot-mono --path godot_ui/open-ttrpg` caso o binário exportado não exista) para validar que a UI, o C# e a biblioteca C++ nativa estão conversando sem nenhum erro.

> **Dica:** Você também pode gerar apenas uma plataforma por vez rodando `make release` (somente Linux) ou `make release-windows` (somente Windows).

---

### Opção B: Gerando a Release Nativamente no Windows (`build_windows.bat`)

Se você estiver em uma máquina Windows com **MinGW-w64 (`g++`)**, **.NET 8 SDK** e **Godot 4 (.NET)** instalados no `PATH`, dê duplo-clique ou execute no Prompt de Comando / PowerShell:

```bat
build_windows.bat
```

O script compila `openttrpg_core.dll` estaticamente, publica a CLI, exporta `OpenTTRPG.exe`, copia a DLL e as fichas para a pasta `bin\release\win-x64\` e executa o auto-teste `--self-test`.

---

### Opção C: Exportando os Executáveis Base do Zero pelo Editor Godot

Caso você execute `make clean` (que apaga a pasta `bin/`) e precise recriar os binários de template do Godot (`OpenTTRPG.x86_64` e `OpenTTRPG.exe`):
1. Abra o projeto no Godot 4 (.NET):
   ```bash
   godot-mono --editor --path godot_ui/open-ttrpg
   ```
2. Vá no menu **Projeto → Exportar...** (`Project → Export...`).
3. Os dois presets já estão configurados em `godot_ui/open-ttrpg/export_presets.cfg`:
   - **Linux** → `../../bin/release/linux-x64/OpenTTRPG.x86_64`
   - **Windows Desktop** → `../../bin/release/win-x64/OpenTTRPG.exe`
4. Clique em **Exportar Tudo (Release)** (ou exporte cada preset) e, em seguida, rode `make release-all` no terminal para injetar as bibliotecas C++ (`libopenttrpg_core.so` e `openttrpg_core.dll`) e a pasta `saves/`.

---

## 3. Detalhes Técnicos Críticos (Por que a Release Não Quebra em Outros PCs)

Dois detalhes técnicos garantem que a release funcione em qualquer computador Linux ou Windows sem erros de `DllNotFoundException`:

### 3.1. Resolução Dinâmica de Caminhos Absolutos (`CombatManager.cs`)
Em builds exportadas pelo Godot, o caminho virtual `res://` fica empacotado dentro do arquivo `OpenTTRPG.pck`, e no Linux a chamada `dlopen` recusa carregar bibliotecas `.so` se o caminho não contiver uma barra `/` (caminho absoluto).

Por isso, o construtor estático de `CombatManager.cs` utiliza `NativeLibrary.SetDllImportResolver` convertendo todos os diretórios candidatos para caminhos absolutos (`Path.GetFullPath`) na seguinte ordem:
1. Diretório do executável (`Path.GetDirectoryName(OS.GetExecutablePath())`);
2. Diretório dos assemblies `.NET` (`AppContext.BaseDirectory`, ou seja, `data_OpenTTRPG_linuxbsd_x86_64/` ou `data_OpenTTRPG_windows_x86_64/`) e seu diretório pai;
3. Diretório do projeto no Editor (`ProjectSettings.GlobalizePath("res://")` e `res://../../bin`);
4. Diretório de trabalho atual (`Directory.GetCurrentDirectory()`).

### 3.2. Linkagem Estática do Runtime MinGW na DLL de Windows (`-static`)
Ao cross-compilar `openttrpg_core.dll` com `x86_64-w64-mingw32-g++`, usar apenas `-static-libgcc -static-libstdc++` ainda deixa uma dependência dinâmica oculta da `libwinpthread-1.dll`. Em um Windows limpo (sem MinGW instalado), o Windows falha ao carregar `openttrpg_core.dll` por falta da `libwinpthread-1.dll`.

O `Makefile` e o `build_windows.bat` utilizam:
```bash
x86_64-w64-mingw32-g++ -std=c++17 -O2 -shared -Iinclude src/core/*.cpp src/api/*.cpp \
    -o bin/release/win-x64/openttrpg_core.dll -static -static-libgcc -static-libstdc++
```
Com a flag `-static`, o `winpthread` é embutido diretamente dentro de `openttrpg_core.dll`, fazendo com que a DLL dependa exclusivamente do `KERNEL32.dll` e da CRT nativa do Windows (`api-ms-win-crt-*.dll`).

### 3.3. Fallback Offline do .NET 8 (`PinOfflineNet8Packs` em `OpenTTRPG.csproj`)
Em ambientes sem acesso à rede (ou durante o *restore* offline do Godot no export Linux/Windows), o SDK do .NET pode tentar baixar pacotes de targeting/apphost mais recentes do que os presentes no cache local.

Para evitar falhas de build offline, [`godot_ui/open-ttrpg/OpenTTRPG.csproj`](../godot_ui/open-ttrpg/OpenTTRPG.csproj) define o target `PinOfflineNet8Packs`: quando o cache em `$(NUGET_PACKAGES)` possui a versão `.NET 8.0.32` instalada e não possui a `8.0.33`, ele fixa automaticamente `Microsoft.NETCore.App`, `Microsoft.NETCore.App` AppHostPack e targeting packs na versão `8.0.32`, permitindo que `make release` e `make release-windows` compilem com zero acesso externo.

---

## 4. Estrutura das Pastas de Release para Distribuição

Para distribuir o programa para outra pessoa, basta compactar (`.zip` ou `.tar.gz`) a pasta inteira da plataforma desejada:

### Linux (`bin/release/linux-x64/`)
- `OpenTTRPG.x86_64` / `OpenTTRPG.sh` — Executável principal da Interface Gráfica Godot 4.
- `OpenTTRPG.pck` — Pacote de cenas, fontes e assets do Godot.
- `libopenttrpg_core.so` — Motor Core C++17 (Estruturas de Dados de AED1 + C ABI).
- `data_OpenTTRPG_linuxbsd_x86_64/` — Runtime .NET 8 embutido + `OpenTTRPG.dll` (Regras de Tormenta20 em C#) + cópia de `libopenttrpg_core.so`.
- `saves/characters.json` e `saves/enemies.json` — Fichas iniciais de heróis, bestiário e diretório portátil de salvamento.
- `OpenTTRPG.CLI` — Versão de linha de comando (CLI) em C#.

### Windows (`bin/release/win-x64/`)
- `OpenTTRPG.exe` — Executável principal da Interface Gráfica Godot 4 para Windows.
- `OpenTTRPG.console.exe` — Executável com console aberto para logs e diagnóstico.
- `OpenTTRPG.pck` — Pacote de cenas, fontes e assets do Godot.
- `openttrpg_core.dll` — Motor Core C++17 compilado para Windows x64 (sem dependências externas).
- `data_OpenTTRPG_windows_x86_64/` — Runtime .NET 8 de Windows embutido + `OpenTTRPG.dll` + cópia de `openttrpg_core.dll`.
- `saves/characters.json` e `saves/enemies.json` — Fichas iniciais de heróis, bestiário e diretório portátil de salvamento.

---

## 5. Como Verificar e Diagnosticar Qualquer Release (`--self-test`)

A release possui um modo de auto-diagnóstico automatizado (`--self-test`) que inicializa o `MainMenu`, transita para a `MainUi` e testa **8/8** fluxos integrados (UI Godot $\leftrightarrow$ C# $\leftrightarrow$ C++ Core) em sequência:
1. Ficha de Personagem (`EncounterSession` inicializada via P/Invoke), partições redimensionáveis (`HSplitFicha` e `VSplitLeftCol`) e abertura da **Árvore N-ária de Habilidades** (`Tree<T>` e `RequirementTree`);
2. Aba **Inimigos / Bestiário T20** (`CreateEnemyFromPreset("orc_berserker")`), validando `IsEnemy`, `ChallengeRating`, `FixedDefense`, `DamageReduction` e sincronização na UI;
3. Rolagem na **Bandeja de Dados Visual** em pop-up (`"2d20kh1+5"` via `DiceVisualCanvas` + `DiceEngine` C++);
4. Lobby de iniciativa e início de combate Herói (Time 1) vs Inimigo (Time 2) via **`RollInitiativeForList`** (`InitiativeTracker` C++);
5. Enfileiramento e execução no **Painel de Ações do Turno** (`PriorityQueue<T>` C++);
6. Validação tática de ataque na **Mesa de Combate** respeitando alcance no tabuleiro;
7. **Tabletop (Mapa Tático)**: alternância da **aba Ajuda** (`ToggleTacticalHelpTab`), controle de zoom suave (`ZoomBy` / `TargetZoom`), movimento de token via `SparseGrid`/`QuadTree` consumindo Ação de Movimento e atualização da **Névoa de Guerra**;
8. Ataque corpo a corpo adjacente no mapa (`DamagePipeline` C++) e persistência/restauração completa do combate com Inimigo e Mapa Tático em JSON.

Para rodar o auto-teste:
- **No Linux:**
  ```bash
  ./bin/release/linux-x64/OpenTTRPG.x86_64 --headless -- --self-test
  ```
- **No Windows (PowerShell / CMD):**
  ```bat
  bin\release\win-x64\OpenTTRPG.console.exe --headless -- --self-test
  ```
