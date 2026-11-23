using System;
using System.Collections.Generic;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class BestiaryPresetInfo
    {
        public string Key { get; }
        public string DisplayName { get; }
        public string ChallengeRating { get; }
        public string Role { get; }

        public BestiaryPresetInfo(string key, string displayName, string challengeRating, string role)
        {
            Key = key;
            DisplayName = displayName;
            ChallengeRating = challengeRating;
            Role = role;
        }
    }

    /// <summary>
    /// Catálogo e fábrica de Fichas de Inimigos / Ameaças canônicas de Tormenta20 (Edição Jogo do Ano).
    /// </summary>
    public static class Tormenta20Bestiary
    {
        public static readonly IReadOnlyList<BestiaryPresetInfo> Presets = new List<BestiaryPresetInfo>
        {
            new BestiaryPresetInfo("goblin_salteador", "Goblin Salteador", "1/4", "Lacaio"),
            new BestiaryPresetInfo("bandido_estradas", "Bandido de Estradas", "1/2", "Lacaio"),
            new BestiaryPresetInfo("esqueleto_guerreiro", "Esqueleto Guerreiro", "1/2", "Lacaio"),
            new BestiaryPresetInfo("lobo_atroz", "Lobo Atroz", "1", "Solo"),
            new BestiaryPresetInfo("orc_berserker", "Orc Berserker", "2", "Solo"),
            new BestiaryPresetInfo("cultista_aharadak", "Cultista da Tormenta", "3", "Especial"),
            new BestiaryPresetInfo("golem_ferro", "Golem de Ferro", "5", "Solo"),
            new BestiaryPresetInfo("dragao_jovem", "Dragão Jovem", "7", "Solo")
        };

        public static CharacterSheetData CreateBlankEnemy(int id, string name = "Nova Ameaça")
        {
            var sheet = new CharacterSheetData(id, name, "Ameaça", "Humanoide", "Bestiário", "Nenhum", 1, 15, 0)
            {
                IsEnemy = true,
                ChallengeRating = "1/2",
                CreatureType = "Humanoide",
                CreatureSize = "Médio",
                ThreatRole = "Solo",
                SpeedMetersOverride = 9f,
                FixedDefense = 14,
                DamageReduction = 0,
                ThreatAttackBonus = 5,
                InitiativeOverride = 2,
                PerceptionOverride = 2,
                FortitudeOverride = 3,
                ReflexOverride = 3,
                WillOverride = 1,
                SensesNotes = "Visão padrão",
                TreasureNotes = "Padrão",
                ThreatNotes = "Descreva táticas e comportamentos desta ameaça."
            };
            sheet.Inventory.Add(new CharacterItem(
                "Ataque Básico", "Ataque corpo a corpo da ameaça", ItemType.Weapon,
                1, "1d6+2", "20/x2", 0, 0, 0, true, T20DamageType.Slashing, false, attackBonus: 5));
            return sheet;
        }

        public static CharacterSheetData CreateFromPreset(int id, string presetKey, string? customName = null)
        {
            string key = (presetKey ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "goblin_salteador" => BuildGoblinSalteador(id, customName),
                "bandido_estradas" => BuildBandidoEstradas(id, customName),
                "esqueleto_guerreiro" => BuildEsqueletoGuerreiro(id, customName),
                "lobo_atroz" => BuildLoboAtroz(id, customName),
                "orc_berserker" => BuildOrcBerserker(id, customName),
                "cultista_aharadak" => BuildCultistaTormenta(id, customName),
                "golem_ferro" => BuildGolemFerro(id, customName),
                "dragao_jovem" => BuildDragaoJovem(id, customName),
                _ => BuildGoblinSalteador(id, customName)
            };
        }

        private static CharacterSheetData BuildGoblinSalteador(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Goblin Salteador", "Lacaio", "Goblin", "Bestiário", "Hyninn", 1, 6, 0)
            {
                IsEnemy = true,
                ChallengeRating = "1/4",
                CreatureType = "Humanoide",
                CreatureSize = "Pequeno",
                ThreatRole = "Lacaio",
                SpeedMetersOverride = 9f,
                FixedDefense = 13,
                DamageReduction = 0,
                ThreatAttackBonus = 6,
                InitiativeOverride = 4,
                PerceptionOverride = 2,
                FortitudeOverride = 1,
                ReflexOverride = 4,
                WillOverride = -1,
                SensesNotes = "Visão no escuro",
                TreasureNotes = "Metade",
                ThreatNotes = "Prefere emboscadas e ataques à distância com arco curto.",
                Strength = 0,
                Dexterity = 3,
                Constitution = 1,
                Intelligence = 0,
                Wisdom = -1,
                Charisma = -1
            };
            s.Inventory.Add(new CharacterItem("Adaga Enferrujada", "Arma leve", ItemType.Weapon, 1, "1d4+2", "19/x2", 0, 0, 2, true, T20DamageType.Piercing, false, attackBonus: 6));
            s.Inventory.Add(new CharacterItem("Arco Curto", "Ataque à distância (alcance médio)", ItemType.Weapon, 1, "1d6", "20/x3", 0, 0, 30, true, T20DamageType.Piercing, true, attackBonus: 6));
            s.Skills.Add(new CharacterSkill("Emboscada Traiçoeira", "Causa +1d6 de dano se o alvo estiver desprevenido ou flanqueado.", 0, T20ActionType.Free, 0, "", false, "1d6"));
            return s;
        }

        private static CharacterSheetData BuildBandidoEstradas(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Bandido de Estradas", "Lacaio", "Humano", "Bestiário", "Nenhum", 1, 14, 2)
            {
                IsEnemy = true,
                ChallengeRating = "1/2",
                CreatureType = "Humanoide",
                CreatureSize = "Médio",
                ThreatRole = "Lacaio",
                SpeedMetersOverride = 9f,
                FixedDefense = 14,
                DamageReduction = 0,
                ThreatAttackBonus = 7,
                InitiativeOverride = 3,
                PerceptionOverride = 3,
                FortitudeOverride = 2,
                ReflexOverride = 4,
                WillOverride = 1,
                SensesNotes = "Visão padrão",
                TreasureNotes = "Padrão",
                ThreatNotes = "Flanqueia alvos isolados ou dispara com besta de cobertura.",
                Strength = 1,
                Dexterity = 2,
                Constitution = 1,
                Intelligence = 0,
                Wisdom = 0,
                Charisma = 0
            };
            s.Inventory.Add(new CharacterItem("Espada Curta", "Lâmina ágil", ItemType.Weapon, 1, "1d6+2", "19/x2", 0, 0, 10, true, T20DamageType.Piercing, false, attackBonus: 7));
            s.Inventory.Add(new CharacterItem("Besta Leve", "Arma de disparo", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 35, true, T20DamageType.Piercing, true, attackBonus: 6));
            s.Skills.Add(new CharacterSkill("Ataque Furtivo", "Ao flanquear, causa +1d6 de dano adicional.", 0, T20ActionType.Free, 0, "", false, "1d6"));
            return s;
        }

        private static CharacterSheetData BuildEsqueletoGuerreiro(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Esqueleto Guerreiro", "Lacaio", "Morto-Vivo", "Bestiário", "Tenebra", 1, 15, 0)
            {
                IsEnemy = true,
                ChallengeRating = "1/2",
                CreatureType = "Morto-Vivo",
                CreatureSize = "Médio",
                ThreatRole = "Lacaio",
                SpeedMetersOverride = 9f,
                FixedDefense = 15,
                DamageReduction = 5,
                ThreatAttackBonus = 7,
                InitiativeOverride = 2,
                PerceptionOverride = 0,
                FortitudeOverride = 1,
                ReflexOverride = 3,
                WillOverride = 2,
                SensesNotes = "Visão no escuro; RD 5 corte/perfuração",
                TreasureNotes = "Nenhum",
                ThreatNotes = "Avança sem medo em linha reta até destruir o alvo mais próximo.",
                Strength = 2,
                Dexterity = 2,
                Constitution = 0,
                Intelligence = -3,
                Wisdom = 0,
                Charisma = -2
            };
            s.Inventory.Add(new CharacterItem("Cimitarra Antiga", "Lâmina curva enferrujada", ItemType.Weapon, 1, "1d6+3", "18/x2", 0, 0, 15, true, T20DamageType.Slashing, false, attackBonus: 7));
            s.Skills.Add(new CharacterSkill("Ossos Rígidos", "Redução de dano natural (RD 5 já aplicada).", 0, T20ActionType.Free));
            return s;
        }

        private static CharacterSheetData BuildLoboAtroz(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Lobo Atroz", "Solo", "Animal", "Bestiário", "Allihanna", 2, 32, 0)
            {
                IsEnemy = true,
                ChallengeRating = "1",
                CreatureType = "Animal",
                CreatureSize = "Grande",
                ThreatRole = "Solo",
                SpeedMetersOverride = 12f,
                FixedDefense = 15,
                DamageReduction = 0,
                ThreatAttackBonus = 10,
                InitiativeOverride = 5,
                PerceptionOverride = 6,
                FortitudeOverride = 6,
                ReflexOverride = 5,
                WillOverride = 2,
                SensesNotes = "Faro apurado, Visão na penumbra",
                TreasureNotes = "Nenhum (Pele: T$ 40)",
                ThreatNotes = "Busca derrubar presas e trabalhar em matilha para flanquear.",
                Strength = 4,
                Dexterity = 3,
                Constitution = 3,
                Intelligence = -4,
                Wisdom = 1,
                Charisma = -1
            };
            s.Inventory.Add(new CharacterItem("Mordida", "Mandíbula poderosa", ItemType.Weapon, 0, "1d8+5", "20/x2", 0, 0, 0, true, T20DamageType.Piercing, false, attackBonus: 10));
            s.Skills.Add(new CharacterSkill("Derrubar", "Ao acertar a mordida, pode tentar derrubar o alvo como ação livre.", 0, T20ActionType.Free));
            return s;
        }

        private static CharacterSheetData BuildOrcBerserker(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Orc Berserker", "Solo", "Humanoide", "Bestiário", "Arsenal", 3, 52, 6)
            {
                IsEnemy = true,
                ChallengeRating = "2",
                CreatureType = "Humanoide",
                CreatureSize = "Médio",
                ThreatRole = "Solo",
                SpeedMetersOverride = 9f,
                FixedDefense = 16,
                DamageReduction = 2,
                ThreatAttackBonus = 11,
                InitiativeOverride = 3,
                PerceptionOverride = 3,
                FortitudeOverride = 8,
                ReflexOverride = 4,
                WillOverride = 2,
                SensesNotes = "Sensibilidade à luz, Visão no escuro",
                TreasureNotes = "Padrão",
                ThreatNotes = "Ativa Fúria Selvagem no 1º turno e parte para combate corpo a corpo.",
                Strength = 4,
                Dexterity = 1,
                Constitution = 3,
                Intelligence = -1,
                Wisdom = 0,
                Charisma = -1
            };
            s.Inventory.Add(new CharacterItem("Machado de Guerra", "Arma pesada brutal", ItemType.Weapon, 2, "1d12+6", "20/x3", 0, 0, 20, true, T20DamageType.Slashing, false, attackBonus: 11));
            s.Skills.Add(new CharacterSkill("Fúria Selvagem", "Gasta 2 PM para receber +2 em ataque e +1d6 no dano até o fim da cena.", 2, T20ActionType.Free, 2, "", false, "1d6"));
            return s;
        }

        private static CharacterSheetData BuildCultistaTormenta(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Cultista da Tormenta", "Especial", "Humanoide", "Bestiário", "Aharadak", 4, 60, 18)
            {
                IsEnemy = true,
                ChallengeRating = "3",
                CreatureType = "Humanoide",
                CreatureSize = "Médio",
                ThreatRole = "Especial",
                SpeedMetersOverride = 9f,
                FixedDefense = 17,
                DamageReduction = 5,
                ThreatAttackBonus = 9,
                InitiativeOverride = 5,
                PerceptionOverride = 6,
                FortitudeOverride = 6,
                ReflexOverride = 5,
                WillOverride = 9,
                SpellKeyAttribute = AttributeType.Charisma,
                SensesNotes = "Visão no escuro",
                TreasureNotes = "Padrão + Símbolo de Aharadak",
                ThreatNotes = "Mantém distância com cobertura leve enquanto conjura magias rubras.",
                Strength = 0,
                Dexterity = 2,
                Constitution = 2,
                Intelligence = 2,
                Wisdom = 2,
                Charisma = 4
            };
            s.Inventory.Add(new CharacterItem("Adaga Ritualística", "Lâmina de matéria vermelha", ItemType.Weapon, 1, "1d4+3", "19/x2", 0, 0, 25, true, T20DamageType.Piercing, false, attackBonus: 9));
            s.Skills.Add(new CharacterSkill("Seta Infalível de Talude", "Gasta 1 PM para disparar setas de essência sem erro.", 1, T20ActionType.Standard, 0, "", true, "2d4+2"));
            s.Skills.Add(new CharacterSkill("Névoa Rubra", "Gasta 2 PM para causar dano ácido/essência em área.", 2, T20ActionType.Standard, 0, "", true, "3d6"));
            return s;
        }

        private static CharacterSheetData BuildGolemFerro(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Golem de Ferro", "Solo", "Construto", "Bestiário", "Nenhum", 6, 140, 10)
            {
                IsEnemy = true,
                ChallengeRating = "5",
                CreatureType = "Construto",
                CreatureSize = "Grande",
                ThreatRole = "Solo",
                SpeedMetersOverride = 6f,
                FixedDefense = 22,
                DamageReduction = 10,
                ThreatAttackBonus = 17,
                InitiativeOverride = 1,
                PerceptionOverride = 4,
                FortitudeOverride = 13,
                ReflexOverride = 5,
                WillOverride = 6,
                SensesNotes = "Visão no escuro; Imune a magia mental e veneno",
                TreasureNotes = "Padrão (Núcleo Elemental: T$ 400)",
                ThreatNotes = "Bloqueia passagens estreitas e usa Sopro Tóxico contra grupos.",
                Strength = 6,
                Dexterity = 0,
                Constitution = 5,
                Intelligence = -2,
                Wisdom = 0,
                Charisma = -4
            };
            s.Inventory.Add(new CharacterItem("Pancada de Ferro", "Punho maciço de metal", ItemType.Weapon, 0, "2d8+10", "20/x2", 0, 0, 0, true, T20DamageType.Bludgeoning, false, attackBonus: 17));
            s.Skills.Add(new CharacterSkill("Sopro Tóxico", "Gasta 3 PM para exalar nuvem venenosa (4d6 de dano).", 3, T20ActionType.Standard, 0, "", false, "4d6"));
            return s;
        }

        private static CharacterSheetData BuildDragaoJovem(int id, string? customName)
        {
            var s = new CharacterSheetData(id, customName ?? "Dragão Jovem", "Solo", "Monstro", "Bestiário", "Kallyadranoch", 8, 220, 24)
            {
                IsEnemy = true,
                ChallengeRating = "7",
                CreatureType = "Monstro",
                CreatureSize = "Enorme",
                ThreatRole = "Solo",
                SpeedMetersOverride = 12f,
                FixedDefense = 25,
                DamageReduction = 10,
                ThreatAttackBonus = 21,
                InitiativeOverride = 8,
                PerceptionOverride = 11,
                FortitudeOverride = 15,
                ReflexOverride = 11,
                WillOverride = 10,
                SpellKeyAttribute = AttributeType.Charisma,
                SensesNotes = "Percepção às cegas 9m, Visão no escuro",
                TreasureNotes = "Dobro",
                ThreatNotes = "Alterna ataques de mordida e garras com Sopro Elemental em cone.",
                Strength = 7,
                Dexterity = 2,
                Constitution = 6,
                Intelligence = 3,
                Wisdom = 3,
                Charisma = 4
            };
            s.Inventory.Add(new CharacterItem("Mordida Dracônica", "Mandíbula devastadora", ItemType.Weapon, 0, "2d10+12", "20/x2", 0, 0, 0, true, T20DamageType.Piercing, false, attackBonus: 21));
            s.Inventory.Add(new CharacterItem("Garras", "Corte duplo com garras", ItemType.Weapon, 0, "2d6+10", "20/x2", 0, 0, 0, true, T20DamageType.Slashing, false, attackBonus: 21));
            s.Skills.Add(new CharacterSkill("Sopro Elemental", "Gasta 4 PM para lançar cone elemental (6d8 de dano).", 4, T20ActionType.Standard, 0, "", false, "6d8"));
            return s;
        }
    }
}
