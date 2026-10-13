# OpenTTRPG

Projeto originalmente desenvolvido para a disciplina de **Algoritmos e Estruturas de Dados 1 (AED1) da UFSCar**. O objetivo é construir uma ferramenta integrada contendo fichas de personagens modulares, motor de regras de RPG e um tabletop tático expansível.

Nesta primeira fase, o motor foi totalmente adaptado para as regras oficiais de **Tormenta20 — Edição Jogo do Ano (T20 JdA)**, o principal RPG de mesa brasileiro.

---

##  Escopo do Projeto

O projeto adota uma arquitetura orientada a objetos desacoplada, com estruturas de dados dinâmicas implementadas manualmente do zero em **C++17** (sem uso de containers da STL para as estruturas avaliadas).

### Fase 1 (Motor Core em C++ + Terminal CLI — Atual)

* **Modelagem Canônica de Tormenta20 (Jogo do Ano):**
    * **6 Atributos com modificador direto:** Força (FOR), Destreza (DES), Constituição (CON), Inteligência (INT), Sabedoria (SAB) e Carisma (CAR).
    * **Recursos Vitais:** **Pontos de Vida (PV)** (com suporte a PV temporário), **Pontos de Mana (PM)** (recurso universal para habilidades de classe e magias) e **Defesa** ($\text{Defesa} = 10 + \text{DES} + \text{Armadura} + \text{Escudo} + \text{Outros}$).
    * **Capacidade de Carga por Espaços (Regra Oficial T20 JdA):**
        * **Limite normal de carga:** $10 + (2 \times \text{FOR})$ espaços (se $\text{FOR} \ge 0$) ou $10 + \text{FOR}$ espaços (se $\text{FOR} < 0$).
        * **Sobrecarga:** Um personagem pode carregar até o dobro ($2\times$) do seu limite. Caso ultrapasse o limite normal (ficando até o dobro), adquire a condição **Sobrecarregado** (penalidade de armadura –5 e deslocamento –3m). É proibido carregar além do dobro.
        > *Nota:* A menção anterior a $\max(10, \text{FOR} \times 3)$ era um resquício da regra legada pré-JdA (que utilizava contagem de peso em kg, calculada como $\text{FOR} \times 3\text{ kg}$). Na Edição Jogo do Ano, o sistema adota exclusivamente o modelo de **Espaços**.
    * **Economia de Arton:** Gerenciamento de dinheiro na moeda oficial do Reinado, **Tibares (T$)**.
    * **Rolagem de Dados Poliédricos:** Rolador de testes em d20 com detecção automática de **20 Natural** (Acerto Crítico!) e **1 Natural** (Falha Crítica!), além de rolagem de dados de dano com multiplicadores de crítico (ex: $1\text{d}8$ com crítico $\times 2$ rola $2\text{d}8$).

* **Estruturas de Dados Manuais (Grupo 1 de AED1):**
    * **Listas Cadastrais (`LinkedList<T>`):**
        * **Inventário de Equipamentos (`LinkedList<Item>`):** Armazenamento encadeado dinâmico de armas, armaduras, escudos e consumíveis, com controle de dano, margem de crítico, espaços ocupados e bônus de Defesa.
        * **Poderes e Magias (`LinkedList<Skill>`):** Registro de habilidades de classe e magias arcanas/divinas, gerenciando custo em PM e tipo de ação (Padrão, Movimento, Livre, Reação).
    * **Fila de Iniciativa e Combate (`Queue<T>`):**
        * Fila dinâmica FIFO (`Queue<CombatParticipant>`) ordenando combatentes pelo teste de iniciativa ($1\text{d}20 + \text{DES}$).
        * Controle de turnos rotativo: o participante no início age (`dequeue()`), gasta PM, sofre dano e retorna ao final da fila (`enqueue()`) se $\text{PV} > 0$. Participantes caídos são retirados do combate e as rodadas avançam ciclicamente.

---

### Fase 2 (Tabletop Tático + Interface Visual em Godot — Futuro)

* **Interface Gráfica e Frontend:** Desenvolvida em **Godot Engine + C#**.
* **Arquitetura Híbrida Desacoplada (P/Invoke):** O motor de regras e gerenciamento de estruturas em C++ será compilado como biblioteca compartilhada (`.so`/`.dll`) e consumido pelo C# via `DllImport`.
* **Estruturas de Dados do Grupo 2 de AED1 (no Core C++):**
    * **Árvores (QuadTree):** Particionamento espacial para grid tático (quadrado ou hexagonal), cálculo de linha de visão (*Line of Sight*) e *Fog of War*.
    * **Fila de Prioridade (Heap):** Sistema de iniciativa avançado com suporte a interrupções, reações e custos temporais variáveis de ações.

---

## Funcionalidades da Aplicação CLI (Terminal Interativo)

Ao executar a aplicação pelo terminal (`make run`), um menu interativo completo é disponibilizado:

1. **Criar Personagem:** Assistente para criar heróis customizados com raça, classe, origem, divindade, atributos de T20, PV e PM.
2. **Listar e Visualizar Fichas:** Exibição detalhada e formatada das fichas dos heróis (incluindo personagens canônicos pré-carregados como Sir Klunc e Sillith).
3. **Gerenciar Ficha:** Adicionar itens, equipar armaduras e escudos (com recálculo dinâmico da Defesa), cadastrar poderes, consumir/recuperar PM, aplicar dano/cura de PV e gerenciar Tibares (T$).
4. **Rolar Dados:** Rolador integrado para testes em d20 e rolagens de dano de armas com multiplicadores.
5. **Combate de Tormenta20:** Simulação de combate em turnos utilizando a `Queue` de iniciativa, permitindo ataques, gasto de PM e controle de rodadas.

---

##  Estrutura de Diretórios

```text
OpenTTRPG/
├── docs/                               # Especificações técnicas e manuais de regras
│   ├── architecture.md                 # Arquitetura, padrão desacoplado e diagramas Mermaid
│   └── tormenta20_rules.md             # Especificação das regras canônicas de T20 JdA
├── include/                            # Headers (.hpp) da aplicação
│   └── openttrpg/
│       ├── core/                       # Enums e tipos básicos (AttributeType, ItemType, etc.)
│       ├── data_structures/            # Estruturas de dados manuais de AED1 (Node, LinkedList, Queue)
│       ├── models/                     # Entidades de T20 (CharacterSheet, Item, Skill, Attribute)
│       ├── systems/                    # Mecânicas de jogo (DiceRoller, InitiativeSystem)
│       └── cli/                        # Controlador do terminal (CLIController)
├── src/                                # Implementações em C++ (.cpp)
│   ├── models/
│   ├── systems/
│   ├── cli/
│   └── main.cpp                        # Ponto de entrada CLI
├── tests/
│   └── test_data_structures.cpp        # Bateria de testes automatizados de AED1
├── Makefile                            # Script de build principal via g++
├── CMakeLists.txt                      # Script de build alternativo via CMake
└── README.md                           # Visão geral do projeto
```

---

##  Como Compilar e Executar

### Pré-requisitos
* Compilador C++ com suporte ao padrão **C++17** (`g++` versão 9 ou superior).
* Utilitário `make` ou `cmake` (versão 3.15+).

---

### Opção 1: Via Makefile (Recomendado)

O projeto conta com um `Makefile` configurado com `-std=c++17 -Wall -Wextra -O2`:

* **1. Executar os Testes Automatizados (AED1):**
  Compila as estruturas manuais (`LinkedList` e `Queue`) e valida a integração com Tormenta20:
  ```bash
  make test
  ```

* **2. Compilar a Aplicação CLI:**
  Gera o binário executável em `bin/openttrpg_cli`:
  ```bash
  make all
  ```

* **3. Executar o Programa Interativo:**
  Inicia a sessão interativa do OpenTTRPG no terminal:
  ```bash
  make run
  ```

* **4. Limpar Artefatos de Build:**
  Remove os arquivos objeto compilados e os binários gerados:
  ```bash
  make clean
  ```

---

### Opção 2: Via CMake

```bash
mkdir -p build && cd build
cmake ..
make
./openttrpg_cli
```

---

##  Documentação Técnica Adicional

* [Arquitetura e Engenharia de Software](docs/architecture.md): Diagramas de classes Mermaid, arquitetura em camadas e modelo de gerenciamento de memória (RAII / Rule of Five).
* [Regras de Tormenta20 (Jogo do Ano)](docs/tormenta20_rules.md): Especificação detalhada de como cada regra de Tormenta20 JdA foi mapeada para as estruturas em C++.
