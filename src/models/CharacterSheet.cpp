#include "openttrpg/models/CharacterSheet.hpp"
#include <sstream>
#include <iomanip>
#include <algorithm>

namespace openttrpg::models {

CharacterSheet::CharacterSheet(std::string name,
                               std::string charClass,
                               std::string race,
                               std::string origin,
                               std::string deity,
                               int level,
                               int maxHp,
                               int maxPm)
    : characterName(std::move(name)),
      characterClass(std::move(charClass)),
      race(std::move(race)),
      origin(std::move(origin)),
      deity(std::move(deity)),
      level(level),
      currentHp(maxHp),
      maxHp(maxHp),
      tempHp(0),
      currentPm(maxPm),
      maxPm(maxPm),
      defenseBonusMisc(0),
      tibares(50) {
    // Inicializa os 6 atributos padrão de Tormenta20 (base 0)
    attributes.emplace_back(core::AttributeType::Strength, 0);
    attributes.emplace_back(core::AttributeType::Dexterity, 0);
    attributes.emplace_back(core::AttributeType::Constitution, 0);
    attributes.emplace_back(core::AttributeType::Intelligence, 0);
    attributes.emplace_back(core::AttributeType::Wisdom, 0);
    attributes.emplace_back(core::AttributeType::Charisma, 0);
}

const std::string& CharacterSheet::getName() const {
    return characterName;
}

const std::string& CharacterSheet::getCharacterClass() const {
    return characterClass;
}

const std::string& CharacterSheet::getRace() const {
    return race;
}

const std::string& CharacterSheet::getOrigin() const {
    return origin;
}

const std::string& CharacterSheet::getDeity() const {
    return deity;
}

int CharacterSheet::getLevel() const {
    return level;
}

int CharacterSheet::getCurrentHp() const {
    return currentHp;
}

int CharacterSheet::getMaxHp() const {
    return maxHp;
}

int CharacterSheet::getTempHp() const {
    return tempHp;
}

int CharacterSheet::getCurrentPm() const {
    return currentPm;
}

int CharacterSheet::getMaxPm() const {
    return maxPm;
}

int CharacterSheet::getTibares() const {
    return tibares;
}

int CharacterSheet::getDefense() const {
    // Regra do Tormenta20: Defesa = 10 + Modificador de Destreza + Equipamentos + Outros
    int total = 10 + getAttribute(core::AttributeType::Dexterity).getModifier() + defenseBonusMisc;

    // Soma o bônus de armadura e escudo equipados
    inventory.for_each([&total](const Item& item, std::size_t) {
        if (item.isEquipped() && item.getDefenseBonus() > 0) {
            total += item.getDefenseBonus();
        }
    });

    return total;
}

int CharacterSheet::getMaxSlots() const {
    // Regra canônica de Carga de Tormenta20 Jogo do Ano:
    // Limite de 10 espaços + 2 espaços por ponto de Força (ou -1 se Força for negativa)
    int str = getAttribute(core::AttributeType::Strength).getModifier();
    return (str >= 0) ? (10 + str * 2) : (10 + str);
}

int CharacterSheet::getCurrentSlots() const {
    int totalSlots = 0;
    inventory.for_each([&totalSlots](const Item& item, std::size_t) {
        totalSlots += item.getSlots();
    });
    return totalSlots;
}

bool CharacterSheet::isOverburdened() const {
    return getCurrentSlots() > getMaxSlots();
}

const Attribute& CharacterSheet::getAttribute(core::AttributeType type) const {
    for (const auto& attr : attributes) {
        if (attr.getType() == type) {
            return attr;
        }
    }
    return attributes[0];
}

Attribute& CharacterSheet::getAttribute(core::AttributeType type) {
    for (auto& attr : attributes) {
        if (attr.getType() == type) {
            return attr;
        }
    }
    return attributes[0];
}

void CharacterSheet::setAttributeValue(core::AttributeType type, int value) {
    getAttribute(type).setBaseValue(value);
}

bool CharacterSheet::spendPm(int amount) {
    if (amount <= currentPm) {
        currentPm -= amount;
        return true;
    }
    return false;
}

void CharacterSheet::recoverPm(int amount) {
    currentPm = std::min(maxPm, currentPm + amount);
}

void CharacterSheet::takeDamage(int damage) {
    if (damage <= 0) return;

    if (tempHp > 0) {
        if (damage <= tempHp) {
            tempHp -= damage;
            return;
        } else {
            damage -= tempHp;
            tempHp = 0;
        }
    }

    currentHp -= damage;
}

void CharacterSheet::healHp(int amount) {
    if (amount <= 0) return;
    currentHp = std::min(maxHp, currentHp + amount);
}

void CharacterSheet::setTempHp(int temp) {
    tempHp = std::max(0, temp);
}

void CharacterSheet::addTibares(int amount) {
    if (amount > 0) {
        tibares += amount;
    }
}

bool CharacterSheet::spendTibares(int amount) {
    if (amount <= tibares) {
        tibares -= amount;
        return true;
    }
    return false;
}

ds::LinkedList<Item>& CharacterSheet::getInventory() {
    return inventory;
}

const ds::LinkedList<Item>& CharacterSheet::getInventory() const {
    return inventory;
}

void CharacterSheet::addItem(const Item& item) {
    inventory.push_back(item);
}

bool CharacterSheet::removeItem(std::size_t index) {
    return inventory.remove_at(index);
}

bool CharacterSheet::equipItem(std::size_t index, bool state) {
    if (index < inventory.size()) {
        inventory.get(index).setEquipped(state);
        return true;
    }
    return false;
}

ds::LinkedList<Skill>& CharacterSheet::getSkills() {
    return skills;
}

const ds::LinkedList<Skill>& CharacterSheet::getSkills() const {
    return skills;
}

void CharacterSheet::addSkill(const Skill& skill) {
    skills.push_back(skill);
}

bool CharacterSheet::removeSkill(std::size_t index) {
    return skills.remove_at(index);
}

void CharacterSheet::setCurrentHp(int hp) {
    currentHp = hp;
}

void CharacterSheet::setMaxHp(int hp) {
    maxHp = hp;
}

void CharacterSheet::setCurrentPm(int pm) {
    currentPm = pm;
}

void CharacterSheet::setMaxPm(int pm) {
    maxPm = pm;
}

void CharacterSheet::setLevel(int newLevel) {
    level = newLevel;
}

void CharacterSheet::setOrigin(const std::string& newOrigin) {
    origin = newOrigin;
}

void CharacterSheet::setDeity(const std::string& newDeity) {
    deity = newDeity;
}

std::string CharacterSheet::formatSheetSummary() const {
    std::ostringstream oss;
    oss << "======================================================================\n";
    oss << " FICHA DE PERSONAGEM — TORMENTA20 (JOGO DO ANO)\n";
    oss << " " << characterName << " | " << race << " " << characterClass << " " << level << "º Nível\n";
    oss << " Origem: " << origin << " | Divindade: " << deity << " | Dinheiro: T$ " << tibares << "\n";
    oss << "----------------------------------------------------------------------\n";
    oss << " PONTOS DE VIDA (PV): " << currentHp << "/" << maxHp;
    if (tempHp > 0) oss << " (+" << tempHp << " Temp)";
    oss << " | PONTOS DE MANA (PM): " << currentPm << "/" << maxPm;
    oss << " | DEFESA: " << getDefense() << "\n";
    oss << " CARGA (ESPAÇOS): " << getCurrentSlots() << "/" << getMaxSlots()
        << (isOverburdened() ? " [SOBRECARREGADO!]" : " [OK]") << "\n";
    oss << "----------------------------------------------------------------------\n";
    oss << " ATRIBUTOS (TORMENTA20):\n";
    for (const auto& attr : attributes) {
        oss << "   " << std::left << std::setw(18) << attr.getName()
            << ": " << attr.formatModifier() << "\n";
    }
    oss << "----------------------------------------------------------------------\n";
    oss << " EQUIPAMENTOS E INVENTÁRIO (Lista Cadastral AED1 - " << inventory.size() << " itens):\n";
    if (inventory.is_empty()) {
        oss << "   (Mochila vazia)\n";
    } else {
        inventory.for_each([&oss](const Item& item, std::size_t idx) {
            oss << "   [" << idx << "] " << item.formatSummary() << "\n";
        });
    }
    oss << "----------------------------------------------------------------------\n";
    oss << " HABILIDADES, PODERES E MAGIAS (Lista Cadastral AED1 - " << skills.size() << " poderes):\n";
    if (skills.is_empty()) {
        oss << "   (Nenhuma habilidade ou magia cadastrada)\n";
    } else {
        skills.for_each([&oss](const Skill& sk, std::size_t idx) {
            oss << "   [" << idx << "] " << sk.formatSummary() << "\n";
        });
    }
    oss << "======================================================================";
    return oss.str();
}

} // namespace openttrpg::models
