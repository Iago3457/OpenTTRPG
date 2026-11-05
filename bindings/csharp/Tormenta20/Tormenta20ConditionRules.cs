using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    /// <summary>
    /// Catálogo e Regras das 13 Condições Canônicas de Tormenta20 (Edição Jogo do Ano),
    /// integradas ao ConditionTracker, ActionEconomy e Stats do Core C++.
    /// </summary>
    public static class Tormenta20ConditionRules
    {
        public const string ShakenKey = "Abalado";
        public const string FrightenedKey = "Apavorado";
        public const string ProneKey = "Caido";
        public const string BlindedKey = "Cego";
        public const string FlatFootedKey = "Desprevenido";
        public const string NauseatedKey = "Enjoado";
        public const string WeakenedKey = "Fraco";
        public const string ImmobileKey = "Imovel";
        public const string SlowedKey = "Lento";
        public const string DazzledKey = "Ofuscado";
        public const string BleedingKey = "Sangrando";
        public const string OverburdenedKey = "Sobrecarregado";
        public const string VulnerableKey = "Vulneravel";

        public static string GetConditionKey(T20Condition condition) => condition switch
        {
            T20Condition.Abalado => ShakenKey,
            T20Condition.Apavorado => FrightenedKey,
            T20Condition.Caido => ProneKey,
            T20Condition.Cego => BlindedKey,
            T20Condition.Desprevenido => FlatFootedKey,
            T20Condition.Enjoado => NauseatedKey,
            T20Condition.Fraco => WeakenedKey,
            T20Condition.Imovel => ImmobileKey,
            T20Condition.Lento => SlowedKey,
            T20Condition.Ofuscado => DazzledKey,
            T20Condition.Sangrando => BleedingKey,
            T20Condition.Sobrecarregado => OverburdenedKey,
            T20Condition.Vulneravel => VulnerableKey,
            _ => condition.ToString()
        };

        public static string GetConditionDescription(T20Condition condition) => condition switch
        {
            T20Condition.Abalado => "-2 em testes de perícia (se ficar Abalado novamente, torna-se Apavorado)",
            T20Condition.Apavorado => "-5 em testes de perícia e não pode se aproximar da fonte do medo",
            T20Condition.Caido => "-5 em ataques corpo a corpo e -5 na Defesa contra corpo a corpo",
            T20Condition.Cego => "Desprevenido e Lento; -5 em perícias de Força ou Destreza",
            T20Condition.Desprevenido => "-5 na Defesa e -5 em Reflexos",
            T20Condition.Enjoado => "Só pode realizar uma Ação Padrão OU de Movimento por turno",
            T20Condition.Fraco => "-2 em Força, Destreza e Constituição e perícias desses atributos",
            T20Condition.Imovel => "Deslocamento reduzido a 0m (não pode se movimentar)",
            T20Condition.Lento => "Deslocamento reduzido à metade e ações limitadas (1 Padrão ou Movimento)",
            T20Condition.Ofuscado => "-2 em testes de ataque e de Percepção",
            T20Condition.Sangrando => "No início do turno, testa CON (CD 15) ou sofre 1d6 de dano",
            T20Condition.Sobrecarregado => "Penalidade de armadura aumenta em -5 e deslocamento -3m",
            T20Condition.Vulneravel => "-2 na Defesa",
            _ => ""
        };

        public static string ApplyCondition(EncounterSession session, int combatantId, T20Condition condition,
                                            int durationTurns = -1, int sourceId = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Regra de Tormenta20: se já está Abalado e recebe Abalado de novo, escala para Apavorado
            if (condition == T20Condition.Abalado &&
                (session.HasCondition(combatantId, ShakenKey) || session.HasCondition(combatantId, FrightenedKey)))
            {
                session.RemoveCondition(combatantId, ShakenKey);
                session.AddCondition(combatantId, FrightenedKey, durationTurns, 1, TickTiming.TurnEnd, StackPolicy.Refresh, sourceId);
                SyncConditionStats(session, combatantId);
                return "[CONDIÇÃO T20] Já estava Abalado -> escalou automaticamente para APAVORADO (-5 em perícias)!";
            }

            string key = GetConditionKey(condition);
            TickTiming timing = condition == T20Condition.Sangrando ? TickTiming.Manual : TickTiming.TurnEnd;
            session.AddCondition(combatantId, key, durationTurns, 1, timing, StackPolicy.Refresh, sourceId);

            if (condition == T20Condition.Cego)
            {
                session.AddCondition(combatantId, FlatFootedKey, durationTurns, 1, TickTiming.TurnEnd, StackPolicy.Refresh, sourceId);
                session.AddCondition(combatantId, SlowedKey, durationTurns, 1, TickTiming.TurnEnd, StackPolicy.Refresh, sourceId);
            }

            if (condition == T20Condition.Enjoado || condition == T20Condition.Lento || condition == T20Condition.Cego)
            {
                Tormenta20ActionRules.ApplySlowedCondition(session, combatantId);
            }

            if (condition == T20Condition.Imovel)
            {
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Movement, 0, refillCurrent: true);
            }

            SyncConditionStats(session, combatantId);
            return $"[CONDIÇÃO T20] '{key}' aplicada ({GetConditionDescription(condition)}).";
        }

        public static void RemoveCondition(EncounterSession session, int combatantId, T20Condition condition)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            string key = GetConditionKey(condition);
            session.RemoveCondition(combatantId, key);

            bool stillSlowedOrNauseated = session.HasCondition(combatantId, SlowedKey) ||
                                          session.HasCondition(combatantId, NauseatedKey) ||
                                          session.HasCondition(combatantId, BlindedKey);
            bool stillImmobile = session.HasCondition(combatantId, ImmobileKey);

            if (!stillSlowedOrNauseated && !stillImmobile)
            {
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Standard, 1, refillCurrent: true);
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Movement, 1, refillCurrent: true);
            }

            SyncConditionStats(session, combatantId);
        }

        public static int GetDefenseModifier(EncounterSession session, int combatantId, bool againstRangedAttack = false)
        {
            int mod = 0;

            if (session.HasCondition(combatantId, FlatFootedKey) || session.HasCondition(combatantId, BlindedKey))
            {
                mod -= 5;
            }

            if (session.HasCondition(combatantId, VulnerableKey))
            {
                mod -= 2;
            }

            if (session.HasCondition(combatantId, ProneKey))
            {
                mod += againstRangedAttack ? +5 : -5;
            }

            return mod;
        }

        public static int GetAttributeModifier(EncounterSession session, int combatantId, AttributeType attr)
        {
            int mod = 0;
            if (session.HasCondition(combatantId, WeakenedKey) &&
                (attr == AttributeType.Strength || attr == AttributeType.Dexterity || attr == AttributeType.Constitution))
            {
                mod -= 2;
            }
            return mod;
        }

        public static int GetSkillModifier(EncounterSession session, int combatantId, T20SkillDefinition skill)
        {
            int mod = 0;

            if (session.HasCondition(combatantId, FrightenedKey))
            {
                mod -= 5;
            }
            else if (session.HasCondition(combatantId, ShakenKey))
            {
                mod -= 2;
            }

            if (session.HasCondition(combatantId, WeakenedKey) &&
                (skill.KeyAttribute == AttributeType.Strength ||
                 skill.KeyAttribute == AttributeType.Dexterity ||
                 skill.KeyAttribute == AttributeType.Constitution))
            {
                mod -= 2;
            }

            if (session.HasCondition(combatantId, BlindedKey) &&
                (skill.KeyAttribute == AttributeType.Strength || skill.KeyAttribute == AttributeType.Dexterity))
            {
                mod -= 5;
            }

            if (session.HasCondition(combatantId, FlatFootedKey) &&
                skill.Name.Equals("Reflexos", StringComparison.OrdinalIgnoreCase))
            {
                mod -= 5;
            }

            if (session.HasCondition(combatantId, DazzledKey) &&
                (skill.Name.Equals("Percepção", StringComparison.OrdinalIgnoreCase) ||
                 skill.Name.Equals("Luta", StringComparison.OrdinalIgnoreCase) ||
                 skill.Name.Equals("Pontaria", StringComparison.OrdinalIgnoreCase)))
            {
                mod -= 2;
            }

            if (session.HasCondition(combatantId, ProneKey) &&
                skill.Name.Equals("Luta", StringComparison.OrdinalIgnoreCase))
            {
                mod -= 5;
            }

            return mod;
        }

        public static List<string> ProcessStartOfTurnConditions(EncounterSession session, int combatantId, CharacterSheetData? sheet = null)
        {
            var logs = new List<string>();
            if (session == null || combatantId <= 0) return logs;

            if (session.HasCondition(combatantId, BleedingKey))
            {
                int conMod = sheet != null ? sheet.Constitution : session.GetStat(combatantId, "CON", 0);
                conMod += GetAttributeModifier(session, combatantId, AttributeType.Constitution);

                RollResult check = session.Roll("1d20");
                int total = check.DiceTotal + conMod;
                string name = sheet?.Name ?? session.GetCombatantName(combatantId);

                if (check.NaturalMaxCount > 0 || (check.NaturalMinCount == 0 && total >= 15))
                {
                    session.RemoveCondition(combatantId, BleedingKey);
                    logs.Add($"[SANGRANDO] {name} passou no teste de Constituição ({total} vs CD 15) e estancou o sangramento!");
                }
                else
                {
                    RollResult bleedRoll = session.Roll("1d6");
                    DamageResult dmg = session.DealDamage(combatantId, bleedRoll.Total, (int)T20DamageType.Untyped, DamageFlags.IgnoreReduction);
                    if (sheet != null)
                    {
                        ResourceInfo hp = session.GetResource(combatantId, Tormenta20ActionRules.HealthResourceId);
                        sheet.CurrentHp = hp.Current;
                    }
                    logs.Add($"[SANGRANDO] {name} falhou no teste de Constituição ({total} vs CD 15) e perdeu {dmg.FinalAmount} PV de sangramento!");
                }
            }

            return logs;
        }

        public static List<string> GetActiveConditionNames(EncounterSession session, int combatantId)
        {
            var active = new List<string>();
            foreach (T20Condition cond in Enum.GetValues(typeof(T20Condition)))
            {
                string key = GetConditionKey(cond);
                if (session.HasCondition(combatantId, key))
                {
                    active.Add(key);
                }
            }
            return active;
        }

        private static void SyncConditionStats(EncounterSession session, int combatantId)
        {
            session.SetStat(combatantId, "Mod_Defesa_Condicoes", GetDefenseModifier(session, combatantId));
        }
    }
}

