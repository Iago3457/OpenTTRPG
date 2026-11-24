using System;
using System.Collections.Generic;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20ClassTemplate
    {
        public T20Class ClassType { get; }
        public string DisplayName { get; }
        public int BaseHpLevel1 { get; }
        public int HpPerLevel { get; }
        public int PmPerLevel { get; }
        public AttributeType SpellKeyAttribute { get; }
        public bool AddsKeyAttrToPm { get; }
        public string[] DefaultTrainedSkills { get; }
        public string IconicSkillName { get; }
        public string IconicSkillDesc { get; }
        public int IconicSkillPm { get; }
        public T20ActionType IconicSkillAction { get; }
        public bool IconicSkillIsSpell { get; }
        public string IconicSkillFormula { get; }

        public T20ClassTemplate(
            T20Class classType,
            string displayName,
            int baseHpLevel1,
            int hpPerLevel,
            int pmPerLevel,
            AttributeType spellKeyAttribute,
            bool addsKeyAttrToPm,
            string[] defaultTrainedSkills,
            string iconicSkillName,
            string iconicSkillDesc,
            int iconicSkillPm,
            T20ActionType iconicSkillAction,
            bool iconicSkillIsSpell = false,
            string iconicSkillFormula = "")
        {
            ClassType = classType;
            DisplayName = displayName;
            BaseHpLevel1 = baseHpLevel1;
            HpPerLevel = hpPerLevel;
            PmPerLevel = pmPerLevel;
            SpellKeyAttribute = spellKeyAttribute;
            AddsKeyAttrToPm = addsKeyAttrToPm;
            DefaultTrainedSkills = defaultTrainedSkills;
            IconicSkillName = iconicSkillName;
            IconicSkillDesc = iconicSkillDesc;
            IconicSkillPm = iconicSkillPm;
            IconicSkillAction = iconicSkillAction;
            IconicSkillIsSpell = iconicSkillIsSpell;
            IconicSkillFormula = iconicSkillFormula;
        }
    }

    public sealed class T20RaceTemplate
    {
        public T20Race RaceType { get; }
        public string DisplayName { get; }
        public int ModFor { get; }
        public int ModDes { get; }
        public int ModCon { get; }
        public int ModInt { get; }
        public int ModSab { get; }
        public int ModCar { get; }
        public int ExtraHpLevel1 { get; }
        public int ExtraHpPerLevel { get; }
        public int ExtraPmPerLevel { get; }
        public int ExtraPmFlat { get; }
        public int NaturalDefenseBonus { get; }
        public string TraitSummary { get; }

        public T20RaceTemplate(
            T20Race raceType,
            string displayName,
            int modFor, int modDes, int modCon, int modInt, int modSab, int modCar,
            int extraHpLevel1, int extraHpPerLevel, int extraPmPerLevel, int extraPmFlat,
            int naturalDefenseBonus,
            string traitSummary)
        {
            RaceType = raceType;
            DisplayName = displayName;
            ModFor = modFor;
            ModDes = modDes;
            ModCon = modCon;
            ModInt = modInt;
            ModSab = modSab;
            ModCar = modCar;
            ExtraHpLevel1 = extraHpLevel1;
            ExtraHpPerLevel = extraHpPerLevel;
            ExtraPmPerLevel = extraPmPerLevel;
            ExtraPmFlat = extraPmFlat;
            NaturalDefenseBonus = naturalDefenseBonus;
            TraitSummary = traitSummary;
        }
    }

    /// <summary>
    /// Regras Canônicas de Criação e Progressão de Personagens de Tormenta20 (Edição Jogo do Ano):
    /// Cálculo automático de PV Máximo, PM Máximo, Defesa Racial, Atributos Raciais e Habilidades Iniciais.
    /// </summary>
    public static class Tormenta20CharacterCreationRules
    {
        public static readonly IReadOnlyList<T20ClassTemplate> CanonicalClasses = new List<T20ClassTemplate>
        {
            new(T20Class.Guerreiro, "Guerreiro", baseHpLevel1: 20, hpPerLevel: 5, pmPerLevel: 3,
                AttributeType.Intelligence, addsKeyAttrToPm: false,
                new[] { "Luta", "Fortitude", "Iniciativa" },
                "Ataque Especial", "Gasta 1 PM para +4 no teste de ataque ou na rolagem de dano", 1, T20ActionType.Free),

            new(T20Class.Arcanista, "Arcanista", baseHpLevel1: 8, hpPerLevel: 2, pmPerLevel: 6,
                AttributeType.Intelligence, addsKeyAttrToPm: true,
                new[] { "Misticismo", "Vontade", "Iniciativa" },
                "Mísseis Mágicos", "Dispara 3 setas de essência arcana que causam 2d4+2 de dano", 1, T20ActionType.Standard, true, "2d4+2"),

            new(T20Class.Barbaro, "Bárbaro", baseHpLevel1: 24, hpPerLevel: 6, pmPerLevel: 3,
                AttributeType.Wisdom, addsKeyAttrToPm: false,
                new[] { "Luta", "Fortitude", "Atletismo" },
                "Fúria", "Gasta 2 PM para receber +2 em testes de ataque e rolagens de dano corpo a corpo", 2, T20ActionType.Free),

            new(T20Class.Bardo, "Bardo", baseHpLevel1: 12, hpPerLevel: 3, pmPerLevel: 4,
                AttributeType.Charisma, addsKeyAttrToPm: true,
                new[] { "Atuação", "Reflexos", "Diplomacia" },
                "Inspiração", "Gasta 2 PM para conceder +1 em testes de perícia para você e aliados", 2, T20ActionType.Standard),

            new(T20Class.Bucaneiro, "Bucaneiro", baseHpLevel1: 16, hpPerLevel: 4, pmPerLevel: 3,
                AttributeType.Charisma, addsKeyAttrToPm: false,
                new[] { "Luta", "Reflexos", "Acrobacia" },
                "Audácia", "Gasta 2 PM para somar seu Carisma em um teste de perícia", 2, T20ActionType.Free),

            new(T20Class.Cacador, "Caçador", baseHpLevel1: 16, hpPerLevel: 4, pmPerLevel: 4,
                AttributeType.Wisdom, addsKeyAttrToPm: false,
                new[] { "Pontaria", "Sobrevivência", "Percepção" },
                "Marca da Presa", "Gasta 1 PM para analisar o alvo e causar +1d4 de dano contra ele", 1, T20ActionType.Movement, false, "1d4"),

            new(T20Class.Cavaleiro, "Cavaleiro", baseHpLevel1: 20, hpPerLevel: 5, pmPerLevel: 3,
                AttributeType.Charisma, addsKeyAttrToPm: false,
                new[] { "Luta", "Fortitude", "Diplomacia" },
                "Baluarte", "Gasta 1 PM para receber +2 na Defesa e nos testes de resistência até o próximo turno", 1, T20ActionType.Free),

            new(T20Class.Clerigo, "Clérigo", baseHpLevel1: 16, hpPerLevel: 4, pmPerLevel: 5,
                AttributeType.Wisdom, addsKeyAttrToPm: true,
                new[] { "Religião", "Vontade", "Cura" },
                "Curar Ferimentos", "Canaliza luz divina para restaurar 2d8+2 PV ou ferir mortos-vivos", 1, T20ActionType.Standard, true, "2d8+2"),

            new(T20Class.Druida, "Druida", baseHpLevel1: 16, hpPerLevel: 4, pmPerLevel: 4,
                AttributeType.Wisdom, addsKeyAttrToPm: true,
                new[] { "Sobrevivência", "Vontade", "Adestramento" },
                "Forma Selvagem", "Assume a forma de um animal feroz (+3 FOR, +2 Defesa, garras 1d8)", 3, T20ActionType.Movement, false, "1d8"),

            new(T20Class.Inventor, "Inventor", baseHpLevel1: 12, hpPerLevel: 3, pmPerLevel: 4,
                AttributeType.Intelligence, addsKeyAttrToPm: false,
                new[] { "Ofício", "Vontade", "Investigação" },
                "Engenhosidade", "Gasta 2 PM para somar sua Inteligência em um teste de perícia", 2, T20ActionType.Free),

            new(T20Class.Ladino, "Ladino", baseHpLevel1: 12, hpPerLevel: 3, pmPerLevel: 4,
                AttributeType.Intelligence, addsKeyAttrToPm: false,
                new[] { "Ladinagem", "Reflexos", "Furtividade" },
                "Ataque Furtivo", "Causa +1d6 de dano extra contra alvos desprevenidos ou flanqueados", 0, T20ActionType.Free, false, "1d6"),

            new(T20Class.Lutador, "Lutador", baseHpLevel1: 20, hpPerLevel: 5, pmPerLevel: 3,
                AttributeType.Strength, addsKeyAttrToPm: false,
                new[] { "Luta", "Fortitude", "Acrobacia" },
                "Golpe Relâmpago", "Gasta 1 PM para desferir um golpe desarmado extra (1d6+FOR)", 1, T20ActionType.Free, false, "1d6"),

            new(T20Class.Nobre, "Nobre", baseHpLevel1: 16, hpPerLevel: 4, pmPerLevel: 4,
                AttributeType.Charisma, addsKeyAttrToPm: false,
                new[] { "Diplomacia", "Vontade", "Intimidação" },
                "Orgulho", "Gasta 1 PM para receber +2 em um teste de perícia", 1, T20ActionType.Free),

            new(T20Class.Paladino, "Paladino", baseHpLevel1: 20, hpPerLevel: 5, pmPerLevel: 3,
                AttributeType.Charisma, addsKeyAttrToPm: true,
                new[] { "Luta", "Vontade", "Religião" },
                "Golpe Divino", "Gasta 2 PM para somar Carisma no ataque e +1d8 de dano sagrado", 2, T20ActionType.Free, false, "1d8")
        };

        public static readonly IReadOnlyList<T20RaceTemplate> CanonicalRaces = new List<T20RaceTemplate>
        {
            new(T20Race.Humano,       "Humano",        +1, +1, +1,  0,  0,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Versátil (+1 em 3 atributos e +1 perícia)"),
            new(T20Race.Anao,         "Anão",           0, -1, +2,  0, +1,  0, extraHpLevel1: 3, extraHpPerLevel: 1, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Duro como Pedra (+3 PV no 1º nível e +1 PV/nível)"),
            new(T20Race.Dahllan,      "Dahllan",        0, +1,  0, -1, +2,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 1, "Armadura de Allihanna (+1 na Defesa)"),
            new(T20Race.Elfo,         "Elfo",           0, +1, -1, +2,  0,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 1, extraPmFlat: 0, naturalDefenseBonus: 0, "Sangue Mágico (+1 PM por nível)"),
            new(T20Race.Goblin,       "Goblin",         0, +2,  0, +1,  0, -1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Engenhoso e Esguio (DES +2, INT +1, CAR -1)"),
            new(T20Race.Lefou,        "Lefou",         +1, +1, +1,  0,  0, -1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Cria da Tormenta (+1 FOR/DES/CON, CAR -1)"),
            new(T20Race.Minotauro,    "Minotauro",     +2,  0, +1,  0, -1,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 1, "Couro Rígido (+1 na Defesa, FOR +2, CON +1)"),
            new(T20Race.Qareen,       "Qareen",         0,  0,  0, +1, -1, +2, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 1, naturalDefenseBonus: 0, "Tatuagem Mística (+1 PM extra, CAR +2, INT +1)"),
            new(T20Race.Golem,        "Golem",         +2,  0, +1,  0,  0, -1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 2, "Chassi de Construto (+2 na Defesa, FOR +2, CON +1)"),
            new(T20Race.Hynne,        "Hynne",         -1, +2,  0,  0,  0, +1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Sorte Salvadora (DES +2, CAR +1, FOR -1)"),
            new(T20Race.Kliren,       "Kliren",        -1,  0,  0, +2,  0, +1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Vanguardista (INT +2, CAR +1, FOR -1)"),
            new(T20Race.Medusa,       "Medusa",         0, +2,  0,  0,  0, +1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Cria de Megalokk (DES +2, CAR +1)"),
            new(T20Race.Osteon,       "Osteon",         0, +1, -1, +1, +1,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Memória Póstuma (RD 5 Corte/Frio/Perfuração, CON -1)"),
            new(T20Race.SereiaTritao, "Sereia/Tritão", +1, +1,  0,  0,  0, +1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Mestre do Tridente e Canção dos Mares"),
            new(T20Race.Silfide,      "Sílfide",       -2, +1,  0,  0,  0, +2, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 1, naturalDefenseBonus: 0, "Espírito da Natureza (+1 PM, CAR +2, DES +1, FOR -2)"),
            new(T20Race.Suraggel,     "Suraggel",       0,  0,  0,  0, +2, +1, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 0, "Herança Planar (Aggelus: SAB +2, CAR +1)"),
            new(T20Race.Trog,         "Trog",          +1,  0, +2, -1,  0,  0, extraHpLevel1: 0, extraHpPerLevel: 0, extraPmPerLevel: 0, extraPmFlat: 0, naturalDefenseBonus: 1, "Couro Escamoso (+1 na Defesa, CON +2, FOR +1)")
        };

        public static T20ClassTemplate GetClassTemplate(T20Class classType)
        {
            foreach (var c in CanonicalClasses)
            {
                if (c.ClassType == classType) return c;
            }
            return CanonicalClasses[0];
        }

        public static T20ClassTemplate GetClassTemplateByName(string? className)
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                string clean = className.Trim();
                foreach (var c in CanonicalClasses)
                {
                    if (c.DisplayName.Equals(clean, StringComparison.OrdinalIgnoreCase) ||
                        c.ClassType.ToString().Equals(clean, StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
                if (clean.StartsWith("Ladina", StringComparison.OrdinalIgnoreCase))
                    return GetClassTemplate(T20Class.Ladino);
                if (clean.StartsWith("Guerreira", StringComparison.OrdinalIgnoreCase))
                    return GetClassTemplate(T20Class.Guerreiro);
                if (clean.StartsWith("Caçadora", StringComparison.OrdinalIgnoreCase))
                    return GetClassTemplate(T20Class.Cacador);
            }
            return CanonicalClasses[0];
        }

        public static T20RaceTemplate GetRaceTemplate(T20Race raceType)
        {
            foreach (var r in CanonicalRaces)
            {
                if (r.RaceType == raceType) return r;
            }
            return CanonicalRaces[0];
        }

        public static T20RaceTemplate GetRaceTemplateByName(string? raceName)
        {
            if (!string.IsNullOrWhiteSpace(raceName))
            {
                string clean = raceName.Trim();
                foreach (var r in CanonicalRaces)
                {
                    if (r.DisplayName.Equals(clean, StringComparison.OrdinalIgnoreCase) ||
                        r.RaceType.ToString().Equals(clean, StringComparison.OrdinalIgnoreCase))
                    {
                        return r;
                    }
                }
                if (clean.Equals("Elfa", StringComparison.OrdinalIgnoreCase))
                    return GetRaceTemplate(T20Race.Elfo);
                if (clean.Equals("Humana", StringComparison.OrdinalIgnoreCase))
                    return GetRaceTemplate(T20Race.Humano);
                if (clean.Equals("Anã", StringComparison.OrdinalIgnoreCase))
                    return GetRaceTemplate(T20Race.Anao);
            }
            return CanonicalRaces[0];
        }

        public static string GetOriginDisplayName(T20Origin origin) => origin switch
        {
            T20Origin.Acolito => "Acólito",
            T20Origin.AmigoDosAnimais => "Amigo dos Animais",
            T20Origin.Aristocrata => "Aristocrata",
            T20Origin.Artesao => "Artesão",
            T20Origin.Artista => "Artista",
            T20Origin.AssistenteDeLaboratorio => "Assistente de Laboratório",
            T20Origin.Batedor => "Batedor",
            T20Origin.Capanga => "Capanga",
            T20Origin.Charlatao => "Charlatão",
            T20Origin.Circense => "Circense",
            T20Origin.Criminoso => "Criminoso",
            T20Origin.Curandeiro => "Curandeiro",
            T20Origin.Eremita => "Eremita",
            T20Origin.Escravo => "Escravo",
            T20Origin.Estudioso => "Estudioso",
            T20Origin.Fazendeiro => "Fazendeiro",
            T20Origin.Forasteiro => "Forasteiro",
            T20Origin.Gladiador => "Gladiador",
            T20Origin.Guarda => "Guarda",
            T20Origin.Herdeiro => "Herdeiro",
            T20Origin.HeroiCampones => "Herói Camponês",
            T20Origin.Marujo => "Marujo",
            T20Origin.Mateiro => "Mateiro",
            T20Origin.MembroDeGuilda => "Membro de Guilda",
            T20Origin.Mercador => "Mercador",
            T20Origin.Minerador => "Minerador",
            T20Origin.Nomade => "Nômade",
            T20Origin.Pivete => "Pivete",
            T20Origin.Refugiado => "Refugiado",
            T20Origin.Seguidor => "Seguidor",
            T20Origin.Selvagem => "Selvagem",
            T20Origin.Soldado => "Soldado",
            T20Origin.Taverneiro => "Taverneiro",
            T20Origin.Trabalhador => "Trabalhador",
            _ => origin.ToString()
        };

        public static string GetDeityDisplayName(T20Deity deity) => deity switch
        {
            T20Deity.NaoDevoto => "Não Devoto (Panteão)",
            T20Deity.LinWu => "Lin-Wu",
            T20Deity.TannaToh => "Tanna-Toh",
            _ => deity.ToString()
        };

        /// <summary>
        /// Calcula o PV Máximo automaticamente segundo as regras de Tormenta20 Jogo do Ano:
        /// Nível 1: max(1, BaseClasse + CON + BônusRaçaNv1)
        /// Cada Nível adicional: max(1, PorNívelClasse + CON + BônusRaçaPorNível)
        /// </summary>
        public static int CalculateMaxHp(T20ClassTemplate classTpl, T20RaceTemplate raceTpl, int level, int conMod)
        {
            int safeLevel = Math.Clamp(level, 1, 20);
            int hpLevel1 = Math.Max(1, classTpl.BaseHpLevel1 + conMod + raceTpl.ExtraHpLevel1);
            int hpPerExtraLevel = Math.Max(1, classTpl.HpPerLevel + conMod + raceTpl.ExtraHpPerLevel);
            return hpLevel1 + (safeLevel - 1) * hpPerExtraLevel;
        }

        /// <summary>
        /// Calcula o PM Máximo automaticamente segundo as regras de Tormenta20 Jogo do Ano:
        /// Total = Nível * (PM_Classe + PM_Raça_Por_Nível) + PM_Raça_Fixo + (AtributoChave se classe conjuradora).
        /// </summary>
        public static int CalculateMaxPm(T20ClassTemplate classTpl, T20RaceTemplate raceTpl, int level, int keyAttrMod = 0)
        {
            int safeLevel = Math.Clamp(level, 1, 20);
            int basePm = safeLevel * (classTpl.PmPerLevel + raceTpl.ExtraPmPerLevel) + raceTpl.ExtraPmFlat;
            if (classTpl.AddsKeyAttrToPm && keyAttrMod > 0)
            {
                basePm += keyAttrMod;
            }
            return Math.Max(1, basePm);
        }

        /// <summary>
        /// Recalcula e atualiza automaticamente o PV Máximo, PM Máximo e Bônus de Defesa Racial de uma ficha
        /// com base na Classe, Raça, Nível e Atributos atuais.
        /// </summary>
        public static void RecalculateSheetResources(CharacterSheetData sheet, bool refillCurrent = false)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (sheet.IsEnemy) return;

            var classTpl = GetClassTemplateByName(sheet.CharacterClass);
            var raceTpl = GetRaceTemplateByName(sheet.Race);

            if (classTpl.ClassType != T20Class.Arcanista ||
                (sheet.SpellKeyAttribute != AttributeType.Intelligence &&
                 sheet.SpellKeyAttribute != AttributeType.Wisdom &&
                 sheet.SpellKeyAttribute != AttributeType.Charisma))
            {
                sheet.SpellKeyAttribute = classTpl.SpellKeyAttribute;
            }
            int keyAttrMod = sheet.GetAttribute(sheet.SpellKeyAttribute).Modifier;

            int oldMaxHp = sheet.MaxHp;
            int oldMaxPm = sheet.MaxPm;

            int newMaxHp = CalculateMaxHp(classTpl, raceTpl, sheet.Level, sheet.Constitution);
            int newMaxPm = CalculateMaxPm(classTpl, raceTpl, sheet.Level, keyAttrMod);

            sheet.MaxHp = newMaxHp;
            sheet.MaxPm = newMaxPm;

            if (refillCurrent)
            {
                sheet.CurrentHp = newMaxHp;
                sheet.CurrentPm = newMaxPm;
                sheet.IsBleedingOut = false;
            }
            else
            {
                int hpDelta = newMaxHp - oldMaxHp;
                int pmDelta = newMaxPm - oldMaxPm;
                if (hpDelta > 0 && sheet.CurrentHp > 0)
                    sheet.CurrentHp = Math.Min(newMaxHp, sheet.CurrentHp + hpDelta);
                else
                    sheet.CurrentHp = Math.Clamp(sheet.CurrentHp, sheet.GetDeathThreshold(), newMaxHp);

                if (pmDelta > 0)
                    sheet.CurrentPm = Math.Min(newMaxPm, sheet.CurrentPm + pmDelta);
                else
                    sheet.CurrentPm = Math.Clamp(sheet.CurrentPm, 0, newMaxPm);
            }
        }
    }
}
