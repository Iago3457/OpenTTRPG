# OpenTTRPG
Project originally developed for the AED1 course at UFSCar. The idea is to build a tool featuring a fully integrated tabletop and character sheets that can be easily modified according to the user's needs.

## Escopo
* O escopo deste projeto é implementar um sistema de jogo de RPG de mesa com uma interface gráfica simples e intuitiva, permitindo que os usuários criem suas próprias aventuras e personagens.
* A arquitetura do projeto será baseada em classes e objetos, seguindo os princípios de Orientação a Objetos.
* As principais funcionalidades desenvolvidas na fase 1 do projeto serão:
    * Criação de personagens com atributos, habilidades(Lista) e equipamentos(Lista).
    * Criação de fichas de personagens com informações básicas e avançadas.
    * Sistema de Iniciativa(Fila)
    * Funcionalidade da rolagem de dados(com possibilidade de adicionar modificadores).


## Fase 2 — Tabletop Tático + Interface Visual
* A fase 2 do projeto focará na implementação de uma interface visual completa e um tabletop tático:
    * **Interface visual** para todas as funcionalidades da Fase 1 (fichas, dados, iniciativa).
    * Sistema de movimentação em grid (hex ou quadrado).
    * Sistema de flanco e posicionamento tático.
    * Fog of War e linha de visão.
    * Interação com o mapa (terrenos, obstáculos, cobertura).
    * Integração com as fichas e sistema de iniciativa da Fase 1.

## Stack

### Fase 1 (AED1) — Core do RPG e Estruturas do 1º Grupo
* **Linguagem:** C++ puro
* **Interface:** Terminal / CLI
* **Estruturas de Dados (Grupo 1):** 
    * **Listas Cadastrais:** Gerenciamento de inventário, equipamentos e lista de habilidades dos personagens.
    * **Filas:** Sistema de iniciativa básico (ordem de turnos no combate).
* **Objetivo:** Garantir a nota da Fase 1 da disciplina construindo um "motor" de RPG robusto e independente.

### Fase 2 (AED1) — Tabletop Tático e Estruturas do 2º Grupo
* **Frontend/Interface:** Godot Engine + C#
* **Backend/Core:** C++ (continuando a Fase 1)
* **Estruturas de Dados (Grupo 2 - implementadas no C++):**
    * **Árvores:** Árvore de habilidades (Skill Tree) ou particionamento espacial (QuadTree) para o mapa tático.
    * **Filas de Prioridade:** Sistema de iniciativa avançado (ações rápidas passam na frente).
* **Objetivo:** Criar a interface visual definitiva e o tabletop tático.

### Estratégia de Integração (O Modelo Híbrido)
Para satisfazer a disciplina (C++) e o futuro do projeto (Godot), a arquitetura será dividida em duas camadas:

1. **Backend (C++ Core Library):** Todo o gerenciamento de memória, estruturas de dados (nós, ponteiros) e regras de RPG serão feitos em C++ puro. Isso garante a avaliação acadêmica. Esse código será compilado como uma **biblioteca dinâmica** (`.dll` no Windows, `.so` no Linux).
2. **Frontend (Godot + C#):** O Godot cuidará apenas da parte gráfica (renderização do grid, cliques, botões). O C# usará **P/Invoke (`DllImport`)** para se comunicar com a biblioteca C++, enviando comandos (ex: "Mover personagem", "Rolar ataque") e recebendo os resultados atualizados das estruturas de dados.
3. **Vantagem:** O "motor" do RPG (C++) fica completamente independente da Engine visual, sendo um padrão de arquitetura excelente para o portfólio e facilitando a manutenção a longo prazo.
