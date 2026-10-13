#pragma once

#include <string>

namespace openttrpg::core {

/**
 * @brief Os 6 Atributos canônicos de Tormenta20 (Jogo do Ano).
 * Em T20 JdA, o modificador é o próprio valor do atributo
 * (ex: Força +3, Destreza +2, etc.).
 */
enum class AttributeType {
    Strength,     // Força (FOR) - Dano corpo a corpo, atletismo e capacidade de carga
    Dexterity,    // Destreza (DES) - Defesa, iniciativa, ataques à distância e acrobacias
    Constitution, // Constituição (CON) - Pontos de Vida (PV) e resistência física
    Intelligence, // Inteligência (INT) - Conhecimento, misticismo e perícias adicionais
    Wisdom,       // Sabedoria (SAB) - Percepção, vontade, cura e conexão divina
    Charisma      // Carisma (CAR) - Persuasão, atuação, liderança e poderes sociais
};

/**
 * @brief Categorias de itens e equipamentos no sistema Tormenta20.
 */
enum class ItemType {
    Weapon,        // Armas (corpo a corpo, disparo, arremesso)
    Armor,         // Armaduras leves e pesadas (concedem bônus de Defesa)
    Shield,        // Escudos (leves ou pesados, adicionam Defesa)
    Consumable,    // Consumíveis (Poções de Cura, Poções de Mana, essências)
    Accessory,     // Acessórios mágicos ou vestimentas especiais
    Miscellaneous  // Itens gerais e ferramentas de aventureiro
};

/**
 * @brief Tipos de ação no combate de Tormenta20.
 * Cada turno é composto por 1 Ação Padrão + 1 Ação de Movimento + Ações Livres/Reações.
 */
enum class ActionType {
    Standard,   // Ação Padrão (atacar, lançar a maioria das magias)
    Movement,   // Ação de Movimento (deslocar-se, sacar itens)
    Full,       // Ação Completa (investida, corrida, magias complexas)
    Free,       // Ação Livre (largar item, falar frases curtas, certos poderes)
    Reaction    // Reação (esquiva rápida, magias de contra-ataque)
};

/**
 * @brief Retorna o nome amigável em português do atributo no padrão T20.
 */
inline std::string attributeTypeToString(AttributeType type) {
    switch (type) {
        case AttributeType::Strength:     return "Força (FOR)";
        case AttributeType::Dexterity:    return "Destreza (DES)";
        case AttributeType::Constitution: return "Constituição (CON)";
        case AttributeType::Intelligence: return "Inteligência (INT)";
        case AttributeType::Wisdom:       return "Sabedoria (SAB)";
        case AttributeType::Charisma:     return "Carisma (CAR)";
        default: return "Desconhecido";
    }
}

/**
 * @brief Retorna a abreviação de 3 letras do atributo (ex: FOR, DES).
 */
inline std::string attributeTypeToAbbr(AttributeType type) {
    switch (type) {
        case AttributeType::Strength:     return "FOR";
        case AttributeType::Dexterity:    return "DES";
        case AttributeType::Constitution: return "CON";
        case AttributeType::Intelligence: return "INT";
        case AttributeType::Wisdom:       return "SAB";
        case AttributeType::Charisma:     return "CAR";
        default: return "???";
    }
}

/**
 * @brief Retorna o nome amigável do tipo de item.
 */
inline std::string itemTypeToString(ItemType type) {
    switch (type) {
        case ItemType::Weapon:        return "Arma";
        case ItemType::Armor:         return "Armadura";
        case ItemType::Shield:        return "Escudo";
        case ItemType::Consumable:    return "Consumível";
        case ItemType::Accessory:     return "Acessório";
        case ItemType::Miscellaneous: return "Item Geral";
        default: return "Outros";
    }
}

/**
 * @brief Retorna o nome amigável do tipo de ação no T20.
 */
inline std::string actionTypeToString(ActionType type) {
    switch (type) {
        case ActionType::Standard: return "Ação Padrão";
        case ActionType::Movement: return "Ação de Movimento";
        case ActionType::Full:     return "Ação Completa";
        case ActionType::Free:     return "Ação Livre";
        case ActionType::Reaction: return "Reação";
        default: return "Livre";
    }
}

} // namespace openttrpg::core
