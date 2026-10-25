using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenTTRPG.Native
{
    public enum OttrpgStatus : int
    {
        Ok = 0,
        ErrNullHandle = -1,
        ErrNotFound = -2,
        ErrInvalidArgument = -3,
        ErrInvalidState = -4,
        ErrBufferTooSmall = -5,
        ErrInsufficientResource = -6,
        ErrInternal = -99
    }

    public enum DamageResponse : int
    {
        Normal = 0,
        Resistant = 1,
        Vulnerable = 2,
        Immune = 3
    }

    [Flags]
    public enum DamageFlags : int
    {
        None = 0,
        IgnoreReduction = 1 << 0,
        IgnoreResponse = 1 << 1
    }

    public enum TickTiming : int
    {
        TurnStart = 0,
        TurnEnd = 1,
        RoundEnd = 2,
        Manual = 3
    }

    public enum StackPolicy : int
    {
        Refresh = 0,
        Replace = 1,
        Stack = 2,
        Ignore = 3
    }

    public enum KeepMode : int
    {
        All = 0,
        Highest = 1,
        Lowest = 2
    }

    public enum RequirementKind : int
    {
        AllOf = 0,            // Nó lógico E (AND)
        AnyOf = 1,            // Nó lógico OU (OR)
        Not = 2,              // Nó lógico NÃO (NOT)
        AtLeastN = 3,         // Nó lógico: pelo menos N filhos
        AbilityUnlocked = 10, // Folha: possui habilidade/talento prévio
        StatAtLeast = 11,     // Folha: possui estatística/atributo >= threshold
        ConditionActive = 12, // Folha: possui condição ativa (intensidade >= threshold)
        ActionUsedInTurn = 13,// Folha: já executou ação neste turno
        ResourceAtLeast = 14  // Folha: possui recurso >= threshold
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DamageResult
    {
        public int RawAmount;
        public int ReducedBy;
        public int FinalAmount;
        public int AbsorbedByTemp;
        public int AppliedToCurrent;
        public int Overflow;
        public DamageResponse Response;
        public int Depleted; // 1 = caiu (recurso vital <= 0)

        public bool IsDepleted => Depleted != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResourceInfo
    {
        public int Current;
        public int Max;
        public int Temp;
        public int Minimum;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CombatantInfo
    {
        public int Id;
        public int InitiativeModifier;
        public int Team;
        public int InInitiative;
        public int Initiative;
        public int IsDefeated;
        public int VitalResource;
        public int ResourceCount;
        public int ConditionCount;

        public bool HasInitiative => InInitiative != 0;
        public bool Defeated => IsDefeated != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ConditionInfo
    {
        public int Duration;
        public int Intensity;
        public TickTiming Timing;
        public int SourceId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RollResult
    {
        public int Total;
        public int DiceTotal;
        public int Modifier;
        public int DiceCount;
        public int NaturalMaxCount;
        public int NaturalMinCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ActionSlotInfo
    {
        public int ActionType;
        public int Priority;
        public int MaxPerTurn; // < 0 = ilimitado
        public int Current;    // < 0 = ilimitado
        public int FallbackCount;

        public bool IsUnlimited => MaxPerTurn < 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QueuedActionInfo
    {
        public int ActionId;
        public int RequiredType;
        public int ConsumedType;
        public int Cost;
        public int TargetId;
        public int Priority;
    }

    public sealed class QueuedActionEntry
    {
        public int ActionId { get; }
        public string Name { get; }
        public int RequiredType { get; }
        public int ConsumedType { get; }
        public int Cost { get; }
        public int TargetId { get; }
        public int Priority { get; }

        public bool UsedFallback => RequiredType != ConsumedType;

        public QueuedActionEntry(QueuedActionInfo info, string name)
        {
            ActionId = info.ActionId;
            Name = name;
            RequiredType = info.RequiredType;
            ConsumedType = info.ConsumedType;
            Cost = info.Cost;
            TargetId = info.TargetId;
            Priority = info.Priority;
        }
    }

    /// <summary>
    /// P/Invoke bindings diretas para a biblioteca nativa openttrpg_core (DLL / .so).
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

    /// <summary>
    /// Gerenciador orientado a objetos em C# para interagir de forma segura e fluida com a DLL nativa.
    /// </summary>
    public sealed class EncounterSession : IDisposable
    {
        private IntPtr _handle;

        public EncounterSession(uint seed = 0)
        {
            _handle = NativeMethods.ottrpg_encounter_create(seed);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Falha ao inicializar encontro nativo: {GetLastError()}");
            }
        }

        public static string GetLastError()
        {
            IntPtr ptr = NativeMethods.ottrpg_last_error();
            return ptr == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringUTF8(ptr) ?? string.Empty);
        }

        public int AddCombatant(string name, int initiativeModifier, int maxHealth, int team = 0)
        {
            EnsureNotDisposed();
            int id = NativeMethods.ottrpg_combatant_add(_handle, name, initiativeModifier, maxHealth, team);
            if (id <= 0)
            {
                throw new InvalidOperationException($"Erro ao adicionar combatente: {GetLastError()}");
            }
            return id;
        }

        public void SetInitiative(int combatantId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_set(_handle, combatantId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir iniciativa: {GetLastError()}");
            }
        }

        public int RollInitiative(int combatantId, int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_roll(_handle, combatantId, diceCount, diceSides, out int total);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa: {GetLastError()}");
            }
            return total;
        }

        /// <summary>
        /// Rola iniciativa apenas para os combatentes selecionados na lista (permite escolher quem participa do combate).
        /// </summary>
        public void RollInitiativeForList(int[] combatantIds, int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            if (combatantIds == null)
            {
                throw new ArgumentNullException(nameof(combatantIds));
            }
            if (combatantIds.Length == 0)
            {
                return;
            }
            int res = NativeMethods.ottrpg_initiative_roll_list(_handle, combatantIds, combatantIds.Length, diceCount, diceSides);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa para a lista selecionada: {GetLastError()}");
            }
        }

        public void RollInitiativeForAll(int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_roll_all(_handle, diceCount, diceSides);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa para todos: {GetLastError()}");
            }
        }

        public void RemoveFromInitiative(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_remove(_handle, combatantId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover da iniciativa: {GetLastError()}");
            }
        }

        public int[] GetInitiativeOrder()
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_initiative_get_order(_handle, null!, 0, out int count);
            if (count <= 0)
            {
                return Array.Empty<int>();
            }
            int[] ids = new int[count];
            NativeMethods.ottrpg_initiative_get_order(_handle, ids, count, out _);
            return ids;
        }

        public CombatantInfo GetCombatantInfo(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_get_info(_handle, combatantId, out CombatantInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar combatente: {GetLastError()}");
            }
            return info;
        }

        public string GetCombatantName(int combatantId)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combatant_get_name(_handle, combatantId, null!, 0, out int required);
            if (required <= 0)
            {
                return string.Empty;
            }
            byte[] buffer = new byte[required];
            int res = NativeMethods.ottrpg_combatant_get_name(_handle, combatantId, buffer, required, out _);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao obter nome do combatente: {GetLastError()}");
            }
            return Encoding.UTF8.GetString(buffer, 0, required - 1);
        }

        public void AddResource(int combatantId, int resourceId, string name, int max, int minimum = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_add(_handle, combatantId, resourceId, name, max, minimum);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar recurso: {GetLastError()}");
            }
        }

        public ResourceInfo GetResource(int combatantId, int resourceId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_get(_handle, combatantId, resourceId, out ResourceInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao obter recurso: {GetLastError()}");
            }
            return info;
        }

        public void SetResourceCurrent(int combatantId, int resourceId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_set_current(_handle, combatantId, resourceId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao atualizar recurso: {GetLastError()}");
            }
        }

        public bool SpendResource(int combatantId, int resourceId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_spend(_handle, combatantId, resourceId, amount);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar recurso: {GetLastError()}");
            }
            return true;
        }

        public int RestoreResource(int combatantId, int resourceId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_restore(_handle, combatantId, resourceId, amount, out int restored);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao restaurar recurso: {GetLastError()}");
            }
            return restored;
        }

        public void SetForcedOut(int combatantId, bool forcedOut)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_set_forced_out(_handle, combatantId, forcedOut ? 1 : 0);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao alterar estado fora de combate: {GetLastError()}");
            }
        }

        public void EndCombat()
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combat_end(_handle);
        }

        public void StartCombat()
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combat_start(_handle);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao iniciar combate: {GetLastError()}");
            }
        }

        public int NextTurn()
        {
            EnsureNotDisposed();
            int nextId = NativeMethods.ottrpg_combat_next_turn(_handle);
            if (nextId <= 0)
            {
                throw new InvalidOperationException($"Erro ao avançar turno: {GetLastError()}");
            }
            return nextId;
        }

        public DamageResult DealDamage(int targetId, int amount, int damageType = 0, DamageFlags flags = DamageFlags.None)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_deal(_handle, targetId, amount, damageType, (int)flags, out DamageResult result);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao aplicar dano: {GetLastError()}");
            }
            return result;
        }

        public int Heal(int targetId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_heal(_handle, targetId, amount, out int healed);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao curar: {GetLastError()}");
            }
            return healed;
        }

        public RollResult Roll(string expression)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_dice_roll_expression(_handle, expression, out RollResult result);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new ArgumentException($"Expressão de dados inválida: {GetLastError()}");
            }
            return result;
        }

        // ----- Economia de Ações e Fila de Prioridades (Genérico) -----

        public void DefineActionType(int combatantId, int actionType, string name, int priority, int maxPerTurn)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_define_type(_handle, combatantId, actionType, name, priority, maxPerTurn);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir tipo de ação: {GetLastError()}");
            }
        }

        public void RemoveActionType(int combatantId, int actionType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_remove_type(_handle, combatantId, actionType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover tipo de ação: {GetLastError()}");
            }
        }

        public void AddActionFallback(int combatantId, int requiredType, int providerType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_add_fallback(_handle, combatantId, requiredType, providerType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar regra de conversão de ação: {GetLastError()}");
            }
        }

        public void RemoveActionFallback(int combatantId, int requiredType, int providerType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_remove_fallback(_handle, combatantId, requiredType, providerType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover regra de conversão de ação: {GetLastError()}");
            }
        }

        public void ClearActionFallbacks(int combatantId, int requiredType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_clear_fallbacks(_handle, combatantId, requiredType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao limpar regras de conversão de ação: {GetLastError()}");
            }
        }

        public ActionSlotInfo GetActionSlot(int combatantId, int actionType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_get_slot(_handle, combatantId, actionType, out ActionSlotInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar slot de ação: {GetLastError()}");
            }
            return info;
        }

        public void SetActionSlotMax(int combatantId, int actionType, int maxPerTurn, bool refillCurrent = true)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_set_max(_handle, combatantId, actionType, maxPerTurn, refillCurrent ? 1 : 0);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir máximo de ações: {GetLastError()}");
            }
        }

        public void SetActionSlotCurrent(int combatantId, int actionType, int current)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_set_current(_handle, combatantId, actionType, current);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir ações atuais: {GetLastError()}");
            }
        }

        public void ModifyActionSlotCurrent(int combatantId, int actionType, int delta)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_modify_current(_handle, combatantId, actionType, delta);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao modificar ações atuais: {GetLastError()}");
            }
        }

        public void ResetTurnActions(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_reset_turn(_handle, combatantId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao resetar ações do turno: {GetLastError()}");
            }
        }

        public bool CanUseAction(int combatantId, int requiredType, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_can_use(_handle, combatantId, requiredType, out consumedType);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao verificar ação: {GetLastError()}");
            }
            return res == 1;
        }

        public bool SpendAction(int combatantId, int requiredType, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_spend(_handle, combatantId, requiredType, out consumedType);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar ação: {GetLastError()}");
            }
            return true;
        }

        public bool SpendCompoundAction(int combatantId, int[] requiredTypes)
        {
            EnsureNotDisposed();
            if (requiredTypes == null || requiredTypes.Length == 0)
            {
                return true;
            }
            int res = NativeMethods.ottrpg_action_spend_compound(_handle, combatantId, requiredTypes, requiredTypes.Length);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar ação composta: {GetLastError()}");
            }
            return true;
        }

        public bool EnqueueAction(int combatantId, int requiredType, string name, int targetId,
                                  out int actionId, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_enqueue(_handle, combatantId, requiredType, name ?? string.Empty,
                                                          targetId, out actionId, out consumedType);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao enfileirar ação: {GetLastError()}");
            }
            return true;
        }

        public bool EnqueueCompoundAction(int combatantId, int[] requiredTypes, int executionPriority,
                                          string name, int targetId, out int actionId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_enqueue_compound(
                _handle, combatantId, requiredTypes, requiredTypes?.Length ?? 0,
                executionPriority, name ?? string.Empty, targetId, out actionId);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao enfileirar ação composta: {GetLastError()}");
            }
            return true;
        }

        public int GetActionQueueCount(int combatantId)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
        }

        public QueuedActionEntry[] GetActionQueue(int combatantId)
        {
            EnsureNotDisposed();
            int count = NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
            if (count <= 0)
            {
                return Array.Empty<QueuedActionEntry>();
            }
            var list = new QueuedActionEntry[count];
            for (int i = 0; i < count; i++)
            {
                NativeMethods.ottrpg_action_queue_get(_handle, combatantId, i, out QueuedActionInfo info, null!, 0, out int req);
                byte[] buf = new byte[Math.Max(1, req)];
                NativeMethods.ottrpg_action_queue_get(_handle, combatantId, i, out info, buf, buf.Length, out _);
                string name = req > 1 ? Encoding.UTF8.GetString(buf, 0, req - 1) : string.Empty;
                list[i] = new QueuedActionEntry(info, name);
            }
            return list;
        }

        public bool PopNextAction(int combatantId, out QueuedActionEntry entry)
        {
            EnsureNotDisposed();
            int count = NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
            if (count <= 0)
            {
                entry = null!;
                return false;
            }
            NativeMethods.ottrpg_action_queue_get(_handle, combatantId, 0, out _, null!, 0, out int req);
            byte[] buf = new byte[Math.Max(1, req)];
            int res = NativeMethods.ottrpg_action_queue_pop(_handle, combatantId, out QueuedActionInfo info, buf, buf.Length, out _);
            if (res != (int)OttrpgStatus.Ok)
            {
                entry = null!;
                return false;
            }
            string name = req > 1 ? Encoding.UTF8.GetString(buf, 0, req - 1) : string.Empty;
            entry = new QueuedActionEntry(info, name);
            return true;
        }

        public bool CancelQueuedAction(int combatantId, int actionId, bool refundSlot = true)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_queue_cancel(_handle, combatantId, actionId, refundSlot ? 1 : 0);
            return res == (int)OttrpgStatus.Ok;
        }

        public void ClearActionQueue(int combatantId, bool refundSlots = false)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_action_queue_clear(_handle, combatantId, refundSlots ? 1 : 0);
        }

        // ----- Árvore de Habilidades, Estatísticas e Pré-requisitos (Genérico) -----

        public void SetStat(int combatantId, string key, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_set_stat(_handle, combatantId, key, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir estatística '{key}': {GetLastError()}");
            }
        }

        public int GetStat(int combatantId, string key, int defaultValue = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_get_stat(_handle, combatantId, key, defaultValue, out int val);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar estatística '{key}': {GetLastError()}");
            }
            return val;
        }

        public void GrantAbilityUnchecked(int combatantId, string abilityKey)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_unlock_ability(_handle, combatantId, abilityKey);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao conceder habilidade '{abilityKey}': {GetLastError()}");
            }
        }

        public bool HasAbility(int combatantId, string abilityKey)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_combatant_has_ability(_handle, combatantId, abilityKey) == 1;
        }

        public void RecordActionInTurn(int combatantId, string actionKey)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combatant_record_action_in_turn(_handle, combatantId, actionKey);
        }

        public bool HasUsedActionInTurn(int combatantId, string actionKey)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_combatant_has_used_action_in_turn(_handle, combatantId, actionKey) == 1;
        }

        public void RegisterAbilityInTree(string key, string name, string parentKey = "")
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_register(_handle, key, name, parentKey ?? string.Empty);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao registrar habilidade '{key}' na árvore: {GetLastError()}");
            }
        }

        public void SetAbilityRequirementRoot(string abilityKey, bool isUsageRequirement, RequirementKind logicalKind, int threshold = 1)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_set_req_root(_handle, abilityKey, isUsageRequirement ? 1 : 0, (int)logicalKind, threshold);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao configurar raiz de pré-requisitos de '{abilityKey}': {GetLastError()}");
            }
        }

        public int AddAbilityRequirementNode(string abilityKey, bool isUsageRequirement, int parentNodeId,
                                             RequirementKind kind, string targetKey = "", int threshold = 1, int resourceId = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_add_req_node(
                _handle, abilityKey, isUsageRequirement ? 1 : 0, parentNodeId,
                (int)kind, targetKey ?? string.Empty, threshold, resourceId, out int nodeId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar nó de pré-requisito em '{abilityKey}': {GetLastError()}");
            }
            return nodeId;
        }

        public bool CanLearnAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_can_learn(_handle, combatantId, abilityKey);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao avaliar concessão de '{abilityKey}': {GetLastError()}");
            }
            unmetReasons = res == 1 ? string.Empty : GetLastError();
            return res == 1;
        }

        public bool TryLearnAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_learn(_handle, combatantId, abilityKey);
            if (res == (int)OttrpgStatus.Ok)
            {
                unmetReasons = string.Empty;
                return true;
            }
            unmetReasons = GetLastError();
            return false;
        }

        public bool CanUseAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_can_use(_handle, combatantId, abilityKey);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao avaliar uso de '{abilityKey}': {GetLastError()}");
            }
            unmetReasons = res == 1 ? string.Empty : GetLastError();
            return res == 1;
        }

        public int CurrentTurn => NativeMethods.ottrpg_combat_current(_handle);
        public int Round => NativeMethods.ottrpg_combat_round(_handle);
        public bool IsActive => NativeMethods.ottrpg_combat_is_active(_handle) != 0;

        private void EnsureNotDisposed()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(EncounterSession));
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.ottrpg_encounter_destroy(_handle);
                _handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~EncounterSession()
        {
            Dispose();
        }
    }
}
