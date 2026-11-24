using System;
using System.Text;
using System.Text.RegularExpressions;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20AttackResolutionResult
    {
        public bool ActionSucceeded { get; set; }
        public bool Hit { get; set; }
        public bool IsCritical { get; set; }
        public bool IsNatural20 { get; set; }
        public bool IsNatural1 { get; set; }
        public int NaturalAttackRoll { get; set; }
        public int AttackModifier { get; set; }
        public int AttackTotal { get; set; }
        public int TargetDefense { get; set; }
        public int ThreatRange { get; set; }
        public int CriticalMultiplier { get; set; }
        public string DamageExpression { get; set; } = string.Empty;
        public int RawDamageRolled { get; set; }
        public DamageResult CoreDamageResult { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Automação completa do fluxo de Ataque vs. Defesa, Margem de Ameaça, Multiplicação de Dados Base no Crítico
    /// e envio ao Pipeline de Dano do Core C++ (RD, vulnerabilidades, resistências e imunidades).
    /// </summary>
    public static class Tormenta20CombatRules
    {
        /// <summary>
        /// Constrói a expressão de dano de uma arma aplicando a regra canônica de Crítico de Tormenta20:
        /// No acerto crítico, multiplica-se APENAS os dados base da arma (ex.: "1d8" com x2 vira "2d8"),
        /// sem multiplicar bônus numéricos fixos (Força, Ataque Poderoso, etc.).
        /// </summary>
        public static string BuildWeaponDamageExpression(string baseDamageDice, bool isCritical, int critMultiplier, int flatBonus)
        {
            string clean = string.IsNullOrWhiteSpace(baseDamageDice) ? "1d4" : baseDamageDice.Trim();
            Match m = Regex.Match(clean, @"^(\d+)d(\d+)(.*)$", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                string sign = flatBonus >= 0 ? $"+{flatBonus}" : flatBonus.ToString();
                return flatBonus == 0 ? clean : $"{clean}{sign}";
            }

            int diceCount = int.Parse(m.Groups[1].Value);
            int diceSides = int.Parse(m.Groups[2].Value);
            string tail = m.Groups[3].Value.Trim();

            int extraInlineMod = 0;
            if (!string.IsNullOrEmpty(tail))
            {
                Match inlineMod = Regex.Match(tail, @"^([+-]\s*\d+)$");
                if (inlineMod.Success)
                {
                    extraInlineMod = int.Parse(inlineMod.Groups[1].Value.Replace(" ", ""));
                    tail = string.Empty;
                }
            }

            int effectiveDiceCount = isCritical ? diceCount * Math.Max(2, critMultiplier) : diceCount;
            int totalFlat = flatBonus + extraInlineMod;

            var sb = new StringBuilder();
            sb.Append($"{effectiveDiceCount}d{diceSides}{tail}");
            if (totalFlat > 0)
            {
                sb.Append($"+{totalFlat}");
            }
            else if (totalFlat < 0)
            {
                sb.Append(totalFlat);
            }
            return sb.ToString();
        }

        public static T20AttackResolutionResult ResolveWeaponAttack(
            EncounterSession session,
            CharacterSheetData attacker, int attackerId,
            CharacterSheetData defender, int defenderId,
            CharacterItem? weapon = null,
            int extraAttackBonus = 0,
            int extraDamageBonus = 0,
            bool useAtaquePoderoso = false,
            bool consumeStandardAction = true)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));

            if (attacker.CurrentHp <= attacker.GetDeathThreshold() ||
                session.HasCondition(attackerId, Tormenta20ConditionRules.UnconsciousKey))
            {
                return new T20AttackResolutionResult
                {
                    ActionSucceeded = false,
                    Message = $"[BLOQUEADO] {attacker.Name} está inconsciente/incapacitado e não pode atacar."
                };
            }

            if (defender.CurrentHp <= defender.GetDeathThreshold())
            {
                return new T20AttackResolutionResult
                {
                    ActionSucceeded = false,
                    Message = $"[BLOQUEADO] {defender.Name} já está fora de combate/morto."
                };
            }

            if (weapon == null)
            {
                foreach (var item in attacker.Inventory)
                {
                    if (item.Equipped && item.Type == ItemType.Weapon)
                    {
                        weapon = item;
                        break;
                    }
                }
                weapon ??= new CharacterItem("Ataque Desarmado", "Golpe desarmado", ItemType.Weapon, 0, "1d4", "20/x2", 0, 0, 0, true, T20DamageType.Bludgeoning, false);
            }

            TacticalAttackContext tacticalCtx = Tormenta20TacticalRules.EvaluateAttackContext(
                session, attackerId, defenderId, weapon);
            if (tacticalCtx.MapActive && (!tacticalCtx.HasLineOfSight || !tacticalCtx.InRange))
            {
                return new T20AttackResolutionResult
                {
                    ActionSucceeded = false,
                    Message = tacticalCtx.BlockingReason
                };
            }

            if (consumeStandardAction)
            {
                T20ActionResult actRes = Tormenta20ActionRules.EnqueueTurnAction(
                    session, attackerId, T20ActionType.Standard, "Agredir (Ataque Padrão)", defenderId, 0);
                if (!actRes.Success)
                {
                    return new T20AttackResolutionResult
                    {
                        ActionSucceeded = false,
                        Message = actRes.Message
                    };
                }
            }
            else
            {
                session.RecordActionInTurn(attackerId, "Agredir (Ataque Padrão)");
            }

            string attackSkill = weapon.IsRanged ? "Pontaria" : "Luta";
            int skillMod;
            if (attacker.IsEnemy && (weapon.AttackBonus != 0 || attacker.ThreatAttackBonus != 0))
            {
                int baseAtk = weapon.AttackBonus != 0 ? weapon.AttackBonus : attacker.ThreatAttackBonus;
                int condAtkMod = 0;
                if (Tormenta20SkillRules.TryGetSkillDefinition(attackSkill, out var atkDef))
                {
                    condAtkMod = Tormenta20ConditionRules.GetSkillModifier(session, attackerId, atkDef);
                }
                skillMod = baseAtk + extraAttackBonus + condAtkMod;
            }
            else
            {
                Tormenta20SkillRules.TryCalculateSkillModifier(
                    attacker, attackSkill, out skillMod, out _, session, attackerId, extraAttackBonus);
                if (weapon.AttackBonus != 0)
                {
                    skillMod += weapon.AttackBonus;
                }
            }

            int meleeWeaponCount = 0;
            int totalWeaponCount = 0;
            bool hasShield = false;
            foreach (var item in attacker.Inventory)
            {
                if (!item.Equipped) continue;
                if (item.Type == ItemType.Weapon)
                {
                    totalWeaponCount++;
                    if (!item.IsRanged) meleeWeaponCount++;
                }
                if (item.Type == ItemType.Shield) hasShield = true;
            }

            bool hasOneWeaponStyle = attacker.UnlockedTreeKeys.Contains("estilo_uma_arma") ||
                                     session.HasAbility(attackerId, "estilo_uma_arma");
            if (hasOneWeaponStyle && meleeWeaponCount == 1 && totalWeaponCount == 1 && !hasShield && !weapon.IsRanged)
            {
                skillMod += 2;
            }

            int flatDamageBonus = extraDamageBonus;
            if (!weapon.IsRanged && !attacker.IsEnemy)
            {
                int strMod = attacker.Strength + Tormenta20ConditionRules.GetAttributeModifier(session, attackerId, AttributeType.Strength);
                flatDamageBonus += strMod;
            }
            else if (weapon.IsRanged && !attacker.IsEnemy)
            {
                bool hasEstiloDisparo = attacker.UnlockedTreeKeys.Contains("estilo_disparo") ||
                                        session.HasAbility(attackerId, "estilo_disparo");
                if (hasEstiloDisparo)
                {
                    int dexMod = attacker.Dexterity + Tormenta20ConditionRules.GetAttributeModifier(session, attackerId, AttributeType.Dexterity);
                    flatDamageBonus += dexMod;
                }
            }

            bool hasPowerAttack = attacker.UnlockedTreeKeys.Contains("ataque_poderoso") ||
                                  session.HasAbility(attackerId, "ataque_poderoso");
            if (useAtaquePoderoso && hasPowerAttack && !weapon.IsRanged)
            {
                skillMod -= 2;
                flatDamageBonus += 5;
            }

            weapon.ParseCritical(out int threatRange, out int critMultiplier);
            bool hasPreciseAttack = attacker.UnlockedTreeKeys.Contains("ataque_preciso") ||
                                    session.HasAbility(attackerId, "ataque_preciso");
            if (hasPreciseAttack && meleeWeaponCount == 1 && totalWeaponCount == 1 && !hasShield && !weapon.IsRanged)
            {
                threatRange = Math.Max(1, threatRange - 2);
                critMultiplier += 1;
            }

            int baseDef = defender.GetDefense(session, defenderId) + tacticalCtx.DefenseCoverBonus;
            if (weapon.IsRanged && session.HasCondition(defenderId, Tormenta20ConditionRules.ProneKey))
            {
                // Tormenta20ConditionRules.GetDefenseModifier já incluiu -5 de Caído (para corpo a corpo),
                // então +10 transforma o modificador líquido contra ataque à distância em +5 na Defesa.
                baseDef += 10;
            }
            skillMod += tacticalCtx.FlankingAttackBonus;

            bool hasDisparoPreciso = attacker.UnlockedTreeKeys.Contains("disparo_preciso") ||
                                     session.HasAbility(attackerId, "disparo_preciso");
            bool engagedInMeleePenalty = false;
            if (weapon.IsRanged && tacticalCtx.MapActive &&
                Tormenta20TacticalRules.IsTargetEngagedInMelee(session, attackerId, defenderId) &&
                !hasDisparoPreciso)
            {
                skillMod -= 5;
                engagedInMeleePenalty = true;
            }

            string tacticalNotes = string.Empty;
            if (tacticalCtx.MapActive)
            {
                if (tacticalCtx.IsFlanking) tacticalNotes += " [Flanqueando +2 Atk]";
                if (tacticalCtx.Cover == T20CoverLevel.Light) tacticalNotes += " [Cobertura Leve +5 Def]";
                if (engagedInMeleePenalty) tacticalNotes += " [Alvo em Corpo a Corpo -5 Atk]";
            }

            RollResult d20 = session.Roll("1d20");
            int natural = d20.DiceTotal;
            int attackTotal = natural + skillMod;
            bool isNat20 = d20.NaturalMaxCount > 0;
            bool isNat1 = d20.NaturalMinCount > 0;

            bool hit = isNat20 || (!isNat1 && attackTotal >= baseDef);
            bool isCrit = hit && (isNat20 || natural >= threatRange);

            if (!hit)
            {
                return new T20AttackResolutionResult
                {
                    ActionSucceeded = true,
                    Hit = false,
                    IsCritical = false,
                    IsNatural20 = isNat20,
                    IsNatural1 = isNat1,
                    NaturalAttackRoll = natural,
                    AttackModifier = skillMod,
                    AttackTotal = attackTotal,
                    TargetDefense = baseDef,
                    ThreatRange = threatRange,
                    CriticalMultiplier = critMultiplier,
                    Message = $"[ATAQUE ERROU] {attacker.Name} atacou {defender.Name} com {weapon.Name}: rolou {attackTotal} (1d20={natural}{FormatSigned(skillMod)}) vs Defesa {baseDef}{tacticalNotes}."
                };
            }

            string dmgExpr = BuildWeaponDamageExpression(weapon.Damage, isCrit, critMultiplier, flatDamageBonus);
            RollResult dmgRoll = session.Roll(dmgExpr);
            int rawDamage = Math.Max(1, dmgRoll.Total);

            DamageResult dmgRes = session.DealDamage(defenderId, rawDamage, (int)weapon.DamageType);
            ResourceInfo defHp = session.GetResource(defenderId, Tormenta20ActionRules.HealthResourceId);
            defender.TempHp = defHp.Temp;
            if (defHp.Minimum == 0 && defender.CurrentHp <= 0)
            {
                defender.CurrentHp = Math.Max(defender.GetDeathThreshold(), defender.CurrentHp - dmgRes.Overflow);
            }
            else if (defHp.Minimum == 0 && dmgRes.Overflow > 0)
            {
                defender.CurrentHp = Math.Max(defender.GetDeathThreshold(), defHp.Current - dmgRes.Overflow);
            }
            else
            {
                defender.CurrentHp = defHp.Current;
            }

            if (defender.CurrentHp <= defender.GetDeathThreshold())
            {
                defender.IsBleedingOut = false;
            }
            else if (defender.CurrentHp <= 0)
            {
                defender.IsBleedingOut = true;
            }

            Tormenta20ConditionRules.SyncDyingAndUnconsciousState(session, defenderId, defender);

            string critLabel = isCrit ? $" [ACERTO CRÍTICO! (Ameaça {threatRange}/x{critMultiplier} -> Dados multiplicados: {dmgExpr})]" : $" ({dmgExpr})";
            string rdLabel = dmgRes.ReducedBy > 0 ? $" [RD absorveu {dmgRes.ReducedBy}]" : "";
            string respLabel = dmgRes.Response switch
            {
                DamageResponse.Vulnerable => " [VULNERÁVEL x2!]",
                DamageResponse.Resistant => " [RESISTENTE x0.5!]",
                DamageResponse.Immune => " [IMUNE x0!]",
                _ => ""
            };
            string downLabel = defender.CurrentHp <= defender.GetDeathThreshold()
                ? (defender.IsEnemy
                    ? $" >>> [DERROTADO] {defender.Name} foi derrotado ({defender.CurrentHp} PV)!"
                    : $" >>> [MORTE] {defender.Name} MORREU ({defender.CurrentHp} PV)!")
                : (defender.CurrentHp <= 0 ? $" >>> [MORRENDO] {defender.Name} CAIU MORRENDO ({defender.CurrentHp} PV - Inconsciente e Sangrando)!" : "");

            return new T20AttackResolutionResult
            {
                ActionSucceeded = true,
                Hit = true,
                IsCritical = isCrit,
                IsNatural20 = isNat20,
                IsNatural1 = isNat1,
                NaturalAttackRoll = natural,
                AttackModifier = skillMod,
                AttackTotal = attackTotal,
                TargetDefense = baseDef,
                ThreatRange = threatRange,
                CriticalMultiplier = critMultiplier,
                DamageExpression = dmgExpr,
                RawDamageRolled = rawDamage,
                CoreDamageResult = dmgRes,
                Message = $"[ATAQUE ACERTOU!] {attacker.Name} atingiu {defender.Name} com {weapon.Name} (Ataque {attackTotal} vs Defesa {baseDef}{tacticalNotes}){critLabel}: " +
                          $"Dano Bruto = {rawDamage}{rdLabel}{respLabel} -> Dano Final Aplicado = {dmgRes.FinalAmount} PV!{downLabel}"
            };
        }

        private static string FormatSigned(int val) => val >= 0 ? $"+{val}" : val.ToString();
    }
}

