using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20BleedingCheckResult
    {
        public int NaturalRoll { get; set; }
        public int ConModifier { get; set; }
        public int Total { get; set; }
        public bool Stabilized { get; set; }
        public int DamageTaken { get; set; }
        public bool Died { get; set; }
        public T20HealthState NewHealthState { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Catálogo e Regras das Condições Canônicas e do Estado de Morrendo de Tormenta20 (Edição Jogo do Ano),
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
        public const string UnconsciousKey = "Inconsciente";
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
            T20Condition.Inconsciente => UnconsciousKey,
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
            T20Condition.Inconsciente => "Indefeso (-10 na Defesa) e não pode realizar ações",
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
            TickTiming timing = (condition == T20Condition.Sangrando || condition == T20Condition.Inconsciente)
                ? TickTiming.Manual
                : TickTiming.TurnEnd;
            session.AddCondition(combatantId, key, durationTurns, 1, timing, StackPolicy.Refresh, sourceId);

            if (condition == T20Condition.Cego)
            {
                session.AddCondition(combatantId, FlatFootedKey, durationTurns, 1, TickTiming.TurnEnd, StackPolicy.Refresh, sourceId);
                session.AddCondition(combatantId, SlowedKey, durationTurns, 1, TickTiming.TurnEnd, StackPolicy.Refresh, sourceId);
            }

            if (condition == T20Condition.Inconsciente)
            {
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Standard, 0, refillCurrent: true);
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Movement, 0, refillCurrent: true);
            }
            else if (condition == T20Condition.Enjoado || condition == T20Condition.Lento || condition == T20Condition.Cego)
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

            bool stillUnconscious = session.HasCondition(combatantId, UnconsciousKey);
            bool stillSlowedOrNauseated = session.HasCondition(combatantId, SlowedKey) ||
                                          session.HasCondition(combatantId, NauseatedKey) ||
                                          session.HasCondition(combatantId, BlindedKey);
            bool stillImmobile = session.HasCondition(combatantId, ImmobileKey);

            if (!stillUnconscious && !stillSlowedOrNauseated && !stillImmobile)
            {
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Standard, 1, refillCurrent: true);
                session.SetActionSlotMax(combatantId, (int)T20ActionType.Movement, 1, refillCurrent: true);
            }

            SyncConditionStats(session, combatantId);
        }

        /// <summary>
        /// Sincroniza as condições de Ferimentos e Morte de Tormenta20 (Inconsciente, Sangrando/Morrendo e Morto)
        /// entre a ficha do personagem e o combatente no Core C++.
        /// </summary>
        public static string SyncDyingAndUnconsciousState(EncounterSession session, int combatantId, CharacterSheetData sheet)
        {
            if (session == null || combatantId <= 0 || sheet == null) return string.Empty;

            int deathThreshold = sheet.GetDeathThreshold();

            if (sheet.CurrentHp <= deathThreshold)
            {
                sheet.IsBleedingOut = false;
                session.RemoveCondition(combatantId, BleedingKey);
                ApplyCondition(session, combatantId, T20Condition.Inconsciente);
                session.SetForcedOut(combatantId, true);
                return sheet.IsEnemy
                    ? $"[DERROTADO] {sheet.Name} chegou a {sheet.CurrentHp} PV e foi derrotado!"
                    : $"[MORTE] {sheet.Name} atingiu {sheet.CurrentHp} PV (limite de morte {deathThreshold} PV) e MORREU!";
            }

            if (sheet.CurrentHp <= 0)
            {
                session.SetForcedOut(combatantId, false);
                ApplyCondition(session, combatantId, T20Condition.Inconsciente);
                if (sheet.IsBleedingOut)
                {
                    ApplyCondition(session, combatantId, T20Condition.Sangrando);
                    return $"[MORRENDO] {sheet.Name} caiu a {sheet.CurrentHp} PV! Está Inconsciente e Sangrando (Teste CON CD 15 no início do turno; morre em {deathThreshold} PV).";
                }
                else
                {
                    session.RemoveCondition(combatantId, BleedingKey);
                    return $"[INCONSCIENTE] {sheet.Name} está Inconsciente e Estabilizado ({sheet.CurrentHp} PV).";
                }
            }

            // PV > 0: Consciente!
            sheet.IsBleedingOut = false;
            session.SetForcedOut(combatantId, false);
            if (session.HasCondition(combatantId, UnconsciousKey))
            {
                RemoveCondition(session, combatantId, T20Condition.Inconsciente);
            }
            if (session.HasCondition(combatantId, BleedingKey))
            {
                RemoveCondition(session, combatantId, T20Condition.Sangrando);
            }
            return string.Empty;
        }

        /// <summary>
        /// Executa o Teste de Constituição (CD 15) contra Sangramento / Estado de Morrendo segundo Tormenta20 JdA.
        /// Funciona tanto durante o combate (sincronizado com o Core) quanto diretamente na Ficha do Personagem.
        /// </summary>
        public static T20BleedingCheckResult RollDeathAndBleedingCheck(
            EncounterSession session,
            CharacterSheetData? sheet,
            int combatantId = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            int conMod = sheet != null
                ? sheet.Constitution
                : (combatantId > 0 ? session.GetStat(combatantId, "CON", 0) : 0);

            if (combatantId > 0)
            {
                conMod += GetAttributeModifier(session, combatantId, AttributeType.Constitution);
            }

            RollResult check = session.Roll("1d20");
            int natural = check.DiceTotal;
            int total = natural + conMod;
            string name = sheet?.Name ?? (combatantId > 0 ? session.GetCombatantName(combatantId) : "Personagem");
            string modStr = conMod >= 0 ? $"+{conMod}" : conMod.ToString();

            bool passed = check.NaturalMaxCount > 0 || (check.NaturalMinCount == 0 && total >= 15);
            if (passed)
            {
                if (sheet != null)
                {
                    sheet.IsBleedingOut = false;
                }
                if (combatantId > 0)
                {
                    session.RemoveCondition(combatantId, BleedingKey);
                }

                T20HealthState stateAfter = sheet?.GetHealthState(session, combatantId) ?? T20HealthState.Healthy;
                string msg = (sheet != null && sheet.CurrentHp <= 0)
                    ? $"[ESTABILIZOU!] {name} passou no teste de Constituição ({total} [1d20={natural}{modStr}] vs CD 15)! Estancou o sangramento e agora está Inconsciente e Estabilizado ({sheet.CurrentHp} PV)."
                    : $"[SANGRAMENTO ESTANCADO!] {name} passou no teste de Constituição ({total} [1d20={natural}{modStr}] vs CD 15) e removeu a condição Sangrando!";

                return new T20BleedingCheckResult
                {
                    NaturalRoll = natural,
                    ConModifier = conMod,
                    Total = total,
                    Stabilized = true,
                    DamageTaken = 0,
                    Died = false,
                    NewHealthState = stateAfter,
                    Message = msg
                };
            }
            else
            {
                RollResult bleedRoll = session.Roll("1d6");
                int dmgTaken = bleedRoll.Total;

                if (combatantId > 0)
                {
                    DamageResult dmg = session.DealDamage(combatantId, bleedRoll.Total, (int)T20DamageType.Untyped, DamageFlags.IgnoreReduction);
                    dmgTaken = dmg.FinalAmount > 0 ? dmg.FinalAmount : bleedRoll.Total;
                    if (sheet != null)
                    {
                        ResourceInfo hp = session.GetResource(combatantId, Tormenta20ActionRules.HealthResourceId);
                        // Se o pool do Core estava limitado a 0 (ex.: combatente criado sem mínimo negativo), desconta na ficha até o limite de morte
                        if (hp.Minimum == 0 && sheet.CurrentHp <= 0)
                        {
                            sheet.TakeDamage(bleedRoll.Total);
                        }
                        else
                        {
                            sheet.CurrentHp = hp.Current;
                            if (sheet.CurrentHp <= sheet.GetDeathThreshold())
                                sheet.IsBleedingOut = false;
                        }
                    }
                }
                else if (sheet != null)
                {
                    sheet.TakeDamage(bleedRoll.Total);
                }

                bool died = sheet != null && sheet.CurrentHp <= sheet.GetDeathThreshold();
                if (died && combatantId > 0 && sheet != null)
                {
                    SyncDyingAndUnconsciousState(session, combatantId, sheet);
                }

                T20HealthState stateAfter = sheet?.GetHealthState(session, combatantId) ?? T20HealthState.Dying;
                string msg = died
                    ? $"[MORTE!] {name} falhou no teste de Constituição ({total} [1d20={natural}{modStr}] vs CD 15), perdeu {dmgTaken} PV de sangramento e atingiu {sheet!.CurrentHp} PV (limite {sheet.GetDeathThreshold()} PV) — {name} MORREU!"
                    : $"[SANGRANDO] {name} falhou no teste de Constituição ({total} [1d20={natural}{modStr}] vs CD 15) e perdeu {dmgTaken} PV de sangramento!" +
                      (sheet != null ? $" (PV atual: {sheet.CurrentHp}/{sheet.MaxHp} | Morre em {sheet.GetDeathThreshold()} PV)" : "");

                return new T20BleedingCheckResult
                {
                    NaturalRoll = natural,
                    ConModifier = conMod,
                    Total = total,
                    Stabilized = false,
                    DamageTaken = dmgTaken,
                    Died = died,
                    NewHealthState = stateAfter,
                    Message = msg
                };
            }
        }

        public static int GetDefenseModifier(EncounterSession session, int combatantId, bool againstRangedAttack = false)
        {
            int mod = 0;

            if (session.HasCondition(combatantId, UnconsciousKey))
            {
                mod -= 10;
            }
            else if (session.HasCondition(combatantId, FlatFootedKey) || session.HasCondition(combatantId, BlindedKey))
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

            bool isBleeding = session.HasCondition(combatantId, BleedingKey) || (sheet != null && sheet.IsBleedingOut && sheet.CurrentHp <= 0);
            if (isBleeding)
            {
                var res = RollDeathAndBleedingCheck(session, sheet, combatantId);
                logs.Add(res.Message);
            }
            else if (sheet != null && sheet.CurrentHp <= 0 && sheet.CurrentHp > sheet.GetDeathThreshold())
            {
                logs.Add($"[INCONSCIENTE] {sheet.Name} está Inconsciente e Estabilizado ({sheet.CurrentHp} PV) — não pode agir até receber cura acima de 0 PV.");
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
