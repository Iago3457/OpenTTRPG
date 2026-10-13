#pragma once

#include "openttrpg/data_structures/Queue.hpp"
#include "openttrpg/systems/DiceRoller.hpp"
#include <vector>
#include <string>

namespace openttrpg::systems {

/**
 * Participante ativo em um combate de Tormenta20.
 * Armazena os dados vitais e de iniciativa necessários para a rodada.
 */
struct CombatParticipant {
    int id;
    std::string name;
    int initiative; // Resultado do teste de Iniciativa (1d20 + DES + Bônus)
    int currentHp;  // Pontos de Vida atuais (PV)
    int maxHp;      // Pontos de Vida máximos
    int currentPm;  // Pontos de Mana atuais (PM)
    int maxPm;      // Pontos de Mana máximos
    int defense;    // Defesa de Tormenta20
};

/**
 * Gerenciador de Combate e Iniciativa baseado na estrutura Queue (Fila AED1).
 * 
 * Regra de combate em T20:
 * - A ordem do turno segue a Fila da maior para a menor iniciativa.
 * - Ao final de seu turno, o combatente é re-enfileirado no fim da Fila (FIFO).
 * - Se um combatente chega a 0 PV, fica inconsciente/fora de combate.
 */
class InitiativeSystem {
private:
    ds::Queue<CombatParticipant> turnQueue; // Fila de turnos (FIFO de AED1)
    DiceRoller diceRoller;
    int currentRound;
    int currentTurnNumber;
    bool combatActive;

public:
    InitiativeSystem();

    // Adiciona um combatente avulso na fila
    void addParticipant(const CombatParticipant& participant);

    // Carrega uma lista de combatentes, ordena pela iniciativa de T20 e alimenta a Fila
    void setupInitiativeFromList(std::vector<CombatParticipant> participants);

    // Inicia a batalha
    bool startCombat();

    // Avança para o próximo da fila (dequeue do atual, enqueue no final se ainda vivo)
    bool nextTurn(CombatParticipant& activeParticipant);

    // Aplica dano ao participante que está na frente da fila
    void applyDamageToCurrent(int damage);

    // Consome PM do participante atual para ativar uma habilidade ou magia
    bool spendPmCurrent(int pmAmount);

    // Encerra o combate e limpa a fila
    bool endCombat();

    [[nodiscard]] bool isCombatActive() const;
    [[nodiscard]] int getCurrentRound() const;
    [[nodiscard]] int getCurrentTurnNumber() const;
    [[nodiscard]] std::size_t getParticipantCount() const;
};

} // namespace openttrpg::systems
