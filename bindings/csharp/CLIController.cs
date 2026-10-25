using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

namespace OpenTTRPG.CLI
{
    public enum ItemType
    {
        Weapon = 1,
        Armor = 2,
        Shield = 3,
        Consumable = 4,
        Miscellaneous = 5
    }

    public enum ActionType
    {
        Standard = 1,
        Movement = 2,
        Free = 3,
        Reaction = 4
    }

    public sealed class CharacterItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int Slots { get; set; }
        public string Damage { get; set; }
        public string Critical { get; set; }
        public int DefenseBonus { get; set; }
        public int ArmorPenalty { get; set; }
        public int Price { get; set; }
        public bool Equipped { get; set; }

        public CharacterItem(string name, string description, ItemType type, int slots,
                             string damage = "", string critical = "20/x2",
                             int defenseBonus = 0, int armorPenalty = 0, int price = 0, bool equipped = false)
        {
            Name = name;
            Description = description;
            Type = type;
            Slots = Math.Max(0, slots);
            Damage = damage ?? string.Empty;
            Critical = string.IsNullOrWhiteSpace(critical) ? "20/x2" : critical;
            DefenseBonus = defenseBonus;
            ArmorPenalty = armorPenalty;
            Price = Math.Max(0, price);
            Equipped = equipped;
        }
    }

    public sealed class CharacterSkill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int PmCost { get; set; }
        public ActionType Action { get; set; }
        public int Modifier { get; set; }

        public CharacterSkill(string name, string description, int pmCost, ActionType action, int modifier = 0)
        {
            Name = name;
            Description = description;
            PmCost = Math.Max(0, pmCost);
            Action = action;
            Modifier = modifier;
        }
    }

    public sealed class CharacterSheetData
    {
        public int Id { get; }
        public string Name { get; set; }
        public string CharacterClass { get; set; }
        public string Race { get; set; }
        public string Origin { get; set; }
        public string Deity { get; set; }
        public int Level { get; set; }

        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int MaxPm { get; set; }
        public int CurrentPm { get; set; }

        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        public int LastInitiativeRoll { get; set; }

        public List<CharacterItem> Inventory { get; } = new List<CharacterItem>();
        public List<CharacterSkill> Skills { get; } = new List<CharacterSkill>();

        public CharacterSheetData(int id, string name, string charClass, string race, string origin, string deity,
                                  int level, int maxHp, int maxPm)
        {
            Id = id;
            Name = name;
            CharacterClass = charClass;
            Race = race;
            Origin = origin;
            Deity = deity;
            Level = Math.Max(1, level);
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            MaxPm = Math.Max(0, maxPm);
            CurrentPm = MaxPm;
        }

        public int GetDefense()
        {
            int armorBonus = 0;
            foreach (var item in Inventory)
            {
                if (item.Equipped && (item.Type == ItemType.Armor || item.Type == ItemType.Shield))
                {
                    armorBonus += item.DefenseBonus;
                }
            }
            return 10 + Dexterity + armorBonus;
        }

        public string FormatSummary()
        {
            return $"  {Name} ({Race} {CharacterClass} Nv.{Level} | Origem: {Origin} | Devoto: {Deity})\n" +
                   $"  PV: {CurrentHp}/{MaxHp} | PM: {CurrentPm}/{MaxPm} | Defesa: {GetDefense()}\n" +
                   $"  FOR: {FormatSigned(Strength)} | DES: {FormatSigned(Dexterity)} | CON: {FormatSigned(Constitution)} | " +
                   $"INT: {FormatSigned(Intelligence)} | SAB: {FormatSigned(Wisdom)} | CAR: {FormatSigned(Charisma)}";
        }

        private static string FormatSigned(int val) => val >= 0 ? $"+{val}" : val.ToString();
    }

    /// <summary>
    /// Controlador de linha de comando (CLI) em C# consumindo a DLL nativa (OpenTTRPG.Native.EncounterSession).
    /// Permite selecionar uma lista de personagens para a iniciativa via RollInitiativeForList,
    /// escolher alvos por ID, cobrar PM dinamicamente das habilidades e controlar turnos explicitamente.
    /// </summary>
    public sealed class CLIController
    {
        private const int HealthResourceId = 0;
        private const int ManaResourceId = 1;

        private readonly List<CharacterSheetData> _characters = new List<CharacterSheetData>();
        private int _nextCharacterId = 1;

        public CLIController()
        {
            SeedDefaultCharacters();
        }

        private void SeedDefaultCharacters()
        {
            // 1. Valeros de Valkaria - Guerreiro Humano
            var warrior = new CharacterSheetData(_nextCharacterId++, "Valeros de Valkaria", "Guerreiro", "Humano", "Soldado", "Valkaria", 1, 24, 4)
            {
                Strength = 3,
                Dexterity = 1,
                Constitution = 2,
                Intelligence = 0,
                Wisdom = 1,
                Charisma = 0
            };
            warrior.Inventory.Add(new CharacterItem("Espada Longa", "Espada versátil de aço", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 15, true));
            warrior.Inventory.Add(new CharacterItem("Cota de Malha", "Armadura pesada de metal", ItemType.Armor, 2, "", "20/x2", 5, -2, 150, true));
            warrior.Inventory.Add(new CharacterItem("Escudo Leve", "Escudo de madeira reforçada", ItemType.Shield, 1, "", "20/x2", 1, -1, 5, true));
            warrior.Inventory.Add(new CharacterItem("Bálsamo Restaurador", "Cura 2d4 PV", ItemType.Consumable, 1, "", "20/x2", 0, 0, 10, false));
            warrior.Skills.Add(new CharacterSkill("Ataque Especial", "Gasta 1 PM para receber +4 no ataque ou +1d6 no dano", 1, ActionType.Free, 4));
            warrior.Skills.Add(new CharacterSkill("Avanço Tático", "Desloca-se pelo campo de batalha buscando flanco", 0, ActionType.Movement, 0));
            warrior.Skills.Add(new CharacterSkill("Golpe Pessoal", "Ataque devastador personalizado de classe", 2, ActionType.Standard, 0));
            _characters.Add(warrior);

            // 2. Sillith - Arcanista Qareen
            var mage = new CharacterSheetData(_nextCharacterId++, "Sillith", "Arcanista", "Qareen", "Mago Aprendiz", "Wynna", 1, 11, 10)
            {
                Strength = -1,
                Dexterity = 2,
                Constitution = 1,
                Intelligence = 4,
                Wisdom = 1,
                Charisma = 3
            };
            mage.Inventory.Add(new CharacterItem("Varinha Arcana", "Foco arcano para canalização", ItemType.Weapon, 1, "1d4", "20/x2", 0, 0, 20, true));
            mage.Inventory.Add(new CharacterItem("Poção de Mana", "Recupera 2d4 PM", ItemType.Consumable, 1, "", "20/x2", 0, 0, 30, false));
            mage.Skills.Add(new CharacterSkill("Passo Etéreo", "Reposicionamento rápido arcano (Movimento)", 1, ActionType.Movement, 0));
            mage.Skills.Add(new CharacterSkill("Mísseis Mágicos", "Projéteis de essência: 2d4+2 de dano sem teste de resistência", 1, ActionType.Standard, 0));
            mage.Skills.Add(new CharacterSkill("Curar Ferimentos", "Recupera 2d8+2 PV de um aliado tocado", 1, ActionType.Standard, 0));
            _characters.Add(mage);
        }

        private static T20ActionType MapActionType(ActionType action)
        {
            return action switch
            {
                ActionType.Free => T20ActionType.Free,
                ActionType.Movement => T20ActionType.Movement,
                ActionType.Standard => T20ActionType.Standard,
                ActionType.Reaction => T20ActionType.Reaction,
                _ => T20ActionType.Standard
            };
        }

        private static string GetCleanLine()
        {
            return (Console.ReadLine() ?? string.Empty).Trim();
        }

        private static int ParsePositiveInt(string input, int defaultValue = 1)
        {
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            Match m = Regex.Match(input, @"[xX\*]?\s*(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int val))
            {
                return Math.Max(1, val);
            }
            return defaultValue;
        }

        private static int ParseSignedInt(string input, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            Match m = Regex.Match(input, @"([+-]?\s*\d+)");
            if (m.Success)
            {
                string cleaned = m.Groups[1].Value.Replace(" ", "");
                if (int.TryParse(cleaned, out int val))
                {
                    return val;
                }
            }
            return defaultValue;
        }

        private void PrintHeader()
        {
            Console.WriteLine("\n======================================================================");
            Console.WriteLine("       OpenTTRPG (C# CLI) — MOTOR DE TORMENTA20 VIA DLL NATIVA        ");
            Console.WriteLine("======================================================================");
        }

        private void ShowMainMenu()
        {
            Console.WriteLine("\n[MENU PRINCIPAL - TORMENTA20 (C#)]");
            Console.WriteLine("  1. Criar Personagem (Ficha de Tormenta20)");
            Console.WriteLine("  2. Listar e Visualizar Fichas de Personagens (PV, PM, Defesa)");
            Console.WriteLine("  3. Gerenciar Ficha (Equipamentos, Poderes com PM, Recursos)");
            Console.WriteLine("  4. Rolar Dados (via Core Nativo C++)");
            Console.WriteLine("  5. Combate com Seleção de Participantes na Iniciativa (EncounterSession)");
            Console.WriteLine("  0. Sair");
            Console.Write("Escolha uma opção: ");
        }

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

            Console.Write("Pontos de Vida Máximos (PV): ");
            int maxHp = ParsePositiveInt(GetCleanLine(), 20);
            Console.Write("Pontos de Mana Máximos (PM): ");
            int maxPm = Math.Max(0, ParseSignedInt(GetCleanLine(), 3));

            var sheet = new CharacterSheetData(_nextCharacterId++, name, charClass, race, origin, deity, 1, maxHp, maxPm);

            Console.WriteLine("\n[DEFINA OS 6 ATRIBUTOS DO T20 (ex: 3 para +3, -1 para -1)]:");
            Console.Write("  Força (FOR): "); sheet.Strength = ParseSignedInt(GetCleanLine());
            Console.Write("  Destreza (DES): "); sheet.Dexterity = ParseSignedInt(GetCleanLine());
            Console.Write("  Constituição (CON): "); sheet.Constitution = ParseSignedInt(GetCleanLine());
            Console.Write("  Inteligência (INT): "); sheet.Intelligence = ParseSignedInt(GetCleanLine());
            Console.Write("  Sabedoria (SAB): "); sheet.Wisdom = ParseSignedInt(GetCleanLine());
            Console.Write("  Carisma (CAR): "); sheet.Charisma = ParseSignedInt(GetCleanLine());

            _characters.Add(sheet);
            Console.WriteLine($"\n[OK] Personagem '{name}' criado com sucesso!");
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
                Console.WriteLine($"\n[{i + 1}] ID {_characters[i].Id} - {_characters[i].Name}");
                Console.WriteLine(_characters[i].FormatSummary());
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
                Console.WriteLine($"  {i + 1}. {_characters[i].Name}");
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
            Console.WriteLine("  1. Adicionar Equipamento / Item");
            Console.WriteLine("  2. Adicionar Poder ou Magia com PM");
            Console.WriteLine("  3. Gastar Pontos de Mana (PM)");
            Console.WriteLine("  4. Recuperar Pontos de Mana (PM)");
            Console.WriteLine("  5. Sofrer Dano em Pontos de Vida (PV)");
            Console.WriteLine("  6. Curar Pontos de Vida (PV)");
            Console.Write("Opção: ");
            int subOpt = ParsePositiveInt(GetCleanLine(), 0);

            if (subOpt == 1)
            {
                Console.Write("Nome do Item: ");
                string itemName = GetCleanLine();
                Console.Write("Descrição: ");
                string desc = GetCleanLine();
                Console.Write("Tipo (1=Arma, 2=Armadura, 3=Escudo, 4=Consumível, 5=Outros): ");
                ItemType type = (ItemType)Math.Clamp(ParsePositiveInt(GetCleanLine(), 1), 1, 5);
                Console.Write("Espaços ocupados (padrão 1): ");
                int slots = ParsePositiveInt(GetCleanLine(), 1);

                string damage = "";
                string crit = "20/x2";
                int defBonus = 0;
                int penalty = 0;

                if (type == ItemType.Weapon)
                {
                    Console.Write("Dano da Arma (ex: 1d8, 2d6): ");
                    damage = GetCleanLine();
                    Console.Write("Crítico (ex: 19/x2, 20/x2): ");
                    crit = GetCleanLine();
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

                selected.Inventory.Add(new CharacterItem(itemName, desc, type, slots, damage, crit, defBonus, penalty, price, false));
                Console.WriteLine("[OK] Item adicionado!");
            }
            else if (subOpt == 2)
            {
                Console.Write("Nome da Habilidade/Poder/Magia: ");
                string skillName = GetCleanLine();
                Console.Write("Descrição: ");
                string desc = GetCleanLine();
                Console.Write("Custo em PM: ");
                int pmCost = Math.Max(0, ParseSignedInt(GetCleanLine(), 1));
                Console.Write("Tipo de Ação (1=Padrão, 2=Movimento, 3=Livre, 4=Reação): ");
                ActionType actionType = (ActionType)Math.Clamp(ParsePositiveInt(GetCleanLine(), 1), 1, 4);
                Console.Write("Modificador numérico associado (ou 0): ");
                int mod = ParseSignedInt(GetCleanLine(), 0);

                selected.Skills.Add(new CharacterSkill(skillName, desc, pmCost, actionType, mod));
                Console.WriteLine("[OK] Poder/Magia adicionado!");
            }
            else if (subOpt == 3)
            {
                Console.Write("Quantidade de PM a gastar: ");
                int pm = ParsePositiveInt(GetCleanLine(), 0);
                if (selected.CurrentPm >= pm)
                {
                    selected.CurrentPm -= pm;
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
                selected.CurrentPm = Math.Min(selected.MaxPm, selected.CurrentPm + pm);
                Console.WriteLine($"[OK] PM Atual: {selected.CurrentPm}/{selected.MaxPm}");
            }
            else if (subOpt == 5)
            {
                Console.Write("Dano sofrido em PV: ");
                int dmg = ParsePositiveInt(GetCleanLine(), 0);
                selected.CurrentHp = Math.Max(0, selected.CurrentHp - dmg);
                Console.WriteLine($"[OK] PV Atual: {selected.CurrentHp}/{selected.MaxHp}");
            }
            else if (subOpt == 6)
            {
                Console.Write("Cura recebida em PV: ");
                int heal = ParsePositiveInt(GetCleanLine(), 0);
                selected.CurrentHp = Math.Min(selected.MaxHp, selected.CurrentHp + heal);
                Console.WriteLine($"[OK] PV Atual: {selected.CurrentHp}/{selected.MaxHp}");
            }
        }

        public void HandleDiceRoll()
        {
            Console.WriteLine("\n--- ROLAGEM DE DADOS (CORE NATIVO) ---");
            Console.Write("Digite a expressão de dados (ex: 1d20+5, 2d20kh1+4, 4d6dl1, 2d8+3): ");
            string expr = GetCleanLine();
            if (string.IsNullOrEmpty(expr)) return;

            using var session = new EncounterSession();
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

        public void HandleInitiativeCombat()
        {
            Console.WriteLine("\n--- COMBATE DE TORMENTA20 (SELEÇÃO DE LISTA DE INICIATIVA) ---");
            if (_characters.Count == 0)
            {
                Console.WriteLine("Cadastre personagens antes de iniciar um combate.");
                return;
            }

            using var session = new EncounterSession();
            var charEntityIds = new List<int>(_characters.Count);

            Console.WriteLine("\nPersonagens cadastrados disponíveis:");
            for (int i = 0; i < _characters.Count; i++)
            {
                CharacterSheetData c = _characters[i];
                int eid = session.AddCombatant(c.Name, c.Dexterity, c.MaxHp, 1);
                session.SetResourceCurrent(eid, HealthResourceId, c.CurrentHp);
                session.AddResource(eid, ManaResourceId, "PM", c.MaxPm, 0);
                session.SetResourceCurrent(eid, ManaResourceId, c.CurrentPm);
                Tormenta20ActionRules.ConfigureTurnActions(session, eid, standardActions: 1, movementActions: 1, freeActions: -1);
                charEntityIds.Add(eid);

                string dexStr = c.Dexterity >= 0 ? $"+{c.Dexterity}" : c.Dexterity.ToString();
                Console.WriteLine($"  [{i + 1}] {c.Name} (PV: {c.CurrentHp}/{c.MaxHp} | PM: {c.CurrentPm}/{c.MaxPm} | DES: {dexStr})");
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

            Console.WriteLine($"\nRolando Iniciativa (1d20 + Destreza) em lote para os {selectedEntityIds.Count} participantes selecionados...");

            // Rola iniciativa para a lista selecionada de uma só vez via P/Invoke
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
                    string name = session.GetCombatantName(id);
                    ResourceInfo hp = session.GetResource(id, HealthResourceId);
                    CombatantInfo info = session.GetCombatantInfo(id);
                    string status = info.Defeated ? " - FORA DE COMBATE/CAÍDO" : "";
                    Console.WriteLine($"  {i + 1}. [ID {id}] {name} (PV: {hp.Current}/{hp.Max}{status})");
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
            while (inCombat && session.IsActive)
            {
                int currentId = session.CurrentTurn;
                if (currentId <= 0) break;

                int charIdx = charEntityIds.IndexOf(currentId);
                string currentName = session.GetCombatantName(currentId);
                CombatantInfo info = session.GetCombatantInfo(currentId);
                ResourceInfo hp = session.GetResource(currentId, HealthResourceId);
                ResourceInfo pm = session.GetResource(currentId, ManaResourceId);
                int defense = charIdx >= 0 ? _characters[charIdx].GetDefense() : 10;

                Console.WriteLine("\n======================================================================");
                Console.WriteLine($">> [RODADA {session.Round}]");
                Console.WriteLine($">> VEZ DE: {currentName} (ID {currentId}) | PV: {hp.Current}/{hp.Max} | PM: {pm.Current}/{pm.Max} | DEFESA: {defense} | INICIATIVA: {info.Initiative}");
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
                Console.WriteLine("  2. Realizar Ataque Padrão (Consome Ação Padrão; bloqueia se só tiver Movimento)");
                Console.WriteLine("  3. Realizar Ação de Movimento (Consome Movimento, ou usa Ação Padrão se não tiver Movimento!)");
                Console.WriteLine("  4. Usar Poder / Habilidade / Magia (Enfileira por Prioridade: Livre -> Movimento -> Padrão)");
                Console.WriteLine("  5. Executar Próxima Ação da Fila de Prioridades Agora");
                Console.WriteLine("  6. Causar Dano a um Alvo (Selecionar por ID)");
                Console.WriteLine("  7. Curar um Alvo (Selecionar por ID)");
                Console.WriteLine("  8. Alternar Estado Fora de Combate / Morrido de um Combatente");
                Console.WriteLine("  9. Encerrar Combate");
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
                    var actRes = Tormenta20ActionRules.EnqueueTurnAction(
                        session, currentId, T20ActionType.Standard, "Agredir (Ataque Padrão)");
                    Console.WriteLine(actRes.Message);
                    if (actRes.Success)
                    {
                        Console.Write("Modificador do ataque/perícia (ex: +6, 0): ");
                        int mod = ParseSignedInt(GetCleanLine(), 0);
                        string expr = mod >= 0 ? $"1d20+{mod}" : $"1d20{mod}";
                        RollResult roll = session.Roll(expr);
                        Console.WriteLine($"Resultado do Ataque: {roll.Total} (Dado: {roll.DiceTotal}, Mod: {roll.Modifier})");
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
                    int pmCost;
                    string skName = "Poder/Magia";
                    T20ActionType reqActionType = T20ActionType.Standard;

                    if (charIdx >= 0 && _characters[charIdx].Skills.Count > 0)
                    {
                        List<CharacterSkill> skills = _characters[charIdx].Skills;
                        Console.WriteLine($"\nHabilidades de {currentName}:");
                        for (int i = 0; i < skills.Count; i++)
                        {
                            T20ActionType skT20Type = MapActionType(skills[i].Action);
                            Console.WriteLine($"  {i + 1}. {skills[i].Name} [{Tormenta20ActionRules.GetActionTypeName(skT20Type)} | {skills[i].PmCost} PM] - {skills[i].Description}");
                        }
                        Console.WriteLine($"  {skills.Count + 1}. Digitar habilidade/magia avulsa");
                        Console.Write("Escolha a habilidade: ");
                        int skChoice = ParsePositiveInt(GetCleanLine(), 0);

                        if (skChoice >= 1 && skChoice <= skills.Count)
                        {
                            CharacterSkill chosen = skills[skChoice - 1];
                            pmCost = chosen.PmCost;
                            skName = chosen.Name;
                            reqActionType = MapActionType(chosen.Action);
                        }
                        else
                        {
                            Console.Write("Tipo de Ação (1=Livre, 2=Movimento, 3=Padrão/Comum, 4=Completa): ");
                            int tChoice = ParsePositiveInt(GetCleanLine(), 3);
                            reqActionType = tChoice switch
                            {
                                1 => T20ActionType.Free,
                                2 => T20ActionType.Movement,
                                4 => T20ActionType.Full,
                                _ => T20ActionType.Standard
                            };
                            Console.Write("Custo em PM: ");
                            pmCost = Math.Max(0, ParseSignedInt(GetCleanLine(), 1));
                        }
                    }
                    else
                    {
                        Console.Write("Tipo de Ação (1=Livre, 2=Movimento, 3=Padrão/Comum, 4=Completa): ");
                        int tChoice = ParsePositiveInt(GetCleanLine(), 3);
                        reqActionType = tChoice switch
                        {
                            1 => T20ActionType.Free,
                            2 => T20ActionType.Movement,
                            4 => T20ActionType.Full,
                            _ => T20ActionType.Standard
                        };
                        Console.Write("Custo em PM do poder/magia: ");
                        pmCost = Math.Max(0, ParseSignedInt(GetCleanLine(), 1));
                    }

                    var res = Tormenta20ActionRules.EnqueueTurnAction(session, currentId, reqActionType, skName, 0, pmCost);
                    SyncBackToSheets();
                    Console.WriteLine(res.Message);
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
            }

            SyncBackToSheets();
        }

        public void Run()
        {
            PrintHeader();
            int option = -1;

            while (option != 0)
            {
                ShowMainMenu();
                string optStr = GetCleanLine();
                if (string.IsNullOrEmpty(optStr)) continue;

                if (!int.TryParse(optStr, out option))
                {
                    Console.WriteLine("\nOpção inválida. Digite um número do menu.");
                    option = -1;
                    continue;
                }

                switch (option)
                {
                    case 1:
                        HandleCreateCharacter();
                        break;
                    case 2:
                        HandleListCharacters();
                        break;
                    case 3:
                        HandleManageCharacter();
                        break;
                    case 4:
                        HandleDiceRoll();
                        break;
                    case 5:
                        HandleInitiativeCombat();
                        break;
                    case 0:
                        Console.WriteLine("\nEncerrando a sessão de Tormenta20 no OpenTTRPG. Glória a Arton!");
                        break;
                    default:
                        Console.WriteLine("\nOpção inválida. Tente novamente.");
                        break;
                }
            }
        }
    }
}

