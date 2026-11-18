using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

public partial class CombatManager : Node
{
	public const int HealthResourceId = Tormenta20ActionRules.HealthResourceId; // 0
	public const int ManaResourceId = Tormenta20ActionRules.ManaResourceId;     // 1

	private EncounterSession _session;
	public EncounterSession Session => _session;

	public List<CharacterSheetData> Characters { get; private set; } = new();
	public Dictionary<int, int> SheetIdToCombatantId { get; private set; } = new();
	public Dictionary<int, CharacterSheetData> CombatantIdToSheet { get; private set; } = new();

	public bool IsInCombat { get; private set; } = false;

	[Signal]
	public delegate void TurnChangedEventHandler(int combatantId, string combatantName, int round);

	[Signal]
	public delegate void CombatLogEventHandler(string message, string colorHex);

	[Signal]
	public delegate void CharactersLoadedEventHandler();

	static CombatManager()
	{
		NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (libraryName, assembly, searchPath) =>
		{
			if (libraryName == "openttrpg_core")
			{
				string resPath = ProjectSettings.GlobalizePath("res://libopenttrpg_core.so");
				if (File.Exists(resPath))
				{
					return NativeLibrary.Load(resPath);
				}

				string binPath = ProjectSettings.GlobalizePath("res://../../bin/libopenttrpg_core.so");
				if (File.Exists(binPath))
				{
					return NativeLibrary.Load(binPath);
				}

				string dllPath = ProjectSettings.GlobalizePath("res://openttrpg_core.dll");
				if (File.Exists(dllPath))
				{
					return NativeLibrary.Load(dllPath);
				}
			}
			return IntPtr.Zero;
		});
	}

	public override void _Ready()
	{
		GD.Print("[OpenTTRPG] Inicializando CombatManager...");
		InitializeSession();
		LoadCharacters();
	}

	public void InitializeSession()
	{
		try
		{
			_session?.Dispose();
			_session = new EncounterSession();
			Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(_session);
			IsInCombat = false;
			GD.Print("[OpenTTRPG] Sessão nativa e Árvores de Poderes de Tormenta20 inicializadas.");
		}
		catch (DllNotFoundException e)
		{
			GD.PushError($"[OpenTTRPG] Falha ao encontrar libopenttrpg_core.so: {e.Message}");
		}
		catch (Exception e)
		{
			GD.PushError($"[OpenTTRPG] Erro ao inicializar engine nativa: {e.Message}");
		}
	}

	public string GetCharactersSavePath()
	{
		string projectPath = ProjectSettings.GlobalizePath("res://../../saves/characters.json");
		if (File.Exists(projectPath))
		{
			return projectPath;
		}

		string localPath = ProjectSettings.GlobalizePath("res://characters.json");
		if (File.Exists(localPath))
		{
			return localPath;
		}

		return projectPath;
	}

	public void LoadCharacters()
	{
		try
		{
			string path = GetCharactersSavePath();
			if (File.Exists(path))
			{
				Characters = Tormenta20Persistence.LoadCharactersFromFile(path);
				GD.Print($"[OpenTTRPG] {Characters.Count} personagens carregados de: {path}");
			}
			else
			{
				GD.Print("[OpenTTRPG] Arquivo de fichas não encontrado. Criando personagens de demonstração...");
				SeedDefaultCharacters();
				SaveCharacters();
			}

			EmitSignal(SignalName.CharactersLoaded);
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao carregar fichas: {ex.Message}");
			if (Characters.Count == 0)
			{
				SeedDefaultCharacters();
				EmitSignal(SignalName.CharactersLoaded);
			}
		}
	}

	public void SaveCharacters()
	{
		try
		{
			string path = GetCharactersSavePath();
			Tormenta20Persistence.SaveCharactersToFile(Characters, path);
			GD.Print($"[OpenTTRPG] Fichas salvas com sucesso em: {path}");
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao salvar fichas: {ex.Message}");
		}
	}

	private void SeedDefaultCharacters()
	{
		Characters.Clear();

		var valeros = new CharacterSheetData(1, "Valeros de Valkaria", "Guerreiro", "Humano", "Soldado", "Valkaria", 1, 24, 4)
		{
			Strength = 3,
			Dexterity = 1,
			Constitution = 2,
			Intelligence = 0,
			Wisdom = 1,
			Charisma = 0,
			SpellKeyAttribute = AttributeType.Intelligence
		};
		valeros.TrainedSkills.Add("Luta");
		valeros.TrainedSkills.Add("Fortitude");
		valeros.TrainedSkills.Add("Atletismo");
		valeros.TrainedSkills.Add("Iniciativa");
		valeros.Inventory.Add(new CharacterItem("Espada Longa", "Espada versátil de aço", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 15, true, T20DamageType.Slashing));
		valeros.Inventory.Add(new CharacterItem("Cota de Malha", "Armadura pesada de metal", ItemType.Armor, 2, "", "20/x2", 5, -2, 150, true));
		valeros.Skills.Add(new CharacterSkill("Ataque Especial", "Gasta 1 PM para +4 no ataque ou dano", 1, T20ActionType.Free, 4));
		Characters.Add(valeros);

		var sillith = new CharacterSheetData(2, "Sillith", "Arcanista", "Qareen", "Mago Aprendiz", "Wynna", 1, 11, 10)
		{
			Strength = -1,
			Dexterity = 2,
			Constitution = 1,
			Intelligence = 4,
			Wisdom = 1,
			Charisma = 2,
			SpellKeyAttribute = AttributeType.Intelligence
		};
		sillith.TrainedSkills.Add("Misticismo");
		sillith.TrainedSkills.Add("Vontade");
		sillith.TrainedSkills.Add("Iniciativa");
		sillith.Inventory.Add(new CharacterItem("Varinha Arcana", "Foco arcano", ItemType.Weapon, 1, "1d4", "20/x2", 0, 0, 10, true, T20DamageType.Piercing));
		sillith.Skills.Add(new CharacterSkill("Mísseis Mágicos", "Gasta 1 PM para causar 2d4+2 de essência", 1, T20ActionType.Standard, 0, "", true, "2d4+2"));
		Characters.Add(sillith);
	}

	// ════════════════════════════════════════════════════════════════════════
	// Criar / Excluir personagens
	// ════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Cria um novo personagem, adiciona à lista e retorna a referência.
	/// O chamador deve chamar SaveCharacters() depois se quiser persistir imediatamente.
	/// </summary>
	public CharacterSheetData CreateCharacter(
		string name, string characterClass, string race,
		string origin, string deity, int level, int maxHp, int maxPm)
	{
		// Gera um ID único (maior ID existente + 1)
		int newId = Characters.Count > 0 ? Characters.Max(c => c.Id) + 1 : 1;

		var novo = new CharacterSheetData(newId, name, characterClass, race, origin, deity, level, maxHp, maxPm);
		Characters.Add(novo);

		GD.Print($"[OpenTTRPG] Personagem criado: {name} (id={newId})");
		return novo;
	}

	/// <summary>
	/// Remove o personagem com o Id fornecido da lista.
	/// Retorna true se encontrado e removido, false caso contrário.
	/// </summary>
	public bool DeleteCharacter(int id)
	{
		var target = Characters.FirstOrDefault(c => c.Id == id);
		if (target == null)
		{
			GD.PushWarning($"[OpenTTRPG] DeleteCharacter: id {id} não encontrado.");
			return false;
		}

		if (IsInCombat && SheetIdToCombatantId.TryGetValue(id, out int cid))
		{
			try
			{
				_session?.SetForcedOut(cid, true);
			}
			catch { }
			SheetIdToCombatantId.Remove(id);
			CombatantIdToSheet.Remove(cid);
		}

		Characters.Remove(target);
		GD.Print($"[OpenTTRPG] Personagem removido: {target.Name} (id={id})");
		return true;
	}

	public bool RollTest(string expression, out RollResult result, out string error)
	{
		result = default;
		error = string.Empty;

		if (_session == null)
		{
			error = "Sessão nativa não iniciada";
			return false;
		}

		try
		{
			result = _session.Roll(expression);
			LogCombat($"[Dados] Rolou {expression}: Total = {result.Total}", "#e0c068");
			return true;
		}
		catch (Exception e)
		{
			error = e.Message;
			LogCombat($"[Dados] Erro na rolagem '{expression}': {e.Message}", "#ff6b6b");
			return false;
		}
	}

	public void StartCombatWithAllCharacters()
	{
		if (Characters.Count == 0)
		{
			LoadCharacters();
		}

		if (Characters.Count == 0)
		{
			SeedDefaultCharacters();
		}

		// Sempre reinicia a sessão nativa do zero para liberar combatentes antigos e evitar IDs fantasmas
		InitializeSession();
		// Permite que personagens Morrendo (PV <= 0 mas > Limite de Morte) recebam seu turno para testar Constituição (CD 15);
		// personagens Mortos (PV <= Limite de Morte) são marcados com SetForcedOut(true).
		_session.SetAutoSkipDefeated(false);

		SheetIdToCombatantId.Clear();
		CombatantIdToSheet.Clear();

		List<int> combatantIds = new();

		foreach (var sheet in Characters)
		{
			Tormenta20SkillRules.TryCalculateSkillModifier(sheet, "Iniciativa", out int initMod, out _);
			int cid = _session.AddCombatant(sheet.Name, initMod, sheet.MaxHp, team: 1);

			// Configura PV com mínimo negativo igual ao Limite de Morte de Tormenta20 (-max(10, MaxHp/2))
			_session.AddResource(cid, HealthResourceId, "Vida", sheet.MaxHp, sheet.GetDeathThreshold());
			_session.SetResourceCurrent(cid, HealthResourceId, sheet.CurrentHp);
			_session.AddResource(cid, ManaResourceId, "PM", sheet.MaxPm, 0);
			_session.SetResourceCurrent(cid, ManaResourceId, sheet.CurrentPm);

			Tormenta20ActionRules.ConfigureTurnActions(_session, cid, standardActions: 1, movementActions: 1, freeActions: -1);
			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				_session, cid, sheet.Level,
				sheet.Strength, sheet.Dexterity, sheet.Constitution,
				sheet.Intelligence, sheet.Wisdom, sheet.Charisma,
				sheet.UnlockedTreeKeys);

			if (sheet.IsOverburdened())
			{
				Tormenta20ConditionRules.ApplyCondition(_session, cid, T20Condition.Sobrecarregado);
			}

			Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_session, cid, sheet);

			SheetIdToCombatantId[sheet.Id] = cid;
			CombatantIdToSheet[cid] = sheet;
			combatantIds.Add(cid);
		}

		_session.RollInitiativeForList(combatantIds.ToArray());
		_session.StartCombat();
		IsInCombat = true;

		LogCombat($"⚔️ Combate iniciado com {combatantIds.Count} participantes!", "#66d9ef");
		NotifyTurnChanged();
	}

	public void NextTurn()
	{
		if (!IsInCombat || _session == null) return;

		int currentCid = _session.CurrentTurn;

		// Drena ações pendentes na ordem da Fila de Prioridades (Livre -> Movimento -> Padrão -> Completa)
		var drained = Tormenta20ActionRules.DrainPriorityQueueInOrder(_session, currentCid);
		if (drained.Count > 0)
		{
			string actorName = CombatantIdToSheet.TryGetValue(currentCid, out var s) ? s.Name : $"ID {currentCid}";
			LogCombat($"📋 Fila de Prioridades executada ({actorName}): {string.Join(" → ", drained.Select(a => a.Name))}", "#c5e1a5");
		}

		_session.NextTurn();
		NotifyTurnChanged();
	}

	public void EndCombat()
	{
		if (_session != null && IsInCombat)
		{
			try
			{
				_session.EndCombat();
			}
			catch { }
		}

		IsInCombat = false;
		SheetIdToCombatantId.Clear();
		CombatantIdToSheet.Clear();

		LogCombat("🕊️ Combate encerrado.", "#99bbff");
		EmitSignal(SignalName.TurnChanged, 0, "Nenhum", 0);
	}

	private void NotifyTurnChanged()
	{
		if (_session == null) return;

		int cid = _session.CurrentTurn;
		string name = "Nenhum";
		CharacterSheetData currentSheet = null;
		if (CombatantIdToSheet.TryGetValue(cid, out var sheet))
		{
			name = sheet.Name;
			currentSheet = sheet;
		}

		LogCombat($"👉 Vez de [b]{name}[/b] (Rodada {_session.Round})", "#a6e22e");

		// Processa condições de início de turno (ex.: teste de Constituição contra Sangrando)
		if (cid > 0)
		{
			var condLogs = Tormenta20ConditionRules.ProcessStartOfTurnConditions(_session, cid, currentSheet);
			foreach (string log in condLogs)
			{
				LogCombat($"🩸 {log}", "#ff7b7b");
			}
		}

		EmitSignal(SignalName.TurnChanged, cid, name, _session.Round);
	}

	public void LogCombat(string message, string colorHex = "#ffffff")
	{
		GD.Print($"[Combate] {message}");
		EmitSignal(SignalName.CombatLog, message, colorHex);
	}

	public override void _ExitTree()
	{
		_session?.Dispose();
		GD.Print("[OpenTTRPG] Sessão nativa encerrada com sucesso.");
	}
}
