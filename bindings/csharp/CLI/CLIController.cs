using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTTRPG.Tormenta20;

namespace OpenTTRPG.CLI
{
    /// <summary>
    /// Controlador de linha de comando (CLI) oficial em C# consumindo a DLL nativa (OpenTTRPG.Native.EncounterSession)
    /// e o pacote modular de regras de Tormenta20 Jogo do Ano (OpenTTRPG.Tormenta20).
    /// Dividido em partial classes (CLIController.cs, CLIController.SheetMenu.cs, CLIController.CombatMenu.cs).
    /// </summary>
    public sealed partial class CLIController
    {
        private const int HealthResourceId = Tormenta20ActionRules.HealthResourceId;
        private const int ManaResourceId = Tormenta20ActionRules.ManaResourceId;

        private readonly List<CharacterSheetData> _characters = new List<CharacterSheetData>();
        private int _nextCharacterId = 1;

        public IReadOnlyList<CharacterSheetData> Characters => _characters;

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
                Charisma = 0,
                SpellKeyAttribute = AttributeType.Intelligence
            };
            warrior.TrainedSkills.Add("Luta");
            warrior.TrainedSkills.Add("Fortitude");
            warrior.TrainedSkills.Add("Atletismo");
            warrior.TrainedSkills.Add("Iniciativa");

            warrior.Inventory.Add(new CharacterItem("Espada Longa", "Espada versátil de aço", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 15, true, T20DamageType.Slashing, false));
            warrior.Inventory.Add(new CharacterItem("Cota de Malha", "Armadura pesada de metal", ItemType.Armor, 2, "", "20/x2", 5, -2, 150, true));
            warrior.Inventory.Add(new CharacterItem("Escudo Leve", "Escudo de madeira reforçada", ItemType.Shield, 1, "", "20/x2", 1, -1, 5, true));
            warrior.Inventory.Add(new CharacterItem("Bálsamo Restaurador", "Cura 2d4 PV", ItemType.Consumable, 1, "", "20/x2", 0, 0, 10, false));

            warrior.Skills.Add(new CharacterSkill("Ataque Especial", "Gasta 1 PM (+aprimoramentos) para receber +4 no ataque ou dano", 1, T20ActionType.Free, 4));
            warrior.Skills.Add(new CharacterSkill("Avanço Tático", "Desloca-se pelo campo de batalha buscando flanco", 0, T20ActionType.Movement, 0));
            warrior.Skills.Add(new CharacterSkill("Golpe Pessoal", "Ataque devastador personalizado de classe", 2, T20ActionType.Standard, 0));
            _characters.Add(warrior);

            // 2. Sillith - Arcanista Qareen
            var mage = new CharacterSheetData(_nextCharacterId++, "Sillith", "Arcanista", "Qareen", "Mago Aprendiz", "Wynna", 1, 11, 10)
            {
                Strength = -1,
                Dexterity = 2,
                Constitution = 1,
                Intelligence = 4,
                Wisdom = 1,
                Charisma = 3,
                SpellKeyAttribute = AttributeType.Intelligence
            };
            mage.TrainedSkills.Add("Misticismo");
            mage.TrainedSkills.Add("Vontade");
            mage.TrainedSkills.Add("Iniciativa");
            mage.TrainedSkills.Add("Conhecimento");

            mage.Inventory.Add(new CharacterItem("Varinha Arcana", "Foco arcano para canalização", ItemType.Weapon, 1, "1d4", "20/x2", 0, 0, 20, true, T20DamageType.Bludgeoning, false));
            mage.Inventory.Add(new CharacterItem("Poção de Mana", "Recupera 2d4 PM", ItemType.Consumable, 1, "", "20/x2", 0, 0, 30, false));

            mage.Skills.Add(new CharacterSkill("Passo Etéreo", "Reposicionamento rápido arcano (Movimento)", 1, T20ActionType.Movement, 0, "", true));
            var magicMissiles = new CharacterSkill("Mísseis Mágicos", "Projéteis de essência: 2d4+2 de dano sem teste de resistência", 1, T20ActionType.Standard, 0, "", true, "2d4+2");
            magicMissiles.Enhancements.Add(new SpellEnhancement(1, "+1 seta de essência (+1d4+1)", "1d4+1"));
            magicMissiles.Enhancements.Add(new SpellEnhancement(2, "+2 setas de essência (+2d4+2)", "2d4+2"));
            mage.Skills.Add(magicMissiles);
            mage.Skills.Add(new CharacterSkill("Curar Ferimentos", "Recupera 2d8+2 PV de um aliado tocado", 1, T20ActionType.Standard, 0, "", true, "2d8+2"));
            _characters.Add(mage);
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
                return Math.Max(Math.Min(0, defaultValue), val);
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
            Console.WriteLine("  1. Criar Personagem (Ficha Completa de Tormenta20)");
            Console.WriteLine("  2. Listar e Visualizar Fichas (PV, PM, Defesa, Carga, Perícias)");
            Console.WriteLine("  3. Gerenciar Ficha (Itens, Árvore de Poderes, Perícias, Descanso)");
            Console.WriteLine("  4. Rolar Dados ou Testar Perícia (via Core Nativo C++)");
            Console.WriteLine("  5. Combate Completo (Iniciativa, Ataque vs Defesa, Condições, Fila de Ações)");
            Console.WriteLine("  0. Sair");
            Console.Write("Escolha uma opção: ");
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

