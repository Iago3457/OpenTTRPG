using System;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    /// <summary>
    /// Regras de Conjuração, Aprimoramentos (+PM) e Limite de Gasto de PM por Nível (e Magia Ilimitada).
    /// </summary>
    public static class Tormenta20MagicRules
    {
        public static int GetPmSpendLimit(CharacterSheetData sheet, EncounterSession? session = null, int combatantId = 0)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            int limit = Math.Max(1, sheet.Level);
            bool hasUnlimitedMagic = sheet.UnlockedTreeKeys.Contains("magia_ilimitada") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "magia_ilimitada"));

            if (hasUnlimitedMagic)
            {
                int keyAttrMod = sheet.GetAttribute(sheet.SpellKeyAttribute).Modifier;
                if (keyAttrMod > 0)
                {
                    limit += keyAttrMod;
                }
            }

            return limit;
        }

        public static int GetArcanoDeBatalhaBonus(CharacterSheetData sheet, EncounterSession? session = null, int combatantId = 0)
        {
            if (sheet == null) return 0;
            bool hasArcanoBatalha = sheet.UnlockedTreeKeys.Contains("arcano_de_batalha") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "arcano_de_batalha"));
            if (!hasArcanoBatalha) return 0;

            return Math.Max(0, sheet.GetAttribute(sheet.SpellKeyAttribute).Modifier);
        }

        public static int GetEffectiveMaxUsesPerTurn(CharacterSkill skill)
        {
            if (skill == null) return 0;

            int configured = skill.MaxUsesPerTurn;
            if (configured == 0 && !string.IsNullOrEmpty(skill.TreeKey))
            {
                var def = Tormenta20AbilityTreeRules.FindPowerDefinition(skill.TreeKey);
                if (def != null && !def.IsPassive)
                {
                    configured = def.MaxUsesPerTurn;
                }
            }

            if (configured < 0)
            {
                return 0; // Explicitamente ilimitado no turno
            }
            if (configured > 0)
            {
                return configured;
            }

            // Habilidades Livres e Reações sem limite explícito recebem 1 uso/turno por padrão
            // para evitar uso infinito abusivo; ações Padrão/Movimento/Completa são regidas pelos slots do turno.
            if (skill.Action == T20ActionType.Free || skill.Action == T20ActionType.Reaction)
            {
                return 1;
            }

            return 0;
        }

        public static T20ActionResult EnqueueSkillWithEnhancements(
            EncounterSession session,
            int combatantId,
            CharacterSheetData sheet,
            CharacterSkill skill,
            int enhancementPm = 0,
            int targetId = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            if (enhancementPm < 0)
            {
                return new T20ActionResult
                {
                    Success = false,
                    RequiredType = skill.Action,
                    Message = "[ERRO] O custo adicional de aprimoramento em PM não pode ser negativo."
                };
            }

            T20PowerTreeDefinition? treeDef = !string.IsNullOrEmpty(skill.TreeKey)
                ? Tormenta20AbilityTreeRules.FindPowerDefinition(skill.TreeKey)
                : null;

            bool isPassive = skill.IsPassive || (treeDef != null && treeDef.IsPassive);
            if (isPassive)
            {
                return new T20ActionResult
                {
                    Success = false,
                    RequiredType = skill.Action,
                    Message = $"[PODER PASSIVO] '{skill.Name}' é uma habilidade passiva da árvore e aplica seus bônus automaticamente."
                };
            }

            if (!string.IsNullOrEmpty(skill.TreeKey) &&
                !session.CanUseAbility(combatantId, skill.TreeKey, out string unmetUsage))
            {
                return new T20ActionResult
                {
                    Success = false,
                    RequiredType = skill.Action,
                    Message = $"[BLOQUEADO PELA ÁRVORE DE PRÉ-REQUISITOS] Não é possível usar '{skill.Name}' agora: {unmetUsage}"
                };
            }

            int maxUsesPerTurn = GetEffectiveMaxUsesPerTurn(skill);
            if (maxUsesPerTurn > 0)
            {
                string usageKey = !string.IsNullOrEmpty(skill.TreeKey) ? skill.TreeKey : skill.Name;
                int usedThisTurn = session.GetActionCountInTurn(combatantId, usageKey);
                if (usedThisTurn >= maxUsesPerTurn)
                {
                    return new T20ActionResult
                    {
                        Success = false,
                        RequiredType = skill.Action,
                        Message = $"[LIMITE DE USOS NO TURNO] '{skill.Name}' já foi utilizada {usedThisTurn}/{maxUsesPerTurn} vez(es) neste turno."
                    };
                }
            }

            int totalPm = skill.PmCost + enhancementPm;
            int pmLimit = GetPmSpendLimit(sheet, session, combatantId);
            int effectiveLimit = Math.Max(pmLimit, skill.PmCost);

            if (totalPm > effectiveLimit)
            {
                bool hasUnlimited = sheet.UnlockedTreeKeys.Contains("magia_ilimitada") ||
                                    session.HasAbility(combatantId, "magia_ilimitada");
                string hint = hasUnlimited
                    ? $" (Nível {sheet.Level} + Magia Ilimitada)"
                    : $" (Nível {sheet.Level}; o poder 'Magia Ilimitada' permite somar o atributo-chave ao limite)";
                return new T20ActionResult
                {
                    Success = false,
                    RequiredType = skill.Action,
                    Message = $"[LIMITE DE PM EXCEDIDO] '{skill.Name}' com +{enhancementPm} PM de aprimoramento custaria {totalPm} PM, " +
                              $"mas o limite de gasto de {sheet.Name} é {effectiveLimit} PM por uso{hint}."
                };
            }

            // Rota nativa ottrpg_ability_enqueue quando é um poder da árvore sem aprimoramento e sem Ação Completa:
            // grava extraHistoryKey no Core C++ para que o cancelamento (Undo) reembolse tanto o nome quanto o TreeKey.
            if (!string.IsNullOrEmpty(skill.TreeKey) &&
                enhancementPm == 0 &&
                skill.Action != T20ActionType.Full &&
                treeDef != null &&
                treeDef.ActionType == skill.Action)
            {
                if (session.HasCondition(combatantId, Tormenta20ConditionRules.UnconsciousKey))
                {
                    return new T20ActionResult
                    {
                        Success = false,
                        RequiredType = skill.Action,
                        Message = $"[BLOQUEADO] Combatente está Inconsciente e não pode executar '{skill.Name}'."
                    };
                }

                if (totalPm > 0)
                {
                    ResourceInfo pmInfo = session.GetResource(combatantId, Tormenta20ActionRules.ManaResourceId);
                    if (pmInfo.Current < totalPm)
                    {
                        return new T20ActionResult
                        {
                            Success = false,
                            RequiredType = skill.Action,
                            Message = $"Pontos de Mana insuficientes ({pmInfo.Current}/{totalPm} PM) para '{skill.Name}'."
                        };
                    }
                }

                if (!session.EnqueueAbility(combatantId, skill.TreeKey, targetId, out int actId, out int consumedCore, out string unmetAbility))
                {
                    return new T20ActionResult
                    {
                        Success = false,
                        RequiredType = skill.Action,
                        Message = $"[BLOQUEADO] Não foi possível realizar '{skill.Name}': {unmetAbility}"
                    };
                }

                if (totalPm > 0)
                {
                    session.SpendResource(combatantId, Tormenta20ActionRules.ManaResourceId, totalPm);
                    sheet.CurrentPm = session.GetResource(combatantId, Tormenta20ActionRules.ManaResourceId).Current;
                }

                if (!string.Equals(skill.Name, treeDef.Name, StringComparison.Ordinal) &&
                    !string.Equals(skill.Name, skill.TreeKey, StringComparison.Ordinal))
                {
                    session.RecordActionInTurn(combatantId, skill.Name);
                }

                T20ActionType consumedType = (T20ActionType)consumedCore;
                bool downgraded = (skill.Action == T20ActionType.Movement && consumedType == T20ActionType.Standard);
                string conversionNote = downgraded
                    ? " (Conversão T20: Ação Padrão foi utilizada como Ação de Movimento!)"
                    : "";

                return new T20ActionResult
                {
                    Success = true,
                    ActionId = actId,
                    RequiredType = skill.Action,
                    ConsumedType = consumedType,
                    UsedStandardAsMovement = downgraded,
                    PmSpent = totalPm,
                    Message = $"'{skill.Name}' ({Tormenta20ActionRules.GetActionTypeName(skill.Action)}) preparada!{conversionNote}"
                };
            }

            string displayActionName = enhancementPm > 0
                ? $"{skill.Name} [Aprimorado +{enhancementPm} PM]"
                : skill.Name;

            var res = Tormenta20ActionRules.EnqueueTurnAction(
                session, combatantId, skill.Action, displayActionName, targetId, totalPm);

            if (res.Success)
            {
                sheet.CurrentPm = session.GetResource(combatantId, Tormenta20ActionRules.ManaResourceId).Current;

                if (displayActionName != skill.Name)
                {
                    session.RecordActionInTurn(combatantId, skill.Name);
                }
                if (!string.IsNullOrEmpty(skill.TreeKey) &&
                    skill.TreeKey != displayActionName &&
                    skill.TreeKey != skill.Name)
                {
                    session.RecordActionInTurn(combatantId, skill.TreeKey);
                }
            }

            return res;
        }
    }
}

