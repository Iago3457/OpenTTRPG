#include "openttrpg/core/Combatant.hpp"

#include <stdexcept>
#include <utility>

namespace openttrpg::core {

Combatant::Combatant(EntityId id, std::string name, int initiativeModifier, int maxHealth, int team)
    : id_(id), name_(std::move(name)), initiativeModifier_(initiativeModifier), team_(team) {
    if (id_ == kInvalidId) {
        throw std::invalid_argument("ID de entidade inválido.");
    }
    if (maxHealth < 0) {
        throw std::invalid_argument("Vida máxima não pode ser negativa.");
    }
    if (maxHealth > 0) {
        addResource(kHealthResource, "Vida", maxHealth);
    }
}

EntityId Combatant::id() const { return id_; }
const std::string& Combatant::name() const { return name_; }
int Combatant::initiativeModifier() const { return initiativeModifier_; }
int Combatant::team() const { return team_; }

void Combatant::setName(const std::string& name) { name_ = name; }
void Combatant::setInitiativeModifier(int modifier) { initiativeModifier_ = modifier; }
void Combatant::setTeam(int team) { team_ = team; }

// ----- Recursos -----

ResourcePool& Combatant::addResource(ResourceId id, const std::string& name, int max, int minimum) {
    NamedResource entry;
    entry.id = id;
    entry.name = name;
    entry.pool = ResourcePool(max, minimum);

    NamedResource* existing = resources_.find_if([id](const NamedResource& r) { return r.id == id; });
    if (existing != nullptr) {
        *existing = entry;
        return existing->pool;
    }
    resources_.push_back(entry);
    return resources_.get(resources_.size() - 1).pool;
}

bool Combatant::removeResource(ResourceId id) {
    const std::size_t idx = resources_.find_index([id](const NamedResource& r) { return r.id == id; });
    return idx != ds::LinkedList<NamedResource>::npos && resources_.remove_at(idx);
}

bool Combatant::hasResource(ResourceId id) const {
    return resourceEntry(id) != nullptr;
}

ResourcePool* Combatant::resource(ResourceId id) {
    NamedResource* r = resources_.find_if([id](const NamedResource& e) { return e.id == id; });
    return r != nullptr ? &r->pool : nullptr;
}

const ResourcePool* Combatant::resource(ResourceId id) const {
    const NamedResource* r = resourceEntry(id);
    return r != nullptr ? &r->pool : nullptr;
}

const NamedResource* Combatant::resourceEntry(ResourceId id) const {
    return resources_.find_if([id](const NamedResource& e) { return e.id == id; });
}

const ds::LinkedList<NamedResource>& Combatant::resources() const {
    return resources_;
}

void Combatant::setVitalResource(ResourceId id) { vitalResource_ = id; }
ResourceId Combatant::vitalResource() const { return vitalResource_; }

bool Combatant::spend(ResourceId id, int amount) {
    ResourcePool* pool = resource(id);
    return pool != nullptr && pool->spend(amount);
}

int Combatant::restore(ResourceId id, int amount) {
    ResourcePool* pool = resource(id);
    return pool != nullptr ? pool->restore(amount) : 0;
}

// ----- Dano e cura -----

DamageResult Combatant::takeDamage(const DamageInstance& damage) {
    ResourcePool* vital = resource(vitalResource_);
    if (vital == nullptr) {
        throw std::logic_error("Entidade '" + name_ + "' não possui recurso vital para receber dano.");
    }
    return applyDamage(*vital, damage, damageProfile_);
}

int Combatant::heal(int amount) {
    ResourcePool* vital = resource(vitalResource_);
    if (vital == nullptr) {
        throw std::logic_error("Entidade '" + name_ + "' não possui recurso vital para ser curada.");
    }
    return applyHealing(*vital, amount);
}

// ----- Estado -----

bool Combatant::isDefeated() const {
    if (forcedOut_) {
        return true;
    }
    const ResourcePool* vital = resource(vitalResource_);
    return vital != nullptr && vital->isDepleted();
}

void Combatant::setForcedOut(bool out) { forcedOut_ = out; }
bool Combatant::isForcedOut() const { return forcedOut_; }

DamageProfile& Combatant::damageProfile() { return damageProfile_; }
const DamageProfile& Combatant::damageProfile() const { return damageProfile_; }
ConditionTracker& Combatant::conditions() { return conditions_; }
const ConditionTracker& Combatant::conditions() const { return conditions_; }
ActionEconomy& Combatant::actions() { return actions_; }
const ActionEconomy& Combatant::actions() const { return actions_; }

// ----- Estatísticas / Atributos Genéricos -----

void Combatant::setStat(const std::string& key, int value) {
    if (key.empty()) {
        return;
    }
    if (NamedStat* existing = stats_.find_if([&key](const NamedStat& s) { return s.key == key; })) {
        existing->value = value;
        return;
    }
    stats_.push_back(NamedStat{key, value});
}

int Combatant::stat(const std::string& key, int defaultValue) const {
    const NamedStat* existing = stats_.find_if([&key](const NamedStat& s) { return s.key == key; });
    return existing != nullptr ? existing->value : defaultValue;
}

bool Combatant::hasStat(const std::string& key) const {
    return stats_.find_if([&key](const NamedStat& s) { return s.key == key; }) != nullptr;
}

const ds::LinkedList<NamedStat>& Combatant::stats() const {
    return stats_;
}

// ----- Habilidades / Poderes Concedidos -----

bool Combatant::unlockAbility(const std::string& key) {
    if (key.empty() || hasAbility(key)) {
        return false;
    }
    unlockedAbilities_.push_back(key);
    return true;
}

bool Combatant::revokeAbility(const std::string& key) {
    const std::size_t idx = unlockedAbilities_.find_index([&key](const std::string& k) { return k == key; });
    return idx != ds::LinkedList<std::string>::npos && unlockedAbilities_.remove_at(idx);
}

bool Combatant::hasAbility(const std::string& key) const {
    return unlockedAbilities_.find_if([&key](const std::string& k) { return k == key; }) != nullptr;
}

const ds::LinkedList<std::string>& Combatant::unlockedAbilities() const {
    return unlockedAbilities_;
}

// ----- Histórico de Ações Executadas no Turno Atual -----

void Combatant::recordActionInTurn(const std::string& actionKey) {
    if (!actionKey.empty()) {
        turnActionHistory_.push_back(actionKey);
    }
}

bool Combatant::hasUsedActionInTurn(const std::string& actionKey) const {
    return turnActionHistory_.find_if([&actionKey](const std::string& k) { return k == actionKey; }) != nullptr;
}

void Combatant::clearTurnActionHistory() {
    turnActionHistory_.clear();
}

const ds::LinkedList<std::string>& Combatant::turnActionHistory() const {
    return turnActionHistory_;
}

} // namespace openttrpg::core
