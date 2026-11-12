using System;
using System.Runtime.InteropServices;

namespace OpenTTRPG.Native
{
    /// <summary>
    /// Declarações P/Invoke diretas para a biblioteca nativa openttrpg_core (DLL / .so).
    /// </summary>
    public static class NativeMethods
    {
        private const string LibName = "openttrpg_core";

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_api_version();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ottrpg_version_string();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ottrpg_last_error();

        // Encontro
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ottrpg_encounter_create(uint seed);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ottrpg_encounter_destroy(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_encounter_set_auto_skip_defeated(IntPtr encounter, int enabled);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_encounter_set_auto_tick_conditions(IntPtr encounter, int enabled);

        // Combatentes
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_add(IntPtr encounter, [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8,
                                                      int initiativeModifier, int maxHealth, int team);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_remove(IntPtr encounter, int id);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_count(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_list(IntPtr encounter, [Out] int[] outIds, int capacity, out int outCount);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_get_info(IntPtr encounter, int id, out CombatantInfo outInfo);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_get_name(IntPtr encounter, int id, [Out] byte[] buffer, int bufferSize, out int outRequired);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_name(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_team(IntPtr encounter, int id, int team);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_initiative_modifier(IntPtr encounter, int id, int modifier);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_forced_out(IntPtr encounter, int id, int outState);

        // Recursos
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_add(IntPtr encounter, int id, int resourceId,
                                                     [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8, int max, int minimum);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_remove(IntPtr encounter, int id, int resourceId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_get(IntPtr encounter, int id, int resourceId, out ResourceInfo outInfo);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_set_current(IntPtr encounter, int id, int resourceId, int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_set_max(IntPtr encounter, int id, int resourceId, int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_set_temp(IntPtr encounter, int id, int resourceId, int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_spend(IntPtr encounter, int id, int resourceId, int amount);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_resource_restore(IntPtr encounter, int id, int resourceId, int amount, out int outRestored);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_vital_resource(IntPtr encounter, int id, int resourceId);

        // Dano e Cura
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_set_response(IntPtr encounter, int id, int damageType, int response);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_set_reduction(IntPtr encounter, int id, int damageType, int amount);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_set_global_reduction(IntPtr encounter, int id, int amount);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_set_order(IntPtr encounter, int id, int order);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_deal(IntPtr encounter, int target, int amount, int damageType,
                                                    int flags, out DamageResult outResult);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_damage_preview(IntPtr encounter, int target, int amount, int damageType,
                                                       int flags, out DamageResult outResult);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_heal(IntPtr encounter, int target, int amount, out int outHealed);

        // Condições
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_condition_add(IntPtr encounter, int target, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8,
                                                      int duration, int intensity, int tickTiming, int stackPolicy, int sourceId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_condition_remove(IntPtr encounter, int target, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_condition_has(IntPtr encounter, int target, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_condition_get(IntPtr encounter, int target, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8,
                                                      out ConditionInfo outInfo);

        // Iniciativa
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_set_order(IntPtr encounter, int lowestFirst);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_roll(IntPtr encounter, int id, int diceCount, int diceSides, out int outTotal);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_roll_list(IntPtr encounter, [In] int[] ids, int count, int diceCount, int diceSides);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_roll_all(IntPtr encounter, int diceCount, int diceSides);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_set(IntPtr encounter, int id, int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_remove(IntPtr encounter, int id);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_initiative_get_order(IntPtr encounter, [Out] int[] outIds, int capacity, out int outCount);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_start(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_next_turn(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_current(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_peek_next(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_round(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_is_active(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_end(IntPtr encounter);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_is_team_defeated(IntPtr encounter, int team);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combat_standing_team_count(IntPtr encounter);

        // Dados
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_dice_roll(IntPtr encounter, int count, int sides, int modifier, out RollResult outResult);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_dice_roll_expression(IntPtr encounter, [MarshalAs(UnmanagedType.LPUTF8Str)] string expressionUtf8,
                                                             out RollResult outResult);

        // Economia de Ações e Fila de Prioridades de Turno (Genérico)
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_define_type(IntPtr encounter, int id, int actionType,
                                                           [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8,
                                                           int priority, int maxPerTurn);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_remove_type(IntPtr encounter, int id, int actionType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_add_fallback(IntPtr encounter, int id, int requiredType, int providerType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_remove_fallback(IntPtr encounter, int id, int requiredType, int providerType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_clear_fallbacks(IntPtr encounter, int id, int requiredType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_get_slot(IntPtr encounter, int id, int actionType, out ActionSlotInfo outInfo);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_set_max(IntPtr encounter, int id, int actionType, int maxPerTurn, int refillCurrent);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_set_current(IntPtr encounter, int id, int actionType, int current);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_modify_current(IntPtr encounter, int id, int actionType, int delta);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_reset_turn(IntPtr encounter, int id);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_can_use(IntPtr encounter, int id, int requiredType, out int outConsumedType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_spend(IntPtr encounter, int id, int requiredType, out int outConsumedType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_spend_compound(IntPtr encounter, int id, [In] int[] requiredTypes, int count);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_enqueue(IntPtr encounter, int id, int requiredType,
                                                       [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8,
                                                       int targetId, out int outActionId, out int outConsumedType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_enqueue_compound(IntPtr encounter, int id, [In] int[] requiredTypes,
                                                                int count, int executionPriority,
                                                                [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8,
                                                                int targetId, out int outActionId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_queue_count(IntPtr encounter, int id);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_queue_get(IntPtr encounter, int id, int index,
                                                         out QueuedActionInfo outInfo, [Out] byte[] nameBuffer,
                                                         int bufferSize, out int outRequired);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_queue_pop(IntPtr encounter, int id,
                                                         out QueuedActionInfo outInfo, [Out] byte[] nameBuffer,
                                                         int bufferSize, out int outRequired);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_queue_cancel(IntPtr encounter, int id, int actionId, int refundSlot);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_action_queue_clear(IntPtr encounter, int id, int refundSlots);

        // Árvore de Habilidades e Pré-requisitos (Genérico)
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_set_stat(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8, int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_get_stat(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8, int defaultValue, out int outValue);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_unlock_ability(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_revoke_ability(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_has_ability(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_record_action_in_turn(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string actionKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_combatant_has_used_action_in_turn(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string actionKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_register(IntPtr encounter,
                                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string keyUtf8,
                                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8,
                                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string parentKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_set_req_root(IntPtr encounter,
                                                             [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8,
                                                             int isUsageReq, int rootKind, int threshold);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_add_req_node(IntPtr encounter,
                                                             [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8,
                                                             int isUsageReq, int parentNodeId, int reqKind,
                                                             [MarshalAs(UnmanagedType.LPUTF8Str)] string targetKeyUtf8,
                                                             int threshold, int resourceId, out int outNodeId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_can_learn(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_learn(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ottrpg_ability_can_use(IntPtr encounter, int id, [MarshalAs(UnmanagedType.LPUTF8Str)] string abilityKeyUtf8);
    }
}

