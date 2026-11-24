using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

/// <summary>
/// Visualizador Gráfico Interativo da Árvore N-ária de Habilidades/Poderes (AbilityTree & RequirementTree).
/// Desenha os 5 ramos de Tormenta20 em layout hierárquico de grafo (Raiz -> Filhos -> Netos) com:
/// - Conexões geométricas luminosas entre nós pais e filhos via CanvasItem._Draw()
/// - Cartões interativos por nó (Dourado = Aprendido, Verde = Disponível, Cinza/Vermelho = Bloqueado)
/// - Explosão de partículas (CpuParticles2D) ao desbloquear um nó na árvore
/// </summary>
public partial class AbilityTreeCanvas : Control
{
	public sealed class NodeEvalState
	{
		public T20PowerTreeDefinition Definition { get; set; }
		public bool IsUnlocked { get; set; }
		public bool CanLearn { get; set; }
		public string UnmetLearnReasons { get; set; } = string.Empty;
		public bool CanUseInTurn { get; set; }
		public string UnmetUsageReasons { get; set; } = string.Empty;
		public Rect2 CardRect { get; set; }
	}

	private readonly Dictionary<string, NodeEvalState> _nodes = new();
	private readonly Dictionary<string, (StyleBoxFlat style, NodeEvalState state)> _cardStyles = new();
	private readonly List<(string parentKey, string childKey)> _edges = new();
	private string _selectedNodeKey = "estilo_uma_arma";
	private CpuParticles2D _unlockParticles;

	public event Action<NodeEvalState> NodeSelected;
	public event Action<T20PowerTreeDefinition> LearnRequested;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(1480, 470);
		_unlockParticles = new CpuParticles2D
		{
			Emitting = false,
			OneShot = true,
			Amount = 36,
			Lifetime = 0.85f,
			Explosiveness = 0.92f,
			Spread = 180f,
			Gravity = new Vector2(0, 110f),
			InitialVelocityMin = 70f,
			InitialVelocityMax = 180f,
			ScaleAmountMin = 3.0f,
			ScaleAmountMax = 6.5f,
			Color = new Color(1f, 0.86f, 0.25f, 1f),
			ZIndex = 10
		};
		AddChild(_unlockParticles);
	}

	public NodeEvalState GetNodeState(string key)
	{
		return !string.IsNullOrEmpty(key) && _nodes.TryGetValue(key, out var st) ? st : null;
	}

	public void PlayUnlockEffectAt(string key)
	{
		if (_unlockParticles != null && _nodes.TryGetValue(key, out var st))
		{
			_unlockParticles.Position = st.CardRect.GetCenter();
			_unlockParticles.Restart();
			_unlockParticles.Emitting = true;
		}
	}

	public void RebuildTree(CharacterSheetData character, EncounterSession activeSession = null, int activeCombatantId = 0)
	{
		foreach (Node child in GetChildren().ToArray())
		{
			if (child != _unlockParticles)
				child.QueueFree();
		}

		_nodes.Clear();
		_cardStyles.Clear();
		_edges.Clear();

		if (character == null)
		{
			QueueRedraw();
			return;
		}

		// Avalia todos os nós usando a AbilityTree e RequirementTree do Core C++
		bool ownSession = activeSession == null || activeCombatantId <= 0;
		EncounterSession evalSession = ownSession ? new EncounterSession() : activeSession;
		int evalId = activeCombatantId;

		try
		{
			if (ownSession)
			{
				Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(evalSession);
				evalId = evalSession.AddCombatant(character.Name, character.Dexterity, character.MaxHp, 1);
			}

			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				evalSession, evalId, character.Level,
				character.Strength, character.Dexterity, character.Constitution,
				character.Intelligence, character.Wisdom, character.Charisma,
				character.UnlockedTreeKeys);

			foreach (var def in Tormenta20AbilityTreeRules.CanonicalPowers)
			{
				string unmetLearn = string.Empty;
				string unmetUse = string.Empty;
				bool unlocked = character.UnlockedTreeKeys.Contains(def.Key) || evalSession.HasAbility(evalId, def.Key);
				bool canLearn = !unlocked && evalSession.CanLearnAbility(evalId, def.Key, out unmetLearn);
				bool canUse = unlocked && evalSession.CanUseAbility(evalId, def.Key, out unmetUse);
				if (!unlocked) unmetUse = "Poder ainda não desbloqueado na árvore.";

				_nodes[def.Key] = new NodeEvalState
				{
					Definition = def,
					IsUnlocked = unlocked,
					CanLearn = canLearn,
					UnmetLearnReasons = unmetLearn ?? string.Empty,
					CanUseInTurn = canUse,
					UnmetUsageReasons = unmetUse ?? string.Empty
				};

				if (!string.IsNullOrEmpty(def.ParentKey))
				{
					_edges.Add((def.ParentKey, def.Key));
				}
			}
		}
		finally
		{
			if (ownSession)
				evalSession.Dispose();
		}

		BuildBranchLayoutAndCards();
		QueueRedraw();

		if (_nodes.TryGetValue(_selectedNodeKey, out var selectedState))
		{
			NodeSelected?.Invoke(selectedState);
		}
		else if (_nodes.Count > 0)
		{
			var first = _nodes.Values.First();
			_selectedNodeKey = first.Definition.Key;
			NodeSelected?.Invoke(first);
		}
	}

	private void BuildBranchLayoutAndCards()
	{
		var branches = Tormenta20AbilityTreeRules.CanonicalPowers
			.GroupBy(p => p.BranchName)
			.ToList();

		float currentX = 16f;
		const float tier1Y = 58f;
		const float tier2Y = 192f;
		const float tier3Y = 326f;
		const float cardHeight = 96f;

		foreach (var branch in branches)
		{
			var tier1Nodes = branch.Where(p => p.Tier == 1).ToList();
			var tier2Nodes = branch.Where(p => p.Tier == 2).ToList();
			var tier3Nodes = branch.Where(p => p.Tier == 3).ToList();

			int maxColsInBranch = Math.Max(1, Math.Max(tier1Nodes.Count, Math.Max(tier2Nodes.Count, tier3Nodes.Count)));
			float cardWidth = maxColsInBranch > 1 ? 158f : 220f;
			float colGap = 12f;
			float branchWidth = maxColsInBranch * cardWidth + (maxColsInBranch - 1) * colGap;

			// Banner de Título do Ramo
			var branchBanner = new PanelContainer
			{
				Position = new Vector2(currentX, 10f),
				Size = new Vector2(branchWidth, 32f)
			};
			var bannerStyle = new StyleBoxFlat
			{
				BgColor = new Color(0.22f, 0.11f, 0.14f, 0.95f),
				BorderColor = new Color(0.65f, 0.48f, 0.25f, 0.9f),
				BorderWidthBottom = 2,
				BorderWidthTop = 1,
				BorderWidthLeft = 1,
				BorderWidthRight = 1,
				CornerRadiusTopLeft = 6,
				CornerRadiusTopRight = 6,
				CornerRadiusBottomLeft = 6,
				CornerRadiusBottomRight = 6
			};
			branchBanner.AddThemeStyleboxOverride("panel", bannerStyle);
			var lblBranch = new Label
			{
				Text = branch.Key.ToUpper(),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			lblBranch.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.55f));
			lblBranch.AddThemeFontSizeOverride("font_size", 12);
			branchBanner.AddChild(lblBranch);
			AddChild(branchBanner);

			void PlaceTierNodes(List<T20PowerTreeDefinition> tierList, float yPos)
			{
				if (tierList.Count == 0) return;
				float totalW = tierList.Count * cardWidth + (tierList.Count - 1) * colGap;
				float startX = currentX + (branchWidth - totalW) * 0.5f;

				for (int i = 0; i < tierList.Count; i++)
				{
					var def = tierList[i];
					float xPos = startX + i * (cardWidth + colGap);
					Rect2 rect = new Rect2(xPos, yPos, cardWidth, cardHeight);
					_nodes[def.Key].CardRect = rect;
					CreateNodeCardControl(_nodes[def.Key], rect);
				}
			}

			PlaceTierNodes(tier1Nodes, tier1Y);
			PlaceTierNodes(tier2Nodes, tier2Y);
			PlaceTierNodes(tier3Nodes, tier3Y);

			currentX += branchWidth + 24f;
		}

		CustomMinimumSize = new Vector2(Math.Max(1150f, currentX + 8f), 445f);
	}

	private void CreateNodeCardControl(NodeEvalState st, Rect2 rect)
	{
		bool isSelected = st.Definition.Key == _selectedNodeKey;

		Color bgColor;
		Color borderColor;
		Color titleColor;
		string statusBadge;

		if (st.IsUnlocked)
		{
			bgColor     = new Color(0.24f, 0.19f, 0.06f, 0.97f);
			borderColor = isSelected ? new Color(1f, 0.96f, 0.55f) : new Color(0.95f, 0.76f, 0.22f);
			titleColor  = new Color(1f, 0.92f, 0.45f);
			statusBadge = "APRENDIDO";
		}
		else if (st.CanLearn)
		{
			bgColor     = new Color(0.08f, 0.22f, 0.14f, 0.97f);
			borderColor = isSelected ? new Color(0.55f, 1f, 0.72f) : new Color(0.25f, 0.85f, 0.48f);
			titleColor  = new Color(0.65f, 1f, 0.78f);
			statusBadge = "DISPONÍVEL";
		}
		else
		{
			bgColor     = new Color(0.12f, 0.13f, 0.16f, 0.94f);
			borderColor = isSelected ? new Color(0.75f, 0.45f, 0.45f) : new Color(0.28f, 0.30f, 0.36f);
			titleColor  = new Color(0.72f, 0.74f, 0.78f);
			statusBadge = "BLOQUEADO";
		}

		var card = new PanelContainer
		{
			Position = rect.Position,
			Size = rect.Size,
			TooltipText = BuildNodeTooltip(st)
		};

		var style = new StyleBoxFlat
		{
			BgColor = bgColor,
			BorderColor = borderColor,
			BorderWidthLeft = isSelected ? 3 : 2,
			BorderWidthTop = isSelected ? 3 : 2,
			BorderWidthRight = isSelected ? 3 : 2,
			BorderWidthBottom = isSelected ? 3 : 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 7,
			ContentMarginRight = 7,
			ContentMarginTop = 5,
			ContentMarginBottom = 5,
			ShadowColor = st.IsUnlocked
				? new Color(1f, 0.8f, 0.15f, 0.22f)
				: (st.CanLearn ? new Color(0.2f, 0.9f, 0.45f, 0.18f) : new Color(0, 0, 0, 0.35f)),
			ShadowSize = (st.IsUnlocked || st.CanLearn) ? 8 : 4
		};
		card.AddThemeStyleboxOverride("panel", style);
		_cardStyles[st.Definition.Key] = (style, st);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 3);
		card.AddChild(vbox);

		// Linha superior: Status + Custo de PM
		var hTop = new HBoxContainer();
		vbox.AddChild(hTop);

		var lblStatus = new Label
		{
			Text = statusBadge,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		lblStatus.AddThemeFontSizeOverride("font_size", 9);
		lblStatus.AddThemeColorOverride("font_color", borderColor);
		hTop.AddChild(lblStatus);

		string costText = st.Definition.PmCost > 0 ? $"{st.Definition.PmCost} PM" : "Passivo";
		var lblCost = new Label { Text = costText };
		lblCost.AddThemeFontSizeOverride("font_size", 9);
		lblCost.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
		hTop.AddChild(lblCost);

		// Nome do Poder
		var lblName = new Label
		{
			Text = st.Definition.Name,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		lblName.AddThemeFontSizeOverride("font_size", 12);
		lblName.AddThemeColorOverride("font_color", titleColor);
		vbox.AddChild(lblName);

		// Rodapé do nó: Botão Aprender ou Resumo de Requisito
		if (st.CanLearn)
		{
			var btnLearn = new Button
			{
				Text = "Aprender Poder",
				SizeFlagsVertical = SizeFlags.ShrinkEnd
			};
			btnLearn.AddThemeFontSizeOverride("font_size", 10);
			btnLearn.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
			btnLearn.Pressed += () =>
			{
				_selectedNodeKey = st.Definition.Key;
				LearnRequested?.Invoke(st.Definition);
			};
			vbox.AddChild(btnLearn);
		}
		else
		{
			string footer = st.IsUnlocked
				? (st.Definition.PmCost > 0 ? $"Ativo ({Tormenta20ActionRules.GetActionTypeName(st.Definition.ActionType)})" : "Bônus Passivo Ativo")
				: ShortenRequirement(st.UnmetLearnReasons);
			var lblFooter = new Label
			{
				Text = footer,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				ClipText = true
			};
			lblFooter.AddThemeFontSizeOverride("font_size", 9);
			lblFooter.AddThemeColorOverride("font_color",
				st.IsUnlocked ? new Color(0.85f, 0.82f, 0.65f) : new Color(0.95f, 0.52f, 0.52f));
			vbox.AddChild(lblFooter);
		}

		// Clique no cartão seleciona o nó no inspetor detalhado
		card.GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				_selectedNodeKey = st.Definition.Key;
				UpdateCardSelectionVisuals();
				NodeSelected?.Invoke(st);
				if (mb.DoubleClick && st.CanLearn)
				{
					LearnRequested?.Invoke(st.Definition);
				}
			}
		};

		AddChild(card);
		if (_unlockParticles != null && IsInsideTree())
		{
			MoveChild(_unlockParticles, GetChildCount() - 1);
		}
	}

	private void UpdateCardSelectionVisuals()
	{
		foreach (var (key, entry) in _cardStyles)
		{
			bool sel = key == _selectedNodeKey;
			var st = entry.state;
			Color border = st.IsUnlocked
				? (sel ? new Color(1f, 0.96f, 0.55f) : new Color(0.95f, 0.76f, 0.22f))
				: (st.CanLearn
					? (sel ? new Color(0.55f, 1f, 0.72f) : new Color(0.25f, 0.85f, 0.48f))
					: (sel ? new Color(0.75f, 0.45f, 0.45f) : new Color(0.28f, 0.30f, 0.36f)));
			entry.style.BorderColor = border;
			int w = sel ? 3 : 2;
			entry.style.BorderWidthLeft = w;
			entry.style.BorderWidthTop = w;
			entry.style.BorderWidthRight = w;
			entry.style.BorderWidthBottom = w;
		}
	}

	private static string ShortenRequirement(string unmet)
	{
		if (string.IsNullOrWhiteSpace(unmet)) return "Requer pré-requisito";
		string first = unmet.Split(';')[0].Trim();
		return first.Length > 34 ? first.Substring(0, 32) + "..." : first;
	}

	private static string BuildNodeTooltip(NodeEvalState st)
	{
		string status = st.IsUnlocked ? "APRENDIDO" : (st.CanLearn ? "DISPONÍVEL PARA APRENDER" : "BLOQUEADO");
		string parentInfo = "Poder Inicial (Nível 1)";
		if (!string.IsNullOrEmpty(st.Definition.ParentKey))
		{
			string parentName = st.Definition.ParentKey;
			foreach (var p in Tormenta20AbilityTreeRules.CanonicalPowers)
			{
				if (p.Key == st.Definition.ParentKey)
				{
					parentName = p.Name;
					break;
				}
			}
			parentInfo = $"Pré-requisito: {parentName} (Grau {st.Definition.Tier})";
		}
		string reqInfo = st.IsUnlocked
			? "Todos os pré-requisitos cumpridos."
			: (st.CanLearn ? "Pré-requisitos cumpridos! Clique para aprender." : $"Pendente: {st.UnmetLearnReasons}");
		return $"{st.Definition.Name} [{status}]\n{parentInfo}\nCusto: {st.Definition.PmCost} PM | Ação: {Tormenta20ActionRules.GetActionTypeName(st.Definition.ActionType)}\n\n{st.Definition.Description}\n\n{reqInfo}";
	}

	public override void _Draw()
	{
		// Desenha as arestas (linhas conectoras) da Árvore N-ária (Tree<T>) ligando cada nó Pai aos seus Filhos
		foreach (var (parentKey, childKey) in _edges)
		{
			if (!_nodes.TryGetValue(parentKey, out var parentState) ||
			    !_nodes.TryGetValue(childKey, out var childState))
				continue;

			Rect2 pRect = parentState.CardRect;
			Rect2 cRect = childState.CardRect;

			Vector2 start = new Vector2(pRect.Position.X + pRect.Size.X * 0.5f, pRect.Position.Y + pRect.Size.Y);
			Vector2 end   = new Vector2(cRect.Position.X + cRect.Size.X * 0.5f, cRect.Position.Y);
			float midY    = (start.Y + end.Y) * 0.5f;

			Vector2 p1 = new Vector2(start.X, midY);
			Vector2 p2 = new Vector2(end.X, midY);

			Color lineColor;
			float lineWidth;

			if (parentState.IsUnlocked && childState.IsUnlocked)
			{
				lineColor = new Color(1.0f, 0.84f, 0.25f, 1.0f);
				lineWidth = 3.4f;
				// Halo dourado
				Color glow = new Color(1.0f, 0.84f, 0.20f, 0.26f);
				DrawLine(start, p1, glow, 7.5f, true);
				DrawLine(p1, p2, glow, 7.5f, true);
				DrawLine(p2, end, glow, 7.5f, true);
			}
			else if (parentState.IsUnlocked && childState.CanLearn)
			{
				lineColor = new Color(0.28f, 0.92f, 0.52f, 0.95f);
				lineWidth = 2.8f;
			}
			else if (parentState.IsUnlocked)
			{
				lineColor = new Color(0.85f, 0.58f, 0.24f, 0.80f);
				lineWidth = 2.2f;
			}
			else
			{
				lineColor = new Color(0.26f, 0.29f, 0.36f, 0.85f);
				lineWidth = 2.0f;
			}

			DrawLine(start, p1, lineColor, lineWidth, true);
			DrawLine(p1, p2, lineColor, lineWidth, true);
			DrawLine(p2, end, lineColor, lineWidth, true);

			// Pontos terminais de conexão no pai e no filho
			DrawCircle(start, 4.2f, lineColor);
			DrawCircle(end, 4.8f, lineColor);
		}
	}
}
