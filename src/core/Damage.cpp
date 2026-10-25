#include "openttrpg/core/Damage.hpp"

#include <algorithm>
#include <stdexcept>

namespace openttrpg::core {

// ---------------------------------------------------------------------------
// DamageProfile
// ---------------------------------------------------------------------------

DamageTrait& DamageProfile::traitFor(DamageTypeId type) {
    DamageTrait* existing = traits_.find_if([type](const DamageTrait& t) { return t.type == type; });
    if (existing != nullptr) {
        return *existing;
    }
    DamageTrait trait;
    trait.type = type;
    traits_.push_back(trait);
    return traits_.get(traits_.size() - 1);
}

void DamageProfile::setResponse(DamageTypeId type, DamageResponse response) {
    traitFor(type).response = response;
}

void DamageProfile::setReduction(DamageTypeId type, int amount) {
    if (amount < 0) {
        throw std::invalid_argument("Redução de dano não pode ser negativa.");
    }
    traitFor(type).flatReduction = amount;
}

void DamageProfile::setGlobalReduction(int amount) {
    if (amount < 0) {
        throw std::invalid_argument("Redução de dano não pode ser negativa.");
    }
    globalReduction_ = amount;
}

void DamageProfile::setOrder(DamageOrder order) {
    order_ = order;
}

DamageResponse DamageProfile::responseTo(DamageTypeId type) const {
    const DamageTrait* t = traits_.find_if([type](const DamageTrait& tr) { return tr.type == type; });
    return t != nullptr ? t->response : DamageResponse::Normal;
}

int DamageProfile::reductionFor(DamageTypeId type) const {
    const DamageTrait* t = traits_.find_if([type](const DamageTrait& tr) { return tr.type == type; });
    return globalReduction_ + (t != nullptr ? t->flatReduction : 0);
}

int DamageProfile::globalReduction() const { return globalReduction_; }
DamageOrder DamageProfile::order() const { return order_; }

bool DamageProfile::removeTrait(DamageTypeId type) {
    const std::size_t idx = traits_.find_index([type](const DamageTrait& t) { return t.type == type; });
    return idx != ds::LinkedList<DamageTrait>::npos && traits_.remove_at(idx);
}

void DamageProfile::clear() {
    traits_.clear();
    globalReduction_ = 0;
    order_ = DamageOrder::ReductionFirst;
}

const ds::LinkedList<DamageTrait>& DamageProfile::traits() const {
    return traits_;
}

// ---------------------------------------------------------------------------
// Funções de dano
// ---------------------------------------------------------------------------

int applyDamageResponse(int amount, DamageResponse response) {
    switch (response) {
        case DamageResponse::Resistant:  return amount / 2;
        case DamageResponse::Vulnerable: return amount * 2;
        case DamageResponse::Immune:     return 0;
        case DamageResponse::Normal:
        default:                         return amount;
    }
}

DamageResult calculateDamage(const DamageInstance& damage, const DamageProfile& profile) {
    if (damage.amount < 0) {
        throw std::invalid_argument("Quantidade de dano não pode ser negativa.");
    }

    DamageResult result;
    result.rawAmount = damage.amount;
    result.response = damage.ignoreResponse ? DamageResponse::Normal : profile.responseTo(damage.type);
    const int reduction = damage.ignoreReduction ? 0 : profile.reductionFor(damage.type);

    if (profile.order() == DamageOrder::ReductionFirst) {
        const int afterReduction = std::max(0, damage.amount - reduction);
        result.reducedBy = damage.amount - afterReduction;
        result.finalAmount = applyDamageResponse(afterReduction, result.response);
    } else {
        const int afterMultiplier = applyDamageResponse(damage.amount, result.response);
        result.finalAmount = std::max(0, afterMultiplier - reduction);
        result.reducedBy = afterMultiplier - result.finalAmount;
    }
    return result;
}

DamageResult applyDamage(ResourcePool& pool, const DamageInstance& damage, const DamageProfile& profile) {
    DamageResult result = calculateDamage(damage, profile);
    const bool wasAlive = !pool.isDepleted();

    const ResourceChange change = pool.reduce(result.finalAmount);
    result.absorbedByTemp = change.absorbedByTemp;
    result.appliedToCurrent = change.appliedToCurrent;
    result.overflow = change.overflow;
    result.depleted = wasAlive && pool.isDepleted();
    return result;
}

DamageResult applyDamage(ResourcePool& pool, int amount) {
    static const DamageProfile kNoDefenses;
    DamageInstance damage;
    damage.amount = amount;
    return applyDamage(pool, damage, kNoDefenses);
}

int applyHealing(ResourcePool& pool, int amount) {
    if (amount < 0) {
        throw std::invalid_argument("Quantidade de cura não pode ser negativa.");
    }
    return pool.restore(amount);
}

} // namespace openttrpg::core
