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

            string displayActionName = enhancementPm > 0
                ? $"{skill.Name} [Aprimorado +{enhancementPm} PM]"
                : skill.Name;

            var res = Tormenta20ActionRules.EnqueueTurnAction(
                session, combatantId, skill.Action, displayActionName, targetId, totalPm);

            if (res.Success)
            {
                sheet.CurrentPm = session.GetResource(combatantId, Tormenta20ActionRules.ManaResourceId).Current;
                if (enhancementPm > 0)
                {
                    session.RecordActionInTurn(combatantId, skill.Name);
                }
            }

            return res;
        }
    }
}

