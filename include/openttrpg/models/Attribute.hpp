#pragma once

#include "openttrpg/core/Types.hpp"
#include <string>

namespace openttrpg::models {

/**
 * Representa um atributo no sistema Tormenta20 (Jogo do Ano).
 * 
 * Em Tormenta20 JdA, os atributos são expressos diretamente pelo seu modificador
 * (por exemplo, Força +3, Destreza +2, Inteligência 0, Carisma -1),
 * abandonando a escala tradicional de 10-18 do d20 antigo.
 */
class Attribute {
private:
    core::AttributeType type;
    int baseValue; // Valor do atributo no T20 (ex: 3 para +3, -1 para -1)
    int bonus;     // Bônus temporários concedidos por magias, poções ou itens

public:
    Attribute(core::AttributeType type = core::AttributeType::Strength, int baseValue = 0, int bonus = 0);

    [[nodiscard]] core::AttributeType getType() const;
    [[nodiscard]] std::string getName() const;
    [[nodiscard]] std::string getAbbreviation() const;
    [[nodiscard]] int getBaseValue() const;
    [[nodiscard]] int getBonus() const;
    [[nodiscard]] int getTotalValue() const;
    [[nodiscard]] int getModifier() const; // Em T20 JdA, o modificador é o próprio valor total!

    void setBaseValue(int value);
    void setBonus(int bonusValue);

    // Formata no padrão T20: "+3" ou "-1"
    [[nodiscard]] std::string formatModifier() const;
};

} // namespace openttrpg::models
