/*
 * OpenTTRPG Core - API C (ABI estável) para consumo via DLL/.so
 *
 * Este header é C puro para poder ser consumido por qualquer linguagem com FFI
 * (C# P/Invoke, GDExtension, Python ctypes, Rust...).
 *
 * Convenções:
 * - Todas as funções usam a convenção de chamada cdecl (C# : CallingConvention.Cdecl).
 * - Funções que retornam int32_t retornam OTTRPG_OK (0) ou um código de erro negativo,
 *   salvo quando documentado (ex: IDs, que são sempre > 0).
 * - Strings de entrada são UTF-8 terminadas em '\0'.
 * - Strings de saída são copiadas para um buffer do chamador. `out_required` recebe o tamanho
 *   necessário em bytes (incluindo o '\0'); se o buffer for pequeno, retorna OTTRPG_ERR_BUFFER_TOO_SMALL.
 * - Nenhuma exceção C++ atravessa a fronteira: a mensagem do último erro fica em ottrpg_last_error()
 *   (por thread).
 * - Todas as structs contêm apenas int32_t (blittable, layout sequencial).
 */
#ifndef OPENTTRPG_API_H
#define OPENTTRPG_API_H

#include <stdint.h>

#if defined(_WIN32) || defined(__CYGWIN__)
    #if defined(OTTRPG_BUILD_DLL)
        #define OTTRPG_API __declspec(dllexport)
    #else
        #define OTTRPG_API __declspec(dllimport)
    #endif
#elif defined(__GNUC__) || defined(__clang__)
    #define OTTRPG_API __attribute__((visibility("default")))
#else
    #define OTTRPG_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define OTTRPG_API_VERSION 1

/* ------------------------------------------------------------------------- */
/* Tipos básicos                                                             */
/* ------------------------------------------------------------------------- */

typedef struct OttrpgEncounter OttrpgEncounter; /* Handle opaco */
typedef int32_t OttrpgId;                       /* IDs válidos são > 0 */

enum OttrpgStatus {
    OTTRPG_OK                        = 0,
    OTTRPG_ERR_NULL_HANDLE           = -1,
    OTTRPG_ERR_NOT_FOUND             = -2,
    OTTRPG_ERR_INVALID_ARGUMENT      = -3,
    OTTRPG_ERR_INVALID_STATE         = -4,
    OTTRPG_ERR_BUFFER_TOO_SMALL      = -5,
    OTTRPG_ERR_INSUFFICIENT_RESOURCE = -6,
    OTTRPG_ERR_INTERNAL              = -99
};

enum OttrpgDamageResponse {
    OTTRPG_DAMAGE_NORMAL     = 0,
    OTTRPG_DAMAGE_RESISTANT  = 1,
    OTTRPG_DAMAGE_VULNERABLE = 2,
    OTTRPG_DAMAGE_IMMUNE     = 3
};

enum OttrpgDamageFlags {
    OTTRPG_DAMAGE_FLAG_NONE             = 0,
    OTTRPG_DAMAGE_FLAG_IGNORE_REDUCTION = 1 << 0,
    OTTRPG_DAMAGE_FLAG_IGNORE_RESPONSE  = 1 << 1
};

enum OttrpgTickTiming {
    OTTRPG_TICK_TURN_START = 0,
    OTTRPG_TICK_TURN_END   = 1,
    OTTRPG_TICK_ROUND_END  = 2,
    OTTRPG_TICK_MANUAL     = 3
};

enum OttrpgStackPolicy {
    OTTRPG_STACK_REFRESH = 0,
    OTTRPG_STACK_REPLACE = 1,
    OTTRPG_STACK_STACK   = 2,
    OTTRPG_STACK_IGNORE  = 3
};

enum OttrpgKeepMode {
    OTTRPG_KEEP_ALL     = 0,
    OTTRPG_KEEP_HIGHEST = 1,
    OTTRPG_KEEP_LOWEST  = 2
};

#define OTTRPG_HEALTH_RESOURCE 0
#define OTTRPG_PERMANENT_DURATION (-1)

typedef struct OttrpgDamageResult {
    int32_t raw_amount;
    int32_t reduced_by;
    int32_t final_amount;
    int32_t absorbed_by_temp;
    int32_t applied_to_current;
    int32_t overflow;
    int32_t response;  /* OttrpgDamageResponse */
    int32_t depleted;  /* 1 se o alvo caiu (recurso vital foi de > 0 para <= 0) */
} OttrpgDamageResult;

typedef struct OttrpgResourceInfo {
    int32_t current;
    int32_t max;
    int32_t temp;
    int32_t minimum;
} OttrpgResourceInfo;

typedef struct OttrpgCombatantInfo {
    OttrpgId id;
    int32_t initiative_modifier;
    int32_t team;
    int32_t in_initiative;   /* 1 se está na ordem de iniciativa */
    int32_t initiative;      /* valor de iniciativa (válido se in_initiative == 1) */
    int32_t is_defeated;
    int32_t vital_resource;
    int32_t resource_count;
    int32_t condition_count;
} OttrpgCombatantInfo;

typedef struct OttrpgConditionInfo {
    int32_t duration;   /* -1 = permanente */
    int32_t intensity;
    int32_t tick_timing;
    OttrpgId source_id; /* 0 = sem origem */
} OttrpgConditionInfo;

typedef struct OttrpgRollResult {
    int32_t total;
    int32_t dice_total;
    int32_t modifier;
    int32_t dice_count;        /* quantidade de dados rolados (inclui explosões e descartados) */
    int32_t natural_max_count; /* dados mantidos que tiraram o valor máximo */
    int32_t natural_min_count; /* dados mantidos que tiraram 1 */
} OttrpgRollResult;

/* ------------------------------------------------------------------------- */
/* Biblioteca                                                                */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t     ottrpg_api_version(void);
OTTRPG_API const char* ottrpg_version_string(void);
/* Mensagem do último erro na thread atual ("" se nenhum). Válida até a próxima chamada da API. */
OTTRPG_API const char* ottrpg_last_error(void);

/* ------------------------------------------------------------------------- */
/* Encontro                                                                  */
/* ------------------------------------------------------------------------- */

/* seed == 0 usa uma semente aleatória. Retorna NULL em caso de falha. */
OTTRPG_API OttrpgEncounter* ottrpg_encounter_create(uint32_t seed);
OTTRPG_API void             ottrpg_encounter_destroy(OttrpgEncounter* encounter);
OTTRPG_API int32_t          ottrpg_encounter_set_auto_skip_defeated(OttrpgEncounter* encounter, int32_t enabled);
OTTRPG_API int32_t          ottrpg_encounter_set_auto_tick_conditions(OttrpgEncounter* encounter, int32_t enabled);

/* ------------------------------------------------------------------------- */
/* Combatentes                                                               */
/* ------------------------------------------------------------------------- */

/* Retorna o ID (> 0) ou um código de erro (< 0). max_health == 0 cria sem recurso de vida. */
OTTRPG_API OttrpgId ottrpg_combatant_add(OttrpgEncounter* encounter, const char* name_utf8,
                                         int32_t initiative_modifier, int32_t max_health, int32_t team);
OTTRPG_API int32_t  ottrpg_combatant_remove(OttrpgEncounter* encounter, OttrpgId id);
OTTRPG_API int32_t  ottrpg_combatant_count(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combatant_list(OttrpgEncounter* encounter, OttrpgId* out_ids,
                                          int32_t capacity, int32_t* out_count);
OTTRPG_API int32_t  ottrpg_combatant_get_info(OttrpgEncounter* encounter, OttrpgId id,
                                              OttrpgCombatantInfo* out_info);
OTTRPG_API int32_t  ottrpg_combatant_get_name(OttrpgEncounter* encounter, OttrpgId id,
                                              char* buffer, int32_t buffer_size, int32_t* out_required);
OTTRPG_API int32_t  ottrpg_combatant_set_name(OttrpgEncounter* encounter, OttrpgId id, const char* name_utf8);
OTTRPG_API int32_t  ottrpg_combatant_set_team(OttrpgEncounter* encounter, OttrpgId id, int32_t team);
OTTRPG_API int32_t  ottrpg_combatant_set_initiative_modifier(OttrpgEncounter* encounter, OttrpgId id,
                                                             int32_t modifier);
/* Marca/desmarca como fora de combate independentemente da vida (fugiu, rendeu-se...) */
OTTRPG_API int32_t  ottrpg_combatant_set_forced_out(OttrpgEncounter* encounter, OttrpgId id, int32_t out);

/* ------------------------------------------------------------------------- */
/* Recursos (Vida, Mana, Estamina...)                                        */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_resource_add(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                       const char* name_utf8, int32_t max, int32_t minimum);
OTTRPG_API int32_t ottrpg_resource_remove(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id);
OTTRPG_API int32_t ottrpg_resource_get(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                       OttrpgResourceInfo* out_info);
OTTRPG_API int32_t ottrpg_resource_set_current(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value);
OTTRPG_API int32_t ottrpg_resource_set_max(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value);
OTTRPG_API int32_t ottrpg_resource_set_temp(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t value);
/* Retorna OTTRPG_ERR_INSUFFICIENT_RESOURCE se não houver saldo */
OTTRPG_API int32_t ottrpg_resource_spend(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id, int32_t amount);
OTTRPG_API int32_t ottrpg_resource_restore(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id,
                                           int32_t amount, int32_t* out_restored);
OTTRPG_API int32_t ottrpg_combatant_set_vital_resource(OttrpgEncounter* encounter, OttrpgId id, int32_t resource_id);

/* ------------------------------------------------------------------------- */
/* Dano e cura                                                               */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_damage_set_response(OttrpgEncounter* encounter, OttrpgId id,
                                              int32_t damage_type, int32_t response);
OTTRPG_API int32_t ottrpg_damage_set_reduction(OttrpgEncounter* encounter, OttrpgId id,
                                               int32_t damage_type, int32_t amount);
OTTRPG_API int32_t ottrpg_damage_set_global_reduction(OttrpgEncounter* encounter, OttrpgId id, int32_t amount);
/* order: 0 = redução antes do multiplicador, 1 = multiplicador antes da redução */
OTTRPG_API int32_t ottrpg_damage_set_order(OttrpgEncounter* encounter, OttrpgId id, int32_t order);
/* flags: combinação de OttrpgDamageFlags. out_result pode ser NULL. */
OTTRPG_API int32_t ottrpg_damage_deal(OttrpgEncounter* encounter, OttrpgId target, int32_t amount,
                                      int32_t damage_type, int32_t flags, OttrpgDamageResult* out_result);
/* Calcula sem aplicar (pré-visualização na UI) */
OTTRPG_API int32_t ottrpg_damage_preview(OttrpgEncounter* encounter, OttrpgId target, int32_t amount,
                                         int32_t damage_type, int32_t flags, OttrpgDamageResult* out_result);
OTTRPG_API int32_t ottrpg_heal(OttrpgEncounter* encounter, OttrpgId target, int32_t amount, int32_t* out_healed);

/* ------------------------------------------------------------------------- */
/* Condições                                                                 */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_condition_add(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8,
                                        int32_t duration, int32_t intensity, int32_t tick_timing,
                                        int32_t stack_policy, OttrpgId source_id);
OTTRPG_API int32_t ottrpg_condition_remove(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8);
/* Retorna 1 (possui), 0 (não possui) ou erro (< 0) */
OTTRPG_API int32_t ottrpg_condition_has(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8);
OTTRPG_API int32_t ottrpg_condition_get(OttrpgEncounter* encounter, OttrpgId target, const char* key_utf8,
                                        OttrpgConditionInfo* out_info);
OTTRPG_API int32_t ottrpg_condition_get_key_at(OttrpgEncounter* encounter, OttrpgId target, int32_t index,
                                               char* buffer, int32_t buffer_size, int32_t* out_required);

/* ------------------------------------------------------------------------- */
/* Iniciativa e turnos                                                       */
/* ------------------------------------------------------------------------- */

/* lowest_first: 0 = maior age primeiro (padrão), 1 = menor age primeiro. Só antes de iniciar. */
OTTRPG_API int32_t  ottrpg_initiative_set_order(OttrpgEncounter* encounter, int32_t lowest_first);
OTTRPG_API int32_t  ottrpg_initiative_roll(OttrpgEncounter* encounter, OttrpgId id, int32_t dice_count,
                                           int32_t dice_sides, int32_t* out_total);
OTTRPG_API int32_t  ottrpg_initiative_roll_all(OttrpgEncounter* encounter, int32_t dice_count, int32_t dice_sides);
OTTRPG_API int32_t  ottrpg_initiative_set(OttrpgEncounter* encounter, OttrpgId id, int32_t value);
OTTRPG_API int32_t  ottrpg_initiative_remove(OttrpgEncounter* encounter, OttrpgId id);
OTTRPG_API int32_t  ottrpg_initiative_get_order(OttrpgEncounter* encounter, OttrpgId* out_ids,
                                                int32_t capacity, int32_t* out_count);

OTTRPG_API int32_t  ottrpg_combat_start(OttrpgEncounter* encounter);
/* Retorna o ID de quem age agora (> 0) ou erro (< 0) */
OTTRPG_API OttrpgId ottrpg_combat_next_turn(OttrpgEncounter* encounter);
OTTRPG_API OttrpgId ottrpg_combat_current(OttrpgEncounter* encounter);
OTTRPG_API OttrpgId ottrpg_combat_peek_next(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combat_round(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combat_is_active(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combat_end(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combat_is_team_defeated(OttrpgEncounter* encounter, int32_t team);
OTTRPG_API int32_t  ottrpg_combat_standing_team_count(OttrpgEncounter* encounter);

/* Condições que expiraram no último start/next_turn */
OTTRPG_API int32_t  ottrpg_combat_expired_count(OttrpgEncounter* encounter);
OTTRPG_API int32_t  ottrpg_combat_expired_get(OttrpgEncounter* encounter, int32_t index, OttrpgId* out_entity,
                                              char* key_buffer, int32_t buffer_size, int32_t* out_required);

/* ------------------------------------------------------------------------- */
/* Dados (usam o gerador do encontro: mesma semente => mesma sequência)      */
/* ------------------------------------------------------------------------- */

OTTRPG_API int32_t ottrpg_dice_roll(OttrpgEncounter* encounter, int32_t count, int32_t sides,
                                    int32_t modifier, OttrpgRollResult* out_result);
OTTRPG_API int32_t ottrpg_dice_roll_keep(OttrpgEncounter* encounter, int32_t count, int32_t sides,
                                         int32_t keep_mode, int32_t keep_count, int32_t modifier,
                                         OttrpgRollResult* out_result);
OTTRPG_API int32_t ottrpg_dice_roll_expression(OttrpgEncounter* encounter, const char* expression_utf8,
                                               OttrpgRollResult* out_result);
/* Valida a expressão sem rolar: OTTRPG_OK ou OTTRPG_ERR_INVALID_ARGUMENT (ver ottrpg_last_error) */
OTTRPG_API int32_t ottrpg_dice_validate_expression(const char* expression_utf8);
/* Valores individuais da última rolagem. out_kept (opcional) recebe 1/0 por dado. */
OTTRPG_API int32_t ottrpg_dice_last_values(OttrpgEncounter* encounter, int32_t* out_values, int32_t* out_kept,
                                           int32_t capacity, int32_t* out_count);
OTTRPG_API int32_t ottrpg_dice_last_format(OttrpgEncounter* encounter, char* buffer, int32_t buffer_size,
                                           int32_t* out_required);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* OPENTTRPG_API_H */
