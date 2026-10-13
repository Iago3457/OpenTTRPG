#include "openttrpg/models/Attribute.hpp"
#include <sstream>

namespace openttrpg::models {

Attribute::Attribute(core::AttributeType type, int baseValue, int bonus)
    : type(type), baseValue(baseValue), bonus(bonus) {
    // Se o usuário digitou uma pontuação clássica de d20 antigo (ex: 16),
    // convertemos automaticamente para o padrão de T20 Jogo do Ano: (16 - 10) / 2 = +3
    if (this->baseValue >= 10) {
        this->baseValue = (this->baseValue - 10) / 2;
    }
}

core::AttributeType Attribute::getType() const {
    return type;
}

std::string Attribute::getName() const {
    return core::attributeTypeToString(type);
}

std::string Attribute::getAbbreviation() const {
    return core::attributeTypeToAbbr(type);
}

int Attribute::getBaseValue() const {
    return baseValue;
}

int Attribute::getBonus() const {
    return bonus;
}

int Attribute::getTotalValue() const {
    return baseValue + bonus;
}

int Attribute::getModifier() const {
    // Em Tormenta20 Jogo do Ano, o modificador É o valor do atributo!
    return getTotalValue();
}

void Attribute::setBaseValue(int value) {
    // Se for inserido como pontuação clássica (>= 10), converte para o padrão T20
    if (value >= 10) {
        baseValue = (value - 10) / 2;
    } else {
        baseValue = value;
    }
}

void Attribute::setBonus(int bonusValue) {
    bonus = bonusValue;
}

std::string Attribute::formatModifier() const {
    int mod = getModifier();
    std::ostringstream oss;
    if (mod >= 0) {
        oss << "+" << mod;
    } else {
        oss << mod;
    }
    return oss.str();
}

} // namespace openttrpg::models
