using System;
using System.Collections.Generic;
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

        public T20PowerTreeDefinition(
            string key,
            string name,
            string parentKey,
            string description,
            T20ActionType actionType,
            int pmCost,
            string branchName = "Combate",
            int tier = 1,
            string baseFormula = "")
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
        }
    }

    /// <summary>
    /// Regras de Concessão e Uso da Árvore N-ária de Poderes/Habilidades de Tormenta20 (Edição Jogo do Ano),
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
                "+2 na Defesa e no ataque empunhando arma em uma mão sem escudo (Requer: DES >= 1)",
                T20ActionType.Free, 0, "1. Esgrima (DES)", tier: 1),
            new T20PowerTreeDefinition(
                "ataque_preciso", "Ataque Preciso", "estilo_uma_arma",
                "+2 na margem de ameaça e +1 no multiplicador de crítico (Requer nó pai: Estilo de Uma Arma)",
                T20ActionType.Free, 0, "1. Esgrima (DES)", tier: 2),
            new T20PowerTreeDefinition(
                "finta_aprimorada", "Finta Aprimorada", "ataque_preciso",
                "+2 em Enganação para fintar e finta como Ação de Movimento (Requer: Ataque Preciso e Nível >= 4)",
                T20ActionType.Movement, 1, "1. Esgrima (DES)", tier: 3),

            // =================================================================
            // RAMO 2: Brutalidade & Força (Ramificação N-ária: 1 Pai -> 2 Filhos + Neto)
            // =================================================================
            new T20PowerTreeDefinition(
                "ataque_poderoso", "Ataque Poderoso", "",
                "Sofre -2 no ataque para receber +5 no dano corpo a corpo (Requer: FOR >= 1)",
                T20ActionType.Free, 0, "2. Brutalidade (FOR)", tier: 1),
            new T20PowerTreeDefinition(
                "trespassar", "Trespassar", "ataque_poderoso",
                "Gasta 1 PM para realizar um ataque extra após golpear (Requer nó pai: Ataque Poderoso | Uso no turno: 'Agredir (Ataque Padrão)')",
                T20ActionType.Free, 1, "2. Brutalidade (FOR)", tier: 2, baseFormula: "1d8"),
            new T20PowerTreeDefinition(
                "quebrar_aprimorado", "Quebrar Aprimorado", "ataque_poderoso",
                "+2 em ataques para quebrar e golpe extra ao destruir item (Requer nó pai: Ataque Poderoso e FOR >= 2)",
                T20ActionType.Free, 1, "2. Brutalidade (FOR)", tier: 2),
            new T20PowerTreeDefinition(
                "golpe_demolidor", "Golpe Demolidor", "trespassar",
                "Gasta 2 PM para desferir um impacto devastador que causa +2d6 de dano extra (Requer: Ataque Poderoso -> Trespassar e Nível >= 8)",
                T20ActionType.Free, 2, "2. Brutalidade (FOR)", tier: 3, baseFormula: "2d6"),

            // =================================================================
            // RAMO 3: Bastião & Armadura Pesada (3 Níveis de Profundidade)
            // =================================================================
            new T20PowerTreeDefinition(
                "encouracado", "Encouraçado", "",
                "+2 na Defesa usando armadura pesada (Requer: CON >= 1)",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 1),
            new T20PowerTreeDefinition(
                "inexpugnavel", "Inexpugnável", "encouracado",
                "+2 em todos os testes de resistência vestindo armadura pesada (Requer nó pai: Encouraçado e Nível >= 6)",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 2),
            new T20PowerTreeDefinition(
                "fanatico", "Fanático", "inexpugnavel",
                "Armadura pesada não reduz seu deslocamento (Requer cadeia ancestral: Encouraçado -> Inexpugnável e Nível >= 12)",
                T20ActionType.Free, 0, "3. Bastião (CON)", tier: 3),

            // =================================================================
            // RAMO 4: Alta Magia & Arcanismo (Lógica AnyOf/OR + 2 Filhos + Neto)
            // =================================================================
            new T20PowerTreeDefinition(
                "magia_ilimitada", "Magia Ilimitada", "",
                "Soma seu atributo-chave no limite de PM que pode gastar em uma magia (Requer: INT >= 1 OU CAR >= 1)",
                T20ActionType.Free, 0, "4. Arcanismo (INT/CAR)", tier: 1),
            new T20PowerTreeDefinition(
                "surto_arcano", "Surto Arcano", "magia_ilimitada",
                "Descarga de 3d6 de essência pura (Requer nó pai: Magia Ilimitada | Uso no turno: 'Foco_Arcano' ativo OU ter usado 'Mísseis Mágicos')",
                T20ActionType.Free, 2, "4. Arcanismo (INT/CAR)", tier: 2, baseFormula: "3d6"),
            new T20PowerTreeDefinition(
                "magia_acelerada", "Magia Acelerada", "magia_ilimitada",
                "Muda a execução de uma magia para Ação Livre (+4 PM) uma vez por rodada (Requer: Magia Ilimitada e Nível >= 4)",
                T20ActionType.Free, 4, "4. Arcanismo (INT/CAR)", tier: 2),
            new T20PowerTreeDefinition(
                "arcano_de_batalha", "Arcano de Batalha", "surto_arcano",
                "Soma seu atributo-chave nas rolagens de dano de todas as suas magias (Requer: Magia Ilimitada -> Surto Arcano e Nível >= 6)",
                T20ActionType.Free, 0, "4. Arcanismo (INT/CAR)", tier: 3),

            // =================================================================
            // RAMO 5: Arquearia & Pontaria (Combate à Distância)
            // =================================================================
            new T20PowerTreeDefinition(
                "estilo_disparo", "Estilo de Disparo", "",
                "Soma sua Destreza nas rolagens de dano com armas de disparo (Requer: DES >= 1)",
                T20ActionType.Free, 0, "5. Arquearia (DES)", tier: 1),
            new T20PowerTreeDefinition(
                "disparo_preciso", "Disparo Preciso", "estilo_disparo",
                "Faz ataques à distância contra oponentes envolvidos em combate corpo a corpo sem sofrer -5 (Requer: Estilo de Disparo)",
                T20ActionType.Free, 0, "5. Arquearia (DES)", tier: 2),
            new T20PowerTreeDefinition(
                "disparo_rapido", "Disparo Rápido", "disparo_preciso",
                "Gasta 1 PM para realizar um disparo adicional no turno com arma à distância (Requer: Estilo de Disparo -> Disparo Preciso e Nível >= 5)",
                T20ActionType.Free, 1, "5. Arquearia (DES)", tier: 3, baseFormula: "1d8")
        };

        public static void RegisterTormenta20PowerTrees(EncounterSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            foreach (var p in CanonicalPowers)
            {
                session.RegisterAbilityInTree(p.Key, p.Name, p.ParentKey);
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
                                                    IEnumerable<string>? unlockedAbilityKeys = null)
        {
            session.SetStat(combatantId, "Nivel", level);
            session.SetStat(combatantId, "FOR", str);
            session.SetStat(combatantId, "DES", dex);
            session.SetStat(combatantId, "CON", con);
            session.SetStat(combatantId, "INT", intel);
            session.SetStat(combatantId, "SAB", wis);
            session.SetStat(combatantId, "CAR", cha);

            if (unlockedAbilityKeys != null)
            {
                foreach (string key in unlockedAbilityKeys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        session.GrantAbilityUnchecked(combatantId, key);
                    }
                }
            }
        }
    }
}
