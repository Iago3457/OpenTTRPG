#pragma once

namespace openttrpg::core {

/**
 * @brief Identificador numérico agnóstico de entidade no Core. IDs válidos são > 0.
 */
using EntityId = int;
constexpr EntityId kInvalidId = 0;

/**
 * @brief Identificador numérico agnóstico de recurso (ex.: 0 = Vida por convenção, 1 = Mana/Estamina, etc.).
 */
using ResourceId = int;
constexpr ResourceId kHealthResource = 0;

/**
 * @brief Identificador numérico agnóstico de tipo de dano.
 * O Core não pressupõe quais tipos de dano existem; 0 representa dano sem tipo.
 */
using DamageTypeId = int;
constexpr DamageTypeId kUntypedDamage = 0;

/**
 * @brief Identificador numérico agnóstico de categoria/tipo de ação de turno.
 * Definido livremente por cada sistema de RPG na camada superior.
 */
using ActionTypeId = int;
constexpr ActionTypeId kInvalidActionType = -1;

} // namespace openttrpg::core
