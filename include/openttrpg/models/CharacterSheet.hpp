#pragma once

#include "openttrpg/core/Types.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"
#include "openttrpg/models/Attribute.hpp"
#include "openttrpg/models/Item.hpp"
#include "openttrpg/models/Skill.hpp"
#include <string>
#include <vector>

namespace openttrpg::models {

/**
 * Representa a Ficha de Personagem completa no sistema Tormenta20 (Jogo do Ano).
 * 
 * Contém:
 * - Dados biográficos (Nome, Raça, Classe, Origem, Divindade e Nível).
 * - Recursos vitais de T20: PV (Pontos de Vida) e PM (Pontos de Mana).
 * - Defesa calculada (10 + DES + Armaduras/Escudos).
 * - Os 6 Atributos canônicos de Arton (FOR, DES, CON, INT, SAB, CAR).
 * - Capacidade de carga por Espaços (limite = 10 + 2*FOR, ou 10+FOR se FOR < 0).
 * - Listas Cadastrais (AED1): Inventário e Poderes/Habilidades.
 */
class CharacterSheet {
private:
    std::string characterName;
    std::string characterClass;
    std::string race;
    std::string origin;
    std::string deity;
    int level;

    // Recursos de Tormenta20 JdA
    int currentHp;
    int maxHp;
    int tempHp;

    int currentPm;
    int maxPm;

    int defenseBonusMisc; // Bônus adicionais de defesa além de DES e equipamentos
    int tibares;          // Dinheiro do personagem (T$ - Tibares)

    std::vector<Attribute> attributes;
    ds::LinkedList<Item> inventory; // Lista Cadastral de Equipamentos (AED1)
    ds::LinkedList<Skill> skills;   // Lista Cadastral de Habilidades/Poderes (AED1)

public:
    CharacterSheet(std::string name = "Herói de Arton",
                   std::string charClass = "Guerreiro",
                   std::string race = "Humano",
                   std::string origin = "Soldado",
                   std::string deity = "Valkaria",
                   int level = 1,
                   int maxHp = 20,
                   int maxPm = 3);

    // Getters Biográficos
    [[nodiscard]] const std::string& getName() const;
    [[nodiscard]] const std::string& getCharacterClass() const;
    [[nodiscard]] const std::string& getRace() const;
    [[nodiscard]] const std::string& getOrigin() const;
    [[nodiscard]] const std::string& getDeity() const;
    [[nodiscard]] int getLevel() const;

    // Getters Vitais (PV, PM, Defesa)
    [[nodiscard]] int getCurrentHp() const;
    [[nodiscard]] int getMaxHp() const;
    [[nodiscard]] int getTempHp() const;
    [[nodiscard]] int getCurrentPm() const;
    [[nodiscard]] int getMaxPm() const;
    [[nodiscard]] int getTibares() const;

    // Cálculo dinâmico de Defesa (T20: 10 + Destreza + Armadura + Escudo + Outros)
    [[nodiscard]] int getDefense() const;

    // Capacidade de Carga (Regra de Espaços do Tormenta20 JdA: 10 + 2*Força)
    [[nodiscard]] int getMaxSlots() const;
    [[nodiscard]] int getCurrentSlots() const;
    [[nodiscard]] bool isOverburdened() const;

    // Gerenciamento de Atributos
    [[nodiscard]] const Attribute& getAttribute(core::AttributeType type) const;
    Attribute& getAttribute(core::AttributeType type);
    void setAttributeValue(core::AttributeType type, int value);

    // Gestão de Recursos (Mecânicas ativas de T20)
    bool spendPm(int amount);
    void recoverPm(int amount);
    void takeDamage(int damage);
    void healHp(int amount);
    void setTempHp(int temp);
    void addTibares(int amount);
    bool spendTibares(int amount);

    // Inventário (Lista Cadastral AED1)
    ds::LinkedList<Item>& getInventory();
    [[nodiscard]] const ds::LinkedList<Item>& getInventory() const;
    void addItem(const Item& item);
    bool removeItem(std::size_t index);
    bool equipItem(std::size_t index, bool state);

    // Habilidades e Poderes (Lista Cadastral AED1)
    ds::LinkedList<Skill>& getSkills();
    [[nodiscard]] const ds::LinkedList<Skill>& getSkills() const;
    void addSkill(const Skill& skill);
    bool removeSkill(std::size_t index);

    // Setters
    void setCurrentHp(int hp);
    void setMaxHp(int hp);
    void setCurrentPm(int pm);
    void setMaxPm(int pm);
    void setLevel(int newLevel);
    void setOrigin(const std::string& newOrigin);
    void setDeity(const std::string& newDeity);

    // Formatação visual da Ficha no padrão Tormenta20 Jogo do Ano
    [[nodiscard]] std::string formatSheetSummary() const;
};

} // namespace openttrpg::models
