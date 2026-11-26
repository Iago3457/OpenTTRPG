#include "openttrpg/core/ActionEconomy.hpp"

#include <algorithm>

namespace openttrpg::core {

ActionTypeSlot& ActionEconomy::defineActionType(ActionTypeId id, const std::string& name, int priority, int maxPerTurn) {
    if (ActionTypeSlot* existing = slot(id)) {
        existing->name = name;
        existing->priority = priority;
        existing->maxPerTurn = maxPerTurn;
        existing->current = maxPerTurn;
        return *existing;
    }

    ActionTypeSlot entry;
    entry.id = id;
    entry.name = name;
    entry.priority = priority;
    entry.maxPerTurn = maxPerTurn;
    entry.current = maxPerTurn;

    slots_.push_back(entry);
    return slots_.get(slots_.size() - 1);
}

bool ActionEconomy::removeActionType(ActionTypeId id) {
    const std::size_t idx = slots_.find_index([id](const ActionTypeSlot& s) { return s.id == id; });
    if (idx == ds::LinkedList<ActionTypeSlot>::npos) {
        return false;
    }
    slots_.remove_at(idx);
    slots_.for_each_mut([id](ActionTypeSlot& s, std::size_t) {
        for (;;) {
            const std::size_t fbIdx = s.fallbackProviders.find_index([id](ActionTypeId p) { return p == id; });
            if (fbIdx == ds::LinkedList<ActionTypeId>::npos) {
                break;
            }
            s.fallbackProviders.remove_at(fbIdx);
        }
    });
    return true;
}

void ActionEconomy::clearActionTypes() {
    slots_.clear();
}

bool ActionEconomy::hasActionType(ActionTypeId id) const {
    return slot(id) != nullptr;
}

ActionTypeSlot* ActionEconomy::slot(ActionTypeId id) {
    return slots_.find_if([id](const ActionTypeSlot& s) { return s.id == id; });
}

const ActionTypeSlot* ActionEconomy::slot(ActionTypeId id) const {
    return slots_.find_if([id](const ActionTypeSlot& s) { return s.id == id; });
}

const ds::LinkedList<ActionTypeSlot>& ActionEconomy::slots() const {
    return slots_;
}

bool ActionEconomy::addFallback(ActionTypeId requiredType, ActionTypeId providerType) {
    if (requiredType == providerType) {
        return false;
    }
    ActionTypeSlot* req = slot(requiredType);
    if (req == nullptr || slot(providerType) == nullptr) {
        return false;
    }
    if (req->fallbackProviders.find_if([providerType](ActionTypeId p) { return p == providerType; }) == nullptr) {
        req->fallbackProviders.push_back(providerType);
    }
    return true;
}

bool ActionEconomy::removeFallback(ActionTypeId requiredType, ActionTypeId providerType) {
    ActionTypeSlot* req = slot(requiredType);
    if (req == nullptr) {
        return false;
    }
    const std::size_t idx = req->fallbackProviders.find_index([providerType](ActionTypeId p) { return p == providerType; });
    if (idx == ds::LinkedList<ActionTypeId>::npos) {
        return false;
    }
    return req->fallbackProviders.remove_at(idx);
}

bool ActionEconomy::clearFallbacks(ActionTypeId requiredType) {
    ActionTypeSlot* req = slot(requiredType);
    if (req == nullptr) {
        return false;
    }
    req->fallbackProviders.clear();
    return true;
}

bool ActionEconomy::canFallbackTo(ActionTypeId requiredType, ActionTypeId providerType) const {
    const ActionTypeSlot* req = slot(requiredType);
    if (req == nullptr) {
        return false;
    }
    return req->fallbackProviders.find_if([providerType](ActionTypeId p) { return p == providerType; }) != nullptr;
}

bool ActionEconomy::setPriority(ActionTypeId id, int priority) {
    ActionTypeSlot* s = slot(id);
    if (s == nullptr) {
        return false;
    }
    s->priority = priority;
    return true;
}

bool ActionEconomy::setMaxPerTurn(ActionTypeId id, int maxPerTurn, bool refillCurrent) {
    ActionTypeSlot* s = slot(id);
    if (s == nullptr) {
        return false;
    }
    s->maxPerTurn = maxPerTurn;
    if (refillCurrent || maxPerTurn < 0) {
        s->current = maxPerTurn;
    } else if (s->current < 0) {
        s->current = 0;
    } else if (s->current > maxPerTurn) {
        s->current = maxPerTurn;
    }
    return true;
}

bool ActionEconomy::setCurrent(ActionTypeId id, int current) {
    ActionTypeSlot* s = slot(id);
    if (s == nullptr) {
        return false;
    }
    if (s->isUnlimited()) {
        return true;
    }
    s->current = std::max(0, current);
    return true;
}

bool ActionEconomy::modifyCurrent(ActionTypeId id, int delta) {
    ActionTypeSlot* s = slot(id);
    if (s == nullptr) {
        return false;
    }
    if (s->isUnlimited()) {
        return true;
    }
    s->current = std::max(0, s->current + delta);
    return true;
}

int ActionEconomy::remaining(ActionTypeId id) const {
    const ActionTypeSlot* s = slot(id);
    if (s == nullptr) {
        return 0;
    }
    return s->current;
}

const ActionTypeSlot* ActionEconomy::findAvailableFallback(const ActionTypeSlot& requiredSlot, int amount) const {
    const ActionTypeSlot* chosen = nullptr;
    requiredSlot.fallbackProviders.for_each([&](const ActionTypeId& providerId, std::size_t) {
        if (chosen != nullptr) {
            return;
        }
        const ActionTypeSlot* provider = slot(providerId);
        if (provider != nullptr && (provider->isUnlimited() || provider->current >= amount)) {
            chosen = provider;
        }
    });
    return chosen;
}

bool ActionEconomy::canUse(ActionTypeId requiredType, int amount, ActionTypeId* outConsumedType) const {
    if (amount < 0) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return false;
    }

    const ActionTypeSlot* req = slot(requiredType);
    if (req == nullptr) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return false;
    }

    if (amount == 0) {
        if (outConsumedType != nullptr) {
            *outConsumedType = req->id;
        }
        return true;
    }

    // 1. Verifica saldo direto no próprio tipo de ação exigido
    if (req->isUnlimited() || req->current >= amount) {
        if (outConsumedType != nullptr) {
            *outConsumedType = req->id;
        }
        return true;
    }

    // 2. Verifica na lista explícita de provedores substitutos (fallbackProviders) daquele tipo
    const ActionTypeSlot* fallback = findAvailableFallback(*req, amount);
    if (fallback != nullptr) {
        if (outConsumedType != nullptr) {
            *outConsumedType = fallback->id;
        }
        return true;
    }

    if (outConsumedType != nullptr) {
        *outConsumedType = kInvalidActionType;
    }
    return false;
}

bool ActionEconomy::spend(ActionTypeId requiredType, int amount, ActionTypeId* outConsumedType) {
    if (amount < 0) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return false;
    }

    ActionTypeId consumedId = kInvalidActionType;
    if (!canUse(requiredType, amount, &consumedId)) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return false;
    }

    if (amount == 0) {
        if (outConsumedType != nullptr) {
            *outConsumedType = consumedId;
        }
        return true;
    }

    ActionTypeSlot* consumedSlot = slot(consumedId);
    if (consumedSlot == nullptr) {
        return false;
    }

    if (!consumedSlot->isUnlimited()) {
        consumedSlot->current -= amount;
    }

    if (outConsumedType != nullptr) {
        *outConsumedType = consumedId;
    }
    return true;
}

bool ActionEconomy::spendCompound(const ActionTypeId* requiredTypes, std::size_t count,
                                  ds::LinkedList<ActionCostTerm>* outConsumed) {
    if (count == 0) {
        return true;
    }
    if (requiredTypes == nullptr) {
        return false;
    }

    ds::LinkedList<ActionTypeSlot> snapshot = slots_;
    ds::LinkedList<ActionCostTerm> spentTerms;
    for (std::size_t i = 0; i < count; ++i) {
        ActionTypeId consumedId = kInvalidActionType;
        if (!spend(requiredTypes[i], 1, &consumedId)) {
            slots_ = snapshot;
            return false;
        }
        if (consumedId != kInvalidActionType) {
            spentTerms.push_back(ActionCostTerm{consumedId, 1});
        }
    }
    if (outConsumed != nullptr) {
        spentTerms.for_each([outConsumed](const ActionCostTerm& term, std::size_t) {
            outConsumed->push_back(term);
        });
    }
    return true;
}

bool ActionEconomy::refund(ActionTypeId consumedType, int amount) {
    if (amount < 0) {
        return false;
    }
    ActionTypeSlot* s = slot(consumedType);
    if (s == nullptr) {
        return false;
    }
    if (amount == 0) {
        return true;
    }
    if (!s->isUnlimited()) {
        s->current += amount;
    }
    return true;
}

int ActionEconomy::enqueueAction(ActionTypeId requiredType, const std::string& name, int targetId,
                                 ActionTypeId* outConsumedType, int cost,
                                 bool markRecorded, const std::string& extraHistoryKey) {
    const ActionTypeSlot* reqSlot = slot(requiredType);
    if (reqSlot == nullptr || cost <= 0) {
        return 0;
    }

    ActionTypeId consumedId = kInvalidActionType;
    if (!spend(requiredType, cost, &consumedId)) {
        if (outConsumedType != nullptr) {
            *outConsumedType = kInvalidActionType;
        }
        return 0;
    }

    if (outConsumedType != nullptr) {
        *outConsumedType = consumedId;
    }

    QueuedAction action;
    action.actionId = nextActionId_++;
    action.name = name;
    action.requiredType = requiredType;
    action.consumedType = consumedId;
    action.cost = cost;
    action.consumedCosts.push_back(ActionCostTerm{consumedId, cost});
    action.targetId = targetId;
    action.priority = reqSlot->priority;
    action.extraHistoryKey = extraHistoryKey;
    action.recordedInTurnHistory = markRecorded;

    actionQueue_.enqueue(action, action.priority);
    return action.actionId;
}

int ActionEconomy::enqueueCompoundAction(const ActionTypeId* requiredTypes, std::size_t count,
                                         int executionPriority, const std::string& name, int targetId,
                                         bool markRecorded) {
    if (count == 0 || requiredTypes == nullptr) {
        return 0;
    }
    ds::LinkedList<ActionCostTerm> consumedTerms;
    if (!spendCompound(requiredTypes, count, &consumedTerms)) {
        return 0;
    }

    QueuedAction action;
    action.actionId = nextActionId_++;
    action.name = name;
    action.requiredType = requiredTypes[0];
    action.consumedType = !consumedTerms.is_empty() ? consumedTerms.get(0).actionType : action.requiredType;
    action.cost = static_cast<int>(count);
    action.consumedCosts = consumedTerms;
    action.targetId = targetId;
    action.priority = executionPriority;
    action.recordedInTurnHistory = markRecorded;

    actionQueue_.enqueue(action, action.priority);
    return action.actionId;
}

bool ActionEconomy::popNextAction(QueuedAction& outAction) {
    if (actionQueue_.is_empty()) {
        return false;
    }
    outAction = actionQueue_.dequeue();
    return true;
}

const QueuedAction* ActionEconomy::peekNextAction() const {
    if (actionQueue_.is_empty()) {
        return nullptr;
    }
    return &actionQueue_.front();
}

bool ActionEconomy::cancelQueuedAction(int actionId, bool refundSlot, QueuedAction* outRemoved) {
    QueuedAction removed;
    const bool found = actionQueue_.remove_if(
        [actionId](const QueuedAction& a) { return a.actionId == actionId; },
        &removed
    );
    if (!found) {
        return false;
    }
    if (refundSlot) {
        if (!removed.consumedCosts.is_empty()) {
            removed.consumedCosts.for_each([this](const ActionCostTerm& term, std::size_t) {
                if (term.actionType != kInvalidActionType && term.amount > 0) {
                    refund(term.actionType, term.amount);
                }
            });
        } else if (removed.consumedType != kInvalidActionType) {
            refund(removed.consumedType, removed.cost);
        }
    }
    if (outRemoved != nullptr) {
        *outRemoved = removed;
    }
    return true;
}

void ActionEconomy::clearQueue(bool refundSlots) {
    if (refundSlots) {
        actionQueue_.for_each([this](const QueuedAction& a, int, std::size_t) {
            if (!a.consumedCosts.is_empty()) {
                a.consumedCosts.for_each([this](const ActionCostTerm& term, std::size_t) {
                    if (term.actionType != kInvalidActionType && term.amount > 0) {
                        refund(term.actionType, term.amount);
                    }
                });
            } else if (a.consumedType != kInvalidActionType) {
                refund(a.consumedType, a.cost);
            }
        });
    }
    actionQueue_.clear();
}

std::size_t ActionEconomy::queueSize() const {
    return actionQueue_.size();
}

bool ActionEconomy::isQueueEmpty() const {
    return actionQueue_.is_empty();
}

const ds::PriorityQueue<QueuedAction>& ActionEconomy::queue() const {
    return actionQueue_;
}

void ActionEconomy::resetTurn() {
    slots_.for_each_mut([](ActionTypeSlot& s, std::size_t) {
        s.current = s.maxPerTurn;
    });
    actionQueue_.clear();
}

} // namespace openttrpg::core
