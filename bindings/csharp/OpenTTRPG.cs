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
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr);
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
            NativeMethods.ottrpg_initiative_get_order(_handle, null, 0, out int count);
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
            NativeMethods.ottrpg_combatant_get_name(_handle, combatantId, null, 0, out int required);
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
