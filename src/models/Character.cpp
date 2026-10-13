#include "openttrpg/models/Character.hpp"
#include <utility>

namespace openttrpg::models {

Character::Character(int id, CharacterSheet sheet)
    : id(id),
      sheet(std::move(sheet)),
      initiativeRoll(0),
      activeInCombat(true) {}

int Character::getId() const {
    return id;
}

const std::string& Character::getName() const {
    return sheet.getName();
}

CharacterSheet& Character::getSheet() {
    return sheet;
}

const CharacterSheet& Character::getSheet() const {
    return sheet;
}

int Character::getInitiativeRoll() const {
    return initiativeRoll;
}

void Character::setInitiativeRoll(int roll) {
    initiativeRoll = roll;
}

bool Character::isActiveInCombat() const {
    return activeInCombat;
}

void Character::setActiveInCombat(bool active) {
    activeInCombat = active;
}

} // namespace openttrpg::models
