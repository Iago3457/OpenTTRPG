using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

/// <summary>
/// Tabuleiro 2D interativo em grade quadrada para combate tático no Godot.
/// Suporta zoom, pan, imagem de fundo, terrenos, tokens animados, régua de alcance/visão e Névoa de Guerra.
/// </summary>
public partial class TacticalGridCanvas : Control
{
	private const float BaseCellSize = 48f;
	private const float MarginHeader = 32f;
	private const float MinZoom = 0.14f;
	private const float MaxZoom = 2.60f;

	public event Action<GridPos> CellClicked;
	public event Action<GridPos> CellDragged;
	public event Action<int> TokenClicked;
	public event Action<string> HoverStatusChanged;
	public event Action<float> ZoomChanged;

	public int GridWidth { get; private set; } = 16;
	public int GridHeight { get; private set; } = 12;

	public bool IsEditorMode { get; set; }
	public bool RulerMode { get; set; }
	public bool FogOfWarEnabled { get; set; }
	public float TargetZoom => _targetZoom;

	private float _zoom = 1.0f;
	private float _targetZoom = 1.0f;
	private Vector2 _panOffset = new Vector2(18f, 18f);
	private Vector2 _targetPanOffset = new Vector2(18f, 18f);
	private bool _hasCenteredOnce;
	private int _lastCenteredW = -1;
	private int _lastCenteredH = -1;

	private bool _isPanning;
	private bool _isLeftPanning;
	private Vector2 _lastPanMouse;
	private bool _isPaintingDrag;
	private GridPos? _lastDraggedCell;

	private bool _cachedRayValid;
	private GridPos _cachedRayOrigin;
	private GridPos _cachedRayDest;
	private bool _cachedRayHasLoS;
	private T20CoverLevel _cachedRayCover;
	private int _cachedRayDistSq;

	private Texture2D _bgTexture;
	private string _bgImagePath = string.Empty;

	private readonly Dictionary<GridPos, MapCellInfo> _cells = new();
	private readonly List<TokenRenderItem> _tokens = new();
	private readonly Dictionary<int, Vector2> _visualPositions = new();
	private readonly Dictionary<GridPos, int> _reachableCells = new();
	private TacticalFogTracker _fogTracker;

	private EncounterSession _sessionRef;
	private int _currentTurnId;
	private int _selectedTargetId;
	private int _selectedMoveTokenId;
	private int _maxWeaponRangeSquares = 1;

	private GridPos? _hoverCell;
	private List<GridPos> _hoverPath = new();
	private int _hoverPathCost = -1;
	private Tween _moveTween;

	private sealed class TokenRenderItem
	{
		public int EntityId;
		public GridPos GridPos;
		public int Team;
		public string Name = string.Empty;
		public string Initials = string.Empty;
		public int CurrentHp;
		public int MaxHp;
		public bool IsBleedingOut;
		public bool IsDead;
	}

	public override void _Ready()
	{
		ClipContents = true;
		MouseFilter = MouseFilterEnum.Stop;
		FocusMode = FocusModeEnum.Click;
		SetProcess(true);
		Resized += OnCanvasResized;
	}

	private void OnCanvasResized()
	{
		_targetPanOffset = ClampPanOffset(_targetPanOffset, _targetZoom);
		_panOffset = ClampPanOffset(_panOffset, _zoom);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (dt <= 0f) return;

		bool changed = false;

		// Navegação suave via teclado (WASD / Setas) quando o mapa está visível e nenhum campo de texto está focado
		if (IsVisibleInTree() && !IsTextInputFocused())
		{
			Vector2 keyDir = Vector2.Zero;
			if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) keyDir.Y += 1f;
			if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) keyDir.Y -= 1f;
			if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) keyDir.X += 1f;
			if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) keyDir.X -= 1f;

			if (keyDir != Vector2.Zero)
			{
				float speed = Input.IsKeyPressed(Key.Shift) ? 880f : 520f;
				_targetPanOffset = ClampPanOffset(_targetPanOffset + keyDir.Normalized() * speed * dt, _targetZoom);
				changed = true;
			}
		}

		// Interpolação exponencial única mantém o pivô de zoom fixo na tela sem saltos
		float alpha = 1f - Mathf.Exp(-18f * dt);
		bool zoomDiff = Mathf.Abs(_targetZoom - _zoom) > 0.0008f;
		bool panDiff = _targetPanOffset.DistanceSquaredTo(_panOffset) > 0.08f;

		if (zoomDiff || panDiff)
		{
			if (zoomDiff)
				_zoom = Mathf.Lerp(_zoom, _targetZoom, alpha);
			else
				_zoom = _targetZoom;

			if (panDiff)
				_panOffset = _panOffset.Lerp(_targetPanOffset, alpha);
			else
				_panOffset = _targetPanOffset;

			changed = true;
		}

		if (changed)
		{
			QueueRedraw();
		}
	}

	private bool IsTextInputFocused()
	{
		Control focus = GetViewport()?.GuiGetFocusOwner();
		return focus is LineEdit or TextEdit or SpinBox;
	}

	private Vector2 ClampPanOffset(Vector2 offset, float zoom)
	{
		if (Size.X < 40f || Size.Y < 40f) return offset;

		float totalW = (GridWidth * BaseCellSize + MarginHeader * 2f) * zoom;
		float totalH = (GridHeight * BaseCellSize + MarginHeader * 2f) * zoom;

		// Mantém sempre pelo menos 30% do tabuleiro ou 110px visível na área do canvas
		float keepVisibleX = Mathf.Min(totalW * 0.45f, Mathf.Max(110f, Size.X * 0.25f));
		float keepVisibleY = Mathf.Min(totalH * 0.45f, Mathf.Max(110f, Size.Y * 0.25f));

		float minX = keepVisibleX - totalW;
		float maxX = Size.X - keepVisibleX;
		float minY = keepVisibleY - totalH;
		float maxY = Size.Y - keepVisibleY;

		if (minX > maxX)
		{
			float midX = (minX + maxX) * 0.5f;
			minX = midX - 80f;
			maxX = midX + 80f;
		}
		if (minY > maxY)
		{
			float midY = (minY + maxY) * 0.5f;
			minY = midY - 80f;
			maxY = midY + 80f;
		}

		return new Vector2(
			Mathf.Clamp(offset.X, minX, maxX),
			Mathf.Clamp(offset.Y, minY, maxY));
	}

	public void SetBackgroundImage(string path)
	{
		string clean = path?.Trim() ?? string.Empty;
		if (clean == _bgImagePath) return;
		_bgImagePath = clean;
		_bgTexture = null;

		if (!string.IsNullOrEmpty(clean) && File.Exists(clean))
		{
			try
			{
				var img = Image.LoadFromFile(clean);
				if (img != null && !img.IsEmpty())
				{
					_bgTexture = ImageTexture.CreateFromImage(img);
				}
			}
			catch { }
		}
		QueueRedraw();
	}

	public void EnsureInitialCenter()
	{
		if (!_hasCenteredOnce || _lastCenteredW != GridWidth || _lastCenteredH != GridHeight)
		{
			CenterView(animate: false);
		}
	}

	public void CenterView(bool animate = true)
	{
		float contentW = GridWidth * BaseCellSize + MarginHeader * 2f;
		float contentH = GridHeight * BaseCellSize + MarginHeader * 2f;
		float desiredZoom;
		Vector2 desiredPan;

		if (Size.X > 40f && Size.Y > 40f && contentW > 0f && contentH > 0f)
		{
			float fitX = (Size.X - 28f) / contentW;
			float fitY = (Size.Y - 28f) / contentH;
			desiredZoom = Mathf.Clamp(Mathf.Min(fitX, fitY), MinZoom, 1.35f);
			float drawnW = contentW * desiredZoom;
			float drawnH = contentH * desiredZoom;
			desiredPan = new Vector2(
				(Size.X - drawnW) * 0.5f,
				(Size.Y - drawnH) * 0.5f);
			_hasCenteredOnce = true;
			_lastCenteredW = GridWidth;
			_lastCenteredH = GridHeight;
		}
		else
		{
			desiredZoom = 1.0f;
			desiredPan = new Vector2(18f, 18f);
		}

		_targetZoom = desiredZoom;
		_targetPanOffset = ClampPanOffset(desiredPan, desiredZoom);
		if (!animate)
		{
			_zoom = _targetZoom;
			_panOffset = _targetPanOffset;
		}
		ZoomChanged?.Invoke(_targetZoom);
		QueueRedraw();
	}

	public void ZoomBy(float factor)
	{
		Vector2 pivot = Size * 0.5f;
		if (pivot.X < 10f || pivot.Y < 10f) pivot = Vector2.Zero;
		ApplyZoom(factor, pivot, immediate: false);
	}

	public void ResetZoom100()
	{
		if (_targetZoom <= 0.001f) return;
		Vector2 pivot = Size * 0.5f;
		if (pivot.X < 10f || pivot.Y < 10f) pivot = Vector2.Zero;
		ApplyZoom(1.0f / _targetZoom, pivot, immediate: false);
	}

	public void SetSelectedEditorToken(int entityId)
	{
		_selectedMoveTokenId = entityId;
		QueueRedraw();
	}

	public void SyncFromSession(
		EncounterSession session,
		Dictionary<int, CharacterSheetData> combatantToSheet,
		int currentTurnId,
		int selectedTargetId,
		TacticalFogTracker fogTracker,
		bool fogEnabled,
		Dictionary<GridPos, int> reachableCells,
		int maxWeaponRangeSquares)
	{
		_sessionRef = session;
		_currentTurnId = currentTurnId;
		_selectedTargetId = selectedTargetId;
		_fogTracker = fogTracker;
		FogOfWarEnabled = fogEnabled;
		_maxWeaponRangeSquares = Math.Max(1, maxWeaponRangeSquares);

		_cells.Clear();
		_tokens.Clear();
		_reachableCells.Clear();
		_hoverPath.Clear();
		_hoverPathCost = -1;
		_cachedRayValid = false;

		if (reachableCells != null)
		{
			foreach (var kv in reachableCells)
				_reachableCells[kv.Key] = kv.Value;
		}

		if (session != null && session.IsMapActive)
		{
			GridWidth = Math.Max(1, session.MapWidth);
			GridHeight = Math.Max(1, session.MapHeight);

			foreach (var cell in session.GetNonDefaultMapCells())
			{
				_cells[new GridPos(cell.X, cell.Y)] = cell;
			}

			var aliveIds = new HashSet<int>();
			foreach (var tok in session.GetAllTokens())
			{
				aliveIds.Add(tok.EntityId);
				CharacterSheetData sheet = null;
				combatantToSheet?.TryGetValue(tok.EntityId, out sheet);

				int team = 1;
				int curHp = sheet?.CurrentHp ?? 10;
				int maxHp = sheet?.MaxHp ?? 10;
				try
				{
					team = session.GetCombatantInfo(tok.EntityId).Team;
					var hpRes = session.GetResource(tok.EntityId, CombatManager.HealthResourceId);
					curHp = hpRes.Current;
					maxHp = Math.Max(1, hpRes.Max);
				}
				catch { }

				string name = sheet?.Name ?? $"#{tok.EntityId}";
				bool isDead = sheet != null
					? curHp <= sheet.GetDeathThreshold()
					: curHp <= -10;
				bool isBleeding = sheet != null ? sheet.IsBleedingOut && !isDead : curHp <= 0 && !isDead;

				var pos = new GridPos(tok.X, tok.Y);
				if (!_visualPositions.ContainsKey(tok.EntityId))
				{
					_visualPositions[tok.EntityId] = new Vector2(pos.X, pos.Y);
				}
				else if (_moveTween == null || !_moveTween.IsRunning())
				{
					_visualPositions[tok.EntityId] = new Vector2(pos.X, pos.Y);
				}

				_tokens.Add(new TokenRenderItem
				{
					EntityId = tok.EntityId,
					GridPos = pos,
					Team = team,
					Name = name,
					Initials = BuildInitials(name),
					CurrentHp = curHp,
					MaxHp = maxHp,
					IsBleedingOut = isBleeding,
					IsDead = isDead
				});
			}

			foreach (int oldId in _visualPositions.Keys.Where(id => !aliveIds.Contains(id)).ToList())
			{
				_visualPositions.Remove(oldId);
			}
		}

		QueueRedraw();
	}

	public void AnimateTokenMove(int entityId, List<GridPos> path)
	{
		if (path == null || path.Count == 0) return;

		_moveTween?.Kill();
		_moveTween = CreateTween();
		_visualPositions[entityId] = new Vector2(path[0].X, path[0].Y);

		float stepDuration = Mathf.Clamp(0.45f / Math.Max(1, path.Count), 0.055f, 0.14f);
		for (int i = 1; i < path.Count; i++)
		{
			Vector2 nextCoord = new Vector2(path[i].X, path[i].Y);
			_moveTween.TweenMethod(
				Callable.From<Vector2>(v =>
				{
					_visualPositions[entityId] = v;
					QueueRedraw();
				}),
				new Vector2(path[i - 1].X, path[i - 1].Y),
				nextCoord,
				stepDuration);
		}
	}

	private static string BuildInitials(string name)
	{
		if (string.IsNullOrWhiteSpace(name)) return "?";
		var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 1)
			return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
		return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
	}

	private float CellPx => BaseCellSize * _zoom;

	private Vector2 GridOriginScreen => _panOffset + new Vector2(MarginHeader * _zoom, MarginHeader * _zoom);

	private GridPos? ScreenToCell(Vector2 localMouse)
	{
		Vector2 rel = localMouse - GridOriginScreen;
		float cell = CellPx;
		if (rel.X < 0 || rel.Y < 0) return null;
		int gx = Mathf.FloorToInt(rel.X / cell);
		int gy = Mathf.FloorToInt(rel.Y / cell);
		if (gx < 0 || gx >= GridWidth || gy < 0 || gy >= GridHeight) return null;
		return new GridPos(gx, gy);
	}

	private Vector2 CellCenterScreen(Vector2 gridCoord)
	{
		float cell = CellPx;
		return GridOriginScreen + new Vector2((gridCoord.X + 0.5f) * cell, (gridCoord.Y + 0.5f) * cell);
	}

	private Rect2 CellRectScreen(int gx, int gy)
	{
		float cell = CellPx;
		Vector2 topLeft = GridOriginScreen + new Vector2(gx * cell, gy * cell);
		return new Rect2(topLeft, new Vector2(cell, cell));
	}

	private TokenRenderItem FindVisibleTokenAt(GridPos cell)
	{
		foreach (var tok in _tokens)
		{
			if (tok.GridPos != cell) continue;
			if (!IsTokenVisibleUnderFog(tok)) continue;
			return tok;
		}
		return null;
	}

	private bool IsTokenVisibleUnderFog(TokenRenderItem tok)
	{
		if (!FogOfWarEnabled || IsEditorMode || _fogTracker == null) return true;
		if (tok.Team == 1) return true;
		return _fogTracker.GetState(tok.GridPos) == FogState.Visible;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMagnifyGesture mg)
		{
			ApplyZoom(mg.Factor, mg.Position, immediate: true);
			AcceptEvent();
			return;
		}
		if (@event is InputEventPanGesture pg)
		{
			Vector2 shifted = ClampPanOffset(_targetPanOffset - pg.Delta * 18f, _targetZoom);
			_targetPanOffset = shifted;
			_panOffset = shifted;
			QueueRedraw();
			AcceptEvent();
			return;
		}

		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
			{
				ApplyZoom(1.14f, mb.Position, immediate: false);
				AcceptEvent();
				return;
			}
			if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
			{
				ApplyZoom(1.0f / 1.14f, mb.Position, immediate: false);
				AcceptEvent();
				return;
			}

			if (mb.ButtonIndex == MouseButton.Right || mb.ButtonIndex == MouseButton.Middle)
			{
				_isPanning = mb.Pressed;
				_isLeftPanning = false;
				_lastPanMouse = mb.Position;
				MouseDefaultCursorShape = _isPanning ? CursorShape.Drag : CursorShape.Arrow;
				AcceptEvent();
				return;
			}

			if (mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed)
				{
					GrabFocus();
					bool spacePan = Input.IsKeyPressed(Key.Space);
					var cellOpt = ScreenToCell(mb.Position);
					if (spacePan || !cellOpt.HasValue)
					{
						_isPanning = true;
						_isLeftPanning = true;
						_lastPanMouse = mb.Position;
						MouseDefaultCursorShape = CursorShape.Drag;
					}
					else
					{
						var cell = cellOpt.Value;
						_isPaintingDrag = IsEditorMode;
						_lastDraggedCell = cell;

						var tok = FindVisibleTokenAt(cell);
						if (tok != null && !IsEditorMode && !RulerMode)
						{
							TokenClicked?.Invoke(tok.EntityId);
						}
						else if (tok != null && IsEditorMode)
						{
							TokenClicked?.Invoke(tok.EntityId);
							CellClicked?.Invoke(cell);
						}
						else
						{
							CellClicked?.Invoke(cell);
						}
					}
					AcceptEvent();
				}
				else
				{
					if (_isLeftPanning)
					{
						_isPanning = false;
						_isLeftPanning = false;
						MouseDefaultCursorShape = CursorShape.Arrow;
					}
					_isPaintingDrag = false;
					_lastDraggedCell = null;
				}
			}
		}
		else if (@event is InputEventMouseMotion mm)
		{
			if (_isPanning)
			{
				Vector2 delta = mm.Position - _lastPanMouse;
				_lastPanMouse = mm.Position;
				Vector2 clamped = ClampPanOffset(_targetPanOffset + delta, _targetZoom);
				_targetPanOffset = clamped;
				_panOffset = ClampPanOffset(_panOffset + delta, _zoom);
				QueueRedraw();
				AcceptEvent();
				return;
			}

			var cellOpt = ScreenToCell(mm.Position);
			if (cellOpt != _hoverCell)
			{
				_hoverCell = cellOpt;
				UpdateHoverPreview();

				if (_isPaintingDrag && IsEditorMode && cellOpt.HasValue && cellOpt != _lastDraggedCell)
				{
					_lastDraggedCell = cellOpt;
					CellDragged?.Invoke(cellOpt.Value);
				}
				QueueRedraw();
			}
		}
	}

	private void ApplyZoom(float factor, Vector2 pivotLocal, bool immediate)
	{
		float oldTarget = _targetZoom;
		float newTarget = Mathf.Clamp(oldTarget * factor, MinZoom, MaxZoom);
		if (Mathf.IsEqualApprox(oldTarget, newTarget)) return;

		Vector2 worldBefore = (pivotLocal - _targetPanOffset) / oldTarget;
		_targetZoom = newTarget;
		_targetPanOffset = ClampPanOffset(pivotLocal - worldBefore * _targetZoom, _targetZoom);

		if (immediate)
		{
			_zoom = _targetZoom;
			_panOffset = _targetPanOffset;
		}
		ZoomChanged?.Invoke(_targetZoom);
		QueueRedraw();
	}

	private static string FormatColumnLabel(int colIndex)
	{
		if (colIndex < 0) return "?";
		string label = string.Empty;
		int idx = colIndex;
		do
		{
			label = (char)('A' + (idx % 26)) + label;
			idx = (idx / 26) - 1;
		} while (idx >= 0);
		return label;
	}

	private void UpdateHoverPreview()
	{
		_hoverPath.Clear();
		_hoverPathCost = -1;
		_cachedRayValid = false;

		if (!_hoverCell.HasValue)
		{
			HoverStatusChanged?.Invoke("Escala: 1q = 1,5m | Pan: Direito/Meio/Espaço+Esquerdo/WASD | Scroll/+/-: Zoom");
			return;
		}

		GridPos cell = _hoverCell.Value;
		string colLetter = FormatColumnLabel(cell.X);
		string coordLabel = $"{colLetter}{cell.Y + 1}";

		string terrainName = "Chão Livre";
		if (_cells.TryGetValue(cell, out var cInfo))
		{
			if (cInfo.TerrainType == MapTerrainType.Blocked) terrainName = "Parede / Obstáculo";
			else if (cInfo.CoverLevel == 1 && cInfo.TerrainType == MapTerrainType.Difficult) terrainName = "Mureta (Terreno Difícil + Cobertura Leve)";
			else if (cInfo.CoverLevel == 1) terrainName = "Cobertura Leve (+5 Def)";
			else if (cInfo.TerrainType == MapTerrainType.Difficult) terrainName = "Terreno Difícil (Custo x2)";
		}

		var tok = FindVisibleTokenAt(cell);
		string tokNote = tok != null ? $" | {tok.Name} (PV {tok.CurrentHp}/{tok.MaxHp})" : string.Empty;

		if (!IsEditorMode && _sessionRef != null && _currentTurnId > 0 && _sessionRef.TryGetToken(_currentTurnId, out var curTok))
		{
			var origin = new GridPos(curTok.X, curTok.Y);
			int straightSquares = Tormenta20TacticalRules.ComputeT20DistanceSquares(origin, cell);
			string straightDist = Tormenta20TacticalRules.FormatMeters(straightSquares);

			if (origin != cell && (RulerMode || (tok != null && tok.EntityId != _currentTurnId)))
			{
				Tormenta20TacticalRules.EvaluateLineOfSightAndCover(_sessionRef, origin, cell, out bool hasLoS, out var cover);
				_cachedRayValid = true;
				_cachedRayOrigin = origin;
				_cachedRayDest = cell;
				_cachedRayHasLoS = hasLoS;
				_cachedRayCover = cover;
				_cachedRayDistSq = straightSquares;
			}

			if (!RulerMode && _reachableCells.TryGetValue(cell, out int costSquares))
			{
				_hoverPath = Tormenta20TacticalRules.FindShortestPath(_sessionRef, _currentTurnId, cell, costSquares, out _hoverPathCost);
				HoverStatusChanged?.Invoke($"{coordLabel} - {terrainName} | Movimento: {Tormenta20TacticalRules.FormatMeters(costSquares)} ({costSquares}q){tokNote}");
				return;
			}

			HoverStatusChanged?.Invoke($"{coordLabel} - {terrainName} | Distância: {straightDist}{tokNote}");
			return;
		}

		HoverStatusChanged?.Invoke($"{coordLabel} - {terrainName}{tokNote}");
	}

	public override void _Draw()
	{
		// 1. Fundo geral
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.06f, 0.07f, 0.09f, 1f));

		float cellPx = CellPx;
		Vector2 origin = GridOriginScreen;
		Vector2 mapSize = new Vector2(GridWidth * cellPx, GridHeight * cellPx);
		Rect2 mapRect = new Rect2(origin, mapSize);

		int visMinX = GridWidth > 0 ? Math.Clamp(Mathf.FloorToInt((0f - origin.X) / cellPx) - 1, 0, GridWidth - 1) : 0;
		int visMaxX = GridWidth > 0 ? Math.Clamp(Mathf.CeilToInt((Size.X - origin.X) / cellPx), 0, GridWidth - 1) : -1;
		int visMinY = GridHeight > 0 ? Math.Clamp(Mathf.FloorToInt((0f - origin.Y) / cellPx) - 1, 0, GridHeight - 1) : 0;
		int visMaxY = GridHeight > 0 ? Math.Clamp(Mathf.CeilToInt((Size.Y - origin.Y) / cellPx), 0, GridHeight - 1) : -1;

		// 2. Chão ou imagem de fundo
		if (_bgTexture != null)
		{
			DrawTextureRect(_bgTexture, mapRect, false, new Color(1f, 1f, 1f, 0.88f));
		}
		else if (cellPx < 10f)
		{
			DrawRect(mapRect, new Color(0.13f, 0.14f, 0.18f, 1f));
		}
		else
		{
			for (int y = visMinY; y <= visMaxY; y++)
			{
				for (int x = visMinX; x <= visMaxX; x++)
				{
					Color tileColor = ((x + y) % 2 == 0)
						? new Color(0.12f, 0.13f, 0.17f, 1f)
						: new Color(0.14f, 0.15f, 0.20f, 1f);
					DrawRect(CellRectScreen(x, y), tileColor);
				}
			}
		}

		// 3. Terrenos especiais (Terreno Difícil, Cobertura Leve, Parede)
		foreach (var kv in _cells)
		{
			var pos = kv.Key;
			var cell = kv.Value;
			if (pos.X < visMinX || pos.X > visMaxX || pos.Y < visMinY || pos.Y > visMaxY) continue;
			Rect2 r = CellRectScreen(pos.X, pos.Y);

			if (cell.TerrainType == MapTerrainType.Blocked)
			{
				DrawRect(r, new Color(0.24f, 0.25f, 0.30f, 0.96f));
				DrawRect(r.Grow(-2f), new Color(0.16f, 0.17f, 0.21f, 1f));
				DrawLine(r.Position, r.End, new Color(0.35f, 0.37f, 0.44f, 0.65f), 1.5f);
				DrawLine(new Vector2(r.End.X, r.Position.Y), new Vector2(r.Position.X, r.End.Y), new Color(0.35f, 0.37f, 0.44f, 0.65f), 1.5f);
				DrawRect(r, new Color(0.55f, 0.58f, 0.68f, 0.8f), false, 1.5f);
			}
			else if (cell.TerrainType == MapTerrainType.Difficult)
			{
				DrawRect(r, new Color(0.62f, 0.42f, 0.10f, 0.42f));
				for (float offset = cellPx * 0.25f; offset < cellPx; offset += cellPx * 0.35f)
				{
					DrawLine(
						new Vector2(r.Position.X + offset, r.Position.Y + 3f),
						new Vector2(r.Position.X + 3f, r.Position.Y + offset),
						new Color(0.95f, 0.68f, 0.22f, 0.55f), 1.5f);
				}
			}

			if (cell.CoverLevel == 1 && cell.TerrainType != MapTerrainType.Blocked)
			{
				Rect2 inner = r.Grow(-cellPx * 0.12f);
				DrawRect(inner, new Color(0.26f, 0.48f, 0.64f, 0.55f));
				DrawRect(inner, new Color(0.50f, 0.80f, 1.0f, 0.85f), false, 2.0f);
			}
		}

		// 4. Células alcançáveis e caminho pré-visualizado no Modo Combate
		if (!IsEditorMode && !RulerMode && _reachableCells.Count > 0)
		{
			foreach (var kv in _reachableCells)
			{
				if (kv.Key.X < visMinX || kv.Key.X > visMaxX || kv.Key.Y < visMinY || kv.Key.Y > visMaxY) continue;
				Rect2 r = CellRectScreen(kv.Key.X, kv.Key.Y);
				DrawRect(r.Grow(-1.5f), new Color(0.20f, 0.56f, 0.95f, 0.22f));
				DrawRect(r.Grow(-1.5f), new Color(0.36f, 0.72f, 1.0f, 0.38f), false, 1.0f);
			}
		}

		// 5. Névoa de Guerra
		if (FogOfWarEnabled && !IsEditorMode && _fogTracker != null)
		{
			for (int y = visMinY; y <= visMaxY; y++)
			{
				for (int x = visMinX; x <= visMaxX; x++)
				{
					FogState fs = _fogTracker.GetState(new GridPos(x, y));
					if (fs == FogState.Unseen)
					{
						DrawRect(CellRectScreen(x, y), new Color(0.03f, 0.04f, 0.06f, 0.95f));
					}
					else if (fs == FogState.Explored)
					{
						DrawRect(CellRectScreen(x, y), new Color(0.04f, 0.05f, 0.08f, 0.62f));
					}
				}
			}
		}

		// 6. Linhas da grade e borda externa
		float lineMinY = origin.Y + visMinY * cellPx;
		float lineMaxY = origin.Y + Math.Min(GridHeight, visMaxY + 1) * cellPx;
		float lineMinX = origin.X + visMinX * cellPx;
		float lineMaxX = origin.X + Math.Min(GridWidth, visMaxX + 1) * cellPx;

		for (int x = visMinX; x <= Math.Min(GridWidth, visMaxX + 1); x++)
		{
			float px = origin.X + x * cellPx;
			float alpha = cellPx < 8f ? (x % 5 == 0 ? 0.18f : 0.06f) : 0.14f;
			DrawLine(new Vector2(px, lineMinY), new Vector2(px, lineMaxY), new Color(1f, 1f, 1f, alpha), 1.0f);
		}
		for (int y = visMinY; y <= Math.Min(GridHeight, visMaxY + 1); y++)
		{
			float py = origin.Y + y * cellPx;
			float alpha = cellPx < 8f ? (y % 5 == 0 ? 0.18f : 0.06f) : 0.14f;
			DrawLine(new Vector2(lineMinX, py), new Vector2(lineMaxX, py), new Color(1f, 1f, 1f, alpha), 1.0f);
		}
		DrawRect(mapRect, new Color(0.75f, 0.62f, 0.32f, 0.75f), false, 2.0f);

		// Coordenadas nas margens (suporta colunas AA+ e passos adaptativos em grids grandes)
		Font font = ThemeDB.FallbackFont;
		int coordFontSize = Math.Clamp((int)(11 * _zoom), 9, 14);
		int labelStep = cellPx < 11f ? 5 : (cellPx < 22f ? 2 : 1);
		for (int x = visMinX; x <= visMaxX; x++)
		{
			if (x != 0 && x % labelStep != 0) continue;
			string col = FormatColumnLabel(x);
			Vector2 pos = new Vector2(origin.X + (x + 0.25f) * cellPx, origin.Y - 6f * Math.Max(0.65f, _zoom));
			DrawString(font, pos, col, HorizontalAlignment.Left, -1, coordFontSize, new Color(0.75f, 0.78f, 0.84f));
		}
		for (int y = visMinY; y <= visMaxY; y++)
		{
			if (y != 0 && y % labelStep != 0) continue;
			float rowWidth = Math.Max(22f, 26f * _zoom);
			Vector2 pos = new Vector2(origin.X - rowWidth - 4f * Math.Max(0.65f, _zoom), origin.Y + (y + 0.65f) * cellPx);
			DrawString(font, pos, (y + 1).ToString(), HorizontalAlignment.Right, rowWidth, coordFontSize, new Color(0.75f, 0.78f, 0.84f));
		}

		// 7. Destaque do hover + Caminho A* + Régua de Linha de Visão
		if (_hoverCell.HasValue)
		{
			Rect2 hr = CellRectScreen(_hoverCell.Value.X, _hoverCell.Value.Y);
			DrawRect(hr, new Color(1f, 0.88f, 0.35f, 0.85f), false, 2.2f);
		}

		if (!IsEditorMode && !RulerMode && _hoverPath.Count >= 2)
		{
			for (int i = 0; i < _hoverPath.Count - 1; i++)
			{
				Vector2 p0 = CellCenterScreen(new Vector2(_hoverPath[i].X, _hoverPath[i].Y));
				Vector2 p1 = CellCenterScreen(new Vector2(_hoverPath[i + 1].X, _hoverPath[i + 1].Y));
				DrawLine(p0, p1, new Color(0.35f, 0.92f, 1.0f, 0.95f), 3.0f);
			}
			if (_hoverPathCost > 0)
			{
				Vector2 endCenter = CellCenterScreen(new Vector2(_hoverPath[^1].X, _hoverPath[^1].Y));
				string distStr = Tormenta20TacticalRules.FormatMeters(_hoverPathCost);
				DrawBadge(font, endCenter + new Vector2(0, -cellPx * 0.48f), distStr, new Color(0.15f, 0.72f, 0.95f));
			}
		}

		if (!IsEditorMode && _cachedRayValid)
		{
			bool inRange = _cachedRayDistSq <= _maxWeaponRangeSquares;

			Color rayColor = !_cachedRayHasLoS || !inRange
				? new Color(1f, 0.30f, 0.30f, 0.9f)
				: (_cachedRayCover == T20CoverLevel.Light ? new Color(1f, 0.82f, 0.25f, 0.92f) : new Color(0.32f, 0.95f, 0.52f, 0.92f));

			Vector2 p0 = CellCenterScreen(new Vector2(_cachedRayOrigin.X, _cachedRayOrigin.Y));
			Vector2 p1 = CellCenterScreen(new Vector2(_cachedRayDest.X, _cachedRayDest.Y));
			DrawLine(p0, p1, rayColor, 2.8f);

			string label = Tormenta20TacticalRules.FormatMeters(_cachedRayDistSq);
			if (!_cachedRayHasLoS) label += " | Sem Visão";
			else if (!inRange) label += " | Fora de Alcance";
			else if (_cachedRayCover == T20CoverLevel.Light) label += " | Cobertura (+5 Def)";
			DrawBadge(font, (p0 + p1) * 0.5f, label, rayColor);
		}

		// 8. Tokens dos combatentes
		int nameFontSize = Math.Clamp((int)(10 * _zoom), 8, 13);
		int initFontSize = Math.Clamp((int)(14 * _zoom), 10, 18);

		foreach (var tok in _tokens)
		{
			if (!IsTokenVisibleUnderFog(tok)) continue;

			Vector2 gridCoord = _visualPositions.TryGetValue(tok.EntityId, out var vp)
				? vp
				: new Vector2(tok.GridPos.X, tok.GridPos.Y);
			Vector2 center = CellCenterScreen(gridCoord);
			float radius = cellPx * 0.36f;

			// Halo do turno atual
			if (tok.EntityId == _currentTurnId)
			{
				DrawArc(center, radius + 5f * _zoom, 0, Mathf.Tau, 28, new Color(1f, 0.86f, 0.25f, 0.95f), 2.8f);
			}
			// Mira do alvo selecionado
			if (tok.EntityId == _selectedTargetId && tok.EntityId != _currentTurnId)
			{
				DrawArc(center, radius + 4.5f * _zoom, 0, Mathf.Tau, 24, new Color(1f, 0.32f, 0.32f, 0.95f), 2.4f);
			}
			// Seleção no editor de movimento
			if (IsEditorMode && tok.EntityId == _selectedMoveTokenId)
			{
				DrawArc(center, radius + 5f * _zoom, 0, Mathf.Tau, 24, new Color(0.45f, 0.95f, 1f, 0.95f), 2.5f);
			}

			Color fillColor = tok.IsDead
				? new Color(0.22f, 0.16f, 0.18f, 0.92f)
				: (tok.Team == 1 ? new Color(0.16f, 0.34f, 0.62f, 0.96f) : new Color(0.56f, 0.16f, 0.18f, 0.96f));
			Color borderColor = tok.Team == 1
				? new Color(0.58f, 0.82f, 1.0f, 1f)
				: new Color(1.0f, 0.52f, 0.45f, 1f);

			DrawCircle(center + new Vector2(0, 2f), radius, new Color(0, 0, 0, 0.45f));
			DrawCircle(center, radius, fillColor);
			DrawArc(center, radius, 0, Mathf.Tau, 24, borderColor, 2.0f);

			string centerText = tok.IsDead ? "X" : (tok.IsBleedingOut ? "!" : tok.Initials);
			DrawString(font, center + new Vector2(-radius * 0.58f, initFontSize * 0.36f), centerText, HorizontalAlignment.Center, radius * 1.16f, initFontSize, Colors.White);

			// Mini-barra de PV
			float barW = cellPx * 0.72f;
			float barH = Mathf.Max(3.5f, 4.5f * _zoom);
			Vector2 barPos = new Vector2(center.X - barW * 0.5f, center.Y + radius + 2f * _zoom);
			DrawRect(new Rect2(barPos, new Vector2(barW, barH)), new Color(0.08f, 0.08f, 0.10f, 0.9f));
			float hpPct = tok.MaxHp > 0 ? Mathf.Clamp((float)tok.CurrentHp / tok.MaxHp, 0f, 1f) : 0f;
			Color hpColor = hpPct > 0.5f
				? new Color(0.25f, 0.85f, 0.42f)
				: (hpPct > 0.25f ? new Color(0.95f, 0.72f, 0.20f) : new Color(0.95f, 0.25f, 0.25f));
			if (hpPct > 0f)
			{
				DrawRect(new Rect2(barPos, new Vector2(barW * hpPct, barH)), hpColor);
			}

			// Nome curto abaixo da célula
			string shortName = tok.Name.Length > 12 ? tok.Name.Substring(0, 11) + "…" : tok.Name;
			DrawString(font, new Vector2(center.X - cellPx * 0.55f, barPos.Y + barH + nameFontSize + 1f),
				shortName, HorizontalAlignment.Center, cellPx * 1.1f, nameFontSize, new Color(0.94f, 0.94f, 0.96f));
		}
	}

	private void DrawBadge(Font font, Vector2 centerPos, string text, Color borderColor)
	{
		int fSize = Math.Clamp((int)(11 * _zoom), 10, 14);
		float estW = Math.Max(46f, text.Length * fSize * 0.62f + 14f);
		float estH = fSize + 10f;
		Rect2 box = new Rect2(centerPos - new Vector2(estW * 0.5f, estH * 0.5f), new Vector2(estW, estH));
		DrawRect(box, new Color(0.06f, 0.07f, 0.10f, 0.92f));
		DrawRect(box, borderColor, false, 1.4f);
		DrawString(font, new Vector2(box.Position.X + 6f, box.Position.Y + fSize + 2f), text, HorizontalAlignment.Left, -1, fSize, Colors.White);
	}
}
