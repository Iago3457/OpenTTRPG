#pragma once

#include "openttrpg/core/Combatant.hpp"
#include "openttrpg/core/Dice.hpp"
#include "openttrpg/core/InitiativeTracker.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"
#include <cstdint>
#include <string>

namespace openttrpg::core {

/**
 * @brief Registro de uma condição que expirou durante a passagem de turno.
 */
struct ConditionEvent {
    EntityId entityId = kInvalidId;
    std::string key;
};

/**
 * @brief Encontro/Combate genérico: dono dos combatentes, da ordem de iniciativa e dos dados.
 *
 * É a fachada principal do Core (e o que a DLL expõe para o C#). Todas as ações são feitas
 * por ID de alvo, nunca "no primeiro da fila", evitando aplicar dano/custo na entidade errada.
 *
 * Automatizações (podem ser desligadas):
 * - autoSkipDefeated: combatentes com recurso vital <= 0 são pulados na ordem (e voltam se curados).
 * - autoTickConditions: condições têm a duração decrementada em início/fim de turno e fim de rodada.
 */
class Encounter {
public:
    Encounter();                             // Semente aleatória
    explicit Encounter(std::uint32_t seed);  // Semente fixa (replays, testes, multiplayer)

    // ----- Combatentes -----
    EntityId addCombatant(const std::string& name, int initiativeModifier = 0, int maxHealth = 0, int team = 0);
    bool removeCombatant(EntityId id); // Remove também da ordem de iniciativa
    Combatant* find(EntityId id);
    [[nodiscard]] const Combatant* find(EntityId id) const;
    [[nodiscard]] std::size_t combatantCount() const;
    [[nodiscard]] const ds::LinkedList<Combatant>& combatants() const;

    // ----- Iniciativa -----
    // Rola [count]d[sides] + modificador de iniciativa e insere/atualiza na ordem. Retorna o total.
    int rollInitiative(EntityId id, int diceCount = 1, int diceSides = 20);
    // Rola para TODOS os combatentes do encontro
    void rollInitiativeForAll(int diceCount = 1, int diceSides = 20);
    // Valor informado manualmente (ex: jogador rolou dado físico). Insere ou reposiciona.
    bool setInitiative(EntityId id, int value);
    bool setInitiativeOrder(InitiativeOrder order);

    // ----- Fluxo de combate -----
    bool startCombat();
    EntityId nextTurn();   // Encerra o turno atual e passa para o próximo ativo
    void endCombat();      // Limpa a ordem (os combatentes permanecem)
    [[nodiscard]] bool isCombatActive() const;
    [[nodiscard]] EntityId currentTurn() const;
    [[nodiscard]] int round() const;
    InitiativeTracker& tracker();
    [[nodiscard]] const InitiativeTracker& tracker() const;

    // Condições expiradas na última chamada de startCombat()/nextTurn()
    [[nodiscard]] const ds::LinkedList<ConditionEvent>& lastExpiredConditions() const;

    // ----- Ações (sempre por ID de alvo) -----
    // Lançam std::out_of_range se o alvo não existir
    DamageResult dealDamage(EntityId target, const DamageInstance& damage);
    DamageResult dealDamage(EntityId target, int amount, DamageTypeId type = kUntypedDamage);
    int heal(EntityId target, int amount);
    bool spendResource(EntityId target, ResourceId resource, int amount);
    int restoreResource(EntityId target, ResourceId resource, int amount);
    bool addCondition(EntityId target, const Condition& condition, StackPolicy policy = StackPolicy::Refresh);
    bool removeCondition(EntityId target, const std::string& key);

    // ----- Estado geral -----
    [[nodiscard]] bool isTeamDefeated(int team) const; // true se o time existe e todos estão fora
    [[nodiscard]] int standingTeamCount() const;       // Quantos times ainda têm alguém de pé

    // ----- Configuração -----
    void setAutoSkipDefeated(bool enabled);
    void setAutoTickConditions(bool enabled);
    [[nodiscard]] bool autoSkipDefeated() const;
    [[nodiscard]] bool autoTickConditions() const;

    DiceEngine& dice();

private:
    ds::LinkedList<Combatant> combatants_;
    InitiativeTracker tracker_;
    DiceEngine dice_;
    EntityId nextId_ = 1;
    bool autoSkipDefeated_ = true;
    bool autoTickConditions_ = true;
    ds::LinkedList<ConditionEvent> lastExpired_;

    Combatant& require(EntityId id);
    int rollInitiativeFor(Combatant& combatant, int diceCount, int diceSides);
    void syncActiveFlags();
    void tickConditions(Combatant& combatant, TickTiming timing);
};

} // namespace openttrpg::core
