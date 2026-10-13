#pragma once

#include "openttrpg/core/Types.hpp"
#include <string>

namespace openttrpg::models {

/**
 * Representa um item ou equipamento no Tormenta20 (Jogo do Ano).
 * 
 * Em T20, os itens utilizam a mecânica de "Espaços" de inventário em vez de quilos.
 * Armas possuem fórmulas de dano (ex: "1d8", "2d6") e margens de crítico (ex: "19/x2", "x3").
 * Armaduras e escudos fornecem bônus na Defesa e podem impor Penalidade de Armadura.
 */
class Item {
private:
    std::string name;          // Nome do item (ex: "Espada Longa", "Cota de Malha")
    std::string description;   // Descrição narrativa ou efeitos especiais
    core::ItemType type;       // Categoria (Arma, Armadura, Escudo, Consumível, etc.)
    int slots;                 // Espaços ocupados no inventário de T20 (padrão: 1)
    std::string damage;        // Dano de arma no padrão de dados (ex: "1d8", "2d6")
    std::string critical;      // Margem e multiplicador de crítico (ex: "19/x2", "x3")
    int defenseBonus;          // Bônus fornecido à Defesa (para armaduras e escudos)
    int armorPenalty;          // Penalidade em perícias de Destreza e Força (ex: -2)
    int priceTibares;          // Preço em Tibares (T$ - moeda oficial de Arton)
    bool equipped;             // Se está atualmente empunhado ou vestido

public:
    Item(std::string name = "",
         std::string description = "",
         core::ItemType type = core::ItemType::Miscellaneous,
         int slots = 1,
         std::string damage = "",
         std::string critical = "20/x2",
         int defenseBonus = 0,
         int armorPenalty = 0,
         int priceTibares = 0,
         bool equipped = false);

    // Getters
    [[nodiscard]] const std::string& getName() const;
    [[nodiscard]] const std::string& getDescription() const;
    [[nodiscard]] core::ItemType getType() const;
    [[nodiscard]] std::string getTypeName() const;
    [[nodiscard]] int getSlots() const;
    [[nodiscard]] const std::string& getDamage() const;
    [[nodiscard]] const std::string& getCritical() const;
    [[nodiscard]] int getDefenseBonus() const;
    [[nodiscard]] int getArmorPenalty() const;
    [[nodiscard]] int getPriceTibares() const;
    [[nodiscard]] bool isEquipped() const;

    // Setters
    void setName(const std::string& newName);
    void setDescription(const std::string& newDescription);
    void setType(core::ItemType newType);
    void setSlots(int newSlots);
    void setDamage(const std::string& newDamage);
    void setCritical(const std::string& newCritical);
    void setDefenseBonus(int bonus);
    void setArmorPenalty(int penalty);
    void setPriceTibares(int tibares);
    void setEquipped(bool state);

    // Formatação detalhada de linha do item no padrão Tormenta20
    [[nodiscard]] std::string formatSummary() const;
};

} // namespace openttrpg::models
