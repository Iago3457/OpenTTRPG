#include "openttrpg/models/Item.hpp"
#include <sstream>
#include <utility>

namespace openttrpg::models {

Item::Item(std::string name,
           std::string description,
           core::ItemType type,
           int slots,
           std::string damage,
           std::string critical,
           int defenseBonus,
           int armorPenalty,
           int priceTibares,
           bool equipped)
    : name(std::move(name)),
      description(std::move(description)),
      type(type),
      slots(slots),
      damage(std::move(damage)),
      critical(std::move(critical)),
      defenseBonus(defenseBonus),
      armorPenalty(armorPenalty),
      priceTibares(priceTibares),
      equipped(equipped) {}

const std::string& Item::getName() const {
    return name;
}

const std::string& Item::getDescription() const {
    return description;
}

core::ItemType Item::getType() const {
    return type;
}

std::string Item::getTypeName() const {
    return core::itemTypeToString(type);
}

int Item::getSlots() const {
    return slots;
}

const std::string& Item::getDamage() const {
    return damage;
}

const std::string& Item::getCritical() const {
    return critical;
}

int Item::getDefenseBonus() const {
    return defenseBonus;
}

int Item::getArmorPenalty() const {
    return armorPenalty;
}

int Item::getPriceTibares() const {
    return priceTibares;
}

bool Item::isEquipped() const {
    return equipped;
}

void Item::setName(const std::string& newName) {
    name = newName;
}

void Item::setDescription(const std::string& newDescription) {
    description = newDescription;
}

void Item::setType(core::ItemType newType) {
    type = newType;
}

void Item::setSlots(int newSlots) {
    slots = newSlots;
}

void Item::setDamage(const std::string& newDamage) {
    damage = newDamage;
}

void Item::setCritical(const std::string& newCritical) {
    critical = newCritical;
}

void Item::setDefenseBonus(int bonus) {
    defenseBonus = bonus;
}

void Item::setArmorPenalty(int penalty) {
    armorPenalty = penalty;
}

void Item::setPriceTibares(int tibares) {
    priceTibares = tibares;
}

void Item::setEquipped(bool state) {
    equipped = state;
}

std::string Item::formatSummary() const {
    std::ostringstream oss;
    oss << name << " (" << getTypeName() << ") [";
    oss << slots << (slots == 1 ? " espaço" : " espaços") << "]";

    if (!damage.empty()) {
        oss << " | Dano: " << damage << " (Crítico: " << critical << ")";
    }
    if (defenseBonus > 0) {
        oss << " | Defesa: +" << defenseBonus;
    }
    if (armorPenalty < 0) {
        oss << " | Penalidade: " << armorPenalty;
    }
    if (priceTibares > 0) {
        oss << " | T$ " << priceTibares;
    }
    if (equipped) {
        oss << " [Equipado]";
    }
    return oss.str();
}

} // namespace openttrpg::models
