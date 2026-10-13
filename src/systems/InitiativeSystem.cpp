#include "openttrpg/systems/InitiativeSystem.hpp"
#include <algorithm>

namespace openttrpg::systems {

InitiativeSystem::InitiativeSystem()
    : currentRound(0), currentTurnNumber(0), combatActive(false) {}

void InitiativeSystem::addParticipant(const CombatParticipant& participant) {
    turnQueue.enqueue(participant);
}

void InitiativeSystem::setupInitiativeFromList(std::vector<CombatParticipant> participants) {
    turnQueue.clear();
    // Em Tormenta20, quem tem maior iniciativa age primeiro
    std::sort(participants.begin(), participants.end(), [](const CombatParticipant& a, const CombatParticipant& b) {
        return a.initiative > b.initiative;
    });

    // Enfileira todos os participantes ordenados na Fila de AED1
    for (const auto& p : participants) {
        turnQueue.enqueue(p);
    }
}

bool InitiativeSystem::startCombat() {
    if (turnQueue.is_empty()) {
        return false;
    }
    combatActive = true;
    currentRound = 1;
    currentTurnNumber = 1;
    return true;
}

bool InitiativeSystem::nextTurn(CombatParticipant& activeParticipant) {
    if (!combatActive || turnQueue.is_empty()) {
        return false;
    }

    // Retira o combatente do início da fila para ele agir
    activeParticipant = turnQueue.dequeue();

    // Se ainda possuir Pontos de Vida (PV > 0), ele retorna para o final da fila para as próximas rodadas
    if (activeParticipant.currentHp > 0) {
        turnQueue.enqueue(activeParticipant);
    }

    currentTurnNumber++;
    // Se completamos um ciclo com todos os participantes, avança a rodada de T20
    if (turnQueue.size() > 0 && (currentTurnNumber % turnQueue.size()) == 1) {
        currentRound++;
    }

    return true;
}

void InitiativeSystem::applyDamageToCurrent(int damage) {
    if (!turnQueue.is_empty()) {
        turnQueue.front().currentHp = std::max(0, turnQueue.front().currentHp - damage);
    }
}

bool InitiativeSystem::spendPmCurrent(int pmAmount) {
    if (!turnQueue.is_empty() && turnQueue.front().currentPm >= pmAmount) {
        turnQueue.front().currentPm -= pmAmount;
        return true;
    }
    return false;
}

bool InitiativeSystem::endCombat() {
    combatActive = false;
    turnQueue.clear();
    currentRound = 0;
    currentTurnNumber = 0;
    return true;
}

bool InitiativeSystem::isCombatActive() const {
    return combatActive;
}

int InitiativeSystem::getCurrentRound() const {
    return currentRound;
}

int InitiativeSystem::getCurrentTurnNumber() const {
    return currentTurnNumber;
}

std::size_t InitiativeSystem::getParticipantCount() const {
    return turnQueue.size();
}

} // namespace openttrpg::systems
