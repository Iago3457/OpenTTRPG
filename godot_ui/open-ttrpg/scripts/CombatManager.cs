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
	public List<CharacterSheetData> Enemies { get; private set; } = new();
	private readonly List<CharacterSheetData> _transientCombatSheets = new();
	public Dictionary<int, int> SheetIdToCombatantId { get; private set; } = new();
	public Dictionary<int, CharacterSheetData> CombatantIdToSheet { get; private set; } = new();

	public bool IsInCombat { get; private set; } = false;

	public const int MinMapSize = 6;
	public const int MaxMapSize = 120;

	public TacticalFogTracker FogTracker { get; } = new();
	public T20ArenaPreset CurrentArenaPreset { get; set; } = T20ArenaPreset.PillaredDungeon;
	public int DefaultMapWidth { get; set; } = 16;
	public int DefaultMapHeight { get; set; } = 12;
	public string CurrentMapBackgroundPath { get; set; } = string.Empty;
	public bool FogOfWarEnabled { get; set; } = false;

	[Signal]
	public delegate void TurnChangedEventHandler(int combatantId, string combatantName, int round);

	[Signal]
	public delegate void CombatLogEventHandler(string message, string colorHex);

	[Signal]
	public delegate void CharactersLoadedEventHandler();

	[Signal]
	public delegate void EnemiesLoadedEventHandler();

	static CombatManager()
	{
		NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (libraryName, assembly, searchPath) =>
		{
			if (libraryName == "openttrpg_core")
			{
				bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				string[] libFileNames = isWin
					? new[] { "openttrpg_core.dll", "libopenttrpg_core.dll" }
					: new[] { "libopenttrpg_core.so", "openttrpg_core.so" };

				var candidateDirs = new List<string>();

				// 1. Diretório do executável exportado (OpenTTRPG.x86_64 ou OpenTTRPG.exe)
				try
				{
					string exePath = OS.GetExecutablePath();
					if (!string.IsNullOrWhiteSpace(exePath))
					{
						string exeDir = Path.GetDirectoryName(exePath);
						if (!string.IsNullOrWhiteSpace(exeDir))
						{
							candidateDirs.Add(exeDir);
							candidateDirs.Add(Path.Combine(exeDir, "..", "..", "bin"));
						}
					}
				}
				catch { }

				// 2. Diretório dos assemblies .NET (data_OpenTTRPG_linuxbsd_x86_64 / data_OpenTTRPG_windows_x86_64) e seu diretório pai
				if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
				{
					candidateDirs.Add(AppContext.BaseDirectory);
					candidateDirs.Add(Path.Combine(AppContext.BaseDirectory, ".."));
					candidateDirs.Add(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "bin"));
				}

				// 3. Caminhos do projeto Godot (quando rodando no Editor Godot)
				try
				{
					candidateDirs.Add(ProjectSettings.GlobalizePath("res://"));
					candidateDirs.Add(ProjectSettings.GlobalizePath("res://../../bin"));
				}
				catch { }

				// 4. Diretório de trabalho atual (CWD)
				try
				{
					string cwd = Directory.GetCurrentDirectory();
					candidateDirs.Add(cwd);
					candidateDirs.Add(Path.Combine(cwd, "bin"));
				}
				catch { }

				foreach (string dir in candidateDirs)
				{
					if (string.IsNullOrWhiteSpace(dir)) continue;
					foreach (string fileName in libFileNames)
					{
						try
						{
							string fullPath = Path.GetFullPath(Path.Combine(dir, fileName));
							if (File.Exists(fullPath))
							{
								GD.Print($"[OpenTTRPG] Carregando biblioteca C++ nativa: {fullPath}");
								return NativeLibrary.Load(fullPath);
							}
						}
						catch { }
					}
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
		LoadEnemies();
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
			GD.PushError($"[OpenTTRPG] Falha ao encontrar biblioteca nativa (libopenttrpg_core.so / openttrpg_core.dll): {e.Message}");
		}
		catch (Exception e)
		{
			GD.PushError($"[OpenTTRPG] Erro ao inicializar engine nativa: {e.Message}");
		}
	}

	public string GetCharactersSavePath()
	{
		// 1. Quando rodando dentro do Editor Godot
		if (OS.HasFeature("editor"))
		{
			string editorProjectPath = Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../saves/characters.json"));
			if (File.Exists(editorProjectPath))
				return editorProjectPath;

			string editorLocalPath = Path.GetFullPath(ProjectSettings.GlobalizePath("res://characters.json"));
			if (File.Exists(editorLocalPath))
				return editorLocalPath;

			return editorProjectPath;
		}

		// 2. Quando rodando como Release Exportada (OpenTTRPG.x86_64 no Linux ou OpenTTRPG.exe no Windows)
		string exeDir = Path.GetDirectoryName(OS.GetExecutablePath());
		if (string.IsNullOrWhiteSpace(exeDir))
		{
			exeDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
		}

		// Prioriza a pasta saves/ ao lado do executável (portátil para distribuição em ZIP)
		string localSavesPath = Path.GetFullPath(Path.Combine(exeDir, "saves", "characters.json"));
		if (File.Exists(localSavesPath))
		{
			return localSavesPath;
		}

		// Se estiver rodando dentro de bin/release/linux-x64 ou bin/release/win-x64 no repositório
		string repoSavesPath = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "saves", "characters.json"));
		if (File.Exists(repoSavesPath))
		{
			return repoSavesPath;
		}

		return localSavesPath;
	}

	public string GetEnemiesSavePath()
	{
		string charDir = Path.GetDirectoryName(GetCharactersSavePath()) ?? ".";
		return Path.Combine(charDir, "enemies.json");
	}

	public int GetNextUniqueSheetId(bool isEnemy)
	{
		int minBase = isEnemy ? 1001 : 1;
		int maxUsed = 0;
		foreach (var c in Characters) if (c.Id > maxUsed) maxUsed = c.Id;
		foreach (var e in Enemies) if (e.Id > maxUsed) maxUsed = e.Id;
		foreach (var t in _transientCombatSheets) if (t.Id > maxUsed) maxUsed = t.Id;
		return Math.Max(minBase, maxUsed + 1);
	}

	public CharacterSheetData FindSheetById(int sheetId)
	{
		return Characters.FirstOrDefault(c => c.Id == sheetId)
			?? Enemies.FirstOrDefault(e => e.Id == sheetId)
			?? _transientCombatSheets.FirstOrDefault(t => t.Id == sheetId);
	}

	public void LoadCharacters()
	{
		try
		{
			string path = GetCharactersSavePath();
			if (File.Exists(path))
			{
				var loaded = Tormenta20Persistence.LoadCharactersFromFile(path);
				Characters = loaded.Where(c => !c.IsEnemy).ToList();
				foreach (var legacyEnemy in loaded.Where(c => c.IsEnemy))
				{
					if (!Enemies.Any(e => e.Id == legacyEnemy.Id))
						Enemies.Add(legacyEnemy);
				}
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

	public void LoadEnemies()
	{
		try
		{
			string path = GetEnemiesSavePath();
			if (File.Exists(path))
			{
				var legacyMigrated = Enemies.ToList();
				var loaded = Tormenta20Persistence.LoadCharactersFromFile(path);
				foreach (var e in loaded)
				{
					e.IsEnemy = true;
				}
				Enemies = loaded;
				foreach (var leg in legacyMigrated)
				{
					if (!Enemies.Any(e => e.Id == leg.Id))
						Enemies.Add(leg);
				}
				GD.Print($"[OpenTTRPG] {Enemies.Count} inimigos carregados de: {path}");
			}
			else
			{
				if (Enemies.Count == 0)
				{
					GD.Print("[OpenTTRPG] Arquivo de bestiário não encontrado. Criando ameaças iniciais...");
					SeedDefaultEnemies();
				}
				SaveEnemies();
			}

			EmitSignal(SignalName.EnemiesLoaded);
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao carregar inimigos: {ex.Message}");
			if (Enemies.Count == 0)
			{
				SeedDefaultEnemies();
				EmitSignal(SignalName.EnemiesLoaded);
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

	public void SaveEnemies()
	{
		try
		{
			string path = GetEnemiesSavePath();
			Tormenta20Persistence.SaveCharactersToFile(Enemies, path);
			GD.Print($"[OpenTTRPG] Bestiário salvo com sucesso em: {path}");
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao salvar inimigos: {ex.Message}");
		}
	}

	private void SeedDefaultEnemies()
	{
		Enemies.Clear();
		int baseId = Math.Max(1001, (Characters.Count > 0 ? Characters.Max(c => c.Id) + 1 : 1001));
		Enemies.Add(Tormenta20Bestiary.CreateFromPreset(baseId++, "goblin_salteador"));
		Enemies.Add(Tormenta20Bestiary.CreateFromPreset(baseId++, "esqueleto_guerreiro"));
		Enemies.Add(Tormenta20Bestiary.CreateFromPreset(baseId++, "orc_berserker"));
		Enemies.Add(Tormenta20Bestiary.CreateFromPreset(baseId++, "golem_ferro"));
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
		int newId = GetNextUniqueSheetId(isEnemy: false);

		var novo = new CharacterSheetData(newId, name, characterClass, race, origin, deity, level, maxHp, maxPm);
		Characters.Add(novo);

		GD.Print($"[OpenTTRPG] Personagem criado: {name} (id={newId})");
		return novo;
	}

	public CharacterSheetData CreateEnemyFromPreset(string presetKey, string customName = null)
	{
		int newId = GetNextUniqueSheetId(isEnemy: true);
		var enemy = Tormenta20Bestiary.CreateFromPreset(newId, presetKey, customName);
		Enemies.Add(enemy);
		GD.Print($"[OpenTTRPG] Inimigo criado do bestiário: {enemy.Name} (id={newId})");
		return enemy;
	}

	public CharacterSheetData CreateBlankEnemy(string name = "Nova Ameaça")
	{
		int newId = GetNextUniqueSheetId(isEnemy: true);
		var enemy = Tormenta20Bestiary.CreateBlankEnemy(newId, name);
		Enemies.Add(enemy);
		GD.Print($"[OpenTTRPG] Novo inimigo criado: {enemy.Name} (id={newId})");
		return enemy;
	}

	public CharacterSheetData DuplicateEnemy(CharacterSheetData source)
	{
		if (source == null) return CreateBlankEnemy();
		int newId = GetNextUniqueSheetId(isEnemy: true);
		var copy = source.CloneForCombat(newId, $"{source.Name} (Cópia)");
		copy.IsEnemy = true;
		Enemies.Add(copy);
		return copy;
	}

	private void RemoveCombatantFromActiveSession(int cid)
	{
		if (!IsInCombat || _session == null) return;
		bool wasCurrentTurn = _session.CurrentTurn == cid;
		try { _session.SetForcedOut(cid, true); } catch { }
		try { _session.RemoveFromInitiative(cid); } catch { }
		try { _session.RemoveToken(cid); } catch { }
		CombatantIdToSheet.Remove(cid);

		if (wasCurrentTurn)
		{
			if (_session.GetInitiativeOrder().Length == 0 || !AdvanceToNextValidTurn())
			{
				EndCombat();
				return;
			}
		}
		Tormenta20TacticalRules.UpdateFogOfWar(_session, FogTracker, alliedTeam: 1);
		if (wasCurrentTurn)
		{
			NotifyTurnChanged();
		}
	}

	public bool DeleteEnemy(int id)
	{
		var target = Enemies.FirstOrDefault(e => e.Id == id);
		if (target == null) return false;

		if (IsInCombat && SheetIdToCombatantId.TryGetValue(id, out int cid))
		{
			SheetIdToCombatantId.Remove(id);
			RemoveCombatantFromActiveSession(cid);
		}

		Enemies.Remove(target);
		GD.Print($"[OpenTTRPG] Inimigo removido: {target.Name} (id={id})");
		return true;
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
			SheetIdToCombatantId.Remove(id);
			RemoveCombatantFromActiveSession(cid);
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

		StartCombatWithSelectedCharacters(Characters);
	}

	public void StartCombatWithSelectedCharacters(
		IEnumerable<CharacterSheetData> selectedParticipants,
		Dictionary<int, int> customTeams = null)
	{
		var participants = selectedParticipants?.ToList() ?? new List<CharacterSheetData>();
		if (participants.Count == 0)
		{
			LogCombat("Selecione pelo menos 1 participante para iniciar o combate.", "#ff6b6b");
			return;
		}

		// Preserva terreno customizado caso o usuário já tenha editado o mapa antes de iniciar o combate
		bool hadActiveMap = _session != null && _session.IsMapActive;
		int prevW = hadActiveMap ? _session.MapWidth : DefaultMapWidth;
		int prevH = hadActiveMap ? _session.MapHeight : DefaultMapHeight;
		MapCellInfo[] prevCells = hadActiveMap ? _session.GetNonDefaultMapCells() : Array.Empty<MapCellInfo>();

		// Sempre reinicia a sessão nativa do zero para liberar combatentes antigos e evitar IDs fantasmas
		InitializeSession();
		if (hadActiveMap)
		{
			_session.ConfigureMap(prevW, prevH);
			foreach (var cell in prevCells)
			{
				_session.SetMapCell(cell.X, cell.Y, cell.TerrainType, cell.CoverLevel, cell.IsSightBlocked);
			}
		}
		else
		{
			Tormenta20TacticalRules.ApplyArenaPreset(_session, CurrentArenaPreset, DefaultMapWidth, DefaultMapHeight);
		}
		FogTracker.Clear();

		// Permite que personagens Morrendo (PV <= 0 mas > Limite de Morte) recebam seu turno para testar Constituição (CD 15);
		// personagens Mortos (PV <= Limite de Morte) são marcados com SetForcedOut(true).
		_session.SetAutoSkipDefeated(false);

		SheetIdToCombatantId.Clear();
		CombatantIdToSheet.Clear();
		_transientCombatSheets.Clear();

		List<int> combatantIds = new();

		foreach (var sheet in participants)
		{
			if (!Characters.Contains(sheet) && !Enemies.Contains(sheet) && !_transientCombatSheets.Contains(sheet))
			{
				_transientCombatSheets.Add(sheet);
			}

			int defaultTeam = sheet.IsEnemy ? 2 : 1;
			int teamId = (customTeams != null && customTeams.TryGetValue(sheet.Id, out int t)) ? t : defaultTeam;
			Tormenta20SkillRules.TryCalculateSkillModifier(sheet, "Iniciativa", out int initMod, out _);
			int cid = _session.AddCombatant(sheet.Name, initMod, sheet.MaxHp, team: teamId);

			// Configura PV com mínimo negativo igual ao Limite de Morte de Tormenta20 (-max(10, MaxHp/2)) ou 0 para ameaças
			_session.AddResource(cid, HealthResourceId, "Vida", sheet.MaxHp, sheet.GetDeathThreshold());
			_session.SetResourceCurrent(cid, HealthResourceId, sheet.CurrentHp);
			if (sheet.TempHp > 0)
				_session.SetResourceTemp(cid, HealthResourceId, sheet.TempHp);
			_session.AddResource(cid, ManaResourceId, "PM", sheet.MaxPm, 0);
			_session.SetResourceCurrent(cid, ManaResourceId, sheet.CurrentPm);
			if (sheet.DamageReduction > 0)
			{
				_session.SetGlobalDamageReduction(cid, sheet.DamageReduction);
			}

			Tormenta20ActionRules.ConfigureTurnActions(_session, cid, standardActions: 1, movementActions: 1, freeActions: -1);
			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				_session, cid, sheet.Level,
				sheet.Strength, sheet.Dexterity, sheet.Constitution,
				sheet.Intelligence, sheet.Wisdom, sheet.Charisma,
				sheet.UnlockedTreeKeys);

			if (sheet.IsOverburdened() && !sheet.IsEnemy)
			{
				Tormenta20ConditionRules.ApplyCondition(_session, cid, T20Condition.Sobrecarregado);
			}

			Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_session, cid, sheet);

			SheetIdToCombatantId[sheet.Id] = cid;
			CombatantIdToSheet[cid] = sheet;
			combatantIds.Add(cid);
		}

		// Rola a iniciativa APENAS para a lista de participantes selecionados (RollInitiativeForList)
		_session.RollInitiativeForList(combatantIds.ToArray(), 1, 20);
		foreach (int cid in combatantIds)
		{
			if (CombatantIdToSheet.TryGetValue(cid, out var sh))
			{
				sh.LastInitiativeRoll = _session.GetCombatantInfo(cid).Initiative;
			}
		}

		_session.StartCombat();
		IsInCombat = true;

		// Se o 1º sorteado já estiver morto/removido, avança para o primeiro combatente válido (vivo ou Morrendo)
		if (!IsCombatantEligibleForTurn(_session.CurrentTurn))
		{
			AdvanceToNextValidTurn();
		}

		EnsureMapInitialized(resetTerrain: false);

		LogCombat($"Combate iniciado com {combatantIds.Count} participante(s)!", "#66d9ef");
		NotifyTurnChanged();
	}

	public void EnsureMapInitialized(bool resetTerrain = false)
	{
		if (_session == null) return;

		if (!_session.IsMapActive || resetTerrain)
		{
			Tormenta20TacticalRules.ApplyArenaPreset(_session, CurrentArenaPreset, DefaultMapWidth, DefaultMapHeight);
			FogTracker.Clear();
		}

		if (IsInCombat)
		{
			int w = Math.Max(4, _session.MapWidth);
			int h = Math.Max(4, _session.MapHeight);
			var occupied = new HashSet<GridPos>();
			foreach (var tok in _session.GetAllTokens())
			{
				occupied.Add(new GridPos(tok.X, tok.Y));
			}

			int heroSlot = 0;
			int enemySlot = 0;
			foreach (int cid in _session.GetInitiativeOrder())
			{
				if (!resetTerrain && _session.TryGetToken(cid, out var existingTok))
				{
					if (_session.GetMapCell(existingTok.X, existingTok.Y).TerrainType != MapTerrainType.Blocked)
					{
						continue;
					}
					_session.RemoveToken(cid);
					occupied.Remove(new GridPos(existingTok.X, existingTok.Y));
				}

				int team = _session.GetCombatantInfo(cid).Team;
				bool isHeroTeam = team == 1;
				int startX = isHeroTeam ? 1 : Math.Max(2, w - 2);
				int dirX = isHeroTeam ? 1 : -1;
				int slot = isHeroTeam ? heroSlot++ : enemySlot++;
				int preferredY = Math.Clamp((h / 2) + ((slot % 2 == 0 ? 1 : -1) * ((slot + 1) / 2) * 2 - 1), 1, h - 2);

				GridPos? spawn = FindNearestFreeCell(startX, preferredY, dirX, w, h, occupied);
				if (spawn.HasValue)
				{
					_session.PlaceToken(cid, spawn.Value.X, spawn.Value.Y, 1);
					occupied.Add(spawn.Value);
				}
			}

			Tormenta20TacticalRules.UpdateFogOfWar(_session, FogTracker, alliedTeam: 1);
		}
	}

	private GridPos? FindNearestFreeCell(int baseX, int baseY, int dirX, int w, int h, HashSet<GridPos> occupied)
	{
		for (int dx = 0; dx < w; dx++)
		{
			int x = baseX + dx * dirX;
			if (x < 0 || x >= w) continue;

			for (int offsetY = 0; offsetY < h; offsetY++)
			{
				int[] candidatesY = offsetY == 0 ? new[] { baseY } : new[] { baseY + offsetY, baseY - offsetY };
				foreach (int y in candidatesY)
				{
					if (y < 0 || y >= h) continue;
					var p = new GridPos(x, y);
					if (occupied.Contains(p)) continue;
					if (_session.GetMapCell(x, y).TerrainType == MapTerrainType.Blocked) continue;
					return p;
				}
			}
		}
		return null;
	}

	private GridPos? FindNearestFreeCellAround(int targetX, int targetY, int w, int h, HashSet<GridPos> occupied)
	{
		int maxRadius = Math.Max(w, h);
		for (int radius = 0; radius <= maxRadius; radius++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
					int x = targetX + dx;
					int y = targetY + dy;
					if (x < 0 || x >= w || y < 0 || y >= h) continue;
					var p = new GridPos(x, y);
					if (occupied.Contains(p)) continue;
					if (_session.GetMapCell(x, y).TerrainType == MapTerrainType.Blocked) continue;
					return p;
				}
			}
		}
		return null;
	}

	public void ResizeMap(int newWidth, int newHeight, bool resetTerrain = false)
	{
		int w = Math.Clamp(newWidth, MinMapSize, MaxMapSize);
		int h = Math.Clamp(newHeight, MinMapSize, MaxMapSize);
		DefaultMapWidth = w;
		DefaultMapHeight = h;

		if (_session == null) return;

		if (!_session.IsMapActive || resetTerrain)
		{
			EnsureMapInitialized(resetTerrain: true);
			return;
		}

		var prevTokens = _session.GetAllTokens();
		_session.ResizeMap(w, h);

		var occupied = new HashSet<GridPos>();
		foreach (var tok in _session.GetAllTokens())
		{
			occupied.Add(new GridPos(tok.X, tok.Y));
		}

		foreach (var tok in prevTokens)
		{
			if (_session.TryGetToken(tok.EntityId, out _)) continue;
			int anchorX = Math.Clamp(tok.X, 0, w - 1);
			int anchorY = Math.Clamp(tok.Y, 0, h - 1);
			GridPos? spawn = FindNearestFreeCellAround(anchorX, anchorY, w, h, occupied);
			if (spawn.HasValue)
			{
				_session.PlaceToken(tok.EntityId, spawn.Value.X, spawn.Value.Y, Math.Max(1, tok.SizeCells));
				occupied.Add(spawn.Value);
			}
		}

		foreach (var exp in FogTracker.ExploredCells.Where(p => p.X < 0 || p.X >= w || p.Y < 0 || p.Y >= h).ToList())
		{
			FogTracker.ExploredCells.Remove(exp);
		}
		foreach (var vis in FogTracker.VisibleCells.Where(p => p.X < 0 || p.X >= w || p.Y < 0 || p.Y >= h).ToList())
		{
			FogTracker.VisibleCells.Remove(vis);
		}
		if (IsInCombat)
		{
			Tormenta20TacticalRules.UpdateFogOfWar(_session, FogTracker, alliedTeam: 1);
		}
	}

	public string GetCombatSnapshotPath()
	{
		string dir = Path.GetDirectoryName(GetCharactersSavePath()) ?? ".";
		return Path.Combine(dir, "encounter_snapshot.json");
	}

	public bool SaveCombatSnapshot() => SaveCombatSnapshotToPath(GetCombatSnapshotPath());

	public bool SaveCombatSnapshotToPath(string filePath)
	{
		if (!IsInCombat || _session == null) return false;

		try
		{
			SaveCharacters();
			SaveEnemies();
			var snap = new EncounterSnapshotDto
			{
				CombatStarted = true,
				Round = _session.Round,
				CurrentTurnSheetId = CombatantIdToSheet.TryGetValue(_session.CurrentTurn, out var curSheet) ? curSheet.Id : 0
			};

			foreach (int cid in _session.GetInitiativeOrder())
			{
				if (!CombatantIdToSheet.TryGetValue(cid, out var sh)) continue;
				CombatantInfo info = _session.GetCombatantInfo(cid);
				ResourceInfo hp = _session.GetResource(cid, HealthResourceId);
				ResourceInfo pm = _session.GetResource(cid, ManaResourceId);

				snap.Participants.Add(new CombatantSnapshotDto
				{
					SheetId = sh.Id,
					Name = sh.Name,
					TeamId = info.Team,
					Initiative = info.Initiative,
					MaxHp = sh.MaxHp,
					CurrentHp = hp.Current,
					TempHp = hp.Temp,
					MaxPm = sh.MaxPm,
					CurrentPm = pm.Current,
					IsBleedingOut = sh.IsBleedingOut,
					ActiveConditions = Tormenta20ConditionRules.GetActiveConditionNames(_session, cid),
					InlineSheet = Tormenta20Persistence.ToDto(sh)
				});
			}

			if (_session.IsMapActive)
			{
				var mapDto = new TacticalMapSnapshotDto
				{
					Width = _session.MapWidth,
					Height = _session.MapHeight,
					BackgroundImagePath = CurrentMapBackgroundPath ?? string.Empty,
					FogOfWarEnabled = FogOfWarEnabled
				};
				foreach (var cell in _session.GetNonDefaultMapCells())
				{
					mapDto.Cells.Add(new MapCellDto
					{
						X = cell.X,
						Y = cell.Y,
						Terrain = cell.Terrain,
						CoverLevel = cell.CoverLevel,
						BlocksSight = cell.IsSightBlocked
					});
				}
				foreach (var tok in _session.GetAllTokens())
				{
					if (CombatantIdToSheet.TryGetValue(tok.EntityId, out var sh))
					{
						mapDto.TokenPositions[sh.Id] = new GridPosDto { X = tok.X, Y = tok.Y };
					}
				}
				foreach (var exp in FogTracker.ExploredCells)
				{
					mapDto.ExploredCells.Add(new GridPosDto { X = exp.X, Y = exp.Y });
				}
				snap.TacticalMap = mapDto;
			}

			Tormenta20Persistence.SaveEncounterSnapshotToFile(snap, filePath);
			LogCombat($"Combate salvo (Rodada {snap.Round}).", "#55ff99");
			return true;
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao salvar combate: {ex.Message}");
			return false;
		}
	}

	public bool LoadCombatSnapshot() => LoadCombatSnapshotFromPath(GetCombatSnapshotPath());

	public bool LoadCombatSnapshotFromPath(string filePath)
	{
		try
		{
			var snap = Tormenta20Persistence.LoadEncounterSnapshotFromFile(filePath);
			if (snap == null || snap.Participants.Count == 0)
			{
				return false;
			}

			LoadCharacters();
			LoadEnemies();
			InitializeSession();
			_session.SetAutoSkipDefeated(false);
			SheetIdToCombatantId.Clear();
			CombatantIdToSheet.Clear();
			_transientCombatSheets.Clear();
			FogTracker.Clear();

			if (snap.TacticalMap != null)
			{
				int mapW = Math.Clamp(snap.TacticalMap.Width, MinMapSize, MaxMapSize);
				int mapH = Math.Clamp(snap.TacticalMap.Height, MinMapSize, MaxMapSize);
				DefaultMapWidth = mapW;
				DefaultMapHeight = mapH;
				_session.ConfigureMap(mapW, mapH);
				CurrentMapBackgroundPath = snap.TacticalMap.BackgroundImagePath ?? string.Empty;
				FogOfWarEnabled = snap.TacticalMap.FogOfWarEnabled;
				foreach (var cell in snap.TacticalMap.Cells)
				{
					_session.SetMapCell(cell.X, cell.Y, (MapTerrainType)cell.Terrain, cell.CoverLevel, cell.BlocksSight);
				}
				foreach (var exp in snap.TacticalMap.ExploredCells)
				{
					FogTracker.ExploredCells.Add(new GridPos(exp.X, exp.Y));
				}
			}

			int targetTurnCid = 0;
			foreach (var p in snap.Participants)
			{
				var sheet = FindSheetById(p.SheetId);
				if (sheet == null && p.InlineSheet != null)
				{
					sheet = Tormenta20Persistence.FromDto(p.InlineSheet);
					_transientCombatSheets.Add(sheet);
				}
				if (sheet == null) continue;

				sheet.CurrentHp = p.CurrentHp;
				sheet.TempHp = p.TempHp;
				sheet.CurrentPm = p.CurrentPm;
				sheet.IsBleedingOut = p.IsBleedingOut;
				sheet.LastInitiativeRoll = p.Initiative;

				Tormenta20SkillRules.TryCalculateSkillModifier(sheet, "Iniciativa", out int initMod, out _);
				int cid = _session.AddCombatant(sheet.Name, initMod, sheet.MaxHp, p.TeamId);

				_session.AddResource(cid, HealthResourceId, "Vida", sheet.MaxHp, sheet.GetDeathThreshold());
				_session.SetResourceCurrent(cid, HealthResourceId, sheet.CurrentHp);
				_session.SetResourceTemp(cid, HealthResourceId, sheet.TempHp);
				_session.AddResource(cid, ManaResourceId, "PM", sheet.MaxPm, 0);
				_session.SetResourceCurrent(cid, ManaResourceId, sheet.CurrentPm);
				if (sheet.DamageReduction > 0)
				{
					_session.SetGlobalDamageReduction(cid, sheet.DamageReduction);
				}

				Tormenta20ActionRules.ConfigureTurnActions(_session, cid, 1, 1, -1);
				Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
					_session, cid, sheet.Level,
					sheet.Strength, sheet.Dexterity, sheet.Constitution,
					sheet.Intelligence, sheet.Wisdom, sheet.Charisma,
					sheet.UnlockedTreeKeys);

				foreach (string condKey in p.ActiveConditions)
				{
					if (Enum.TryParse<T20Condition>(condKey, true, out var parsedCond))
						Tormenta20ConditionRules.ApplyCondition(_session, cid, parsedCond);
				}

				Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_session, cid, sheet);
				_session.SetInitiative(cid, p.Initiative);

				SheetIdToCombatantId[sheet.Id] = cid;
				CombatantIdToSheet[cid] = sheet;
				if (sheet.Id == snap.CurrentTurnSheetId)
					targetTurnCid = cid;

				if (snap.TacticalMap != null && snap.TacticalMap.TokenPositions.TryGetValue(sheet.Id, out var posDto))
				{
					if (_session.GetMapCell(posDto.X, posDto.Y).TerrainType != MapTerrainType.Blocked)
					{
						_session.PlaceToken(cid, posDto.X, posDto.Y, 1);
					}
				}
			}

			if (CombatantIdToSheet.Count == 0) return false;

			_session.StartCombat();
			IsInCombat = true;
			if (targetTurnCid > 0)
			{
				_session.RestoreCombatState(Math.Max(1, snap.Round), targetTurnCid);
			}
			else if (snap.Round > 1)
			{
				_session.RestoreCombatState(snap.Round, _session.CurrentTurn);
			}
			EnsureMapInitialized(resetTerrain: false);

			Tormenta20TacticalRules.UpdateFogOfWar(_session, FogTracker, alliedTeam: 1);
			LogCombat($"Combate restaurado (Rodada {_session.Round}, {CombatantIdToSheet.Count} participantes).", "#66d9ef");
			NotifyTurnChanged(processStartOfTurn: false);
			return true;
		}
		catch (Exception ex)
		{
			GD.PushError($"[OpenTTRPG] Erro ao carregar combate: {ex.Message}");
			return false;
		}
	}

	private bool IsCombatantEligibleForTurn(int cid)
	{
		if (cid <= 0 || !CombatantIdToSheet.TryGetValue(cid, out var sheet) || sheet == null)
			return false;
		// Heróis Morrendo (0 >= PV > DeathThreshold) ainda agem no turno para testar CON CD 15;
		// Mortos (ou inimigos <= 0 PV) não recebem turno.
		return sheet.CurrentHp > sheet.GetDeathThreshold();
	}

	private bool AdvanceToNextValidTurn()
	{
		if (_session == null) return false;
		int maxSteps = Math.Max(1, _session.GetInitiativeOrder().Length + 1);
		for (int i = 0; i < maxSteps; i++)
		{
			try
			{
				int nextCid = _session.NextTurn();
				if (IsCombatantEligibleForTurn(nextCid))
					return true;
			}
			catch
			{
				return false;
			}
		}
		return false;
	}

	public void NextTurn()
	{
		if (!IsInCombat || _session == null) return;

		int currentCid = _session.CurrentTurn;

		// Drena ações pendentes no encerramento do turno
		var drained = Tormenta20ActionRules.DrainPriorityQueueInOrder(_session, currentCid);
		if (drained.Count > 0)
		{
			string actorName = CombatantIdToSheet.TryGetValue(currentCid, out var s) ? s.Name : $"ID {currentCid}";
			LogCombat($"Ações concluídas ({actorName}): {string.Join(" -> ", drained.Select(a => a.Name))}", "#c5e1a5");
		}

		if (!AdvanceToNextValidTurn())
		{
			LogCombat("Nenhum combatente ativo ou morrendo restante para agir.", "#ffb74d");
			EndCombat();
			return;
		}
		Tormenta20TacticalRules.UpdateFogOfWar(_session, FogTracker, alliedTeam: 1);
		NotifyTurnChanged(processStartOfTurn: true);
	}

	public void EndCombat()
	{
		if (_session != null)
		{
			try
			{
				foreach (var tok in _session.GetAllTokens())
				{
					_session.RemoveToken(tok.EntityId);
				}
			}
			catch { }
			if (IsInCombat)
			{
				try
				{
					_session.EndCombat();
				}
				catch { }
			}
		}

		FogTracker.Clear();
		IsInCombat = false;
		SheetIdToCombatantId.Clear();
		CombatantIdToSheet.Clear();
		_transientCombatSheets.Clear();

		LogCombat("Combate encerrado.", "#99bbff");
		EmitSignal(SignalName.TurnChanged, 0, "Nenhum", 0);
	}

	private void NotifyTurnChanged(bool processStartOfTurn = true)
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

		LogCombat($"Vez de [b]{name}[/b] (Rodada {_session.Round})", "#a6e22e");

		// Processa condições de início de turno (ex.: teste de Constituição contra Sangrando)
		if (processStartOfTurn && cid > 0)
		{
			var condLogs = Tormenta20ConditionRules.ProcessStartOfTurnConditions(_session, cid, currentSheet);
			foreach (string log in condLogs)
			{
				LogCombat(log, "#ff7b7b");
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
