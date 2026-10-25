using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    /// <summary>
    /// Tipos de ação de Tormenta20 (Edição Jogo do Ano).
    /// A ordem na Fila de Prioridades do turno é:
    /// 1º Ação Livre (Prioridade 10) -> 2º Ação de Movimento (Prioridade 20) -> 3º Ação Padrão/Comum (Prioridade 30) -> 4º Ação Completa (Prioridade 40).
    /// </summary>
    public enum T20ActionType : int
    {
        Free = 0,     // 0 - Ação Livre (Prioridade 10)
        Movement = 1, // 1 - Ação de Movimento (Prioridade 20)
        Standard = 2, // 2 - Ação Padrão / Comum (Prioridade 30)
        Full = 3,     // 3 - Ação Completa (Consome 1 Padrão + 1 Movimento, Prioridade 40)
        Reaction = 4  // 4 - Reação (Acionada fora/durante turnos por gatilho)
    }

    public sealed class T20ActionResult
    {
        public bool Success { get; set; }
        public int ActionId { get; set; }
        public T20ActionType RequiredType { get; set; }
        public T20ActionType ConsumedType { get; set; }
        public bool UsedStandardAsMovement { get; set; }
        public int PmSpent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Regras oficiais de Economia de Ações e Fila de Prioridades de Tormenta20 (Jogo do Ano)
    /// construídas sobre o Core genérico (EncounterSession / C++ DLL).
    /// </summary>
    public static class Tormenta20ActionRules
    {
        public const int HealthResourceId = 0;
        public const int ManaResourceId = 1;

        public const int FreePriority = 10;
        public const int MovementPriority = 20;
        public const int StandardPriority = 30;
        public const int FullPriority = 40;

        /// <summary>
        /// Configura a economia de ações padrão de Tormenta20 Jogo do Ano em um combatente:
        /// - Ações Livres: Ilimitadas (Prioridade 10 -> 1ª a executar na Fila de Prioridades)
        /// - Ação de Movimento: 1 por turno (Prioridade 20 -> 2ª a executar; NÃO pode pagar Ação Padrão)
        /// - Ação Padrão (Comum): 1 por turno (Prioridade 30 -> 3ª a executar; PODE ser rebaixada para pagar Ação de Movimento)
        /// </summary>
        public static void ConfigureTurnActions(EncounterSession session, int combatantId,
                                                int standardActions = 1, int movementActions = 1, int freeActions = -1)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // 1º na Fila: Ação Livre (prioridade 10)
            session.DefineActionType(
                combatantId,
                (int)T20ActionType.Free,
                "Ação Livre",
                FreePriority,
                freeActions
            );

            // 2º na Fila: Ação de Movimento (prioridade 20)
            session.DefineActionType(
                combatantId,
                (int)T20ActionType.Movement,
                "Ação de Movimento",
                MovementPriority,
                movementActions
            );

            // 3º na Fila: Ação Padrão / Comum (prioridade 30)
            session.DefineActionType(
                combatantId,
                (int)T20ActionType.Standard,
                "Ação Padrão",
                StandardPriority,
                standardActions
            );

            // Regra de Tormenta20 Jogo do Ano:
            // Uma Ação Padrão pode ser gasta como se fosse uma Ação de Movimento, mas o contrário não vale.
            session.AddActionFallback(combatantId, (int)T20ActionType.Movement, (int)T20ActionType.Standard);
        }

        /// <summary>
        /// Aplica a condição "Lento" de Tormenta20 Jogo do Ano:
        /// O personagem só pode realizar 1 Ação Padrão OU 1 Ação de Movimento por turno
        /// (representado por 1 Ação Padrão e 0 de Movimento, já que a Padrão pode ser usada como Movimento).
        /// </summary>
        public static void ApplySlowedCondition(EncounterSession session, int combatantId)
        {
            session.SetActionSlotMax(combatantId, (int)T20ActionType.Standard, 1, refillCurrent: true);
            session.SetActionSlotMax(combatantId, (int)T20ActionType.Movement, 0, refillCurrent: true);
        }

        /// <summary>
        /// Aplica o efeito da magia/poder "Velocidade" (+1 Ação Padrão ou de Movimento extra no turno).
        /// </summary>
        public static void ApplyHasteBonus(EncounterSession session, int combatantId, bool extraStandard = true)
        {
            int targetSlot = extraStandard ? (int)T20ActionType.Standard : (int)T20ActionType.Movement;
            session.ModifyActionSlotCurrent(combatantId, targetSlot, +1);
        }

        public static string GetActionTypeName(T20ActionType type)
        {
            return type switch
            {
                T20ActionType.Free => "Ação Livre",
                T20ActionType.Movement => "Ação de Movimento",
                T20ActionType.Standard => "Ação Padrão (Comum)",
                T20ActionType.Full => "Ação Completa",
                T20ActionType.Reaction => "Reação",
                _ => "Ação"
            };
        }

        public static string FormatRemainingActions(EncounterSession session, int combatantId)
        {
            ActionSlotInfo stdSlot = session.GetActionSlot(combatantId, (int)T20ActionType.Standard);
            ActionSlotInfo movSlot = session.GetActionSlot(combatantId, (int)T20ActionType.Movement);
            ActionSlotInfo freeSlot = session.GetActionSlot(combatantId, (int)T20ActionType.Free);

            string freeStr = freeSlot.IsUnlimited ? "Ilimitadas" : $"{freeSlot.Current}/{freeSlot.MaxPerTurn}";
            return $"Padrão: {stdSlot.Current}/{stdSlot.MaxPerTurn} | Movimento: {movSlot.Current}/{movSlot.MaxPerTurn} | Livre: {freeStr}";
        }

        /// <summary>
        /// Valida ações + PM e insere a ação na Fila de Prioridades do turno do combatente.
        /// Garante:
        /// - Ordem de execução na fila: Ação Livre -> Ação de Movimento -> Ação Padrão (Comum) -> Ação Completa.
        /// - Se exigir Ação de Movimento e só houver Ação Padrão, usa a Ação Padrão como Movimento.
        /// - Se exigir Ação Padrão e só houver Ação de Movimento, bloqueia (o contrário não vale).
        /// </summary>
        public static T20ActionResult EnqueueTurnAction(EncounterSession session, int combatantId,
                                                        T20ActionType requiredType, string actionName,
                                                        int targetId = 0, int pmCost = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // 1. Verifica se possui PM suficiente antes de consumir a ação
            if (pmCost > 0)
            {
                ResourceInfo pmInfo = session.GetResource(combatantId, ManaResourceId);
                if (pmInfo.Current < pmCost)
                {
                    return new T20ActionResult
                    {
                        Success = false,
                        RequiredType = requiredType,
                        Message = $"Pontos de Mana insuficientes ({pmInfo.Current}/{pmCost} PM) para '{actionName}'."
                    };
                }
            }

            // 2. Caso especial de T20: Ação Completa = 1 Ação Padrão + 1 Ação de Movimento
            if (requiredType == T20ActionType.Full)
            {
                int[] compoundCost = { (int)T20ActionType.Standard, (int)T20ActionType.Movement };
                if (!session.EnqueueCompoundAction(combatantId, compoundCost, FullPriority, actionName, targetId, out int fullActId))
                {
                    return new T20ActionResult
                    {
                        Success = false,
                        RequiredType = T20ActionType.Full,
                        Message = $"Ações insuficientes para '{actionName}' (Ação Completa exige 1 Ação Padrão + 1 Ação de Movimento)."
                    };
                }

                if (pmCost > 0)
                {
                    session.SpendResource(combatantId, ManaResourceId, pmCost);
                }

                return new T20ActionResult
                {
                    Success = true,
                    ActionId = fullActId,
                    RequiredType = T20ActionType.Full,
                    ConsumedType = T20ActionType.Full,
                    UsedStandardAsMovement = false,
                    PmSpent = pmCost,
                    Message = $"[FILA DE PRIORIDADES] '{actionName}' agendada como Ação Completa (consumiu 1 Padrão + 1 Movimento)."
                };
            }

            // 3. Reação não consome slots do turno ativo; trata como Livre se enfileirada no turno
            int coreRequired = requiredType == T20ActionType.Reaction
                ? (int)T20ActionType.Free
                : (int)requiredType;

            if (!session.EnqueueAction(combatantId, coreRequired, actionName, targetId, out int actionId, out int consumedCore))
            {
                string reason = requiredType == T20ActionType.Standard
                    ? "Você não possui Ação Padrão disponível (Ação de Movimento NÃO pode ser usada como Ação Padrão)."
                    : $"Você não possui {GetActionTypeName(requiredType)} nem Ação Padrão disponível neste turno.";

                return new T20ActionResult
                {
                    Success = false,
                    RequiredType = requiredType,
                    Message = $"[BLOQUEADO] Não foi possível realizar '{actionName}': {reason}"
                };
            }

            if (pmCost > 0)
            {
                session.SpendResource(combatantId, ManaResourceId, pmCost);
            }

            T20ActionType consumedType = (T20ActionType)consumedCore;
            bool downgraded = (requiredType == T20ActionType.Movement && consumedType == T20ActionType.Standard);

            string conversionNote = downgraded
                ? " (Conversão T20: Ação Padrão foi utilizada como Ação de Movimento!)"
                : "";

            return new T20ActionResult
            {
                Success = true,
                ActionId = actionId,
                RequiredType = requiredType,
                ConsumedType = consumedType,
                UsedStandardAsMovement = downgraded,
                PmSpent = pmCost,
                Message = $"[FILA DE PRIORIDADES] '{actionName}' ({GetActionTypeName(requiredType)}) enfileirada com sucesso!{conversionNote}"
            };
        }

        /// <summary>
        /// Desenfileira todas as ações agendadas do turno na ordem estrita da Fila de Prioridades:
        /// 1º Ação Livre -> 2º Ação de Movimento -> 3º Ação Padrão (Comum) -> 4º Ação Completa.
        /// </summary>
        public static List<QueuedActionEntry> DrainPriorityQueueInOrder(EncounterSession session, int combatantId)
        {
            var executed = new List<QueuedActionEntry>();
            while (session.PopNextAction(combatantId, out QueuedActionEntry entry))
            {
                executed.Add(entry);
            }
            return executed;
        }
    }
}

