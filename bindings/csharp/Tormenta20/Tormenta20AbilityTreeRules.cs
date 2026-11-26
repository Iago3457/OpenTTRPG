using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20PowerTreeDefinition
    {
        public string Key { get; }
        public string Name { get; }
        public string ParentKey { get; }
        public string BranchName { get; }
        public int Tier { get; }
        public string Description { get; }
        public T20ActionType ActionType { get; }
        public int PmCost { get; }
        public string BaseFormula { get; }
        public int MaxUsesPerTurn { get; }
        public bool IsPassive { get; }

        public T20PowerTreeDefinition(
            string key,
            string name,
            string parentKey,
            string description,
            T20ActionType actionType,
            int pmCost,
            string branchName = "Combate",
            int tier = 1,
            string baseFormula = "",
            int maxUsesPerTurn = 1,
            bool isPassive = false)
        {
            Key = key;
            Name = name;
            ParentKey = parentKey ?? string.Empty;
            Description = description;
            ActionType = actionType;
            PmCost = pmCost;
            BranchName = branchName;
            Tier = tier;
            BaseFormula = baseFormula ?? string.Empty;
            MaxUsesPerTurn = maxUsesPerTurn;
            IsPassive = isPassive;
        }
    }

    /// <summary>
    /// Regras de Concessão (limitada por Nível e Raça) e Uso (economia de ações e usos por turno)
    /// da Árvore N-ária de Poderes/Habilidades de Tormenta20 (Edição Jogo do Ano),
    /// implementadas em C# sobre a Árvore Genérica do Core C++ (Tree&lt;T&gt; / AbilityTree / RequirementTree).
    /// </summary>
    public static class Tormenta20AbilityTreeRules
    {
        public const int RootRequirementNodeId = 1;

        public static readonly IReadOnlyList<T20PowerTreeDefinition> CanonicalPowers = new List<T20PowerTreeDefinition>
        {
            // =================================================================
            // RAMO 1: Esgrima & Destreza (Combate com Uma Arma)
            // =================================================================
            new T20PowerTreeDefinition(
                "estilo_uma_arma", "Estilo de Uma Arma", "",
                "+2 na Defesa e no ataque empunhando arma em uma mão sem escudo (Requer: DES >= 1) [Passivo]",
                T20ActionType.Free, 0, "1. Esgrima (DES)", tier: 1, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "ataque_preciso", "Ataque Preciso", "estilo_uma_arma",
                "+2 na margem de ameaça e +1 no multiplicador de crítico (Requer: Estilo de Uma Arma) [Passivo]",
                T20ActionType.Free, 0, "1. Esgrima (DES)", tier: 2, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "finta_aprimorada", "Finta Aprimorada", "ataque_preciso",
                "+2 em Enganação para fintar e finta como Ação de Movimento (Requer: Ataque Preciso e Nível >= 4)",
                T20ActionType.Movement, 1, "1. Esgrima (DES)", tier: 3, maxUsesPerTurn: 1, isPassive: false),

            // =================================================================
            // RAMO 2: Brutalidade & Força (Ramificação N-ária: 1 Pai -> 2 Filhos + Neto)
            // =================================================================
            new T20PowerTreeDefinition(
                "ataque_poderoso", "Ataque Poderoso", "",
                "Sofre -2 no ataque para receber +5 no dano corpo a corpo até 1x/turno (Requer: FOR >= 1)",
                T20ActionType.Free, 0, "2. Brutalidade (FOR)", tier: 1, maxUsesPerTurn: 1, isPassive: false),
            new T20PowerTreeDefinition(
                "trespassar", "Trespassar", "ataque_poderoso",
                "Gasta 1 PM para realizar um ataque extra após golpear (Requer: Ataque Poderoso | Uso no turno após Agredir)",
                T20ActionType.Free, 1, "2. Brutalidade (FOR)", tier: 2, baseFormula: "1d8", maxUsesPerTurn: 1, isPassive: false),
            new T20PowerTreeDefinition(
                "quebrar_aprimorado", "Quebrar Aprimorado", "ataque_poderoso",
                "+2 em ataques para quebrar e golpe extra ao destruir item (Requer: Ataque Poderoso e FOR >= 2)",
                T20ActionType.Free, 1, "2. Brutalidade (FOR)", tier: 2, maxUsesPerTurn: 1, isPassive: false),
            new T20PowerTreeDefinition(
                "golpe_demolidor", "Golpe Demolidor", "trespassar",
                "Gasta 2 PM (Ação Padrão) para desferir um impacto devastador que causa +2d6 de dano extra (Requer: Ataque Poderoso -> Trespassar e Nível >= 8)",
                T20ActionType.Standard, 2, "2. Brutalidade (FOR)", tier: 3, baseFormula: "2d6", maxUsesPerTurn: 1, isPassive: false),

            // =================================================================
            // RAMO 3: Bastião & Armadura Pesada (3 Níveis de Profundidade)
            // =================================================================
            new T20PowerTreeDefinition(
                "encouracado", "Encouraçado", "",
                "+2 na Defesa usando armadura pesada (Requer: CON >= 1) [Passivo]",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 1, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "inexpugnavel", "Inexpugnável", "encouracado",
                "+2 em todos os testes de resistência vestindo armadura pesada (Requer: Encouraçado e Nível >= 6) [Passivo]",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 2, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "fanatico", "Fanático", "inexpugnavel",
                "Armadura pesada não reduz seu deslocamento (Requer: Encouraçado -> Inexpugnável e Nível >= 12) [Passivo]",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 3, maxUsesPerTurn: 1, isPassive: true),

            // =================================================================
            // RAMO 4: Alta Magia & Arcanismo (Lógica AnyOf/OR + 2 Filhos + Neto)
            // =================================================================
            new T20PowerTreeDefinition(
                "magia_ilimitada", "Magia Ilimitada", "",
                "Soma seu atributo-chave no limite de PM que pode gastar em uma magia (Requer: INT >= 1 OU SAB >= 1 OU CAR >= 1) [Passivo]",
                T20ActionType.Free, 0, "4. Arcanismo (INT/CAR)", tier: 1, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "surto_arcano", "Surto Arcano", "magia_ilimitada",
                "Descarga de 3d6 de essência pura (Ação de Movimento | Requer: Magia Ilimitada | Ativar com Foco Arcano ou Mísseis Mágicos no turno)",
                T20ActionType.Movement, 2, "4. Arcanismo (INT/CAR)", tier: 2, baseFormula: "3d6", maxUsesPerTurn: 1, isPassive: false),
            new T20PowerTreeDefinition(
                "magia_acelerada", "Magia Acelerada", "magia_ilimitada",
                "Muda a execução de uma magia para Ação Livre (+4 PM) uma vez por rodada (Requer: Magia Ilimitada e Nível >= 4)",
                T20ActionType.Free, 4, "4. Arcanismo (INT/CAR)", tier: 2, maxUsesPerTurn: 1, isPassive: false),
            new T20PowerTreeDefinition(
                "arcano_de_batalha", "Arcano de Batalha", "surto_arcano",
                "Soma seu atributo-chave nas rolagens de dano de todas as suas magias (Requer: Magia Ilimitada -> Surto Arcano e Nível >= 6) [Passivo]",
                T20ActionType.Free, 0, "4. Arcanismo (INT/CAR)", tier: 3, maxUsesPerTurn: 1, isPassive: true),

            // =================================================================
            // RAMO 5: Arquearia & Pontaria (Combate à Distância)
            // =================================================================
            new T20PowerTreeDefinition(
                "estilo_disparo", "Estilo de Disparo", "",
                "Soma sua Destreza nas rolagens de dano com armas de disparo (Requer: DES >= 1) [Passivo]",
                T20ActionType.Free, 0, "5. Arquearia (DES)", tier: 1, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "disparo_preciso", "Disparo Preciso", "estilo_disparo",
                "Faz ataques à distância contra oponentes envolvidos em combate corpo a corpo sem sofrer -5 (Requer: Estilo de Disparo) [Passivo]",
                T20ActionType.Free, 0, "5. Arquearia (DES)", tier: 2, maxUsesPerTurn: 1, isPassive: true),
            new T20PowerTreeDefinition(
                "disparo_rapido", "Disparo Rápido", "disparo_preciso",
                "Gasta 1 PM para realizar um disparo adicional no turno com arma à distância (Requer: Estilo de Disparo -> Disparo Preciso e Nível >= 5)",
                T20ActionType.Free, 1, "5. Arquearia (DES)", tier: 3, baseFormula: "1d8", maxUsesPerTurn: 1, isPassive: false)
        };

        public static T20PowerTreeDefinition? FindPowerDefinition(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (var p in CanonicalPowers)
            {
                if (p.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        private static string NormalizeRaceKey(string? race)
        {
            if (string.IsNullOrWhiteSpace(race)) return string.Empty;
            string decomposed = race.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Retorna a quantidade extra de habilidades/poderes concedidos pela raça em Tormenta20
        /// (ex.: Humano "Versátil" +1, Lefou "Cria da Tormenta/Deformidade" +1, Osteon "Memória Póstuma" +1).
        /// </summary>
        public static int GetRacialBonusAbilitySlots(string? race)
        {
            string norm = NormalizeRaceKey(race);
            if (string.IsNullOrEmpty(norm)) return 0;

            if (norm.StartsWith("human", StringComparison.Ordinal) ||
                norm.StartsWith("lefou", StringComparison.Ordinal) ||
                norm.StartsWith("osteon", StringComparison.Ordinal))
            {
                return 1;
            }
            return 0;
        }

        public static string GetRacialBonusAbilityReason(string? race)
        {
            string norm = NormalizeRaceKey(race);
            if (norm.StartsWith("human", StringComparison.Ordinal)) return "Versátil (Humano: +1 habilidade)";
            if (norm.StartsWith("lefou", StringComparison.Ordinal)) return "Cria da Tormenta (Lefou: +1 habilidade)";
            if (norm.StartsWith("osteon", StringComparison.Ordinal)) return "Memória Póstuma (Osteon: +1 habilidade)";
            return string.Empty;
        }

        /// <summary>
        /// Em Tormenta20, o personagem pode adquirir 1 poder/habilidade por nível + eventuais bônus raciais.
        /// </summary>
        public static int CalculateMaxAbilitySlots(int level, string? race = null, int bonusAbilitySlots = 0)
        {
            int effectiveLevel = Math.Max(1, level);
            return Math.Max(0, effectiveLevel + GetRacialBonusAbilitySlots(race) + bonusAbilitySlots);
        }

        public static int GetMaxAbilitySlots(CharacterSheetData sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            return CalculateMaxAbilitySlots(sheet.Level, sheet.Race, sheet.BonusAbilitySlots);
        }

        public static int GetUsedAbilitySlots(CharacterSheetData sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in sheet.UnlockedTreeKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    unique.Add(key);
                }
            }
            return unique.Count;
        }

        public static int GetRemainingAbilitySlots(CharacterSheetData sheet)
        {
            return Math.Max(0, GetMaxAbilitySlots(sheet) - GetUsedAbilitySlots(sheet));
        }

        public static string FormatAbilitySlotBudget(CharacterSheetData sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            int used = GetUsedAbilitySlots(sheet);
            int max = GetMaxAbilitySlots(sheet);
            int remaining = Math.Max(0, max - used);
            int racialBonus = GetRacialBonusAbilitySlots(sheet.Race);

            var parts = new List<string> { $"Nível {Math.Max(1, sheet.Level)}" };
            if (racialBonus > 0)
            {
                parts.Add($"Raça {sheet.Race} +{racialBonus}");
            }
            if (sheet.BonusAbilitySlots != 0)
            {
                string sign = sheet.BonusAbilitySlots > 0 ? "+" : "";
                parts.Add($"Bônus {sign}{sheet.BonusAbilitySlots}");
            }

            return $"{used}/{max} usadas ({remaining} disponível(is)) [{string.Join(" | ", parts)}]";
        }

        public static void RegisterTormenta20PowerTrees(EncounterSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            foreach (var p in CanonicalPowers)
            {
                session.RegisterAbilityInTree(p.Key, p.Name, p.ParentKey);
                session.SetAbilityActionConfig(p.Key, (int)p.ActionType, p.IsPassive ? 0 : p.MaxUsesPerTurn);
            }

            // ── Ramo 1: Esgrima ───────────────────────────────────────────────
            session.AddAbilityRequirementNode("estilo_uma_arma", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "DES", threshold: 1);
            session.AddAbilityRequirementNode("finta_aprimorada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 4);

            // ── Ramo 2: Brutalidade ───────────────────────────────────────────
            session.AddAbilityRequirementNode("ataque_poderoso", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "FOR", threshold: 1);
            session.AddAbilityRequirementNode("trespassar", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ActionUsedInTurn, "Agredir (Ataque Padrão)", threshold: 1);
            session.AddAbilityRequirementNode("quebrar_aprimorado", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "FOR", threshold: 2);
            session.AddAbilityRequirementNode("golpe_demolidor", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 8);

            // ── Ramo 3: Bastião ───────────────────────────────────────────────
            session.AddAbilityRequirementNode("encouracado", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "CON", threshold: 1);
            session.AddAbilityRequirementNode("inexpugnavel", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 6);
            session.AddAbilityRequirementNode("fanatico", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 12);

            // ── Ramo 4: Arcanismo ─────────────────────────────────────────────
            session.SetAbilityRequirementRoot("magia_ilimitada", isUsageRequirement: false, RequirementKind.AnyOf);
            session.AddAbilityRequirementNode("magia_ilimitada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "INT", threshold: 1);
            session.AddAbilityRequirementNode("magia_ilimitada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "SAB", threshold: 1);
            session.AddAbilityRequirementNode("magia_ilimitada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "CAR", threshold: 1);

            session.SetAbilityRequirementRoot("surto_arcano", isUsageRequirement: true, RequirementKind.AnyOf);
            session.AddAbilityRequirementNode("surto_arcano", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ConditionActive, "Foco_Arcano", threshold: 1);
            session.AddAbilityRequirementNode("surto_arcano", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ActionUsedInTurn, "Mísseis Mágicos", threshold: 1);

            session.AddAbilityRequirementNode("magia_acelerada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 4);
            session.AddAbilityRequirementNode("arcano_de_batalha", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 6);

            // ── Ramo 5: Arquearia ─────────────────────────────────────────────
            session.AddAbilityRequirementNode("estilo_disparo", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "DES", threshold: 1);
            session.AddAbilityRequirementNode("disparo_rapido", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 5);
        }

        public static void SyncCharacterStatsToCore(EncounterSession session, int combatantId,
                                                    int level, int str, int dex, int con, int intel, int wis, int cha,
                                                    IEnumerable<string>? unlockedAbilityKeys = null,
                                                    string? race = null, int bonusAbilitySlots = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            session.SetStat(combatantId, "Nivel", level);
            session.SetStat(combatantId, "FOR", str);
            session.SetStat(combatantId, "DES", dex);
            session.SetStat(combatantId, "CON", con);
            session.SetStat(combatantId, "INT", intel);
            session.SetStat(combatantId, "SAB", wis);
            session.SetStat(combatantId, "CAR", cha);
            session.SetMaxAbilities(combatantId, CalculateMaxAbilitySlots(level, race, bonusAbilitySlots));

            if (unlockedAbilityKeys != null)
            {
                var unlockedSet = new HashSet<string>(unlockedAbilityKeys, StringComparer.OrdinalIgnoreCase);
                foreach (var p in CanonicalPowers)
                {
                    if (!unlockedSet.Contains(p.Key))
                    {
                        session.RevokeAbility(combatantId, p.Key);
                    }
                }
                foreach (string key in unlockedSet)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        session.GrantAbilityUnchecked(combatantId, key);
                    }
                }
            }
        }

        public static void SyncCharacterToCore(EncounterSession session, int combatantId, CharacterSheetData sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            SyncCharacterStatsToCore(
                session, combatantId,
                sheet.Level,
                sheet.Strength, sheet.Dexterity, sheet.Constitution,
                sheet.Intelligence, sheet.Wisdom, sheet.Charisma,
                sheet.UnlockedTreeKeys,
                sheet.Race,
                sheet.BonusAbilitySlots);
        }

        public static bool CanLearnPower(EncounterSession session, int combatantId, CharacterSheetData sheet,
                                         string powerKey, out string unmetReasons)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            session.SetMaxAbilities(combatantId, GetMaxAbilitySlots(sheet));
            return session.CanLearnAbility(combatantId, powerKey, out unmetReasons);
        }

        public static bool TryLearnPower(EncounterSession session, int combatantId, CharacterSheetData sheet,
                                         string powerKey, out string message)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            var power = FindPowerDefinition(powerKey);
            if (power == null)
            {
                message = $"[ERRO] Poder '{powerKey}' não encontrado na Árvore de Poderes de Tormenta20.";
                return false;
            }

            if (sheet.UnlockedTreeKeys.Contains(power.Key))
            {
                message = $"[AVISO] {sheet.Name} já possui '{power.Name}'.";
                return false;
            }

            session.SetMaxAbilities(combatantId, GetMaxAbilitySlots(sheet));

            if (!session.TryLearnAbility(combatantId, power.Key, out string unmet))
            {
                string racialNote = GetRacialBonusAbilityReason(sheet.Race);
                string racialHint = string.IsNullOrEmpty(racialNote) ? "" : $" | {racialNote}";
                message = $"[PRÉ-REQUISITOS / LIMITE DE NÍVEL NA ÁRVORE] Não foi possível aprender '{power.Name}': {unmet} " +
                          $"(Saldo: {FormatAbilitySlotBudget(sheet)}{racialHint})";
                return false;
            }

            sheet.UnlockedTreeKeys.Add(power.Key);

            bool hasSkillEntry = false;
            foreach (var existingSkill in sheet.Skills)
            {
                if (existingSkill.TreeKey == power.Key)
                {
                    hasSkillEntry = true;
                    break;
                }
            }
            if (!hasSkillEntry)
            {
                sheet.Skills.Add(new CharacterSkill(
                    power.Name,
                    power.Description,
                    power.PmCost,
                    power.ActionType,
                    0,
                    power.Key,
                    isSpell: false,
                    baseFormula: power.BaseFormula,
                    maxUsesPerTurn: power.IsPassive ? 0 : power.MaxUsesPerTurn,
                    isPassive: power.IsPassive));
            }

            message = $"[SUCESSO] '{power.Name}' desbloqueado na Árvore de Poderes! ({FormatAbilitySlotBudget(sheet)})";
            return true;
        }
    }
}
