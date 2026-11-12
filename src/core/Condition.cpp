#include "openttrpg/core/Condition.hpp"

#include <algorithm>
#include <stdexcept>

namespace openttrpg::core {

namespace {

int mergedDuration(int a, int b) {
    if (a == kPermanentDuration || b == kPermanentDuration) {
        return kPermanentDuration;
    }
    return std::max(a, b);
}

} // namespace

bool ConditionTracker::add(const Condition& condition, StackPolicy policy) {
    if (condition.key.empty()) {
        throw std::invalid_argument("Condição precisa de uma chave (key) não vazia.");
    }
    if (condition.duration == 0 || condition.duration < kPermanentDuration) {
        throw std::invalid_argument("Duração da condição deve ser > 0 ou -1 (permanente).");
    }
    if (condition.intensity < 1) {
        throw std::invalid_argument("Intensidade da condição deve ser >= 1.");
    }

    Condition* existing = get(condition.key);
    if (existing == nullptr) {
        conditions_.push_back(condition);
        return true;
    }

    switch (policy) {
        case StackPolicy::Ignore:
            return false;
        case StackPolicy::Replace:
            *existing = condition;
            return true;
        case StackPolicy::Stack:
            existing->intensity += condition.intensity;
            existing->duration = mergedDuration(existing->duration, condition.duration);
            existing->sourceId = condition.sourceId;
            return true;
        case StackPolicy::Refresh:
        default:
            existing->intensity = std::max(existing->intensity, condition.intensity);
            existing->duration = mergedDuration(existing->duration, condition.duration);
            existing->sourceId = condition.sourceId;
            return true;
    }
}

bool ConditionTracker::remove(const std::string& key) {
    const std::size_t idx = conditions_.find_index([&key](const Condition& c) { return c.key == key; });
    return idx != ds::LinkedList<Condition>::npos && conditions_.remove_at(idx);
}

bool ConditionTracker::has(const std::string& key) const {
    return get(key) != nullptr;
}

const Condition* ConditionTracker::get(const std::string& key) const {
    return conditions_.find_if([&key](const Condition& c) { return c.key == key; });
}

Condition* ConditionTracker::get(const std::string& key) {
    return conditions_.find_if([&key](const Condition& c) { return c.key == key; });
}

int ConditionTracker::tick(TickTiming timing, ds::LinkedList<std::string>* expiredKeys) {
    // 1ª passada: decrementa
    conditions_.for_each_mut([timing](Condition& c, std::size_t) {
        if (c.tickOn == timing && c.duration > 0) {
            --c.duration;
        }
    });

    // 2ª passada: remove expiradas (de trás para frente para manter índices válidos)
    int expired = 0;
    for (std::size_t i = conditions_.size(); i > 0; --i) {
        const Condition& c = conditions_.get(i - 1);
        if (c.duration == 0) {
            if (expiredKeys != nullptr) {
                expiredKeys->push_front(c.key); // push_front preserva a ordem original
            }
            conditions_.remove_at(i - 1);
            ++expired;
        }
    }
    return expired;
}

std::size_t ConditionTracker::count() const {
    return conditions_.size();
}

void ConditionTracker::clear() {
    conditions_.clear();
}

const ds::LinkedList<Condition>& ConditionTracker::all() const {
    return conditions_;
}

} // namespace openttrpg::core
