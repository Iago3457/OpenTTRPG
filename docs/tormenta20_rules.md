# Regras de Tormenta20 (Edição Jogo do Ano) no OpenTTRPG

Este documento descreve como as regras canônicas de **Tormenta20 — Edição Jogo do Ano (T20 JdA)** foram modeladas no pacote modular C# (`bindings/csharp/Tormenta20/`) e integradas ao motor agnóstico em C++17 do **OpenTTRPG**.

---

## 1. Atributos Canônicos (Modificador Direto) — `Tormenta20Models.cs`

Diferente do d20 clássico (que utilizava valores de 10 a 18 e calculava `(valor - 10) / 2`), o **Tormenta20 Jogo do Ano** adota o padrão moderno onde **o valor do atributo é o próprio modificador**:

* **Força (FOR):** Dano corpo a corpo, testes de Luta/Atletismo e capacidade de carga (Espaços).
* **Destreza (DES):** Defesa, Iniciativa, testes de Pontaria, Acrobacia, Furtividade e Reflexos.
* **Constituição (CON):** Pontos de Vida (PV), testes de Fortitude e resistência a Sangramento.
* **Inteligência (INT):** Perícias treinadas, magias arcanas e testes de Misticismo/Conhecimento.
* **Sabedoria (SAB):** Percepção, Vontade, Cura e conjuração divina.
* **Carisma (CAR):** Interação social (Enganação, Diplomacia, Intimidação) e conjuração de Feiticeiros/Bardos.

> **Compatibilidade Automática:** `Attribute.NormalizeT20Value(int)` aceita tanto o modificador direto de T20 JdA (`+3`, `-1`) quanto converte valores legados $\ge 10$ automaticamente (`16` $\rightarrow$ `+3`).

---

## 2. Recursos Vitais e Defesa — `Tormenta20Models.cs`

### Pontos de Vida (PV) e Pontos de Mana (PM)
* **PV (`T20Resource.Health = 0`):** Suporta PV Atual, PV Máximo e **PV Temporário** (sempre absorvido primeiro pelo `ResourcePool` do Core C++ antes de descontar o PV real).
* **PM (`T20Resource.Mana = 1`):** Recurso central usado por todas as classes de Arton para poderes de combate (*Ataque Especial*, *Fúria*, *Golpe Divino*) e magias.

### Defesa (DEF)
Calculada dinamicamente em `CharacterSheetData.GetDefense(session, combatantId)`:
$$\text{Defesa} = 10 + (\text{DES} + \Delta\text{DES}_{\text{Condições}}) + \text{Armadura} + \text{Escudo} + \text{Poderes} + \text{Outros} + \Delta\text{DEF}_{\text{Condições}}$$
* **Bônus Automáticos de Poderes na Árvore:**
  - *Estilo de Uma Arma* (`estilo_uma_arma`): `+2` na Defesa se estiver empunhando 1 arma e nenhum escudo.
  - *Encouraçado* (`encouracado`): `+2` na Defesa se estiver vestindo armadura pesada (`DefenseBonus >= 5`).

---

## 3. Capacidade de Carga por Espaços e Sobrecarga — `Tormenta20Models.cs`

Tormenta20 Jogo do Ano utiliza o sistema de **Espaços**:
* **Limite Normal de Carga:**
  $$\text{Espaços Máximos} = \begin{cases} 10 + (2 \times \text{FOR}), & \text{se } \text{FOR} \ge 0 \\ 10 + \text{FOR}, & \text{se } \text{FOR} < 0 \end{cases}$$
* **Sobrecarga (`IsOverburdened()`):** Se os itens equipados/carregados ultrapassarem o limite normal (até o dobro), o personagem fica **Sobrecarregado**, sofrendo penalidade de armadura adicional de `–5` e redução de deslocamento.

---

## 4. Sistema de Perícias — `Tormenta20SkillRules.cs`

Implementa as **30 perícias oficiais** de Tormenta20 Jogo do Ano:
$$\text{Modificador de Perícia} = \left\lfloor \frac{\text{Nível}}{2} \right\rfloor + \text{Atributo} + \text{Bônus de Treinamento} + \text{Penalidade de Armadura} + \text{Condições} + \text{Outros}$$

* **Bônus de Treinamento por Patamar:**
  - Níveis 1 a 6 (*Iniciante*): `+2`
  - Níveis 7 a 14 (*Veterano*): `+4`
  - Níveis 15 a 20 (*Campeão*): `+6`
* **Perícias Somente Treinadas (`TrainedOnly`):** *Adestramento*, *Conhecimento*, *Cura*, *Guerra*, *Jogatina*, *Ladinagem*, *Misticismo*, *Nobreza*, *Ofício*, *Pilotagem* e *Religião* não podem ser roladas sem treinamento.
* **Penalidade de Armadura (`ArmorPenaltyApplies`):** Aplicada automaticamente em *Acrobacia*, *Furtividade* e *Ladinagem*.

---

## 5. Economia de Ações e Fila de Prioridades — `Tormenta20ActionRules.cs`

A cada turno, o combatente recebe:
* **1 Ação Padrão** (`Prioridade 30`)
* **1 Ação de Movimento** (`Prioridade 20`)
* **Ações Livres Ilimitadas** (`Prioridade 10`)
* **Reações Ilimitadas** (`Prioridade 5`)

### Regras Oficiais Modeladas:
1. **Ordem de Execução na Fila de Prioridades (`PriorityQueue`):** Independentemente da ordem em que o jogador clique nas ações durante o planejamento do turno, elas são ordenadas e executadas como:
   $$\text{1º Ações Livres} \longrightarrow \text{2º Ações de Movimento} \longrightarrow \text{3º Ações Padrão} \longrightarrow \text{4º Ações Completas}$$
2. **Conversão Unidirecional (`Padrão -> Movimento`):** Se o personagem já gastou sua Ação de Movimento, pode realizar uma segunda Ação de Movimento consumindo sua Ação Padrão. O inverso é proibido.
3. **Ação Completa:** Consome simultaneamente `1 Ação Padrão + 1 Ação de Movimento` via `EnqueueCompoundAction`.

---

## 6. Resolução de Ataques, Críticos e RD — `Tormenta20CombatRules.cs`

O método `Tormenta20CombatRules.ResolveWeaponAttack(...)` automatiza a regra oficial de combate:
1. **Teste de Ataque:** Rola `1d20 + Luta` (corpo a corpo) ou `1d20 + Pontaria` (à distância) contra a `Defesa` do alvo.
2. **Acerto e Falha Automáticos:** `20 natural` sempre acerta; `1 natural` sempre erra.
3. **Margem de Ameaça e Multiplicador de Crítico:**
   - Interpreta o perfil da arma (`"19/x2"`, `"18/x3"`, `"20/x2"`).
   - Se o atacante possuir o poder *Ataque Preciso*, recebe `+2` na margem de ameaça (ex.: `19` vira `17`) e `+1` no multiplicador de crítico (ex.: `x2` vira `x3`).
   - **Regra de Multiplicação Crítica de T20:** No acerto crítico, **apenas os dados base da arma são multiplicados** (ex.: uma espada longa `1d8` com crítico `x2` rola `2d8`), enquanto o modificador de Força e bônus numéricos fixos (como `+5` de *Ataque Poderoso*) são somados apenas uma vez!
4. **Pipeline de Dano:** O total é enviado a `session.DealDamage(targetId, totalDamage, damageType)`, descontando Redução de Dano (RD), aplicando vulnerabilidades/resistências e absorvendo em PV Temporário antes de reduzir o PV atual.

---

## 7. Condições Canônicas — `Tormenta20ConditionRules.cs`

Contém 13 condições oficiais de Tormenta20 integradas ao `ConditionTracker` do Core:
* **Abalado** (`–2` em perícias; se receber **Abalado** novamente, escala automaticamente para **Apavorado** com `–5`!).
* **Caído** (`–5` em ataques corpo a corpo e `–5` na Defesa contra ataques corpo a corpo).
* **Cego** (`–5` na Defesa e `–2` em testes de Força e Destreza).
* **Desprevenido** (`–5` na Defesa e em Reflexos).
* **Enjoado** (só pode realizar 1 Ação Padrão **ou** 1 de Movimento por turno, nunca ambas).
* **Fraco** (`–2` em Força, Destreza e Constituição).
* **Imóvel** / **Lento** (impede deslocamento ou bloqueia Ações Completas).
* **Ofuscado** (`–2` em ataques e Percepção).
* **Sangrando** (no início do turno exige teste de Constituição `CD 15`; se falhar, perde `1d6` PV; se passar, remove a condição).
* **Sobrecarregado** (`–5` de penalidade de armadura e redução de deslocamento).
* **Vulnerável** (`–2` na Defesa).

---

## 8. Magias, Limite de PM e Descanso — `Tormenta20MagicRules.cs` & `Tormenta20RestRules.cs`

* **Limite de Gasto de PM (`Tormenta20MagicRules`):** O custo total de uma magia (custo base + aprimoramentos escolhidos) não pode ultrapassar o **Nível do Personagem** (ou $\text{Nível} + \text{Atributo-Chave}$ se possuir o poder *Magia Ilimitada*).
* **Recuperação por Descanso (`Tormenta20RestRules`):**
  - **Ruim:** recupera $\max(1, \lfloor \text{Nível}/2 \rfloor)$ PV e PM.
  - **Normal:** recupera $1 \times \text{Nível}$ PV e PM.
  - **Confortável:** recupera $2 \times \text{Nível}$ PV e PM.
  - **Luxuoso:** recupera $3 \times \text{Nível}$ PV e PM.
