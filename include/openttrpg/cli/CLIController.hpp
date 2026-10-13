#pragma once

#include "openttrpg/models/Character.hpp"
#include "openttrpg/systems/DiceRoller.hpp"
#include "openttrpg/systems/InitiativeSystem.hpp"
#include <vector>

namespace openttrpg::cli {

/**
 * Controlador principal da interface de linha de comando (CLI) adaptado
 * para o sistema de regras Tormenta20 (Edição Jogo do Ano).
 * 
 * Orquestra:
 * - Fichas com PV, PM, Defesa, Carga por Espaços e Tibares (T$).
 * - Listas Cadastrais (AED1) para Inventário e Habilidades/Magias com custo de PM.
 * - Testes de d20 com detecção de 20 Natural (Crítico!) e 1 Natural (Falha Crítica!).
 * - Combate com Fila de Iniciativa de AED1 (FIFO).
 */
class CLIController {
private:
    std::vector<models::Character> characters;
    systems::DiceRoller diceRoller;
    systems::InitiativeSystem initiativeSystem;
    int nextCharacterId;

    void printHeader() const;
    void showMainMenu() const;
    void handleCreateCharacter();
    void handleListCharacters() const;
    void handleManageCharacter();
    void handleDiceRoll();
    void handleInitiativeCombat();

public:
    // Inicializa a CLI com heróis de exemplo de Tormenta20 (Guerreiro e Arcanista)
    CLIController();

    // Loop principal da interface de terminal
    void run();
};

} // namespace openttrpg::cli
