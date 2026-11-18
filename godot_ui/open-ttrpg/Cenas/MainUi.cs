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
	private Button _btnViewCombate;
	private Button _btnViewDados;
	private Button _btnSalvarFichas;
	private Button _btnVoltarMenu;
	private Button _btnNovoPersonagem;
	private Button _btnExcluirPersonagem;

	// ── Views ───────────────────────────────────────────────────────────────
	private Control _viewFicha;
	private Control _viewCombate;
	private Control _viewDados;

	// ── Ficha: Cabeçalho ────────────────────────────────────────────────────
	private Label _lblCharName;
	private Label _lblCharClassRace;
	private Label _lblCharNivel;
	private Label _lblCharOrigem;
	private Label _lblCharCondicoes;
	private Button _btnDescansar;
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
	private HBoxContainer _boxIniciativa;
	private Label _lblStatusCombate;
	private Label _lblTurnoAtual;
	private RichTextLabel _txtLogCombate;
	private Button _btnAtacar;
	private Button _btnMovimento;
	private Button _btnUsarHabilidade;
	private OptionButton _optAlvoCombate;

	// ── Dados ───────────────────────────────────────────────────────────────
	private LineEdit _txtExpressao;
	private Button _btnRolar;
	private Label _lblResultado;
	private Label _lblDetalhes;
	private RichTextLabel _txtHistorico;

	// ── Pop-up Perícia & Toast ──────────────────────────────────────────────
	private ColorRect _popupPericia;
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
	private Window _dlgArvorePoderes;
	private VBoxContainer _boxArvoreList;
	private Window _dlgUsarHabilidadeCombate;
	private ItemList _listHabilidadesCombate;

	private CharacterSheetData _selectedCharacter;
	private string _currentSubTab = "Ataques";

	// ════════════════════════════════════════════════════════════════════════
	// _Ready
	// ════════════════════════════════════════════════════════════════════════
	public override void _Ready()
	{
		_combatManager = GetNode<CombatManager>("/root/CombatManager");

		BindUiElements();
		SetupAttributeEditButtons();
		SetupRestMenu();
		BuildDialogs();
		ConnectEvents();

		_combatManager.CharactersLoaded += () =>
		{
			UpdateCharacterDropdown();
			if (_combatManager.Characters.Count > 0)
				SelectCharacter(0);
		};

		if (_combatManager.Characters.Count > 0)
		{
			UpdateCharacterDropdown();
			SelectCharacter(0);
		}

		ShowView("Ficha");
		UpdateCombatStatusDisplay();
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
		_viewFicha   = GetNode<Control>($"{mc}/ViewFicha");
		_viewCombate = GetNode<Control>($"{mc}/ViewCombate");
		_viewDados   = GetNode<Control>($"{mc}/ViewDados");

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

		// Dados
		string dv = $"{mc}/ViewDados/VBox";
		_txtExpressao = GetNode<LineEdit>($"{dv}/InputBox/TxtExpressao");
		_btnRolar     = GetNode<Button>($"{dv}/InputBox/BtnRolar");
		_lblResultado = GetNode<Label>($"{dv}/DisplayBox/LblResultado");
		_lblDetalhes  = GetNode<Label>($"{dv}/DisplayBox/LblDetalhes");
		_txtHistorico = GetNode<RichTextLabel>($"{dv}/TxtHistorico");

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

	private void AjustarAtributo(AttributeType type, int delta)
	{
		if (_selectedCharacter == null) return;
		var attr = _selectedCharacter.GetAttribute(type);
		int novo = Math.Clamp(attr.BaseValue + delta, -5, 15);
		_selectedCharacter.SetAttributeValue(type, novo);
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
			var result = Tormenta20RestRules.ApplyRest(_selectedCharacter, quality);
			RefreshSheetUI();
			ShowToast($"🏕️ {result.Message}", new Color(0.4f, 0.85f, 1f));
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

		_dlgArvorePoderes = BuildArvorePoderesDialog();
		AddChild(_dlgArvorePoderes);

		_dlgUsarHabilidadeCombate = BuildUsarHabilidadeCombateDialog();
		AddChild(_dlgUsarHabilidadeCombate);
	}

	private Window BuildNovoPersonagemDialog()
	{
		var win = new Window
		{
			Title = "Novo Personagem (Tormenta20)",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(430, 620),
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

		LineEdit AddRow(VBoxContainer parent, string label, string placeholder = "")
		{
			var lbl = new Label { Text = label };
			lbl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			lbl.AddThemeFontSizeOverride("font_size", 12);
			parent.AddChild(lbl);
			var le = new LineEdit { PlaceholderText = placeholder };
			parent.AddChild(le);
			return le;
		}

		var leName      = AddRow(vbox, "Nome *", "Ex: Thorin Ironfoot");
		var leClasse    = AddRow(vbox, "Classe *", "Ex: Guerreiro");
		var leRaca      = AddRow(vbox, "Raça", "Ex: Anão");
		var leOrigem    = AddRow(vbox, "Origem", "Ex: Nobre");
		var leDivindade = AddRow(vbox, "Divindade", "Ex: Valkaria");

		SpinBox AddSpin(Container parent, string label, int min, int max, int def, int width = 76)
		{
			var vb = new VBoxContainer();
			parent.AddChild(vb);
			var l = new Label { Text = label };
			l.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
			l.AddThemeFontSizeOverride("font_size", 11);
			vb.AddChild(l);
			var sp = new SpinBox { MinValue = min, MaxValue = max, Value = def, CustomMinimumSize = new Vector2(width, 0) };
			vb.AddChild(sp);
			return sp;
		}

		var hboxStats = new HBoxContainer();
		hboxStats.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hboxStats);
		var spNivel = AddSpin(hboxStats, "Nível", 1, 20, 1, 90);
		var spPv    = AddSpin(hboxStats, "PV Máx", 1, 999, 20, 100);
		var spPm    = AddSpin(hboxStats, "PM Máx", 0, 999, 3, 100);

		var lblAttrTitle = new Label { Text = "Atributos Iniciais (Modificadores T20):" };
		lblAttrTitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.4f));
		lblAttrTitle.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(lblAttrTitle);

		var gridAttrs = new GridContainer { Columns = 3 };
		gridAttrs.AddThemeConstantOverride("h_separation", 10);
		gridAttrs.AddThemeConstantOverride("v_separation", 6);
		vbox.AddChild(gridAttrs);

		var spFor = AddSpin(gridAttrs, "FOR", -5, 15, 0, 115);
		var spDes = AddSpin(gridAttrs, "DES", -5, 15, 0, 115);
		var spCon = AddSpin(gridAttrs, "CON", -5, 15, 0, 115);
		var spInt = AddSpin(gridAttrs, "INT", -5, 15, 0, 115);
		var spSab = AddSpin(gridAttrs, "SAB", -5, 15, 0, 115);
		var spCar = AddSpin(gridAttrs, "CAR", -5, 15, 0, 115);

		var hboxBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hboxBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hboxBtns);

		var btnCancelar = new Button { Text = "Cancelar" };
		var btnCriar    = new Button { Text = "✔ Criar Personagem" };
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
				leName.PlaceholderText = "⚠️ Nome é obrigatório!";
				leName.Modulate = new Color(1f, 0.4f, 0.4f);
				return;
			}

			leName.Modulate = Colors.White;
			OnCriarPersonagem(
				nome,
				leClasse.Text.Trim().Length > 0 ? leClasse.Text.Trim() : "Aventureiro",
				leRaca.Text.Trim().Length > 0 ? leRaca.Text.Trim() : "Humano",
				leOrigem.Text.Trim(),
				leDivindade.Text.Trim(),
				(int)spNivel.Value,
				(int)spPv.Value,
				(int)spPm.Value,
				(int)spFor.Value, (int)spDes.Value, (int)spCon.Value,
				(int)spInt.Value, (int)spSab.Value, (int)spCar.Value
			);

			leName.Text = leClasse.Text = leRaca.Text = leOrigem.Text = leDivindade.Text = "";
			spNivel.Value = 1; spPv.Value = 20; spPm.Value = 3;
			spFor.Value = spDes.Value = spCon.Value = spInt.Value = spSab.Value = spCar.Value = 0;
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

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnAdd    = new Button { Text = "✔ Adicionar Item" };
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

			var tipo = (ItemType)optTipo.GetSelectedId();
			string dano = tipo == ItemType.Weapon ? leDano.Text.Trim() : "";
			_selectedCharacter.Inventory.Add(new CharacterItem(
				nome, "", tipo, (int)spSlots.Value, dano, leCrit.Text.Trim(),
				(int)spDef.Value, (int)spPen.Value, (int)spPrice.Value, chkEquip.ButtonPressed));

			leNome.Text = "";
			win.Hide();
			RefreshSheetUI();
			ShowToast($"🎒 Item \"{nome}\" adicionado ao inventário!");
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
			Size = new Vector2I(400, 380),
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

		var leNome = new LineEdit { PlaceholderText = "Ex: Bola de Fogo, Curar Ferimentos..." };
		vbox.AddChild(new Label { Text = "Nome da Magia / Poder *" });
		vbox.AddChild(leNome);

		var leDesc = new LineEdit { PlaceholderText = "Ex: 6d6 de dano de fogo em área" };
		vbox.AddChild(new Label { Text = "Descrição / Efeito" });
		vbox.AddChild(leDesc);

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
		vbPm.AddChild(new Label { Text = "Custo (PM)" });
		vbPm.AddChild(spPm);
		var vbAc = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		vbAc.AddChild(new Label { Text = "Execução" });
		vbAc.AddChild(optAcao);
		hRow.AddChild(vbPm);
		hRow.AddChild(vbAc);

		var chkSpell = new CheckBox { Text = "É uma Magia (Arcana/Divina)", ButtonPressed = true };
		vbox.AddChild(chkSpell);

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnAdd    = new Button { Text = "✔ Adicionar" };
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

			_selectedCharacter.Skills.Add(new CharacterSkill(
				nome, leDesc.Text.Trim(), (int)spPm.Value,
				(T20ActionType)optAcao.GetSelectedId(), 0, "", chkSpell.ButtonPressed));

			leNome.Text = leDesc.Text = "";
			win.Hide();
			RefreshSheetUI();
			ShowToast($"🔮 \"{nome}\" adicionada à ficha!");
		};

		win.Hide();
		return win;
	}

	private Window BuildArvorePoderesDialog()
	{
		var win = new Window
		{
			Title = "Árvore de Poderes — Tormenta20",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(560, 480),
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

		var lblInfo = new Label { Text = "Clique em 'Aprender' nos poderes disponíveis (pré-requisitos validados pelo Core C++):" };
		lblInfo.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(lblInfo);

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill };
		vbox.AddChild(scroll);

		_boxArvoreList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
		_boxArvoreList.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(_boxArvoreList);

		var btnFechar = new Button { Text = "Fechar", SizeFlagsHorizontal = SizeFlags.ShrinkEnd };
		btnFechar.Pressed  += () => win.Hide();
		win.CloseRequested += () => win.Hide();
		vbox.AddChild(btnFechar);

		win.Hide();
		return win;
	}

	private void AbrirArvorePoderes()
	{
		if (_selectedCharacter == null) return;

		foreach (Node child in _boxArvoreList.GetChildren().ToArray())
			child.QueueFree();

		using var session = new EncounterSession();
		Tormenta20AbilityTreeRules.RegisterTormenta20PowerTrees(session);
		int eid = session.AddCombatant(_selectedCharacter.Name, _selectedCharacter.Dexterity, _selectedCharacter.MaxHp, 1);
		Tormenta20AbilityTreeRules.SyncCharacterStatsToCore(
			session, eid, _selectedCharacter.Level,
			_selectedCharacter.Strength, _selectedCharacter.Dexterity, _selectedCharacter.Constitution,
			_selectedCharacter.Intelligence, _selectedCharacter.Wisdom, _selectedCharacter.Charisma,
			_selectedCharacter.UnlockedTreeKeys);

		foreach (var p in Tormenta20AbilityTreeRules.CanonicalPowers)
		{
			bool alreadyHas = session.HasAbility(eid, p.Key);
			bool canLearn   = session.CanLearnAbility(eid, p.Key, out string unmet);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);

			string prefix = string.IsNullOrEmpty(p.ParentKey) ? "" : "↳ ";
			var lbl = new Label
			{
				Text = $"{prefix}{p.Name} — {p.Description}",
				SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			lbl.AddThemeFontSizeOverride("font_size", 12);
			if (alreadyHas) lbl.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.55f));
			else if (!canLearn) lbl.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
			row.AddChild(lbl);

			if (alreadyHas)
			{
				var tag = new Label { Text = "[APRENDIDO]" };
				tag.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.55f));
				tag.AddThemeFontSizeOverride("font_size", 11);
				row.AddChild(tag);
			}
			else if (canLearn)
			{
				var btnLearn = new Button { Text = "Aprender" };
				btnLearn.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
				var powerCopy = p;
				btnLearn.Pressed += () =>
				{
					_selectedCharacter.UnlockedTreeKeys.Add(powerCopy.Key);
					_selectedCharacter.Skills.Add(new CharacterSkill(
						powerCopy.Name, powerCopy.Description, powerCopy.PmCost, powerCopy.ActionType, 0, powerCopy.Key));
					RefreshSheetUI();
					ShowToast($"✨ Poder \"{powerCopy.Name}\" desbloqueado!");
					AbrirArvorePoderes();
				};
				row.AddChild(btnLearn);
			}
			else
			{
				var tagBlock = new Label { Text = $"[Bloqueado: {unmet}]" };
				tagBlock.AddThemeColorOverride("font_color", new Color(0.95f, 0.45f, 0.45f));
				tagBlock.AddThemeFontSizeOverride("font_size", 10);
				row.AddChild(tagBlock);
			}

			_boxArvoreList.AddChild(row);
		}

		_dlgArvorePoderes.PopupCentered();
	}

	private Window BuildUsarHabilidadeCombateDialog()
	{
		var win = new Window
		{
			Title = "Usar Poder / Magia no Turno",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(440, 320),
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

		var hBtns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		hBtns.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(hBtns);
		var btnCancel = new Button { Text = "Cancelar" };
		var btnUsar   = new Button { Text = "✨ Executar Habilidade" };
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
			if (sk.PmCost > 0)
			{
				bool spentCore  = _combatManager.Session.SpendResource(actorId, Tormenta20ActionRules.ManaResourceId, sk.PmCost);
				bool spentSheet = spentCore && actorSheet.SpendPm(sk.PmCost);
				if (!spentSheet)
				{
					AppendCombatLog($"⚠️ {actorSheet.Name} não possui PM suficientes ({sk.PmCost} PM) para \"{sk.Name}\"!", "#ff6b6b");
					win.Hide();
					return;
				}
			}

			var actRes = Tormenta20ActionRules.EnqueueTurnAction(_combatManager.Session, actorId, sk.Action, sk.Name);
			if (!actRes.Success)
			{
				// Estorna PM se a ação não estava disponível no turno
				if (sk.PmCost > 0)
				{
					_combatManager.Session.RestoreResource(actorId, Tormenta20ActionRules.ManaResourceId, sk.PmCost);
					actorSheet.RecoverPm(sk.PmCost);
				}
				AppendCombatLog($"⚠️ Ação indisponível para \"{sk.Name}\": {actRes.Message}", "#ff6b6b");
			}
			else
			{
				AppendCombatLog($"✨ {actorSheet.Name} usou [b]{sk.Name}[/b] (-{sk.PmCost} PM): {sk.Description}", "#66d9ef");
			}

			RefreshSheetUI();
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
		// TopBar
		_btnViewFicha.Pressed         += () => ShowView("Ficha");
		_btnViewCombate.Pressed       += () => ShowView("Combate");
		_btnViewDados.Pressed         += () => ShowView("Dados");
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
		_btnIniciarCombate.Pressed += OnIniciarCombatePressed;
		_btnProximoTurno.Pressed   += () => _combatManager.NextTurn();
		_btnAtacar.Pressed         += OnAtacarPressed;
		_btnMovimento.Pressed      += OnMovimentoPressed;
		_btnUsarHabilidade.Pressed += OnAbrirUsarHabilidadeCombate;

		_combatManager.TurnChanged += (cid, cname, round) =>
		{
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
		};

		_combatManager.CombatLog += (msg, hex) => AppendCombatLog(msg, hex);

		// Dados
		_btnRolar.Pressed           += () => RolarDadoExpressao(_txtExpressao.Text);
		_txtExpressao.TextSubmitted += RolarDadoExpressao;

		SetupQuickDice("BtnD4", "1d4");
		SetupQuickDice("BtnD6", "1d6");
		SetupQuickDice("BtnD8", "1d8");
		SetupQuickDice("BtnD10", "1d10");
		SetupQuickDice("BtnD12", "1d12");
		SetupQuickDice("BtnD20", "1d20");
		SetupQuickDice("BtnD100", "1d100");
	}

	private void SetupQuickDice(string btnName, string expr)
	{
		string path = $"MarginContainer/VBoxMain/MainContent/ViewDados/VBox/QuickDice/{btnName}";
		if (HasNode(path))
			GetNode<Button>(path).Pressed += () => RolarDadoExpressao(expr);
	}

	// ════════════════════════════════════════════════════════════════════════
	// View switching & Toast
	// ════════════════════════════════════════════════════════════════════════
	private void ShowView(string view)
	{
		_viewFicha.Visible   = view == "Ficha";
		_viewCombate.Visible = view == "Combate";
		_viewDados.Visible   = view == "Dados";
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
	// Criar / Excluir Personagens
	// ════════════════════════════════════════════════════════════════════════
	private void OnCriarPersonagem(string nome, string classe, string raca,
		string origem, string divindade, int nivel, int maxPv, int maxPm,
		int str, int dex, int con, int intel, int wis, int cha)
	{
		var novo = _combatManager.CreateCharacter(nome, classe, raca, origem, divindade, nivel, maxPv, maxPm);
		novo.Strength     = str;
		novo.Dexterity    = dex;
		novo.Constitution = con;
		novo.Intelligence = intel;
		novo.Wisdom       = wis;
		novo.Charisma     = cha;
		_combatManager.SaveCharacters();

		UpdateCharacterDropdown();
		int idx = _combatManager.Characters.IndexOf(novo);
		if (idx >= 0)
		{
			_optPersonagens.Select(idx);
			SelectCharacter(idx);
		}

		ShowToast($"✅ Personagem \"{nome}\" criado e salvo!");
		AppendCombatLog($"✅ Personagem \"{nome}\" criado com sucesso!", "#55ff99");
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
		ShowToast($"🗑️ Personagem \"{nome}\" excluído.", new Color(1f, 0.45f, 0.45f));
		AppendCombatLog($"🗑️ Personagem \"{nome}\" excluído.", "#ff9999");

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

		// Cabeçalho
		_lblCharName.Text      = c.Name;
		_lblCharClassRace.Text = $"{c.Race} {c.CharacterClass} ({c.Origin})";
		_lblCharNivel.Text     = $"NÍVEL {c.Level}";
		_lblCharOrigem.Text    = $"ORIGEM: {c.Origin}";
		_lblCharCondicoes.Text = "CONDIÇÕES: Nenhuma condição ativa";

		// PV + cor dinâmica da borda
		_lblPvValor.Text    = c.CurrentHp.ToString();
		_lblPvSubtexto.Text = $"PV {c.CurrentHp}/{c.MaxHp}";
		UpdatePvCircleStyle(c);

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
		int penArmadura = c.GetTotalArmorPenalty();
		_lblResumoNivelTreino.Text = $"½ Nível: +{halfLevel}   |   Treino: +{treinoBonus}   |   Pen. Armadura: {penArmadura}";
	}

	private void UpdatePvCircleStyle(CharacterSheetData c)
	{
		if (_circlePv.GetThemeStylebox("panel") is not StyleBoxFlat sb) return;
		var style = (StyleBoxFlat)sb.Duplicate();

		if (c.CurrentHp <= 0)
		{
			style.BorderColor = new Color(0.9f, 0.22f, 0.22f);
			style.BgColor     = new Color(0.24f, 0.08f, 0.08f);
		}
		else if (c.CurrentHp <= c.MaxHp / 2)
		{
			style.BorderColor = new Color(0.92f, 0.62f, 0.15f);
			style.BgColor     = new Color(0.22f, 0.16f, 0.07f);
		}
		else
		{
			style.BorderColor = new Color(0.18f, 0.69f, 0.35f);
			style.BgColor     = new Color(0.08f, 0.18f, 0.11f);
		}

		_circlePv.AddThemeStyleboxOverride("panel", style);
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

	// ── Sub-tabs e ações contextuais ─────────────────────────────────────────
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
		_btnBannerSubTab.Text = tab.ToUpper();

		// Atualiza indicador de Carga
		bool sobrecarregado = c.IsOverburdened();
		_lblCargaInfo.Text = $"Carga: {c.GetCurrentSlots()}/{c.GetMaxSlots()} esp.{(sobrecarregado ? " [SOBRECARREGADO]" : "")}";
		_lblCargaInfo.AddThemeColorOverride("font_color",
			sobrecarregado ? new Color(1f, 0.4f, 0.4f) : new Color(0.75f, 0.75f, 0.75f));

		switch (tab)
		{
			case "Ataques":
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "➕ Novo Item";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "🗑️ Remover";

				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Weapon))
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"🗡️ {item.Name} — Dano: {item.Damage} ({item.Critical}) {eq}");
					_listSubItems.SetItemMetadata(idx, c.Inventory.IndexOf(item));
				}
				if (!c.Inventory.Any(i => i.Type == ItemType.Weapon))
					_listSubItems.AddItem("Nenhuma arma cadastrada.");
				break;

			case "Defesa":
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "➕ Novo Item";
				_btnSubAcao3.Visible = false;

				_lblDefesaDetalhe.Text    = $"Defesa Total: {c.GetDefense()}   |   Penalidade de Armadura: {c.GetTotalArmorPenalty()}";
				_lblDefesaDetalhe.Visible = true;
				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Armor || i.Type == ItemType.Shield))
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"🛡️ {item.Name} +{item.DefenseBonus} def (Pen: {item.ArmorPenalty}) {eq}");
					_listSubItems.SetItemMetadata(idx, c.Inventory.IndexOf(item));
				}
				break;

			case "Poderes":
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "🌳 Árvore de Poderes";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "➕ Novo Poder";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "🗑️ Remover";

				foreach (var sk in c.Skills.Where(s => !s.IsSpell))
				{
					int idx = _listSubItems.AddItem($"✨ {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
					_listSubItems.SetItemMetadata(idx, c.Skills.IndexOf(sk));
				}
				if (!c.Skills.Any(s => !s.IsSpell))
					_listSubItems.AddItem("Nenhum poder aprendido. Clique em 'Árvore de Poderes'.");
				break;

			case "Magias":
				_btnSubAcao1.Visible = false;
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "➕ Nova Magia";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "🗑️ Remover";

				foreach (var sk in c.Skills.Where(s => s.IsSpell))
				{
					int idx = _listSubItems.AddItem($"🔮 {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
					_listSubItems.SetItemMetadata(idx, c.Skills.IndexOf(sk));
				}
				if (!c.Skills.Any(s => s.IsSpell))
					_listSubItems.AddItem("Nenhuma magia aprendida.");
				break;

			case "Equipamentos":
				_btnSubAcao1.Visible = true;  _btnSubAcao1.Text = "Equipar / Desequipar";
				_btnSubAcao2.Visible = true;  _btnSubAcao2.Text = "➕ Novo Item";
				_btnSubAcao3.Visible = true;  _btnSubAcao3.Text = "🗑️ Remover";

				for (int i = 0; i < c.Inventory.Count; i++)
				{
					var item = c.Inventory[i];
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					int idx = _listSubItems.AddItem($"🎒 {item.Name} {eq} ({item.Slots} esp.) — T$ {item.Price}");
					_listSubItems.SetItemMetadata(idx, i);
				}
				if (c.Inventory.Count == 0)
					_listSubItems.AddItem("Inventário vazio.");
				break;
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
		if (sel.Length == 0) { ShowToast("⚠️ Selecione um item na lista primeiro."); return; }

		var meta = _listSubItems.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int invIdx = (int)meta;
		if (invIdx < 0 || invIdx >= _selectedCharacter.Inventory.Count) return;

		var item = _selectedCharacter.Inventory[invIdx];
		item.Equipped = !item.Equipped;
		RefreshSheetUI();
		ShowToast($"{(item.Equipped ? "✅ Equipado" : "📦 Guardado")}: {item.Name}");
	}

	private void OnSubAcao2Pressed()
	{
		if (_selectedCharacter == null) return;
		if (_currentSubTab == "Poderes" || _currentSubTab == "Magias")
			_dlgNovaMagia.PopupCentered();
		else
			_dlgNovoItem.PopupCentered();
	}

	private void OnSubAcao3Pressed()
	{
		if (_selectedCharacter == null) return;
		var sel = _listSubItems.GetSelectedItems();
		if (sel.Length == 0) { ShowToast("⚠️ Selecione um item para remover."); return; }

		var meta = _listSubItems.GetItemMetadata(sel[0]);
		if (meta.VariantType != Variant.Type.Int) return;
		int idx = (int)meta;

		if (_currentSubTab == "Poderes" || _currentSubTab == "Magias")
		{
			if (idx >= 0 && idx < _selectedCharacter.Skills.Count)
			{
				string removedName = _selectedCharacter.Skills[idx].Name;
				_selectedCharacter.Skills.RemoveAt(idx);
				RefreshSheetUI();
				ShowToast($"🗑️ Removido: {removedName}");
			}
		}
		else
		{
			if (idx >= 0 && idx < _selectedCharacter.Inventory.Count)
			{
				string removedName = _selectedCharacter.Inventory[idx].Name;
				_selectedCharacter.Inventory.RemoveAt(idx);
				RefreshSheetUI();
				ShowToast($"🗑️ Removido: {removedName}");
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

		foreach (T20SkillDefinition def in Tormenta20SkillRules.CanonicalSkills)
		{
			if (!string.IsNullOrEmpty(busca) && !def.Name.ToLower().Contains(busca))
				continue;

			bool canRoll  = Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, def.Name, out int totalMod, out _);
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

		var check = Tormenta20SkillRules.RollSkillCheck(_combatManager.Session, _selectedCharacter, nomePericia, dc: 0);
		if (!check.Allowed)
		{
			ShowToast($"⚠️ {check.Message}", new Color(1f, 0.45f, 0.45f));
			return;
		}

		// Extrai o valor total rolado para exibir em destaque no pop-up
		Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, nomePericia, out int mod, out _);
		string expr = mod >= 0 ? $"1d20+{mod}" : $"1d20{mod}";
		_combatManager.RollTest(expr, out RollResult res, out _);

		bool critico = res.NaturalMaxCount > 0;

		_lblPopupTitulo.Text      = $"TESTE DE {nomePericia.ToUpper()}";
		_lblPopupPersonagem.Text  = $"Personagem: {_selectedCharacter.Name}";
		_lblPopupResultado.Text   = res.Total.ToString();
		_lblPopupFormula.Text     = $"1d20 rolou {res.DiceTotal} + Mod {mod} = {res.Total}";
		_lblPopupDestaque.Visible = critico;
		_lblPopupResultado.AddThemeColorOverride("font_color",
			critico ? new Color(1f, 0.84f, 0f) : new Color(1f, 1f, 1f));

		_popupPericia.Modulate = new Color(1, 1, 1, 0);
		_popupPericia.Visible  = true;
		var tween = CreateTween();
		tween.TweenProperty(_popupPericia, "modulate", new Color(1, 1, 1, 1), 0.22f);

		AppendCombatLog($"🎲 {_selectedCharacter.Name} rolou {nomePericia}: {res.Total}", "#ffdd66");
	}

	// ── PV / PM modificar ────────────────────────────────────────────────────
	private void ModificarRecurso(bool isPm, bool cura)
	{
		if (_selectedCharacter == null) return;
		int delta = (int)(isPm ? _spinPmDelta.Value : _spinPvDelta.Value);

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

		RefreshSheetUI();
	}

	private void OnSalvarFichasPressed()
	{
		_combatManager.SaveCharacters();
		ShowToast($"💾 {_combatManager.Characters.Count} ficha(s) salva(s) com sucesso!");
		AppendCombatLog("💾 Fichas salvas com sucesso!", "#55ff99");
	}

	// ════════════════════════════════════════════════════════════════════════
	// Combate
	// ════════════════════════════════════════════════════════════════════════
	private void OnIniciarCombatePressed()
	{
		_combatManager.StartCombatWithAllCharacters();
		UpdateCombatStatusDisplay();
		RefreshInitiativeBar();
		PopularAlvoCombate();
	}

	private void UpdateCombatStatusDisplay()
	{
		if (_combatManager.IsInCombat)
		{
			int round = _combatManager.Session.Round;
			int cid   = _combatManager.Session.CurrentTurn;
			string nome = _combatManager.CombatantIdToSheet.TryGetValue(cid, out var sheet)
				? sheet.Name : "Desconhecido";
			_lblStatusCombate.Text    = $"⚔️ Rodada {round} — Vez de {nome}";
			_lblTurnoAtual.Text       = $"Combatente atual: {nome}";
			_btnProximoTurno.Disabled = false;
		}
		else
		{
			_lblStatusCombate.Text    = "Fora de combate.";
			_lblTurnoAtual.Text       = "Clique em 'Iniciar' para começar.";
			_btnProximoTurno.Disabled = true;
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
				btn.Text = $"{sh.Name} ({sh.CurrentHp}/{sh.MaxHp})";
			else
				btn.Text = $"ID {cid}";

			btn.Modulate = cid == current ? new Color(1f, 0.85f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);
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
		foreach (int cid in order)
		{
			if (!_combatManager.CombatantIdToSheet.TryGetValue(cid, out var sh)) continue;
			string label = cid == actorId
				? $"[Você] {sh.Name} (PV {sh.CurrentHp}/{sh.MaxHp})"
				: $"{sh.Name} (PV {sh.CurrentHp}/{sh.MaxHp})";

			_optAlvoCombate.AddItem(label);
			int itemIdx = _optAlvoCombate.ItemCount - 1;
			_optAlvoCombate.SetItemMetadata(itemIdx, cid);
			if (cid == prevTargetId) restoreIdx = itemIdx;
		}

		if (restoreIdx >= 0)
			_optAlvoCombate.Select(restoreIdx);
	}

	private void OnAtacarPressed()
	{
		if (!_combatManager.IsInCombat)
		{
			AppendCombatLog("⚠️ Inicie o combate antes de atacar!", "#ff9999");
			return;
		}

		int actorId = _combatManager.Session.CurrentTurn;
		if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) return;

		var weapon = actorSheet.Inventory.FirstOrDefault(i => i.Type == ItemType.Weapon && i.Equipped)
			?? new CharacterItem("Ataque Desarmado", "", ItemType.Weapon, 0, "1d3", "20/x2");

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

		if (!_combatManager.CombatantIdToSheet.TryGetValue(targetId, out var targetSheet))
			targetSheet = actorSheet;

		var resultado = Tormenta20CombatRules.ResolveWeaponAttack(
			_combatManager.Session, actorSheet, actorId, targetSheet, targetId, weapon);

		if (!resultado.ActionSucceeded)
			AppendCombatLog($"⚠️ {resultado.Message}", "#ff9999");
		else if (resultado.Hit)
		{
			string crit = resultado.IsCritical ? " [CRÍTICO!]" : "";
			AppendCombatLog($"⚔️ {actorSheet.Name} atacou {targetSheet.Name}{crit}! Dano: {resultado.CoreDamageResult.FinalAmount}", "#ffaa44");
		}
		else
			AppendCombatLog($"🛡️ {actorSheet.Name} atacou {targetSheet.Name}: ERROU ({resultado.AttackTotal} vs {targetSheet.GetDefense()})", "#99bbff");

		RefreshSheetUI();
		RefreshInitiativeBar();
		PopularAlvoCombate();
	}

	private void OnMovimentoPressed()
	{
		if (!_combatManager.IsInCombat) return;
		int actorId = _combatManager.Session.CurrentTurn;
		var res = Tormenta20ActionRules.EnqueueTurnAction(_combatManager.Session, actorId, T20ActionType.Movement, "Avanço Tático");
		AppendCombatLog(res.Success
			? "🏃 Ação de Movimento enfileirada."
			: $"⚠️ Não foi possível mover: {res.Message}", res.Success ? "#a6e22e" : "#ff6b6b");
	}

	private void OnAbrirUsarHabilidadeCombate()
	{
		if (!_combatManager.IsInCombat)
		{
			AppendCombatLog("⚠️ Inicie o combate antes de usar habilidades!", "#ff9999");
			return;
		}

		int actorId = _combatManager.Session.CurrentTurn;
		if (!_combatManager.CombatantIdToSheet.TryGetValue(actorId, out var actorSheet)) return;

		_listHabilidadesCombate.Clear();
		if (actorSheet.Skills.Count == 0)
		{
			AppendCombatLog($"⚠️ {actorSheet.Name} não possui poderes ou magias na ficha.", "#ff9999");
			return;
		}

		foreach (var sk in actorSheet.Skills)
		{
			string tipo = sk.IsSpell ? "🔮" : "✨";
			_listHabilidadesCombate.AddItem($"{tipo} {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
		}
		_listHabilidadesCombate.Select(0);
		_dlgUsarHabilidadeCombate.PopupCentered();
	}

	// ════════════════════════════════════════════════════════════════════════
	// Dados
	// ════════════════════════════════════════════════════════════════════════
	private void RolarDadoExpressao(string expr)
	{
		if (string.IsNullOrWhiteSpace(expr)) expr = "1d20";

		if (_combatManager.RollTest(expr, out RollResult res, out string erro))
		{
			_lblResultado.Text = res.Total.ToString();
			_lblDetalhes.Text  = $"Fórmula: {expr} | Dados: {res.DiceTotal} + Mod: {res.Modifier}";

			string cor = "#ffffff";
			if (res.NaturalMaxCount > 0 && expr.Contains("d20")) { cor = "#ffd700"; _lblResultado.Modulate = new Color(1f, 0.84f, 0f); }
			else if (res.NaturalMinCount > 0 && expr.Contains("d20")) { cor = "#ff4444"; _lblResultado.Modulate = new Color(1f, 0.27f, 0.27f); }
			else _lblResultado.Modulate = Colors.White;

			_txtHistorico.AppendText($"[color={cor}]🎲 {expr} → [b]{res.Total}[/b] (d:{res.DiceTotal}, mod:{res.Modifier})[/color]\n");
		}
		else
		{
			_lblResultado.Text = "Erro";
			_lblDetalhes.Text  = erro;
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	// Helpers
	// ════════════════════════════════════════════════════════════════════════
	private void AppendCombatLog(string message, string colorHex)
	{
		_txtLogCombate?.AppendText($"[color={colorHex}]{message}[/color]\n");
	}
}