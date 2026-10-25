#pragma once

#include "openttrpg/core/Condition.hpp"
#include "openttrpg/core/Damage.hpp"
#include "openttrpg/core/Resource.hpp"
#include "openttrpg/data_structures/LinkedList.hpp"
#include <string>

namespace openttrpg::core {

// Identificador de entidade. IDs válidos são > 0.
using EntityId = int;
constexpr EntityId kInvalidId = 0;

// Identificador de recurso. O significado é definido pelo sistema (0 = Vida por convenção).
using ResourceId = int;
constexpr ResourceId kHealthResource = 0;

/**
 * @brief Recurso nomeado pertencente a uma entidade.
 */
struct NamedResource {
    ResourceId id = kHealthResource;
    std::string name;
    ResourcePool pool;
};

/**
 * @brief Entidade de combate genérica, sem nenhuma regra de sistema embutida.
 *
 * Possui:
 * - Recursos arbitrários (Vida, Mana, Estamina...), com um "recurso vital" que define
 *   se a entidade está fora de combate (atual <= 0).
 * - Perfil de defesas contra dano.
 * - Condições ativas.
 * - Time/facção (para saber quando um lado venceu).
 */
class Combatant {
public:
    Combatant(EntityId id, std::string name, int initiativeModifier = 0, int maxHealth = 0, int team = 0);

    [[nodiscard]] EntityId id() const;
    [[nodiscard]] const std::string& name() const;
    [[nodiscard]] int initiativeModifier() const;
    [[nodiscard]] int team() const;

    void setName(const std::string& name);
    void setInitiativeModifier(int modifier);
    void setTeam(int team);

    // ----- Recursos -----
    // Cria (ou substitui) um recurso e retorna referência para ele
    ResourcePool& addResource(ResourceId id, const std::string& name, int max, int minimum = 0);
    bool removeResource(ResourceId id);
    [[nodiscard]] bool hasResource(ResourceId id) const;
    ResourcePool* resource(ResourceId id);
    [[nodiscard]] const ResourcePool* resource(ResourceId id) const;
    [[nodiscard]] const NamedResource* resourceEntry(ResourceId id) const;
    [[nodiscard]] const ds::LinkedList<NamedResource>& resources() const;

    // Recurso que define se a entidade está de pé (padrão: kHealthResource)
    void setVitalResource(ResourceId id);
    [[nodiscard]] ResourceId vitalResource() const;

    bool spend(ResourceId id, int amount); // false se não tiver o recurso ou saldo insuficiente
    int restore(ResourceId id, int amount); // retorna quanto recuperou (0 se não tiver o recurso)

    // ----- Dano e cura (no recurso vital) -----
    // Lança std::logic_error se não houver recurso vital
    DamageResult takeDamage(const DamageInstance& damage);
    int heal(int amount);

    // ----- Estado -----
    // Fora de combate se o recurso vital estiver <= 0 ou se foi removido manualmente (fugiu, etc.)
    [[nodiscard]] bool isDefeated() const;
    void setForcedOut(bool out);
    [[nodiscard]] bool isForcedOut() const;

    DamageProfile& damageProfile();
    [[nodiscard]] const DamageProfile& damageProfile() const;
    ConditionTracker& conditions();
    [[nodiscard]] const ConditionTracker& conditions() const;

private:
    EntityId id_;
    std::string name_;
    int initiativeModifier_;
    int team_;
    bool forcedOut_ = false;
    ResourceId vitalResource_ = kHealthResource;

    ds::LinkedList<NamedResource> resources_;
    DamageProfile damageProfile_;
    ConditionTracker conditions_;
};

} // namespace openttrpg::core
