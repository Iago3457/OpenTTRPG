using System;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

namespace OpenTTRPG.CLI
{
    public sealed partial class CLIController
    {
        public void HandleCreateCharacter()
        {
            Console.WriteLine("\n--- CRIAÇÃO DE PERSONAGEM (TORMENTA20) ---");
            Console.Write("Nome do Personagem: ");
            string name = GetCleanLine();
            Console.Write("Raça: ");
            string race = GetCleanLine();
            Console.Write("Classe: ");
            string charClass = GetCleanLine();
            Console.Write("Origem: ");
            string origin = GetCleanLine();
            Console.Write("Divindade: ");
            string deity = GetCleanLine();
            Console.Write("Nível (padrão 1): ");
            int level = Math.Max(1, ParsePositiveInt(GetCleanLine(), 1));

            Console.Write("Pontos de Vida Máximos (PV): ");
            int maxHp = Math.Max(1, ParsePositiveInt(GetCleanLine(), 20));
            Console.Write("Pontos de Mana Máximos (PM): ");
            int maxPm = Math.Max(0, ParseSignedInt(GetCleanLine(), 3));

            var sheet = new CharacterSheetData(_nextCharacterId++, name, charClass, race, origin, deity, level, maxHp, maxPm);

            Console.WriteLine("\n[DEFINA OS 6 ATRIBUTOS DO T20 (ex: 3 para +3, -1 para -1)]:");
            Console.Write("  Força (FOR): "); sheet.Strength = ParseSignedInt(GetCleanLine());
            Console.Write("  Destreza (DES): "); sheet.Dexterity = ParseSignedInt(GetCleanLine());
            Console.Write("  Constituição (CON): "); sheet.Constitution = ParseSignedInt(GetCleanLine());
            Console.Write("  Inteligência (INT): "); sheet.Intelligence = ParseSignedInt(GetCleanLine());
            Console.Write("  Sabedoria (SAB): "); sheet.Wisdom = ParseSignedInt(GetCleanLine());
            Console.Write("  Carisma (CAR): "); sheet.Charisma = ParseSignedInt(GetCleanLine());

            _characters.Add(sheet);
            Console.WriteLine($"\n[OK] Personagem '{name}' (Nv.{level}) criado com sucesso!");
        }

        public void HandleListCharacters()
        {
            Console.WriteLine($"\n--- PERSONAGENS CADASTRADOS ({_characters.Count}) ---");
            if (_characters.Count == 0)
            {
                Console.WriteLine("Nenhum aventureiro cadastrado ainda.");
                return;
            }

            for (int i = 0; i < _characters.Count; i++)
            {
                CharacterSheetData c = _characters[i];
                Console.WriteLine($"\n[{i + 1}] ID {c.Id} - {c.Name}");
                Console.WriteLine(c.FormatSummary());
                if (c.TrainedSkills.Count > 0)
                {
                    Console.WriteLine($"  Perícias Treinadas: {string.Join(", ", c.TrainedSkills)}");
                }
                if (c.Inventory.Count > 0)
                {
                    Console.WriteLine("  Inventário:");
                    foreach (var it in c.Inventory)
                    {
                        Console.WriteLine($"    - {it.FormatSummary()}");
                    }
                }
            }
        }

        public void HandleManageCharacter()
        {
            if (_characters.Count == 0)
            {
                Console.WriteLine("\nNenhum personagem disponível para gerenciar.");
                return;
            }

            Console.WriteLine("\n--- SELECIONE O PERSONAGEM ---");
            for (int i = 0; i < _characters.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_characters[i].Name} (Nv.{_characters[i].Level})");
            }
            Console.Write("Número do personagem: ");
            int choice = ParsePositiveInt(GetCleanLine(), 0);
            if (choice < 1 || choice > _characters.Count)
            {
                Console.WriteLine("Seleção inválida.");
                return;
            }

            CharacterSheetData selected = _characters[choice - 1];
            Console.WriteLine($"\nGerenciando Ficha de: {selected.Name}");
            Console.WriteLine("  1. Adicionar Equipamento / Item (com Espaços e Penalidade de Armadura)");
            Console.WriteLine("  2. Adicionar Poder ou Magia com PM");
            Console.WriteLine("  3. Gastar Pontos de Mana (PM)");
            Console.WriteLine("  4. Recuperar Pontos de Mana (PM)");
            Console.WriteLine("  5. Sofrer Dano em Pontos de Vida (PV)");
            Console.WriteLine("  6. Curar Pontos de Vida (PV)");
            Console.WriteLine("  7. Desbloquear Poder na Árvore de Pré-requisitos (Tormenta20)");
            Console.WriteLine("  8. Treinar Perícia ou Consultar Tabela de Perícias de T20");
            Console.WriteLine("  9. Descansar (Recuperação de PV e PM por Qualidade do Descanso)");
            Console.WriteLine("  10. Equipar / Desequipar Item do Inventário");
            Console.Write("Opção: ");
            int subOpt = ParsePositiveInt(GetCleanLine(), 0);

            if (subOpt == 1)
            {
                Console.Write("Nome do Item: ");
                string itemName = GetCleanLine();
                Console.Write("Descrição: ");
                string desc = GetCleanLine();
                Console.Write("Tipo (1=Arma, 2=Armadura, 3=Escudo, 4=Consumível, 5=Acessório, 6=Outros): ");
                ItemType type = (ItemType)Math.Clamp(ParsePositiveInt(GetCleanLine(), 1), 1, 6);
                Console.Write("Espaços ocupados (padrão 1): ");
                int slots = ParsePositiveInt(GetCleanLine(), 1);

                string damage = "";
                string crit = "20/x2";
                int defBonus = 0;
                int penalty = 0;
                bool isRanged = false;

                if (type == ItemType.Weapon)
                {
                    Console.Write("Dano da Arma (ex: 1d8, 2d6): ");
                    damage = GetCleanLine();
                    Console.Write("Crítico (ex: 19/x2, 18/x3, 20/x2): ");
                    crit = GetCleanLine();
                    Console.Write("Arma à distância / disparo? (s/N): ");
                    isRanged = GetCleanLine().StartsWith("s", StringComparison.OrdinalIgnoreCase);
                }
                else if (type == ItemType.Armor || type == ItemType.Shield)
                {
                    Console.Write("Bônus na Defesa (ex: +5): ");
                    defBonus = ParseSignedInt(GetCleanLine());
                    Console.Write("Penalidade de Armadura (ex: -2): ");
                    penalty = ParseSignedInt(GetCleanLine());
                }

                Console.Write("Preço em Tibares (T$): ");
                int price = ParsePositiveInt(GetCleanLine(), 0);
                Console.Write("Equipar imediatamente? (s/N): ");
                bool equip = GetCleanLine().StartsWith("s", StringComparison.OrdinalIgnoreCase);

                selected.Inventory.Add(new CharacterItem(itemName, desc, type, slots, damage, crit, defBonus, penalty, price, equip, T20DamageType.Slashing, isRanged));
                Console.WriteLine($"[OK] Item adicionado! Carga atual: {selected.GetCurrentSlots()}/{selected.GetMaxSlots()} espaços.");
                if (selected.IsOverburdened())
                {
                    Console.WriteLine("[AVISO T20] Personagem ficou SOBRECARREGADO (-5 na penalidade de armadura e -3m deslocamento)!");
                }
            }
            else if (subOpt == 2)
            {
                Console.Write("Nome da Habilidade/Poder/Magia: ");
                string skillName = GetCleanLine();
                Console.Write("Descrição: ");
                string desc = GetCleanLine();
                Console.Write("Custo base em PM: ");
                int pmCost = Math.Max(0, ParseSignedInt(GetCleanLine(), 1));
                Console.Write("Tipo de Ação (1=Padrão, 2=Movimento, 3=Livre, 4=Reação, 5=Completa): ");
                int actOpt = Math.Clamp(ParsePositiveInt(GetCleanLine(), 1), 1, 5);
                T20ActionType actionType = actOpt switch
                {
                    2 => T20ActionType.Movement,
                    3 => T20ActionType.Free,
                    4 => T20ActionType.Reaction,
                    5 => T20ActionType.Full,
                    _ => T20ActionType.Standard
                };
                Console.Write("Modificador numérico associado (ou 0): ");
                int mod = ParseSignedInt(GetCleanLine(), 0);

                selected.Skills.Add(new CharacterSkill(skillName, desc, pmCost, actionType, mod));
                Console.WriteLine("[OK] Poder/Magia adicionado!");
            }
            else if (subOpt == 3)
            {
                Console.Write("Quantidade de PM a gastar: ");
                int pm = ParsePositiveInt(GetCleanLine(), 0);
                if (selected.SpendPm(pm))
                {
                    Console.WriteLine($"[OK] {pm} PM gastos. PM Atual: {selected.CurrentPm}/{selected.MaxPm}");
                }
                else
                {
                    Console.WriteLine("[ERRO] Pontos de Mana insuficientes!");
                }
            }
            else if (subOpt == 4)
            {
                Console.Write("Quantidade de PM a recuperar: ");
                int pm = ParsePositiveInt(GetCleanLine(), 0);
                selected.RecoverPm(pm);
                Console.WriteLine($"[OK] PM Atual: {selected.CurrentPm}/{selected.MaxPm}");
            }
            else if (subOpt == 5)
            {
                Console.Write("Dano sofrido em PV: ");
                int dmg = ParsePositiveInt(GetCleanLine(), 0);
                selected.TakeDamage(dmg);
                Console.WriteLine($"[OK] PV Atual: {selected.CurrentHp}/{selected.MaxHp}");
            }
            else if (subOpt == 6)
            {
                Console.Write("Cura recebida em PV: ");
                int heal = ParsePositiveInt(GetCleanLine(), 0);
                selected.HealHp(heal);
                Console.WriteLine($"[OK] PV Atual: {selected.CurrentHp}/{selected.MaxHp}");
            }
            else if (subOpt == 7)
            {
                using var session = new EncounterSession();
                Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);
                int eid = session.AddCombatant(selected.Name, selected.Dexterity, selected.MaxHp, 1);
                Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
                    session, eid, selected.Level,
                    selected.Strength, selected.Dexterity, selected.Constitution,
                    selected.Intelligence, selected.Wisdom, selected.Charisma,
                    selected.UnlockedTreeKeys);

                Console.WriteLine($"\n--- ÁRVORE DE PODERES DE TORMENTA20 ({selected.Name} Nv.{selected.Level}) ---");
                var powers = Tormenta20AbilityTreeRules.CanonicalPowers;
                for (int i = 0; i < powers.Count; i++)
                {
                    var p = powers[i];
                    bool alreadyHas = session.HasAbility(eid, p.Key);
                    bool canLearn = session.CanLearnAbility(eid, p.Key, out string unmet);
                    string status = alreadyHas
                        ? "[JÁ APRENDIDO]"
                        : (canLearn ? "[DISPONÍVEL]" : $"[BLOQUEADO: {unmet}]");
                    string indent = string.IsNullOrEmpty(p.ParentKey) ? "  " : "    -> ";
                    Console.WriteLine($"{indent}{i + 1}. {p.Name} {status} — {p.Description}");
                }

                Console.Write("\nEscolha o número do poder para tentar aprender (ou 0 para cancelar): ");
                int pChoice = ParsePositiveInt(GetCleanLine(), 0);
                if (pChoice >= 1 && pChoice <= powers.Count)
                {
                    var chosenPower = powers[pChoice - 1];
                    if (session.HasAbility(eid, chosenPower.Key))
                    {
                        Console.WriteLine($"[AVISO] {selected.Name} já possui '{chosenPower.Name}'.");
                    }
                    else if (session.TryLearnAbility(eid, chosenPower.Key, out string unmet))
                    {
                        selected.UnlockedTreeKeys.Add(chosenPower.Key);
                        selected.Skills.Add(new CharacterSkill(
                            chosenPower.Name, chosenPower.Description, chosenPower.PmCost, chosenPower.ActionType, 0, chosenPower.Key));
                        Console.WriteLine($"[SUCESSO] '{chosenPower.Name}' desbloqueado na Árvore de Poderes e adicionado à ficha!");
                    }
                    else
                    {
                        Console.WriteLine($"[PRÉ-REQUISITOS NÃO ATENDIDOS NA ÁRVORE] Não foi possível aprender '{chosenPower.Name}': {unmet}");
                    }
                }
            }
            else if (subOpt == 8)
            {
                Console.WriteLine($"\n--- TABELA DE PERÍCIAS DE TORMENTA20 ({selected.Name} Nv.{selected.Level}) ---");
                var allSkills = Tormenta20SkillRules.CanonicalSkills;
                for (int i = 0; i < allSkills.Count; i++)
                {
                    var sk = allSkills[i];
                    bool trained = selected.TrainedSkills.Contains(sk.Name);
                    bool canRoll = Tormenta20SkillRules.TryCalculateSkillModifier(selected, sk.Name, out int mod, out _);
                    string modStr = canRoll ? (mod >= 0 ? $"+{mod}" : mod.ToString()) : "BLOQUEADA (Somente Treinada)";
                    string flags = $"{(trained ? "[TREINADA] " : "")}{(sk.TrainedOnly ? "(Somente Treinada) " : "")}{(sk.AppliesArmorPenalty ? "(Pen.Armadura)" : "")}";
                    Console.WriteLine($"  {i + 1,2}. {sk.Name,-14} | Total: {modStr,-28} | {flags}");
                }
                Console.Write("\nDigite o número da perícia para alternar Treinamento (ou 0 para voltar): ");
                int skIdx = ParsePositiveInt(GetCleanLine(), 0);
                if (skIdx >= 1 && skIdx <= allSkills.Count)
                {
                    string skName = allSkills[skIdx - 1].Name;
                    if (!selected.TrainedSkills.Add(skName))
                    {
                        selected.TrainedSkills.Remove(skName);
                        Console.WriteLine($"[OK] Perícia '{skName}' marcada como NÃO treinada.");
                    }
                    else
                    {
                        Console.WriteLine($"[OK] Perícia '{skName}' agora é TREINADA!");
                    }
                }
            }
            else if (subOpt == 9)
            {
                Console.WriteLine("\n--- DESCANSO E RECUPERAÇÃO ENTRE ENCONTROS (TORMENTA20) ---");
                Console.WriteLine("  1. Ruim (0,5x Nível em PV e PM, mín. 1)");
                Console.WriteLine("  2. Normal (1x Nível em PV e PM)");
                Console.WriteLine("  3. Confortável (2x Nível em PV e PM)");
                Console.WriteLine("  4. Luxuoso (3x Nível em PV e PM)");
                Console.Write("Qualidade do descanso: ");
                RestQuality q = (RestQuality)Math.Clamp(ParsePositiveInt(GetCleanLine(), 2), 1, 4);
                T20RestResult restRes = Tormenta20RestRules.ApplyRest(selected, q);
                Console.WriteLine(restRes.Message);
            }
            else if (subOpt == 10)
            {
                if (selected.Inventory.Count == 0)
                {
                    Console.WriteLine("Inventário vazio.");
                    return;
                }
                for (int i = 0; i < selected.Inventory.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {selected.Inventory[i].FormatSummary()}");
                }
                Console.Write("Número do item para alternar Equipado/Guardado: ");
                int itIdx = ParsePositiveInt(GetCleanLine(), 0);
                if (itIdx >= 1 && itIdx <= selected.Inventory.Count)
                {
                    var item = selected.Inventory[itIdx - 1];
                    item.Equipped = !item.Equipped;
                    Console.WriteLine($"[OK] '{item.Name}' agora está {(item.Equipped ? "Equipado" : "Guardado")}. Defesa: {selected.GetDefense()} | Pen.Armadura: {selected.GetTotalArmorPenalty()}");
                }
            }
        }

        public void HandleDiceRoll()
        {
            Console.WriteLine("\n--- ROLAGEM DE DADOS OU TESTE DE PERÍCIA (CORE NATIVO) ---");
            Console.WriteLine("  1. Rolar expressão de dados livre (ex: 1d20+5, 2d20kh1+4, 4d6dl1)");
            Console.WriteLine("  2. Realizar Teste de Perícia de um Personagem (ex: Acrobacia, Luta, Ladinagem, Misticismo)");
            Console.Write("Opção (Enter=1): ");
            int mode = ParsePositiveInt(GetCleanLine(), 1);

            using var session = new EncounterSession();
            if (mode == 2 && _characters.Count > 0)
            {
                for (int i = 0; i < _characters.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {_characters[i].Name}");
                }
                Console.Write("Escolha o personagem: ");
                int cIdx = ParsePositiveInt(GetCleanLine(), 1);
                if (cIdx >= 1 && cIdx <= _characters.Count)
                {
                    var sheet = _characters[cIdx - 1];
                    Console.Write("Nome da Perícia (ex: Luta, Acrobacia, Furtividade, Ladinagem, Misticismo): ");
                    string skName = GetCleanLine();
                    Console.Write("Classe de Dificuldade - CD (0 para sem CD): ");
                    int dc = Math.Max(0, ParseSignedInt(GetCleanLine(), 0));
                    T20SkillCheckResult check = Tormenta20SkillRules.RollSkillCheck(session, sheet, skName, dc);
                    Console.WriteLine(check.Message);
                }
                return;
            }

            Console.Write("Digite a expressão de dados (ex: 1d20+5, 2d20kh1+4, 4d6dl1, 2d8+3): ");
            string expr = GetCleanLine();
            if (string.IsNullOrEmpty(expr)) return;

            try
            {
                RollResult res = session.Roll(expr);
                Console.WriteLine($"Resultado: Total = {res.Total} (Dados: {res.DiceTotal}, Modificador: {res.Modifier}, Críticos Naturais: {res.NaturalMaxCount})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] {ex.Message}");
            }
        }
        public void HandleSaveCharacters()
        {
            Console.WriteLine("\n--- SALVAR FICHAS EM JSON ---");
            if (_characters.Count == 0)
            {
                Console.WriteLine("[AVISO] Nenhuma ficha cadastrada para salvar.");
                return;
            }

            string path = "saves/characters.json";
            Tormenta20Persistence.SaveCharactersToFile(_characters, path);
            Console.WriteLine($"[SUCESSO] {_characters.Count} ficha(s) salva(s) com sucesso em '{path}'!");
        }

        public void HandleLoadCharacters()
        {
            Console.WriteLine("\n--- CARREGAR FICHAS DE JSON ---");
            string path = "saves/characters.json";
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"[AVISO] Arquivo '{path}' não encontrado.");
                return;
            }

            var loaded = Tormenta20Persistence.LoadCharactersFromFile(path);
            if (loaded.Count == 0)
            {
                Console.WriteLine("[AVISO] O arquivo JSON não possui fichas.");
                return;
            }

            _characters.Clear();
            _characters.AddRange(loaded);

            // Recalcula o próximo ID para evitar IDs repetidos ao criar novos personagens
            int maxId = 0;
            foreach (var c in _characters)
            {
                if (c.Id > maxId) maxId = c.Id;
            }
            _nextCharacterId = maxId + 1;

            Console.WriteLine($"[SUCESSO] {loaded.Count} ficha(s) carregada(s) com sucesso de '{path}'!");
        }
    }
}

