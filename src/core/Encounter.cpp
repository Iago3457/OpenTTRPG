#include "openttrpg/core/Encounter.hpp"

#include <stdexcept>

namespace openttrpg::core {

Encounter::Encounter() = default;

Encounter::Encounter(std::uint32_t seed) : dice_(seed) {}

// ---------------------------------------------------------------------------
// Auxiliares internos
// ---------------------------------------------------------------------------

Combatant& Encounter::require(EntityId id) {
    Combatant* c = find(id);
    if (c == nullptr) {
        throw std::out_of_range("Combatente com ID " + std::to_string(id) + " não encontrado.");
    }
    return *c;
}

int Encounter::rollInitiativeFor(Combatant& combatant, int diceCount, int diceSides) {
    const DiceRollResult roll = dice_.roll(diceCount, diceSides, combatant.initiativeModifier());
    if (tracker_.contains(combatant.id())) {
        tracker_.setInitiative(combatant.id(), roll.total, combatant.initiativeModifier());
    } else {
        tracker_.add(combatant.id(), roll.total, combatant.initiativeModifier());
    }
    return roll.total;
}

void Encounter::syncActiveFlags() {
    if (!autoSkipDefeated_) {
        return;
    }
    combatants_.for_each([this](const Combatant& c, std::size_t) {
        tracker_.setActive(c.id(), !c.isDefeated());
    });
}

void Encounter::tickConditions(Combatant& combatant, TickTiming timing) {
    if (!autoTickConditions_) {
        return;
    }
    ds::LinkedList<std::string> expired;
    combatant.conditions().tick(timing, &expired);
    const EntityId id = combatant.id();
    expired.for_each([this, id](const std::string& key, std::size_t) {
        ConditionEvent ev;
        ev.entityId = id;
        ev.key = key;
        lastExpired_.push_back(ev);
    });
}

// ---------------------------------------------------------------------------
// Combatentes
// ---------------------------------------------------------------------------

EntityId Encounter::addCombatant(const std::string& name, int initiativeModifier, int maxHealth, int team) {
    const EntityId id = nextId_;
    combatants_.push_back(Combatant(id, name, initiativeModifier, maxHealth, team)); // pode lançar
    ++nextId_;
    return id;
}

bool Encounter::removeCombatant(EntityId id) {
    const std::size_t idx = combatants_.find_index([id](const Combatant& c) { return c.id() == id; });
    if (idx == ds::LinkedList<Combatant>::npos) {
        return false;
    }
    tracker_.remove(id);
    combatants_.remove_at(idx);
    return true;
}

Combatant* Encounter::find(EntityId id) {
    return combatants_.find_if([id](const Combatant& c) { return c.id() == id; });
}

const Combatant* Encounter::find(EntityId id) const {
    return combatants_.find_if([id](const Combatant& c) { return c.id() == id; });
}

std::size_t Encounter::combatantCount() const {
    return combatants_.size();
}

const ds::LinkedList<Combatant>& Encounter::combatants() const {
    return combatants_;
}

// ---------------------------------------------------------------------------
// Iniciativa
// ---------------------------------------------------------------------------

int Encounter::rollInitiative(EntityId id, int diceCount, int diceSides) {
    return rollInitiativeFor(require(id), diceCount, diceSides);
}

void Encounter::rollInitiativeForList(const ds::LinkedList<EntityId>& ids, int diceCount, int diceSides) {
    ids.for_each([&](const EntityId& id, std::size_t) {
        rollInitiativeFor(require(id), diceCount, diceSides);
    });
}

void Encounter::rollInitiativeForList(const EntityId* ids, std::size_t count, int diceCount, int diceSides) {
    if (ids == nullptr && count > 0) {
        throw std::invalid_argument("Lista de IDs de iniciativa nula.");
    }
    for (std::size_t i = 0; i < count; ++i) {
        rollInitiativeFor(require(ids[i]), diceCount, diceSides);
    }
}

void Encounter::rollInitiativeForAll(int diceCount, int diceSides) {
    combatants_.for_each_mut([&](Combatant& c, std::size_t) {
        rollInitiativeFor(c, diceCount, diceSides);
    });
}

bool Encounter::setInitiative(EntityId id, int value) {
    Combatant* c = find(id);
    if (c == nullptr) {
        return false;
    }
    if (tracker_.contains(id)) {
        return tracker_.setInitiative(id, value, c->initiativeModifier());
    }
    return tracker_.add(id, value, c->initiativeModifier());
}

bool Encounter::setInitiativeOrder(InitiativeOrder order) {
    return tracker_.setOrder(order);
}

// ---------------------------------------------------------------------------
// Fluxo de combate
// ---------------------------------------------------------------------------

bool Encounter::startCombat() {
    lastExpired_.clear();
    syncActiveFlags();
    if (!tracker_.start()) {
        return false;
    }
    if (Combatant* first = find(tracker_.current())) {
        first->actions().resetTurn();
        tickConditions(*first, TickTiming::TurnStart);
    }
    return true;
}

EntityId Encounter::nextTurn() {
    if (!tracker_.isStarted()) {
        return kInvalidId;
    }
    lastExpired_.clear();

    // Fim do turno de quem estava agindo
    if (Combatant* previous = find(tracker_.current())) {
        previous->actions().clearQueue(false);
        tickConditions(*previous, TickTiming::TurnEnd);
    }

    syncActiveFlags();
    const int roundBefore = tracker_.round();
    const EntityId nextId = tracker_.next();

    // Virou a rodada: efeitos de "fim de rodada" para todos
    if (tracker_.round() != roundBefore) {
        combatants_.for_each_mut([this](Combatant& c, std::size_t) {
            tickConditions(c, TickTiming::RoundEnd);
        });
    }

    if (Combatant* current = find(nextId)) {
        current->actions().resetTurn();
        tickConditions(*current, TickTiming::TurnStart);
    }
    return nextId;
}

void Encounter::endCombat() {
    tracker_.clear();
    lastExpired_.clear();
}

bool Encounter::isCombatActive() const { return tracker_.isStarted(); }
EntityId Encounter::currentTurn() const { return tracker_.current(); }
int Encounter::round() const { return tracker_.round(); }
InitiativeTracker& Encounter::tracker() { return tracker_; }
const InitiativeTracker& Encounter::tracker() const { return tracker_; }

const ds::LinkedList<ConditionEvent>& Encounter::lastExpiredConditions() const {
    return lastExpired_;
}

// ---------------------------------------------------------------------------
// Ações
// ---------------------------------------------------------------------------

DamageResult Encounter::dealDamage(EntityId target, const DamageInstance& damage) {
    Combatant& c = require(target);
    DamageResult result = c.takeDamage(damage);
    if (autoSkipDefeated_) {
        tracker_.setActive(target, !c.isDefeated());
    }
    return result;
}

DamageResult Encounter::dealDamage(EntityId target, int amount, DamageTypeId type) {
    DamageInstance damage;
    damage.amount = amount;
    damage.type = type;
    return dealDamage(target, damage);
}

int Encounter::heal(EntityId target, int amount) {
    Combatant& c = require(target);
    const int healed = c.heal(amount);
    if (autoSkipDefeated_) {
        tracker_.setActive(target, !c.isDefeated()); // curado volta à ordem
    }
    return healed;
}

bool Encounter::spendResource(EntityId target, ResourceId resource, int amount) {
    return require(target).spend(resource, amount);
}

int Encounter::restoreResource(EntityId target, ResourceId resource, int amount) {
    return require(target).restore(resource, amount);
}

bool Encounter::addCondition(EntityId target, const Condition& condition, StackPolicy policy) {
    return require(target).conditions().add(condition, policy);
}

bool Encounter::removeCondition(EntityId target, const std::string& key) {
    return require(target).conditions().remove(key);
}

bool Encounter::canUseAction(EntityId actor, ActionTypeId requiredType, ActionTypeId* outConsumedType) const {
    const Combatant* c = find(actor);
    if (c == nullptr) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return false;
    }
    return c->actions().canUse(requiredType, 1, outConsumedType);
}

bool Encounter::spendAction(EntityId actor, ActionTypeId requiredType, ActionTypeId* outConsumedType) {
    return require(actor).actions().spend(requiredType, 1, outConsumedType);
}

int Encounter::enqueueAction(EntityId actor, ActionTypeId requiredType, const std::string& name, int targetId,
                             ActionTypeId* outConsumedType) {
    return require(actor).actions().enqueueAction(requiredType, name, targetId, outConsumedType);
}

bool Encounter::popNextAction(EntityId actor, QueuedAction& outAction) {
    return require(actor).actions().popNextAction(outAction);
}

// ---------------------------------------------------------------------------
// Estado geral
// ---------------------------------------------------------------------------

bool Encounter::isTeamDefeated(int team) const {
    bool hasMembers = false;
    bool anyStanding = false;
    combatants_.for_each([&](const Combatant& c, std::size_t) {
        if (c.team() == team) {
            hasMembers = true;
            if (!c.isDefeated()) {
                anyStanding = true;
            }
        }
    });
    return hasMembers && !anyStanding;
}

int Encounter::standingTeamCount() const {
    int count = 0;
    combatants_.for_each([&](const Combatant& c, std::size_t idx) {
        if (c.isDefeated()) {
            return;
        }
        // Conta o time apenas na primeira ocorrência de alguém de pé daquele time
        bool seenBefore = false;
        combatants_.for_each([&](const Combatant& other, std::size_t otherIdx) {
            if (otherIdx < idx && !other.isDefeated() && other.team() == c.team()) {
                seenBefore = true;
            }
        });
        if (!seenBefore) {
            ++count;
        }
    });
    return count;
}

// ---------------------------------------------------------------------------
// Configuração
// ---------------------------------------------------------------------------

void Encounter::setAutoSkipDefeated(bool enabled) {
    autoSkipDefeated_ = enabled;
    if (!enabled) {
        combatants_.for_each([this](const Combatant& c, std::size_t) { tracker_.setActive(c.id(), true); });
    } else {
        syncActiveFlags();
    }
}

void Encounter::setAutoTickConditions(bool enabled) { autoTickConditions_ = enabled; }
bool Encounter::autoSkipDefeated() const { return autoSkipDefeated_; }
bool Encounter::autoTickConditions() const { return autoTickConditions_; }

DiceEngine& Encounter::dice() { return dice_; }

} // namespace openttrpg::core
