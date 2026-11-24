# Regras de Tormenta20 (Edição Jogo do Ano) no OpenTTRPG

Este documento especifica como as regras canônicas de **Tormenta20 — Edição Jogo do Ano (T20 JdA)** estão implementadas nos **13 módulos** do pacote [`bindings/csharp/Tormenta20/`](../bindings/csharp/Tormenta20/) e conectadas ao Core agnóstico em C++17.

---

## 1. Atributos Canônicos (Modificador Direto) — `Tormenta20Models.cs`

O **Tormenta20 Jogo do Ano** utiliza **modificadores diretos** como valor de atributo:

* **Força (FOR):** Ataques e dano corpo a corpo, Luta/Atletismo e capacidade de carga (Espaços).
* **Destreza (DES):** Defesa, Iniciativa, Pontaria, Acrobacia, Furtividade, Ladinagem e Reflexos.
* **Constituição (CON):** Pontos de Vida (PV), Fortitude e testes de estabilização ao sangrar/morrer.
* **Inteligência (INT):** Perícias treinadas, magias arcanas e testes de Misticismo/Conhecimento/Guerra.
* **Sabedoria (SAB):** Percepção, Vontade, Cura, Sobrevivência e conjuração divina.
* **Carisma (CAR):** Perícias sociais (Enganação, Diplomacia, Intimidação, Atuação) e conjuração de Feiticeiros/Bardos.

> **Compatibilidade:** `Attribute.NormalizeT20Value(int)` aceita modificadores diretos (`+3`, `-1`) e converte automaticamente valores legados $\ge 10$ (`16` $\rightarrow$ `+3`).

---

## 2. Criação Automática de Personagens — `Tormenta20CharacterCreationRules.cs` & `Tormenta20Enums.cs`

O sistema oferece seletores canônicos completos para criação de fichas de Heróis sem necessidade de calcular fórmulas manualmente:
* **Catálogo em Enums:** **14 Classes** (`T20Class`), **17 Raças** (`T20Race`), **34 Origens** (`T20Origin`) e **20 Deuses Maiores + Não Devoto** (`T20Deity`).
* **Cálculo Automático de PV e PM (`CalculateMaxHp` / `CalculateMaxPm`):**
  - PV no 1º nível = `PV iniciais da Classe + CON (+ bônus racial quando aplicável)`.
  - PV por nível adicional = `PV por nível da Classe + CON (+ bônus racial por nível, ex.: Golem/Qareen conforme template)`.
  - PM = `PM por nível da Classe * Nível (+ atributo-chave no 1º nível conforme classe conjuradora)`.
  - `RecalculateSheetResources(sheet, refillCurrent)` atualiza PV máximo, PM máximo e deslocamento de forma instantânea ao trocar classe, raça, nível ou atributo na ficha.

---

## 3. Recursos Vitais, Defesa e Estado de Morrendo — `Tormenta20Models.cs` & `Tormenta20ConditionRules.cs`

### Pontos de Vida (PV), PV Temporário e PM
* **PV (`T20Resource.Health = 0`):** Suporta PV atual, máximo e **PV Temporário** (sempre absorvido primeiro pelo `ResourcePool` do Core C++).
* **PM (`T20Resource.Mana = 1`):** Consumido por poderes de classe, habilidades na ficha e magias com aprimoramentos.

### Estado de Morrendo (T20 JdA) e Limiar de Morte
Implementado em `CharacterSheetData.GetDeathThreshold()` e integrado a `Tormenta20ConditionRules` / `Tormenta20CombatRules`:
* **Para Heróis (`IsEnemy == false`):**
  - Ao chegar a `0 PV` ou menos, mas acima do limiar negativo de morte $-\max(10, \lfloor \text{PV Máx} / 2 \rfloor)$, o herói fica **Inconsciente** e **Sangrando** (`IsBleedingOut = true`, estado `T20HealthState.Morrendo`).
  - No início de cada turno enquanto estiver sangrando (`ProcessStartOfTurnBleeding`), rola **Constituição (`1d20 + CON >= CD 15`)**: se passar, estabiliza (`Estabilizado`); se falhar, sofre `1d6` de dano de PV.
  - Ao atingir ou ultrapassar o limiar negativo $-\max(10, \lfloor \text{PV Máx} / 2 \rfloor)$, o estado passa a **Morto** (`T20HealthState.Morto`).
* **Para Inimigos / Ameaças (`IsEnemy == true`):**
  - Seguem a regra padrão de ameaças: `GetDeathThreshold() == 0` (ao chegar a `0 PV` são imediatamente derrotados/abatidos em combate).

### Defesa (DEF)
Calculada dinamicamente em `CharacterSheetData.GetDefense(session, combatantId)`:
* **Heróis:** $\text{Defesa} = 10 + (\text{DES} + \Delta\text{DES}_{\text{Cond}}) + \text{Armadura} + \text{Escudo} + \text{Poderes} + \text{Outros} + \Delta\text{DEF}_{\text{Cond}}$.
  - *Estilo de Uma Arma* (`estilo_uma_arma`): `+2` na Defesa empunhando arma em uma mão sem escudo.
  - *Encouraçado* (`encouracado`): `+2` na Defesa vestindo armadura pesada (`DefenseBonus >= 5`).
* **Ameaças com Defesa Fixa (`FixedDefense > 0`):** Utilizam `FixedDefense + modificadores de condições + bônus misc`, respeitando os blocos oficiais do bestiário de T20.

---

## 4. Ficha Própria de Inimigos & Bestiário — `Tormenta20Bestiary.cs`

Para representar ameaças fiéis ao livro básico sem precisar simular níveis de classe de jogador, o sistema conta com **8 presets canônicos** (`Tormenta20Bestiary.Presets`) + criação livre (`CreateBlankEnemy`):

| Chave do Preset | Nome da Ameaça | ND (CR) | Tipo / Papel |
| :--- | :--- | :---: | :--- |
| `goblin_salteador` | Goblin Salteador | `1/4` | Humanoide / Lacaio |
| `bandido_estradas` | Bandido de Estradas | `1/2` | Humanoide / Lacaio |
| `esqueleto_guerreiro` | Esqueleto Guerreiro | `1/2` | Morto-vivo / Lacaio |
| `lobo_atroz` | Lobo Atroz | `1` | Animal / Solo |
| `orc_berserker` | Orc Berserker | `2` | Humanoide / Bruto |
| `cultista_aharadak` | Cultista da Tormenta | `3` | Humanoide / Especial |
| `golem_ferro` | Golem de Ferro | `5` | Construto / Chefe |
| `dragao_jovem` | Dragão Jovem | `7` | Monstro / Chefe |

* **Campos dedicados de Ameaça:** `ChallengeRating`, `CreatureType`, `CreatureSize`, `ThreatRole`, `FixedDefense`, `DamageReduction` (RD automática no `EncounterSession`), `SpeedMetersOverride`, `ThreatAttackBonus`, e overrides específicos de perícias-chave (`InitiativeOverride`, `PerceptionOverride`, `FortitudeOverride`, `ReflexOverride`, `WillOverride`).
* **Ataque de Ameaças (`AttackBonus`):** Armas naturais/ataques de ameaças com `AttackBonus > 0` (ex.: `Mordida +14`) utilizam diretamente esse modificador total de ataque sem somar Força duplicada nem exigir treinamento em Luta/Pontaria.

---

## 5. Capacidade de Carga por Espaços e Sobrecarga — `Tormenta20Models.cs`

$$\text{Espaços Máximos} = \begin{cases} 10 + (2 \times \text{FOR}), & \text{se } \text{FOR} \ge 0 \\ 10 + \text{FOR}, & \text{se } \text{FOR} < 0 \end{cases}$$

Se a soma de espaços carregados ultrapassar o limite normal (`IsOverburdened()`), o personagem sofre penalidade de armadura adicional de `–5` e redução de deslocamento.

---

## 6. Sistema de Perícias — `Tormenta20SkillRules.cs`

Implementa as **29 perícias oficiais** de Tormenta20 Jogo do Ano:
$$\text{Modificador de Perícia} = \left\lfloor \frac{\text{Nível}}{2} \right\rfloor + \text{Atributo} + \text{Bônus de Treino} + \text{Penalidade de Armadura} + \text{Condições} + \text{Outros}$$

* **Bônus de Treinamento:** Níveis 1–6 (`+2`), Níveis 7–14 (`+4`), Níveis 15–20 (`+6`).
* **Somente Treinadas (`TrainedOnly`):** Exigem treinamento prévio para rolar (como *Adestramento*, *Conhecimento*, *Cura*, *Guerra*, *Jogatina*, *Ladinagem*, *Misticismo*, *Nobreza*, *Ofício*, *Pilotagem* e *Religião*).
* **Penalidade de Armadura (`ArmorPenaltyApplies`):** Aplicada automaticamente em *Acrobacia*, *Furtividade* e *Ladinagem*.
* **Overrides de Ameaças:** Inimigos podem definir valores fixos diretos para *Iniciativa*, *Percepção*, *Fortitude*, *Reflexos* e *Vontade*.

---

## 7. Economia de Ações e Árvore de Poderes — `Tormenta20ActionRules.cs` & `Tormenta20AbilityTreeRules.cs`

### Economia de Ações (`Tormenta20ActionRules`)
Por turno: **Reações ilimitadas (`5`)**, **Ações Livres ilimitadas (`10`)**, **1 Ação de Movimento (`20`)**, **1 Ação Padrão (`30`)** e **Ação Completa (`40`)** (consome `Padrão + Movimento`).
* **Ordem de Execução (`PriorityQueue`):** $\text{Reação} \rightarrow \text{Livre} \rightarrow \text{Movimento} \rightarrow \text{Padrão} \rightarrow \text{Completa}$.
* **Fallback Unidirecional:** A **Ação Padrão** pode ser gasta para executar uma segunda **Ação de Movimento**, nunca o inverso.

### Árvore N-ária de Poderes (`Tormenta20AbilityTreeRules`)
Estrutura **5 ramos / 17 poderes canônicos**:
1. **Esgrima (DES):** *Estilo de Uma Arma* $\rightarrow$ *Ataque Preciso* $\rightarrow$ *Finta Aprimorada*.
2. **Brutalidade (FOR):** *Ataque Poderoso* $\rightarrow$ *Trespassar* ($\rightarrow$ *Golpe Demolidor*) e *Quebrar Aprimorado*.
3. **Bastião (CON):** *Encouraçado* $\rightarrow$ *Inexpugnável* $\rightarrow$ *Fanático*.
4. **Arcanismo (INT/CAR):** *Magia Ilimitada* $\rightarrow$ *Surto Arcano* ($\rightarrow$ *Arcano de Batalha*) e *Magia Acelerada*.
5. **Arquearia (DES):** *Estilo de Disparo* $\rightarrow$ *Disparo Preciso* $\rightarrow$ *Disparo Rápido*.

---

## 8. Combate, Críticos e RD — `Tormenta20CombatRules.cs`

`Tormenta20CombatRules.ResolveWeaponAttack(...)` executa:
1. **Teste de Ataque (`1d20 + Luta/Pontaria` ou `AttackBonus` de ameaça):** `20` natural sempre acerta; `1` natural sempre erra.
2. **Contexto Tático (`TacticalAttackContext`):** Soma `+2` no ataque ao flanquear o alvo e aplica `+5` na Defesa do alvo se houver Cobertura Leve na Linha de Visão.
3. **Crítico Exclusivo nos Dados Base:** Interpreta `"19/x2"`, `"18/x3"`, `"20/x2"` (com bônus de *Ataque Preciso* quando aplicável). No crítico, **apenas os dados base da arma são multiplicados**, somando modificador de atributo e bônus fixos uma única vez.
4. **Pipeline de Dano no Core:** Envia o total a `session.DealDamage(...)`, descontando RD configurada, vulnerabilidades/resistências e PV Temporário, e atualizando o Estado de Morrendo.

---

## 9. Catálogo de 14 Condições Canônicas — `Tormenta20ConditionRules.cs`

Integra **14 condições oficiais** ao `ConditionTracker` e aos stats do Core:
* **Abalado** (`–2` em perícias; receber *Abalado* novamente escala para **Apavorado** com `–5`).
* **Apavorado** (`–5` em perícias e restrição de aproximação).
* **Caído** (`–5` em ataques corpo a corpo e `–5` na Defesa contra corpo a corpo).
* **Cego** (Desprevenido e Lento; `–5` em perícias de Força/Destreza).
* **Desprevenido** (`–5` na Defesa e em Reflexos).
* **Enjoado** (apenas 1 Ação Padrão **ou** 1 de Movimento por turno).
* **Fraco** (`–2` em FOR, DES e CON).
* **Imóvel** (deslocamento `0m`).
* **Inconsciente** (Indefeso `–10` na Defesa e sem ações).
* **Lento** (deslocamento pela metade e bloqueio de Ações Completas).
* **Ofuscado** (`–2` em ataques e Percepção).
* **Sangrando** (`CON CD 15` no início do turno para estancar ou perde `1d6` PV).
* **Sobrecarregado** (`–5` de penalidade de armadura e `-3m` de deslocamento).
* **Vulnerável** (`–2` na Defesa).

---

## 10. Magias e Descanso — `Tormenta20MagicRules.cs` & `Tormenta20RestRules.cs`

* **Limite de PM (`Tormenta20MagicRules`):** O custo total de uma magia (base + aprimoramentos cumulativos) não pode exceder o **Nível do Personagem** (ou $\text{Nível} + \text{Atributo-Chave}$ com *Magia Ilimitada*).
* **Recuperação por Descanso (`Tormenta20RestRules`):**
  - *Ruim*: $\max(1, \lfloor \text{Nível}/2 \rfloor)$ PV e PM.
  - *Normal*: $1 \times \text{Nível}$ PV e PM.
  - *Confortável*: $2 \times \text{Nível}$ PV e PM.
  - *Luxuoso*: $3 \times \text{Nível}$ PV e PM.

---

## 11. Regras Táticas em Grade — `Tormenta20TacticalRules.cs`

* **Escala e Diagonais (`1q = 1,5m`):** Medição oficial **1-2-1-2** (primeira diagonal custa 1 quadrado, segunda custa 2 quadrados).
* **Deslocamentos e Custos:** Deslocamento racial padrão (ex.: `9m` = 6q; Anão `6m` = 4q; Sílfide/velocidades customizadas via `SpeedMetersOverride`), ajuste automático de *Sobrecarregado* (`-3m`), *Lento* (metade), *Imóvel* (`0m`) e poder *Fanático* (anula penalidade de armadura pesada). Terreno Difícil custa `2x`; **Correr** dobra o alcance com Ação Completa.
* **Pathfinding & Alcance:** Dijkstra (`ComputeReachableCells`) e A\* (`FindShortestPath`) respeitando paredes e ocupação de combatentes.
* **Linha de Visão, Cobertura e Flanqueamento:** Raycasting Supercover avaliando bloqueio de visão (`BlocksSight`), **Cobertura Leve (`+5 Defesa`)** e **Flanqueamento (`+2 Ataque` quando há aliado posicionado em célula oposta/adjacente ao alvo)**.
* **Arenas & Névoa de Guerra:** **4 presets de arena** em `T20ArenaPreset` + rastreamento *Unseen / Explored / Visible* por visão aliada em `TacticalFogTracker`.
