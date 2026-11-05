using System;
using System.Runtime.InteropServices;

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
}

