# Regras de Tormenta20 (Edição Jogo do Ano) no OpenTTRPG

Este documento descreve como as regras canônicas de **Tormenta20 — Edição Jogo do Ano (T20 JdA)** foram modeladas e integradas ao motor em C++ do **OpenTTRPG**, em conformidade com as exigências pedagógicas da disciplina de **Algoritmos e Estruturas de Dados 1 (AED1 - UFSCar)**.

---

## 1. Atributos Canônicos (Modificador Direto)

Diferente do d20 tradicional (que utilizava valores de 10 a 18 e calculava `(valor - 10) / 2`), o **Tormenta20 Jogo do Ano** adota o padrão moderno onde **o valor do atributo é o próprio modificador**:

* **Força (FOR):** Dano corpo a corpo, testes de Atletismo e capacidade de carga (Espaços).
* **Destreza (DES):** Defesa, Iniciativa, testes de Acrobacia, Furtividade e ataques à distância.
* **Constituição (CON):** Pontos de Vida (PV) e testes de Fortitude.
* **Inteligência (INT):** Quantidade de perícias treinadas, magias conhecidas e testes de Misticismo/Conhecimento.
* **Sabedoria (SAB):** Pontos de Mana (PM) para certas classes divinas, Percepção e Vontade.
* **Carisma (CAR):** Interação social (Enganação, Diplomacia, Intimidação) e conjuração para Arcanistas Feiticeiros / Bardos.

> **Compatibilidade:** O motor do OpenTTRPG aceita tanto o valor direto de T20 (ex: `+3`, `-1`) quanto converte automaticamente valores legados (ex: `16` converte para `+3`).

---

## 2. Recursos Vitais de Arton

### Pontos de Vida (PV)
* Representam a integridade física do herói.
* O sistema possui suporte a **PV Atual**, **PV Máximo** e **PV Temporário** (absorvido primeiro antes de reduzir o PV real).

### Pontos de Mana (PM)
* **O recurso central de Tormenta20:** No T20, praticamente todas as classes usam Mana!
  * Guerreiros usam PM para ativar *Ataque Especial* e *Golpe Pessoal*.
  * Bárbaros usam PM para entrar em *Fúria*.
  * Paladinos gastam PM para desferir *Golpe Divino*.
  * Arcanistas e Clérigos usam PM para conjurar todas as suas magias.
* A ficha gerencia o consumo via `spendPm(int)` e recuperação via `recoverPm(int)`, impedindo gastos quando o personagem não possuir mana suficiente.

### Defesa (DEF)
* Substitui a antiga "Classe de Armadura (CA)".
* Fórmula de cálculo no OpenTTRPG:
  $$\text{Defesa} = 10 + \text{Modificador de Destreza} + \text{Bônus de Armaduras} + \text{Bônus de Escudos} + \text{Outros Bônus}$$
* Itens equipados recalculam a Defesa dinamicamente.

---

## 3. Capacidade de Carga e Inventário por Espaços

Tormenta20 Jogo do Ano aboliu a contagem de peso em quilogramas, utilizando o sistema intuitivo de **Espaços**:

* **Limite de Carga do Personagem (Regra Oficial T20 JdA):**
  $$\text{Espaços Máximos} = 10 + (2 \times \text{Força}) \quad (\text{se } \text{Força} \ge 0) \quad \text{ou} \quad 10 + \text{Força} \quad (\text{se } \text{Força} < 0)$$
* **Tamanhos e Espaços típicos dos Itens:**
  * Itens pequenos / leves: 0 ou 1 espaço.
  * Armas de uma mão: 1 espaço.
  * Armas de duas mãos / Armaduras pesadas: 2 ou mais espaços.
* **Sobrecarga:** Um personagem pode carregar até o dobro ($2\times$) do seu limite. Caso ultrapasse o limite normal (ficando até o dobro), adquire a condição **Sobrecarregado** (–5 na penalidade de armadura e –3m no deslocamento). É proibido carregar além do dobro.
* Moeda padrão: **Tibares (T$)**, a moeda oficial do Reinado de Arton.

---

## 4. Integração com as Estruturas de Dados (AED1)

### 4.1 Lista Cadastral (`LinkedList<Item>` e `LinkedList<Skill>`)
* **Inventário:** Cada item (arma, armadura, consumível, ferramenta) é armazenado dinamicamente em nós encadeados (`Node<Item>`).
  * Armas registram dano em dados (ex: `1d8`, `2d6`) e margem de crítico (ex: `19/x2`, `x3`).
  * Permite adicionar, remover por índice, equipar e listar com ordenação e paginação.
* **Poderes e Magias:** Habilidades de classe e magias são cadastradas em uma `LinkedList<Skill>`, contendo o custo de PM e o tipo de ação (`Ação Padrão`, `Ação de Movimento`, `Ação Livre` ou `Reação`).

### 4.2 Fila de Iniciativa (`Queue<CombatParticipant>`)
* O teste de Iniciativa em T20 é rolado como:
  $$\text{Iniciativa} = 1\text{d}20 + \text{Destreza}$$
* Os combatentes são ordenados decrescentemente pela iniciativa e inseridos na **Fila (FIFO)**.
* No início de cada turno:
  1. O combatente da frente da fila é retirado via `dequeue()` para agir.
  2. Ele pode realizar ações, rolar testes e **gastar PM** para ativar poderes.
  3. Se ainda estiver com vida (`PV > 0`), ele é re-enfileirado no fim da fila (`enqueue()`).
  4. Caso seus PVs cheguem a zero, ele cai inconsciente e sai da fila de turnos.
  5. Cada volta completa incrementa a **Rodada de Combate**.

---

## 5. Rolagem de Dados e Regras de Crítico no T20

O módulo `DiceRoller` implementa as regras específicas de Tormenta20:
* **20 Natural:** Considerado automaticamente um **Acerto Crítico** em testes de ataque e resistências.
* **1 Natural:** Considerado automaticamente uma **Falha Crítica**.
* **Dano Crítico:** Quando um ataque é crítico, o multiplicador (ex: $\times 2$, $\times 3$) multiplica a quantidade de dados rolados (ex: uma arma de $1\text{d}8$ com crítico $\times 2$ rola $2\text{d}8$ de dano).
