using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Controlador do Menu Principal RPG do OpenTTRPG.
/// Gerencia animações via MainMenuAnimator, highlight de foco via MainMenuButtonFx,
/// troca dinâmica de background sample e transição animada para MainUi.tscn.
/// </summary>
public partial class MainMenu : Control
{
	private const string ConfigPath = "user://main_menu_settings.cfg";
	private const string DefaultD20Path = "res://Assets/d20_logo.png";
	private const string DefaultFontPath = "res://Assets/MedievalSharp-Regular.ttf";

	[ExportGroup("Fundo / Background Samples")]
	[Export] public Texture2D BackgroundSample { get; set; }
	[Export] public Godot.Collections.Array<Texture2D> BackgroundSamples { get; set; } = new();
	[Export] public Godot.Collections.Array<string> BackgroundSampleNames { get; set; } = new();
	[Export] public int DefaultBackgroundIndex { get; set; } = 0;
	[Export(PropertyHint.Range, "0.0,0.85,0.05")] public float BackgroundDarkenAmount { get; set; } = 0.52f;

	[ExportGroup("Cena de Destino")]
	[Export(PropertyHint.File, "*.tscn")] public string MainUiScenePath { get; set; } = "res://Cenas/MainUi.tscn";

	[ExportGroup("Referências da Cena")]
	[Export] private MainMenuAnimator _animator;
	[Export] private Control _bgStage;
	[Export] private TextureRect _bgCurrent;
	[Export] private TextureRect _bgNext;
	[Export] private ColorRect _bgDimmer;
	[Export] private Control _contentRoot;
	[Export] private Control _titleRow;
	[Export] private TextureRect _d20Icon;
	[Export] private Control _ornamentRow;
	[Export] private Label _lblSubtitle;
	[Export] private Control _menuCard;
	[Export] private Control _footerBar;
	[Export] private MainMenuButtonFx _btnIniciar;
	[Export] private MainMenuButtonFx _btnOpcoes;
	[Export] private MainMenuButtonFx _btnSair;
	[Export] private MainMenuButtonFx _btnCycleBg;
	[Export] private ColorRect _transitionOverlay;
	[Export] private Label _lblTransition;

	private sealed class BackgroundEntry
	{
		public string Name { get; set; }
		public Texture2D Texture { get; set; }
		public string CustomFilePath { get; set; } = string.Empty;
	}

	private readonly List<BackgroundEntry> _catalog = new();
	private int _currentBgIndex = 0;
	private float _defaultDarkenAmount = 0.52f;
	private float _currentDarkenAmount = 0.52f;
	private bool _isTransitioning = false;

	private Window _dlgOpcoes;
	private FileDialog _fileDlgCustomBg;
	private OptionButton _optBgSample;
	private HSlider _sliderDarken;
	private Label _lblDarkenValor;
	private CheckBox _chkFullscreen;
	private Label _lblOptFeedback;

	public override void _Ready()
	{
		if (OS.GetCmdlineUserArgs().Contains("--self-test") || OS.GetCmdlineArgs().Contains("--self-test"))
		{
			GD.Print("[SELF-TEST] MainMenu carregado -> transitando automaticamente para MainUi.tscn...");
			CallDeferred(nameof(TransitionImmediateForSelfTest));
			return;
		}

		BindNodes();
		EnsureD20TextureLoaded();

		_defaultDarkenAmount = Mathf.Clamp(BackgroundDarkenAmount, 0.0f, 0.85f);
		_currentDarkenAmount = _defaultDarkenAmount;

		BuildBackgroundCatalog();
		LoadPreferences();

		if (_catalog.Count > 0)
		{
			_currentBgIndex = Mathf.Clamp(_currentBgIndex, 0, _catalog.Count - 1);
			ApplyBackgroundEntry(_currentBgIndex, animate: false);
		}
		ApplyDimmerAmount(_currentDarkenAmount);

		SetupFocusNeighbors();
		ConnectSignals();
		BuildOpcoesDialog();

		var buttons = CollectMenuButtons();
		_animator?.PrepareEntranceHidden(
			_transitionOverlay,
			_lblTransition,
			_titleRow,
			_ornamentRow,
			_lblSubtitle,
			_menuCard,
			buttons,
			_footerBar
		);

		CallDeferred(nameof(StartMenuAnimations));
	}

	private void TransitionImmediateForSelfTest()
	{
		GetTree().ChangeSceneToFile(MainUiScenePath);
	}

	private void BindNodes()
	{
		_animator ??= GetNodeOrNull<MainMenuAnimator>("MenuAnimator");
		_bgStage ??= GetNodeOrNull<Control>("BackgroundClip/BackgroundStage");
		_bgCurrent ??= GetNodeOrNull<TextureRect>("BackgroundClip/BackgroundStage/BgCurrent");
		_bgNext ??= GetNodeOrNull<TextureRect>("BackgroundClip/BackgroundStage/BgNext");
		_bgDimmer ??= GetNodeOrNull<ColorRect>("BgDimmer");
		_contentRoot ??= GetNodeOrNull<Control>("SafeMargin/LayoutVBox/ContentRoot");
		_titleRow ??= GetNodeOrNull<Control>("SafeMargin/LayoutVBox/ContentRoot/HeaderBox/TitleRow");
		_d20Icon ??= GetNodeOrNull<TextureRect>("SafeMargin/LayoutVBox/ContentRoot/HeaderBox/TitleRow/D20Icon");
		_ornamentRow ??= GetNodeOrNull<Control>("SafeMargin/LayoutVBox/ContentRoot/HeaderBox/OrnamentRow");
		_lblSubtitle ??= GetNodeOrNull<Label>("SafeMargin/LayoutVBox/ContentRoot/HeaderBox/LblSubtitle");
		_menuCard ??= GetNodeOrNull<Control>("SafeMargin/LayoutVBox/ContentRoot/MenuCard");
		_footerBar ??= GetNodeOrNull<Control>("SafeMargin/LayoutVBox/FooterBar");
		_btnIniciar ??= GetNodeOrNull<MainMenuButtonFx>("SafeMargin/LayoutVBox/ContentRoot/MenuCard/CardMargin/MenuButtons/BtnIniciar");
		_btnOpcoes ??= GetNodeOrNull<MainMenuButtonFx>("SafeMargin/LayoutVBox/ContentRoot/MenuCard/CardMargin/MenuButtons/BtnOpcoes");
		_btnSair ??= GetNodeOrNull<MainMenuButtonFx>("SafeMargin/LayoutVBox/ContentRoot/MenuCard/CardMargin/MenuButtons/BtnSair");
		_btnCycleBg ??= GetNodeOrNull<MainMenuButtonFx>("SafeMargin/LayoutVBox/FooterBar/BtnCycleBg");
		_transitionOverlay ??= GetNodeOrNull<ColorRect>("TransitionOverlay");
		_lblTransition ??= GetNodeOrNull<Label>("TransitionOverlay/Center/LblTransition");
	}

	private Control[] CollectMenuButtons()
	{
		var list = new List<Control>(3);
		if (_btnIniciar != null) list.Add(_btnIniciar);
		if (_btnOpcoes != null) list.Add(_btnOpcoes);
		if (_btnSair != null) list.Add(_btnSair);
		return list.ToArray();
	}

	private void EnsureD20TextureLoaded()
	{
		if (_d20Icon == null) return;
		if (_d20Icon.Texture == null)
		{
			_d20Icon.Texture = LoadTextureWithFallback(DefaultD20Path);
		}
	}

	private static Texture2D LoadTextureWithFallback(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) return null;

		if (ResourceLoader.Exists(path))
		{
			var loaded = GD.Load<Texture2D>(path);
			if (loaded != null) return loaded;
		}

		string globalPath = path.StartsWith("res://") || path.StartsWith("user://")
			? ProjectSettings.GlobalizePath(path)
			: path;

		if (File.Exists(globalPath))
		{
			var img = Image.LoadFromFile(globalPath);
			if (img != null && !img.IsEmpty())
			{
				return ImageTexture.CreateFromImage(img);
			}
		}

		return null;
	}

	private void BuildBackgroundCatalog()
	{
		_catalog.Clear();

		if (BackgroundSample != null)
		{
			_catalog.Add(new BackgroundEntry
			{
				Name = "Sample Geral",
				Texture = BackgroundSample
			});
		}

		if (BackgroundSamples != null)
		{
			for (int i = 0; i < BackgroundSamples.Count; i++)
			{
				Texture2D tex = BackgroundSamples[i];
				if (tex == null) continue;

				string name = (BackgroundSampleNames != null && i < BackgroundSampleNames.Count &&
							   !string.IsNullOrWhiteSpace(BackgroundSampleNames[i]))
					? BackgroundSampleNames[i]
					: $"Sample {i + 1}";

				_catalog.Add(new BackgroundEntry
				{
					Name = name,
					Texture = tex
				});
			}
		}

		if (_catalog.Count == 0)
		{
			Texture2D t20 = LoadTextureWithFallback("res://Tormenta20TTRPG.jpg");
			if (t20 != null)
			{
				_catalog.Add(new BackgroundEntry { Name = "Tormenta20", Texture = t20 });
			}

			Texture2D parchment = LoadTextureWithFallback("res://Assets/f00b9dd676916076c3182094d27d2e0e.png");
			if (parchment != null)
			{
				_catalog.Add(new BackgroundEntry { Name = "Pergaminho Antigo", Texture = parchment });
			}
		}

		_catalog.Add(new BackgroundEntry
		{
			Name = "Obsidiana & Carmesim",
			Texture = CreateObsidianCrimsonBackdrop()
		});

		_currentBgIndex = Mathf.Clamp(DefaultBackgroundIndex, 0, Mathf.Max(0, _catalog.Count - 1));
	}

	private static Texture2D CreateObsidianCrimsonBackdrop()
	{
		var grad = new Gradient();
		grad.SetColor(0, new Color(0.26f, 0.06f, 0.09f, 1.0f));
		grad.SetOffset(0, 0.0f);
		grad.SetColor(1, new Color(0.04f, 0.03f, 0.06f, 1.0f));
		grad.SetOffset(1, 1.0f);
		grad.AddPoint(0.50f, new Color(0.12f, 0.05f, 0.08f, 1.0f));

		return new GradientTexture2D
		{
			Width = 640,
			Height = 360,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.42f),
			FillTo = new Vector2(1.05f, 1.05f),
			Gradient = grad
		};
	}

	private void SetupFocusNeighbors()
	{
		if (_btnIniciar == null || _btnOpcoes == null || _btnSair == null) return;

		_btnIniciar.FocusNeighborTop = _btnIniciar.GetPathTo(_btnSair);
		_btnIniciar.FocusNeighborBottom = _btnIniciar.GetPathTo(_btnOpcoes);

		_btnOpcoes.FocusNeighborTop = _btnOpcoes.GetPathTo(_btnIniciar);
		_btnOpcoes.FocusNeighborBottom = _btnOpcoes.GetPathTo(_btnSair);

		_btnSair.FocusNeighborTop = _btnSair.GetPathTo(_btnOpcoes);
		_btnSair.FocusNeighborBottom = _btnSair.GetPathTo(_btnIniciar);

		if (_btnCycleBg != null)
		{
			_btnCycleBg.FocusNeighborTop = _btnCycleBg.GetPathTo(_btnSair);
		}
	}

	private void ConnectSignals()
	{
		if (_btnIniciar != null) _btnIniciar.Pressed += OnIniciarPressed;
		if (_btnOpcoes != null) _btnOpcoes.Pressed += OnOpcoesPressed;
		if (_btnSair != null) _btnSair.Pressed += OnSairPressed;
		if (_btnCycleBg != null) _btnCycleBg.Pressed += CycleBackgroundSample;
		if (_d20Icon != null) _d20Icon.GuiInput += OnD20GuiInput;
		Resized += UpdateResponsivePivots;
	}

	private async void StartMenuAnimations()
	{
		if (!IsInsideTree() || _isTransitioning) return;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!IsInsideTree() || _isTransitioning) return;

		UpdateResponsivePivots();
		_animator?.StartBackgroundBreathing(_bgStage);
		_animator?.PlayEntrance(
			_transitionOverlay,
			_titleRow,
			_d20Icon,
			_ornamentRow,
			_lblSubtitle,
			_menuCard,
			CollectMenuButtons(),
			_footerBar
		);
	}

	private void UpdateResponsivePivots()
	{
		MainMenuAnimator.CenterPivot(_bgStage);
		MainMenuAnimator.CenterPivot(_d20Icon);
		MainMenuAnimator.CenterPivot(_contentRoot);
		MainMenuAnimator.CenterPivot(_titleRow);
		MainMenuAnimator.CenterPivot(_ornamentRow);
		MainMenuAnimator.CenterPivot(_menuCard);
		MainMenuAnimator.CenterPivot(_footerBar);
	}

	private void OnD20GuiInput(InputEvent @event)
	{
		if (_isTransitioning) return;
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			_animator?.PlayD20RollSpin(_d20Icon);
		}
	}

	private void OnIniciarPressed()
	{
		if (_isTransitioning) return;
		_isTransitioning = true;

		LockMenuButtons(true);

		if (_animator != null)
		{
			_animator.PlayTransitionToMainUi(
				_transitionOverlay,
				_lblTransition,
				_contentRoot,
				_d20Icon,
				_bgStage,
				() =>
				{
					if (IsInsideTree())
					{
						GetTree().ChangeSceneToFile(MainUiScenePath);
					}
				}
			);
		}
		else
		{
			GetTree().ChangeSceneToFile(MainUiScenePath);
		}
	}

	private void OnOpcoesPressed()
	{
		if (_isTransitioning || _dlgOpcoes == null) return;
		RefreshOptionsDialogState();
		_dlgOpcoes.PopupCentered();
	}

	private void OnSairPressed()
	{
		if (_isTransitioning) return;
		_isTransitioning = true;

		LockMenuButtons(true);

		if (_animator != null)
		{
			_animator.PlayQuitTransition(_transitionOverlay, _contentRoot, () =>
			{
				if (IsInsideTree())
				{
					GetTree().Quit();
				}
			});
		}
		else
		{
			GetTree().Quit();
		}
	}

	private void LockMenuButtons(bool locked)
	{
		if (_btnIniciar != null) { _btnIniciar.InputLocked = locked; _btnIniciar.Disabled = locked; }
		if (_btnOpcoes != null) { _btnOpcoes.InputLocked = locked; _btnOpcoes.Disabled = locked; }
		if (_btnSair != null) { _btnSair.InputLocked = locked; _btnSair.Disabled = locked; }
		if (_btnCycleBg != null) { _btnCycleBg.InputLocked = locked; _btnCycleBg.Disabled = locked; }
	}

	/// <summary>
	/// Permite definir programaticamente um Texture2D de fundo como Sample Geral no menu.
	/// </summary>
	public void SetBackgroundSample(Texture2D sampleTexture, string displayName = "Sample Geral", bool animate = true)
	{
		if (sampleTexture == null) return;

		string resolvedName = string.IsNullOrWhiteSpace(displayName) ? "Sample Geral" : displayName;
		int existingIndex = _catalog.FindIndex(e => e.Name == resolvedName);

		if (existingIndex >= 0)
		{
			_catalog[existingIndex].Texture = sampleTexture;
			_catalog[existingIndex].CustomFilePath = string.Empty;
			_currentBgIndex = existingIndex;
		}
		else
		{
			_catalog.Insert(0, new BackgroundEntry
			{
				Name = resolvedName,
				Texture = sampleTexture
			});
			_currentBgIndex = 0;
		}

		ApplyBackgroundEntry(_currentBgIndex, animate);
		RefreshOptionsCatalogDropdown();
	}

	private void CycleBackgroundSample()
	{
		if (_isTransitioning || _catalog.Count == 0) return;

		_currentBgIndex = (_currentBgIndex + 1) % _catalog.Count;
		ApplyBackgroundEntry(_currentBgIndex, animate: true);
		RefreshOptionsDialogState();
		SavePreferences();
	}

	private void ApplyBackgroundEntry(int index, bool animate)
	{
		if (index < 0 || index >= _catalog.Count) return;

		BackgroundEntry entry = _catalog[index];
		_currentBgIndex = index;

		if (_animator != null)
		{
			_animator.CrossfadeBackground(_bgCurrent, _bgNext, entry.Texture, animate);
		}
		else if (_bgCurrent != null)
		{
			_bgCurrent.Texture = entry.Texture;
		}

		if (_btnCycleBg != null)
		{
			_btnCycleBg.Text = $"Fundo: {entry.Name} ↻";
		}
	}

	private void ApplyDimmerAmount(float amount)
	{
		_currentDarkenAmount = Mathf.Clamp(amount, 0.0f, 0.85f);
		if (_bgDimmer != null)
		{
			_bgDimmer.Color = new Color(0.04f, 0.02f, 0.03f, _currentDarkenAmount);
		}
	}

	private void LoadPreferences()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(ConfigPath) != Error.Ok) return;

		_currentDarkenAmount = Mathf.Clamp(
			(float)cfg.GetValue("background", "darken_amount", _defaultDarkenAmount),
			0.0f,
			0.85f
		);

		string customPath = (string)cfg.GetValue("background", "custom_bg_path", string.Empty);
		int savedIndex = (int)cfg.GetValue("background", "background_index", DefaultBackgroundIndex);

		if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
		{
			Texture2D customTex = LoadTextureWithFallback(customPath);
			if (customTex != null)
			{
				RegisterCustomBackgroundEntry(customTex, customPath, select: true, animate: false);
				return;
			}
		}

		if (_catalog.Count > 0)
		{
			_currentBgIndex = Mathf.Clamp(savedIndex, 0, _catalog.Count - 1);
		}
	}

	private void SavePreferences()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("background", "background_index", _currentBgIndex);
		cfg.SetValue("background", "darken_amount", _currentDarkenAmount);

		string activeCustomPath = (_currentBgIndex >= 0 && _currentBgIndex < _catalog.Count)
			? _catalog[_currentBgIndex].CustomFilePath
			: string.Empty;
		cfg.SetValue("background", "custom_bg_path", activeCustomPath ?? string.Empty);

		cfg.Save(ConfigPath);
	}

	private void RegisterCustomBackgroundEntry(Texture2D texture, string filePath, bool select, bool animate)
	{
		const string customName = "Sample Personalizado";
		int idx = _catalog.FindIndex(e => e.Name == customName);
		if (idx >= 0)
		{
			_catalog[idx].Texture = texture;
			_catalog[idx].CustomFilePath = filePath;
		}
		else
		{
			_catalog.Add(new BackgroundEntry
			{
				Name = customName,
				Texture = texture,
				CustomFilePath = filePath
			});
			idx = _catalog.Count - 1;
		}

		if (select)
		{
			_currentBgIndex = idx;
			ApplyBackgroundEntry(idx, animate);
		}
		RefreshOptionsCatalogDropdown();
	}

	private void BuildOpcoesDialog()
	{
		Font titleFont = ResourceLoader.Exists(DefaultFontPath) ? GD.Load<Font>(DefaultFontPath) : null;

		_dlgOpcoes = new Window
		{
			Title = "Opções — OpenTTRPG (Tormenta20 JdA)",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(500, 420),
			Exclusive = true,
			Visible = false,
			Unresizable = false
		};

		var backdrop = new PanelContainer();
		backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
		var bgStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.08f, 0.06f, 0.09f, 0.98f),
			BorderColor = new Color(0.68f, 0.50f, 0.22f, 0.95f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomRight = 8,
			CornerRadiusBottomLeft = 8
		};
		backdrop.AddThemeStyleboxOverride("panel", bgStyle);
		_dlgOpcoes.AddChild(backdrop);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 22);
		margin.AddThemeConstantOverride("margin_right", 22);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		backdrop.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		margin.AddChild(vbox);

		var lblTitle = new Label
		{
			Text = "Configurações de Vídeo, Fundo & Sessão",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		if (titleFont != null) lblTitle.AddThemeFontOverride("font", titleFont);
		lblTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.36f));
		lblTitle.AddThemeFontSizeOverride("font_size", 22);
		vbox.AddChild(lblTitle);

		vbox.AddChild(new HSeparator());

		_chkFullscreen = new CheckBox
		{
			Text = "Tela Cheia (Fullscreen)",
			ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
		};
		_chkFullscreen.Toggled += (bool pressed) =>
		{
			DisplayServer.WindowSetMode(pressed
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);
		};
		vbox.AddChild(_chkFullscreen);

		// Seção: Sample de Fundo
		var lblBg = new Label { Text = "Sample de Fundo (Background Geral):" };
		lblBg.AddThemeColorOverride("font_color", new Color(0.92f, 0.85f, 0.72f));
		vbox.AddChild(lblBg);

		_optBgSample = new OptionButton();
		RefreshOptionsCatalogDropdown();
		_optBgSample.ItemSelected += (long idx) =>
		{
			int i = (int)idx;
			if (i >= 0 && i < _catalog.Count)
			{
				ApplyBackgroundEntry(i, animate: true);
				SavePreferences();
				if (_lblOptFeedback != null)
				{
					_lblOptFeedback.Text = $"Fundo alterado para: {_catalog[i].Name}";
				}
			}
		};
		vbox.AddChild(_optBgSample);

		var hboxBgButtons = new HBoxContainer();
		hboxBgButtons.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hboxBgButtons);

		var btnCarregarSample = new Button
		{
			Text = "Carregar Imagem (Sample)...",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		btnCarregarSample.Pressed += () => _fileDlgCustomBg?.PopupCenteredRatio(0.65f);
		hboxBgButtons.AddChild(btnCarregarSample);

		var btnRestaurarBg = new Button
		{
			Text = "Restaurar Padrão",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		btnRestaurarBg.Pressed += OnRestaurarFundoPadrao;
		hboxBgButtons.AddChild(btnRestaurarBg);

		// Seção: Escurecimento
		var hboxDarken = new HBoxContainer();
		hboxDarken.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hboxDarken);

		var lblDarken = new Label
		{
			Text = "Escurecimento do Fundo:",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		lblDarken.AddThemeColorOverride("font_color", new Color(0.92f, 0.85f, 0.72f));
		hboxDarken.AddChild(lblDarken);

		_lblDarkenValor = new Label
		{
			Text = $"{Mathf.RoundToInt(_currentDarkenAmount * 100f)}%",
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize = new Vector2(48, 0)
		};
		_lblDarkenValor.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.36f));
		hboxDarken.AddChild(_lblDarkenValor);

		_sliderDarken = new HSlider
		{
			MinValue = 0.0,
			MaxValue = 0.85,
			Step = 0.05,
			Value = _currentDarkenAmount
		};
		_sliderDarken.ValueChanged += (double val) =>
		{
			ApplyDimmerAmount((float)val);
			if (_lblDarkenValor != null)
			{
				_lblDarkenValor.Text = $"{Mathf.RoundToInt((float)val * 100f)}%";
			}
			SavePreferences();
		};
		vbox.AddChild(_sliderDarken);

		// Recarregar fichas
		var btnRecarregar = new Button
		{
			Text = "Recarregar Fichas Salvas"
		};
		btnRecarregar.Pressed += () =>
		{
			var cm = GetNodeOrNull<CombatManager>("/root/CombatManager");
			cm?.LoadCharacters();
			cm?.LoadEnemies();
			if (_lblOptFeedback != null)
			{
				_lblOptFeedback.Text = cm != null
					? "Fichas e inimigos recarregados com sucesso."
					: "CombatManager não disponível nesta cena.";
			}
		};
		vbox.AddChild(btnRecarregar);

		_lblOptFeedback = new Label
		{
			Text = "OpenTTRPG — Sistema Tormenta20 (Jogo do Ano)",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblOptFeedback.AddThemeColorOverride("font_color", new Color(0.75f, 0.72f, 0.66f));
		_lblOptFeedback.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(_lblOptFeedback);

		var btnFechar = new Button
		{
			Text = "Fechar",
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(130, 34)
		};
		btnFechar.Pressed += () => _dlgOpcoes.Hide();
		_dlgOpcoes.CloseRequested += () => _dlgOpcoes.Hide();
		vbox.AddChild(btnFechar);

		AddChild(_dlgOpcoes);

		// FileDialog para escolher imagem de background personalizada
		_fileDlgCustomBg = new FileDialog
		{
			Title = "Selecionar Sample de Fundo (Imagem)",
			Access = FileDialog.AccessEnum.Filesystem,
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Filters = new[] { "*.png, *.jpg, *.jpeg, *.webp ; Imagens (*.png, *.jpg, *.jpeg, *.webp)" }
		};
		_fileDlgCustomBg.FileSelected += OnCustomBgFileSelected;
		AddChild(_fileDlgCustomBg);
	}

	private void RefreshOptionsCatalogDropdown()
	{
		if (_optBgSample == null) return;
		_optBgSample.Clear();

		for (int i = 0; i < _catalog.Count; i++)
		{
			_optBgSample.AddItem(_catalog[i].Name, i);
		}
		if (_currentBgIndex >= 0 && _currentBgIndex < _catalog.Count)
		{
			_optBgSample.Select(_currentBgIndex);
		}
	}

	private void RefreshOptionsDialogState()
	{
		if (_chkFullscreen != null)
		{
			_chkFullscreen.SetPressedNoSignal(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen);
		}
		RefreshOptionsCatalogDropdown();
		if (_sliderDarken != null)
		{
			_sliderDarken.SetValueNoSignal(_currentDarkenAmount);
		}
		if (_lblDarkenValor != null)
		{
			_lblDarkenValor.Text = $"{Mathf.RoundToInt(_currentDarkenAmount * 100f)}%";
		}
		if (_lblOptFeedback != null)
		{
			_lblOptFeedback.Text = "OpenTTRPG — Sistema Tormenta20 (Jogo do Ano)";
		}
	}

	private void OnCustomBgFileSelected(string filePath)
	{
		Texture2D tex = LoadTextureWithFallback(filePath);
		if (tex == null)
		{
			if (_lblOptFeedback != null)
			{
				_lblOptFeedback.Text = "Não foi possível carregar a imagem selecionada.";
			}
			return;
		}

		RegisterCustomBackgroundEntry(tex, filePath, select: true, animate: true);
		SavePreferences();

		if (_lblOptFeedback != null)
		{
			_lblOptFeedback.Text = $"Sample personalizado aplicado: {Path.GetFileName(filePath)}";
		}
	}

	private void OnRestaurarFundoPadrao()
	{
		_catalog.RemoveAll(e => !string.IsNullOrEmpty(e.CustomFilePath));

		int fallbackIdx = Mathf.Clamp(DefaultBackgroundIndex, 0, Mathf.Max(0, _catalog.Count - 1));
		ApplyDimmerAmount(_defaultDarkenAmount);
		ApplyBackgroundEntry(fallbackIdx, animate: true);
		RefreshOptionsDialogState();
		SavePreferences();

		if (_lblOptFeedback != null)
		{
			_lblOptFeedback.Text = "Fundo padrão restaurado.";
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isTransitioning) return;
		if (_dlgOpcoes != null && _dlgOpcoes.Visible) return;
		if (GetViewport().GuiGetFocusOwner() != null) return;

		if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_up"))
		{
			if (_btnIniciar != null && !_btnIniciar.Disabled)
			{
				_btnIniciar.GrabFocus();
				GetViewport().SetInputAsHandled();
			}
		}
	}
}
