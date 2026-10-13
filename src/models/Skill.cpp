#include "openttrpg/models/Skill.hpp"
#include <sstream>
#include <utility>

namespace openttrpg::models {

Skill::Skill(std::string name,
             std::string description,
             int pmCost,
             core::ActionType actionType,
             int bonusModifier)
    : name(std::move(name)),
      description(std::move(description)),
      pmCost(pmCost),
      actionType(actionType),
      bonusModifier(bonusModifier) {}

const std::string& Skill::getName() const {
    return name;
}

const std::string& Skill::getDescription() const {
    return description;
}

int Skill::getPmCost() const {
    return pmCost;
}

core::ActionType Skill::getActionType() const {
    return actionType;
}

std::string Skill::getActionTypeName() const {
    return core::actionTypeToString(actionType);
}

int Skill::getBonusModifier() const {
    return bonusModifier;
}

void Skill::setName(const std::string& newName) {
    name = newName;
}

void Skill::setDescription(const std::string& newDescription) {
    description = newDescription;
}

void Skill::setPmCost(int cost) {
    pmCost = cost;
}

void Skill::setActionType(core::ActionType type) {
    actionType = type;
}

void Skill::setBonusModifier(int mod) {
    bonusModifier = mod;
}

std::string Skill::formatSummary() const {
    std::ostringstream oss;
    oss << name << " [Custo: " << pmCost << " PM | " << getActionTypeName() << "]";
    if (bonusModifier != 0) {
        oss << " (Mod: " << (bonusModifier > 0 ? "+" : "") << bonusModifier << ")";
    }
    if (!description.empty()) {
        oss << " - " << description;
    }
    return oss.str();
}

} // namespace openttrpg::models
