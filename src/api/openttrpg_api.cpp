#define OTTRPG_BUILD_DLL
#include "openttrpg/api/openttrpg_api.h"

#include "openttrpg/core/Combatant.hpp"
#include "openttrpg/core/Condition.hpp"
#include "openttrpg/core/Damage.hpp"
#include "openttrpg/core/Dice.hpp"
#include "openttrpg/core/Encounter.hpp"
#include "openttrpg/core/InitiativeTracker.hpp"
#include "openttrpg/core/Resource.hpp"

#include <algorithm>
#include <cstring>
#include <exception>
#include <new>
#include <string>

// ---------------------------------------------------------------------------
// Estrutura interna do Handle Opaco
// ---------------------------------------------------------------------------

struct OttrpgEncounter {
    openttrpg::core::Encounter encounter;
    openttrpg::core::DiceRollResult lastDiceResult;

    explicit OttrpgEncounter(uint32_t seed)
        : encounter(seed == 0 ? openttrpg::core::Encounter() : openttrpg::core::Encounter(seed)) {}
};

namespace {

thread_local std::string g_last_error;

void clear_last_error() {
    g_last_error.clear();
}

void set_last_error(const std::string& err) {
    g_last_error = err;
}

int32_t handle_exception() {
    try {
        throw;
    } catch (const std::out_of_range& e) {
        set_last_error(e.what());
        return OTTRPG_ERR_NOT_FOUND;
    } catch (const std::invalid_argument& e) {
        set_last_error(e.what());
        return OTTRPG_ERR_INVALID_ARGUMENT;
    } catch (const std::logic_error& e) {
        set_last_error(e.what());
        return OTTRPG_ERR_INVALID_STATE;
    } catch (const std::bad_alloc& e) {
        set_last_error(e.what());
        return OTTRPG_ERR_INTERNAL;
    } catch (const std::exception& e) {
        set_last_error(e.what());
        return OTTRPG_ERR_INTERNAL;
    } catch (...) {
        set_last_error("Erro interno desconhecido.");
        return OTTRPG_ERR_INTERNAL;
    }
}

int32_t copy_string_to_buffer(const std::string& src, char* buffer, int32_t buffer_size, int32_t* out_required) {
    const int32_t required = static_cast<int32_t>(src.size() + 1);
    if (out_required != nullptr) {
        *out_required = required;
    }
    if (buffer == nullptr || buffer_size < required) {
        return OTTRPG_ERR_BUFFER_TOO_SMALL;
    }
    std::memcpy(buffer, src.c_str(), static_cast<std::size_t>(required));
    return OTTRPG_OK;
}

void fill_roll_result(const openttrpg::core::DiceRollResult& roll, OttrpgRollResult* out_result) {
    if (out_result == nullptr) {
        return;
    }
    out_result->total = roll.total;
    out_result->dice_total = roll.diceTotal;
    out_result->modifier = roll.modifier;
    out_result->dice_count = static_cast<int32_t>(roll.rolls.size());
    out_result->natural_max_count = roll.countNaturalMax();
    out_result->natural_min_count = roll.countNaturalMin();
}

} // namespace

extern "C" {

/* ------------------------------------------------------------------------- */
/* Biblioteca                                                                */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_api_version(void) {
    return OTTRPG_API_VERSION;
}

OTTRPG_API const char* ottrpg_version_string(void) {
    return "1.0.0";
}

OTTRPG_API const char* ottrpg_last_error(void) {
    return g_last_error.c_str();
}

/* ------------------------------------------------------------------------- */
/* Encontro                                                                  */
/* ------------------------------------------------------------------------- */

OTTRPG_API OttrpgEncounter* ottrpg_encounter_create(uint32_t seed) {
    clear_last_error();
    try {
        return new OttrpgEncounter(seed);
    } catch (...) {
        handle_exception();
        return nullptr;
    }
}

OTTRPG_API void ottrpg_encounter_destroy(OttrpgEncounter* encounter) {
    delete encounter;
}

OTTRPG_API int32_t ottrpg_encounter_set_auto_skip_defeated(OttrpgEncounter* encounter, int32_t enabled) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    encounter->encounter.setAutoSkipDefeated(enabled != 0);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_encounter_set_auto_tick_conditions(OttrpgEncounter* encounter, int32_t enabled) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    encounter->encounter.setAutoTickConditions(enabled != 0);
    return OTTRPG_OK;
}

/* ------------------------------------------------------------------------- */
/* Combatentes                                                               */
/* ------------------------------------------------------------------------- */

OTTRPG_API OttrpgId ottrpg_combatant_add(OttrpgEncounter* encounter, const char* name_utf8,
                                         int32_t initiative_modifier, int32_t max_health, int32_t team) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (name_utf8 == nullptr) {
        set_last_error("Nome do combatente nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    try {
        return encounter->encounter.addCombatant(name_utf8, initiative_modifier, max_health, team);
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_combatant_remove(OttrpgEncounter* encounter, OttrpgId id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (!encounter->encounter.removeCombatant(id)) {
        set_last_error("Combatente não encontrado para remoção.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_count(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    return static_cast<int32_t>(encounter->encounter.combatantCount());
}

OTTRPG_API int32_t ottrpg_combatant_list(OttrpgEncounter* encounter, OttrpgId* out_ids,
                                          int32_t capacity, int32_t* out_count) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const std::size_t total = encounter->encounter.combatantCount();
    if (out_count != nullptr) {
        *out_count = static_cast<int32_t>(total);
    }
    if (out_ids == nullptr || capacity <= 0) {
        return OTTRPG_OK;
    }

    const int32_t toCopy = std::min(capacity, static_cast<int32_t>(total));
    int32_t idx = 0;
    encounter->encounter.combatants().for_each([&](const openttrpg::core::Combatant& c, std::size_t) {
        if (idx < toCopy) {
            out_ids[idx++] = c.id();
        }
    });
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_get_info(OttrpgEncounter* encounter, OttrpgId id,
                                              OttrpgCombatantInfo* out_info) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (out_info == nullptr) {
        set_last_error("Ponteiro de saída nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }

    out_info->id = c->id();
    out_info->initiative_modifier = c->initiativeModifier();
    out_info->team = c->team();
    out_info->in_initiative = encounter->encounter.tracker().contains(id) ? 1 : 0;
    out_info->initiative = encounter->encounter.tracker().initiativeOf(id);
    out_info->is_defeated = c->isDefeated() ? 1 : 0;
    out_info->vital_resource = c->vitalResource();
    out_info->resource_count = static_cast<int32_t>(c->resources().size());
    out_info->condition_count = static_cast<int32_t>(c->conditions().count());
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_get_name(OttrpgEncounter* encounter, OttrpgId id,
                                              char* buffer, int32_t buffer_size, int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return copy_string_to_buffer(c->name(), buffer, buffer_size, out_required);
}

OTTRPG_API int32_t ottrpg_combatant_set_name(OttrpgEncounter* encounter, OttrpgId id, const char* name_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (name_utf8 == nullptr) {
        set_last_error("Nome nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setName(name_utf8);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_set_team(OttrpgEncounter* encounter, OttrpgId id, int32_t team) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setTeam(team);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_set_initiative_modifier(OttrpgEncounter* encounter, OttrpgId id,
                                                             int32_t modifier) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setInitiativeModifier(modifier);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_set_forced_out(OttrpgEncounter* encounter, OttrpgId id, int32_t out) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setForcedOut(out != 0);
    if (encounter->encounter.autoSkipDefeated()) {
        encounter->encounter.tracker().setActive(id, !c->isDefeated());
    }
    return OTTRPG_OK;
}

/* ------------------------------------------------------------------------- */
/* Recursos (Vida, Mana, Estamina...)                                        */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_resource_add(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                       const char* name_utf8, int32_t max, int32_t minimum) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        c->addResource(resource_id, name_utf8 ? name_utf8 : "", max, minimum);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_resource_remove(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->removeResource(resource_id)) {
        set_last_error("Recurso não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_resource_get(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                       OttrpgResourceInfo* out_info) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (out_info == nullptr) {
        set_last_error("Ponteiro de saída nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const openttrpg::core::ResourcePool* pool = c->resource(resource_id);
    if (pool == nullptr) {
        set_last_error("Recurso não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }

    out_info->current = pool->current();
    out_info->max = pool->max();
    out_info->temp = pool->temp();
    out_info->minimum = pool->minimum();
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_resource_set_current(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::core::ResourcePool* pool = c->resource(resource_id);
    if (pool == nullptr) {
        set_last_error("Recurso não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    pool->setCurrent(value);
    if (resource_id == c->vitalResource() && encounter->encounter.autoSkipDefeated()) {
        encounter->encounter.tracker().setActive(id, !c->isDefeated());
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_resource_set_max(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::core::ResourcePool* pool = c->resource(resource_id);
    if (pool == nullptr) {
        set_last_error("Recurso não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        pool->setMax(value);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_resource_set_temp(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::core::ResourcePool* pool = c->resource(resource_id);
    if (pool == nullptr) {
        set_last_error("Recurso não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        pool->setTemp(value);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_resource_spend(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t amount) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        if (!c->spend(resource_id, amount)) {
            set_last_error("Recurso insuficiente para gasto.");
            return OTTRPG_ERR_INSUFFICIENT_RESOURCE;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_resource_restore(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                           int32_t amount, int32_t* out_restored) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        const int restored = c->restore(resource_id, amount);
        if (out_restored != nullptr) {
            *out_restored = restored;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_combatant_set_vital_resource(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setVitalResource(resource_id);
    return OTTRPG_OK;
}

/* ------------------------------------------------------------------------- */
/* Dano e cura                                                               */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_damage_set_response(OttrpgEncounter* encounter, OttrpgId id,
                                              int32_t damage_type, int32_t response) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->damageProfile().setResponse(damage_type, static_cast<openttrpg::core::DamageResponse>(response));
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_damage_set_reduction(OttrpgEncounter* encounter, OttrpgId id,
                                               int32_t damage_type, int32_t amount) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        c->damageProfile().setReduction(damage_type, amount);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_damage_set_global_reduction(OttrpgEncounter* encounter, OttrpgId id, int32_t amount) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        c->damageProfile().setGlobalReduction(amount);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_damage_set_order(OttrpgEncounter* encounter, OttrpgId id, int32_t order) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->damageProfile().setOrder(static_cast<openttrpg::core::DamageOrder>(order));
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_damage_deal(OttrpgEncounter* encounter, OttrpgId target, int32_t amount,
                                      int32_t damage_type, int32_t flags, OttrpgDamageResult* out_result) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::DamageInstance dmg;
    dmg.amount = amount;
    dmg.type = damage_type;
    dmg.ignoreReduction = (flags & OTTRPG_DAMAGE_FLAG_IGNORE_REDUCTION) != 0;
    dmg.ignoreResponse = (flags & OTTRPG_DAMAGE_FLAG_IGNORE_RESPONSE) != 0;

    try {
        const openttrpg::core::DamageResult res = encounter->encounter.dealDamage(target, dmg);
        if (out_result != nullptr) {
            out_result->raw_amount = res.rawAmount;
            out_result->reduced_by = res.reducedBy;
            out_result->final_amount = res.finalAmount;
            out_result->absorbed_by_temp = res.absorbedByTemp;
            out_result->applied_to_current = res.appliedToCurrent;
            out_result->overflow = res.overflow;
            out_result->response = static_cast<int32_t>(res.response);
            out_result->depleted = res.depleted ? 1 : 0;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_damage_preview(OttrpgEncounter* encounter, OttrpgId target, int32_t amount,
                                         int32_t damage_type, int32_t flags, OttrpgDamageResult* out_result) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (out_result == nullptr) {
        set_last_error("Ponteiro de resultado nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(target);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }

    openttrpg::core::DamageInstance dmg;
    dmg.amount = amount;
    dmg.type = damage_type;
    dmg.ignoreReduction = (flags & OTTRPG_DAMAGE_FLAG_IGNORE_REDUCTION) != 0;
    dmg.ignoreResponse = (flags & OTTRPG_DAMAGE_FLAG_IGNORE_RESPONSE) != 0;

    try {
        const openttrpg::core::DamageResult res = openttrpg::core::calculateDamage(dmg, c->damageProfile());
        out_result->raw_amount = res.rawAmount;
        out_result->reduced_by = res.reducedBy;
        out_result->final_amount = res.finalAmount;
        out_result->absorbed_by_temp = 0;
        out_result->applied_to_current = 0;
        out_result->overflow = 0;
        out_result->response = static_cast<int32_t>(res.response);
        out_result->depleted = 0;
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_heal(OttrpgEncounter* encounter, OttrpgId target, int32_t amount, int32_t* out_healed) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    try {
        const int healed = encounter->encounter.heal(target, amount);
        if (out_healed != nullptr) {
            *out_healed = healed;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

/* ------------------------------------------------------------------------- */
/* Condições                                                                 */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_condition_add(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8,
                                        int32_t duration, int32_t intensity, int32_t tick_timing,
                                        int32_t stack_policy, OttrpgId source_id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr) {
        set_last_error("Chave de condição nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }

    openttrpg::core::Condition cond;
    cond.key = key_utf8;
    cond.duration = duration;
    cond.intensity = intensity;
    cond.tickOn = static_cast<openttrpg::core::TickTiming>(tick_timing);
    cond.sourceId = source_id;

    try {
        encounter->encounter.addCondition(target, cond, static_cast<openttrpg::core::StackPolicy>(stack_policy));
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_condition_remove(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr) {
        set_last_error("Chave de condição nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    try {
        if (!encounter->encounter.removeCondition(target, key_utf8)) {
            set_last_error("Condição não encontrada.");
            return OTTRPG_ERR_NOT_FOUND;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_condition_has(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr) {
        set_last_error("Chave de condição nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(target);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return c->conditions().has(key_utf8) ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_condition_get(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8,
                                        OttrpgConditionInfo* out_info) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr || out_info == nullptr) {
        set_last_error("Argumento nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(target);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const openttrpg::core::Condition* cond = c->conditions().get(key_utf8);
    if (cond == nullptr) {
        set_last_error("Condição não encontrada.");
        return OTTRPG_ERR_NOT_FOUND;
    }

    out_info->duration = cond->duration;
    out_info->intensity = cond->intensity;
    out_info->tick_timing = static_cast<int32_t>(cond->tickOn);
    out_info->source_id = cond->sourceId;
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_condition_get_key_at(OttrpgEncounter* encounter, OttrpgId target, int32_t index,
                                               char* buffer, int32_t buffer_size, int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(target);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (index < 0 || static_cast<std::size_t>(index) >= c->conditions().count()) {
        set_last_error("Índice de condição fora dos limites.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const openttrpg::core::Condition& cond = c->conditions().all().get(static_cast<std::size_t>(index));
    return copy_string_to_buffer(cond.key, buffer, buffer_size, out_required);
}

/* ------------------------------------------------------------------------- */
/* Iniciativa e turnos                                                       */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_initiative_set_order(OttrpgEncounter* encounter, int32_t lowest_first) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const auto order = lowest_first ? openttrpg::core::InitiativeOrder::LowestFirst
                                    : openttrpg::core::InitiativeOrder::HighestFirst;
    if (!encounter->encounter.setInitiativeOrder(order)) {
        set_last_error("Não é possível alterar a ordem de iniciativa após o combate ter iniciado.");
        return OTTRPG_ERR_INVALID_STATE;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_initiative_roll(OttrpgEncounter* encounter, OttrpgId id, int32_t dice_count,
                                           int32_t dice_sides, int32_t* out_total) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    try {
        const int total = encounter->encounter.rollInitiative(id, dice_count, dice_sides);
        if (out_total != nullptr) {
            *out_total = total;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_initiative_roll_list(OttrpgEncounter* encounter, const OttrpgId* ids, int32_t count,
                                                int32_t dice_count, int32_t dice_sides) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (count < 0 || (count > 0 && ids == nullptr)) {
        set_last_error("Lista de IDs de combatentes inválida.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    try {
        encounter->encounter.rollInitiativeForList(ids, static_cast<std::size_t>(count), dice_count, dice_sides);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_initiative_roll_all(OttrpgEncounter* encounter, int32_t dice_count, int32_t dice_sides) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    try {
        encounter->encounter.rollInitiativeForAll(dice_count, dice_sides);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_initiative_set(OttrpgEncounter* encounter, OttrpgId id, int32_t value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (!encounter->encounter.setInitiative(id, value)) {
        set_last_error("Combatente não encontrado para definir iniciativa.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_initiative_remove(OttrpgEncounter* encounter, OttrpgId id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (!encounter->encounter.tracker().remove(id)) {
        set_last_error("Combatente não encontrado na lista de iniciativa.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_initiative_get_order(OttrpgEncounter* encounter, OttrpgId* out_ids,
                                                int32_t capacity, int32_t* out_count) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const std::size_t total = encounter->encounter.tracker().size();
    if (out_count != nullptr) {
        *out_count = static_cast<int32_t>(total);
    }
    if (out_ids == nullptr || capacity <= 0) {
        return OTTRPG_OK;
    }

    const int32_t toCopy = std::min(capacity, static_cast<int32_t>(total));
    int32_t idx = 0;
    encounter->encounter.tracker().entries().for_each([&](const openttrpg::core::InitiativeEntry& e, std::size_t) {
        if (idx < toCopy) {
            out_ids[idx++] = e.id;
        }
    });
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combat_start(OttrpgEncounter* encounter) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (!encounter->encounter.startCombat()) {
        set_last_error("Não foi possível iniciar combate: nenhum participante ativo na iniciativa.");
        return OTTRPG_ERR_INVALID_STATE;
    }
    return OTTRPG_OK;
}

OTTRPG_API OttrpgId ottrpg_combat_next_turn(OttrpgEncounter* encounter) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (!encounter->encounter.isCombatActive()) {
        set_last_error("Combate não está ativo.");
        return OTTRPG_ERR_INVALID_STATE;
    }
    const OttrpgId nextId = encounter->encounter.nextTurn();
    if (nextId == openttrpg::core::kInvalidId) {
        set_last_error("Nenhum participante ativo para o próximo turno.");
        return OTTRPG_ERR_INVALID_STATE;
    }
    return nextId;
}

OTTRPG_API OttrpgId ottrpg_combat_current(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    return encounter->encounter.currentTurn();
}

OTTRPG_API OttrpgId ottrpg_combat_peek_next(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    return encounter->encounter.tracker().peekNext();
}

OTTRPG_API int32_t ottrpg_combat_round(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    return encounter->encounter.round();
}

OTTRPG_API int32_t ottrpg_combat_is_active(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        return 0;
    }
    return encounter->encounter.isCombatActive() ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_combat_end(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    encounter->encounter.endCombat();
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combat_is_team_defeated(OttrpgEncounter* encounter, int32_t team) {
    if (encounter == nullptr) {
        return 0;
    }
    return encounter->encounter.isTeamDefeated(team) ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_combat_standing_team_count(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        return 0;
    }
    return encounter->encounter.standingTeamCount();
}

OTTRPG_API int32_t ottrpg_combat_expired_count(OttrpgEncounter* encounter) {
    if (encounter == nullptr) {
        return 0;
    }
    return static_cast<int32_t>(encounter->encounter.lastExpiredConditions().size());
}

OTTRPG_API int32_t ottrpg_combat_expired_get(OttrpgEncounter* encounter, int32_t index, OttrpgId* out_entity,
                                              char* key_buffer, int32_t buffer_size, int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const auto& list = encounter->encounter.lastExpiredConditions();
    if (index < 0 || static_cast<std::size_t>(index) >= list.size()) {
        set_last_error("Índice de condição expirada fora dos limites.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const auto& ev = list.get(static_cast<std::size_t>(index));
    if (out_entity != nullptr) {
        *out_entity = ev.entityId;
    }
    return copy_string_to_buffer(ev.key, key_buffer, buffer_size, out_required);
}

/* ------------------------------------------------------------------------- */
/* Economia de Ações e Fila de Prioridades de Turno (Genérico)               */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_action_define_type(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type,
                                             const char* name_utf8, int32_t priority, int32_t max_per_turn) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (name_utf8 == nullptr) {
        set_last_error("Nome do tipo de ação nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        c->actions().defineActionType(action_type, name_utf8, priority, max_per_turn);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_action_remove_type(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().removeActionType(action_type)) {
        set_last_error("Tipo de ação não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_add_fallback(OttrpgEncounter* encounter, OttrpgId id,
                                              int32_t required_type, int32_t provider_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().addFallback(required_type, provider_type)) {
        set_last_error("Não foi possível registrar regra de conversão entre os tipos de ação informados.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_remove_fallback(OttrpgEncounter* encounter, OttrpgId id,
                                                 int32_t required_type, int32_t provider_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().removeFallback(required_type, provider_type)) {
        set_last_error("Regra de fallback não encontrada.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_clear_fallbacks(OttrpgEncounter* encounter, OttrpgId id, int32_t required_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().clearFallbacks(required_type)) {
        set_last_error("Tipo de ação não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_get_slot(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type,
                                          OttrpgActionSlotInfo* out_info) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (out_info == nullptr) {
        set_last_error("Ponteiro out_info nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const openttrpg::core::ActionTypeSlot* s = c->actions().slot(action_type);
    if (s == nullptr) {
        set_last_error("Tipo de ação não encontrado para este combatente.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    out_info->action_type = s->id;
    out_info->priority = s->priority;
    out_info->max_per_turn = s->maxPerTurn;
    out_info->current = s->current;
    out_info->fallback_count = static_cast<int32_t>(s->fallbackProviders.size());
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_set_max(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type,
                                         int32_t max_per_turn, int32_t refill_current) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().setMaxPerTurn(action_type, max_per_turn, refill_current != 0)) {
        set_last_error("Tipo de ação não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_set_current(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type,
                                             int32_t current) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().setCurrent(action_type, current)) {
        set_last_error("Tipo de ação não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_modify_current(OttrpgEncounter* encounter, OttrpgId id, int32_t action_type,
                                                int32_t delta) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().modifyCurrent(action_type, delta)) {
        set_last_error("Tipo de ação não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_reset_turn(OttrpgEncounter* encounter, OttrpgId id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->actions().resetTurn();
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_can_use(OttrpgEncounter* encounter, OttrpgId id, int32_t required_type,
                                         int32_t* out_consumed_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    int consumed = openttrpg::core::kInvalidActionType;
    const bool ok = c->actions().canUse(required_type, 1, &consumed);
    if (out_consumed_type != nullptr) {
        *out_consumed_type = consumed;
    }
    return ok ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_action_spend(OttrpgEncounter* encounter, OttrpgId id, int32_t required_type,
                                       int32_t* out_consumed_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    int consumed = openttrpg::core::kInvalidActionType;
    if (!c->actions().spend(required_type, 1, &consumed)) {
        set_last_error("Ações insuficientes no turno para realizar esta ação.");
        if (out_consumed_type != nullptr) {
            *out_consumed_type = openttrpg::core::kInvalidActionType;
        }
        return OTTRPG_ERR_INSUFFICIENT_RESOURCE;
    }
    if (out_consumed_type != nullptr) {
        *out_consumed_type = consumed;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_spend_compound(OttrpgEncounter* encounter, OttrpgId id,
                                                const int32_t* required_types, int32_t count) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (count < 0 || (count > 0 && required_types == nullptr)) {
        set_last_error("Lista de tipos de ação inválida.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().spendCompound(required_types, static_cast<std::size_t>(count))) {
        set_last_error("Ações insuficientes no turno para realizar a ação composta.");
        return OTTRPG_ERR_INSUFFICIENT_RESOURCE;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_enqueue(OttrpgEncounter* encounter, OttrpgId id, int32_t required_type,
                                         const char* name_utf8, OttrpgId target_id,
                                         int32_t* out_action_id, int32_t* out_consumed_type) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (name_utf8 == nullptr) {
        set_last_error("Nome da ação nulo.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        int consumed = openttrpg::core::kInvalidActionType;
        const int actionId = c->actions().enqueueAction(required_type, name_utf8, target_id, &consumed);
        if (actionId <= 0) {
            set_last_error("Ações insuficientes no turno para enfileirar esta ação.");
            if (out_action_id != nullptr) *out_action_id = 0;
            if (out_consumed_type != nullptr) *out_consumed_type = openttrpg::core::kInvalidActionType;
            return OTTRPG_ERR_INSUFFICIENT_RESOURCE;
        }
        if (out_action_id != nullptr) *out_action_id = actionId;
        if (out_consumed_type != nullptr) *out_consumed_type = consumed;
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_action_enqueue_compound(OttrpgEncounter* encounter, OttrpgId id,
                                                  const int32_t* required_types, int32_t count,
                                                  int32_t execution_priority, const char* name_utf8,
                                                  OttrpgId target_id, int32_t* out_action_id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (name_utf8 == nullptr || count < 0 || (count > 0 && required_types == nullptr)) {
        set_last_error("Argumentos inválidos para ação composta.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    try {
        const int actionId = c->actions().enqueueCompoundAction(
            required_types, static_cast<std::size_t>(count), execution_priority, name_utf8, target_id);
        if (actionId <= 0) {
            set_last_error("Ações insuficientes no turno para enfileirar a ação composta.");
            if (out_action_id != nullptr) *out_action_id = 0;
            return OTTRPG_ERR_INSUFFICIENT_RESOURCE;
        }
        if (out_action_id != nullptr) *out_action_id = actionId;
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_action_queue_count(OttrpgEncounter* encounter, OttrpgId id) {
    if (encounter == nullptr) {
        return 0;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        return 0;
    }
    return static_cast<int32_t>(c->actions().queueSize());
}

OTTRPG_API int32_t ottrpg_action_queue_get(OttrpgEncounter* encounter, OttrpgId id, int32_t index,
                                           OttrpgQueuedActionInfo* out_info, char* name_buffer,
                                           int32_t buffer_size, int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (index < 0 || static_cast<std::size_t>(index) >= c->actions().queueSize()) {
        set_last_error("Índice da fila de prioridades fora dos limites.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    const openttrpg::core::QueuedAction& qa = c->actions().queue().get(static_cast<std::size_t>(index));
    if (out_info != nullptr) {
        out_info->action_id = qa.actionId;
        out_info->required_type = qa.requiredType;
        out_info->consumed_type = qa.consumedType;
        out_info->cost = qa.cost;
        out_info->target_id = qa.targetId;
        out_info->priority = qa.priority;
    }
    if (name_buffer != nullptr || out_required != nullptr) {
        return copy_string_to_buffer(qa.name, name_buffer, buffer_size, out_required);
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_queue_pop(OttrpgEncounter* encounter, OttrpgId id,
                                           OttrpgQueuedActionInfo* out_info, char* name_buffer,
                                           int32_t buffer_size, int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::core::QueuedAction qa;
    if (!c->actions().popNextAction(qa)) {
        set_last_error("Fila de prioridades de ações está vazia.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->recordActionInTurn(qa.name);
    if (out_info != nullptr) {
        out_info->action_id = qa.actionId;
        out_info->required_type = qa.requiredType;
        out_info->consumed_type = qa.consumedType;
        out_info->cost = qa.cost;
        out_info->target_id = qa.targetId;
        out_info->priority = qa.priority;
    }
    if (name_buffer != nullptr || out_required != nullptr) {
        return copy_string_to_buffer(qa.name, name_buffer, buffer_size, out_required);
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_queue_cancel(OttrpgEncounter* encounter, OttrpgId id, int32_t action_id,
                                              int32_t refund_slot) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->actions().cancelQueuedAction(action_id, refund_slot != 0)) {
        set_last_error("Ação não encontrada na fila de prioridades.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_action_queue_clear(OttrpgEncounter* encounter, OttrpgId id, int32_t refund_slots) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->actions().clearQueue(refund_slots != 0);
    return OTTRPG_OK;
}

/* ------------------------------------------------------------------------- */
/* Árvore de Habilidades e Pré-requisitos (Concessão e Uso)                  */
/* ------------------------------------------------------------------------- */

namespace {

std::string join_reasons(const openttrpg::ds::LinkedList<std::string>& reasons) {
    std::string combined;
    reasons.for_each([&](const std::string& r, std::size_t idx) {
        if (idx > 0) {
            combined += "; ";
        }
        combined += r;
    });
    return combined;
}

} // namespace

OTTRPG_API int32_t ottrpg_combatant_set_stat(OttrpgEncounter* encounter, OttrpgId id,
                                             const char* key_utf8, int32_t value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr || key_utf8[0] == '\0') {
        set_last_error("Chave de estatística inválida.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->setStat(key_utf8, value);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_get_stat(OttrpgEncounter* encounter, OttrpgId id,
                                             const char* key_utf8, int32_t default_value, int32_t* out_value) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr || out_value == nullptr) {
        set_last_error("Argumentos nulos em ottrpg_combatant_get_stat.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    *out_value = c->stat(key_utf8, default_value);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_unlock_ability(OttrpgEncounter* encounter, OttrpgId id,
                                                   const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr || ability_key_utf8[0] == '\0') {
        set_last_error("Chave de habilidade inválida.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->unlockAbility(ability_key_utf8);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_revoke_ability(OttrpgEncounter* encounter, OttrpgId id,
                                                   const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    if (!c->revokeAbility(ability_key_utf8)) {
        set_last_error("Habilidade não estava desbloqueada.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_has_ability(OttrpgEncounter* encounter, OttrpgId id,
                                                const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return c->hasAbility(ability_key_utf8) ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_combatant_record_action_in_turn(OttrpgEncounter* encounter, OttrpgId id,
                                                          const char* action_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (action_key_utf8 == nullptr || action_key_utf8[0] == '\0') {
        set_last_error("Chave de ação inválida.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    c->recordActionInTurn(action_key_utf8);
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_combatant_has_used_action_in_turn(OttrpgEncounter* encounter, OttrpgId id,
                                                            const char* action_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (action_key_utf8 == nullptr) {
        set_last_error("Chave de ação nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const openttrpg::core::Combatant* c = encounter->encounter.find(id);
    if (c == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    return c->hasUsedActionInTurn(action_key_utf8) ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_ability_register(OttrpgEncounter* encounter, const char* key_utf8,
                                           const char* name_utf8, const char* parent_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (key_utf8 == nullptr || key_utf8[0] == '\0' || name_utf8 == nullptr) {
        set_last_error("Chave ou nome da habilidade inválidos.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    const std::string parentKey = (parent_key_utf8 != nullptr) ? parent_key_utf8 : "";
    try {
        if (encounter->encounter.abilityTree().registerAbility(key_utf8, name_utf8, parentKey) == nullptr) {
            set_last_error("Não foi possível registrar habilidade (verifique se a habilidade-pai existe na árvore).");
            return OTTRPG_ERR_NOT_FOUND;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_ability_set_req_root(OttrpgEncounter* encounter, const char* ability_key_utf8,
                                               int32_t is_usage_req, int32_t root_kind, int32_t threshold) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::AbilityNode* ability = encounter->encounter.abilityTree().findAbility(ability_key_utf8);
    if (ability == nullptr) {
        set_last_error("Habilidade não encontrada na árvore.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    auto& reqTree = is_usage_req ? ability->usageRequirements : ability->grantRequirements;
    if (!reqTree.setRootKind(static_cast<openttrpg::core::RequirementKind>(root_kind), threshold)) {
        set_last_error("Tipo de nó raiz inválido (deve ser um operador lógico).");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_ability_add_req_node(OttrpgEncounter* encounter, const char* ability_key_utf8,
                                               int32_t is_usage_req, int32_t parent_node_id,
                                               int32_t req_kind, const char* target_key_utf8,
                                               int32_t threshold, int32_t resource_id, int32_t* out_node_id) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::core::AbilityNode* ability = encounter->encounter.abilityTree().findAbility(ability_key_utf8);
    if (ability == nullptr) {
        set_last_error("Habilidade não encontrada na árvore.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    auto& reqTree = is_usage_req ? ability->usageRequirements : ability->grantRequirements;
    const std::string targetKey = (target_key_utf8 != nullptr) ? target_key_utf8 : "";
    try {
        const int nodeId = reqTree.addNode(
            parent_node_id,
            static_cast<openttrpg::core::RequirementKind>(req_kind),
            targetKey,
            threshold,
            resource_id
        );
        if (nodeId <= 0) {
            set_last_error("Não foi possível adicionar nó de pré-requisito (verifique parent_node_id).");
            return OTTRPG_ERR_INVALID_ARGUMENT;
        }
        if (out_node_id != nullptr) {
            *out_node_id = nodeId;
        }
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_ability_can_learn(OttrpgEncounter* encounter, OttrpgId id, const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    if (encounter->encounter.find(id) == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::ds::LinkedList<std::string> reasons;
    const bool ok = encounter->encounter.canLearnAbility(id, ability_key_utf8, &reasons);
    if (!ok) {
        set_last_error(join_reasons(reasons));
    }
    return ok ? 1 : 0;
}

OTTRPG_API int32_t ottrpg_ability_learn(OttrpgEncounter* encounter, OttrpgId id, const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    if (encounter->encounter.find(id) == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::ds::LinkedList<std::string> reasons;
    if (!encounter->encounter.learnAbility(id, ability_key_utf8, &reasons)) {
        set_last_error(join_reasons(reasons));
        return OTTRPG_ERR_INVALID_STATE;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_ability_can_use(OttrpgEncounter* encounter, OttrpgId id, const char* ability_key_utf8) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (ability_key_utf8 == nullptr) {
        set_last_error("Chave de habilidade nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    if (encounter->encounter.find(id) == nullptr) {
        set_last_error("Combatente não encontrado.");
        return OTTRPG_ERR_NOT_FOUND;
    }
    openttrpg::ds::LinkedList<std::string> reasons;
    const bool ok = encounter->encounter.canUseAbility(id, ability_key_utf8, &reasons);
    if (!ok) {
        set_last_error(join_reasons(reasons));
    }
    return ok ? 1 : 0;
}

/* ------------------------------------------------------------------------- */
/* Dados                                                                     */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_dice_roll(OttrpgEncounter* encounter, int32_t count, int32_t sides,
                                    int32_t modifier, OttrpgRollResult* out_result) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    try {
        encounter->lastDiceResult = encounter->encounter.dice().roll(count, sides, modifier);
        fill_roll_result(encounter->lastDiceResult, out_result);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_dice_roll_keep(OttrpgEncounter* encounter, int32_t count, int32_t sides,
                                         int32_t keep_mode, int32_t keep_count, int32_t modifier,
                                         OttrpgRollResult* out_result) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    try {
        encounter->lastDiceResult = encounter->encounter.dice().rollKeep(
            count, sides, static_cast<openttrpg::core::KeepMode>(keep_mode), keep_count, modifier);
        fill_roll_result(encounter->lastDiceResult, out_result);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_dice_roll_expression(OttrpgEncounter* encounter, const char* expression_utf8,
                                               OttrpgRollResult* out_result) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    if (expression_utf8 == nullptr) {
        set_last_error("Expressão de dados nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    try {
        encounter->lastDiceResult = encounter->encounter.dice().rollExpression(expression_utf8);
        fill_roll_result(encounter->lastDiceResult, out_result);
        return OTTRPG_OK;
    } catch (...) {
        return handle_exception();
    }
}

OTTRPG_API int32_t ottrpg_dice_validate_expression(const char* expression_utf8) {
    clear_last_error();
    if (expression_utf8 == nullptr) {
        set_last_error("Expressão nula.");
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    openttrpg::ds::LinkedList<openttrpg::core::DiceTerm> terms;
    int modifier = 0;
    std::string error;
    if (!openttrpg::core::DiceEngine::parseExpression(expression_utf8, terms, modifier, &error)) {
        set_last_error(error);
        return OTTRPG_ERR_INVALID_ARGUMENT;
    }
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_dice_last_values(OttrpgEncounter* encounter, int32_t* out_values, int32_t* out_kept,
                                           int32_t capacity, int32_t* out_count) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    const std::size_t total = encounter->lastDiceResult.rolls.size();
    if (out_count != nullptr) {
        *out_count = static_cast<int32_t>(total);
    }
    if (out_values == nullptr || capacity <= 0) {
        return OTTRPG_OK;
    }

    const int32_t toCopy = std::min(capacity, static_cast<int32_t>(total));
    int32_t idx = 0;
    encounter->lastDiceResult.rolls.for_each([&](const openttrpg::core::DieRoll& d, std::size_t) {
        if (idx < toCopy) {
            out_values[idx] = d.value;
            if (out_kept != nullptr) {
                out_kept[idx] = d.kept ? 1 : 0;
            }
            ++idx;
        }
    });
    return OTTRPG_OK;
}

OTTRPG_API int32_t ottrpg_dice_last_format(OttrpgEncounter* encounter, char* buffer, int32_t buffer_size,
                                           int32_t* out_required) {
    clear_last_error();
    if (encounter == nullptr) {
        set_last_error("Encounter nulo.");
        return OTTRPG_ERR_NULL_HANDLE;
    }
    return copy_string_to_buffer(encounter->lastDiceResult.format(), buffer, buffer_size, out_required);
}

} // extern "C"
