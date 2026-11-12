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
        public string Description { get; }
        public T20ActionType ActionType { get; }
        public int PmCost { get; }

        public T20PowerTreeDefinition(string key, string name, string parentKey, string description,
                                      T20ActionType actionType, int pmCost)
        {
            Key = key;
            Name = name;
            ParentKey = parentKey ?? string.Empty;
            Description = description;
            ActionType = actionType;
            PmCost = pmCost;
        }
    }

    /// <summary>
    /// Regras de Concessão e Uso da Árvore de Poderes/Habilidades de Tormenta20 (Edição Jogo do Ano),
    /// implementadas em C# sobre a Árvore Genérica do Core (EncounterSession).
    /// </summary>
    public static class Tormenta20AbilityTreeRules
    {
        public const int RootRequirementNodeId = 1;

        public static readonly IReadOnlyList<T20PowerTreeDefinition> CanonicalPowers = new List<T20PowerTreeDefinition>
        {
            // Ramo 1: Combate com Uma Arma (Destreza)
            new T20PowerTreeDefinition(
                "estilo_uma_arma", "Estilo de Uma Arma", "",
                "+2 na Defesa e no ataque empunhando arma em uma mão (Requer: DES >= 1)",
                T20ActionType.Free, 0),
            new T20PowerTreeDefinition(
                "ataque_preciso", "Ataque Preciso", "estilo_uma_arma",
                "+2 na margem de ameaça e +1 no multiplicador de crítico (Requer nó pai: Estilo de Uma Arma)",
                T20ActionType.Free, 0),

            // Ramo 2: Combate Pesado / Força (Com pré-requisito de ação usada antes no turno!)
            new T20PowerTreeDefinition(
                "ataque_poderoso", "Ataque Poderoso", "",
                "Sofre -2 no ataque para receber +5 no dano (Requer: FOR >= 1)",
                T20ActionType.Free, 0),
            new T20PowerTreeDefinition(
                "trespassar", "Trespassar", "ataque_poderoso",
                "Realiza ataque extra após golpear (Requer aprender: Ataque Poderoso | Requer usar no turno antes: 'Agredir (Ataque Padrão)')",
                T20ActionType.Free, 1),

            // Ramo 3: Armadura Pesada (3 níveis de profundidade na Árvore!)
            new T20PowerTreeDefinition(
                "encouracado", "Encouraçado", "",
                "+2 na Defesa com armadura pesada (Requer: CON >= 1)",
                T20ActionType.Free, 0),
            new T20PowerTreeDefinition(
                "inexpugnavel", "Inexpugnável", "encouracado",
                "+2 em todos os testes de resistência (Requer nó pai: Encouraçado E Nivel >= 6)",
                T20ActionType.Free, 0),
            new T20PowerTreeDefinition(
                "fanatico", "Fanático", "inexpugnavel",
                "Armadura pesada não reduz deslocamento (Requer cadeia ancestral: Encouraçado -> Inexpugnável E Nivel >= 12)",
                T20ActionType.Free, 0),

            // Ramo 4: Magia Arcana (Pré-requisito lógico OU para aprender + Condição ou Ação prévia para usar)
            new T20PowerTreeDefinition(
                "magia_ilimitada", "Magia Ilimitada", "",
                "Soma atributo-chave no limite de PM (Requer: INT >= 1 OU CAR >= 1)",
                T20ActionType.Free, 0),
            new T20PowerTreeDefinition(
                "surto_arcano", "Surto Arcano", "magia_ilimitada",
                "Descarga arcana concentrada (Requer nó pai: Magia Ilimitada | Para usar no turno: condição 'Foco_Arcano' ativa OU ter usado 'Mísseis Mágicos' no turno)",
                T20ActionType.Free, 2)
        };

        public static void RegisterTormenta20PowerTrees(EncounterSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            foreach (var p in CanonicalPowers)
            {
                session.RegisterAbilityInTree(p.Key, p.Name, p.ParentKey);
            }

            // 1. Estilo de Uma Arma -> Requer DES >= 1
            session.AddAbilityRequirementNode("estilo_uma_arma", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "DES", threshold: 1);

            // 2. Ataque Poderoso -> Requer FOR >= 1
            session.AddAbilityRequirementNode("ataque_poderoso", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "FOR", threshold: 1);

            // 3. Trespassar -> Requer ter usado "Agredir (Ataque Padrão)" antes neste turno
            session.AddAbilityRequirementNode("trespassar", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ActionUsedInTurn, "Agredir (Ataque Padrão)", threshold: 1);

            // 4. Encouraçado -> Inexpugnável (Nivel >= 6) -> Fanático (Nivel >= 12)
            session.AddAbilityRequirementNode("encouracado", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "CON", threshold: 1);
            session.AddAbilityRequirementNode("inexpugnavel", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 6);
            session.AddAbilityRequirementNode("fanatico", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "Nivel", threshold: 12);

            // 5. Magia Ilimitada -> Requer (INT >= 1 OU CAR >= 1)
            session.SetAbilityRequirementRoot("magia_ilimitada", isUsageRequirement: false, RequirementKind.AnyOf);
            session.AddAbilityRequirementNode("magia_ilimitada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "INT", threshold: 1);
            session.AddAbilityRequirementNode("magia_ilimitada", isUsageRequirement: false,
                RootRequirementNodeId, RequirementKind.StatAtLeast, "CAR", threshold: 1);

            // 6. Surto Arcano -> Condição "Foco_Arcano" ativa OU ter usado "Mísseis Mágicos" neste turno
            session.SetAbilityRequirementRoot("surto_arcano", isUsageRequirement: true, RequirementKind.AnyOf);
            session.AddAbilityRequirementNode("surto_arcano", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ConditionActive, "Foco_Arcano", threshold: 1);
            session.AddAbilityRequirementNode("surto_arcano", isUsageRequirement: true,
                RootRequirementNodeId, RequirementKind.ActionUsedInTurn, "Mísseis Mágicos", threshold: 1);
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

