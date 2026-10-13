#pragma once

#include "openttrpg/models/CharacterSheet.hpp"
#include <memory>

namespace openttrpg::models {

class Character {
private:
    int id;
    CharacterSheet sheet;
    int initiativeRoll;
    bool activeInCombat;

public:
    Character(int id, CharacterSheet sheet);

    [[nodiscard]] int getId() const;
    [[nodiscard]] const std::string& getName() const;
    [[nodiscard]] CharacterSheet& getSheet();
    [[nodiscard]] const CharacterSheet& getSheet() const;

    [[nodiscard]] int getInitiativeRoll() const;
    void setInitiativeRoll(int roll);

    [[nodiscard]] bool isActiveInCombat() const;
    void setActiveInCombat(bool active);
};

} // namespace openttrpg::models
