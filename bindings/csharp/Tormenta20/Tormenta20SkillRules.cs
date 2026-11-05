using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20SkillDefinition
    {
        public string Name { get; }
        public AttributeType KeyAttribute { get; }
        public bool TrainedOnly { get; }
        public bool AppliesArmorPenalty { get; }

        public T20SkillDefinition(string name, AttributeType keyAttribute, bool trainedOnly, bool appliesArmorPenalty)
        {
            Name = name;
            KeyAttribute = keyAttribute;
            TrainedOnly = trainedOnly;
            AppliesArmorPenalty = appliesArmorPenalty;
        }
    }

    public sealed class T20SkillCheckResult
    {
        public bool Allowed { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int NaturalRoll { get; set; }
        public int HalfLevel { get; set; }
        public int AttributeModifier { get; set; }
        public int TrainingBonus { get; set; }
        public int ArmorPenalty { get; set; }
        public int ConditionModifier { get; set; }
        public int OtherModifier { get; set; }
        public int TotalModifier { get; set; }
        public int Total { get; set; }
        public int DifficultyClass { get; set; }
        public bool Success { get; set; }
        public bool IsNatural20 { get; set; }
        public bool IsNatural1 { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tabela Oficial de Perícias e Regras de Teste de Tormenta20 (Edição Jogo do Ano).
    /// Fórmula: Metade do Nível + Atributo + Bônus de Treinamento (+2/+4/+6) + Outros - Penalidade de Armadura.
    /// </summary>
    public static class Tormenta20SkillRules
    {
        public static readonly IReadOnlyList<T20SkillDefinition> CanonicalSkills = new List<T20SkillDefinition>
        {
            new T20SkillDefinition("Acrobacia",     AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: true),
            new T20SkillDefinition("Adestramento",  AttributeType.Charisma,     trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Atletismo",     AttributeType.Strength,     trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Atuação",       AttributeType.Charisma,     trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Cavalgar",      AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Conhecimento",  AttributeType.Intelligence, trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Cura",          AttributeType.Wisdom,       trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Diplomacia",    AttributeType.Charisma,     trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Enganação",     AttributeType.Charisma,     trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Fortitude",     AttributeType.Constitution, trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Furtividade",   AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: true),
            new T20SkillDefinition("Guerra",        AttributeType.Intelligence, trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Iniciativa",    AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Intimidação",   AttributeType.Charisma,     trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Intuição",      AttributeType.Wisdom,       trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Investigação",  AttributeType.Intelligence, trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Jogatina",      AttributeType.Charisma,     trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Ladinagem",     AttributeType.Dexterity,    trainedOnly: true,  appliesArmorPenalty: true),
            new T20SkillDefinition("Luta",          AttributeType.Strength,     trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Misticismo",    AttributeType.Intelligence, trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Nobreza",       AttributeType.Intelligence, trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Ofício",        AttributeType.Intelligence, trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Percepção",     AttributeType.Wisdom,       trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Pilotagem",     AttributeType.Dexterity,    trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Pontaria",      AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Reflexos",      AttributeType.Dexterity,    trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Religião",      AttributeType.Wisdom,       trainedOnly: true,  appliesArmorPenalty: false),
            new T20SkillDefinition("Sobrevivência", AttributeType.Wisdom,       trainedOnly: false, appliesArmorPenalty: false),
            new T20SkillDefinition("Vontade",       AttributeType.Wisdom,       trainedOnly: false, appliesArmorPenalty: false)
        };

        private static readonly Dictionary<string, T20SkillDefinition> _byName = BuildSkillDictionary();

        private static Dictionary<string, T20SkillDefinition> BuildSkillDictionary()
        {
            var dict = new Dictionary<string, T20SkillDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var sk in CanonicalSkills)
            {
                dict[sk.Name] = sk;
            }
            dict["Atuacao"] = dict["Atuação"];
            dict["Enganacao"] = dict["Enganação"];
            dict["Intimidacao"] = dict["Intimidação"];
            dict["Intuicao"] = dict["Intuição"];
            dict["Investigacao"] = dict["Investigação"];
            dict["Oficio"] = dict["Ofício"];
            dict["Percepcao"] = dict["Percepção"];
            dict["Religiao"] = dict["Religião"];
            dict["Sobrevivencia"] = dict["Sobrevivência"];
            return dict;
        }

        public static bool TryGetSkillDefinition(string skillName, out T20SkillDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                definition = null!;
                return false;
            }
            return _byName.TryGetValue(skillName.Trim(), out definition!);
        }

        public static int GetTrainingBonus(int level, bool isTrained)
        {
            if (!isTrained) return 0;
            if (level >= 15) return 6;
            if (level >= 7) return 4;
            return 2;
        }

        public static bool TryCalculateSkillModifier(CharacterSheetData sheet, string skillName,
                                                     out int totalModifier, out string breakdownOrError,
                                                     EncounterSession? session = null, int combatantId = 0,
                                                     int extraBonus = 0)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            if (!TryGetSkillDefinition(skillName, out T20SkillDefinition def))
            {
                totalModifier = 0;
                breakdownOrError = $"Perícia '{skillName}' não encontrada na tabela de Tormenta20.";
                return false;
            }

            bool isTrained = sheet.TrainedSkills.Contains(def.Name);
            if (def.TrainedOnly && !isTrained)
            {
                totalModifier = 0;
                breakdownOrError = $"[BLOQUEADO] A perícia '{def.Name}' é Somente Treinada e {sheet.Name} não possui treinamento nela.";
                return false;
            }

            int halfLevel = sheet.Level / 2;
            int attrMod = sheet.GetAttribute(def.KeyAttribute).Modifier;
            int trainBonus = GetTrainingBonus(sheet.Level, isTrained);
            int armorPenalty = def.AppliesArmorPenalty ? sheet.GetTotalArmorPenalty(session, combatantId) : 0;

            int miscBonus = extraBonus;
            if (sheet.SkillMiscBonuses.TryGetValue(def.Name, out int savedMisc))
            {
                miscBonus += savedMisc;
            }

            bool isSavingThrow = def.Name.Equals("Fortitude", StringComparison.OrdinalIgnoreCase) ||
                                 def.Name.Equals("Reflexos", StringComparison.OrdinalIgnoreCase) ||
                                 def.Name.Equals("Vontade", StringComparison.OrdinalIgnoreCase);
            bool hasInexpugnable = sheet.UnlockedTreeKeys.Contains("inexpugnavel") ||
                (session != null && combatantId > 0 && session.HasAbility(combatantId, "inexpugnavel"));
            if (isSavingThrow && hasInexpugnable)
            {
                miscBonus += 2;
            }

            int condPenalty = 0;
            if (session != null && combatantId > 0)
            {
                condPenalty = Tormenta20ConditionRules.GetSkillModifier(session, combatantId, def);
            }

            totalModifier = halfLevel + attrMod + trainBonus + armorPenalty + miscBonus + condPenalty;
            breakdownOrError =
                $"Metade Nv (+{halfLevel}) + {sheet.GetAttribute(def.KeyAttribute).GetAbbreviation()} ({FormatSigned(attrMod)})" +
                (trainBonus > 0 ? $" + Treino (+{trainBonus})" : "") +
                (armorPenalty < 0 ? $" + Pen.Armadura ({armorPenalty})" : "") +
                (condPenalty != 0 ? $" + Condições ({FormatSigned(condPenalty)})" : "") +
                (miscBonus != 0 ? $" + Outros ({FormatSigned(miscBonus)})" : "");
            return true;
        }

        public static T20SkillCheckResult RollSkillCheck(EncounterSession session, CharacterSheetData sheet,
                                                         string skillName, int dc = 0, int extraBonus = 0, int combatantId = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            if (!TryGetSkillDefinition(skillName, out T20SkillDefinition def))
            {
                return new T20SkillCheckResult
                {
                    Allowed = false,
                    SkillName = skillName,
                    Message = $"[ERRO] Perícia '{skillName}' desconhecida."
                };
            }

            bool isTrained = sheet.TrainedSkills.Contains(def.Name);
            if (def.TrainedOnly && !isTrained)
            {
                return new T20SkillCheckResult
                {
                    Allowed = false,
                    SkillName = def.Name,
                    DifficultyClass = dc,
                    Success = false,
                    Message = $"[BLOQUEADO] '{def.Name}' exige treinamento (Somente Treinada) e {sheet.Name} não é treinado."
                };
            }

            int halfLevel = sheet.Level / 2;
            int attrMod = sheet.GetAttribute(def.KeyAttribute).Modifier;
            int trainBonus = GetTrainingBonus(sheet.Level, isTrained);
            int armorPenalty = def.AppliesArmorPenalty ? sheet.GetTotalArmorPenalty(session, combatantId) : 0;
            int condMod = combatantId > 0 ? Tormenta20ConditionRules.GetSkillModifier(session, combatantId, def) : 0;

            int otherMod = extraBonus;
            if (sheet.SkillMiscBonuses.TryGetValue(def.Name, out int savedMisc))
            {
                otherMod += savedMisc;
            }

            bool isSavingThrow = def.Name.Equals("Fortitude", StringComparison.OrdinalIgnoreCase) ||
                                 def.Name.Equals("Reflexos", StringComparison.OrdinalIgnoreCase) ||
                                 def.Name.Equals("Vontade", StringComparison.OrdinalIgnoreCase);
            bool hasInexpugnable = sheet.UnlockedTreeKeys.Contains("inexpugnavel") ||
                (combatantId > 0 && session.HasAbility(combatantId, "inexpugnavel"));
            if (isSavingThrow && hasInexpugnable)
            {
                otherMod += 2;
            }

            int totalMod = halfLevel + attrMod + trainBonus + armorPenalty + condMod + otherMod;
            RollResult d20 = session.Roll("1d20");
            int natural = d20.DiceTotal;
            int total = natural + totalMod;

            bool isNat20 = d20.NaturalMaxCount > 0;
            bool isNat1 = d20.NaturalMinCount > 0;
            bool success = dc <= 0 ? !isNat1 : (isNat20 || (!isNat1 && total >= dc));

            string dcText = dc > 0 ? $" vs CD {dc} -> {(success ? "SUCESSO!" : "FALHA!")}" : "";
            string critText = isNat20 ? " [20 NATURAL!]" : (isNat1 ? " [1 NATURAL - FALHA CRÍTICA!]" : "");

            return new T20SkillCheckResult
            {
                Allowed = true,
                SkillName = def.Name,
                NaturalRoll = natural,
                HalfLevel = halfLevel,
                AttributeModifier = attrMod,
                TrainingBonus = trainBonus,
                ArmorPenalty = armorPenalty,
                ConditionModifier = condMod,
                OtherModifier = otherMod,
                TotalModifier = totalMod,
                Total = total,
                DifficultyClass = dc,
                Success = success,
                IsNatural20 = isNat20,
                IsNatural1 = isNat1,
                Message = $"[TESTE DE {def.Name.ToUpperInvariant()}] {sheet.Name} rolou {total} (1d20={natural}{FormatSigned(totalMod)}){critText}{dcText}"
            };
        }

        private static string FormatSigned(int val) => val >= 0 ? $"+{val}" : val.ToString();
    }
}

