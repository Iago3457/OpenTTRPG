#pragma once

#include "openttrpg/core/Types.hpp"
#include <string>

namespace openttrpg::models {

/**
 * Representa uma Habilidade de Classe, Poder Geral ou Magia em Tormenta20 (Jogo do Ano).
 * 
 * Em T20, todas as habilidades especiais de classe (como Ataque Especial de Guerreiro,
 * Fúria de Bárbaro, Golpe Divino de Paladino) e magias consomem Pontos de Mana (PM).
 */
class Skill {
private:
    std::string name;             // Nome do poder ou magia (ex: "Ataque Especial", "Mísseis Mágicos")
    std::string description;      // Descrição mecânica e narrativa do efeito
    int pmCost;                   // Custo em Pontos de Mana (PM) para ativar
    core::ActionType actionType;  // Tipo de ação necessária no turno (Padrão, Movimento, Livre, Reação)
    int bonusModifier;            // Bônus numérico fixo ou adicional associado (ex: +4 no ataque)

public:
    Skill(std::string name = "",
          std::string description = "",
          int pmCost = 1,
          core::ActionType actionType = core::ActionType::Standard,
          int bonusModifier = 0);

    // Getters
    [[nodiscard]] const std::string& getName() const;
    [[nodiscard]] const std::string& getDescription() const;
    [[nodiscard]] int getPmCost() const;
    [[nodiscard]] core::ActionType getActionType() const;
    [[nodiscard]] std::string getActionTypeName() const;
    [[nodiscard]] int getBonusModifier() const;

    // Setters
    void setName(const std::string& newName);
    void setDescription(const std::string& newDescription);
    void setPmCost(int cost);
    void setActionType(core::ActionType type);
    void setBonusModifier(int mod);

    // Formatação amigável para exibição na ficha e terminal
    [[nodiscard]] std::string formatSummary() const;
};

} // namespace openttrpg::models
