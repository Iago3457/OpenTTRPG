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
            set => _baseValue = NormalizeT20Value(value);
        }

        public int TotalValue => _baseValue + Bonus;
        public int Modifier => TotalValue;

        public Attribute(AttributeType type, int baseValue = 0, int bonus = 0)
        {
            Type = type;
            _baseValue = NormalizeT20Value(baseValue);
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

        public CharacterItem(string name, string description, ItemType type, int slots = 1,
                             string damage = "", string critical = "20/x2",
                             int defenseBonus = 0, int armorPenalty = 0, int price = 0, bool equipped = false,
                             T20DamageType damageType = T20DamageType.Slashing, bool isRanged = false)
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
                sb.Append($" | Dano: {Damage} (Crítico: {Critical})");
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
        public List<SpellEnhancement> Enhancements { get; } = new List<SpellEnhancement>();

        public CharacterSkill(string name, string description, int pmCost, T20ActionType action,
                              int modifier = 0, string treeKey = "", bool isSpell = false, string baseFormula = "")
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            PmCost = Math.Max(0, pmCost);
            Action = action;
            Modifier = modifier;
            TreeKey = treeKey ?? string.Empty;
            IsSpell = isSpell;
            BaseFormula = baseFormula ?? string.Empty;
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
        public AttributeType SpellKeyAttribute { get; set; } = AttributeType.Intelligence;

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
            int armorBonus = 0;
            bool wearingHeavyArmor = false;
            int weaponCount = 0;
            bool hasShield = false;

            foreach (var item in Inventory)
            {
                if (!item.Equipped) continue;

                if (item.Type == ItemType.Armor || item.Type == ItemType.Shield)
                {
                    armorBonus += item.DefenseBonus;
                    if (item.Type == ItemType.Armor && item.DefenseBonus >= 5)
                    {
                        wearingHeavyArmor = true;
                    }
                    if (item.Type == ItemType.Shield)
                    {
                        hasShield = true;
                    }
                }
                else if (item.Type == ItemType.Weapon)
                {
                    weaponCount++;
                }
            }

            int powerBonus = 0;
            bool hasOneWeaponStyle = UnlockedTreeKeys.Contains("estilo_uma_arma") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "estilo_uma_arma"));
            if (hasOneWeaponStyle && weaponCount == 1 && !hasShield)
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

            return 10 + (Dexterity + dexPenaltyFromConditions) + armorBonus + DefenseBonusMisc + powerBonus + conditionMod;
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
            CurrentHp = Math.Max(0, CurrentHp - damage);
        }

        public void HealHp(int amount)
        {
            if (amount <= 0) return;
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public string FormatSummary(EncounterSession? session = null, int combatantId = 0)
        {
            string overburdenTag = IsOverburdened() ? " [SOBRECARREGADO!]" : "";
            int pmLimit = Tormenta20MagicRules.GetPmSpendLimit(this, session, combatantId);
            return $"  {Name} ({Race} {CharacterClass} Nv.{Level} | Origem: {Origin} | Devoto: {Deity} | T$ {Tibares})\n" +
                   $"  PV: {CurrentHp}/{MaxHp} | PM: {CurrentPm}/{MaxPm} (Limite Gasto: {pmLimit} PM) | Defesa: {GetDefense(session, combatantId)}\n" +
                   $"  Carga: {GetCurrentSlots()}/{GetMaxSlots()} espaços{overburdenTag} | Penalidade Armadura: {GetTotalArmorPenalty(session, combatantId)}\n" +
                   $"  FOR: {FormatSigned(Strength)} | DES: {FormatSigned(Dexterity)} | CON: {FormatSigned(Constitution)} | " +
                   $"INT: {FormatSigned(Intelligence)} | SAB: {FormatSigned(Wisdom)} | CAR: {FormatSigned(Charisma)}";
        }

        private static string FormatSigned(int val) => val >= 0 ? $"+{val}" : val.ToString();
    }
}

