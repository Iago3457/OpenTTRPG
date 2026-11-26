using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    /// <summary>
    /// Representa um dos 6 atributos de um personagem em Tormenta20 (Jogo do Ano).
    /// </summary>
    public sealed class Attribute
    {
        private int _baseValue;

        public AttributeType Type { get; }
        public int Bonus { get; set; }

        public int BaseValue
        {
            get => _baseValue;
            set => _baseValue = value;
        }

        public int TotalValue => _baseValue + Bonus;
        public int Modifier => TotalValue;

        public Attribute(AttributeType type, int baseValue = 0, int bonus = 0)
        {
            Type = type;
            _baseValue = baseValue;
            Bonus = bonus;
        }

        public static int NormalizeT20Value(int value)
        {
            return value >= 10 ? (value - 10) / 2 : value;
        }

        public string GetName() => Type switch
        {
            AttributeType.Strength => "Força (FOR)",
            AttributeType.Dexterity => "Destreza (DES)",
            AttributeType.Constitution => "Constituição (CON)",
            AttributeType.Intelligence => "Inteligência (INT)",
            AttributeType.Wisdom => "Sabedoria (SAB)",
            AttributeType.Charisma => "Carisma (CAR)",
            _ => "Desconhecido"
        };

        public string GetAbbreviation() => Type switch
        {
            AttributeType.Strength => "FOR",
            AttributeType.Dexterity => "DES",
            AttributeType.Constitution => "CON",
            AttributeType.Intelligence => "INT",
            AttributeType.Wisdom => "SAB",
            AttributeType.Charisma => "CAR",
            _ => "???"
        };

        public string FormatModifier() => Modifier >= 0 ? $"+{Modifier}" : Modifier.ToString();
    }

    /// <summary>
    /// Representa um item ou equipamento no inventário de Tormenta20 (sistema de Espaços e Penalidade de Armadura).
    /// </summary>
    public sealed class CharacterItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int Slots { get; set; }
        public string Damage { get; set; }
        public string Critical { get; set; }
        public T20DamageType DamageType { get; set; }
        public bool IsRanged { get; set; }
        public int DefenseBonus { get; set; }
        public int ArmorPenalty { get; set; }
        public int Price { get; set; }
        public bool Equipped { get; set; }
        public int AttackBonus { get; set; }
        public bool IsHeavyArmor { get; set; }

        public CharacterItem(string name, string description, ItemType type, int slots = 1,
                             string damage = "", string critical = "20/x2",
                             int defenseBonus = 0, int armorPenalty = 0, int price = 0, bool equipped = false,
                             T20DamageType damageType = T20DamageType.Slashing, bool isRanged = false,
                             int attackBonus = 0, bool? isHeavyArmor = null)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Type = type;
            Slots = Math.Max(0, slots);
            Damage = damage ?? string.Empty;
            Critical = string.IsNullOrWhiteSpace(critical) ? "20/x2" : critical.Trim();
            DefenseBonus = defenseBonus;
            ArmorPenalty = armorPenalty > 0 ? -armorPenalty : armorPenalty;
            Price = Math.Max(0, price);
            Equipped = equipped;
            DamageType = damageType;
            IsRanged = isRanged;
            AttackBonus = attackBonus;
            IsHeavyArmor = isHeavyArmor ?? (type == ItemType.Armor && (defenseBonus >= 5 || ArmorPenalty <= -4));
        }

        public void ParseCritical(out int threatRange, out int multiplier)
        {
            threatRange = 20;
            multiplier = 2;

            if (string.IsNullOrWhiteSpace(Critical))
            {
                return;
            }

            string clean = Critical.Trim().ToLowerInvariant();
            Match both = Regex.Match(clean, @"(\d+)\s*/\s*x(\d+)");
            if (both.Success)
            {
                threatRange = Math.Clamp(int.Parse(both.Groups[1].Value), 1, 20);
                multiplier = Math.Max(2, int.Parse(both.Groups[2].Value));
                return;
            }

            Match multOnly = Regex.Match(clean, @"x(\d+)");
            if (multOnly.Success)
            {
                multiplier = Math.Max(2, int.Parse(multOnly.Groups[1].Value));
                return;
            }

            Match threatOnly = Regex.Match(clean, @"^(\d+)$");
            if (threatOnly.Success)
            {
                threatRange = Math.Clamp(int.Parse(threatOnly.Groups[1].Value), 1, 20);
            }
        }

        public string GetTypeName() => Type switch
        {
            ItemType.Weapon => "Arma",
            ItemType.Armor => "Armadura",
            ItemType.Shield => "Escudo",
            ItemType.Consumable => "Consumível",
            ItemType.Accessory => "Acessório",
            _ => "Item Geral"
        };

        public string FormatSummary()
        {
            var sb = new StringBuilder();
            sb.Append($"{Name} ({GetTypeName()}) [{Slots} {(Slots == 1 ? "espaço" : "espaços")}]");
            if (!string.IsNullOrEmpty(Damage))
            {
                string atkTag = AttackBonus != 0 ? $"Atk {(AttackBonus >= 0 ? "+" : "")}{AttackBonus} | " : string.Empty;
                sb.Append($" | {atkTag}Dano: {Damage} (Crítico: {Critical})");
            }
            if (DefenseBonus > 0)
            {
                sb.Append($" | Defesa: +{DefenseBonus}");
            }
            if (ArmorPenalty < 0)
            {
                sb.Append($" | Penalidade: {ArmorPenalty}");
            }
            if (Price > 0)
            {
                sb.Append($" | T$ {Price}");
            }
            if (Equipped)
            {
                sb.Append(" [Equipado]");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Aprimoramento de magia ou poder em Tormenta20 (+PM para ampliar efeitos/dados).
    /// </summary>
    public sealed class SpellEnhancement
    {
        public int ExtraPmCost { get; }
        public string Description { get; }
        public string ExtraDice { get; }

        public SpellEnhancement(int extraPmCost, string description, string extraDice = "")
        {
            ExtraPmCost = Math.Max(1, extraPmCost);
            Description = description ?? string.Empty;
            ExtraDice = extraDice ?? string.Empty;
        }
    }

    /// <summary>
    /// Representa uma Habilidade de Classe, Poder Geral ou Magia em Tormenta20 (Jogo do Ano).
    /// </summary>
    public sealed class CharacterSkill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int PmCost { get; set; }
        public T20ActionType Action { get; set; }
        public int Modifier { get; set; }
        public string TreeKey { get; set; }
        public bool IsSpell { get; set; }
        public string BaseFormula { get; set; }
        public int MaxUsesPerTurn { get; set; }
        public bool IsPassive { get; set; }
        public List<SpellEnhancement> Enhancements { get; } = new List<SpellEnhancement>();

        public CharacterSkill(string name, string description, int pmCost, T20ActionType action,
                              int modifier = 0, string treeKey = "", bool isSpell = false, string baseFormula = "",
                              int maxUsesPerTurn = 0, bool isPassive = false)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            PmCost = Math.Max(0, pmCost);
            Action = action;
            Modifier = modifier;
            TreeKey = treeKey ?? string.Empty;
            IsSpell = isSpell;
            BaseFormula = baseFormula ?? string.Empty;
            MaxUsesPerTurn = maxUsesPerTurn;
            IsPassive = isPassive;
        }
    }

    /// <summary>
    /// Ficha completa de Personagem de Tormenta20 (Jogo do Ano) consolidada em C#.
    /// </summary>
    public sealed class CharacterSheetData
    {
        private readonly Dictionary<AttributeType, Attribute> _attributes = new Dictionary<AttributeType, Attribute>();

        public int Id { get; }
        public string Name { get; set; }
        public string CharacterClass { get; set; }
        public string Race { get; set; }
        public string Origin { get; set; }
        public string Deity { get; set; }
        public int Level { get; set; }

        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int TempHp { get; set; }
        public int MaxPm { get; set; }
        public int CurrentPm { get; set; }

        public int DefenseBonusMisc { get; set; }
        public int Tibares { get; set; } = 50;
        public int BonusAbilitySlots { get; set; } = 0;
        public AttributeType SpellKeyAttribute { get; set; } = AttributeType.Intelligence;
        public bool IsBleedingOut { get; set; }

        // Campos dedicados a Fichas de Inimigos / Ameaças (Tormenta20 Jogo do Ano)
        public bool IsEnemy { get; set; }
        public string ChallengeRating { get; set; } = "1";
        public string CreatureType { get; set; } = "Humanoide";
        public string CreatureSize { get; set; } = "Médio";
        public string ThreatRole { get; set; } = "Solo";
        public float SpeedMetersOverride { get; set; }
        public int FixedDefense { get; set; }
        public int DamageReduction { get; set; }
        public int ThreatAttackBonus { get; set; }
        public int? InitiativeOverride { get; set; }
        public int? PerceptionOverride { get; set; }
        public int? FortitudeOverride { get; set; }
        public int? ReflexOverride { get; set; }
        public int? WillOverride { get; set; }
        public string SensesNotes { get; set; } = string.Empty;
        public string TreasureNotes { get; set; } = "Padrão";
        public string ThreatNotes { get; set; } = string.Empty;

        public int LastInitiativeRoll { get; set; }

        public List<CharacterItem> Inventory { get; } = new List<CharacterItem>();
        public List<CharacterSkill> Skills { get; } = new List<CharacterSkill>();
        public List<string> UnlockedTreeKeys { get; } = new List<string>();
        public HashSet<string> TrainedSkills { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SkillMiscBonuses { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int Strength
        {
            get => _attributes[AttributeType.Strength].Modifier;
            set => _attributes[AttributeType.Strength].BaseValue = value;
        }

        public int Dexterity
        {
            get => _attributes[AttributeType.Dexterity].Modifier;
            set => _attributes[AttributeType.Dexterity].BaseValue = value;
        }

        public int Constitution
        {
            get => _attributes[AttributeType.Constitution].Modifier;
            set => _attributes[AttributeType.Constitution].BaseValue = value;
        }

        public int Intelligence
        {
            get => _attributes[AttributeType.Intelligence].Modifier;
            set => _attributes[AttributeType.Intelligence].BaseValue = value;
        }

        public int Wisdom
        {
            get => _attributes[AttributeType.Wisdom].Modifier;
            set => _attributes[AttributeType.Wisdom].BaseValue = value;
        }

        public int Charisma
        {
            get => _attributes[AttributeType.Charisma].Modifier;
            set => _attributes[AttributeType.Charisma].BaseValue = value;
        }

        public CharacterSheetData(int id, string name, string charClass, string race, string origin, string deity,
                                  int level, int maxHp, int maxPm)
        {
            Id = id;
            Name = name ?? "Herói de Arton";
            CharacterClass = charClass ?? "Guerreiro";
            Race = race ?? "Humano";
            Origin = origin ?? "Soldado";
            Deity = deity ?? "Valkaria";
            Level = Math.Max(1, level);
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            TempHp = 0;
            MaxPm = Math.Max(0, maxPm);
            CurrentPm = MaxPm;

            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                _attributes[type] = new Attribute(type, 0);
            }
        }

        public Attribute GetAttribute(AttributeType type) => _attributes[type];

        public void SetAttributeValue(AttributeType type, int value)
        {
            _attributes[type].BaseValue = value;
        }

        public int GetMaxSlots()
        {
            int str = Strength;
            return str >= 0 ? 10 + (str * 2) : 10 + str;
        }

        public int GetCurrentSlots()
        {
            int total = 0;
            foreach (var item in Inventory)
            {
                total += item.Slots;
            }
            return total;
        }

        public bool IsOverburdened() => GetCurrentSlots() > GetMaxSlots();

        public bool IsAboveHardLoadLimit() => GetCurrentSlots() > GetMaxSlots() * 2;

        public bool IsWearingHeavyArmor()
        {
            foreach (var item in Inventory)
            {
                if (item.Equipped && item.Type == ItemType.Armor &&
                    (item.IsHeavyArmor || item.DefenseBonus >= 5 || item.ArmorPenalty <= -4))
                {
                    return true;
                }
            }
            return false;
        }

        public int GetTotalArmorPenalty(EncounterSession? session = null, int combatantId = 0)
        {
            int penalty = 0;
            foreach (var item in Inventory)
            {
                if (item.Equipped && (item.Type == ItemType.Armor || item.Type == ItemType.Shield))
                {
                    penalty += item.ArmorPenalty;
                }
            }

            bool overburdened = IsOverburdened() ||
                (session != null && combatantId > 0 && session.HasCondition(combatantId, Tormenta20ConditionRules.OverburdenedKey));

            if (overburdened)
            {
                penalty -= 5;
            }

            return penalty;
        }

        public int GetDefense(EncounterSession? session = null, int combatantId = 0)
        {
            if (IsEnemy && FixedDefense > 0)
            {
                int condMod = 0;
                if (session != null && combatantId > 0)
                {
                    condMod = Tormenta20ConditionRules.GetDefenseModifier(session, combatantId);
                }
                return FixedDefense + DefenseBonusMisc + condMod;
            }

            int armorBonus = 0;
            bool wearingHeavyArmor = IsWearingHeavyArmor();
            int meleeWeaponCount = 0;
            int totalWeaponCount = 0;
            bool hasShield = false;

            foreach (var item in Inventory)
            {
                if (!item.Equipped) continue;

                if (item.Type == ItemType.Armor || item.Type == ItemType.Shield)
                {
                    armorBonus += item.DefenseBonus;
                    if (item.Type == ItemType.Shield)
                    {
                        hasShield = true;
                    }
                }
                else if (item.Type == ItemType.Weapon)
                {
                    totalWeaponCount++;
                    if (!item.IsRanged) meleeWeaponCount++;
                }
            }

            int powerBonus = 0;
            bool hasOneWeaponStyle = UnlockedTreeKeys.Contains("estilo_uma_arma") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "estilo_uma_arma"));
            if (hasOneWeaponStyle && meleeWeaponCount == 1 && totalWeaponCount == 1 && !hasShield)
            {
                powerBonus += 2;
            }

            bool hasArmored = UnlockedTreeKeys.Contains("encouracado") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "encouracado"));
            if (hasArmored && wearingHeavyArmor)
            {
                powerBonus += 2;
            }

            int conditionMod = 0;
            int dexPenaltyFromConditions = 0;
            if (session != null && combatantId > 0)
            {
                conditionMod = Tormenta20ConditionRules.GetDefenseModifier(session, combatantId);
                dexPenaltyFromConditions = Tormenta20ConditionRules.GetAttributeModifier(session, combatantId, AttributeType.Dexterity);
            }

            int racialDefense = !IsEnemy
                ? Tormenta20CharacterCreationRules.GetRaceTemplateByName(Race).NaturalDefenseBonus
                : 0;
            int dexContribution = wearingHeavyArmor ? 0 : (Dexterity + dexPenaltyFromConditions);

            return 10 + dexContribution + armorBonus + racialDefense + DefenseBonusMisc + powerBonus + conditionMod;
        }

        /// <summary>
        /// Limite de PV negativo para morte em Tormenta20 Jogo do Ano:
        /// -max(10, metade dos PV máximos) para heróis; 0 PV para ameaças/inimigos.
        /// </summary>
        public int GetDeathThreshold() => IsEnemy ? 0 : -Math.Max(10, MaxHp / 2);

        /// <summary>
        /// Cria uma cópia independente da ficha (útil para instanciar múltiplos inimigos no combate).
        /// </summary>
        public CharacterSheetData CloneForCombat(int runtimeId, string? customName = null)
        {
            var copy = new CharacterSheetData(
                runtimeId,
                string.IsNullOrWhiteSpace(customName) ? Name : customName,
                CharacterClass,
                Race,
                Origin,
                Deity,
                Level,
                MaxHp,
                MaxPm)
            {
                CurrentHp = MaxHp,
                TempHp = TempHp,
                CurrentPm = MaxPm,
                DefenseBonusMisc = DefenseBonusMisc,
                Tibares = Tibares,
                BonusAbilitySlots = BonusAbilitySlots,
                SpellKeyAttribute = SpellKeyAttribute,
                IsBleedingOut = false,
                IsEnemy = IsEnemy,
                ChallengeRating = ChallengeRating,
                CreatureType = CreatureType,
                CreatureSize = CreatureSize,
                ThreatRole = ThreatRole,
                SpeedMetersOverride = SpeedMetersOverride,
                FixedDefense = FixedDefense,
                DamageReduction = DamageReduction,
                ThreatAttackBonus = ThreatAttackBonus,
                InitiativeOverride = InitiativeOverride,
                PerceptionOverride = PerceptionOverride,
                FortitudeOverride = FortitudeOverride,
                ReflexOverride = ReflexOverride,
                WillOverride = WillOverride,
                SensesNotes = SensesNotes,
                TreasureNotes = TreasureNotes,
                ThreatNotes = ThreatNotes
            };

            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                copy.SetAttributeValue(type, GetAttribute(type).BaseValue);
            }
            foreach (var item in Inventory)
            {
                copy.Inventory.Add(new CharacterItem(
                    item.Name, item.Description, item.Type, item.Slots,
                    item.Damage, item.Critical, item.DefenseBonus,
                    item.ArmorPenalty, item.Price, item.Equipped,
                    item.DamageType, item.IsRanged, item.AttackBonus, item.IsHeavyArmor));
            }
            foreach (var sk in Skills)
            {
                var skCopy = new CharacterSkill(
                    sk.Name, sk.Description, sk.PmCost, sk.Action,
                    sk.Modifier, sk.TreeKey, sk.IsSpell, sk.BaseFormula,
                    sk.MaxUsesPerTurn, sk.IsPassive);
                foreach (var enh in sk.Enhancements)
                {
                    skCopy.Enhancements.Add(new SpellEnhancement(enh.ExtraPmCost, enh.Description, enh.ExtraDice));
                }
                copy.Skills.Add(skCopy);
            }
            copy.UnlockedTreeKeys.AddRange(UnlockedTreeKeys);
            foreach (string tr in TrainedSkills)
            {
                copy.TrainedSkills.Add(tr);
            }
            foreach (var kv in SkillMiscBonuses)
            {
                copy.SkillMiscBonuses[kv.Key] = kv.Value;
            }
            return copy;
        }

        /// <summary>
        /// Retorna o estado vital atual do personagem segundo as regras de Ferimentos e Morte de Tormenta20.
        /// </summary>
        public T20HealthState GetHealthState(EncounterSession? session = null, int combatantId = 0)
        {
            int deathThreshold = GetDeathThreshold();
            if (CurrentHp <= deathThreshold)
            {
                return T20HealthState.Dead;
            }

            if (CurrentHp <= 0)
            {
                bool bleedingInSession = session != null && combatantId > 0 &&
                                         session.HasCondition(combatantId, Tormenta20ConditionRules.BleedingKey);
                if (IsBleedingOut || bleedingInSession)
                {
                    return T20HealthState.Dying;
                }
                return T20HealthState.UnconsciousStable;
            }

            if (CurrentHp <= MaxHp / 2)
            {
                return T20HealthState.Bloodied;
            }

            return T20HealthState.Healthy;
        }

        public string GetHealthStateLabel(EncounterSession? session = null, int combatantId = 0)
        {
            return GetHealthState(session, combatantId) switch
            {
                T20HealthState.Dead => IsEnemy ? "DERROTADO (0 PV)" : $"MORTO (Limite {GetDeathThreshold()} PV atingido)",
                T20HealthState.Dying => $"MORRENDO (Inconsciente e Sangrando | Morte em {GetDeathThreshold()} PV)",
                T20HealthState.UnconsciousStable => $"INCONSCIENTE (Estabilizado | Morte em {GetDeathThreshold()} PV)",
                T20HealthState.Bloodied => "MACHUCADO",
                _ => "ATIVO"
            };
        }

        public void RecalculateMaxResources(bool refill = false)
        {
            Tormenta20CharacterCreationRules.RecalculateSheetResources(this, refill);
        }

        public bool SpendPm(int amount)
        {
            if (amount < 0 || amount > CurrentPm) return false;
            CurrentPm -= amount;
            return true;
        }

        public void RecoverPm(int amount)
        {
            if (amount <= 0) return;
            CurrentPm = Math.Min(MaxPm, CurrentPm + amount);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            if (TempHp > 0)
            {
                if (damage <= TempHp)
                {
                    TempHp -= damage;
                    return;
                }
                damage -= TempHp;
                TempHp = 0;
            }

            int deathThreshold = GetDeathThreshold();
            CurrentHp = Math.Max(deathThreshold, CurrentHp - damage);
            if (CurrentHp <= deathThreshold)
            {
                IsBleedingOut = false;
            }
            else if (CurrentHp <= 0)
            {
                IsBleedingOut = true;
            }
        }

        public void HealHp(int amount)
        {
            if (amount <= 0) return;
            if (CurrentHp <= GetDeathThreshold()) return; // Morto não recupera PV com cura comum (use Revive)
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            IsBleedingOut = false;
        }

        public void Revive(int restoredHp = 1)
        {
            CurrentHp = Math.Clamp(restoredHp, 1, MaxHp);
            IsBleedingOut = false;
        }

        public string FormatSummary(EncounterSession? session = null, int combatantId = 0)
        {
            string overburdenTag = IsOverburdened() ? " [SOBRECARREGADO!]" : "";
            int pmLimit = Tormenta20MagicRules.GetPmSpendLimit(this, session, combatantId);
            string treeBudget = Tormenta20AbilityTreeRules.FormatAbilitySlotBudget(this);
            return $"  {Name} ({Race} {CharacterClass} Nv.{Level} | Origem: {Origin} | Devoto: {Deity} | T$ {Tibares})\n" +
                   $"  Estado: {GetHealthStateLabel(session, combatantId)} | PV: {CurrentHp}/{MaxHp} (Morte: {GetDeathThreshold()}) | PM: {CurrentPm}/{MaxPm} (Limite Gasto: {pmLimit} PM) | Defesa: {GetDefense(session, combatantId)}\n" +
                   $"  Árvore de Poderes: {treeBudget}\n" +
                   $"  Carga: {GetCurrentSlots()}/{GetMaxSlots()} espaços{overburdenTag} | Penalidade Armadura: {GetTotalArmorPenalty(session, combatantId)}\n" +
                   $"  FOR: {FormatSigned(Strength)} | DES: {FormatSigned(Dexterity)} | CON: {FormatSigned(Constitution)} | " +
                   $"INT: {FormatSigned(Intelligence)} | SAB: {FormatSigned(Wisdom)} | CAR: {FormatSigned(Charisma)}";
        }

        private static string FormatSigned(int val) => val >= 0 ? $"+{val}" : val.ToString();
    }
}

