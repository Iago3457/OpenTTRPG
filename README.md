# OpenTTRPG

Projeto originalmente desenvolvido para a disciplina de **Algoritmos e Estruturas de Dados 1 (AED1) da UFSCar**. O objetivo é construir uma ferramenta integrada contendo fichas de personagens modulares, motor de regras de RPG e um tabletop tático expansível.

Nesta primeira fase, o motor foi totalmente adaptado para as regras oficiais de **Tormenta20 — Edição Jogo do Ano (T20 JdA)**, o principal RPG de mesa brasileiro.

---

## Escopo do Projeto

* O escopo é implementar um sistema de RPG de mesa robusto com Orientação a Objetos e estruturas de dados dinâmicas implementadas do zero (sem containers da STL para as estruturas avaliadas).
* **Fase 1 (Motor Core em C++ + Terminal CLI):**
    * Modelagem canônica de Tormenta20 Jogo do Ano:
        * 6 Atributos com modificador direto: Força (FOR), Destreza (DES), Constituição (CON), Inteligência (INT), Sabedoria (SAB) e Carisma (CAR).
        * Recursos vitais: **PV (Pontos de Vida)**, **PM (Pontos de Mana)** e **Defesa**.
        * Sistema de Carga por **Espaços** ($\max(10, \text{FOR} \times 3)$) e economia em **Tibares (T$)**.
    * **Listas Cadastrais (Grupo 1 de AED1):**
        * Inventário e equipamentos com dados de dano, margem de crítico (ex: `19/x2`), espaços e bônus de Defesa.
        * Lista de habilidades, poderes gerais e magias com consumo ativo de **PM**.
    * **Sistema de Iniciativa via Fila (Grupo 1 de AED1):**
        * Teste de Iniciativa ($1\text{d}20 + \text{DES}$) ordenando a `Queue` de combate (FIFO).
    * **Rolagem de Dados de Tormenta20:**
        * Rolagem de dados poliédricos com modificadores, detecção de **20 Natural** (Acerto Crítico!) e **1 Natural** (Falha Crítica!), e multiplicadores de dados de dano.

* **Fase 2 (Tabletop Tático + Interface Visual em Godot):**
    * Interface visual completa integrada via **P/Invoke (C# + Godot Engine)** consumindo a biblioteca dinâmica C++ (`.so`/`.dll`).
    * Grid tático (hexagonal ou quadrado) com particionamento espacial (QuadTree) e fila de prioridade para iniciativas complexas (Grupo 2 de AED1).
    * Fog of War, linha de visão e combate tático.

---

## Como Compilar e Executar (Fase 1)

O projeto conta com um `Makefile` configurado para compilar via `g++` com a flag `-std=c++17`:

### 1. Executar os Testes Unitários de AED1
Compila as estruturas de dados manuais (`LinkedList` e `Queue`) e valida a integração com Tormenta20:
```bash
make test
```

### 2. Compilar a Aplicação CLI
Gera o binário executável em `bin/openttrpg_cli`:
```bash
make all
```

### 3. Executar o Programa no Terminal
Inicia a sessão interativa do OpenTTRPG com personagens canônicos pré-carregados:
```bash
make run
```

### 4. Limpar Diretórios de Build
Remove arquivos temporários e binários compilados:
```bash
make clean
```

---

## Documentação Adicional

* [Arquitetura e Engenharia de Software](docs/architecture.md): Diagrama Mermaid de classes, padrão arquitetural desacoplado e modelo de memória.
* [Regras de Tormenta20 (Jogo do Ano)](docs/tormenta20_rules.md): Especificação detalhada de como as regras de T20 JdA foram implementadas no C++.
