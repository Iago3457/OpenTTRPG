using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

namespace OpenTTRPG.CLI
{
    public sealed partial class CLIController
    {
        public void HandleInitiativeCombat()
        {
            Console.WriteLine("\n--- COMBATE DE TORMENTA20 (SELEÇÃO DE LISTA DE INICIATIVA) ---");
            if (_characters.Count == 0)
            {
                Console.WriteLine("Cadastre personagens antes de iniciar um combate.");
                return;
            }

            using var session = new EncounterSession();
            Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);
            var charEntityIds = new List<int>(_characters.Count);

            Console.WriteLine("\nPersonagens cadastrados disponíveis:");
            for (int i = 0; i < _characters.Count; i++)
            {
                CharacterSheetData c = _characters[i];
                Tormenta20SkillRules.TryCalculateSkillModifier(c, "Iniciativa", out int initMod, out _);
                int eid = session.AddCombatant(c.Name, initMod, c.MaxHp, 1);
                session.SetResourceCurrent(eid, HealthResourceId, c.CurrentHp);
                session.AddResource(eid, ManaResourceId, "PM", c.MaxPm, 0);
                session.SetResourceCurrent(eid, ManaResourceId, c.CurrentPm);
                Tormenta20ActionRules.ConfigureTurnActions(session, eid, standardActions: 1, movementActions: 1, freeActions: -1);
                Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
                    session, eid, c.Level,
                    c.Strength, c.Dexterity, c.Constitution,
                    c.Intelligence, c.Wisdom, c.Charisma,
                    c.UnlockedTreeKeys);

                if (c.IsOverburdened())
                {
                    Tormenta20ConditionRules.ApplyCondition(session, eid, T20Condition.Sobrecarregado);
                }

                charEntityIds.Add(eid);

                string initStr = initMod >= 0 ? $"+{initMod}" : initMod.ToString();
                Console.WriteLine($"  [{i + 1}] {c.Name} (PV: {c.CurrentHp}/{c.MaxHp} | PM: {c.CurrentPm}/{c.MaxPm} | Iniciativa: {initStr})");
            }

            Console.WriteLine("\nQuais personagens vão participar da lista de iniciativa?");
            Console.Write("Digite os números separados por espaço/vírgula (ex: 1 2) ou Enter/T para Todos: ");
            string selLine = GetCleanLine();

            var selectedEntityIds = new List<int>();
            var selectedCharIndices = new List<int>();

            if (string.IsNullOrEmpty(selLine) ||
                selLine.Equals("t", StringComparison.OrdinalIgnoreCase) ||
                selLine.Equals("todos", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < charEntityIds.Count; i++)
                {
                    selectedEntityIds.Add(charEntityIds[i]);
                    selectedCharIndices.Add(i);
                }
            }
            else
            {
                foreach (Match match in Regex.Matches(selLine, @"\d+"))
                {
                    if (int.TryParse(match.Value, out int idx) && idx >= 1 && idx <= charEntityIds.Count)
                    {
                        int eid = charEntityIds[idx - 1];
                        if (!selectedEntityIds.Contains(eid))
                        {
                            selectedEntityIds.Add(eid);
                            selectedCharIndices.Add(idx - 1);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  [AVISO] Índice '{match.Value}' ignorado (fora do intervalo).");
                    }
                }
            }

            if (selectedEntityIds.Count == 0)
            {
                Console.WriteLine("[AVISO] Nenhum combatente selecionado para a iniciativa. Combate cancelado.");
                return;
            }

            Console.WriteLine($"\nRolando Iniciativa (1d20 + Perícia Iniciativa) em lote para os {selectedEntityIds.Count} participantes selecionados...");
            session.RollInitiativeForList(selectedEntityIds.ToArray(), 1, 20);

            foreach (int charIdx in selectedCharIndices)
            {
                int eid = charEntityIds[charIdx];
                CombatantInfo info = session.GetCombatantInfo(eid);
                _characters[charIdx].LastInitiativeRoll = info.Initiative;
                Console.WriteLine($"  - {_characters[charIdx].Name} entrou na iniciativa com total: {info.Initiative}");
            }

            try
            {
                session.StartCombat();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] {ex.Message}");
                return;
            }

            void SyncBackToSheets()
            {
                for (int i = 0; i < charEntityIds.Count; i++)
                {
                    ResourceInfo hp = session.GetResource(charEntityIds[i], HealthResourceId);
                    ResourceInfo pm = session.GetResource(charEntityIds[i], ManaResourceId);
                    _characters[i].CurrentHp = hp.Current;
                    _characters[i].CurrentPm = pm.Current;
                }
            }

            int SelectParticipantTarget(string prompt)
            {
                Console.WriteLine($"\n{prompt}");
                int[] orderIds = session.GetInitiativeOrder();
                for (int i = 0; i < orderIds.Length; i++)
                {
                    int id = orderIds[i];
                    int cIdx = charEntityIds.IndexOf(id);
                    string name = session.GetCombatantName(id);
                    ResourceInfo hp = session.GetResource(id, HealthResourceId);
                    CombatantInfo info = session.GetCombatantInfo(id);
                    int def = cIdx >= 0 ? _characters[cIdx].GetDefense(session, id) : 10;
                    string status = info.Defeated ? " - FORA DE COMBATE/CAÍDO" : "";
                    Console.WriteLine($"  {i + 1}. [ID {id}] {name} (PV: {hp.Current}/{hp.Max} | Defesa: {def}{status})");
                }
                Console.Write($"Escolha o número na lista (1 a {orderIds.Length}): ");
                int choice = ParsePositiveInt(GetCleanLine(), 0);
                if (choice < 1 || choice > orderIds.Length)
                {
                    Console.WriteLine("[ERRO] Alvo inválido.");
                    return 0;
                }
                return orderIds[choice - 1];
            }

            Console.WriteLine("\nCombate iniciado! Ações durante o turno NÃO avançam a vez automaticamente.");

            bool inCombat = true;
            int lastProcessedTurnId = -1;
            int lastProcessedRound = -1;

            while (inCombat && session.IsActive)
            {
                int currentId = session.CurrentTurn;
                if (currentId <= 0) break;

                int charIdx = charEntityIds.IndexOf(currentId);
                CharacterSheetData? currentSheet = charIdx >= 0 ? _characters[charIdx] : null;

                if (currentId != lastProcessedTurnId || session.Round != lastProcessedRound)
                {
                    lastProcessedTurnId = currentId;
                    lastProcessedRound = session.Round;
                    var startLogs = Tormenta20ConditionRules.ProcessStartOfTurnConditions(session, currentId, currentSheet);
                    foreach (string log in startLogs)
                    {
                        Console.WriteLine(log);
                    }
                }

                string currentName = session.GetCombatantName(currentId);
                CombatantInfo info = session.GetCombatantInfo(currentId);
                ResourceInfo hp = session.GetResource(currentId, HealthResourceId);
                ResourceInfo pm = session.GetResource(currentId, ManaResourceId);
                int defense = currentSheet != null ? currentSheet.GetDefense(session, currentId) : 10;
                var activeConditions = Tormenta20ConditionRules.GetActiveConditionNames(session, currentId);
                string condStr = activeConditions.Count > 0 ? $" | CONDIÇÕES: [{string.Join(", ", activeConditions)}]" : "";

                Console.WriteLine("\n======================================================================");
                Console.WriteLine($">> [RODADA {session.Round}]");
                Console.WriteLine($">> VEZ DE: {currentName} (ID {currentId}) | PV: {hp.Current}/{hp.Max} | PM: {pm.Current}/{pm.Max} | DEFESA: {defense} | INICIATIVA: {info.Initiative}{condStr}");
                Console.WriteLine($">> AÇÕES NO TURNO (T20): {Tormenta20ActionRules.FormatRemainingActions(session, currentId)}");

                QueuedActionEntry[] queued = session.GetActionQueue(currentId);
                if (queued.Length > 0)
                {
                    Console.WriteLine(">> FILA DE PRIORIDADES DE AÇÕES (Ordem: Livre -> Movimento -> Padrão):");
                    for (int q = 0; q < queued.Length; q++)
                    {
                        var qa = queued[q];
                        string reqName = Tormenta20ActionRules.GetActionTypeName((T20ActionType)qa.RequiredType);
                        string consName = Tormenta20ActionRules.GetActionTypeName((T20ActionType)qa.ConsumedType);
                        string conv = qa.UsedFallback ? $" [Pagou usando {consName}]" : "";
                        Console.WriteLine($"     {q + 1}º. [{reqName}] {qa.Name}{conv}");
                    }
                }
                Console.WriteLine("======================================================================");

                Console.WriteLine("Ações de Tormenta20 no Turno:");
                Console.WriteLine("  1. Encerrar Turno (Executa a Fila de Prioridades: Livre -> Movimento -> Padrão e avança)");
                Console.WriteLine("  2. Realizar Ataque Padrão (Automático contra Defesa do alvo ou rolagem manual)");
                Console.WriteLine("  3. Realizar Ação de Movimento (Consome Movimento, ou usa Ação Padrão se não tiver Movimento!)");
                Console.WriteLine("  4. Usar Poder / Habilidade / Magia (com suporte a Aprimoramentos +PM e Limite de PM)");
                Console.WriteLine("  5. Executar Próxima Ação da Fila de Prioridades Agora");
                Console.WriteLine("  6. Causar Dano a um Alvo (Selecionar por ID)");
                Console.WriteLine("  7. Curar um Alvo (Selecionar por ID)");
                Console.WriteLine("  8. Alternar Estado Fora de Combate / Morrido de um Combatente");
                Console.WriteLine("  9. Encerrar Combate");
                Console.WriteLine("  10. Aplicar ou Remover Condição de Tormenta20 (Abalado, Caído, Sangrando, etc.)");
                Console.Write("Escolha uma ação: ");

                int action = ParsePositiveInt(GetCleanLine(), 0);

                if (action == 1)
                {
                    List<QueuedActionEntry> executed = Tormenta20ActionRules.DrainPriorityQueueInOrder(session, currentId);
                    if (executed.Count > 0)
                    {
                        Console.WriteLine("\n>> Executando Fila de Prioridades do turno (Livre -> Movimento -> Padrão):");
                        for (int i = 0; i < executed.Count; i++)
                        {
                            var ex = executed[i];
                            string rName = Tormenta20ActionRules.GetActionTypeName((T20ActionType)ex.RequiredType);
                            Console.WriteLine($"   [{i + 1}º] ({rName}) {ex.Name} executada!");
                        }
                    }

                    try
                    {
                        session.NextTurn();
                    }
                    catch
                    {
                        Console.WriteLine("\n[FIM] Nenhum combatente ativo restante na iniciativa.");
                        inCombat = false;
                    }
                }
                else if (action == 2)
                {
                    Console.Write("Modificador do ataque/perícia (ex: +6, 0) ou 'A' para Ataque Automático contra Alvo: ");
                    string atkInput = GetCleanLine();

                    if (atkInput.Equals("a", StringComparison.OrdinalIgnoreCase) && currentSheet != null)
                    {
                        int targetId = SelectParticipantTarget("Selecione o ALVO do ataque:");
                        int targetIdx = charEntityIds.IndexOf(targetId);
                        if (targetId > 0 && targetIdx >= 0)
                        {
                            var atkRes = Tormenta20CombatRules.ResolveWeaponAttack(
                                session, currentSheet, currentId, _characters[targetIdx], targetId);
                            SyncBackToSheets();
                            Console.WriteLine(atkRes.Message);
                        }
                    }
                    else
                    {
                        var actRes = Tormenta20ActionRules.EnqueueTurnAction(
                            session, currentId, T20ActionType.Standard, "Agredir (Ataque Padrão)");
                        Console.WriteLine(actRes.Message);
                        if (actRes.Success)
                        {
                            int mod = ParseSignedInt(atkInput, 0);
                            string expr = mod >= 0 ? $"1d20+{mod}" : $"1d20{mod}";
                            RollResult roll = session.Roll(expr);
                            Console.WriteLine($"Resultado do Ataque: {roll.Total} (Dado: {roll.DiceTotal}, Mod: {roll.Modifier})");
                        }
                    }
                }
                else if (action == 3)
                {
                    Console.Write("Descrição do Movimento (ex: Deslocar 9m, Sacar Arma, Levantar-se): ");
                    string movDesc = GetCleanLine();
                    if (string.IsNullOrEmpty(movDesc)) movDesc = "Movimentar-se";

                    var actRes = Tormenta20ActionRules.EnqueueTurnAction(
                        session, currentId, T20ActionType.Movement, movDesc);
                    Console.WriteLine(actRes.Message);
                }
                else if (action == 4)
                {
                    if (currentSheet != null && currentSheet.Skills.Count > 0)
                    {
                        List<CharacterSkill> skills = currentSheet.Skills;
                        int pmLimit = Tormenta20MagicRules.GetPmSpendLimit(currentSheet, session, currentId);
                        Console.WriteLine($"\nHabilidades de {currentName} (Limite de gasto por uso: {pmLimit} PM):");
                        for (int i = 0; i < skills.Count; i++)
                        {
                            Console.WriteLine($"  {i + 1}. {skills[i].Name} [{Tormenta20ActionRules.GetActionTypeName(skills[i].Action)} | {skills[i].PmCost} PM] - {skills[i].Description}");
                        }
                        Console.WriteLine($"  {skills.Count + 1}. Digitar habilidade/magia avulsa");
                        Console.Write("Escolha a habilidade: ");
                        int skChoice = ParsePositiveInt(GetCleanLine(), 0);

                        if (skChoice >= 1 && skChoice <= skills.Count)
                        {
                            CharacterSkill chosen = skills[skChoice - 1];
                            int enhPm = 0;
                            if (chosen.IsSpell || chosen.Enhancements.Count > 0)
                            {
                                Console.Write($"PM extras em Aprimoramentos (0 para base, máx permitido pelo limite={Math.Max(0, pmLimit - chosen.PmCost)}): ");
                                enhPm = Math.Max(0, ParseSignedInt(GetCleanLine(), 0));
                            }

                            var res = Tormenta20MagicRules.EnqueueSkillWithEnhancements(
                                session, currentId, currentSheet, chosen, enhPm);
                            SyncBackToSheets();
                            Console.WriteLine(res.Message);
                            continue;
                        }
                    }

                    Console.Write("Tipo de Ação (1=Livre, 2=Movimento, 3=Padrão/Comum, 4=Completa): ");
                    int tChoice = ParsePositiveInt(GetCleanLine(), 3);
                    T20ActionType reqActionType = tChoice switch
                    {
                        1 => T20ActionType.Free,
                        2 => T20ActionType.Movement,
                        4 => T20ActionType.Full,
                        _ => T20ActionType.Standard
                    };
                    Console.Write("Custo em PM: ");
                    int pmCost = Math.Max(0, ParseSignedInt(GetCleanLine(), 1));

                    var fallbackRes = Tormenta20ActionRules.EnqueueTurnAction(session, currentId, reqActionType, "Poder/Magia", 0, pmCost);
                    SyncBackToSheets();
                    Console.WriteLine(fallbackRes.Message);
                }
                else if (action == 5)
                {
                    if (session.PopNextAction(currentId, out QueuedActionEntry nextEntry))
                    {
                        string rName = Tormenta20ActionRules.GetActionTypeName((T20ActionType)nextEntry.RequiredType);
                        Console.WriteLine($"[EXECUTADA] ({rName}) {nextEntry.Name} foi consumida da frente da Fila de Prioridades!");
                    }
                    else
                    {
                        Console.WriteLine("[AVISO] A Fila de Prioridades de ações deste turno está vazia.");
                    }
                }
                else if (action == 6)
                {
                    int targetId = SelectParticipantTarget("Selecione o ALVO que sofrerá o dano:");
                    if (targetId > 0)
                    {
                        Console.Write("Quantidade de dano: ");
                        int dmgAmt = ParsePositiveInt(GetCleanLine(), 0);
                        DamageResult res = session.DealDamage(targetId, dmgAmt);
                        SyncBackToSheets();
                        string targetName = session.GetCombatantName(targetId);
                        string fallenMsg = res.IsDepleted ? " >>> O ALVO CAIU (0 PV) E SERÁ PULADO NA INICIATIVA!" : "";
                        Console.WriteLine($"[OK] {targetName} sofreu {res.FinalAmount} de dano!{fallenMsg}");
                    }
                }
                else if (action == 7)
                {
                    int targetId = SelectParticipantTarget("Selecione o ALVO que receberá a cura:");
                    if (targetId > 0)
                    {
                        Console.Write("Quantidade de cura (PV): ");
                        int healAmt = ParsePositiveInt(GetCleanLine(), 0);
                        int healed = session.Heal(targetId, healAmt);
                        SyncBackToSheets();
                        string targetName = session.GetCombatantName(targetId);
                        Console.WriteLine($"[OK] {targetName} recuperou {healed} PV!");
                    }
                }
                else if (action == 8)
                {
                    int targetId = SelectParticipantTarget("Selecione o combatente para alternar Fora de Combate:");
                    if (targetId > 0)
                    {
                        CombatantInfo targetInfo = session.GetCombatantInfo(targetId);
                        bool nextForcedOut = !targetInfo.Defeated;
                        session.SetForcedOut(targetId, nextForcedOut);
                        string targetName = session.GetCombatantName(targetId);
                        Console.WriteLine($"[OK] {targetName} agora está {(nextForcedOut ? "FORA DE COMBATE" : "ATIVO NO COMBATE")}.");
                    }
                }
                else if (action == 9)
                {
                    SyncBackToSheets();
                    session.EndCombat();
                    Console.WriteLine("Combate de Tormenta20 encerrado.");
                    inCombat = false;
                }
                else if (action == 10)
                {
                    int targetId = SelectParticipantTarget("Selecione o combatente para gerenciar Condições de T20:");
                    if (targetId > 0)
                    {
                        var allConds = (T20Condition[])Enum.GetValues(typeof(T20Condition));
                        Console.WriteLine("\nCatálogo de Condições Canônicas de Tormenta20:");
                        for (int i = 0; i < allConds.Length; i++)
                        {
                            string key = Tormenta20ConditionRules.GetConditionKey(allConds[i]);
                            bool hasIt = session.HasCondition(targetId, key);
                            Console.WriteLine($"  {i + 1,2}. {key,-15} {(hasIt ? "[ATIVA]" : "       ")} — {Tormenta20ConditionRules.GetConditionDescription(allConds[i])}");
                        }
                        Console.Write("Escolha o número da condição para aplicar/remover: ");
                        int cOpt = ParsePositiveInt(GetCleanLine(), 0);
                        if (cOpt >= 1 && cOpt <= allConds.Length)
                        {
                            T20Condition chosenCond = allConds[cOpt - 1];
                            string key = Tormenta20ConditionRules.GetConditionKey(chosenCond);
                            if (session.HasCondition(targetId, key) && chosenCond != T20Condition.Abalado)
                            {
                                Tormenta20ConditionRules.RemoveCondition(session, targetId, chosenCond);
                                Console.WriteLine($"[OK] Condição '{key}' removida.");
                            }
                            else
                            {
                                string msg = Tormenta20ConditionRules.ApplyCondition(session, targetId, chosenCond);
                                Console.WriteLine(msg);
                            }
                        }
                    }
                }
            }

            SyncBackToSheets();
        }
    }
}

