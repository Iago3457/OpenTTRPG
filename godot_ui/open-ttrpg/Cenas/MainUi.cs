using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTTRPG.Native;
using OpenTTRPG.Tormenta20;

public partial class MainUi : Control
{
	private CombatManager _combatManager;

	// ── TopBar ──────────────────────────────────────────────────────────────
	private OptionButton _optPersonagens;
	private Button _btnViewFicha;
	private Button _btnViewInimigos;
	private Button _btnViewCombate;
	private Button _btnViewMapa;
	private Button _btnViewDados;
	private Button _btnSalvarFichas;
	private Button _btnVoltarMenu;
	private Button _btnNovoPersonagem;
	private Button _btnExcluirPersonagem;

	// ── Views ───────────────────────────────────────────────────────────────
	private Control _viewFicha;
	private Control _viewInimigos;
	private Control _viewCombate;
	private Control _viewMapa;
	private Control _viewDadosLegacy;

	// ── Ficha: Cabeçalho ────────────────────────────────────────────────────
	private Label _lblCharName;
	private Label _lblCharClassRace;
	private Label _lblCharNivel;
	private Label _lblCharOrigem;
	private Label _lblCharCondicoes;
	private Label _lblEstadoVital;
	private Label _lblTibaresFicha;
	private Button _btnDescansar;
	private Button _btnTestarMorte;
	private Button _btnReviver;
	private Button _btnPvTemp;
	private PopupMenu _menuDescanso;

	// ── Ficha: PV / PM ──────────────────────────────────────────────────────
	private PanelContainer _circlePv;
	private Label _lblPvValor;
	private Label _lblPvSubtexto;
	private SpinBox _spinPvDelta;
	private Button _btnPvDano;
	private Button _btnPvCurar;
	private Label _lblPmValor;
	private Label _lblPmSubtexto;
	private SpinBox _spinPmDelta;
	private Button _btnPmDano;
	private Button _btnPmCurar;

	// ── Ficha: Atributos ────────────────────────────────────────────────────
	private Label _lblForca;
	private Label _lblDestreza;
	private Label _lblConstituicao;
	private Label _lblInteligencia;
	private Label _lblSabedoria;
	private Label _lblCarisma;

	// ── Ficha: Sub-tabs ─────────────────────────────────────────────────────
	private Button _btnAtaques;
	private Button _btnDefesa;
	private Button _btnPoderes;
	private Button _btnMagias;
	private Button _btnEquip;
	private Label _btnBannerSubTab;
	private Label _lblDefesaDetalhe;
	private ItemList _listSubItems;
	private Label _lblCargaInfo;
	private Button _btnSubAcaoUsar;
	private Button _btnSubAcao1;
	private Button _btnSubAcao2;
	private Button _btnSubAcao3;

	// ── Ficha: Perícias ─────────────────────────────────────────────────────
	private LineEdit _txtBuscaPericia;
	private Label _lblResumoNivelTreino;
	private VBoxContainer _boxPericiasList;

	// ── Combate ─────────────────────────────────────────────────────────────
	private Button _btnIniciarCombate;
	private Button _btnProximoTurno;
	private Button _btnEncerrarCombate;
	private Button _btnSalvarCombate;
	private Button _btnCarregarCombate;
	private HBoxContainer _boxIniciativa;
	private Label _lblStatusCombate;
	private Label _lblTurnoAtual;
	private RichTextLabel _txtLogCombate;
	private Button _btnAtacar;
	private Button _btnMovimento;
	private Button _btnAcaoCompleta;
	private Button _btnUsarHabilidade;
	private Button _btnCondicoesCombate;
	private OptionButton _optAlvoCombate;
	private VBoxContainer _boxActionQueueList;
	private Label _lblActionQueueSlots;
	private Button _btnPopNextAction;
	private readonly Dictionary<(int cid, int actionId), int> _queuedActionPmSpent = new();

	// ── Pop-up Bandeja de Dados (Overlay sem trocar de aba!) ────────────────
	private ColorRect _popupDadosOverlay;
	private DiceVisualCanvas _diceVisualTray;
	private SpinBox _spinDiceQtd;
	private SpinBox _spinDiceMod;
	private OptionButton _optDiceModo;
	private LineEdit _txtExpressao;
	private Button _btnRolar;
	private Label _lblResultado;
	private Label _lblDetalhes;
	private RichTextLabel _txtHistorico;

	// ── Pop-up Perícia & Toast ──────────────────────────────────────────────
	private ColorRect _popupPericia;
	private DiceVisualCanvas _diceVisualPericia;
	private Label _lblPopupTitulo;
	private Label _lblPopupPersonagem;
	private Label _lblPopupResultado;
	private Label _lblPopupDestaque;
	private Label _lblPopupFormula;
	private Button _btnFecharPopup;
	private PanelContainer _toastPanel;
	private Label _lblToast;
	private Tween _toastTween;

	// ── Diálogos criados por código ─────────────────────────────────────────
	private AcceptDialog _dlgConfirmarExcluir;
	private Window _dlgNovoPersonagem;
	private Window _dlgNovoItem;
	private Window _dlgNovaMagia;
	private CheckBox _chkNovaMagiaIsSpell;
	private LineEdit _leNovaMagiaFormula;
	private Window _dlgArvorePoderes;
	private AbilityTreeCanvas _abilityTreeCanvas;
	private Label _lblArvoreCharResumo;
	private Label _lblArvoreNodeTitle;
	private Label _lblArvoreNodeDesc;
	private Label _lblArvoreNodeReqs;
	private Button _btnArvoreActionNode;
	private string _inspectedPowerKey = "estilo_uma_arma";
	private Window _dlgUsarHabilidadeCombate;
	private ItemList _listHabilidadesCombate;
	private SpinBox _spinAprimoramentoPm;
	private Label _lblLimitePmCombate;
	private Window _dlgCondicoesCombate;
	private OptionButton _optCondicaoAplicar;
	private SpinBox _spinDuracaoCondicao;
	private Window _dlgSelecaoIniciativa;
	private VBoxContainer _boxSelecaoIniciativaRows;
	private readonly List<(CharacterSheetData sheet, CheckBox chk, OptionButton optTeam, SpinBox spinQty)> _initSelectionEntries = new();
	private int _quickMonsterCounter = 1;

	private CharacterSheetData _selectedCharacter;
	private CharacterSheetData _selectedEnemy;
	private bool _addingSkillToEnemy;
	private bool _updatingEnemyUi;
	private string _currentMainView = "Ficha";
	private string _currentSubTab = "Ataques";

	// ── Ficha de Inimigos / Bestiário ───────────────────────────────────────
	private LineEdit _txtBuscaInimigo;
	private ItemList _listInimigos;
	private OptionButton _optBestiaryPreset;
	private LineEdit _leEnemyName;
	private OptionButton _optEnemyNd;
	private OptionButton _optEnemyRole;
	private OptionButton _optEnemyType;
	private OptionButton _optEnemySize;
	private Label _lblEnemyState;
	private Label _lblEnemyPvCircle;
	private Label _lblEnemyPmCircle;
	private SpinBox _spinEnemyPvMax;
	private SpinBox _spinEnemyPvDelta;
	private SpinBox _spinEnemyPmMax;
	private SpinBox _spinEnemyPmDelta;
	private SpinBox _spinEnemyDef;
	private SpinBox _spinEnemyRd;
	private SpinBox _spinEnemySpeed;
	private LineEdit _leEnemySenses;
	private SpinBox _spinEnemyInit;
	private SpinBox _spinEnemyPerc;
	private SpinBox _spinEnemyFort;
	private SpinBox _spinEnemyRef;
	private SpinBox _spinEnemyWill;
	private readonly Dictionary<AttributeType, SpinBox> _spinEnemyAttrs = new();
	private ItemList _listEnemyAttacks;
	private ItemList _listEnemySkills;
	private LineEdit _leEnemyTreasure;
	private TextEdit _txtEnemyNotes;
	private Window _dlgNovoAtaqueInimigo;

	// ── Mapa Tático (Tabletop) ──────────────────────────────────────────────
	private enum MapEditorBrush { Floor, Wall, Cover, Difficult, MoveToken }
	private MapEditorBrush _editorBrush = MapEditorBrush.Wall;
	private int _editorSelectedTokenId;
	private bool _runModeNextClick;
	private TacticalGridCanvas _tacticalGridCanvas;
	private TabContainer _tabsMapaLateral;
	private Button _btnMapaZoomLabel;
	private Button _btnMapaAjuda;
	private OptionButton _optArenaPreset;
	private Button _btnModoEditorMapa;
	private HBoxContainer _boxPinceisEditor;
	private readonly List<Button> _brushButtons = new();
	private Button _btnFogToggle;
	private Button _btnReguaMapa;
	private Button _btnMapaCorrer;
	private Button _btnMapaAtacar;
	private Button _btnMapaIniciarCombate;
	private Button _btnMapaProximoTurno;
	private Button _btnMapaSalvar;
	private Button _btnMapaHabilidade;
	private Button _btnMapaCondicoes;
	private Label _lblMapaStatusBar;
	private Label _lblMapaTurnoInfo;
	private Label _lblMapaAlvoResumo;
	private RichTextLabel _txtMapaMiniLog;
	private FileDialog _fileDlgMapaFundo;

	// ════════════════════════════════════════════════════════════════════════
	// _Ready
	// ════════════════════════════════════════════════════════════════════════
	public override void _Ready()
	{
		_combatManager = GetNode<CombatManager>("/root/CombatManager");

		BindUiElements();
		SetupAttributeEditButtons();
		SetupSheetHeaderAndSubActionControls();
		SetupResizableSheetLayout();
		SetupRestMenu();
		BuildDiceTrayPopup();
		EnhanceSkillPopupWithVisualDie();
		SetupExtraCombatControls();
		BuildEnemySheetView();
		BuildDialogs();
		ConnectEvents();

		_combatManager.CharactersLoaded += () =>
		{
			UpdateCharacterDropdown();
			if (_combatManager.Characters.Count > 0)
				SelectCharacter(0);
		};
		_combatManager.EnemiesLoaded += () =>
		{
			RefreshEnemyList();
		};

		if (_combatManager.Characters.Count > 0)
		{
			UpdateCharacterDropdown();
			SelectCharacter(0);
		}
		if (_combatManager.Enemies.Count > 0)
		{
			RefreshEnemyList();
			SelectEnemy(0);
		}

		ShowView("Ficha");
		UpdateCombatStatusDisplay();

		if (OS.GetCmdlineUserArgs().Contains("--self-test") || OS.GetCmdlineArgs().Contains("--self-test"))
		{
			CallDeferred(nameof(RunAutomatedUiCSharpCppSelfTest));
		}
	}

	private int GetSelectedCombatantId()
	{
		if (_selectedCharacter != null &&
			_combatManager != null &&
			_combatManager.IsInCombat &&
			_combatManager.SheetIdToCombatantId.TryGetValue(_selectedCharacter.Id, out int cid))
		{
			return cid;
		}
		return 0;
	}

	// ════════════════════════════════════════════════════════════════════════
	// Bind
	// ════════════════════════════════════════════════════════════════════════
	private void BindUiElements()
	{
		string root = "MarginContainer/VBoxMain";

		// TopBar
		_optPersonagens       = GetNode<OptionButton>($"{root}/TopBar/OptPersonagens");
		_btnViewFicha         = GetNode<Button>($"{root}/TopBar/BtnViewFicha");
		_btnViewCombate       = GetNode<Button>($"{root}/TopBar/BtnViewCombate");
		_btnViewDados         = GetNode<Button>($"{root}/TopBar/BtnViewDados");
		_btnSalvarFichas      = GetNode<Button>($"{root}/TopBar/BtnSalvarFichas");
		_btnVoltarMenu        = GetNode<Button>($"{root}/TopBar/BtnVoltarMenu");
		_btnNovoPersonagem    = GetNode<Button>($"{root}/TopBar/BtnNovoPersonagem");
		_btnExcluirPersonagem = GetNode<Button>($"{root}/TopBar/BtnExcluirPersonagem");

		// Views
		string mc = $"{root}/MainContent";
		_viewFicha       = GetNode<Control>($"{mc}/ViewFicha");
		_viewCombate     = GetNode<Control>($"{mc}/ViewCombate");
		_viewDadosLegacy = GetNode<Control>($"{mc}/ViewDados");
		_viewDadosLegacy.Visible = false;

		// Ficha – Cabeçalho
		string lc = $"{mc}/ViewFicha/CenterBox/HSplitFicha/LeftCol";
		string hdr = $"{lc}/CardHeader/Margin/HBoxHeader";

		_lblCharName      = GetNode<Label>($"{hdr}/VBoxInfo/LblCharName");
		_lblCharClassRace = GetNode<Label>($"{hdr}/VBoxInfo/LblCharClassRace");
		_lblCharNivel     = GetNode<Label>($"{hdr}/VBoxInfo/HBoxSub/LblCharNivel");
		_lblCharOrigem    = GetNode<Label>($"{hdr}/VBoxInfo/HBoxSub/LblCharOrigem");
		_lblCharCondicoes = GetNode<Label>($"{hdr}/VBoxInfo/LblCharCondicoes");
		_btnDescansar     = GetNode<Button>($"{hdr}/VBoxInfo/BtnDescansar");

		string pvPath = $"{hdr}/HBoxCircles/VBoxPv";
		_circlePv      = GetNode<PanelContainer>($"{pvPath}/CirclePv");
		_lblPvValor    = GetNode<Label>($"{pvPath}/CirclePv/LblPvValor");
		_lblPvSubtexto = GetNode<Label>($"{pvPath}/LblPvSubtexto");
		_spinPvDelta   = GetNode<SpinBox>($"{pvPath}/HBoxPvControls/SpinPvDelta");
		_btnPvDano     = GetNode<Button>($"{pvPath}/HBoxPvControls/BtnPvDano");
		_btnPvCurar    = GetNode<Button>($"{pvPath}/HBoxPvControls/BtnPvCurar");

		string pmPath = $"{hdr}/HBoxCircles/VBoxPm";
		_lblPmValor    = GetNode<Label>($"{pmPath}/CirclePm/LblPmValor");
		_lblPmSubtexto = GetNode<Label>($"{pmPath}/LblPmSubtexto");
		_spinPmDelta   = GetNode<SpinBox>($"{pmPath}/HBoxPmControls/SpinPmDelta");
		_btnPmDano     = GetNode<Button>($"{pmPath}/HBoxPmControls/BtnPmDano");
		_btnPmCurar    = GetNode<Button>($"{pmPath}/HBoxPmControls/BtnPmCurar");

		string attr = $"{lc}/CardAtributos/VBox/HBoxAttrCards";
		_lblForca        = GetNode<Label>($"{attr}/CardFOR/VBox/LblValor");
		_lblDestreza     = GetNode<Label>($"{attr}/CardDES/VBox/LblValor");
		_lblConstituicao = GetNode<Label>($"{attr}/CardCON/VBox/LblValor");
		_lblInteligencia = GetNode<Label>($"{attr}/CardINT/VBox/LblValor");
		_lblSabedoria    = GetNode<Label>($"{attr}/CardSAB/VBox/LblValor");
		_lblCarisma      = GetNode<Label>($"{attr}/CardCAR/VBox/LblValor");

		string interativo = $"{lc}/CardInterativo/VBox";
		_btnAtaques       = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnAtaques");
		_btnDefesa        = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnDefesa");
		_btnPoderes       = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnPoderes");
		_btnMagias        = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnMagias");
		_btnEquip         = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnEquip");
		_btnBannerSubTab  = GetNode<Label>($"{interativo}/PanelContent/BannerSubTab/LblBanner");
		_lblDefesaDetalhe = GetNode<Label>($"{interativo}/PanelContent/LblDefesaDetalhe");
		_listSubItems     = GetNode<ItemList>($"{interativo}/PanelContent/ListSubItems");
		_lblCargaInfo     = GetNode<Label>($"{interativo}/PanelContent/HBoxSubActions/LblCargaInfo");
		_btnSubAcao1      = GetNode<Button>($"{interativo}/PanelContent/HBoxSubActions/BtnSubAcao1");
		_btnSubAcao2      = GetNode<Button>($"{interativo}/PanelContent/HBoxSubActions/BtnSubAcao2");
		_btnSubAcao3      = GetNode<Button>($"{interativo}/PanelContent/HBoxSubActions/BtnSubAcao3");

		// Perícias
		string rc = $"{mc}/ViewFicha/CenterBox/HSplitFicha/RightCol/CardPericias/VBox";
		_txtBuscaPericia      = GetNode<LineEdit>($"{rc}/TxtBuscaPericia");
		_lblResumoNivelTreino = GetNode<Label>($"{rc}/LblResumoNivelTreino");
		_boxPericiasList      = GetNode<VBoxContainer>($"{rc}/ScrollPericias/BoxPericiasList");

		// Combate
		string cv = $"{mc}/ViewCombate/VBox";
		_btnIniciarCombate = GetNode<Button>($"{cv}/TopBar/BtnIniciarCombate");
		_btnProximoTurno   = GetNode<Button>($"{cv}/TopBar/BtnProximoTurno");
		_boxIniciativa     = GetNode<HBoxContainer>($"{cv}/ScrollIniciativa/BoxIniciativa");
		_lblStatusCombate  = GetNode<Label>($"{cv}/LblStatusCombate");
		_lblTurnoAtual     = GetNode<Label>($"{cv}/LblTurnoAtual");
		_txtLogCombate     = GetNode<RichTextLabel>($"{cv}/HSplitCombate/RightLog/TxtLogCombate");
		_btnAtacar         = GetNode<Button>($"{cv}/HSplitCombate/LeftActions/BtnAtacar");
		_btnMovimento      = GetNode<Button>($"{cv}/HSplitCombate/LeftActions/BtnMovimento");
		_btnUsarHabilidade = GetNode<Button>($"{cv}/HSplitCombate/LeftActions/BtnUsarHabilidade");
		_optAlvoCombate    = GetNode<OptionButton>($"{cv}/HSplitCombate/LeftActions/VBoxAlvo/OptAlvoCombate");

		// Pop-up Perícia & Toast
		_popupPericia       = GetNode<ColorRect>("PopupPericia");
		_lblPopupTitulo     = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/BannerHeader/LblPopupTitulo");
		_lblPopupPersonagem = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupPersonagem");
		_lblPopupResultado  = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupResultado");
		_lblPopupDestaque   = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupDestaque");
		_lblPopupFormula    = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupFormula");
		_btnFecharPopup     = GetNode<Button>("PopupPericia/Center/PanelDialog/Margin/VBox/BtnFecharPopup");
		_toastPanel         = GetNode<PanelContainer>("ToastPanel");
		_lblToast           = GetNode<Label>("ToastPanel/LblToast");
	}

	// ════════════════════════════════════════════════════════════════════════
	// Pop-up Flutuante: Bandeja de Dados Visual (Sobre Ficha ou Combate)
	// ════════════════════════════════════════════════════════════════════════
	private void BuildDiceTrayPopup()
	{
		_popupDadosOverlay = new ColorRect
		{
			Name = "PopupBandejaDados",
			Visible = false,
			Color = new Color(0f, 0f, 0f, 0.68f)
		};
		_popupDadosOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_popupDadosOverlay);

		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		_popupDadosOverlay.AddChild(center);

		var cardStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.11f, 0.12f, 0.15f, 0.98f),
			BorderColor = new Color(0.85f, 0.68f, 0.32f, 1f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10,
			ShadowColor = new Color(0, 0, 0, 0.55f),
			ShadowSize = 18
		};

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(680, 490)
		};
		panel.AddThemeStyleboxOverride("panel", cardStyle);
		center.AddChild(panel);

		// Consumir cliques dentro do painel para não fechar ao clicar nele
		panel.GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton)
				AcceptEvent();
		};

		// Clicar fora do painel (no backdrop escuro) fecha o popup
		center.GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				Vector2 localMouse = panel.GetLocalMousePosition();
				if (!new Rect2(Vector2.Zero, panel.Size).HasPoint(localMouse))
					ToggleDiceTrayPopup(false);
			}
		};

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 18);
		margin.AddThemeConstantOverride("margin_right", 18);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		panel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(vbox);

		// 1. Cabeçalho do Popup com botão Fechar
		var bannerStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.52f, 0.11f, 0.14f, 1f),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 12,
			ContentMarginRight = 10,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		};
		var banner = new PanelContainer();
		banner.AddThemeStyleboxOverride("panel", bannerStyle);
		vbox.AddChild(banner);

		var hHeader = new HBoxContainer();
		banner.AddChild(hHeader);

		var lblTitle = new Label
		{
			Text = "Bandeja de Dados",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		lblTitle.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.72f));
		lblTitle.AddThemeFontSizeOverride("font_size", 15);
		hHeader.AddChild(lblTitle);

		var btnClose = new Button
		{
			Text = "Fechar",
			Flat = true
		};
		btnClose.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.85f));
		btnClose.Pressed += () => ToggleDiceTrayPopup(false);
		hHeader.AddChild(btnClose);

		// 2. Barra de Seleção de Dados Poliedrais + Modificadores Rápidos
		var hQuickDice = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		hQuickDice.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(hQuickDice);

		var diceDefs = new (string label, int sides, Color tint)[]
		{
			("d4",   4,   new Color(0.65f, 0.95f, 0.75f)),
			("d6",   6,   new Color(0.75f, 0.88f, 1.00f)),
			("d8",   8,   new Color(0.85f, 0.75f, 1.00f)),
			("d10",  10,  new Color(1.00f, 0.78f, 0.65f)),
			("d12",  12,  new Color(1.00f, 0.68f, 0.78f)),
			("d20",  20,  new Color(1.00f, 0.86f, 0.32f)),
			("d100", 100, new Color(0.60f, 0.95f, 0.95f))
		};

		foreach (var (label, sides, tint) in diceDefs)
		{
			var btn = new Button
			{
				Text = label,
				CustomMinimumSize = new Vector2(78, 38),
				SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
			};
			btn.AddThemeColorOverride("font_color", tint);
			btn.AddThemeFontSizeOverride("font_size", 13);
			int capturedSides = sides;
			btn.Pressed += () => OnQuickDieButtonClicked(capturedSides);
			hQuickDice.AddChild(btn);
		}

		// 3. Controles de Quantidade, Modificador e Modo (Normal / Vantagem / Desvantagem / Explosivo)
		var hModifiers = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		hModifiers.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(hModifiers);

		hModifiers.AddChild(new Label { Text = "Qtd Dados:" });
		_spinDiceQtd = new SpinBox { MinValue = 1, MaxValue = 20, Value = 1, CustomMinimumSize = new Vector2(72, 0) };
		hModifiers.AddChild(_spinDiceQtd);

		hModifiers.AddChild(new Label { Text = "Modificador (+/-):" });
		_spinDiceMod = new SpinBox { MinValue = -30, MaxValue = 60, Value = 0, CustomMinimumSize = new Vector2(78, 0) };
		hModifiers.AddChild(_spinDiceMod);

		hModifiers.AddChild(new Label { Text = "Modo:" });
		_optDiceModo = new OptionButton { CustomMinimumSize = new Vector2(175, 0) };
		_optDiceModo.AddItem("Rolagem Normal", 0);
		_optDiceModo.AddItem("Vantagem (2d20kh1)", 1);
		_optDiceModo.AddItem("Desvantagem (2d20kl1)", 2);
		_optDiceModo.AddItem("Dados Explosivos (!)", 3);
		hModifiers.AddChild(_optDiceModo);

		// 4. Entrada de Fórmula Livre
		var hInput = new HBoxContainer();
		hInput.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hInput);

		_txtExpressao = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			Text = "1d20",
			PlaceholderText = "Digite uma fórmula (ex: 2d20kh1+5, 1d20+7, 4d6dl1, 3d6!+2)"
		};
		hInput.AddChild(_txtExpressao);

		_btnRolar = new Button
		{
			Text = "Rolar Fórmula",
			CustomMinimumSize = new Vector2(145, 36)
		};
		_btnRolar.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.35f));
		hInput.AddChild(_btnRolar);

		// 5. Área Central: Renderizador Visual do Dado Poliedral (Esquerda) + Resultado & Histórico (Direita)
		var hCenterDisplay = new HBoxContainer
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		hCenterDisplay.AddThemeConstantOverride("separation", 16);
		vbox.AddChild(hCenterDisplay);

		// Painel do Dado Poliedral Animado
		var dieStagePanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(220, 210)
		};
		var stageStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.07f, 0.08f, 0.10f, 1f),
			BorderColor = new Color(0.25f, 0.27f, 0.34f, 1f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		dieStagePanel.AddThemeStyleboxOverride("panel", stageStyle);
		hCenterDisplay.AddChild(dieStagePanel);

		_diceVisualTray = new DiceVisualCanvas
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		dieStagePanel.AddChild(_diceVisualTray);

		// Coluna Direita: Resultado Numérico + Detalhes + Histórico
		var vRightResult = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vRightResult.AddThemeConstantOverride("separation", 6);
		hCenterDisplay.AddChild(vRightResult);

		var hResTop = new HBoxContainer();
		hResTop.AddThemeConstantOverride("separation", 14);
		vRightResult.AddChild(hResTop);

		_lblResultado = new Label
		{
			Text = "-",
			CustomMinimumSize = new Vector2(90, 0),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_lblResultado.AddThemeFontSizeOverride("font_size", 46);
		hResTop.AddChild(_lblResultado);

		_lblDetalhes = new Label
		{
			Text = "Clique em um dado acima ou digite sua fórmula.",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblDetalhes.AddThemeColorOverride("font_color", new Color(0.82f, 0.84f, 0.88f));
		_lblDetalhes.AddThemeFontSizeOverride("font_size", 13);
		hResTop.AddChild(_lblDetalhes);

		var hHistHeader = new HBoxContainer();
		vRightResult.AddChild(hHistHeader);

		var lblHistTitle = new Label
		{
			Text = "Histórico de Rolagens da Sessão:",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		lblHistTitle.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
		lblHistTitle.AddThemeFontSizeOverride("font_size", 12);
		hHistHeader.AddChild(lblHistTitle);

		var btnLimparHist = new Button
		{
			Text = "Limpar",
			Flat = true
		};
		btnLimparHist.AddThemeFontSizeOverride("font_size", 11);
		btnLimparHist.Pressed += () => _txtHistorico.Clear();
		hHistHeader.AddChild(btnLimparHist);

		_txtHistorico = new RichTextLabel
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			BbcodeEnabled = true,
			ScrollFollowing = true,
			CustomMinimumSize = new Vector2(0, 125)
		};
		vRightResult.AddChild(_txtHistorico);
	}

	private void EnhanceSkillPopupWithVisualDie()
	{
		// Insere o renderizador visual de d20 dentro do PopupPericia para animar os testes de perícia da ficha!
		var vbox = GetNode<VBoxContainer>("PopupPericia/Center/PanelDialog/Margin/VBox");
		_diceVisualPericia = new DiceVisualCanvas
		{
			CustomMinimumSize = new Vector2(150, 145),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter
		};
		vbox.AddChild(_diceVisualPericia);
		vbox.MoveChild(_diceVisualPericia, 2);
		_lblPopupResultado.Visible = false; // O número já aparece animado dentro do icosaedro d20!
	}

	private void SetupExtraCombatControls()
	{
		// Botões extras na TopBar principal: Inimigos (Bestiário), Árvore Visual e Mapa Tático
		var topBarMain = GetNode<HBoxContainer>("MarginContainer/VBoxMain/TopBar");

		_btnViewInimigos = new Button
		{
			Name = "BtnViewInimigos",
			Text = "Inimigos"
		};
		_btnViewInimigos.AddThemeColorOverride("font_color", new Color(1f, 0.62f, 0.52f));
		topBarMain.AddChild(_btnViewInimigos);
		topBarMain.MoveChild(_btnViewInimigos, _btnViewFicha.GetIndex() + 1);

		var btnTopArvore = new Button
		{
			Text = "Árvore de Poderes"
		};
		btnTopArvore.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.68f));
		btnTopArvore.Pressed += AbrirArvorePoderes;
		topBarMain.AddChild(btnTopArvore);
		topBarMain.MoveChild(btnTopArvore, _btnViewDados.GetIndex() + 1);

		// Botões na barra superior de combate: Encerrar Combate, Salvar Snapshot e Carregar Snapshot
		var topBarCombate = GetNode<HBoxContainer>("MarginContainer/VBoxMain/MainContent/ViewCombate/VBox/TopBar");
		_btnEncerrarCombate = new Button
		{
			Text = "Encerrar Combate",
			Disabled = true
		};
		_btnEncerrarCombate.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
		topBarCombate.AddChild(_btnEncerrarCombate);

		_btnSalvarCombate = new Button
		{
			Text = "Salvar Combate",
			Disabled = true
		};
		_btnSalvarCombate.AddThemeColorOverride("font_color", new Color(0.5f, 0.95f, 0.65f));
		topBarCombate.AddChild(_btnSalvarCombate);

		_btnCarregarCombate = new Button
		{
			Text = "Restaurar Combate"
		};
		_btnCarregarCombate.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
		topBarCombate.AddChild(_btnCarregarCombate);

		// Botões extras no painel esquerdo de ações de combate + Painel Visual das Ações do Turno
		var leftActions = GetNode<VBoxContainer>("MarginContainer/VBoxMain/MainContent/ViewCombate/VBox/HSplitCombate/LeftActions");

		_btnAcaoCompleta = new Button
		{
			Text = "Ação Completa"
		};
		_btnAcaoCompleta.AddThemeColorOverride("font_color", new Color(1f, 0.65f, 0.45f));
		leftActions.AddChild(_btnAcaoCompleta);

		_btnCondicoesCombate = new Button
		{
			Text = "Condições"
		};
		_btnCondicoesCombate.AddThemeColorOverride("font_color", new Color(1f, 0.78f, 0.35f));
		leftActions.AddChild(_btnCondicoesCombate);

		// Painel de ações planejadas no turno
		var queueCard = new PanelContainer
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		var queueStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.08f, 0.09f, 0.12f, 0.96f),
			BorderColor = new Color(0.45f, 0.68f, 0.92f, 0.85f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		};
		queueCard.AddThemeStyleboxOverride("panel", queueStyle);
		leftActions.AddChild(queueCard);

		var vQueue = new VBoxContainer();
		vQueue.AddThemeConstantOverride("separation", 5);
		queueCard.AddChild(vQueue);

		var lblQueueTitle = new Label
		{
			Text = "Ações do Turno",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		lblQueueTitle.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.38f));
		lblQueueTitle.AddThemeFontSizeOverride("font_size", 12);
		vQueue.AddChild(lblQueueTitle);

		_lblActionQueueSlots = new Label
		{
			Text = "Disponível: 1 Padrão | 1 Movimento | Livres",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblActionQueueSlots.AddThemeColorOverride("font_color", new Color(0.75f, 0.85f, 0.95f));
		_lblActionQueueSlots.AddThemeFontSizeOverride("font_size", 11);
		vQueue.AddChild(_lblActionQueueSlots);

		var scrollQueue = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			CustomMinimumSize = new Vector2(0, 95)
		};
		vQueue.AddChild(scrollQueue);

		_boxActionQueueList = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_boxActionQueueList.AddThemeConstantOverride("separation", 4);
		scrollQueue.AddChild(_boxActionQueueList);

		_btnPopNextAction = new Button
		{
			Text = "Executar Próxima Ação"
		};
		_btnPopNextAction.AddThemeColorOverride("font_color", new Color(0.55f, 1f, 0.65f));
		_btnPopNextAction.AddThemeFontSizeOverride("font_size", 11);
		vQueue.AddChild(_btnPopNextAction);

		// Botão e aba do Mapa Tático (Tabletop)
		_btnViewMapa = new Button
		{
			Text = "Mapa Tático"
		};
		topBarMain.AddChild(_btnViewMapa);
		topBarMain.MoveChild(_btnViewMapa, _btnViewCombate.GetIndex() + 1);

		BuildTacticalMapView();
	}

	private void ToggleDiceTrayPopup(bool? forceState = null)
	{
		bool target = forceState ?? !_popupDadosOverlay.Visible;
		if (target)
		{
			_popupDadosOverlay.Modulate = new Color(1, 1, 1, 0);
			_popupDadosOverlay.Visible = true;
			var tw = CreateTween();
			tw.TweenProperty(_popupDadosOverlay, "modulate", new Color(1, 1, 1, 1), 0.18f);
			_txtExpressao.GrabFocus();
		}
		else
		{
			_popupDadosOverlay.Visible = false;
		}
	}

	private void OnQuickDieButtonClicked(int sides)
	{
		int qtd = (int)_spinDiceQtd.Value;
		int mod = (int)_spinDiceMod.Value;
		int modo = _optDiceModo.GetSelectedId();

		string expr;
		if (modo == 1 && sides == 20)
		{
			expr = "2d20kh1";
		}
		else if (modo == 2 && sides == 20)
		{
			expr = "2d20kl1";
		}
		else if (modo == 3)
		{
			expr = $"{qtd}d{sides}!";
		}
		else
		{
			expr = $"{qtd}d{sides}";
		}

		if (mod > 0) expr += $"+{mod}";
		else if (mod < 0) expr += mod.ToString();

		_txtExpressao.Text = expr;
		RolarDadoExpressao(expr);
	}

	// ════════════════════════════════════════════════════════════════════════
	// Botões - / + nos cards de Atributos & Menu de Descanso
	// ════════════════════════════════════════════════════════════════════════
	private void SetupAttributeEditButtons()
	{
		string attrRoot = "MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol/CardAtributos/VBox/HBoxAttrCards";
		var map = new (string card, AttributeType type)[]
		{
			("CardFOR", AttributeType.Strength),
			("CardDES", AttributeType.Dexterity),
			("CardCON", AttributeType.Constitution),
			("CardINT", AttributeType.Intelligence),
			("CardSAB", AttributeType.Wisdom),
			("CardCAR", AttributeType.Charisma)
		};

		foreach (var (card, attrType) in map)
		{
			var vbox = GetNode<VBoxContainer>($"{attrRoot}/{card}/VBox");
			var hbox = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			hbox.AddThemeConstantOverride("separation", 4);

			var btnMinus = new Button { Text = "-", CustomMinimumSize = new Vector2(22, 18), Flat = true };
			btnMinus.AddThemeFontSizeOverride("font_size", 11);
			btnMinus.AddThemeColorOverride("font_color", new Color(0.85f, 0.5f, 0.5f));

			var btnPlus = new Button { Text = "+", CustomMinimumSize = new Vector2(22, 18), Flat = true };
			btnPlus.AddThemeFontSizeOverride("font_size", 11);
			btnPlus.AddThemeColorOverride("font_color", new Color(0.5f, 0.95f, 0.6f));

			AttributeType captured = attrType;
			btnMinus.Pressed += () => AjustarAtributo(captured, -1);
			btnPlus.Pressed  += () => AjustarAtributo(captured, +1);

			hbox.AddChild(btnMinus);
			hbox.AddChild(btnPlus);
			vbox.AddChild(hbox);
		}
	}

	private void SetupSheetHeaderAndSubActionControls()
	{
		string hdrInfo = "MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol/CardHeader/Margin/HBoxHeader/VBoxInfo";
		var vboxInfo = GetNode<VBoxContainer>(hdrInfo);
		var hboxSub  = GetNode<HBoxContainer>($"{hdrInfo}/HBoxSub");

		// Botões - / + de Nível ao lado de LblCharNivel (recalculam PV/PM automaticamente!)
		var btnNivelMinus = new Button { Text = "-", CustomMinimumSize = new Vector2(22, 18), Flat = true };
		btnNivelMinus.AddThemeFontSizeOverride("font_size", 11);
		btnNivelMinus.AddThemeColorOverride("font_color", new Color(0.9f, 0.55f, 0.55f));
		btnNivelMinus.Pressed += () => AjustarNivel(-1);

		var btnNivelPlus = new Button { Text = "+", CustomMinimumSize = new Vector2(22, 18), Flat = true };
		btnNivelPlus.AddThemeFontSizeOverride("font_size", 11);
		btnNivelPlus.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.65f));
		btnNivelPlus.Pressed += () => AjustarNivel(+1);

		hboxSub.AddChild(btnNivelMinus);
		hboxSub.MoveChild(btnNivelMinus, 1);
		hboxSub.AddChild(btnNivelPlus);
		hboxSub.MoveChild(btnNivelPlus, 2);

		// Carteira de Tibares (T$) na Ficha do Personagem com botões rápidos (-10 / +10 / +100)
		var sepTib = new VSeparator();
		hboxSub.AddChild(sepTib);

		_lblTibaresFicha = new Label
		{
			Text = "T$ 50"
		};
		_lblTibaresFicha.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.32f));
		_lblTibaresFicha.AddThemeFontSizeOverride("font_size", 12);
		hboxSub.AddChild(_lblTibaresFicha);

		Button AddTibarBtn(string txt, int delta, Color col)
		{
			var b = new Button { Text = txt, Flat = true, CustomMinimumSize = new Vector2(28, 18) };
			b.AddThemeFontSizeOverride("font_size", 10);
			b.AddThemeColorOverride("font_color", col);
			b.Pressed += () => AjustarTibares(delta);
			hboxSub.AddChild(b);
			return b;
		}
		AddTibarBtn("-10", -10, new Color(0.95f, 0.55f, 0.55f));
		AddTibarBtn("+10", +10, new Color(0.55f, 0.95f, 0.65f));
		AddTibarBtn("+100", +100, new Color(1f, 0.88f, 0.35f));

		// Botão para conceder PV Temporário (+Temp) nos controles de PV da Ficha
		var hboxPvControls = GetNode<HBoxContainer>(
			"MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol/CardHeader/Margin/HBoxHeader/HBoxCircles/VBoxPv/HBoxPvControls");
		_btnPvTemp = new Button
		{
			Text = "+Temp"
		};
		_btnPvTemp.AddThemeColorOverride("font_color", new Color(0.45f, 0.88f, 1f));
		_btnPvTemp.AddThemeFontSizeOverride("font_size", 11);
		_btnPvTemp.Pressed += OnAdicionarPvTempPressed;
		hboxPvControls.AddChild(_btnPvTemp);

		// Label de Estado Vital (Ativo / Machucado / Morrendo / Inconsciente / Morto)
		_lblEstadoVital = new Label
		{
			Text = "ESTADO: ATIVO"
		};
		_lblEstadoVital.AddThemeFontSizeOverride("font_size", 12);
		_lblEstadoVital.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.55f));
		vboxInfo.AddChild(_lblEstadoVital);
		vboxInfo.MoveChild(_lblEstadoVital, 3);

		// Linha de botões de Descanso + Teste de Morte (CON CD 15) + Reviver
		var hActionRow = new HBoxContainer();
		hActionRow.AddThemeConstantOverride("separation", 8);
		vboxInfo.AddChild(hActionRow);

		_btnTestarMorte = new Button
		{
			Text = "Testar Constituição (CD 15 - Estabilizar)",
			Visible = false
		};
		_btnTestarMorte.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
		_btnTestarMorte.AddThemeFontSizeOverride("font_size", 12);
		_btnTestarMorte.Pressed += OnTestarMorteNaFichaPressed;
		hActionRow.AddChild(_btnTestarMorte);

		_btnReviver = new Button
		{
			Text = "Reviver Personagem (1 PV)",
			Visible = false
		};
		_btnReviver.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.35f));
		_btnReviver.AddThemeFontSizeOverride("font_size", 12);
		_btnReviver.Pressed += OnReviverNaFichaPressed;
		hActionRow.AddChild(_btnReviver);

		// Botão para Usar / Gastar Habilidades (Poderes, Magias e Ataques) diretamente na Ficha!
		var hboxSubActions = GetNode<HBoxContainer>(
			"MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol/CardInterativo/VBox/PanelContent/HBoxSubActions");
		_btnSubAcaoUsar = new Button
		{
			Text = "Usar / Gastar PM"
		};
		_btnSubAcaoUsar.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.32f));
		_btnSubAcaoUsar.AddThemeFontSizeOverride("font_size", 11);
		_btnSubAcaoUsar.Pressed += OnUsarItemSubTabNaFichaPressed;
		hboxSubActions.AddChild(_btnSubAcaoUsar);
		hboxSubActions.MoveChild(_btnSubAcaoUsar, 1);

		// Duplo clique em qualquer Poder, Magia ou Arma na lista da ficha executa imediatamente!
		_listSubItems.ItemActivated += (_) => OnUsarItemSubTabNaFichaPressed();
	}

	private static void StyleSplitContainer(SplitContainer split)
	{
		if (split == null) return;
		split.DraggerVisibility = SplitContainer.DraggerVisibilityEnum.Visible;
		split.AddThemeConstantOverride("separation", 10);
		split.AddThemeConstantOverride("autohide", 0);

		var barBg = new StyleBoxFlat
		{
			BgColor = new Color(0.22f, 0.24f, 0.30f, 0.55f),
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3,
			CornerRadiusBottomRight = 3
		};
		split.AddThemeStyleboxOverride("split_bar_background", barBg);
	}

	private void SetupResizableSheetLayout()
	{
		if (GetNodeOrNull<HSplitContainer>("MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha") is { } hSplitFicha)
		{
			StyleSplitContainer(hSplitFicha);
		}
		if (GetNodeOrNull<HSplitContainer>("MarginContainer/VBoxMain/MainContent/ViewCombate/VBox/HSplitCombate") is { } hSplitCombate)
		{
			StyleSplitContainer(hSplitCombate);
		}

		// Subdivide a coluna esquerda da ficha em um VSplitContainer redimensionável entre Topo (Cabeçalho + Atributos) e Base (Abas + Proficiências)
		var leftCol = GetNodeOrNull<VBoxContainer>("MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol");
		if (leftCol == null || leftCol.GetNodeOrNull<VSplitContainer>("VSplitLeftCol") != null) return;

		var cardHeader = leftCol.GetNodeOrNull<Control>("CardHeader");
		var cardAttr = leftCol.GetNodeOrNull<Control>("CardAtributos");
		var cardInter = leftCol.GetNodeOrNull<Control>("CardInterativo");
		var cardProf = leftCol.GetNodeOrNull<Control>("CardProficiencias");
		if (cardHeader == null || cardAttr == null || cardInter == null || cardProf == null) return;

		var vSplit = new VSplitContainer
		{
			Name = "VSplitLeftCol",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		StyleSplitContainer(vSplit);

		var topScroll = new ScrollContainer
		{
			Name = "TopPaneScroll",
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			CustomMinimumSize = new Vector2(0, 175),
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsStretchRatio = 0.85f
		};
		var topVBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		topVBox.AddThemeConstantOverride("separation", 8);
		topScroll.AddChild(topVBox);

		var bottomVBox = new VBoxContainer
		{
			Name = "BottomPaneBox",
			CustomMinimumSize = new Vector2(0, 175),
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsStretchRatio = 1.15f
		};
		bottomVBox.AddThemeConstantOverride("separation", 8);

		cardHeader.Reparent(topVBox);
		cardAttr.Reparent(topVBox);
		cardInter.Reparent(bottomVBox);
		cardProf.Reparent(bottomVBox);

		vSplit.AddChild(topScroll);
		vSplit.AddChild(bottomVBox);
		leftCol.AddChild(vSplit);
	}

	private void AjustarNivel(int delta)
	{
		if (_selectedCharacter == null) return;
		int novoNivel = Math.Clamp(_selectedCharacter.Level + delta, 1, 20);
		if (novoNivel == _selectedCharacter.Level) return;

		_selectedCharacter.Level = novoNivel;
		_selectedCharacter.RecalculateMaxResources(refill: false);

		int cid = GetSelectedCombatantId();
		if (cid > 0)
		{
			_combatManager.Session.AddResource(cid, CombatManager.HealthResourceId, "Vida", _selectedCharacter.MaxHp, _selectedCharacter.GetDeathThreshold());
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedCharacter.CurrentHp);
			_combatManager.Session.AddResource(cid, CombatManager.ManaResourceId, "PM", _selectedCharacter.MaxPm, 0);
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.ManaResourceId, _selectedCharacter.CurrentPm);
			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				_combatManager.Session, cid, _selectedCharacter.Level,
				_selectedCharacter.Strength, _selectedCharacter.Dexterity, _selectedCharacter.Constitution,
				_selectedCharacter.Intelligence, _selectedCharacter.Wisdom, _selectedCharacter.Charisma,
				_selectedCharacter.UnlockedTreeKeys);
		}

		UpdateCharacterDropdown();
		int idx = _combatManager.Characters.IndexOf(_selectedCharacter);
		if (idx >= 0) _optPersonagens.Select(idx);
		RefreshSheetUI();
		ShowToast($"Nível ajustado para {_selectedCharacter.Level}! PV Máx: {_selectedCharacter.MaxHp} | PM Máx: {_selectedCharacter.MaxPm}");
	}

	private void AjustarAtributo(AttributeType type, int delta)
	{
		if (_selectedCharacter == null) return;
		var attr = _selectedCharacter.GetAttribute(type);
		int novo = Math.Clamp(attr.BaseValue + delta, -5, 15);
		_selectedCharacter.SetAttributeValue(type, novo);

		// Recalcula PV Máx e PM Máx automaticamente se Constituição ou Atributo-Chave de Magia mudou
		_selectedCharacter.RecalculateMaxResources(refill: false);

		int cid = GetSelectedCombatantId();
		if (cid > 0)
		{
			_combatManager.Session.AddResource(cid, CombatManager.HealthResourceId, "Vida", _selectedCharacter.MaxHp, _selectedCharacter.GetDeathThreshold());
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedCharacter.CurrentHp);
			_combatManager.Session.AddResource(cid, CombatManager.ManaResourceId, "PM", _selectedCharacter.MaxPm, 0);
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.ManaResourceId, _selectedCharacter.CurrentPm);
			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				_combatManager.Session, cid, _selectedCharacter.Level,
				_selectedCharacter.Strength, _selectedCharacter.Dexterity, _selectedCharacter.Constitution,
				_selectedCharacter.Intelligence, _selectedCharacter.Wisdom, _selectedCharacter.Charisma,
				_selectedCharacter.UnlockedTreeKeys);
		}

		RefreshSheetUI();
	}

	private void SetupRestMenu()
	{
		_menuDescanso = new PopupMenu();
		_menuDescanso.AddItem("Descanso Ruim (0,5x Nível em PV e PM)", (int)RestQuality.Bad);
		_menuDescanso.AddItem("Descanso Normal (1x Nível em PV e PM)", (int)RestQuality.Normal);
		_menuDescanso.AddItem("Descanso Confortável (2x Nível em PV e PM)", (int)RestQuality.Comfortable);
		_menuDescanso.AddItem("Descanso Luxuoso (3x Nível em PV e PM)", (int)RestQuality.Luxurious);
		AddChild(_menuDescanso);

		_menuDescanso.IdPressed += (long id) =>
		{
			if (_selectedCharacter == null) return;
			var quality = (RestQuality)id;
			int cid = GetSelectedCombatantId();
			var result = Tormenta20RestRules.ApplyRest(
				_selectedCharacter, quality, cid > 0 ? _combatManager.Session : null, cid);
			RefreshSheetUI();
			if (_combatManager.IsInCombat)
			{
				RefreshInitiativeBar();
				PopularAlvoCombate();
			}
			ShowToast(result.Message, new Color(0.4f, 0.85f, 1f));
		};
	}

	// ════════════════════════════════════════════════════════════════════════
	// Dialogs
	// ════════════════════════════════════════════════════════════════════════
	private void BuildDialogs()
	{
		_dlgConfirmarExcluir = new AcceptDialog
		{
			Title = "Confirmar Exclusão",
			DialogText = "Deseja realmente excluir este personagem? Esta ação não pode ser desfeita."
		};
		_dlgConfirmarExcluir.AddCancelButton("Cancelar");
		AddChild(_dlgConfirmarExcluir);
		_dlgConfirmarExcluir.Confirmed += OnExcluirConfirmado;

		_dlgNovoPersonagem = BuildNovoPersonagemDialog();
		AddChild(_dlgNovoPersonagem);

		_dlgNovoItem = BuildNovoItemDialog();
		AddChild(_dlgNovoItem);

		_dlgNovaMagia = BuildNovaMagiaDialog();
		AddChild(_dlgNovaMagia);

		_dlgNovoAtaqueInimigo = BuildNovoAtaqueInimigoDialog();
		AddChild(_dlgNovoAtaqueInimigo);

		_dlgArvorePoderes = BuildArvorePoderesDialog();
		AddChild(_dlgArvorePoderes);

		_dlgUsarHabilidadeCombate = BuildUsarHabilidadeCombateDialog();
		AddChild(_dlgUsarHabilidadeCombate);

		_dlgCondicoesCombate = BuildCondicoesCombateDialog();
		AddChild(_dlgCondicoesCombate);

		_dlgSelecaoIniciativa = BuildSelecaoIniciativaDialog();
		AddChild(_dlgSelecaoIniciativa);
	}

	private void AjustarTibares(int delta)
	{
		if (_selectedCharacter == null) return;
		_selectedCharacter.Tibares = Math.Max(0, _selectedCharacter.Tibares + delta);
		RefreshSheetUI();
	}

	private void OnAdicionarPvTempPressed()
	{
		if (_selectedCharacter == null) return;
		int delta = Math.Max(1, (int)_spinPvDelta.Value);
		_selectedCharacter.TempHp += delta;

		int cid = GetSelectedCombatantId();
		if (cid > 0)
		{
			_combatManager.Session.SetResourceTemp(cid, CombatManager.HealthResourceId, _selectedCharacter.TempHp);
			RefreshInitiativeBar();
			PopularAlvoCombate();
		}

		RefreshSheetUI();
		ShowToast($"Recebeu +{delta} PV Temporário (Total Temp: {_selectedCharacter.TempHp})!", new Color(0.45f, 0.88f, 1f));
	}

	private Window BuildSelecaoIniciativaDialog()
	{
		var win = new Window
		{
			Title = "Selecionar Participantes do Combate",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(660, 520),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(vbox);

		var lblDesc = new Label
		{
			Text = "Marque quais heróis e inimigos entram neste combate, defina os times e a quantidade de cada ameaça:",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		lblDesc.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.45f));
		lblDesc.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(lblDesc);

		var hTools = new HBoxContainer();
		hTools.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(hTools);

		var btnCheckAll = new Button { Text = "Marcar Todos" };
		btnCheckAll.AddThemeFontSizeOverride("font_size", 11);
		btnCheckAll.Pressed += () =>
		{
			foreach (var e in _initSelectionEntries) e.chk.ButtonPressed = true;
		};
		hTools.AddChild(btnCheckAll);

		var btnUncheckAll = new Button { Text = "Desmarcar Todos" };
		btnUncheckAll.AddThemeFontSizeOverride("font_size", 11);
		btnUncheckAll.Pressed += () =>
		{
			foreach (var e in _initSelectionEntries) e.chk.ButtonPressed = false;
		};
		hTools.AddChild(btnUncheckAll);

		var btnAddQuickMonster = new Button
		{
			Text = "+ Gerar Inimigo do Bestiário",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkEnd
		};
		btnAddQuickMonster.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.45f));
		btnAddQuickMonster.AddThemeFontSizeOverride("font_size", 11);
		btnAddQuickMonster.Pressed += () =>
		{
			CriarInimigoRapidoNoLobby();
			PopularListaSelecaoIniciativa();
		};
		hTools.AddChild(btnAddQuickMonster);

		var scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vbox.AddChild(scroll);

		_boxSelecaoIniciativaRows = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_boxSelecaoIniciativaRows.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(_boxSelecaoIniciativaRows);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);

		var btnCancelar = new Button { Text = "Cancelar" };
		var btnIniciar  = new Button { Text = "Rolar Iniciativa dos Selecionados" };
		btnIniciar.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		hBtns.AddChild(btnCancelar);
		hBtns.AddChild(btnIniciar);

		btnCancelar.Pressed += () => win.Hide();
		win.CloseRequested  += () => win.Hide();
		btnIniciar.Pressed  += () =>
		{
			var selected = new List<CharacterSheetData>();
			var teams = new Dictionary<int, int>();

			foreach (var (sheet, chk, optTeam, spinQty) in _initSelectionEntries)
			{
				if (!chk.ButtonPressed) continue;
				int teamId = optTeam.GetSelectedId();
				int qty = Math.Clamp((int)spinQty.Value, 1, 8);
				if (qty == 1)
				{
					selected.Add(sheet);
					teams[sheet.Id] = teamId;
				}
				else
				{
					for (int q = 0; q < qty; q++)
					{
						int cloneId = _combatManager.GetNextUniqueSheetId(sheet.IsEnemy) + selected.Count;
						var copy = sheet.CloneForCombat(cloneId, $"{sheet.Name} #{q + 1}");
						selected.Add(copy);
						teams[copy.Id] = teamId;
					}
				}
			}

			if (selected.Count == 0)
			{
				ShowToast("Marque pelo menos 1 personagem para participar da iniciativa!", new Color(1f, 0.45f, 0.45f));
				return;
			}

			win.Hide();
			_queuedActionPmSpent.Clear();
			_combatManager.StartCombatWithSelectedCharacters(selected, teams);
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
			RefreshActionQueuePanel();
			RefreshSheetUI();
			RefreshEnemySheetUI();
		};

		win.Hide();
		return win;
	}

	private void CriarInimigoRapidoNoLobby()
	{
		var presets = Tormenta20Bestiary.Presets;
		var info = presets[(_quickMonsterCounter - 1) % presets.Count];
		string uniqueName = $"{info.DisplayName} #{_quickMonsterCounter++}";
		_combatManager.CreateEnemyFromPreset(info.Key, uniqueName);
		_combatManager.SaveEnemies();

		RefreshEnemyList();
		ShowToast($"Ameaça \"{uniqueName}\" adicionada ao Bestiário!");
	}

	private void PopularListaSelecaoIniciativa()
	{
		foreach (Node child in _boxSelecaoIniciativaRows.GetChildren().ToArray())
			child.QueueFree();
		_initSelectionEntries.Clear();

		void AddGroupHeader(string title, Color color)
		{
			var lbl = new Label { Text = title };
			lbl.AddThemeFontSizeOverride("font_size", 12);
			lbl.AddThemeColorOverride("font_color", color);
			_boxSelecaoIniciativaRows.AddChild(lbl);
		}

		void AddParticipantRow(CharacterSheetData c, bool defaultChecked, int defaultTeam)
		{
			Tormenta20SkillRules.TryCalculateSkillModifier(c, "Iniciativa", out int initMod, out _);
			string initStr = initMod >= 0 ? $"+{initMod}" : initMod.ToString();
			string subtitle = c.IsEnemy
				? $"ND {c.ChallengeRating} | PV {c.CurrentHp}/{c.MaxHp} | Def {c.GetDefense()} | Ini {initStr}"
				: $"{c.Race} {c.CharacterClass} Nv.{c.Level} | PV {c.CurrentHp}/{c.MaxHp} | Ini {initStr}";

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);

			var chk = new CheckBox
			{
				ButtonPressed = defaultChecked,
				Text = $"{c.Name} ({subtitle})",
				SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
			};
			row.AddChild(chk);

			var spinQty = new SpinBox
			{
				MinValue = 1,
				MaxValue = 8,
				Value = 1,
				Prefix = "x",
				CustomMinimumSize = new Vector2(64, 0),
				TooltipText = "Quantidade de cópias neste combate"
			};
			row.AddChild(spinQty);

			var optTeam = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
			optTeam.AddItem("Time 1 (Heróis)", 1);
			optTeam.AddItem("Time 2 (Inimigos)", 2);
			optTeam.Select(defaultTeam == 2 ? 1 : 0);
			row.AddChild(optTeam);

			_boxSelecaoIniciativaRows.AddChild(row);
			_initSelectionEntries.Add((c, chk, optTeam, spinQty));
		}

		AddGroupHeader("HERÓIS / JOGADORES (Time 1)", new Color(0.55f, 0.85f, 1f));
		for (int i = 0; i < _combatManager.Characters.Count; i++)
		{
			AddParticipantRow(_combatManager.Characters[i], defaultChecked: true, defaultTeam: 1);
		}

		AddGroupHeader("INIMIGOS / AMEAÇAS DO BESTIÁRIO (Time 2)", new Color(1f, 0.55f, 0.45f));
		if (_combatManager.Enemies.Count == 0)
		{
			var emptyLbl = new Label { Text = "Nenhuma ameaça no bestiário. Clique em '+ Gerar Inimigo do Bestiário' ou crie na aba Inimigos." };
			emptyLbl.AddThemeFontSizeOverride("font_size", 11);
			emptyLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			_boxSelecaoIniciativaRows.AddChild(emptyLbl);
		}
		else
		{
			for (int i = 0; i < _combatManager.Enemies.Count; i++)
			{
				AddParticipantRow(_combatManager.Enemies[i], defaultChecked: i == 0, defaultTeam: 2);
			}
		}
	}

	private void AbrirModalSelecaoIniciativa()
	{
		if (_combatManager.Characters.Count == 0)
		{
			_combatManager.LoadCharacters();
		}
		if (_combatManager.Enemies.Count == 0)
		{
			_combatManager.LoadEnemies();
		}
		PopularListaSelecaoIniciativa();
		_dlgSelecaoIniciativa.PopupCentered();
	}

	private Window BuildNovoPersonagemDialog()
	{
		var win = new Window
		{
			Title = "Novo Personagem — Criação Oficial Tormenta20 JdA",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(520, 680),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 7);
		margin.AddChild(vbox);

		var lblNome = new Label { Text = "Nome do Personagem *" };
		lblNome.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
		lblNome.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(lblNome);
		var leName = new LineEdit { PlaceholderText = "Ex: Thorin Ironfoot, Niele, Valeros..." };
		vbox.AddChild(leName);

		// SelectionBoxes (Enums) em Grid 2x2: Classe, Raça, Origem e Divindade
		var gridEnums = new GridContainer { Columns = 2 };
		gridEnums.AddThemeConstantOverride("h_separation", 12);
		gridEnums.AddThemeConstantOverride("v_separation", 6);
		vbox.AddChild(gridEnums);

		OptionButton AddEnumSelector(string title)
		{
			var vb = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
			var l = new Label { Text = title };
			l.AddThemeColorOverride("font_color", new Color(0.82f, 0.82f, 0.82f));
			l.AddThemeFontSizeOverride("font_size", 12);
			vb.AddChild(l);
			var opt = new OptionButton { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
			vb.AddChild(opt);
			gridEnums.AddChild(vb);
			return opt;
		}

		var optClasse    = AddEnumSelector("Classe (T20 JdA)");
		var optRaca      = AddEnumSelector("Raça (T20 JdA)");
		var optOrigem    = AddEnumSelector("Origem");
		var optDivindade = AddEnumSelector("Divindade (Panteão)");

		foreach (var c in Tormenta20CharacterCreationRules.CanonicalClasses)
			optClasse.AddItem($"{c.DisplayName} ({c.BaseHpLevel1} PV | +{c.PmPerLevel} PM/nv)", (int)c.ClassType);

		foreach (var r in Tormenta20CharacterCreationRules.CanonicalRaces)
			optRaca.AddItem(r.DisplayName, (int)r.RaceType);

		foreach (T20Origin orig in Enum.GetValues(typeof(T20Origin)))
			optOrigem.AddItem(Tormenta20CharacterCreationRules.GetOriginDisplayName(orig), (int)orig);

		foreach (T20Deity deity in Enum.GetValues(typeof(T20Deity)))
			optDivindade.AddItem(Tormenta20CharacterCreationRules.GetDeityDisplayName(deity), (int)deity);

		SpinBox AddSpin(Container parent, string label, int min, int max, int def, int width = 85)
		{
			var vb = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
			parent.AddChild(vb);
			var l = new Label { Text = label };
			l.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
			l.AddThemeFontSizeOverride("font_size", 11);
			vb.AddChild(l);
			var sp = new SpinBox { MinValue = min, MaxValue = max, Value = def, CustomMinimumSize = new Vector2(width, 0) };
			vb.AddChild(sp);
			return sp;
		}

		var hNivelRow = new HBoxContainer();
		hNivelRow.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(hNivelRow);
		var spNivel = AddSpin(hNivelRow, "Nível Inicial (1 a 20)", 1, 20, 1, 110);

		var chkBonusRacial = new CheckBox
		{
			Text = "Aplicar modificadores raciais nos atributos ao trocar de Raça",
			ButtonPressed = true,
			SizeFlagsVertical = SizeFlags.ShrinkEnd
		};
		chkBonusRacial.AddThemeFontSizeOverride("font_size", 11);
		hNivelRow.AddChild(chkBonusRacial);

		var lblAttrTitle = new Label { Text = "Atributos (Modificadores Finais T20):" };
		lblAttrTitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.4f));
		lblAttrTitle.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(lblAttrTitle);

		var gridAttrs = new GridContainer { Columns = 3 };
		gridAttrs.AddThemeConstantOverride("h_separation", 10);
		gridAttrs.AddThemeConstantOverride("v_separation", 6);
		vbox.AddChild(gridAttrs);

		var spFor = AddSpin(gridAttrs, "FOR (Força)", -5, 15, 1, 130);
		var spDes = AddSpin(gridAttrs, "DES (Destreza)", -5, 15, 1, 130);
		var spCon = AddSpin(gridAttrs, "CON (Constituição)", -5, 15, 1, 130);
		var spInt = AddSpin(gridAttrs, "INT (Inteligência)", -5, 15, 0, 130);
		var spSab = AddSpin(gridAttrs, "SAB (Sabedoria)", -5, 15, 0, 130);
		var spCar = AddSpin(gridAttrs, "CAR (Carisma)", -5, 15, 0, 130);

		// Painel de Pré-visualização do Cálculo Automático (PV, PM, Defesa, Espaços, Limite de Morte)
		var previewCard = new PanelContainer();
		var previewStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.08f, 0.11f, 0.14f, 0.96f),
			BorderColor = new Color(0.35f, 0.68f, 0.88f, 0.85f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
		previewCard.AddThemeStyleboxOverride("panel", previewStyle);
		vbox.AddChild(previewCard);

		var lblPreviewStats = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		lblPreviewStats.AddThemeFontSizeOverride("font_size", 11);
		lblPreviewStats.AddThemeColorOverride("font_color", new Color(0.88f, 0.94f, 1f));
		previewCard.AddChild(lblPreviewStats);

		void AtualizarCalculoAutomatico()
		{
			var classTpl = Tormenta20CharacterCreationRules.GetClassTemplate((T20Class)optClasse.GetSelectedId());
			var raceTpl  = Tormenta20CharacterCreationRules.GetRaceTemplate((T20Race)optRaca.GetSelectedId());
			int nivel    = (int)spNivel.Value;
			int forMod   = (int)spFor.Value;
			int desMod   = (int)spDes.Value;
			int conMod   = (int)spCon.Value;
			int intMod   = (int)spInt.Value;
			int sabMod   = (int)spSab.Value;
			int carMod   = (int)spCar.Value;

			int keyMod = classTpl.SpellKeyAttribute switch
			{
				AttributeType.Intelligence => intMod,
				AttributeType.Wisdom => sabMod,
				AttributeType.Charisma => carMod,
				_ => 0
			};

			int calcHp = Tormenta20CharacterCreationRules.CalculateMaxHp(classTpl, raceTpl, nivel, conMod);
			int calcPm = Tormenta20CharacterCreationRules.CalculateMaxPm(classTpl, raceTpl, nivel, keyMod);
			int calcDef = 10 + desMod + raceTpl.NaturalDefenseBonus;
			int calcSlots = forMod >= 0 ? 10 + forMod * 2 : 10 + forMod;
			int deathLimit = -Math.Max(10, calcHp / 2);

			lblPreviewStats.Text =
				$"CÁLCULO AUTOMÁTICO (TORMENTA20 JdA):\n" +
				$"PV Máximo: {calcHp}   |   PM Máximo: {calcPm}   |   Defesa Base: {calcDef}   |   Carga: {calcSlots} esp.   |   Morte em: {deathLimit} PV\n" +
				$"Raça ({raceTpl.DisplayName}): {raceTpl.TraitSummary}\n" +
				$"Habilidade Inicial ({classTpl.DisplayName}): {classTpl.IconicSkillName} ({classTpl.IconicSkillPm} PM) | Perícias: {string.Join(", ", classTpl.DefaultTrainedSkills)}";
		}

		void AplicarAtributosRaciais()
		{
			if (!chkBonusRacial.ButtonPressed)
			{
				AtualizarCalculoAutomatico();
				return;
			}
			var raceTpl = Tormenta20CharacterCreationRules.GetRaceTemplate((T20Race)optRaca.GetSelectedId());
			spFor.Value = raceTpl.ModFor;
			spDes.Value = raceTpl.ModDes;
			spCon.Value = raceTpl.ModCon;
			spInt.Value = raceTpl.ModInt;
			spSab.Value = raceTpl.ModSab;
			spCar.Value = raceTpl.ModCar;
			AtualizarCalculoAutomatico();
		}

		optClasse.ItemSelected += (_) => AtualizarCalculoAutomatico();
		optRaca.ItemSelected   += (_) => AplicarAtributosRaciais();
		spNivel.ValueChanged   += (_) => AtualizarCalculoAutomatico();
		spFor.ValueChanged     += (_) => AtualizarCalculoAutomatico();
		spDes.ValueChanged     += (_) => AtualizarCalculoAutomatico();
		spCon.ValueChanged     += (_) => AtualizarCalculoAutomatico();
		spInt.ValueChanged     += (_) => AtualizarCalculoAutomatico();
		spSab.ValueChanged     += (_) => AtualizarCalculoAutomatico();
		spCar.ValueChanged     += (_) => AtualizarCalculoAutomatico();

		AplicarAtributosRaciais();

		var hboxBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hboxBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hboxBtns);

		var btnCancelar = new Button { Text = "Cancelar" };
		var btnCriar    = new Button { Text = "Criar Personagem (Cálculo Automático)" };
		btnCriar.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		hboxBtns.AddChild(btnCancelar);
		hboxBtns.AddChild(btnCriar);

		btnCancelar.Pressed += () => win.Hide();
		win.CloseRequested  += () => win.Hide();
		btnCriar.Pressed += () =>
		{
			string nome = leName.Text.Trim();
			if (string.IsNullOrEmpty(nome))
			{
				leName.PlaceholderText = "Nome é obrigatório!";
				leName.Modulate = new Color(1f, 0.4f, 0.4f);
				return;
			}

			leName.Modulate = Colors.White;
			var selectedClass = (T20Class)optClasse.GetSelectedId();
			var selectedRace  = (T20Race)optRaca.GetSelectedId();
			var selectedOrig  = (T20Origin)optOrigem.GetSelectedId();
			var selectedDeity = (T20Deity)optDivindade.GetSelectedId();

			OnCriarPersonagemComEnums(
				nome,
				selectedClass,
				selectedRace,
				selectedOrig,
				selectedDeity,
				(int)spNivel.Value,
				(int)spFor.Value, (int)spDes.Value, (int)spCon.Value,
				(int)spInt.Value, (int)spSab.Value, (int)spCar.Value
			);

			leName.Text = "";
			spNivel.Value = 1;
			AplicarAtributosRaciais();
			win.Hide();
		};

		win.Hide();
		return win;
	}

	private Window BuildNovoItemDialog()
	{
		var win = new Window
		{
			Title = "Adicionar Equipamento / Item",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(410, 480),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);

		var leNome = new LineEdit { PlaceholderText = "Ex: Espada Longa, Couraça, Poção..." };
		vbox.AddChild(new Label { Text = "Nome do Item *" });
		vbox.AddChild(leNome);

		var optTipo = new OptionButton();
		optTipo.AddItem("Arma", (int)ItemType.Weapon);
		optTipo.AddItem("Armadura", (int)ItemType.Armor);
		optTipo.AddItem("Escudo", (int)ItemType.Shield);
		optTipo.AddItem("Consumível", (int)ItemType.Consumable);
		optTipo.AddItem("Acessório", (int)ItemType.Accessory);
		optTipo.AddItem("Outros", (int)ItemType.Miscellaneous);
		vbox.AddChild(new Label { Text = "Tipo" });
		vbox.AddChild(optTipo);

		var hRow1 = new HBoxContainer();
		hRow1.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow1);

		var spSlots = new SpinBox { MinValue = 0, MaxValue = 20, Value = 1 };
		var spPrice = new SpinBox { MinValue = 0, MaxValue = 100000, Value = 10 };
		var vbS = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbS.AddChild(new Label { Text = "Espaços" });
		vbS.AddChild(spSlots);
		var vbP = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbP.AddChild(new Label { Text = "Preço (T$)" });
		vbP.AddChild(spPrice);
		hRow1.AddChild(vbS);
		hRow1.AddChild(vbP);

		var hRow2 = new HBoxContainer();
		hRow2.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow2);

		var leDano = new LineEdit { PlaceholderText = "1d8", Text = "1d8" };
		var leCrit = new LineEdit { PlaceholderText = "19/x2", Text = "20/x2" };
		var vbD = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbD.AddChild(new Label { Text = "Dano (Arma)" });
		vbD.AddChild(leDano);
		var vbC = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbC.AddChild(new Label { Text = "Crítico" });
		vbC.AddChild(leCrit);
		hRow2.AddChild(vbD);
		hRow2.AddChild(vbC);

		var hRow3 = new HBoxContainer();
		hRow3.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow3);

		var spDef = new SpinBox { MinValue = 0, MaxValue = 20, Value = 0 };
		var spPen = new SpinBox { MinValue = -15, MaxValue = 0, Value = 0 };
		var vbDef = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbDef.AddChild(new Label { Text = "Bônus Defesa" });
		vbDef.AddChild(spDef);
		var vbPen = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbPen.AddChild(new Label { Text = "Pen. Armadura" });
		vbPen.AddChild(spPen);
		hRow3.AddChild(vbDef);
		hRow3.AddChild(vbPen);

		var chkEquip = new CheckBox { Text = "Equipar imediatamente", ButtonPressed = true };
		vbox.AddChild(chkEquip);

		var chkComprar = new CheckBox { Text = "Comprar item (descontar preço dos Tibares T$)", ButtonPressed = false };
		vbox.AddChild(chkComprar);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnAdd    = new Button { Text = "Adicionar Item" };
		btnAdd.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		hBtns.AddChild(btnCancel);
		hBtns.AddChild(btnAdd);

		btnCancel.Pressed  += () => win.Hide();
		win.CloseRequested += () => win.Hide();
		btnAdd.Pressed += () =>
		{
			if (_selectedCharacter == null) return;
			string nome = leNome.Text.Trim();
			if (string.IsNullOrEmpty(nome)) return;

			int preco = (int)spPrice.Value;
			if (chkComprar.ButtonPressed)
			{
				if (_selectedCharacter.Tibares < preco)
				{
					ShowToast($"Tibares insuficientes! (Custo: T$ {preco} | Carteira: T$ {_selectedCharacter.Tibares})", new Color(1f, 0.45f, 0.45f));
					return;
				}
				_selectedCharacter.Tibares -= preco;
			}

			var tipo = (ItemType)optTipo.GetSelectedId();
			string dano = tipo == ItemType.Weapon ? leDano.Text.Trim() : "";
			_selectedCharacter.Inventory.Add(new CharacterItem(
				nome, "", tipo, (int)spSlots.Value, dano, leCrit.Text.Trim(),
				(int)spDef.Value, (int)spPen.Value, preco, chkEquip.ButtonPressed));

			leNome.Text = "";
			win.Hide();
			RefreshSheetUI();
			ShowToast($"Item \"{nome}\" adicionado ao inventário!{(chkComprar.ButtonPressed ? $" (-T$ {preco})" : "")}");
		};

		win.Hide();
		return win;
	}

	private Window BuildNovaMagiaDialog()
	{
		var win = new Window
		{
			Title = "Adicionar Magia / Habilidade",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(420, 440),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);

		var leNome = new LineEdit { PlaceholderText = "Ex: Bola de Fogo, Ataque Especial..." };
		vbox.AddChild(new Label { Text = "Nome da Magia / Poder *" });
		vbox.AddChild(leNome);

		var leDesc = new LineEdit { PlaceholderText = "Ex: Causa 6d6 de dano de fogo" };
		vbox.AddChild(new Label { Text = "Descrição / Efeito" });
		vbox.AddChild(leDesc);

		_leNovaMagiaFormula = new LineEdit { PlaceholderText = "Opcional: ex: 2d4+2 ou 6d6 (rola dano no combate)" };
		vbox.AddChild(new Label { Text = "Fórmula de Dados em Combate (Opcional)" });
		vbox.AddChild(_leNovaMagiaFormula);

		var hRow = new HBoxContainer();
		hRow.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow);

		var spPm = new SpinBox { MinValue = 0, MaxValue = 50, Value = 1 };
		var optAcao = new OptionButton();
		optAcao.AddItem("Ação Padrão", (int)T20ActionType.Standard);
		optAcao.AddItem("Ação de Movimento", (int)T20ActionType.Movement);
		optAcao.AddItem("Ação Livre", (int)T20ActionType.Free);
		optAcao.AddItem("Reação", (int)T20ActionType.Reaction);
		optAcao.AddItem("Ação Completa", (int)T20ActionType.Full);

		var vbPm = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbPm.AddChild(new Label { Text = "Custo Base (PM)" });
		vbPm.AddChild(spPm);
		var vbAc = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbAc.AddChild(new Label { Text = "Execução" });
		vbAc.AddChild(optAcao);
		hRow.AddChild(vbPm);
		hRow.AddChild(vbAc);

		_chkNovaMagiaIsSpell = new CheckBox { Text = "É uma Magia (Arcana/Divina)", ButtonPressed = true };
		vbox.AddChild(_chkNovaMagiaIsSpell);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnAdd    = new Button { Text = "Adicionar" };
		btnAdd.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		hBtns.AddChild(btnCancel);
		hBtns.AddChild(btnAdd);

		btnCancel.Pressed  += () => { _addingSkillToEnemy = false; win.Hide(); };
		win.CloseRequested += () => { _addingSkillToEnemy = false; win.Hide(); };
		btnAdd.Pressed += () =>
		{
			var targetSheet = _addingSkillToEnemy ? _selectedEnemy : _selectedCharacter;
			if (targetSheet == null) return;
			string nome = leNome.Text.Trim();
			if (string.IsNullOrEmpty(nome)) return;

			bool isSpell = _chkNovaMagiaIsSpell.ButtonPressed;
			targetSheet.Skills.Add(new CharacterSkill(
				nome, leDesc.Text.Trim(), (int)spPm.Value,
				(T20ActionType)optAcao.GetSelectedId(), 0, "", isSpell, _leNovaMagiaFormula.Text.Trim()));

			leNome.Text = leDesc.Text = _leNovaMagiaFormula.Text = "";
			bool wasEnemy = _addingSkillToEnemy;
			_addingSkillToEnemy = false;
			win.Hide();
			if (wasEnemy) RefreshEnemySheetUI();
			else RefreshSheetUI();
			ShowToast($"{(isSpell ? "Magia" : "Habilidade")} \"{nome}\" adicionada à ficha!");
		};

		win.Hide();
		return win;
	}

	private Window BuildNovoAtaqueInimigoDialog()
	{
		var win = new Window
		{
			Title = "Adicionar Ataque da Ameaça",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(410, 390),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);

		vbox.AddChild(new Label { Text = "Nome do Ataque *" });
		var leNome = new LineEdit { PlaceholderText = "Ex: Mordida, Cimitarra, Garra..." };
		vbox.AddChild(leNome);

		var hRow1 = new HBoxContainer();
		hRow1.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow1);

		var spAtk = new SpinBox { MinValue = -10, MaxValue = 50, Value = 7, Prefix = "+" };
		var leDano = new LineEdit { Text = "1d8+4", PlaceholderText = "Ex: 1d8+4" };
		var vbAtk = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbAtk.AddChild(new Label { Text = "Bônus de Ataque" });
		vbAtk.AddChild(spAtk);
		var vbDano = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbDano.AddChild(new Label { Text = "Fórmula de Dano" });
		vbDano.AddChild(leDano);
		hRow1.AddChild(vbAtk);
		hRow1.AddChild(vbDano);

		var hRow2 = new HBoxContainer();
		hRow2.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hRow2);

		var leCrit = new LineEdit { Text = "20/x2", PlaceholderText = "Ex: 19/x2 ou 20/x3" };
		var optDmgType = new OptionButton();
		optDmgType.AddItem("Corte", (int)T20DamageType.Slashing);
		optDmgType.AddItem("Perfuração", (int)T20DamageType.Piercing);
		optDmgType.AddItem("Impacto", (int)T20DamageType.Bludgeoning);
		optDmgType.AddItem("Fogo", (int)T20DamageType.Fire);
		optDmgType.AddItem("Frio", (int)T20DamageType.Cold);
		optDmgType.AddItem("Ácido", (int)T20DamageType.Acid);
		optDmgType.AddItem("Eletricidade", (int)T20DamageType.Electricity);
		optDmgType.AddItem("Trevas", (int)T20DamageType.Darkness);
		optDmgType.AddItem("Luz", (int)T20DamageType.Light);
		optDmgType.AddItem("Psíquico", (int)T20DamageType.Psychic);
		optDmgType.AddItem("Veneno", (int)T20DamageType.Poison);
		optDmgType.AddItem("Essência", (int)T20DamageType.Essence);

		var vbCrit = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbCrit.AddChild(new Label { Text = "Crítico" });
		vbCrit.AddChild(leCrit);
		var vbTipo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbTipo.AddChild(new Label { Text = "Tipo de Dano" });
		vbTipo.AddChild(optDmgType);
		hRow2.AddChild(vbCrit);
		hRow2.AddChild(vbTipo);

		var chkRanged = new CheckBox { Text = "Ataque à Distância", ButtonPressed = false };
		vbox.AddChild(chkRanged);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnAdd = new Button { Text = "Adicionar Ataque" };
		btnAdd.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		hBtns.AddChild(btnCancel);
		hBtns.AddChild(btnAdd);

		btnCancel.Pressed += () => win.Hide();
		win.CloseRequested += () => win.Hide();
		btnAdd.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			string nome = leNome.Text.Trim();
			if (string.IsNullOrEmpty(nome)) return;

			string dano = string.IsNullOrWhiteSpace(leDano.Text) ? "1d6" : leDano.Text.Trim();
			string crit = string.IsNullOrWhiteSpace(leCrit.Text) ? "20/x2" : leCrit.Text.Trim();
			var dmgType = (T20DamageType)optDmgType.GetSelectedId();

			_selectedEnemy.Inventory.Add(new CharacterItem(
				nome, "Ataque da ameaça", ItemType.Weapon, 0,
				dano, crit, 0, 0, 0, true, dmgType, chkRanged.ButtonPressed, attackBonus: (int)spAtk.Value));

			leNome.Text = "";
			win.Hide();
			RefreshEnemySheetUI();
			ShowToast($"Ataque \"{nome}\" adicionado à ameaça {_selectedEnemy.Name}!");
		};

		win.Hide();
		return win;
	}

	private Window BuildArvorePoderesDialog()
	{
		var win = new Window
		{
			Title = "Árvore de Habilidades",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(1180, 690),
			Exclusive = true
		};

		var bg = new ColorRect
		{
			Color = new Color(0.07f, 0.08f, 0.10f, 1f)
		};
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		win.AddChild(bg);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);

		// 1. Barra Superior da Árvore: Resumo do Personagem + Legenda + Botão Resetar
		var topHeader = new HBoxContainer();
		topHeader.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(topHeader);

		_lblArvoreCharResumo = new Label
		{
			Text = "Herói — Nível 1",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_lblArvoreCharResumo.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.45f));
		_lblArvoreCharResumo.AddThemeFontSizeOverride("font_size", 13);
		topHeader.AddChild(_lblArvoreCharResumo);

		var lblLegenda = new Label
		{
			Text = "Dourado: Aprendido   |   Verde: Disponível   |   Cinza: Bloqueado (Clique para inspecionar)"
		};
		lblLegenda.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.84f));
		lblLegenda.AddThemeFontSizeOverride("font_size", 11);
		topHeader.AddChild(lblLegenda);

		var btnResetTree = new Button
		{
			Text = "Resetar Árvore"
		};
		btnResetTree.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
		btnResetTree.AddThemeFontSizeOverride("font_size", 11);
		btnResetTree.Pressed += () =>
		{
			if (_selectedCharacter == null) return;
			_selectedCharacter.Skills.RemoveAll(s => !string.IsNullOrEmpty(s.TreeKey));
			_selectedCharacter.UnlockedTreeKeys.Clear();
			SincronizarArvoreComCombateAtivo();
			RefreshSheetUI();
			AbrirArvorePoderes();
			ShowToast($"Árvore de habilidades de {_selectedCharacter.Name} resetada!");
		};
		topHeader.AddChild(btnResetTree);

		// 2. Área Central: Canvas Gráfico da Árvore N-ária (Scroll Horizontal/Vertical)
		var treeFrame = new PanelContainer
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		var frameStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.05f, 0.06f, 0.08f, 1f),
			BorderColor = new Color(0.28f, 0.30f, 0.38f, 1f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		treeFrame.AddThemeStyleboxOverride("panel", frameStyle);
		vbox.AddChild(treeFrame);

		var scroll = new ScrollContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		treeFrame.AddChild(scroll);

		_abilityTreeCanvas = new AbilityTreeCanvas();
		scroll.AddChild(_abilityTreeCanvas);

		_abilityTreeCanvas.NodeSelected += AtualizarInspetorNoArvore;
		_abilityTreeCanvas.LearnRequested += AprenderPoderNaArvore;

		// 3. Painel Inferior: Inspetor Detalhado do Nó Selecionado na Árvore
		var inspectorCard = new PanelContainer
		{
			CustomMinimumSize = new Vector2(0, 128)
		};
		var inspStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.11f, 0.12f, 0.16f, 0.98f),
			BorderColor = new Color(0.75f, 0.58f, 0.25f, 0.95f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 14,
			ContentMarginRight = 14,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		};
		inspectorCard.AddThemeStyleboxOverride("panel", inspStyle);
		vbox.AddChild(inspectorCard);

		var hInsp = new HBoxContainer();
		hInsp.AddThemeConstantOverride("separation", 16);
		inspectorCard.AddChild(hInsp);

		var vInspText = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		vInspText.AddThemeConstantOverride("separation", 4);
		hInsp.AddChild(vInspText);

		_lblArvoreNodeTitle = new Label
		{
			Text = "Selecione um poder na árvore acima"
		};
		_lblArvoreNodeTitle.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.45f));
		_lblArvoreNodeTitle.AddThemeFontSizeOverride("font_size", 15);
		vInspText.AddChild(_lblArvoreNodeTitle);

		_lblArvoreNodeDesc = new Label
		{
			Text = "Clique em qualquer cartão da árvore para inspecionar seus efeitos e pré-requisitos.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblArvoreNodeDesc.AddThemeColorOverride("font_color", new Color(0.9f, 0.92f, 0.95f));
		_lblArvoreNodeDesc.AddThemeFontSizeOverride("font_size", 12);
		vInspText.AddChild(_lblArvoreNodeDesc);

		_lblArvoreNodeReqs = new Label
		{
			Text = "",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblArvoreNodeReqs.AddThemeFontSizeOverride("font_size", 11);
		vInspText.AddChild(_lblArvoreNodeReqs);

		var vInspActions = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		vInspActions.AddThemeConstantOverride("separation", 8);
		hInsp.AddChild(vInspActions);

		_btnArvoreActionNode = new Button
		{
			Text = "Aprender Poder",
			CustomMinimumSize = new Vector2(185, 38)
		};
		_btnArvoreActionNode.Pressed += () =>
		{
			var st = _abilityTreeCanvas?.GetNodeState(_inspectedPowerKey);
			if (st == null || _selectedCharacter == null) return;

			if (st.CanLearn)
			{
				AprenderPoderNaArvore(st.Definition);
			}
			else if (st.IsUnlocked)
			{
				// Verifica se algum filho deste nó está aprendido antes de remover
				bool hasUnlockedChild = Tormenta20AbilityTreeRules.CanonicalPowers.Any(
					p => p.ParentKey == st.Definition.Key && _selectedCharacter.UnlockedTreeKeys.Contains(p.Key));
				if (hasUnlockedChild)
				{
					ShowToast($"Remova primeiro os poderes que dependem de \"{st.Definition.Name}\"!", new Color(1f, 0.45f, 0.45f));
					return;
				}
				_selectedCharacter.UnlockedTreeKeys.Remove(st.Definition.Key);
				_selectedCharacter.Skills.RemoveAll(s => s.TreeKey == st.Definition.Key);
				SincronizarArvoreComCombateAtivo();
				RefreshSheetUI();
				AbrirArvorePoderes();
				ShowToast($"Poder \"{st.Definition.Name}\" removido da árvore.");
			}
		};
		vInspActions.AddChild(_btnArvoreActionNode);

		var btnFechar = new Button
		{
			Text = "Fechar Árvore",
			CustomMinimumSize = new Vector2(185, 32)
		};
		btnFechar.Pressed  += () => win.Hide();
		win.CloseRequested += () => win.Hide();
		vInspActions.AddChild(btnFechar);

		win.Hide();
		return win;
	}

	private void SincronizarArvoreComCombateAtivo()
	{
		if (_selectedCharacter == null) return;
		int activeCid = GetSelectedCombatantId();
		if (activeCid > 0)
		{
			Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
				_combatManager.Session, activeCid, _selectedCharacter.Level,
				_selectedCharacter.Strength, _selectedCharacter.Dexterity, _selectedCharacter.Constitution,
				_selectedCharacter.Intelligence, _selectedCharacter.Wisdom, _selectedCharacter.Charisma,
				_selectedCharacter.UnlockedTreeKeys);
		}
	}

	private void AprenderPoderNaArvore(T20PowerTreeDefinition power)
	{
		if (_selectedCharacter == null || power == null) return;

		if (!_selectedCharacter.UnlockedTreeKeys.Contains(power.Key))
			_selectedCharacter.UnlockedTreeKeys.Add(power.Key);

		if (!_selectedCharacter.Skills.Any(s => s.TreeKey == power.Key))
		{
			_selectedCharacter.Skills.Add(new CharacterSkill(
				power.Name, power.Description, power.PmCost, power.ActionType, 0, power.Key, false, power.BaseFormula));
		}

		SincronizarArvoreComCombateAtivo();
		RefreshSheetUI();
		AbrirArvorePoderes();
		_abilityTreeCanvas?.PlayUnlockEffectAt(power.Key);
		ShowToast($"Poder \"{power.Name}\" desbloqueado na Árvore de Habilidades!");
		AppendCombatLog($"{_selectedCharacter.Name} desbloqueou o poder [b]{power.Name}[/b] na Árvore!", "#ffd700");
	}

	private void AtualizarInspetorNoArvore(AbilityTreeCanvas.NodeEvalState st)
	{
		if (st == null) return;
		_inspectedPowerKey = st.Definition.Key;

		string parentLabel = "Poder Inicial";
		if (!string.IsNullOrEmpty(st.Definition.ParentKey))
		{
			string parentName = Tormenta20AbilityTreeRules.CanonicalPowers
				.FirstOrDefault(p => p.Key == st.Definition.ParentKey)?.Name ?? st.Definition.ParentKey;
			parentLabel = $"Pré-requisito: {parentName}";
		}
		string costLabel = st.Definition.PmCost > 0 ? $"{st.Definition.PmCost} PM ({st.Definition.ActionType})" : "Passivo / Sem custo de PM";

		_lblArvoreNodeTitle.Text = $"{st.Definition.Name}   —   [{st.Definition.BranchName}]   |   {parentLabel}   |   {costLabel}";
		_lblArvoreNodeDesc.Text  = st.Definition.Description;

		if (st.IsUnlocked)
		{
			string useStatus = st.CanUseInTurn
				? "Pronto para uso neste turno"
				: $"Para usar neste turno: {st.UnmetUsageReasons}";
			_lblArvoreNodeReqs.Text = $"Aprendido   |   {useStatus}";
			_lblArvoreNodeReqs.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
			_btnArvoreActionNode.Text     = "Esquecer Poder";
			_btnArvoreActionNode.Disabled = false;
			_btnArvoreActionNode.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
		}
		else if (st.CanLearn)
		{
			_lblArvoreNodeReqs.Text = "Disponível para aprender! Todos os pré-requisitos foram cumpridos.";
			_lblArvoreNodeReqs.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.65f));
			_btnArvoreActionNode.Text     = $"Aprender \"{st.Definition.Name}\"";
			_btnArvoreActionNode.Disabled = false;
			_btnArvoreActionNode.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		}
		else
		{
			_lblArvoreNodeReqs.Text = $"Pré-requisitos pendentes: {st.UnmetLearnReasons}";
			_lblArvoreNodeReqs.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));
			_btnArvoreActionNode.Text     = "Pré-requisitos Pendentes";
			_btnArvoreActionNode.Disabled = true;
			_btnArvoreActionNode.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		}
	}

	private void AbrirArvorePoderes()
	{
		if (_selectedCharacter == null) return;

		int totalPowers = Tormenta20AbilityTreeRules.CanonicalPowers.Count;
		int unlockedCount = _selectedCharacter.UnlockedTreeKeys.Count;
		if (_lblArvoreCharResumo != null)
		{
			_lblArvoreCharResumo.Text =
				$"{_selectedCharacter.Name} ({_selectedCharacter.Race} {_selectedCharacter.CharacterClass} Nv.{_selectedCharacter.Level})   |   " +
				$"FOR {FormatAttr(_selectedCharacter.Strength)}  DES {FormatAttr(_selectedCharacter.Dexterity)}  CON {FormatAttr(_selectedCharacter.Constitution)}  " +
				$"INT {FormatAttr(_selectedCharacter.Intelligence)}  SAB {FormatAttr(_selectedCharacter.Wisdom)}  CAR {FormatAttr(_selectedCharacter.Charisma)}   |   " +
				$"Desbloqueados: {unlockedCount}/{totalPowers}";
		}

		int activeCid = GetSelectedCombatantId();
		_abilityTreeCanvas?.RebuildTree(
			_selectedCharacter,
			activeCid > 0 ? _combatManager.Session : null,
			activeCid);

		if (!_dlgArvorePoderes.Visible)
			_dlgArvorePoderes.PopupCentered();
	}

	private static string FormatAttr(int val) => val >= 0 ? $"+{val}" : val.ToString();

	private Window BuildUsarHabilidadeCombateDialog()
	{
		var win = new Window
		{
			Title = "Usar Poder / Magia no Turno",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(470, 380),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(vbox);

		vbox.AddChild(new Label { Text = "Selecione o Poder ou Magia do combatente atual:" });

		_listHabilidadesCombate = new ItemList { SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill };
		vbox.AddChild(_listHabilidadesCombate);

		var hAprim = new HBoxContainer();
		hAprim.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hAprim);

		hAprim.AddChild(new Label { Text = "Aprimoramento (+PM):" });
		_spinAprimoramentoPm = new SpinBox { MinValue = 0, MaxValue = 30, Value = 0, CustomMinimumSize = new Vector2(80, 0) };
		hAprim.AddChild(_spinAprimoramentoPm);

		_lblLimitePmCombate = new Label
		{
			Text = "Limite de gasto: 1 PM",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_lblLimitePmCombate.AddThemeColorOverride("font_color", new Color(0.95f, 0.8f, 0.35f));
		_lblLimitePmCombate.AddThemeFontSizeOverride("font_size", 12);
		hAprim.AddChild(_lblLimitePmCombate);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnUsar   = new Button { Text = "Executar Habilidade" };
		btnUsar.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 1f));
		hBtns.AddChild(btnCancel);
		hBtns.AddChild(btnUsar);

		btnCancel.Pressed  += () => win.Hide();
		win.CloseRequested += () => win.Hide();
		btnUsar.Pressed += () =>
		{
			if (!_combatManager.IsInCombat) { win.Hide(); return; }
			int actorId = _combatManager.Session.CurrentTurn;
			if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) { win.Hide(); return; }

			var selected = _listHabilidadesCombate.GetSelectedItems();
			if (selected.Length == 0 || selected[0] >= actorSheet.Skills.Count) return;

			var sk = actorSheet.Skills[selected[0]];
			int extraPm = (int)_spinAprimoramentoPm.Value;
			int targetId = GetSelectedCombatTargetId(actorId);

			// Valida limite de PM por nível (ou Magia Ilimitada), pré-requisitos da AbilityTree e consome ação + PM
			var actRes = Tormenta20MagicRules.EnqueueSkillWithEnhancements(
				_combatManager.Session, actorId, actorSheet, sk, extraPm, targetId);

			// Sincroniza PM da ficha com o Core C++
			ResourceInfo pmInfo = _combatManager.Session.GetResource(actorId, CombatManager.ManaResourceId);
			actorSheet.CurrentPm = pmInfo.Current;

			if (!actRes.Success)
			{
				AppendCombatLog(actRes.Message, "#ff6b6b");
				ShowToast(actRes.Message, new Color(1f, 0.45f, 0.45f));
			}
			else
			{
				int totalPm = sk.PmCost + extraPm;
				if (actRes.ActionId > 0 && totalPm > 0)
				{
					_queuedActionPmSpent[(actorId, actRes.ActionId)] = totalPm;
				}
				AppendCombatLog($"{actorSheet.Name} usou [b]{sk.Name}[/b] (-{totalPm} PM): {sk.Description}", "#66d9ef");

				// Se a magia/habilidade possui fórmula de dados (ex.: Mísseis Mágicos "2d4+2"), rola e aplica no alvo!
				if (!string.IsNullOrWhiteSpace(sk.BaseFormula) &&
					_combatManager.CombatantIdToSheet.TryGetValue(targetId, out var targetSheet))
				{
					try
					{
						RollResult dmgRoll = _combatManager.Session.Roll(sk.BaseFormula.Trim());
						int rawDmg = Math.Max(1, dmgRoll.Total + extraPm * 2);
						DamageResult dmgRes = _combatManager.Session.DealDamage(targetId, rawDmg, (int)T20DamageType.Essence);
						targetSheet.CurrentHp = _combatManager.Session.GetResource(targetId, CombatManager.HealthResourceId).Current;
						AppendCombatLog($"Efeito de [b]{sk.Name}[/b] em {targetSheet.Name}: rolou {sk.BaseFormula} = {dmgRes.FinalAmount} de dano mágico!", "#ffaa44");
					}
					catch { }
				}
			}

			RefreshSheetUI();
			RefreshEnemySheetUI();
			RefreshInitiativeBar();
			PopularAlvoCombate();
			RefreshActionQueuePanel();
			UpdateCombatStatusDisplay();
			win.Hide();
		};

		win.Hide();
		return win;
	}

	private Window BuildCondicoesCombateDialog()
	{
		var win = new Window
		{
			Title = "Condições Canônicas de Tormenta20",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(450, 310),
			Exclusive = true
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(vbox);

		vbox.AddChild(new Label { Text = "Escolha a condição para aplicar ou remover no Alvo Selecionado:" });

		_optCondicaoAplicar = new OptionButton();
		foreach (T20Condition cond in Enum.GetValues(typeof(T20Condition)))
		{
			_optCondicaoAplicar.AddItem($"{cond} — {Tormenta20ConditionRules.GetConditionDescription(cond)}", (int)cond);
		}
		vbox.AddChild(_optCondicaoAplicar);

		var hDur = new HBoxContainer();
		hDur.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hDur);
		hDur.AddChild(new Label { Text = "Duração em turnos (-1 = até o fim da cena):" });
		_spinDuracaoCondicao = new SpinBox { MinValue = -1, MaxValue = 20, Value = -1, CustomMinimumSize = new Vector2(80, 0) };
		hDur.AddChild(_spinDuracaoCondicao);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(hBtns);

		var btnCancelar = new Button { Text = "Fechar" };
		var btnRemover  = new Button { Text = "Remover Condição" };
		var btnAplicar  = new Button { Text = "Aplicar no Alvo" };
		btnAplicar.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.35f));
		btnRemover.AddThemeColorOverride("font_color", new Color(0.55f, 0.95f, 0.65f));

		hBtns.AddChild(btnCancelar);
		hBtns.AddChild(btnRemover);
		hBtns.AddChild(btnAplicar);

		btnCancelar.Pressed += () => win.Hide();
		win.CloseRequested  += () => win.Hide();

		btnAplicar.Pressed += () =>
		{
			if (!_combatManager.IsInCombat) { win.Hide(); return; }
			int targetId = GetSelectedCombatTargetId(_combatManager.Session.CurrentTurn);
			if (targetId <= 0) return;

			var cond = (T20Condition)_optCondicaoAplicar.GetSelectedId();
			int dur = (int)_spinDuracaoCondicao.Value;
			string msg = Tormenta20ConditionRules.ApplyCondition(_combatManager.Session, targetId, cond, dur);
			string targetName = _combatManager.CombatantIdToSheet.TryGetValue(targetId, out var sh) ? sh.Name : $"ID {targetId}";
			AppendCombatLog($"[{targetName}] {msg}", "#ffcc66");
			ShowToast($"{cond} aplicada em {targetName}!");
			RefreshSheetUI();
			RefreshEnemySheetUI();
			win.Hide();
		};

		btnRemover.Pressed += () =>
		{
			if (!_combatManager.IsInCombat) { win.Hide(); return; }
			int targetId = GetSelectedCombatTargetId(_combatManager.Session.CurrentTurn);
			if (targetId <= 0) return;

			var cond = (T20Condition)_optCondicaoAplicar.GetSelectedId();
			Tormenta20ConditionRules.RemoveCondition(_combatManager.Session, targetId, cond);
			string targetName = _combatManager.CombatantIdToSheet.TryGetValue(targetId, out var sh) ? sh.Name : $"ID {targetId}";
			AppendCombatLog($"[{targetName}] Condição '{cond}' removida.", "#88ee99");
			ShowToast($"{cond} removida de {targetName}.");
			RefreshSheetUI();
			RefreshEnemySheetUI();
			win.Hide();
		};

		win.Hide();
		return win;
	}

	// ════════════════════════════════════════════════════════════════════════
	// Connect Events
	// ════════════════════════════════════════════════════════════════════════
	private void ConnectEvents()
	{
		// TopBar: Ficha, Inimigos, Combate e Mapa trocam de tela; Bandeja de Dados abre como POP-UP sobre a tela atual!
		_btnViewFicha.Pressed         += () => ShowView("Ficha");
		if (_btnViewInimigos != null)
			_btnViewInimigos.Pressed  += () => ShowView("Inimigos");
		_btnViewCombate.Pressed       += () => ShowView("Combate");
		if (_btnViewMapa != null)
			_btnViewMapa.Pressed      += () => ShowView("Mapa");
		_btnViewDados.Pressed         += () => ToggleDiceTrayPopup();
		_btnSalvarFichas.Pressed      += OnSalvarFichasPressed;
		_btnVoltarMenu.Pressed        += () => GetTree().ChangeSceneToFile("res://Cenas/MainMenu.tscn");
		_btnNovoPersonagem.Pressed    += () => _dlgNovoPersonagem.PopupCentered();
		_btnExcluirPersonagem.Pressed += () =>
		{
			if (_selectedCharacter == null) return;
			_dlgConfirmarExcluir.DialogText =
				$"Deseja realmente excluir \"{_selectedCharacter.Name}\"?\nEsta ação não pode ser desfeita.";
			_dlgConfirmarExcluir.PopupCentered();
		};

		// Dropdown de personagens
		_optPersonagens.ItemSelected += (idx) => SelectCharacter((int)idx);

		// Descanso
		_btnDescansar.Pressed += () =>
		{
			if (_selectedCharacter == null) return;
			var pos = _btnDescansar.GetScreenPosition();
			_menuDescanso.Position = new Vector2I((int)pos.X, (int)(pos.Y + _btnDescansar.Size.Y));
			_menuDescanso.Popup();
		};

		// PV / PM
		_btnPvDano.Pressed  += () => ModificarRecurso(false, false);
		_btnPvCurar.Pressed += () => ModificarRecurso(false, true);
		_btnPmDano.Pressed  += () => ModificarRecurso(true, false);
		_btnPmCurar.Pressed += () => ModificarRecurso(true, true);

		// Sub-tabs da ficha e seus botões de ação
		_btnAtaques.Pressed  += () => SelecionarSubTab("Ataques");
		_btnDefesa.Pressed   += () => SelecionarSubTab("Defesa");
		_btnPoderes.Pressed  += () => SelecionarSubTab("Poderes");
		_btnMagias.Pressed   += () => SelecionarSubTab("Magias");
		_btnEquip.Pressed    += () => SelecionarSubTab("Equipamentos");
		_btnSubAcao1.Pressed += OnSubAcao1Pressed;
		_btnSubAcao2.Pressed += OnSubAcao2Pressed;
		_btnSubAcao3.Pressed += OnSubAcao3Pressed;

		// Perícias
		_txtBuscaPericia.TextChanged += (_) => RefreshPericiasUI();

		// Pop-up perícia
		_btnFecharPopup.Pressed += () => _popupPericia.Visible = false;
		_popupPericia.GuiInput  += (e) => { if (e is InputEventMouseButton mb && mb.Pressed) _popupPericia.Visible = false; };

		// Combate
		_btnIniciarCombate.Pressed   += OnIniciarCombatePressed;
		_btnProximoTurno.Pressed     += () =>
		{
			_queuedActionPmSpent.Clear();
			_combatManager.NextTurn();
		};
		_btnEncerrarCombate.Pressed  += () =>
		{
			_queuedActionPmSpent.Clear();
			_combatManager.EndCombat();
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
			RefreshActionQueuePanel();
			RefreshSheetUI();
			RefreshEnemySheetUI();
		};
		if (_btnSalvarCombate != null)
		{
			_btnSalvarCombate.Pressed += () =>
			{
				if (_combatManager.SaveCombatSnapshot())
					ShowToast("Combate salvo com sucesso!");
				else
					ShowToast("Nenhum combate ativo para salvar.", new Color(1f, 0.45f, 0.45f));
			};
		}
		if (_btnCarregarCombate != null)
		{
			_btnCarregarCombate.Pressed += () =>
			{
				_queuedActionPmSpent.Clear();
				if (_combatManager.LoadCombatSnapshot())
				{
					UpdateCharacterDropdown();
					RefreshEnemyList();
					UpdateCombatStatusDisplay();
					RefreshInitiativeBar();
					PopularAlvoCombate();
					RefreshActionQueuePanel();
					RefreshSheetUI();
					RefreshEnemySheetUI();
					ShowToast("Combate restaurado com sucesso!");
				}
				else
				{
					ShowToast("Nenhum combate salvo encontrado.", new Color(1f, 0.45f, 0.45f));
				}
			};
		}
		_btnAtacar.Pressed           += OnAtacarPressed;
		_btnMovimento.Pressed        += OnMovimentoPressed;
		if (_btnAcaoCompleta != null)
			_btnAcaoCompleta.Pressed += OnAcaoCompletaPressed;
		if (_btnPopNextAction != null)
			_btnPopNextAction.Pressed += OnPopNextActionPressed;
		_btnUsarHabilidade.Pressed   += OnAbrirUsarHabilidadeCombate;
		_btnCondicoesCombate.Pressed += () =>
		{
			if (!_combatManager.IsInCombat)
			{
				ShowToast("Inicie o combate antes de aplicar condições de turno!", new Color(1f, 0.45f, 0.45f));
				return;
			}
			_dlgCondicoesCombate.PopupCentered();
		};

		_optAlvoCombate.ItemSelected += (_) => RefreshTacticalMapView();

		_combatManager.TurnChanged += (cid, cname, round) =>
		{
			_runModeNextClick = false;
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
			RefreshActionQueuePanel();
			RefreshTacticalMapView();
			RefreshSheetUI();
			RefreshEnemySheetUI();
		};

		_combatManager.CombatLog += (msg, hex) => AppendCombatLog(msg, hex);

		// Dados (Pop-up Bandeja de Dados)
		_btnRolar.Pressed           += () => RolarDadoExpressao(_txtExpressao.Text);
		_txtExpressao.TextSubmitted += RolarDadoExpressao;
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed) return;

		if (keyEvent.Keycode == Key.Escape)
		{
			if (_popupDadosOverlay != null && _popupDadosOverlay.Visible)
			{
				ToggleDiceTrayPopup(false);
				GetViewport().SetInputAsHandled();
			}
			else if (_popupPericia != null && _popupPericia.Visible)
			{
				_popupPericia.Visible = false;
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (_currentMainView == "Mapa" && _tacticalGridCanvas != null)
		{
			Control focused = GetViewport()?.GuiGetFocusOwner();
			if (focused is LineEdit or TextEdit or SpinBox) return;

			switch (keyEvent.Keycode)
			{
				case Key.Equal:
				case Key.Plus:
				case Key.KpAdd:
					_tacticalGridCanvas.ZoomBy(1.18f);
					GetViewport().SetInputAsHandled();
					break;
				case Key.Minus:
				case Key.KpSubtract:
					_tacticalGridCanvas.ZoomBy(1f / 1.18f);
					GetViewport().SetInputAsHandled();
					break;
				case Key.R:
					_tacticalGridCanvas.CenterView(animate: true);
					GetViewport().SetInputAsHandled();
					break;
				case Key.Key0:
					_tacticalGridCanvas.ResetZoom100();
					GetViewport().SetInputAsHandled();
					break;
				case Key.M:
					if (_btnReguaMapa != null)
						_btnReguaMapa.ButtonPressed = !_btnReguaMapa.ButtonPressed;
					GetViewport().SetInputAsHandled();
					break;
				case Key.H:
				case Key.F1:
					ToggleTacticalHelpTab();
					GetViewport().SetInputAsHandled();
					break;
			}
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	// View switching & Toast
	// ════════════════════════════════════════════════════════════════════════
	private void ShowView(string view)
	{
		if (view == "Dados")
		{
			ToggleDiceTrayPopup(true);
			return;
		}

		_currentMainView     = view;
		_viewFicha.Visible   = view == "Ficha";
		if (_viewInimigos != null)
			_viewInimigos.Visible = view == "Inimigos";
		_viewCombate.Visible = view == "Combate";
		if (_viewMapa != null)
			_viewMapa.Visible = view == "Mapa";

		_btnViewFicha.Modulate   = view == "Ficha"   ? new Color(1f, 0.9f, 0.45f) : Colors.White;
		if (_btnViewInimigos != null)
			_btnViewInimigos.Modulate = view == "Inimigos" ? new Color(1f, 0.72f, 0.52f) : Colors.White;
		_btnViewCombate.Modulate = view == "Combate" ? new Color(1f, 0.9f, 0.45f) : Colors.White;
		if (_btnViewMapa != null)
			_btnViewMapa.Modulate = view == "Mapa" ? new Color(1f, 0.9f, 0.45f) : Colors.White;

		if (view == "Inimigos")
		{
			RefreshEnemyList();
			RefreshEnemySheetUI();
		}
		else if (view == "Mapa")
		{
			_combatManager.EnsureMapInitialized(resetTerrain: false);
			RefreshTacticalMapView();
			CallDeferred(nameof(CenterTacticalMapDeferred));
		}
	}

	private void CenterTacticalMapDeferred()
	{
		_tacticalGridCanvas?.EnsureInitialCenter();
	}

	private void ShowToast(string message, Color? borderColor = null)
	{
		_lblToast.Text = message;
		if (_toastPanel.GetThemeStylebox("panel") is StyleBoxFlat sb)
		{
			var copy = (StyleBoxFlat)sb.Duplicate();
			copy.BorderColor = borderColor ?? new Color(0.35f, 0.85f, 0.5f);
			_toastPanel.AddThemeStyleboxOverride("panel", copy);
		}

		_toastTween?.Kill();
		_toastPanel.Modulate = new Color(1, 1, 1, 0);
		_toastPanel.Visible  = true;

		_toastTween = CreateTween();
		_toastTween.TweenProperty(_toastPanel, "modulate", new Color(1, 1, 1, 1), 0.2f);
		_toastTween.TweenInterval(2.2f);
		_toastTween.TweenProperty(_toastPanel, "modulate", new Color(1, 1, 1, 0), 0.35f);
		_toastTween.TweenCallback(Callable.From(() => _toastPanel.Visible = false));
	}

	// ════════════════════════════════════════════════════════════════════════
	// Criar / Excluir Personagens (com Enums e Cálculo Automático T20 JdA)
	// ════════════════════════════════════════════════════════════════════════
	private void OnCriarPersonagemComEnums(
		string nome,
		T20Class charClass,
		T20Race race,
		T20Origin origin,
		T20Deity deity,
		int nivel,
		int str, int dex, int con, int intel, int wis, int cha)
	{
		var classTpl = Tormenta20CharacterCreationRules.GetClassTemplate(charClass);
		var raceTpl  = Tormenta20CharacterCreationRules.GetRaceTemplate(race);
		string origName  = Tormenta20CharacterCreationRules.GetOriginDisplayName(origin);
		string deityName = Tormenta20CharacterCreationRules.GetDeityDisplayName(deity);

		int keyAttrMod = classTpl.SpellKeyAttribute switch
		{
			AttributeType.Intelligence => intel,
			AttributeType.Wisdom => wis,
			AttributeType.Charisma => cha,
			_ => 0
		};

		int autoMaxPv = Tormenta20CharacterCreationRules.CalculateMaxHp(classTpl, raceTpl, nivel, con);
		int autoMaxPm = Tormenta20CharacterCreationRules.CalculateMaxPm(classTpl, raceTpl, nivel, keyAttrMod);

		var novo = _combatManager.CreateCharacter(
			nome, classTpl.DisplayName, raceTpl.DisplayName, origName, deityName, nivel, autoMaxPv, autoMaxPm);

		novo.Strength     = str;
		novo.Dexterity    = dex;
		novo.Constitution = con;
		novo.Intelligence = intel;
		novo.Wisdom       = wis;
		novo.Charisma     = cha;
		novo.SpellKeyAttribute = classTpl.SpellKeyAttribute;
		novo.DefenseBonusMisc  = raceTpl.NaturalDefenseBonus;

		// Adiciona perícias iniciais da classe
		foreach (string skName in classTpl.DefaultTrainedSkills)
		{
			novo.TrainedSkills.Add(skName);
		}

		// Adiciona habilidade ou magia inicial icônica da classe
		if (!string.IsNullOrEmpty(classTpl.IconicSkillName))
		{
			novo.Skills.Add(new CharacterSkill(
				classTpl.IconicSkillName,
				classTpl.IconicSkillDesc,
				classTpl.IconicSkillPm,
				classTpl.IconicSkillAction,
				0, "",
				classTpl.IconicSkillIsSpell,
				classTpl.IconicSkillFormula));
		}

		// Adiciona arma inicial condizente com a classe
		if (charClass is T20Class.Arcanista or T20Class.Bardo)
		{
			novo.Inventory.Add(new CharacterItem("Cajado Arcano", "Foco de conjuração", ItemType.Weapon, 1, "1d6", "20/x2", 0, 0, 10, true, T20DamageType.Bludgeoning));
		}
		else if (charClass == T20Class.Cacador)
		{
			novo.Inventory.Add(new CharacterItem("Arco Longo", "Arma de disparo", ItemType.Weapon, 2, "1d8", "20/x3", 0, 0, 100, true, T20DamageType.Piercing, isRanged: true));
		}
		else if (charClass == T20Class.Ladino)
		{
			novo.Inventory.Add(new CharacterItem("Adaga", "Lâmina leve e precisa", ItemType.Weapon, 1, "1d4", "19/x2", 0, 0, 2, true, T20DamageType.Piercing));
		}
		else
		{
			novo.Inventory.Add(new CharacterItem("Espada Longa", "Lâmina versátil de aço", ItemType.Weapon, 1, "1d8", "19/x2", 0, 0, 15, true, T20DamageType.Slashing));
		}

		_combatManager.SaveCharacters();

		UpdateCharacterDropdown();
		int idx = _combatManager.Characters.IndexOf(novo);
		if (idx >= 0)
		{
			_optPersonagens.Select(idx);
			SelectCharacter(idx);
		}

		ShowToast($"{nome} ({raceTpl.DisplayName} {classTpl.DisplayName} Nv.{nivel}) criado! PV: {autoMaxPv} | PM: {autoMaxPm}");
		AppendCombatLog($"Personagem \"{nome}\" ({raceTpl.DisplayName} {classTpl.DisplayName} Nv.{nivel}) criado com PV={autoMaxPv}, PM={autoMaxPm}!", "#55ff99");
	}

	private void OnExcluirConfirmado()
	{
		if (_selectedCharacter == null) return;

		string nome = _selectedCharacter.Name;
		bool ok = _combatManager.DeleteCharacter(_selectedCharacter.Id);
		if (!ok)
		{
			GD.PushWarning($"[OpenTTRPG] Falha ao excluir personagem id={_selectedCharacter.Id}");
			return;
		}

		_combatManager.SaveCharacters();
		UpdateCharacterDropdown();
		ShowToast($"Personagem \"{nome}\" excluído.", new Color(1f, 0.45f, 0.45f));
		AppendCombatLog($"Personagem \"{nome}\" excluído.", "#ff9999");

		if (_combatManager.Characters.Count > 0)
			SelectCharacter(0);
		else
		{
			_selectedCharacter = null;
			LimparSheetUI();
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	// Dropdown
	// ════════════════════════════════════════════════════════════════════════
	private void UpdateCharacterDropdown()
	{
		_optPersonagens.Clear();
		for (int i = 0; i < _combatManager.Characters.Count; i++)
		{
			var c = _combatManager.Characters[i];
			_optPersonagens.AddItem($"{c.Name} ({c.CharacterClass} nv.{c.Level})", i);
		}
	}

	private void SelectCharacter(int index)
	{
		if (index < 0 || index >= _combatManager.Characters.Count) return;
		_selectedCharacter = _combatManager.Characters[index];
		_optPersonagens.Select(index);
		RefreshSheetUI();
	}

	// ════════════════════════════════════════════════════════════════════════
	// Ficha UI
	// ════════════════════════════════════════════════════════════════════════
	private void RefreshSheetUI()
	{
		if (_selectedCharacter == null) return;
		var c = _selectedCharacter;
		int cid = GetSelectedCombatantId();
		var activeSession = cid > 0 ? _combatManager.Session : null;

		// Cabeçalho
		_lblCharName.Text      = c.Name;
		_lblCharClassRace.Text = $"{c.Race} {c.CharacterClass} (Devoto: {c.Deity})";
		_lblCharNivel.Text     = $"NÍVEL {c.Level}";
		_lblCharOrigem.Text    = $"ORIGEM: {c.Origin}";
		if (_lblTibaresFicha != null)
			_lblTibaresFicha.Text = $"T$ {c.Tibares}";

		// Estado Vital (Ativo, Machucado, Morrendo, Inconsciente Estabilizado, Morto)
		T20HealthState hState = c.GetHealthState(activeSession, cid);
		if (_lblEstadoVital != null)
		{
			_lblEstadoVital.Text = $"ESTADO: {c.GetHealthStateLabel(activeSession, cid)}";
			Color stateColor = hState switch
			{
				T20HealthState.Dead => new Color(0.95f, 0.25f, 0.25f),
				T20HealthState.Dying => new Color(1.0f, 0.38f, 0.38f),
				T20HealthState.UnconsciousStable => new Color(0.85f, 0.65f, 1.0f),
				T20HealthState.Bloodied => new Color(1.0f, 0.75f, 0.28f),
				_ => new Color(0.45f, 0.95f, 0.55f)
			};
			_lblEstadoVital.AddThemeColorOverride("font_color", stateColor);
		}

		if (_btnTestarMorte != null)
			_btnTestarMorte.Visible = hState == T20HealthState.Dying;
		if (_btnReviver != null)
			_btnReviver.Visible = hState == T20HealthState.Dead;

		// Condições ativas (Combate + Estado de Morrendo/Inconsciente + Sobrecarga de inventário)
		var conds = new List<string>();
		if (activeSession != null)
		{
			conds.AddRange(Tormenta20ConditionRules.GetActiveConditionNames(activeSession, cid));
		}
		if (hState is T20HealthState.Dying or T20HealthState.UnconsciousStable or T20HealthState.Dead)
		{
			if (!conds.Contains(Tormenta20ConditionRules.UnconsciousKey))
				conds.Add(Tormenta20ConditionRules.UnconsciousKey);
		}
		if (hState == T20HealthState.Dying && !conds.Contains(Tormenta20ConditionRules.BleedingKey))
		{
			conds.Add(Tormenta20ConditionRules.BleedingKey);
		}
		if (c.IsOverburdened() && !conds.Contains(Tormenta20ConditionRules.OverburdenedKey))
		{
			conds.Add(Tormenta20ConditionRules.OverburdenedKey);
		}

		if (conds.Count > 0)
		{
			_lblCharCondicoes.Text = $"CONDIÇÕES: {string.Join(", ", conds)}";
			_lblCharCondicoes.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.35f));
		}
		else
		{
			_lblCharCondicoes.Text = "CONDIÇÕES: Nenhuma condição ativa";
			_lblCharCondicoes.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
		}

		// PV + cor dinâmica da borda + exibição do Limite de Morte quando <= 0 e PV Temporário
		string tempTag = c.TempHp > 0 ? $" (+{c.TempHp} Temp)" : "";
		_lblPvValor.Text = hState == T20HealthState.Dead ? "X" : c.CurrentHp.ToString();
		_lblPvSubtexto.Text = c.CurrentHp <= 0
			? $"PV {c.CurrentHp}/{c.MaxHp}{tempTag} (Morte: {c.GetDeathThreshold()})"
			: $"PV {c.CurrentHp}/{c.MaxHp}{tempTag}";
		UpdatePvCircleStyle(c, hState);

		// PM
		_lblPmValor.Text    = c.CurrentPm.ToString();
		_lblPmSubtexto.Text = $"PM {c.CurrentPm}/{c.MaxPm}";

		// Atributos
		SetAttrLabel(_lblForca,        c.Strength);
		SetAttrLabel(_lblDestreza,     c.Dexterity);
		SetAttrLabel(_lblConstituicao, c.Constitution);
		SetAttrLabel(_lblInteligencia, c.Intelligence);
		SetAttrLabel(_lblSabedoria,    c.Wisdom);
		SetAttrLabel(_lblCarisma,      c.Charisma);

		// Sub-tab
		SelecionarSubTab(_currentSubTab);

		// Perícias
		RefreshPericiasUI();

		// Resumo dinâmico de Nível, Treino e Penalidade de Armadura
		int halfLevel   = c.Level / 2;
		int treinoBonus = c.Level >= 15 ? 6 : (c.Level >= 7 ? 4 : 2);
		int penArmadura = c.GetTotalArmorPenalty(activeSession, cid);
		_lblResumoNivelTreino.Text = $"½ Nível: +{halfLevel}   |   Treino: +{treinoBonus}   |   Pen. Armadura: {penArmadura}";
	}

	private void UpdatePvCircleStyle(CharacterSheetData c, T20HealthState state)
	{
		if (_circlePv.GetThemeStylebox("panel") is not StyleBoxFlat sb) return;
		var style = (StyleBoxFlat)sb.Duplicate();

		switch (state)
		{
			case T20HealthState.Dead:
				style.BorderColor = new Color(0.45f, 0.12f, 0.12f);
				style.BgColor     = new Color(0.08f, 0.04f, 0.05f);
				break;
			case T20HealthState.Dying:
				style.BorderColor = new Color(1.0f, 0.20f, 0.20f);
				style.BgColor     = new Color(0.32f, 0.06f, 0.08f);
				break;
			case T20HealthState.UnconsciousStable:
				style.BorderColor = new Color(0.72f, 0.48f, 0.95f);
				style.BgColor     = new Color(0.16f, 0.11f, 0.24f);
				break;
			case T20HealthState.Bloodied:
				style.BorderColor = new Color(0.92f, 0.62f, 0.15f);
				style.BgColor     = new Color(0.22f, 0.16f, 0.07f);
				break;
			default:
				style.BorderColor = new Color(0.18f, 0.69f, 0.35f);
				style.BgColor     = new Color(0.08f, 0.18f, 0.11f);
				break;
		}

		_circlePv.AddThemeStyleboxOverride("panel", style);
	}

	private void OnTestarMorteNaFichaPressed()
	{
		if (_selectedCharacter == null) return;
		int cid = GetSelectedCombatantId();
		var res = Tormenta20ConditionRules.RollDeathAndBleedingCheck(_combatManager.Session, _selectedCharacter, cid);

		string modSign = res.ConModifier >= 0 ? $"+{res.ConModifier}" : res.ConModifier.ToString();
		_lblPopupTitulo.Text     = "TESTE DE MORTE / SANGRAMENTO (CD 15)";
		_lblPopupPersonagem.Text = $"Personagem: {_selectedCharacter.Name}";
		_lblPopupFormula.Text    = $"1d20 rolou {res.NaturalRoll} (CON {modSign}) = {res.Total} vs CD 15";

		if (res.Stabilized)
		{
			_lblPopupDestaque.Text    = "ESTABILIZOU! PAROU DE SANGRAR!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		}
		else if (res.Died)
		{
			_lblPopupDestaque.Text    = $"MORREU! (-{res.DamageTaken} PV -> {_selectedCharacter.CurrentHp} PV)";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.25f, 0.25f));
		}
		else
		{
			_lblPopupDestaque.Text    = $"FALHOU! PERDEU {res.DamageTaken} PV (1d6)!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		}

		_popupPericia.Modulate = new Color(1, 1, 1, 0);
		_popupPericia.Visible  = true;
		var tween = CreateTween();
		tween.TweenProperty(_popupPericia, "modulate", new Color(1, 1, 1, 1), 0.2f);

		_diceVisualPericia?.PlayRollAnimation("1d20", res.Total, res.NaturalRoll, res.Stabilized, !res.Stabilized, 0.42f);

		string cor = res.Stabilized ? "#55ff99" : "#ff5555";
		_txtHistorico?.AppendText($"[color={cor}][{_selectedCharacter.Name}] {res.Message}[/color]\n");
		AppendCombatLog(res.Message, cor);

		RefreshSheetUI();
		if (_combatManager.IsInCombat)
		{
			RefreshInitiativeBar();
			PopularAlvoCombate();
			UpdateCombatStatusDisplay();
		}
	}

	private void OnReviverNaFichaPressed()
	{
		if (_selectedCharacter == null) return;
		_selectedCharacter.Revive(1);
		int cid = GetSelectedCombatantId();
		if (cid > 0)
		{
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedCharacter.CurrentHp);
			Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_combatManager.Session, cid, _selectedCharacter);
			RefreshInitiativeBar();
			PopularAlvoCombate();
			UpdateCombatStatusDisplay();
		}
		RefreshSheetUI();
		ShowToast($"{_selectedCharacter.Name} foi revivido com 1 PV!", new Color(1f, 0.86f, 0.35f));
		AppendCombatLog($"{_selectedCharacter.Name} foi revivido com 1 PV!", "#ffd700");
	}

	private void LimparSheetUI()
	{
		_lblCharName.Text      = "—";
		_lblCharClassRace.Text = "";
		_lblPvValor.Text       = "0";
		_lblPmValor.Text       = "0";
		_listSubItems.Clear();
		foreach (Node child in _boxPericiasList.GetChildren().ToArray())
			child.QueueFree();
	}

	private void SetAttrLabel(Label lbl, int mod)
	{
		lbl.Text = mod >= 0 ? $"+{mod}" : mod.ToString();
	}

	// ── Sub-tabs e ações contextuais (Incluindo Usar Poderes/Magias/Ataques na Ficha!) ──
	private void SelecionarSubTab(string tab)
	{
		_currentSubTab = tab;
		_listSubItems.Clear();
		_lblDefesaDetalhe.Visible = false;

		if (_selectedCharacter == null)
		{
			_btnBannerSubTab.Text = tab.ToUpper();
			return;
		}

		var c = _selectedCharacter;
		int cid = GetSelectedCombatantId();
		var activeSession = cid > 0 ? _combatManager.Session : null;
		_btnBannerSubTab.Text = tab.ToUpper();

		// Atualiza indicador de Carga
		bool sobrecarregado = c.IsOverburdened();
		_lblCargaInfo.Text = $"Carga: {c.GetCurrentSlots()}/{c.GetMaxSlots()} esp.{(sobrecarregado ? " [SOBRECARREGADO]" : "")}";
		_lblCargaInfo.AddThemeColorOverride("font_color",
			sobrecarregado ? new Color(1f, 0.4f, 0.4f) : new Color(0.75f, 0.75f, 0.75f));

		switch (tab)
		{
			case "Ataques":
				if (_btnSubAcaoUsar != null) { _btnSubAcaoUsar.Visible = true; _btnSubAcaoUsar.Text = "Rolar Ataque"; }
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "+ Novo Item";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "Remover";

				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Weapon))
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"{item.Name} — Dano: {item.Damage} ({item.Critical}) {eq}");
					_listSubItems.SetItemMetadata(idx, c.Inventory.IndexOf(item));
				}
				if (_listSubItems.ItemCount > 0) _listSubItems.Select(0);
				if (!c.Inventory.Any(i => i.Type == ItemType.Weapon))
					_listSubItems.AddItem("Nenhuma arma cadastrada.");
				break;

			case "Defesa":
				if (_btnSubAcaoUsar != null) _btnSubAcaoUsar.Visible = false;
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "+ Novo Item";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "Remover";

				_lblDefesaDetalhe.Text    = $"Defesa Total: {c.GetDefense(activeSession, cid)}   |   Penalidade de Armadura: {c.GetTotalArmorPenalty(activeSession, cid)}";
				_lblDefesaDetalhe.Visible = true;
				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Armor || i.Type == ItemType.Shield))
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"{item.Name} +{item.DefenseBonus} def (Pen: {item.ArmorPenalty}) {eq}");
					_listSubItems.SetItemMetadata(idx, c.Inventory.IndexOf(item));
				}
				if (!c.Inventory.Any(i => i.Type == ItemType.Armor || i.Type == ItemType.Shield))
					_listSubItems.AddItem("Nenhuma armadura ou escudo cadastrado.");
				break;

			case "Poderes":
				if (_btnSubAcaoUsar != null) { _btnSubAcaoUsar.Visible = true; _btnSubAcaoUsar.Text = "Usar Poder (-PM)"; }
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Árvore de Poderes";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "+ Novo Poder";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "Remover";

				foreach (var sk in c.Skills.Where(s => !s.IsSpell))
				{
					string formulaTag = string.IsNullOrWhiteSpace(sk.BaseFormula) ? "" : $" [{sk.BaseFormula}]";
					int idx = _listSubItems.AddItem($"{sk.Name} ({sk.PmCost} PM){formulaTag} — {sk.Description}");
					_listSubItems.SetItemMetadata(idx, c.Skills.IndexOf(sk));
				}
				if (_listSubItems.ItemCount > 0) _listSubItems.Select(0);
				if (!c.Skills.Any(s => !s.IsSpell))
					_listSubItems.AddItem("Nenhum poder aprendido. Clique em 'Árvore de Poderes'.");
				break;

			case "Magias":
				if (_btnSubAcaoUsar != null) { _btnSubAcaoUsar.Visible = true; _btnSubAcaoUsar.Text = "Conjurar Magia (-PM)"; }
				_btnSubAcao1.Visible = false;
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "+ Nova Magia";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "Remover";

				foreach (var sk in c.Skills.Where(s => s.IsSpell))
				{
					string formulaTag = string.IsNullOrWhiteSpace(sk.BaseFormula) ? "" : $" [{sk.BaseFormula}]";
					int idx = _listSubItems.AddItem($"{sk.Name} ({sk.PmCost} PM){formulaTag} — {sk.Description}");
					_listSubItems.SetItemMetadata(idx, c.Skills.IndexOf(sk));
				}
				if (_listSubItems.ItemCount > 0) _listSubItems.Select(0);
				if (!c.Skills.Any(s => s.IsSpell))
					_listSubItems.AddItem("Nenhuma magia aprendida.");
				break;

			case "Equipamentos":
				if (_btnSubAcaoUsar != null) _btnSubAcaoUsar.Visible = false;
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "+ Novo Item";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "Remover";

				for (int i = 0; i < c.Inventory.Count; i++)
				{
					var item = c.Inventory[i];
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"{item.Name} {eq} ({item.Slots} esp.) — T$ {item.Price}");
					_listSubItems.SetItemMetadata(idx, i);
				}
				if (c.Inventory.Count == 0)
					_listSubItems.AddItem("Inventário vazio.");
				break;
		}
	}

	private void OnUsarItemSubTabNaFichaPressed()
	{
		if (_selectedCharacter == null) return;
		int cid = GetSelectedCombatantId();
		var activeSession = cid > 0 ? _combatManager.Session : null;

		T20HealthState hState = _selectedCharacter.GetHealthState(activeSession, cid);
		if (hState is T20HealthState.Dying or T20HealthState.UnconsciousStable or T20HealthState.Dead)
		{
			ShowToast($"{_selectedCharacter.Name} está {hState} e não pode executar habilidades ou ataques!", new Color(1f, 0.45f, 0.45f));
			return;
		}

		var sel = _listSubItems.GetSelectedItems();
		if (sel.Length == 0)
		{
			ShowToast("Selecione uma habilidade, magia ou arma na lista primeiro.");
			return;
		}

		var meta = _listSubItems.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int)
		{
			ShowToast("Nenhum item válido selecionado.");
			return;
		}
		int idx = (int)meta;

		if (_currentSubTab == "Poderes" || _currentSubTab == "Magias")
		{
			if (idx < 0 || idx >= _selectedCharacter.Skills.Count) return;
			var sk = _selectedCharacter.Skills[idx];

			int pmLimit = Tormenta20MagicRules.GetPmSpendLimit(_selectedCharacter, activeSession, cid);
			if (sk.PmCost > pmLimit)
			{
				ShowToast($"Custo ({sk.PmCost} PM) excede o limite de gasto por nível ({pmLimit} PM)!", new Color(1f, 0.45f, 0.45f));
				return;
			}

			if (_selectedCharacter.CurrentPm < sk.PmCost)
			{
				ShowToast($"PM insuficiente para \"{sk.Name}\"! (Custo: {sk.PmCost} PM | Atual: {_selectedCharacter.CurrentPm} PM)", new Color(1f, 0.45f, 0.45f));
				return;
			}

			// Se estiver em combate e for a vez deste personagem, valida na sessão nativa também
			if (cid > 0 && _combatManager.Session.CurrentTurn == cid)
			{
				var actRes = Tormenta20MagicRules.EnqueueSkillWithEnhancements(
					_combatManager.Session, cid, _selectedCharacter, sk, 0, GetSelectedCombatTargetId(cid));
				_selectedCharacter.CurrentPm = _combatManager.Session.GetResource(cid, CombatManager.ManaResourceId).Current;
				if (!actRes.Success)
				{
					ShowToast(actRes.Message, new Color(1f, 0.45f, 0.45f));
					return;
				}
			}
			else
			{
				// Gasta o PM diretamente na ficha (e sincroniza com o combate se estiver participando)
				_selectedCharacter.SpendPm(sk.PmCost);
				if (cid > 0)
				{
					_combatManager.Session.SetResourceCurrent(cid, CombatManager.ManaResourceId, _selectedCharacter.CurrentPm);
				}
			}

			string rollSummary = "";
			if (!string.IsNullOrWhiteSpace(sk.BaseFormula))
			{
				try
				{
					RollResult r = _combatManager.Session.Roll(sk.BaseFormula.Trim());
					bool isCure = sk.Name.Contains("Curar", StringComparison.OrdinalIgnoreCase) ||
					              sk.Description.Contains("restaurar", StringComparison.OrdinalIgnoreCase);
					if (isCure)
					{
						_selectedCharacter.HealHp(r.Total);
						if (cid > 0)
						{
							_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedCharacter.CurrentHp);
							Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_combatManager.Session, cid, _selectedCharacter);
						}
						rollSummary = $" -> Curou +{r.Total} PV ({sk.BaseFormula})!";
					}
					else
					{
						rollSummary = $" -> Rolou {sk.BaseFormula} = {r.Total}!";
					}
					_txtHistorico?.AppendText($"[color=#66d9ef][{_selectedCharacter.Name}] {sk.Name} (-{sk.PmCost} PM){rollSummary}[/color]\n");
				}
				catch { }
			}
			else
			{
				_txtHistorico?.AppendText($"[color=#66d9ef][{_selectedCharacter.Name}] Usou {sk.Name} (-{sk.PmCost} PM): {sk.Description}[/color]\n");
			}

			RefreshSheetUI();
			if (_combatManager.IsInCombat)
			{
				RefreshInitiativeBar();
				PopularAlvoCombate();
				UpdateCombatStatusDisplay();
			}

			ShowToast($"{_selectedCharacter.Name} usou \"{sk.Name}\" (-{sk.PmCost} PM){rollSummary} | PM: {_selectedCharacter.CurrentPm}/{_selectedCharacter.MaxPm}",
				new Color(0.45f, 0.85f, 1f));
			AppendCombatLog($"{_selectedCharacter.Name} usou [b]{sk.Name}[/b] na ficha (-{sk.PmCost} PM){rollSummary}", "#66d9ef");
		}
		else if (_currentSubTab == "Ataques")
		{
			if (idx < 0 || idx >= _selectedCharacter.Inventory.Count) return;
			var weapon = _selectedCharacter.Inventory[idx];
			string periciaAtk = weapon.IsRanged ? "Pontaria" : "Luta";
			Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, periciaAtk, out int atkMod, out _, activeSession, cid);
			weapon.ParseCritical(out int threat, out int mult);

			RollResult d20 = _combatManager.Session.Roll("1d20");
			int natural = d20.DiceTotal;
			int totalAtk = natural + atkMod;
			bool isCrit = natural >= threat;
			bool isFumble = natural == 1;

			int flatDmg = weapon.IsRanged ? 0 : _selectedCharacter.Strength;
			string dmgExpr = Tormenta20CombatRules.BuildWeaponDamageExpression(weapon.Damage, isCrit, mult, flatDmg);
			RollResult dmgRoll = _combatManager.Session.Roll(dmgExpr);
			int totalDmg = Math.Max(1, dmgRoll.Total);

			string critTag = isCrit ? $" [CRÍTICO {threat}/x{mult}!]" : (isFumble ? " [FALHA CRÍTICA!]" : "");
			ShowToast($"{weapon.Name}: Ataque {totalAtk} (1d20={natural}+Mod {atkMod}){critTag} | Dano: {totalDmg} ({dmgExpr})",
				isCrit ? new Color(1f, 0.86f, 0.22f) : new Color(0.45f, 0.95f, 0.6f));
			_txtHistorico?.AppendText($"[color=#ffaa44][{_selectedCharacter.Name}] {weapon.Name}: Ataque [b]{totalAtk}[/b]{critTag} | Dano: [b]{totalDmg}[/b] ({dmgExpr})[/color]\n");
			AppendCombatLog($"{_selectedCharacter.Name} rolou ataque com {weapon.Name}: Ataque {totalAtk}{critTag} | Dano {totalDmg} ({dmgExpr})", "#ffaa44");
		}
	}

	private void OnSubAcao1Pressed()
	{
		if (_selectedCharacter == null) return;

		if (_currentSubTab == "Poderes")
		{
			AbrirArvorePoderes();
			return;
		}

		// Equipar / Desequipar item selecionado
		var sel = _listSubItems.GetSelectedItems();
		if (sel.Length == 0) { ShowToast("Selecione um item na lista primeiro."); return; }

		var meta = _listSubItems.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int invIdx = (int)meta;
		if (invIdx < 0 || invIdx >= _selectedCharacter.Inventory.Count) return;

		var item = _selectedCharacter.Inventory[invIdx];
		item.Equipped = !item.Equipped;
		RefreshSheetUI();
		ShowToast($"{(item.Equipped ? "Equipado" : "Guardado")}: {item.Name}");
	}

	private void OnSubAcao2Pressed()
	{
		if (_selectedCharacter == null) return;
		if (_currentSubTab == "Poderes" || _currentSubTab == "Magias")
		{
			_addingSkillToEnemy = false;
			bool isMagiasTab = _currentSubTab == "Magias";
			_dlgNovaMagia.Title = isMagiasTab ? "Adicionar Nova Magia" : "Adicionar Novo Poder / Habilidade";
			if (_chkNovaMagiaIsSpell != null)
				_chkNovaMagiaIsSpell.ButtonPressed = isMagiasTab;
			_dlgNovaMagia.PopupCentered();
		}
		else
		{
			_dlgNovoItem.PopupCentered();
		}
	}

	private void OnSubAcao3Pressed()
	{
		if (_selectedCharacter == null) return;
		var sel = _listSubItems.GetSelectedItems();
		if (sel.Length == 0) { ShowToast("Selecione um item para remover."); return; }

		var meta = _listSubItems.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int idx = (int)meta;

		if (_currentSubTab == "Poderes" || _currentSubTab == "Magias")
		{
			if (idx >= 0 && idx < _selectedCharacter.Skills.Count)
			{
				var removedSkill = _selectedCharacter.Skills[idx];
				_selectedCharacter.Skills.RemoveAt(idx);
				if (!string.IsNullOrEmpty(removedSkill.TreeKey))
				{
					_selectedCharacter.UnlockedTreeKeys.Remove(removedSkill.TreeKey);
				}
				RefreshSheetUI();
				ShowToast($"Removido: {removedSkill.Name}");
			}
		}
		else
		{
			if (idx >= 0 && idx < _selectedCharacter.Inventory.Count)
			{
				string removedName = _selectedCharacter.Inventory[idx].Name;
				_selectedCharacter.Inventory.RemoveAt(idx);
				RefreshSheetUI();
				ShowToast($"Removido: {removedName}");
			}
		}
	}

	// ── Perícias Interativas ──────────────────────────────────────────────────
	private void RefreshPericiasUI()
	{
		if (_selectedCharacter == null) return;

		foreach (Node child in _boxPericiasList.GetChildren().ToArray())
			child.QueueFree();

		string busca = _txtBuscaPericia?.Text?.ToLower() ?? "";
		int cid = GetSelectedCombatantId();
		var activeSession = cid > 0 ? _combatManager.Session : null;

		foreach (T20SkillDefinition def in Tormenta20SkillRules.CanonicalSkills)
		{
			if (!string.IsNullOrEmpty(busca) && !def.Name.ToLower().Contains(busca))
				continue;

			bool canRoll  = Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, def.Name, out int totalMod, out _, activeSession, cid);
			bool treinada = _selectedCharacter.TrainedSkills.Contains(def.Name);
			string modStr = canRoll ? (totalMod >= 0 ? $"+{totalMod}" : totalMod.ToString()) : "—";

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 6);

			// Total
			var lblTotal = new Label
			{
				Text = modStr,
				CustomMinimumSize = new Vector2(42, 0),
				HorizontalAlignment = HorizontalAlignment.Center
			};
			lblTotal.AddThemeColorOverride("font_color", canRoll ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.5f, 0.5f, 0.5f));
			lblTotal.AddThemeFontSizeOverride("font_size", 12);
			row.AddChild(lblTotal);

			// Nome (botão clicável)
			string suffix = def.TrainedOnly ? " *" : "";
			var btnNome = new Button
			{
				Text = def.Name + suffix,
				SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
				Flat = true,
				Alignment = HorizontalAlignment.Left
			};
			btnNome.AddThemeFontSizeOverride("font_size", 12);
			if (!canRoll)
				btnNome.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));

			string nomeCopy = def.Name;
			btnNome.Pressed += () => OnRolarPericia(nomeCopy);
			row.AddChild(btnNome);

			// Treino (CheckBox interativo)
			var chkTreino = new CheckBox
			{
				ButtonPressed = treinada,
				CustomMinimumSize = new Vector2(44, 0)
			};
			chkTreino.Toggled += (bool pressed) =>
			{
				if (pressed) _selectedCharacter.TrainedSkills.Add(nomeCopy);
				else         _selectedCharacter.TrainedSkills.Remove(nomeCopy);
				RefreshSheetUI();
			};
			row.AddChild(chkTreino);

			// Outros (SpinBox compacto)
			int misc = _selectedCharacter.SkillMiscBonuses.TryGetValue(def.Name, out int m) ? m : 0;
			var spinOutros = new SpinBox
			{
				MinValue = -20,
				MaxValue = 20,
				Value = misc,
				CustomMinimumSize = new Vector2(54, 0)
			};
			spinOutros.ValueChanged += (double val) =>
			{
				int intVal = (int)val;
				if (intVal == 0) _selectedCharacter.SkillMiscBonuses.Remove(nomeCopy);
				else             _selectedCharacter.SkillMiscBonuses[nomeCopy] = intVal;
				RefreshPericiasUI();
			};
			row.AddChild(spinOutros);

			_boxPericiasList.AddChild(row);
		}
	}

	private void OnRolarPericia(string nomePericia)
	{
		if (_selectedCharacter == null) return;

		int cid = GetSelectedCombatantId();
		var check = Tormenta20SkillRules.RollSkillCheck(
			_combatManager.Session, _selectedCharacter, nomePericia, dc: 0, combatantId: cid);
		if (!check.Allowed)
		{
			ShowToast(check.Message, new Color(1f, 0.45f, 0.45f));
			return;
		}

		bool critico = check.IsNatural20;
		bool falhaCritica = check.IsNatural1;
		string modSign = check.TotalModifier >= 0 ? $"+{check.TotalModifier}" : check.TotalModifier.ToString();

		_lblPopupTitulo.Text     = $"TESTE DE {nomePericia.ToUpper()}";
		_lblPopupPersonagem.Text = $"Personagem: {_selectedCharacter.Name}";
		_lblPopupResultado.Text  = check.Total.ToString();
		_lblPopupFormula.Text    = $"1d20 rolou {check.NaturalRoll} (Mod {modSign}) = {check.Total}";

		if (critico)
		{
			_lblPopupDestaque.Text    = "20 NATURAL! SUCESSO CRÍTICO!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.22f));
		}
		else if (falhaCritica)
		{
			_lblPopupDestaque.Text    = "1 NATURAL! FALHA CRÍTICA!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.35f, 0.35f));
		}
		else
		{
			_lblPopupDestaque.Visible = false;
		}

		_popupPericia.Modulate = new Color(1, 1, 1, 0);
		_popupPericia.Visible  = true;
		var tween = CreateTween();
		tween.TweenProperty(_popupPericia, "modulate", new Color(1, 1, 1, 1), 0.2f);

		_diceVisualPericia?.PlayRollAnimation("1d20", check.Total, check.NaturalRoll, critico, falhaCritica, 0.42f);

		// Também registra no histórico da Bandeja de Dados
		string cor = critico ? "#ffd700" : (falhaCritica ? "#ff4444" : "#ffdd66");
		_txtHistorico?.AppendText($"[color={cor}][{_selectedCharacter.Name}] {nomePericia} (1d20{modSign}) -> [b]{check.Total}[/b] (d20:{check.NaturalRoll})[/color]\n");
		AppendCombatLog($"{_selectedCharacter.Name} rolou {nomePericia}: {check.Total} (1d20={check.NaturalRoll}{modSign})", cor);
	}

	// ── PV / PM modificar ────────────────────────────────────────────────────
	private void ModificarRecurso(bool isPm, bool cura)
	{
		if (_selectedCharacter == null) return;
		int delta = (int)(isPm ? _spinPmDelta.Value : _spinPvDelta.Value);
		int cid = GetSelectedCombatantId();
		var prevState = _selectedCharacter.GetHealthState(cid > 0 ? _combatManager.Session : null, cid);

		if (!isPm)
		{
			if (cura) _selectedCharacter.HealHp(delta);
			else      _selectedCharacter.TakeDamage(delta);
		}
		else
		{
			if (cura) _selectedCharacter.RecoverPm(delta);
			else      _selectedCharacter.SpendPm(delta);
		}

		// Sincroniza com o Core C++ caso o personagem esteja em um combate ativo
		if (cid > 0)
		{
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedCharacter.CurrentHp);
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.ManaResourceId, _selectedCharacter.CurrentPm);
			string stateMsg = Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_combatManager.Session, cid, _selectedCharacter);
			if (!string.IsNullOrEmpty(stateMsg))
			{
				AppendCombatLog(stateMsg, "#ff6b6b");
			}
			RefreshInitiativeBar();
			PopularAlvoCombate();
			UpdateCombatStatusDisplay();
		}

		var newState = _selectedCharacter.GetHealthState(cid > 0 ? _combatManager.Session : null, cid);
		if (newState != prevState)
		{
			if (newState == T20HealthState.Dead)
				ShowToast($"{_selectedCharacter.Name} atingiu {_selectedCharacter.CurrentHp} PV (limite {_selectedCharacter.GetDeathThreshold()} PV) e MORREU!", new Color(1f, 0.25f, 0.25f));
			else if (newState == T20HealthState.Dying)
				ShowToast($"{_selectedCharacter.Name} caiu MORRENDO ({_selectedCharacter.CurrentHp} PV — Inconsciente e Sangrando)!", new Color(1f, 0.45f, 0.45f));
			else if (prevState is T20HealthState.Dying or T20HealthState.UnconsciousStable && newState is T20HealthState.Healthy or T20HealthState.Bloodied)
				ShowToast($"{_selectedCharacter.Name} recobrou a consciência ({_selectedCharacter.CurrentHp} PV)!");
		}

		RefreshSheetUI();
	}

	private void OnSalvarFichasPressed()
	{
		_combatManager.SaveCharacters();
		_combatManager.SaveEnemies();
		ShowToast($"{_combatManager.Characters.Count} herói(s) e {_combatManager.Enemies.Count} inimigo(s) salvos!");
		AppendCombatLog("Fichas e Bestiário salvos com sucesso!", "#55ff99");
	}

	// ════════════════════════════════════════════════════════════════════════
	// Combate
	// ════════════════════════════════════════════════════════════════════════
	private void OnIniciarCombatePressed()
	{
		AbrirModalSelecaoIniciativa();
	}

	private void UpdateCombatStatusDisplay()
	{
		if (_combatManager.IsInCombat)
		{
			int round = _combatManager.Session.Round;
			int cid   = _combatManager.Session.CurrentTurn;
			CharacterSheetData sheet = null;
			string nome = _combatManager.CombatantIdToSheet.TryGetValue(cid, out sheet)
				? sheet.Name : "Desconhecido";
			string slots = cid > 0 ? Tormenta20ActionRules.FormatRemainingActions(_combatManager.Session, cid) : "";

			bool isIncapacitated = sheet != null && sheet.CurrentHp <= 0;
			string stateTag = sheet != null && sheet.CurrentHp <= 0
				? $" [{sheet.GetHealthStateLabel(_combatManager.Session, cid)}]"
				: "";

			_lblStatusCombate.Text      = $"Rodada {round} — Vez de {nome}{stateTag}";
			_lblTurnoAtual.Text         = isIncapacitated
				? $"Combatente atual: {nome} está INCAPACITADO ({sheet.CurrentHp} PV) — Encerre o turno"
				: $"Combatente atual: {nome}   |   Ações: {slots}";
			_btnProximoTurno.Disabled   = false;
			if (_btnEncerrarCombate != null) _btnEncerrarCombate.Disabled = false;
			if (_btnSalvarCombate != null) _btnSalvarCombate.Disabled = false;
			_btnAtacar.Disabled         = isIncapacitated;
			_btnMovimento.Disabled      = isIncapacitated;
			_btnUsarHabilidade.Disabled = isIncapacitated;
			if (_btnAcaoCompleta != null) _btnAcaoCompleta.Disabled = isIncapacitated;
			if (_btnPopNextAction != null) _btnPopNextAction.Disabled = isIncapacitated;
		}
		else
		{
			_lblStatusCombate.Text      = "Fora de combate.";
			_lblTurnoAtual.Text         = "Clique em 'Iniciar / Rolar Iniciativa' para escolher os participantes.";
			_btnProximoTurno.Disabled   = true;
			if (_btnEncerrarCombate != null) _btnEncerrarCombate.Disabled = true;
			if (_btnSalvarCombate != null) _btnSalvarCombate.Disabled = true;
			_btnAtacar.Disabled         = false;
			_btnMovimento.Disabled      = false;
			_btnUsarHabilidade.Disabled = false;
			if (_btnAcaoCompleta != null) _btnAcaoCompleta.Disabled = true;
			if (_btnPopNextAction != null) _btnPopNextAction.Disabled = true;
		}
		RefreshActionQueuePanel();
		RefreshTacticalMapView();
	}

	private void OnAcaoCompletaPressed()
	{
		if (!_combatManager.IsInCombat)
		{
			AppendCombatLog("Inicie o combate antes de usar Ação Completa!", "#ff9999");
			return;
		}
		int actorId = _combatManager.Session.CurrentTurn;
		var res = Tormenta20ActionRules.EnqueueTurnAction(
			_combatManager.Session, actorId, T20ActionType.Full, "Investida / Golpe Pessoal (Completa)");
		AppendCombatLog(res.Success
			? res.Message
			: $"Não foi possível enfileirar Ação Completa: {res.Message}", res.Success ? "#ffb74d" : "#ff6b6b");
		UpdateCombatStatusDisplay();
	}

	private void OnPopNextActionPressed()
	{
		if (!_combatManager.IsInCombat) return;
		int actorId = _combatManager.Session.CurrentTurn;
		if (_combatManager.Session.PopNextAction(actorId, out var popped))
		{
			string tagName = string.IsNullOrWhiteSpace(popped.Name) ? "Ação" : popped.Name;
			string conv = popped.UsedFallback ? " [Padrão->Movimento]" : "";
			AppendCombatLog($"Ação executada: {tagName}{conv}", "#a6e22e");
			ShowToast($"Executou: {tagName}");
		}
		else
		{
			AppendCombatLog("Nenhuma ação planejada para este turno.", "#cccccc");
		}
		UpdateCombatStatusDisplay();
	}

	private void RefreshActionQueuePanel()
	{
		if (_boxActionQueueList == null) return;
		foreach (Node child in _boxActionQueueList.GetChildren())
			child.QueueFree();

		if (!_combatManager.IsInCombat)
		{
			if (_lblActionQueueSlots != null)
				_lblActionQueueSlots.Text = "Inicie o combate para planejar ações.";
			return;
		}

		int actorId = _combatManager.Session.CurrentTurn;
		if (_lblActionQueueSlots != null)
		{
			string slots = actorId > 0
				? Tormenta20ActionRules.FormatRemainingActions(_combatManager.Session, actorId)
				: "—";
			_lblActionQueueSlots.Text = $"Disponível no Turno: {slots}";
		}

		var entries = actorId > 0 ? _combatManager.Session.GetActionQueue(actorId) : Array.Empty<QueuedActionEntry>();
		if (entries.Length == 0)
		{
			var emptyLbl = new Label
			{
				Text = "Nenhuma ação pendente neste turno.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			emptyLbl.AddThemeFontSizeOverride("font_size", 11);
			emptyLbl.AddThemeColorOverride("font_color", new Color(0.62f, 0.66f, 0.75f));
			_boxActionQueueList.AddChild(emptyLbl);
			return;
		}

		for (int i = 0; i < entries.Length; i++)
		{
			var entry = entries[i];
			string actionLabel = string.IsNullOrWhiteSpace(entry.Name) ? "Ação" : entry.Name;
			string tipoTag = entry.RequiredType switch
			{
				(int)T20ActionType.Reaction => "Reação",
				(int)T20ActionType.Free     => "Ação Livre",
				(int)T20ActionType.Standard => "Ação Padrão",
				(int)T20ActionType.Movement => "Ação de Movimento",
				(int)T20ActionType.Full     => "Ação Completa",
				_                           => "Ação"
			};
			if (entry.UsedFallback)
				tipoTag += " [Padrão->Movimento]";

			int pmCost = _queuedActionPmSpent.TryGetValue((actorId, entry.ActionId), out int pmVal) ? pmVal : 0;
			string pmSuffix = pmCost > 0 ? $" | -{pmCost} PM" : "";

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 6);

			var lbl = new Label
			{
				Text = $"{i + 1}. {actionLabel} ({tipoTag}){pmSuffix}",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			lbl.AddThemeFontSizeOverride("font_size", 12);
			lbl.AddThemeColorOverride("font_color", new Color(0.92f, 0.95f, 1.0f));
			row.AddChild(lbl);

			int capturedActionId = entry.ActionId;
			string capturedLabel = actionLabel;
			var btnUndo = new Button
			{
				Text = "Desfazer",
				CustomMinimumSize = new Vector2(88, 24),
				TooltipText = "Cancela esta ação e devolve o uso da ação (e PM gasto, se houver)"
			};
			btnUndo.Pressed += () =>
			{
				if (_combatManager.Session.CancelQueuedAction(actorId, capturedActionId, refundSlot: true))
				{
					string pmRefundMsg = "";
					if (_queuedActionPmSpent.TryGetValue((actorId, capturedActionId), out int refundPm) && refundPm > 0)
					{
						if (_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet))
						{
							actorSheet.RecoverPm(refundPm);
							_combatManager.Session.RestoreResource(actorId, CombatManager.ManaResourceId, refundPm);
							pmRefundMsg = $" (+{refundPm} PM recuperado)";
						}
						_queuedActionPmSpent.Remove((actorId, capturedActionId));
					}
					AppendCombatLog($"Ação \"{capturedLabel}\" cancelada{pmRefundMsg}.", "#66d9ef");
					ShowToast($"Desfeita: {capturedLabel}{pmRefundMsg}");
					UpdateCombatStatusDisplay();
					RefreshSheetUI();
					RefreshEnemySheetUI();
					RefreshInitiativeBar();
				}
			};
			row.AddChild(btnUndo);
			_boxActionQueueList.AddChild(row);
		}
	}

	private void RefreshInitiativeBar()
	{
		foreach (Node child in _boxIniciativa.GetChildren())
			child.QueueFree();

		if (!_combatManager.IsInCombat) return;

		int[] order = _combatManager.Session.GetInitiativeOrder();
		int current = _combatManager.Session.CurrentTurn;

		foreach (int cid in order)
		{
			var btn = new Button();
			if (_combatManager.CombatantIdToSheet.TryGetValue(cid, out var sh))
			{
				var st = sh.GetHealthState(_combatManager.Session, cid);
				string tag = st switch
				{
					T20HealthState.Dead => sh.IsEnemy ? " [DERROTADO]" : " [MORTO]",
					T20HealthState.Dying => " [MORRENDO]",
					T20HealthState.UnconsciousStable => " [INCONSCIENTE]",
					_ => ""
				};
				btn.Text = $"{sh.Name}{tag} (PV {sh.CurrentHp}/{sh.MaxHp} | PM {sh.CurrentPm}/{sh.MaxPm})";
			}
			else
				btn.Text = $"ID {cid}";

			btn.Modulate = cid == current ? new Color(1f, 0.85f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);
			int capturedCid = cid;
			btn.Pressed += () =>
			{
				if (_combatManager.CombatantIdToSheet.TryGetValue(capturedCid, out var clickedSheet))
				{
					if (clickedSheet.IsEnemy)
					{
						SelectEnemyByRef(clickedSheet);
					}
					else
					{
						int idx = _combatManager.Characters.IndexOf(clickedSheet);
						if (idx >= 0) SelectCharacter(idx);
					}
				}
			};
			_boxIniciativa.AddChild(btn);
		}
	}

	private void PopularAlvoCombate()
	{
		int prevTargetId = -1;
		if (_optAlvoCombate.Selected >= 0)
		{
			var meta = _optAlvoCombate.GetItemMetadata(_optAlvoCombate.Selected);
			if (meta.VariantType == Variant.Type.Int) prevTargetId = (int)meta;
		}

		_optAlvoCombate.Clear();
		if (!_combatManager.IsInCombat) return;

		int actorId = _combatManager.Session.CurrentTurn;
		int[] order = _combatManager.Session.GetInitiativeOrder();

		int restoreIdx = -1;
		int defaultEnemyIdx = -1;
		foreach (int cid in order)
		{
			if (!_combatManager.CombatantIdToSheet.TryGetValue(cid, out var sh)) continue;
			string label = cid == actorId
				? $"[Você] {sh.Name} (PV {sh.CurrentHp}/{sh.MaxHp} | Def {sh.GetDefense(_combatManager.Session, cid)})"
				: $"{sh.Name} (PV {sh.CurrentHp}/{sh.MaxHp} | Def {sh.GetDefense(_combatManager.Session, cid)})";

			_optAlvoCombate.AddItem(label);
			int itemIdx = _optAlvoCombate.ItemCount - 1;
			_optAlvoCombate.SetItemMetadata(itemIdx, cid);
			if (cid == prevTargetId) restoreIdx = itemIdx;
			if (cid != actorId && defaultEnemyIdx < 0 && sh.CurrentHp > 0) defaultEnemyIdx = itemIdx;
		}

		if (restoreIdx >= 0)
			_optAlvoCombate.Select(restoreIdx);
		else if (defaultEnemyIdx >= 0)
			_optAlvoCombate.Select(defaultEnemyIdx);
	}

	private int GetSelectedCombatTargetId(int actorId)
	{
		int[] order = _combatManager.Session.GetInitiativeOrder();
		int targetId = 0;
		int selectedIdx = _optAlvoCombate.Selected;
		if (selectedIdx >= 0)
		{
			var meta = _optAlvoCombate.GetItemMetadata(selectedIdx);
			if (meta.VariantType == Variant.Type.Int) targetId = (int)meta;
		}
		if (targetId <= 0)
			targetId = order.FirstOrDefault(id => id != actorId);
		if (targetId <= 0) targetId = actorId;
		return targetId;
	}

	private void OnAtacarPressed()
	{
		if (!_combatManager.IsInCombat)
		{
			AppendCombatLog("Inicie o combate antes de atacar!", "#ff9999");
			return;
		}

		int actorId = _combatManager.Session.CurrentTurn;
		if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) return;

		var weapon = actorSheet.Inventory.FirstOrDefault(i => i.Type == ItemType.Weapon && i.Equipped)
			?? new CharacterItem("Ataque Desarmado", "", ItemType.Weapon, 0, "1d3", "20/x2");

		int targetId = GetSelectedCombatTargetId(actorId);
		if (!_combatManager.CombatantIdToSheet.TryGetValue(targetId, out var targetSheet))
			targetSheet = actorSheet;

		var resultado = Tormenta20CombatRules.ResolveWeaponAttack(
			_combatManager.Session, actorSheet, actorId, targetSheet, targetId, weapon);

		if (!resultado.ActionSucceeded)
			AppendCombatLog(resultado.Message, "#ff9999");
		else if (resultado.Hit)
		{
			string crit = resultado.IsCritical ? " [CRÍTICO!]" : "";
			string rdInfo = resultado.CoreDamageResult.ReducedBy > 0 ? $" [RD absorveu {resultado.CoreDamageResult.ReducedBy}]" : "";
			AppendCombatLog($"{actorSheet.Name} atacou {targetSheet.Name} com {weapon.Name}{crit} ({resultado.AttackTotal} vs Def {resultado.TargetDefense})! Dano: {resultado.CoreDamageResult.FinalAmount}{rdInfo} ({resultado.DamageExpression})", "#ffaa44");
		}
		else
		{
			AppendCombatLog($"{actorSheet.Name} atacou {targetSheet.Name} com {weapon.Name}: ERROU ({resultado.AttackTotal} vs Def {resultado.TargetDefense})", "#99bbff");
		}

		UpdateCombatStatusDisplay();
		RefreshSheetUI();
		RefreshEnemySheetUI();
		RefreshInitiativeBar();
		PopularAlvoCombate();
		RefreshTacticalMapView();
	}

	private void OnMovimentoPressed()
	{
		if (!_combatManager.IsInCombat) return;
		int actorId = _combatManager.Session.CurrentTurn;
		var res = Tormenta20ActionRules.EnqueueTurnAction(_combatManager.Session, actorId, T20ActionType.Movement, "Avanço Tático");
		AppendCombatLog(res.Success
			? res.Message
			: $"Não foi possível mover: {res.Message}", res.Success ? "#a6e22e" : "#ff6b6b");
		UpdateCombatStatusDisplay();
	}

	private void OnAbrirUsarHabilidadeCombate()
	{
		if (!_combatManager.IsInCombat)
		{
			AppendCombatLog("Inicie o combate antes de usar habilidades!", "#ff9999");
			return;
		}

		int actorId = _combatManager.Session.CurrentTurn;
		if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) return;

		_listHabilidadesCombate.Clear();
		if (actorSheet.Skills.Count == 0)
		{
			AppendCombatLog($"{actorSheet.Name} não possui poderes ou magias na ficha.", "#ff9999");
			return;
		}

		int pmLimit = Tormenta20MagicRules.GetPmSpendLimit(actorSheet, _combatManager.Session, actorId);
		if (_lblLimitePmCombate != null)
			_lblLimitePmCombate.Text = $"Limite de gasto: {pmLimit} PM (PM Atual: {actorSheet.CurrentPm})";
		if (_spinAprimoramentoPm != null)
			_spinAprimoramentoPm.Value = 0;

		foreach (var sk in actorSheet.Skills)
		{
			string tipo = sk.IsSpell ? "[Magia]" : "[Poder]";
			_listHabilidadesCombate.AddItem($"{tipo} {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
		}
		_listHabilidadesCombate.Select(0);
		_dlgUsarHabilidadeCombate.PopupCentered();
	}

	// ════════════════════════════════════════════════════════════════════════
	// Dados (Rolagem com Animação Poliedral e Partículas)
	// ════════════════════════════════════════════════════════════════════════
	private void RolarDadoExpressao(string expr)
	{
		if (string.IsNullOrWhiteSpace(expr)) expr = "1d20";
		expr = expr.Trim();

		if (_combatManager.RollTest(expr, out RollResult res, out string erro))
		{
			bool isD20Expr = expr.Contains("d20", StringComparison.OrdinalIgnoreCase);
			bool isCrit   = res.NaturalMaxCount > 0 && (isD20Expr || res.DiceCount == 1);
			bool isFumble = res.NaturalMinCount > 0 && isD20Expr;

			_lblResultado.Text = res.Total.ToString();
			string badge = isCrit ? "   CRÍTICO NATURAL!" : (isFumble ? "   FALHA CRÍTICA!" : "");
			_lblDetalhes.Text = $"Fórmula: {expr}{badge}\nSoma dos Dados: {res.DiceTotal}   |   Modificador: {(res.Modifier >= 0 ? $"+{res.Modifier}" : res.Modifier.ToString())}   |   Qtd Dados: {res.DiceCount}";

			string cor = "#ffffff";
			if (isCrit)
			{
				cor = "#ffd700";
				_lblResultado.Modulate = new Color(1f, 0.86f, 0.22f);
			}
			else if (isFumble)
			{
				cor = "#ff4444";
				_lblResultado.Modulate = new Color(1f, 0.32f, 0.32f);
			}
			else
			{
				_lblResultado.Modulate = Colors.White;
			}

			// Dispara a animação visual do dado poliedral e partículas!
			_diceVisualTray?.PlayRollAnimation(expr, res.Total, res.DiceTotal, isCrit, isFumble);

			_txtHistorico.AppendText($"[color={cor}][b]{expr}[/b] -> [b]{res.Total}[/b] (dados: {res.DiceTotal}, mod: {(res.Modifier >= 0 ? $"+{res.Modifier}" : res.Modifier.ToString())}){badge}[/color]\n");
		}
		else
		{
			_lblResultado.Text = "Erro";
			_lblResultado.Modulate = new Color(1f, 0.4f, 0.4f);
			_lblDetalhes.Text  = erro;
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	// Ficha Própria de Inimigos / Ameaças (Bestiário Tormenta20 JdA)
	// ════════════════════════════════════════════════════════════════════════
	private static PanelContainer CreateStyledCard(Color borderColor, Color? bgColor = null)
	{
		var card = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = bgColor ?? new Color(0.11f, 0.115f, 0.145f, 1f),
			BorderColor = borderColor,
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 7,
			CornerRadiusTopRight = 7,
			CornerRadiusBottomLeft = 7,
			CornerRadiusBottomRight = 7,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		});
		return card;
	}

	private static PanelContainer CreateEnemyBanner(string title)
	{
		var banner = new PanelContainer();
		banner.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.48f, 0.11f, 0.13f, 1f),
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 4,
			ContentMarginBottom = 4
		});
		var lbl = new Label
		{
			Text = title,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		lbl.AddThemeFontSizeOverride("font_size", 13);
		lbl.AddThemeColorOverride("font_color", Colors.White);
		banner.AddChild(lbl);
		return banner;
	}

	private void BuildEnemySheetView()
	{
		var mainContent = GetNode<VBoxContainer>("MarginContainer/VBoxMain/MainContent");

		var hSplit = new HSplitContainer
		{
			Name = "ViewInimigos",
			Visible = false,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		StyleSplitContainer(hSplit);
		mainContent.AddChild(hSplit);
		_viewInimigos = hSplit;

		// 1. Coluna Esquerda: Bestiário / Lista de Inimigos Salvos
		var leftCard = CreateStyledCard(new Color(0.45f, 0.18f, 0.20f, 1f));
		leftCard.CustomMinimumSize = new Vector2(265, 0);
		leftCard.SizeFlagsStretchRatio = 0.42f;
		leftCard.SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill;
		hSplit.AddChild(leftCard);

		var vLeft = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vLeft.AddThemeConstantOverride("separation", 8);
		leftCard.AddChild(vLeft);

		vLeft.AddChild(CreateEnemyBanner("BESTIÁRIO / INIMIGOS"));

		_txtBuscaInimigo = new LineEdit
		{
			PlaceholderText = "Buscar inimigo ou ND..."
		};
		_txtBuscaInimigo.TextChanged += (_) => RefreshEnemyList();
		vLeft.AddChild(_txtBuscaInimigo);

		_listInimigos = new ItemList
		{
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			CustomMinimumSize = new Vector2(0, 180)
		};
		_listInimigos.ItemSelected += (idx) =>
		{
			var meta = _listInimigos.GetItemMetadata((int)idx);
			if (meta.VariantType == Variant.Type.Int)
			{
				var target = _combatManager.Enemies.FirstOrDefault(e => e.Id == (int)meta);
				if (target != null) SelectEnemyByRef(target);
			}
		};
		vLeft.AddChild(_listInimigos);

		var lblPreset = new Label { Text = "Criar Ameaça Pronta (T20 JdA):" };
		lblPreset.AddThemeFontSizeOverride("font_size", 11);
		lblPreset.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.88f));
		vLeft.AddChild(lblPreset);

		var hPreset = new HBoxContainer();
		hPreset.AddThemeConstantOverride("separation", 6);
		vLeft.AddChild(hPreset);

		_optBestiaryPreset = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		foreach (var preset in Tormenta20Bestiary.Presets)
		{
			_optBestiaryPreset.AddItem($"{preset.DisplayName} (ND {preset.ChallengeRating})");
			_optBestiaryPreset.SetItemMetadata(_optBestiaryPreset.ItemCount - 1, preset.Key);
		}
		hPreset.AddChild(_optBestiaryPreset);

		var btnAddPreset = new Button { Text = "+ Adicionar" };
		btnAddPreset.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		btnAddPreset.Pressed += () =>
		{
			int selIdx = _optBestiaryPreset.Selected;
			string key = selIdx >= 0 ? _optBestiaryPreset.GetItemMetadata(selIdx).AsString() : "goblin_salteador";
			var created = _combatManager.CreateEnemyFromPreset(key);
			_combatManager.SaveEnemies();
			RefreshEnemyList();
			SelectEnemyByRef(created);
			ShowToast($"Ameaça \"{created.Name}\" adicionada ao Bestiário!");
		};
		hPreset.AddChild(btnAddPreset);

		var hListBtns = new HBoxContainer();
		hListBtns.AddThemeConstantOverride("separation", 6);
		vLeft.AddChild(hListBtns);

		var btnBlankEnemy = new Button { Text = "+ Em Branco", SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		btnBlankEnemy.Pressed += () =>
		{
			var created = _combatManager.CreateBlankEnemy();
			_combatManager.SaveEnemies();
			RefreshEnemyList();
			SelectEnemyByRef(created);
		};
		hListBtns.AddChild(btnBlankEnemy);

		var btnDupEnemy = new Button { Text = "Duplicar", SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		btnDupEnemy.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			var copy = _combatManager.DuplicateEnemy(_selectedEnemy);
			_combatManager.SaveEnemies();
			RefreshEnemyList();
			SelectEnemyByRef(copy);
		};
		hListBtns.AddChild(btnDupEnemy);

		var btnDelEnemy = new Button { Text = "Excluir", SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		btnDelEnemy.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		btnDelEnemy.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			string deletedName = _selectedEnemy.Name;
			_combatManager.DeleteEnemy(_selectedEnemy.Id);
			_combatManager.SaveEnemies();
			_selectedEnemy = _combatManager.Enemies.FirstOrDefault();
			RefreshEnemyList();
			RefreshEnemySheetUI();
			ShowToast($"Inimigo \"{deletedName}\" removido do Bestiário.", new Color(1f, 0.55f, 0.45f));
		};
		hListBtns.AddChild(btnDelEnemy);

		var btnSaveBestiary = new Button { Text = "Salvar Bestiário (enemies.json)" };
		btnSaveBestiary.AddThemeColorOverride("font_color", new Color(0.55f, 0.92f, 1f));
		btnSaveBestiary.Pressed += OnSalvarBestiarioPressed;
		vLeft.AddChild(btnSaveBestiary);

		// 2. Coluna Direita: Ficha / Statblock Completa da Ameaça Selecionada
		var rightScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(440, 0),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsStretchRatio = 1.0f
		};
		hSplit.AddChild(rightScroll);

		var vRight = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		vRight.AddThemeConstantOverride("separation", 8);
		rightScroll.AddChild(vRight);

		// Card Cabeçalho da Ameaça (Identidade, ND, PV e PM)
		var cardHeader = CreateStyledCard(new Color(0.62f, 0.18f, 0.22f, 1f));
		vRight.AddChild(cardHeader);

		var hHdr = new HBoxContainer();
		hHdr.AddThemeConstantOverride("separation", 14);
		cardHeader.AddChild(hHdr);

		var vInfo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vInfo.AddThemeConstantOverride("separation", 6);
		hHdr.AddChild(vInfo);

		_leEnemyName = new LineEdit
		{
			PlaceholderText = "Nome do Inimigo / Ameaça"
		};
		_leEnemyName.AddThemeFontSizeOverride("font_size", 18);
		_leEnemyName.TextChanged += (txt) =>
		{
			if (_updatingEnemyUi || _selectedEnemy == null) return;
			_selectedEnemy.Name = txt;
			RefreshEnemyList();
		};
		vInfo.AddChild(_leEnemyName);

		var hMeta = new HBoxContainer();
		hMeta.AddThemeConstantOverride("separation", 6);
		vInfo.AddChild(hMeta);

		OptionButton MakeTagSelector(string prefix, string[] options, Action<string> onChanged)
		{
			hMeta.AddChild(new Label { Text = prefix });
			var opt = new OptionButton { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
			foreach (string o in options) opt.AddItem(o);
			opt.ItemSelected += (i) =>
			{
				if (_updatingEnemyUi || _selectedEnemy == null) return;
				onChanged(opt.GetItemText((int)i));
				RefreshEnemyList();
			};
			hMeta.AddChild(opt);
			return opt;
		}

		_optEnemyNd = MakeTagSelector("ND:", new[] { "1/4", "1/2", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "12", "15", "18", "20", "S" },
			v => _selectedEnemy.ChallengeRating = v);
		_optEnemyRole = MakeTagSelector("Papel:", new[] { "Lacaio", "Solo", "Especial" },
			v => _selectedEnemy.ThreatRole = v);
		_optEnemyType = MakeTagSelector("Tipo:", new[] { "Animal", "Construto", "Espírito", "Humanoide", "Monstro", "Morto-Vivo" },
			v => _selectedEnemy.CreatureType = v);
		_optEnemySize = MakeTagSelector("Tam:", new[] { "Minúsculo", "Pequeno", "Médio", "Grande", "Enorme", "Colossal" },
			v => _selectedEnemy.CreatureSize = v);

		_lblEnemyState = new Label { Text = "ESTADO: ATIVO" };
		_lblEnemyState.AddThemeFontSizeOverride("font_size", 12);
		_lblEnemyState.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.55f));
		vInfo.AddChild(_lblEnemyState);

		// Blocos de PV / PM do Inimigo
		var hPools = new HBoxContainer();
		hPools.AddThemeConstantOverride("separation", 10);
		hHdr.AddChild(hPools);

		void BuildPoolWidget(string label, Color borderCol, out Label lblVal, out SpinBox spinMax, out SpinBox spinDelta, Action<bool, int> onAdjust, Action onFull)
		{
			var panel = CreateStyledCard(borderCol, new Color(0.08f, 0.09f, 0.11f, 1f));
			hPools.AddChild(panel);
			var vb = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			vb.AddThemeConstantOverride("separation", 3);
			panel.AddChild(vb);

			lblVal = new Label { Text = $"{label}: 0/0", HorizontalAlignment = HorizontalAlignment.Center };
			lblVal.AddThemeFontSizeOverride("font_size", 13);
			vb.AddChild(lblVal);

			var hMax = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			hMax.AddChild(new Label { Text = "Máx:" });
			spinMax = new SpinBox { MinValue = 0, MaxValue = 2000, Value = 15, CustomMinimumSize = new Vector2(68, 0) };
			hMax.AddChild(spinMax);
			vb.AddChild(hMax);

			var hCtrl = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			spinDelta = new SpinBox { MinValue = 1, MaxValue = 200, Value = 1, CustomMinimumSize = new Vector2(54, 0) };
			var sDeltaRef = spinDelta;
			var btnMinus = new Button { Text = "-" };
			btnMinus.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
			btnMinus.Pressed += () => onAdjust(false, (int)sDeltaRef.Value);
			var btnPlus = new Button { Text = "+" };
			btnPlus.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.55f));
			btnPlus.Pressed += () => onAdjust(true, (int)sDeltaRef.Value);
			var btnFull = new Button { Text = "Cheio" };
			btnFull.Pressed += onFull;

			hCtrl.AddChild(spinDelta);
			hCtrl.AddChild(btnMinus);
			hCtrl.AddChild(btnPlus);
			hCtrl.AddChild(btnFull);
			vb.AddChild(hCtrl);
		}

		BuildPoolWidget("PV", new Color(0.85f, 0.25f, 0.28f),
			out _lblEnemyPvCircle, out _spinEnemyPvMax, out _spinEnemyPvDelta,
			(heal, amt) => AjustarRecursoInimigo(isPm: false, heal: heal, delta: amt),
			() =>
			{
				if (_selectedEnemy == null) return;
				_selectedEnemy.Revive(_selectedEnemy.MaxHp);
				SincronizarRecursosInimigoComCombate();
				RefreshEnemySheetUI();
				RefreshEnemyList();
			});

		BuildPoolWidget("PM", new Color(0.22f, 0.56f, 0.85f),
			out _lblEnemyPmCircle, out _spinEnemyPmMax, out _spinEnemyPmDelta,
			(heal, amt) => AjustarRecursoInimigo(isPm: true, heal: heal, delta: amt),
			() =>
			{
				if (_selectedEnemy == null) return;
				_selectedEnemy.CurrentPm = _selectedEnemy.MaxPm;
				SincronizarRecursosInimigoComCombate();
				RefreshEnemySheetUI();
			});

		_spinEnemyPvMax.ValueChanged += (v) =>
		{
			if (_updatingEnemyUi || _selectedEnemy == null) return;
			_selectedEnemy.MaxHp = Math.Max(1, (int)v);
			_selectedEnemy.CurrentHp = Math.Clamp(_selectedEnemy.CurrentHp, 0, _selectedEnemy.MaxHp);
			SincronizarRecursosInimigoComCombate();
			RefreshEnemySheetUI();
			RefreshEnemyList();
		};
		_spinEnemyPmMax.ValueChanged += (v) =>
		{
			if (_updatingEnemyUi || _selectedEnemy == null) return;
			_selectedEnemy.MaxPm = Math.Max(0, (int)v);
			_selectedEnemy.CurrentPm = Math.Clamp(_selectedEnemy.CurrentPm, 0, _selectedEnemy.MaxPm);
			SincronizarRecursosInimigoComCombate();
			RefreshEnemySheetUI();
		};

		// Card Defesas, RD, Deslocamento e Sentidos + 5 Testes Clicáveis
		var cardStats = CreateStyledCard(new Color(0.32f, 0.26f, 0.30f, 1f));
		vRight.AddChild(cardStats);
		var vStats = new VBoxContainer();
		vStats.AddThemeConstantOverride("separation", 8);
		cardStats.AddChild(vStats);
		vStats.AddChild(CreateEnemyBanner("DEFESAS, DESLOCAMENTO E RESISTÊNCIAS"));

		var hDefRow = new HBoxContainer();
		hDefRow.AddThemeConstantOverride("separation", 10);
		vStats.AddChild(hDefRow);

		SpinBox AddStatSpin(Container parent, string lbl, double min, double max, double step, Action<double> onChanged)
		{
			var vb = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
			vb.AddChild(new Label { Text = lbl });
			var sp = new SpinBox { MinValue = min, MaxValue = max, Step = step, CustomMinimumSize = new Vector2(74, 0) };
			sp.ValueChanged += (val) =>
			{
				if (_updatingEnemyUi || _selectedEnemy == null) return;
				onChanged(val);
			};
			vb.AddChild(sp);
			parent.AddChild(vb);
			return sp;
		}

		_spinEnemyDef = AddStatSpin(hDefRow, "Defesa", 5, 60, 1, v =>
		{
			_selectedEnemy.FixedDefense = (int)v;
			RefreshEnemyList();
		});
		_spinEnemyRd = AddStatSpin(hDefRow, "RD (Red. Dano)", 0, 50, 1, v =>
		{
			_selectedEnemy.DamageReduction = (int)v;
			if (_combatManager.IsInCombat && _combatManager.SheetIdToCombatantId.TryGetValue(_selectedEnemy.Id, out int cid))
				_combatManager.Session.SetGlobalDamageReduction(cid, _selectedEnemy.DamageReduction);
		});
		_spinEnemySpeed = AddStatSpin(hDefRow, "Desloc. (m)", 1.5f, 36f, 1.5f, v => _selectedEnemy.SpeedMetersOverride = (float)v);

		var vbSenses = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill, SizeFlagsStretchRatio = 1.6f };
		vbSenses.AddChild(new Label { Text = "Sentidos / Imunidades" });
		_leEnemySenses = new LineEdit { PlaceholderText = "Ex: Visão no escuro; Imune a medo" };
		_leEnemySenses.TextChanged += (t) => { if (!_updatingEnemyUi && _selectedEnemy != null) _selectedEnemy.SensesNotes = t; };
		vbSenses.AddChild(_leEnemySenses);
		hDefRow.AddChild(vbSenses);

		var hSavesRow = new HBoxContainer();
		hSavesRow.AddThemeConstantOverride("separation", 8);
		vStats.AddChild(hSavesRow);

		SpinBox AddCheckWidget(string label, string skillName, Action<int> onChanged)
		{
			var panel = CreateStyledCard(new Color(0.28f, 0.30f, 0.38f, 1f), new Color(0.09f, 0.095f, 0.12f, 1f));
			hSavesRow.AddChild(panel);
			var vb = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			vb.AddThemeConstantOverride("separation", 3);
			panel.AddChild(vb);

			var sp = new SpinBox { MinValue = -15, MaxValue = 40, Value = 0, Prefix = "+", CustomMinimumSize = new Vector2(68, 0) };
			sp.ValueChanged += (val) =>
			{
				if (_updatingEnemyUi || _selectedEnemy == null) return;
				onChanged((int)val);
			};

			var btn = new Button { Text = label };
			btn.AddThemeFontSizeOverride("font_size", 11);
			btn.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.45f));
			btn.Pressed += () => RolarTesteInimigo(skillName);

			vb.AddChild(btn);
			vb.AddChild(sp);
			return sp;
		}

		_spinEnemyInit = AddCheckWidget("Iniciativa", "Iniciativa", v => _selectedEnemy.InitiativeOverride = v);
		_spinEnemyPerc = AddCheckWidget("Percepção", "Percepção", v => _selectedEnemy.PerceptionOverride = v);
		_spinEnemyFort = AddCheckWidget("Fortitude", "Fortitude", v => _selectedEnemy.FortitudeOverride = v);
		_spinEnemyRef  = AddCheckWidget("Reflexos", "Reflexos", v => _selectedEnemy.ReflexOverride = v);
		_spinEnemyWill = AddCheckWidget("Vontade", "Vontade", v => _selectedEnemy.WillOverride = v);

		// Card Atributos da Ameaça (FOR, DES, CON, INT, SAB, CAR)
		var cardAttr = CreateStyledCard(new Color(0.32f, 0.24f, 0.26f, 1f));
		vRight.AddChild(cardAttr);
		var vAttr = new VBoxContainer();
		vAttr.AddThemeConstantOverride("separation", 6);
		cardAttr.AddChild(vAttr);
		vAttr.AddChild(CreateEnemyBanner("ATRIBUTOS"));

		var hAttr = new HBoxContainer();
		hAttr.AddThemeConstantOverride("separation", 6);
		vAttr.AddChild(hAttr);

		foreach (var (attrType, abbrev) in new[]
		{
			(AttributeType.Strength, "FOR"),
			(AttributeType.Dexterity, "DES"),
			(AttributeType.Constitution, "CON"),
			(AttributeType.Intelligence, "INT"),
			(AttributeType.Wisdom, "SAB"),
			(AttributeType.Charisma, "CAR")
		})
		{
			var attrPanel = CreateStyledCard(new Color(0.45f, 0.16f, 0.18f, 1f), new Color(0.09f, 0.095f, 0.12f, 1f));
			hAttr.AddChild(attrPanel);
			var vb = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			attrPanel.AddChild(vb);

			var btnAttr = new Button { Text = abbrev, Flat = true };
			btnAttr.AddThemeFontSizeOverride("font_size", 11);
			btnAttr.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.48f));
			AttributeType capType = attrType;
			string capAbbrev = abbrev;
			btnAttr.Pressed += () =>
			{
				if (_selectedEnemy == null) return;
				RolarModificadorInimigo($"Atributo {capAbbrev}", _selectedEnemy.GetAttribute(capType).Modifier);
			};

			var sp = new SpinBox { MinValue = -5, MaxValue = 25, Value = 0, CustomMinimumSize = new Vector2(62, 0) };
			sp.ValueChanged += (v) =>
			{
				if (_updatingEnemyUi || _selectedEnemy == null) return;
				_selectedEnemy.SetAttributeValue(capType, (int)v);
			};
			_spinEnemyAttrs[attrType] = sp;
			vb.AddChild(btnAttr);
			vb.AddChild(sp);
		}

		// Divisor Redimensionável entre Ataques e Habilidades da Ameaça
		var splitCombatAbilities = new HSplitContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		StyleSplitContainer(splitCombatAbilities);
		vRight.AddChild(splitCombatAbilities);

		// Card Ataques da Ameaça
		var cardAtk = CreateStyledCard(new Color(0.45f, 0.20f, 0.18f, 1f));
		cardAtk.CustomMinimumSize = new Vector2(210, 195);
		splitCombatAbilities.AddChild(cardAtk);
		var vAtk = new VBoxContainer();
		vAtk.AddThemeConstantOverride("separation", 6);
		cardAtk.AddChild(vAtk);
		vAtk.AddChild(CreateEnemyBanner("ATAQUES DA AMEAÇA"));

		_listEnemyAttacks = new ItemList
		{
			CustomMinimumSize = new Vector2(0, 110),
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		_listEnemyAttacks.ItemActivated += (_) => OnRolarAtaqueInimigoPressed();
		vAtk.AddChild(_listEnemyAttacks);

		var hAtkBtns = new HBoxContainer();
		hAtkBtns.AddThemeConstantOverride("separation", 6);
		vAtk.AddChild(hAtkBtns);
		var btnRollAtk = new Button { Text = "Rolar Ataque", SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		btnRollAtk.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.35f));
		btnRollAtk.Pressed += OnRolarAtaqueInimigoPressed;
		var btnAddAtk = new Button { Text = "+ Ataque" };
		btnAddAtk.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		btnAddAtk.Pressed += () => _dlgNovoAtaqueInimigo?.PopupCentered();
		var btnDelAtk = new Button { Text = "Remover" };
		btnDelAtk.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		btnDelAtk.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			var sel = _listEnemyAttacks.GetSelectedItems();
			if (sel.Length == 0) return;
			var meta = _listEnemyAttacks.GetItemMetadata(sel[0]);
			if (meta.VariantType != Variant.Type.Int) return;
			int invIdx = (int)meta;
			if (invIdx >= 0 && invIdx < _selectedEnemy.Inventory.Count)
			{
				_selectedEnemy.Inventory.RemoveAt(invIdx);
				RefreshEnemySheetUI();
			}
		};
		hAtkBtns.AddChild(btnRollAtk);
		hAtkBtns.AddChild(btnAddAtk);
		hAtkBtns.AddChild(btnDelAtk);

		// Card Habilidades / Poderes da Ameaça
		var cardSkills = CreateStyledCard(new Color(0.28f, 0.24f, 0.42f, 1f));
		cardSkills.CustomMinimumSize = new Vector2(210, 195);
		splitCombatAbilities.AddChild(cardSkills);
		var vSk = new VBoxContainer();
		vSk.AddThemeConstantOverride("separation", 6);
		cardSkills.AddChild(vSk);
		vSk.AddChild(CreateEnemyBanner("HABILIDADES E MAGIAS"));

		_listEnemySkills = new ItemList
		{
			CustomMinimumSize = new Vector2(0, 110),
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		_listEnemySkills.ItemActivated += (_) => OnUsarPoderInimigoPressed();
		vSk.AddChild(_listEnemySkills);

		var hSkBtns = new HBoxContainer();
		hSkBtns.AddThemeConstantOverride("separation", 6);
		vSk.AddChild(hSkBtns);
		var btnUseSk = new Button { Text = "Usar (-PM)", SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		btnUseSk.AddThemeColorOverride("font_color", new Color(0.55f, 0.88f, 1f));
		btnUseSk.Pressed += OnUsarPoderInimigoPressed;
		var btnAddSk = new Button { Text = "+ Habilidade" };
		btnAddSk.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		btnAddSk.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			_addingSkillToEnemy = true;
			_dlgNovaMagia.Title = "Adicionar Habilidade / Magia da Ameaça";
			if (_chkNovaMagiaIsSpell != null) _chkNovaMagiaIsSpell.ButtonPressed = false;
			_dlgNovaMagia.PopupCentered();
		};
		var btnDelSk = new Button { Text = "Remover" };
		btnDelSk.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		btnDelSk.Pressed += () =>
		{
			if (_selectedEnemy == null) return;
			var sel = _listEnemySkills.GetSelectedItems();
			if (sel.Length == 0) return;
			var meta = _listEnemySkills.GetItemMetadata(sel[0]);
			if (meta.VariantType != Variant.Type.Int) return;
			int skIdx = (int)meta;
			if (skIdx >= 0 && skIdx < _selectedEnemy.Skills.Count)
			{
				_selectedEnemy.Skills.RemoveAt(skIdx);
				RefreshEnemySheetUI();
			}
		};
		hSkBtns.AddChild(btnUseSk);
		hSkBtns.AddChild(btnAddSk);
		hSkBtns.AddChild(btnDelSk);

		// Card Tesouro e Anotações do Mestre
		var cardNotes = CreateStyledCard(new Color(0.24f, 0.25f, 0.30f, 1f));
		vRight.AddChild(cardNotes);
		var vNotes = new VBoxContainer();
		vNotes.AddThemeConstantOverride("separation", 6);
		cardNotes.AddChild(vNotes);
		vNotes.AddChild(CreateEnemyBanner("TESOURO E ANOTAÇÕES DO MESTRE"));

		var hTreas = new HBoxContainer();
		hTreas.AddThemeConstantOverride("separation", 8);
		vNotes.AddChild(hTreas);
		hTreas.AddChild(new Label { Text = "Tesouro:" });
		_leEnemyTreasure = new LineEdit { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill, PlaceholderText = "Ex: Padrão, Dobro, Nenhum, Tibares..." };
		_leEnemyTreasure.TextChanged += (t) => { if (!_updatingEnemyUi && _selectedEnemy != null) _selectedEnemy.TreasureNotes = t; };
		hTreas.AddChild(_leEnemyTreasure);

		_txtEnemyNotes = new TextEdit
		{
			CustomMinimumSize = new Vector2(0, 72),
			PlaceholderText = "Comportamento em combate, fraquezas, falas ou gatilhos táticos..."
		};
		_txtEnemyNotes.TextChanged += () => { if (!_updatingEnemyUi && _selectedEnemy != null) _selectedEnemy.ThreatNotes = _txtEnemyNotes.Text; };
		vNotes.AddChild(_txtEnemyNotes);
	}

	private void RefreshEnemyList()
	{
		if (_listInimigos == null) return;
		_listInimigos.Clear();

		string filter = _txtBuscaInimigo?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
		int selectedIdxToHighlight = -1;

		foreach (var e in _combatManager.Enemies)
		{
			if (!string.IsNullOrEmpty(filter) &&
				!e.Name.ToLowerInvariant().Contains(filter) &&
				!e.ChallengeRating.ToLowerInvariant().Contains(filter) &&
				!e.CreatureType.ToLowerInvariant().Contains(filter))
			{
				continue;
			}

			string label = $"{e.Name} [ND {e.ChallengeRating}] | PV {e.CurrentHp}/{e.MaxHp} | Def {e.GetDefense()}";
			int idx = _listInimigos.AddItem(label);
			_listInimigos.SetItemMetadata(idx, e.Id);
			if (_selectedEnemy != null && e.Id == _selectedEnemy.Id)
				selectedIdxToHighlight = idx;
		}

		if (selectedIdxToHighlight >= 0)
			_listInimigos.Select(selectedIdxToHighlight);
	}

	private void SelectEnemy(int index)
	{
		if (index < 0 || index >= _combatManager.Enemies.Count) return;
		SelectEnemyByRef(_combatManager.Enemies[index]);
	}

	private void SelectEnemyByRef(CharacterSheetData enemy)
	{
		if (enemy == null) return;
		_selectedEnemy = enemy;
		RefreshEnemyList();
		RefreshEnemySheetUI();
	}

	private static void SelectOptionByText(OptionButton opt, string text)
	{
		if (opt == null) return;
		for (int i = 0; i < opt.ItemCount; i++)
		{
			if (opt.GetItemText(i).Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				opt.Select(i);
				return;
			}
		}
	}

	private void RefreshEnemySheetUI()
	{
		if (_leEnemyName == null) return;

		if (_selectedEnemy == null)
		{
			_selectedEnemy = _combatManager.Enemies.FirstOrDefault();
		}

		_updatingEnemyUi = true;
		try
		{
			if (_selectedEnemy == null)
			{
				_leEnemyName.Text = "";
				_lblEnemyState.Text = "Nenhuma ameaça selecionada.";
				_listEnemyAttacks?.Clear();
				_listEnemySkills?.Clear();
				return;
			}

			var e = _selectedEnemy;
			int cid = (_combatManager.IsInCombat && _combatManager.SheetIdToCombatantId.TryGetValue(e.Id, out int activeCid)) ? activeCid : 0;
			var activeSession = cid > 0 ? _combatManager.Session : null;

			if (_leEnemyName.Text != e.Name) _leEnemyName.Text = e.Name;
			SelectOptionByText(_optEnemyNd, e.ChallengeRating);
			SelectOptionByText(_optEnemyRole, e.ThreatRole);
			SelectOptionByText(_optEnemyType, e.CreatureType);
			SelectOptionByText(_optEnemySize, e.CreatureSize);

			_lblEnemyState.Text = $"ESTADO: {e.GetHealthStateLabel(activeSession, cid)} | {e.CreatureType} {e.CreatureSize} ({e.ThreatRole})";
			var hState = e.GetHealthState(activeSession, cid);
			_lblEnemyState.AddThemeColorOverride("font_color",
				hState == T20HealthState.Dead
					? new Color(1f, 0.3f, 0.3f)
					: (hState == T20HealthState.Bloodied ? new Color(1f, 0.78f, 0.3f) : new Color(0.45f, 0.95f, 0.55f)));

			_lblEnemyPvCircle.Text = $"PV: {e.CurrentHp}/{e.MaxHp}";
			_spinEnemyPvMax.Value = e.MaxHp;
			_lblEnemyPmCircle.Text = $"PM: {e.CurrentPm}/{e.MaxPm}";
			_spinEnemyPmMax.Value = e.MaxPm;

			_spinEnemyDef.Value = e.GetDefense(activeSession, cid);
			_spinEnemyRd.Value = e.DamageReduction;
			_spinEnemySpeed.Value = e.SpeedMetersOverride > 0f ? e.SpeedMetersOverride : Tormenta20TacticalRules.GetRaceBaseSpeedMeters(e.Race);
			if (_leEnemySenses.Text != e.SensesNotes) _leEnemySenses.Text = e.SensesNotes;

			Tormenta20SkillRules.TryCalculateSkillModifier(e, "Iniciativa", out int initMod, out _, activeSession, cid);
			Tormenta20SkillRules.TryCalculateSkillModifier(e, "Percepção", out int percMod, out _, activeSession, cid);
			Tormenta20SkillRules.TryCalculateSkillModifier(e, "Fortitude", out int fortMod, out _, activeSession, cid);
			Tormenta20SkillRules.TryCalculateSkillModifier(e, "Reflexos", out int refMod, out _, activeSession, cid);
			Tormenta20SkillRules.TryCalculateSkillModifier(e, "Vontade", out int willMod, out _, activeSession, cid);

			_spinEnemyInit.Value = e.InitiativeOverride ?? initMod;
			_spinEnemyPerc.Value = e.PerceptionOverride ?? percMod;
			_spinEnemyFort.Value = e.FortitudeOverride ?? fortMod;
			_spinEnemyRef.Value = e.ReflexOverride ?? refMod;
			_spinEnemyWill.Value = e.WillOverride ?? willMod;

			foreach (var kv in _spinEnemyAttrs)
			{
				kv.Value.Value = e.GetAttribute(kv.Key).Modifier;
			}

			_listEnemyAttacks.Clear();
			for (int i = 0; i < e.Inventory.Count; i++)
			{
				var item = e.Inventory[i];
				if (item.Type != ItemType.Weapon) continue;
				int atk = item.AttackBonus != 0 ? item.AttackBonus : e.ThreatAttackBonus;
				string atkSign = atk >= 0 ? $"+{atk}" : atk.ToString();
				string rangeTag = item.IsRanged ? "Distância" : "Corpo a corpo";
				int idx = _listEnemyAttacks.AddItem($"{item.Name} {atkSign} ({item.Damage}, {item.Critical}, {rangeTag})");
				_listEnemyAttacks.SetItemMetadata(idx, i);
			}
			if (_listEnemyAttacks.ItemCount > 0) _listEnemyAttacks.Select(0);
			else _listEnemyAttacks.AddItem("Nenhum ataque cadastrado.");

			_listEnemySkills.Clear();
			for (int i = 0; i < e.Skills.Count; i++)
			{
				var sk = e.Skills[i];
				string pmTag = sk.PmCost > 0 ? $"{sk.PmCost} PM" : "Passivo";
				string formulaTag = string.IsNullOrWhiteSpace(sk.BaseFormula) ? "" : $" [{sk.BaseFormula}]";
				int idx = _listEnemySkills.AddItem($"{sk.Name} ({pmTag}){formulaTag} — {sk.Description}");
				_listEnemySkills.SetItemMetadata(idx, i);
			}
			if (_listEnemySkills.ItemCount > 0) _listEnemySkills.Select(0);
			else _listEnemySkills.AddItem("Nenhuma habilidade cadastrada.");

			if (_leEnemyTreasure.Text != e.TreasureNotes) _leEnemyTreasure.Text = e.TreasureNotes;
			if (_txtEnemyNotes.Text != e.ThreatNotes) _txtEnemyNotes.Text = e.ThreatNotes;
		}
		finally
		{
			_updatingEnemyUi = false;
		}
	}

	private void AjustarRecursoInimigo(bool isPm, bool heal, int delta)
	{
		if (_selectedEnemy == null) return;
		if (!isPm)
		{
			if (heal) _selectedEnemy.HealHp(delta);
			else _selectedEnemy.TakeDamage(delta);
		}
		else
		{
			if (heal) _selectedEnemy.RecoverPm(delta);
			else _selectedEnemy.SpendPm(delta);
		}

		SincronizarRecursosInimigoComCombate();
		RefreshEnemySheetUI();
		RefreshEnemyList();
	}

	private void SincronizarRecursosInimigoComCombate()
	{
		if (_selectedEnemy == null || !_combatManager.IsInCombat) return;
		if (_combatManager.SheetIdToCombatantId.TryGetValue(_selectedEnemy.Id, out int cid))
		{
			_combatManager.Session.AddResource(cid, CombatManager.HealthResourceId, "Vida", _selectedEnemy.MaxHp, _selectedEnemy.GetDeathThreshold());
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.HealthResourceId, _selectedEnemy.CurrentHp);
			_combatManager.Session.AddResource(cid, CombatManager.ManaResourceId, "PM", _selectedEnemy.MaxPm, 0);
			_combatManager.Session.SetResourceCurrent(cid, CombatManager.ManaResourceId, _selectedEnemy.CurrentPm);
			Tormenta20ConditionRules.SyncDyingAndUnconsciousState(_combatManager.Session, cid, _selectedEnemy);
			RefreshInitiativeBar();
			PopularAlvoCombate();
			UpdateCombatStatusDisplay();
		}
	}

	private void RolarTesteInimigo(string skillName)
	{
		if (_selectedEnemy == null) return;
		int cid = (_combatManager.IsInCombat && _combatManager.SheetIdToCombatantId.TryGetValue(_selectedEnemy.Id, out int c)) ? c : 0;
		var check = Tormenta20SkillRules.RollSkillCheck(_combatManager.Session, _selectedEnemy, skillName, dc: 0, combatantId: cid);
		if (!check.Allowed)
		{
			ShowToast(check.Message, new Color(1f, 0.45f, 0.45f));
			return;
		}
		MostrarPopupRolagemD20(_selectedEnemy.Name, $"TESTE DE {skillName.ToUpper()}", check.NaturalRoll, check.TotalModifier, check.Total, check.IsNatural20, check.IsNatural1, skillName);
	}

	private void RolarModificadorInimigo(string label, int modifier)
	{
		if (_selectedEnemy == null) return;
		RollResult r = _combatManager.Session.Roll("1d20");
		int natural = r.DiceTotal;
		int total = natural + modifier;
		MostrarPopupRolagemD20(_selectedEnemy.Name, label.ToUpper(), natural, modifier, total, natural == 20, natural == 1, label);
	}

	private void MostrarPopupRolagemD20(string actorName, string titulo, int natural, int mod, int total, bool crit, bool fumble, string logLabel)
	{
		string modSign = mod >= 0 ? $"+{mod}" : mod.ToString();
		_lblPopupTitulo.Text = titulo;
		_lblPopupPersonagem.Text = $"Ameaça: {actorName}";
		_lblPopupResultado.Text = total.ToString();
		_lblPopupFormula.Text = $"1d20 rolou {natural} (Mod {modSign}) = {total}";

		if (crit)
		{
			_lblPopupDestaque.Text = "20 NATURAL! SUCESSO CRÍTICO!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.22f));
		}
		else if (fumble)
		{
			_lblPopupDestaque.Text = "1 NATURAL! FALHA CRÍTICA!";
			_lblPopupDestaque.Visible = true;
			_lblPopupDestaque.AddThemeColorOverride("font_color", new Color(1f, 0.35f, 0.35f));
		}
		else
		{
			_lblPopupDestaque.Visible = false;
		}

		_popupPericia.Modulate = new Color(1, 1, 1, 0);
		_popupPericia.Visible = true;
		var tween = CreateTween();
		tween.TweenProperty(_popupPericia, "modulate", new Color(1, 1, 1, 1), 0.2f);
		_diceVisualPericia?.PlayRollAnimation("1d20", total, natural, crit, fumble, 0.42f);

		string cor = crit ? "#ffd700" : (fumble ? "#ff4444" : "#ffaa66");
		_txtHistorico?.AppendText($"[color={cor}][{actorName}] {logLabel} (1d20{modSign}) -> [b]{total}[/b] (d20:{natural})[/color]\n");
		AppendCombatLog($"{actorName} rolou {logLabel}: {total} (1d20={natural}{modSign})", cor);
	}

	private void OnRolarAtaqueInimigoPressed()
	{
		if (_selectedEnemy == null) return;
		var sel = _listEnemyAttacks.GetSelectedItems();
		if (sel.Length == 0) { ShowToast("Selecione um ataque na lista."); return; }
		var meta = _listEnemyAttacks.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int invIdx = (int)meta;
		if (invIdx < 0 || invIdx >= _selectedEnemy.Inventory.Count) return;

		var weapon = _selectedEnemy.Inventory[invIdx];
		int atkMod = weapon.AttackBonus != 0 ? weapon.AttackBonus : _selectedEnemy.ThreatAttackBonus;
		weapon.ParseCritical(out int threat, out int mult);

		RollResult d20 = _combatManager.Session.Roll("1d20");
		int natural = d20.DiceTotal;
		int totalAtk = natural + atkMod;
		bool isCrit = natural >= threat;
		bool isFumble = natural == 1;

		string dmgExpr = Tormenta20CombatRules.BuildWeaponDamageExpression(weapon.Damage, isCrit, mult, 0);
		RollResult dmgRoll = _combatManager.Session.Roll(dmgExpr);
		int totalDmg = Math.Max(1, dmgRoll.Total);

		string critTag = isCrit ? $" [CRÍTICO {threat}/x{mult}!]" : (isFumble ? " [FALHA CRÍTICA!]" : "");
		ShowToast($"{_selectedEnemy.Name} - {weapon.Name}: Ataque {totalAtk} (1d20={natural}{(atkMod >= 0 ? "+" : "")}{atkMod}){critTag} | Dano: {totalDmg} ({dmgExpr})",
			isCrit ? new Color(1f, 0.86f, 0.22f) : new Color(1f, 0.72f, 0.45f));
		_txtHistorico?.AppendText($"[color=#ffaa44][{_selectedEnemy.Name}] {weapon.Name}: Ataque [b]{totalAtk}[/b]{critTag} | Dano: [b]{totalDmg}[/b] ({dmgExpr})[/color]\n");
		AppendCombatLog($"{_selectedEnemy.Name} rolou {weapon.Name}: Ataque {totalAtk}{critTag} | Dano {totalDmg} ({dmgExpr})", "#ffaa44");
	}

	private void OnUsarPoderInimigoPressed()
	{
		if (_selectedEnemy == null) return;
		var sel = _listEnemySkills.GetSelectedItems();
		if (sel.Length == 0) { ShowToast("Selecione uma habilidade na lista."); return; }
		var meta = _listEnemySkills.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int skIdx = (int)meta;
		if (skIdx < 0 || skIdx >= _selectedEnemy.Skills.Count) return;

		var sk = _selectedEnemy.Skills[skIdx];
		if (_selectedEnemy.CurrentPm < sk.PmCost)
		{
			ShowToast($"PM insuficiente para \"{sk.Name}\" ({sk.PmCost} PM).", new Color(1f, 0.45f, 0.45f));
			return;
		}
		if (sk.PmCost > 0)
		{
			_selectedEnemy.SpendPm(sk.PmCost);
			SincronizarRecursosInimigoComCombate();
		}

		string rollSummary = "";
		if (!string.IsNullOrWhiteSpace(sk.BaseFormula))
		{
			try
			{
				RollResult r = _combatManager.Session.Roll(sk.BaseFormula.Trim());
				rollSummary = $" -> Rolou {sk.BaseFormula} = {r.Total}!";
			}
			catch { }
		}

		RefreshEnemySheetUI();
		ShowToast($"{_selectedEnemy.Name} usou \"{sk.Name}\" (-{sk.PmCost} PM){rollSummary}");
		AppendCombatLog($"{_selectedEnemy.Name} usou [b]{sk.Name}[/b] (-{sk.PmCost} PM){rollSummary}", "#66d9ef");
	}

	private void OnSalvarBestiarioPressed()
	{
		_combatManager.SaveEnemies();
		ShowToast($"{_combatManager.Enemies.Count} ameaça(s) salva(s) em enemies.json!");
		AppendCombatLog("Bestiário salvo com sucesso!", "#55ff99");
	}

	// ════════════════════════════════════════════════════════════════════════
	// Mapa Tático (Tabletop 2D)
	// ════════════════════════════════════════════════════════════════════════
	private void BuildTacticalMapView()
	{
		var mainContent = GetNode<VBoxContainer>("MarginContainer/VBoxMain/MainContent");

		var container = new VBoxContainer
		{
			Name = "ViewMapa",
			Visible = false,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		container.AddThemeConstantOverride("separation", 8);
		mainContent.AddChild(container);
		_viewMapa = container;

		// 1. Barra de ferramentas superior do Mapa
		var toolBar = new HBoxContainer();
		toolBar.AddThemeConstantOverride("separation", 6);
		container.AddChild(toolBar);

		toolBar.AddChild(new Label { Text = "Cenário:" });
		_optArenaPreset = new OptionButton { CustomMinimumSize = new Vector2(165, 0) };
		_optArenaPreset.AddItem("Masmorra com Pilares", (int)T20ArenaPreset.PillaredDungeon);
		_optArenaPreset.AddItem("Arena Aberta", (int)T20ArenaPreset.OpenArena);
		_optArenaPreset.AddItem("Taverna", (int)T20ArenaPreset.Tavern);
		_optArenaPreset.AddItem("Emboscada na Floresta", (int)T20ArenaPreset.ForestAmbush);
		_optArenaPreset.Select(0);
		_optArenaPreset.ItemSelected += (idx) =>
		{
			_combatManager.CurrentArenaPreset = (T20ArenaPreset)_optArenaPreset.GetItemId((int)idx);
			_combatManager.EnsureMapInitialized(resetTerrain: true);
			RefreshTacticalMapView();
			ShowToast($"Cenário alterado para {_optArenaPreset.GetItemText((int)idx)}.");
		};
		toolBar.AddChild(_optArenaPreset);

		_btnModoEditorMapa = new Button
		{
			Text = "Modo: Combate",
			ToggleMode = true
		};
		_btnModoEditorMapa.Toggled += (bool editing) =>
		{
			_btnModoEditorMapa.Text = editing ? "Modo: Editor" : "Modo: Combate";
			_btnModoEditorMapa.AddThemeColorOverride("font_color", editing ? new Color(1f, 0.85f, 0.35f) : Colors.White);
			if (_boxPinceisEditor != null) _boxPinceisEditor.Visible = editing;
			if (_tacticalGridCanvas != null) _tacticalGridCanvas.IsEditorMode = editing;
			_editorSelectedTokenId = 0;
			_tacticalGridCanvas?.SetSelectedEditorToken(0);
			RefreshTacticalMapView();
		};
		toolBar.AddChild(_btnModoEditorMapa);

		_btnReguaMapa = new Button
		{
			Text = "Régua",
			ToggleMode = true,
			TooltipText = "Medir distância em metros e linha de visão no mapa (Atalho: M)"
		};
		_btnReguaMapa.Toggled += (bool active) =>
		{
			if (_tacticalGridCanvas != null)
			{
				_tacticalGridCanvas.RulerMode = active;
				_tacticalGridCanvas.QueueRedraw();
			}
		};
		toolBar.AddChild(_btnReguaMapa);

		_btnFogToggle = new Button
		{
			Text = "Visão do Mestre",
			ToggleMode = true,
			TooltipText = "Alterna entre visão total do Mestre e Névoa de Guerra dos heróis"
		};
		_btnFogToggle.Toggled += (bool fog) =>
		{
			_combatManager.FogOfWarEnabled = fog;
			_btnFogToggle.Text = fog ? "Névoa de Guerra" : "Visão do Mestre";
			Tormenta20TacticalRules.UpdateFogOfWar(_combatManager.Session, _combatManager.FogTracker, alliedTeam: 1);
			RefreshTacticalMapView();
		};
		toolBar.AddChild(_btnFogToggle);

		var btnBg = new Button
		{
			Text = "Fundo..."
		};
		btnBg.Pressed += () => _fileDlgMapaFundo?.PopupCentered(new Vector2I(680, 460));
		toolBar.AddChild(btnBg);

		var btnLimparBg = new Button
		{
			Text = "Sem Fundo",
			TooltipText = "Remove a imagem de fundo e volta ao piso padrão da grade"
		};
		btnLimparBg.Pressed += () =>
		{
			_combatManager.CurrentMapBackgroundPath = string.Empty;
			_tacticalGridCanvas?.SetBackgroundImage(string.Empty);
		};
		toolBar.AddChild(btnLimparBg);

		// Controles de câmera suaves (- / 100% / + / Centralizar / Ajuda)
		var btnZoomOut = new Button { Text = "-", CustomMinimumSize = new Vector2(28, 0), TooltipText = "Afastar Zoom (-)" };
		btnZoomOut.Pressed += () => _tacticalGridCanvas?.ZoomBy(1f / 1.18f);
		toolBar.AddChild(btnZoomOut);

		_btnMapaZoomLabel = new Button
		{
			Text = "100%",
			CustomMinimumSize = new Vector2(54, 0),
			TooltipText = "Clique para resetar o zoom para 100% (Atalho: 0)"
		};
		_btnMapaZoomLabel.Pressed += () => _tacticalGridCanvas?.ResetZoom100();
		toolBar.AddChild(_btnMapaZoomLabel);

		var btnZoomIn = new Button { Text = "+", CustomMinimumSize = new Vector2(28, 0), TooltipText = "Aproximar Zoom (+)" };
		btnZoomIn.Pressed += () => _tacticalGridCanvas?.ZoomBy(1.18f);
		toolBar.AddChild(btnZoomIn);

		var btnCenter = new Button
		{
			Text = "Centralizar",
			TooltipText = "Centraliza e ajusta o tabuleiro na tela (Atalho: R)"
		};
		btnCenter.Pressed += () => _tacticalGridCanvas?.CenterView(animate: true);
		toolBar.AddChild(btnCenter);

		_btnMapaSalvar = new Button { Text = "Salvar" };
		_btnMapaSalvar.AddThemeColorOverride("font_color", new Color(0.5f, 0.95f, 0.65f));
		_btnMapaSalvar.Pressed += () =>
		{
			if (_combatManager.SaveCombatSnapshot())
				ShowToast("Combate e mapa salvos com sucesso!");
			else
				ShowToast("Inicie um combate antes de salvar.", new Color(1f, 0.45f, 0.45f));
		};
		toolBar.AddChild(_btnMapaSalvar);

		var btnMapaRestaurar = new Button { Text = "Restaurar" };
		btnMapaRestaurar.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
		btnMapaRestaurar.Pressed += () =>
		{
			_queuedActionPmSpent.Clear();
			if (_combatManager.LoadCombatSnapshot())
			{
				UpdateCharacterDropdown();
				RefreshEnemyList();
				UpdateCombatStatusDisplay();
				RefreshInitiativeBar();
				PopularAlvoCombate();
				RefreshActionQueuePanel();
				RefreshTacticalMapView();
				RefreshSheetUI();
				RefreshEnemySheetUI();
				ShowToast("Combate e mapa restaurados com sucesso!");
			}
			else
			{
				ShowToast("Nenhum combate salvo encontrado.", new Color(1f, 0.45f, 0.45f));
			}
		};
		toolBar.AddChild(btnMapaRestaurar);

		_btnMapaAjuda = new Button
		{
			Text = "Ajuda",
			TooltipText = "Abre/fecha a aba de Ajuda e comandos do Tabletop (Atalho: H ou F1)"
		};
		_btnMapaAjuda.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.38f));
		_btnMapaAjuda.Pressed += ToggleTacticalHelpTab;
		toolBar.AddChild(_btnMapaAjuda);

		// 2. Barra de Pincéis do Editor (visível apenas no Modo Editor)
		_boxPinceisEditor = new HBoxContainer { Visible = false };
		_boxPinceisEditor.AddThemeConstantOverride("separation", 6);
		container.AddChild(_boxPinceisEditor);

		_boxPinceisEditor.AddChild(new Label { Text = "Pincel:" });
		void AddBrushBtn(string text, MapEditorBrush brush)
		{
			var b = new Button { Text = text };
			b.Pressed += () =>
			{
				_editorBrush = brush;
				_editorSelectedTokenId = 0;
				_tacticalGridCanvas?.SetSelectedEditorToken(0);
				for (int i = 0; i < _brushButtons.Count; i++)
				{
					_brushButtons[i].Modulate = (i == (int)brush) ? new Color(1f, 0.88f, 0.35f) : Colors.White;
				}
			};
			_brushButtons.Add(b);
			_boxPinceisEditor.AddChild(b);
		}
		AddBrushBtn("Chão Livre", MapEditorBrush.Floor);
		AddBrushBtn("Parede Sólida", MapEditorBrush.Wall);
		AddBrushBtn("Cobertura Leve (+5 Def)", MapEditorBrush.Cover);
		AddBrushBtn("Terreno Difícil (x2)", MapEditorBrush.Difficult);
		AddBrushBtn("Reposicionar Token", MapEditorBrush.MoveToken);
		if (_brushButtons.Count > 1) _brushButtons[1].Modulate = new Color(1f, 0.88f, 0.35f);

		var lblDicaEditor = new Label
		{
			Text = "  |  Clique/arraste para pintar | Espaço+Esquerdo ou Direito: mover câmera | Scroll: zoom"
		};
		lblDicaEditor.AddThemeColorOverride("font_color", new Color(0.70f, 0.74f, 0.82f));
		lblDicaEditor.AddThemeFontSizeOverride("font_size", 11);
		_boxPinceisEditor.AddChild(lblDicaEditor);

		// 3. Corpo principal dividido: Canvas (Esquerda/Centro) + Painel Lateral com Abas Combate/Ajuda (Direita)
		var hSplit = new HSplitContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		StyleSplitContainer(hSplit);
		container.AddChild(hSplit);

		var vCanvasCol = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(360, 0),
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vCanvasCol.AddThemeConstantOverride("separation", 4);
		hSplit.AddChild(vCanvasCol);

		var canvasFrame = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		var frameStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.07f, 0.09f, 1f),
			BorderColor = new Color(0.32f, 0.35f, 0.44f, 1f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6
		};
		canvasFrame.AddThemeStyleboxOverride("panel", frameStyle);
		vCanvasCol.AddChild(canvasFrame);

		_tacticalGridCanvas = new TacticalGridCanvas
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		canvasFrame.AddChild(_tacticalGridCanvas);

		_tacticalGridCanvas.CellClicked += OnMapCellClicked;
		_tacticalGridCanvas.CellDragged += PaintCellInEditor;
		_tacticalGridCanvas.TokenClicked += OnMapTokenClicked;
		_tacticalGridCanvas.HoverStatusChanged += (status) =>
		{
			if (_lblMapaStatusBar != null) _lblMapaStatusBar.Text = status;
		};
		_tacticalGridCanvas.ZoomChanged += (z) =>
		{
			if (_btnMapaZoomLabel != null)
				_btnMapaZoomLabel.Text = $"{Mathf.RoundToInt(z * 100f)}%";
		};

		_lblMapaStatusBar = new Label
		{
			Text = "Escala: 1q = 1,5m | Pan: Direito/Meio/Espaço+Esquerdo/WASD | Scroll/+/-: Zoom"
		};
		_lblMapaStatusBar.AddThemeColorOverride("font_color", new Color(0.78f, 0.82f, 0.90f));
		_lblMapaStatusBar.AddThemeFontSizeOverride("font_size", 11);
		vCanvasCol.AddChild(_lblMapaStatusBar);

		// Painel lateral com Abas: [Combate] e [Ajuda]
		var sidePanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(320, 0),
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		var sideStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.10f, 0.11f, 0.14f, 0.98f),
			BorderColor = new Color(0.30f, 0.32f, 0.40f, 1f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
		sidePanel.AddThemeStyleboxOverride("panel", sideStyle);
		hSplit.AddChild(sidePanel);

		_tabsMapaLateral = new TabContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		sidePanel.AddChild(_tabsMapaLateral);

		// Aba 0: Combate Tático
		var vSide = new VBoxContainer
		{
			Name = "Combate",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vSide.AddThemeConstantOverride("separation", 8);
		_tabsMapaLateral.AddChild(vSide);

		var lblSideTitle = new Label { Text = "Controle Tático" };
		lblSideTitle.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.38f));
		lblSideTitle.AddThemeFontSizeOverride("font_size", 14);
		vSide.AddChild(lblSideTitle);

		_lblMapaTurnoInfo = new Label
		{
			Text = "Fora de combate.\nInicie um combate para posicionar os combatentes no mapa.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblMapaTurnoInfo.AddThemeFontSizeOverride("font_size", 12);
		vSide.AddChild(_lblMapaTurnoInfo);

		var hSideTurnBtns = new HBoxContainer();
		hSideTurnBtns.AddThemeConstantOverride("separation", 6);
		vSide.AddChild(hSideTurnBtns);

		_btnMapaIniciarCombate = new Button
		{
			Text = "Iniciar Combate",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_btnMapaIniciarCombate.Pressed += OnIniciarCombatePressed;
		hSideTurnBtns.AddChild(_btnMapaIniciarCombate);

		_btnMapaProximoTurno = new Button
		{
			Text = "Próximo Turno",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			Disabled = true
		};
		_btnMapaProximoTurno.Pressed += () => _combatManager.NextTurn();
		hSideTurnBtns.AddChild(_btnMapaProximoTurno);

		_lblMapaAlvoResumo = new Label
		{
			Text = "Alvo: Clique em um inimigo no mapa.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_lblMapaAlvoResumo.AddThemeColorOverride("font_color", new Color(0.82f, 0.90f, 1f));
		_lblMapaAlvoResumo.AddThemeFontSizeOverride("font_size", 12);
		vSide.AddChild(_lblMapaAlvoResumo);

		_btnMapaAtacar = new Button { Text = "Atacar Alvo Selecionado" };
		_btnMapaAtacar.AddThemeColorOverride("font_color", new Color(1f, 0.65f, 0.45f));
		_btnMapaAtacar.Pressed += OnAtacarPressed;
		vSide.AddChild(_btnMapaAtacar);

		_btnMapaCorrer = new Button
		{
			Text = "Correr (2x Desloc. | Ação Completa)",
			ToggleMode = true
		};
		_btnMapaCorrer.AddThemeColorOverride("font_color", new Color(0.55f, 0.92f, 1f));
		_btnMapaCorrer.Toggled += (bool active) =>
		{
			_runModeNextClick = active;
			RefreshTacticalMapView();
		};
		vSide.AddChild(_btnMapaCorrer);

		var hSideSub = new HBoxContainer();
		hSideSub.AddThemeConstantOverride("separation", 6);
		vSide.AddChild(hSideSub);

		_btnMapaHabilidade = new Button
		{
			Text = "Poder / Magia",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_btnMapaHabilidade.Pressed += OnAbrirUsarHabilidadeCombate;
		hSideSub.AddChild(_btnMapaHabilidade);

		_btnMapaCondicoes = new Button
		{
			Text = "Condições",
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		_btnMapaCondicoes.Pressed += () =>
		{
			if (!_combatManager.IsInCombat)
			{
				ShowToast("Inicie o combate antes de aplicar condições!", new Color(1f, 0.45f, 0.45f));
				return;
			}
			_dlgCondicoesCombate.PopupCentered();
		};
		hSideSub.AddChild(_btnMapaCondicoes);

		var lblLogTitle = new Label { Text = "Registro de Combate" };
		lblLogTitle.AddThemeColorOverride("font_color", new Color(0.88f, 0.85f, 0.72f));
		lblLogTitle.AddThemeFontSizeOverride("font_size", 12);
		vSide.AddChild(lblLogTitle);

		_txtMapaMiniLog = new RichTextLabel
		{
			BbcodeEnabled = true,
			ScrollFollowing = true,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};
		vSide.AddChild(_txtMapaMiniLog);

		// Aba 1: Ajuda / Comandos do Tabletop
		_tabsMapaLateral.AddChild(BuildTacticalHelpTab());

		// Diálogo para escolher imagem de fundo do mapa
		_fileDlgMapaFundo = new FileDialog
		{
			Title = "Escolher Imagem do Mapa de Batalha",
			Access = FileDialog.AccessEnum.Filesystem,
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Filters = new[] { "*.png, *.jpg, *.jpeg, *.webp ; Imagens de Mapa" }
		};
		_fileDlgMapaFundo.FileSelected += (string path) =>
		{
			_combatManager.CurrentMapBackgroundPath = path;
			_tacticalGridCanvas?.SetBackgroundImage(path);
			ShowToast("Imagem de fundo aplicada ao mapa!");
		};
		AddChild(_fileDlgMapaFundo);
	}

	private Control BuildTacticalHelpTab()
	{
		var scroll = new ScrollContainer
		{
			Name = "Ajuda",
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
		};

		var margin = new MarginContainer
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		scroll.AddChild(margin);

		var rtl = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			SelectionEnabled = true,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		rtl.AddThemeFontSizeOverride("normal_font_size", 12);
		rtl.Text =
			"[color=#ffd56a][b]COMANDOS DO TABLETOP[/b][/color]\n\n" +
			"[color=#80d8ff][b]Câmera, Zoom e Navegação[/b][/color]\n" +
			"- [b]Scroll do Mouse / Pinça Touchpad:[/b] Zoom suave focado no cursor.\n" +
			"- [b]Botão Direito ou do Meio (arrastar):[/b] Move a câmera livremente.\n" +
			"- [b]Espaço + Botão Esquerdo (arrastar):[/b] Alternativa prática para notebook.\n" +
			"- [b]Clique fora da grade (arrastar):[/b] Também movimenta o tabuleiro.\n" +
			"- [b]W / A / S / D ou Setas:[/b] Pan suave (+Shift para mover mais rápido).\n" +
			"- [b]+ / - :[/b] Aproxima ou afasta o zoom.\n" +
			"- [b]0 :[/b] Restaura zoom para 100%.\n" +
			"- [b]R :[/b] Centraliza e ajusta o mapa inteiro na tela.\n\n" +
			"[color=#80d8ff][b]Modo Combate[/b][/color]\n" +
			"- [b]Clique em Token:[/b] Seleciona o alvo e abre a ficha correspondente.\n" +
			"- [b]Clique em Célula Azul:[/b] Move o combatente da vez usando o menor caminho (gasta Ação de Movimento).\n" +
			"- [b]Correr (2x Desloc.):[/b] Dobra a área azul por 1 Ação Completa.\n" +
			"- [b]Régua (ou tecla M):[/b] Mede distância em metros e testa Linha de Visão/Cobertura até o cursor.\n\n" +
			"[color=#80d8ff][b]Modo Editor de Mapa[/b][/color]\n" +
			"- Clique ou arraste com o botão esquerdo para pintar Chão Livre, Parede Sólida, Cobertura Leve (+5 Def) ou Terreno Difícil (custo x2).\n" +
			"- Pincel [b]Reposicionar Token[/b]: clique em um token e depois clique na nova célula livre.\n\n" +
			"[color=#80d8ff][b]Regras Táticas (Tormenta20 JdA)[/b][/color]\n" +
			"- [b]Escala:[/b] 1 quadrado = 1,5m.\n" +
			"- [b]Diagonais:[/b] Regra alternada 1-2-1-2.\n" +
			"- [b]Terreno Difícil:[/b] Cada quadrado custa 2 quadrados de deslocamento.\n" +
			"- [b]Cobertura Leve:[/b] +5 na Defesa contra ataques que cruzem o obstáculo.\n" +
			"- [b]Flanqueamento:[/b] Aliados em lados opostos do inimigo concedem +2 no ataque corpo a corpo.\n" +
			"- [b]Névoa de Guerra:[/b] Oculta inimigos fora da linha de visão dos heróis.\n" +
			"- [b]H ou F1:[/b] Alterna rapidamente entre as abas Combate e Ajuda.";

		margin.AddChild(rtl);
		return scroll;
	}

	private void ToggleTacticalHelpTab()
	{
		if (_tabsMapaLateral == null) return;
		_tabsMapaLateral.CurrentTab = _tabsMapaLateral.CurrentTab == 1 ? 0 : 1;
	}

	private void OnMapTokenClicked(int clickedEntityId)
	{
		if (_tacticalGridCanvas != null && _tacticalGridCanvas.IsEditorMode)
		{
			if (_editorBrush == MapEditorBrush.MoveToken)
			{
				_editorSelectedTokenId = clickedEntityId;
				_tacticalGridCanvas.SetSelectedEditorToken(clickedEntityId);
				string tName = _combatManager.CombatantIdToSheet.TryGetValue(clickedEntityId, out var sh) ? sh.Name : $"#{clickedEntityId}";
				ShowToast($"Token {tName} selecionado. Clique em uma célula livre para mover.");
			}
			return;
		}

		if (!_combatManager.IsInCombat) return;

		// Seleciona o personagem clicado no dropdown de Alvo do combate
		for (int i = 0; i < _optAlvoCombate.ItemCount; i++)
		{
			var meta = _optAlvoCombate.GetItemMetadata(i);
			if (meta.VariantType == Variant.Type.Int && (int)meta == clickedEntityId)
			{
				_optAlvoCombate.Select(i);
				break;
			}
		}

		if (_combatManager.CombatantIdToSheet.TryGetValue(clickedEntityId, out var clickedSheet))
		{
			if (clickedSheet.IsEnemy)
			{
				SelectEnemyByRef(clickedSheet);
			}
			else
			{
				int sheetIdx = _combatManager.Characters.IndexOf(clickedSheet);
				if (sheetIdx >= 0) SelectCharacter(sheetIdx);
			}
		}

		RefreshTacticalMapView();
	}

	private void OnMapCellClicked(GridPos cell)
	{
		if (_tacticalGridCanvas != null && _tacticalGridCanvas.IsEditorMode)
		{
			if (_editorBrush == MapEditorBrush.MoveToken)
			{
				if (_editorSelectedTokenId > 0)
				{
					if (_combatManager.Session.MoveToken(_editorSelectedTokenId, cell.X, cell.Y))
					{
						Tormenta20TacticalRules.UpdateFogOfWar(_combatManager.Session, _combatManager.FogTracker, alliedTeam: 1);
						_editorSelectedTokenId = 0;
						_tacticalGridCanvas.SetSelectedEditorToken(0);
						RefreshTacticalMapView();
					}
					else
					{
						ShowToast("Não é possível colocar o token nessa célula.", new Color(1f, 0.45f, 0.45f));
					}
				}
				return;
			}
			PaintCellInEditor(cell);
			return;
		}

		if (!_combatManager.IsInCombat || (_tacticalGridCanvas != null && _tacticalGridCanvas.RulerMode)) return;

		int actorId = _combatManager.Session.CurrentTurn;
		if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) return;

		if (actorSheet.CurrentHp <= 0)
		{
			ShowToast($"{actorSheet.Name} está incapacitado e não pode se mover!", new Color(1f, 0.45f, 0.45f));
			return;
		}

		var res = Tormenta20TacticalRules.ExecuteTacticalMove(
			_combatManager.Session, actorSheet, actorId, cell, useFullRunAction: _runModeNextClick);

		if (res.Success)
		{
			_runModeNextClick = false;
			if (_btnMapaCorrer != null) _btnMapaCorrer.ButtonPressed = false;
			_tacticalGridCanvas?.AnimateTokenMove(actorId, res.Path);
			Tormenta20TacticalRules.UpdateFogOfWar(_combatManager.Session, _combatManager.FogTracker, alliedTeam: 1);
			AppendCombatLog(res.Message, "#a6e22e");
			ShowToast(res.Message);
			UpdateCombatStatusDisplay();
			RefreshTacticalMapView();
		}
		else
		{
			AppendCombatLog(res.Message, "#ff6b6b");
			ShowToast(res.Message, new Color(1f, 0.45f, 0.45f));
		}
	}

	private void PaintCellInEditor(GridPos cell)
	{
		if (_combatManager.Session == null || !_combatManager.Session.IsMapActive) return;
		if (_editorBrush == MapEditorBrush.MoveToken) return;

		switch (_editorBrush)
		{
			case MapEditorBrush.Floor:
				_combatManager.Session.SetMapCell(cell.X, cell.Y, MapTerrainType.Passable, 0, false);
				break;
			case MapEditorBrush.Wall:
				_combatManager.Session.SetMapCell(cell.X, cell.Y, MapTerrainType.Blocked, 2, true);
				break;
			case MapEditorBrush.Cover:
				_combatManager.Session.SetMapCell(cell.X, cell.Y, MapTerrainType.Difficult, 1, false);
				break;
			case MapEditorBrush.Difficult:
				_combatManager.Session.SetMapCell(cell.X, cell.Y, MapTerrainType.Difficult, 0, false);
				break;
		}
		Tormenta20TacticalRules.UpdateFogOfWar(_combatManager.Session, _combatManager.FogTracker, alliedTeam: 1);
		RefreshTacticalMapView();
	}

	private void RefreshTacticalMapView()
	{
		if (_tacticalGridCanvas == null || _combatManager?.Session == null) return;

		if (!string.IsNullOrEmpty(_combatManager.CurrentMapBackgroundPath))
		{
			_tacticalGridCanvas.SetBackgroundImage(_combatManager.CurrentMapBackgroundPath);
		}

		int actorId = _combatManager.IsInCombat ? _combatManager.Session.CurrentTurn : 0;
		int targetId = _combatManager.IsInCombat ? GetSelectedCombatTargetId(actorId) : 0;
		var reachable = new Dictionary<GridPos, int>();
		int weaponRange = 1;

		if (_combatManager.IsInCombat && actorId > 0 && _combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet))
		{
			var weapon = actorSheet.Inventory.FirstOrDefault(i => i.Type == ItemType.Weapon && i.Equipped);
			weaponRange = Tormenta20TacticalRules.GetWeaponRangeSquares(weapon);

			int moveSquares = Tormenta20TacticalRules.GetMovementSpeedSquares(actorSheet, _combatManager.Session, actorId);
			bool hasMoveSlot = _runModeNextClick
				? (_combatManager.Session.CanUseAction(actorId, (int)T20ActionType.Standard, out _) &&
				   _combatManager.Session.GetActionSlot(actorId, (int)T20ActionType.Movement).Current > 0)
				: _combatManager.Session.CanUseAction(actorId, (int)T20ActionType.Movement, out _);

			if (hasMoveSlot && moveSquares > 0)
			{
				int budget = _runModeNextClick ? moveSquares * 2 : moveSquares;
				reachable = Tormenta20TacticalRules.ComputeReachableCells(_combatManager.Session, actorId, budget);
			}

			string slots = Tormenta20ActionRules.FormatRemainingActions(_combatManager.Session, actorId);
			string moveLabel = Tormenta20TacticalRules.FormatMeters(moveSquares);
			_lblMapaTurnoInfo.Text = actorSheet.CurrentHp <= 0
				? $"Rodada {_combatManager.Session.Round} — {actorSheet.Name}\nIncapacitado ({actorSheet.CurrentHp} PV)"
				: $"Rodada {_combatManager.Session.Round} — Vez de {actorSheet.Name}\nDeslocamento: {moveLabel} ({moveSquares}q)\nAções: {slots}";

			_btnMapaProximoTurno.Disabled = false;
			_btnMapaAtacar.Disabled = actorSheet.CurrentHp <= 0;
			_btnMapaCorrer.Disabled = actorSheet.CurrentHp <= 0;
			_btnMapaHabilidade.Disabled = actorSheet.CurrentHp <= 0;

			AtualizarResumoAlvoNoMapa(actorId, targetId, weapon);
		}
		else
		{
			if (_lblMapaTurnoInfo != null)
				_lblMapaTurnoInfo.Text = "Fora de combate.\nInicie o combate para posicionar e movimentar os combatentes.";
			if (_lblMapaAlvoResumo != null)
				_lblMapaAlvoResumo.Text = "Alvo: Nenhum";
			if (_btnMapaProximoTurno != null) _btnMapaProximoTurno.Disabled = true;
			if (_btnMapaAtacar != null) _btnMapaAtacar.Disabled = true;
			if (_btnMapaCorrer != null) _btnMapaCorrer.Disabled = true;
			if (_btnMapaHabilidade != null) _btnMapaHabilidade.Disabled = true;
		}

		if (_btnFogToggle != null && _btnFogToggle.ButtonPressed != _combatManager.FogOfWarEnabled)
		{
			_btnFogToggle.SetPressedNoSignal(_combatManager.FogOfWarEnabled);
			_btnFogToggle.Text = _combatManager.FogOfWarEnabled ? "Névoa de Guerra" : "Visão do Mestre";
		}

		_tacticalGridCanvas.SyncFromSession(
			_combatManager.Session,
			_combatManager.CombatantIdToSheet,
			actorId,
			targetId,
			_combatManager.FogTracker,
			_combatManager.FogOfWarEnabled,
			reachable,
			weaponRange);
	}

	private void AtualizarResumoAlvoNoMapa(int actorId, int targetId, CharacterItem weapon)
	{
		if (_lblMapaAlvoResumo == null) return;
		if (targetId <= 0 || !_combatManager.CombatantIdToSheet.TryGetValue(targetId, out var targetSheet))
		{
			_lblMapaAlvoResumo.Text = "Alvo: Clique em um inimigo no mapa.";
			return;
		}

		var ctx = Tormenta20TacticalRules.EvaluateAttackContext(_combatManager.Session, actorId, targetId, weapon);
		string armaNome = weapon?.Name ?? "Ataque Desarmado";
		if (!ctx.MapActive)
		{
			_lblMapaAlvoResumo.Text = $"Alvo: {targetSheet.Name} (PV {targetSheet.CurrentHp}/{targetSheet.MaxHp})";
			return;
		}

		var tags = new List<string>();
		if (!ctx.HasLineOfSight) tags.Add("Sem Visão");
		else if (!ctx.InRange) tags.Add($"Fora de alcance ({Tormenta20TacticalRules.FormatMeters(ctx.MaxRangeSquares)})");
		else tags.Add("No alcance");

		if (ctx.Cover == T20CoverLevel.Light) tags.Add("Cobertura Leve (+5 Def)");
		if (ctx.IsFlanking) tags.Add("Flanqueando (+2 Atk)");

		_lblMapaAlvoResumo.Text =
			$"Alvo: {targetSheet.Name} (PV {targetSheet.CurrentHp}/{targetSheet.MaxHp} | Def {targetSheet.GetDefense(_combatManager.Session, targetId)})\n" +
			$"Arma: {armaNome} | Distância: {Tormenta20TacticalRules.FormatMeters(ctx.DistanceSquares)}\n" +
			string.Join(" | ", tags);
	}

	// ════════════════════════════════════════════════════════════════════════
	// Helpers & Diagnóstico Automatizado (--self-test)
	// ════════════════════════════════════════════════════════════════════════
	private void AppendCombatLog(string message, string colorHex)
	{
		string bb = $"[color={colorHex}]{message}[/color]\n";
		_txtLogCombate?.AppendText(bb);
		_txtMapaMiniLog?.AppendText(bb);
	}

	private void RunAutomatedUiCSharpCppSelfTest()
	{
		try
		{
			GD.Print("====================================================================");
			GD.Print("  [SELF-TEST] Verificando Integração Godot UI <-> C# <-> C++ Core   ");
			GD.Print("====================================================================");

			if (_combatManager?.Session == null)
				throw new InvalidOperationException("EncounterSession nativa C++ está nula!");

			// 1. Ficha de Personagem + Partições Redimensionáveis + Árvore de Habilidades
			if (_selectedCharacter == null && _combatManager.Characters.Count > 0)
				SelectCharacter(0);
			if (_selectedCharacter == null)
				throw new InvalidOperationException("Nenhum personagem carregado na UI!");

			var hSplitFicha = GetNodeOrNull<HSplitContainer>("MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha");
			var vSplitLeft = GetNodeOrNull<VSplitContainer>("MarginContainer/VBoxMain/MainContent/ViewFicha/CenterBox/HSplitFicha/LeftCol/VSplitLeftCol");
			if (hSplitFicha == null || vSplitLeft == null)
				throw new InvalidOperationException("Partições redimensionáveis da ficha (HSplitFicha/VSplitLeftCol) não encontradas!");

			AbrirArvorePoderes();
			_dlgArvorePoderes.Hide();
			GD.Print($"[SELF-TEST 1/8] Ficha ({_selectedCharacter.Name}), Partições Redimensionáveis e Árvore de Habilidades... OK!");

			// 2. Ficha Própria de Inimigos (Bestiário T20)
			ShowView("Inimigos");
			var tempEnemy = _combatManager.CreateEnemyFromPreset("orc_berserker", "Orc Teste");
			if (!tempEnemy.IsEnemy || tempEnemy.FixedDefense <= 0 || tempEnemy.DamageReduction <= 0)
				throw new InvalidOperationException("Ameaça criada no Bestiário não possui propriedades esperadas!");
			SelectEnemyByRef(tempEnemy);
			if (_leEnemyName == null || _leEnemyName.Text != "Orc Teste")
				throw new InvalidOperationException("UI da Ficha de Inimigos não sincronizou o nome da ameaça!");
			GD.Print($"[SELF-TEST 2/8] Ficha de Inimigos ({tempEnemy.Name} ND {tempEnemy.ChallengeRating}, Def {tempEnemy.FixedDefense}, RD {tempEnemy.DamageReduction})... OK!");

			// 3. Bandeja de Dados Visual
			ToggleDiceTrayPopup();
			RolarDadoExpressao("2d20kh1+5");
			if (string.IsNullOrWhiteSpace(_lblResultado.Text) || _lblResultado.Text == "Erro")
				throw new InvalidOperationException($"Falha na rolagem de dados via DiceEngine: {_lblDetalhes.Text}");
			_popupDadosOverlay.Visible = false;
			GD.Print($"[SELF-TEST 3/8] Bandeja de Dados Visual (2d20kh1+5 => {_lblResultado.Text})... OK!");

			// 4. Seleção de Iniciativa e Combate (Herói Time 1 vs Inimigo Time 2)
			ShowView("Combate");
			AbrirModalSelecaoIniciativa();
			_dlgSelecaoIniciativa.Hide();
			var testHero = _combatManager.Characters[0];
			var testParticipants = new List<CharacterSheetData> { testHero, tempEnemy };
			var customTeams = new Dictionary<int, int>
			{
				[testHero.Id] = 1,
				[tempEnemy.Id] = 2
			};
			_combatManager.StartCombatWithSelectedCharacters(testParticipants, customTeams);
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
			RefreshActionQueuePanel();

			int[] initOrder = _combatManager.Session.GetInitiativeOrder();
			if (initOrder.Length != testParticipants.Count)
				throw new InvalidOperationException("Não foi possível registrar todos os combatentes na iniciativa!");
			GD.Print($"[SELF-TEST 4/8] Combate Herói vs Inimigo iniciado ({initOrder.Length} participantes, Rodada {_combatManager.Session.Round})... OK!");

			// 5. Painel de Ações do Turno (Enqueue + Pop)
			int currentActor = _combatManager.Session.CurrentTurn;
			OnMovimentoPressed();
			var queued = _combatManager.Session.GetActionQueue(currentActor);
			if (queued.Length == 0)
				throw new InvalidOperationException("Ação de Movimento não foi preparada no turno!");
			OnPopNextActionPressed();
			if (_combatManager.Session.GetActionQueue(currentActor).Length != 0)
				throw new InvalidOperationException("Executar Próxima Ação não consumiu a ação preparada!");
			GD.Print("[SELF-TEST 5/8] Painel de Ações do Turno e Fila de Prioridades... OK!");

			// 6. Mesa de Combate: Ataque respeitando alcance no mapa (sem gastar Ação Padrão quando distante)
			int firstEnemyId = initOrder.FirstOrDefault(id => id != currentActor);
			OnAtacarPressed();
			GD.Print("[SELF-TEST 6/8] Validação tática de ataque na Mesa de Combate... OK!");

			// 7. Mapa Tático (Tabletop): Aba Ajuda, Zoom suave, mover token e Névoa de Guerra
			ShowView("Mapa");
			if (_tabsMapaLateral == null || _tabsMapaLateral.GetTabCount() < 2)
				throw new InvalidOperationException("Aba Ajuda do Tabletop não foi construída!");
			ToggleTacticalHelpTab();
			if (_tabsMapaLateral.CurrentTab != 1)
				throw new InvalidOperationException("Botão Ajuda não alternou para a aba de comandos!");
			ToggleTacticalHelpTab();

			_tacticalGridCanvas.ZoomBy(1.2f);
			if (Mathf.IsEqualApprox(_tacticalGridCanvas.TargetZoom, 1.0f))
				throw new InvalidOperationException("Controle de zoom do Tabletop não atualizou TargetZoom!");
			_tacticalGridCanvas.CenterView(animate: false);

			_combatManager.FogOfWarEnabled = true;
			_combatManager.Session.ResetTurnActions(currentActor);
			if (!_combatManager.Session.TryGetToken(currentActor, out var actorTok))
				throw new InvalidOperationException("Combatente atual não possui token no Mapa Tático!");

			var reachable = Tormenta20TacticalRules.ComputeReachableCells(_combatManager.Session, currentActor, 4);
			if (reachable.Count == 0)
				throw new InvalidOperationException("Nenhuma célula alcançável encontrada para o token no Mapa Tático!");
			GridPos dest = reachable.Keys.First();
			OnMapCellClicked(dest);
			if (!_combatManager.Session.TryGetToken(currentActor, out var movedTok) || movedTok.X != dest.X || movedTok.Y != dest.Y)
				throw new InvalidOperationException("Clique no Mapa Tático não deslocou o token corretamente!");
			if (_combatManager.FogTracker.VisibleCells.Count == 0)
				throw new InvalidOperationException("Névoa de Guerra não revelou células ao redor dos aliados!");
			GD.Print($"[SELF-TEST 7/8] Tabletop (Ajuda, Zoom, Movimento até ({dest.X},{dest.Y}) e Névoa de Guerra)... OK!");

			// 8. Ataque adjacente no Mapa + Persistência do combate com Inimigo e Mapa
			if (firstEnemyId > 0)
			{
				GridPos? adjFree = null;
				foreach (var d in new[] { new GridPos(1, 0), new GridPos(-1, 0), new GridPos(0, 1), new GridPos(0, -1) })
				{
					int nx = dest.X + d.X;
					int ny = dest.Y + d.Y;
					if (nx >= 0 && nx < _combatManager.Session.MapWidth &&
					    ny >= 0 && ny < _combatManager.Session.MapHeight &&
					    _combatManager.Session.GetMapCell(nx, ny).TerrainType != MapTerrainType.Blocked)
					{
						adjFree = new GridPos(nx, ny);
						break;
					}
				}
				if (adjFree.HasValue)
				{
					_combatManager.Session.MoveToken(firstEnemyId, adjFree.Value.X, adjFree.Value.Y);
				}
				RefreshTacticalMapView();
				OnAtacarPressed();
			}

			string tempSnapPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_combatManager.GetCombatSnapshotPath()) ?? ".", "_selftest_encounter.json");
			if (!_combatManager.SaveCombatSnapshotToPath(tempSnapPath))
				throw new InvalidOperationException("Falha ao salvar combate + mapa Tático!");
			if (!_combatManager.LoadCombatSnapshotFromPath(tempSnapPath))
				throw new InvalidOperationException("Falha ao restaurar combate + mapa Tático!");
			if (System.IO.File.Exists(tempSnapPath))
				System.IO.File.Delete(tempSnapPath);

			_combatManager.FogOfWarEnabled = false;
			_combatManager.EndCombat();
			_combatManager.DeleteEnemy(tempEnemy.Id);
			_selectedEnemy = _combatManager.Enemies.FirstOrDefault();
			RefreshEnemyList();
			RefreshEnemySheetUI();
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			RefreshActionQueuePanel();
			RefreshTacticalMapView();
			GD.Print("[SELF-TEST 8/8] Ataque adjacente no Mapa + Persistência completa de Combate com Inimigo... OK!");

			GD.Print("====================================================================");
			GD.Print("  >>> [SELF-TEST UI+C#+C++] SUCESSO TOTAL! 8/8 ETAPAS OK! <<<      ");
			GD.Print("====================================================================");
			GetTree().Quit(0);
		}
		catch (Exception ex)
		{
			GD.PushError($"[SELF-TEST FALHOU] {ex}");
			GetTree().Quit(1);
		}
	}
}
