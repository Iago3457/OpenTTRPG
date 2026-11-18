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

	// ── Ficha: PV / PM ──────────────────────────────────────────────────────
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
	private OptionButton _optAlvoCombate;

	// ── Dados ───────────────────────────────────────────────────────────────
	private LineEdit _txtExpressao;
	private Button _btnRolar;
	private Label _lblResultado;
	private Label _lblDetalhes;
	private RichTextLabel _txtHistorico;

	// ── Pop-up Perícia ──────────────────────────────────────────────────────
	private ColorRect _popupPericia;
	private Label _lblPopupTitulo;
	private Label _lblPopupPersonagem;
	private Label _lblPopupResultado;
	private Label _lblPopupDestaque;
	private Label _lblPopupFormula;
	private Button _btnFecharPopup;

	// ── Diálogos de criar / excluir (criados por código) ────────────────────
	private AcceptDialog _dlgConfirmarExcluir;
	private Window _dlgNovoPersonagem;

	private CharacterSheetData _selectedCharacter;
	private string _currentSubTab = "Ataques";

	// ════════════════════════════════════════════════════════════════════════
	// _Ready
	// ════════════════════════════════════════════════════════════════════════
	public override void _Ready()
	{
		_combatManager = GetNode<CombatManager>("/root/CombatManager");

		BindUiElements();
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
		_optPersonagens    = GetNode<OptionButton>($"{root}/TopBar/OptPersonagens");
		_btnViewFicha      = GetNode<Button>($"{root}/TopBar/BtnViewFicha");
		_btnViewCombate    = GetNode<Button>($"{root}/TopBar/BtnViewCombate");
		_btnViewDados      = GetNode<Button>($"{root}/TopBar/BtnViewDados");
		_btnSalvarFichas   = GetNode<Button>($"{root}/TopBar/BtnSalvarFichas");
		_btnVoltarMenu     = GetNode<Button>($"{root}/TopBar/BtnVoltarMenu");
		_btnNovoPersonagem = GetNode<Button>($"{root}/TopBar/BtnNovoPersonagem");
		_btnExcluirPersonagem = GetNode<Button>($"{root}/TopBar/BtnExcluirPersonagem");

		// Views
		string mc = $"{root}/MainContent";
		_viewFicha   = GetNode<Control>($"{mc}/ViewFicha");
		_viewCombate = GetNode<Control>($"{mc}/ViewCombate");
		_viewDados   = GetNode<Control>($"{mc}/ViewDados");

		// Ficha – caminhos via CenterBox
		string lc = $"{mc}/ViewFicha/CenterBox/HSplitFicha/LeftCol";
		string hdr = $"{lc}/CardHeader/Margin/HBoxHeader";

		_lblCharName       = GetNode<Label>($"{hdr}/VBoxInfo/LblCharName");
		_lblCharClassRace  = GetNode<Label>($"{hdr}/VBoxInfo/LblCharClassRace");
		_lblCharNivel      = GetNode<Label>($"{hdr}/VBoxInfo/HBoxSub/LblCharNivel");
		_lblCharOrigem     = GetNode<Label>($"{hdr}/VBoxInfo/HBoxSub/LblCharOrigem");
		_lblCharCondicoes  = GetNode<Label>($"{hdr}/VBoxInfo/LblCharCondicoes");

		string pvPath = $"{hdr}/HBoxCircles/VBoxPv";
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
		_btnAtaques      = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnAtaques");
		_btnDefesa       = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnDefesa");
		_btnPoderes      = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnPoderes");
		_btnMagias       = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnMagias");
		_btnEquip        = GetNode<Button>($"{interativo}/HBoxSubTabs/BtnEquip");
		_btnBannerSubTab = GetNode<Label>($"{interativo}/PanelContent/BannerSubTab/LblBanner");
		_lblDefesaDetalhe = GetNode<Label>($"{interativo}/PanelContent/LblDefesaDetalhe");
		_listSubItems    = GetNode<ItemList>($"{interativo}/PanelContent/ListSubItems");

		// Perícias (coluna direita)
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
		_optAlvoCombate    = GetNode<OptionButton>($"{cv}/HSplitCombate/LeftActions/VBoxAlvo/OptAlvoCombate");

		// Dados
		string dv = $"{mc}/ViewDados/VBox";
		_txtExpressao   = GetNode<LineEdit>($"{dv}/InputBox/TxtExpressao");
		_btnRolar       = GetNode<Button>($"{dv}/InputBox/BtnRolar");
		_lblResultado   = GetNode<Label>($"{dv}/DisplayBox/LblResultado");
		_lblDetalhes    = GetNode<Label>($"{dv}/DisplayBox/LblDetalhes");
		_txtHistorico   = GetNode<RichTextLabel>($"{dv}/TxtHistorico");

		// Pop-up Perícia
		_popupPericia       = GetNode<ColorRect>("PopupPericia");
		_lblPopupTitulo     = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/BannerHeader/LblPopupTitulo");
		_lblPopupPersonagem = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupPersonagem");
		_lblPopupResultado  = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupResultado");
		_lblPopupDestaque   = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupDestaque");
		_lblPopupFormula    = GetNode<Label>("PopupPericia/Center/PanelDialog/Margin/VBox/LblPopupFormula");
		_btnFecharPopup     = GetNode<Button>("PopupPericia/Center/PanelDialog/Margin/VBox/BtnFecharPopup");
	}

	// ════════════════════════════════════════════════════════════════════════
	// Dialogs
	// ════════════════════════════════════════════════════════════════════════
	private void BuildDialogs()
	{
		// ── Confirmar exclusão ───────────────────────────────────────────────
		_dlgConfirmarExcluir = new AcceptDialog();
		_dlgConfirmarExcluir.Title = "Confirmar Exclusão";
		_dlgConfirmarExcluir.DialogText = "Deseja realmente excluir este personagem? Esta ação não pode ser desfeita.";
		_dlgConfirmarExcluir.AddCancelButton("Cancelar");
		AddChild(_dlgConfirmarExcluir);
		_dlgConfirmarExcluir.Confirmed += OnExcluirConfirmado;

		// ── Novo personagem ──────────────────────────────────────────────────
		_dlgNovoPersonagem = BuildNovoPersonagemDialog();
		AddChild(_dlgNovoPersonagem);
	}

	private Window BuildNovoPersonagemDialog()
	{
		var win = new Window();
		win.Title = "Novo Personagem";
		win.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
		win.Size = new Vector2I(400, 540);
		win.Exclusive = true;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		win.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(vbox);

		// Helper
		(LineEdit, Label) AddRow(VBoxContainer parent, string label, string placeholder = "")
		{
			var lbl = new Label { Text = label };
			lbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
			parent.AddChild(lbl);
			var le = new LineEdit { PlaceholderText = placeholder };
			parent.AddChild(le);
			return (le, lbl);
		}

		var (leName, _)    = AddRow(vbox, "Nome *", "Ex: Thorin Ironfoot");
		var (leClasse, _)  = AddRow(vbox, "Classe *", "Ex: Guerreiro");
		var (leRaca, _)    = AddRow(vbox, "Raça", "Ex: Anão");
		var (leOrigem, _)  = AddRow(vbox, "Origem", "Ex: Nobre");
		var (leDivindade, _) = AddRow(vbox, "Divindade", "Ex: Valkaria");

		// Nível / PV / PM
		var hboxStats = new HBoxContainer();
		hboxStats.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(hboxStats);

		SpinBox AddSpin(HBoxContainer parent, string label, int min, int max, int def)
		{
			var vb = new VBoxContainer();
			parent.AddChild(vb);
			var l = new Label { Text = label };
			l.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
			vb.AddChild(l);
			var sp = new SpinBox { MinValue = min, MaxValue = max, Value = def };
			sp.CustomMinimumSize = new Vector2(80, 0);
			vb.AddChild(sp);
			return sp;
		}

		var spNivel = AddSpin(hboxStats, "Nível", 1, 20, 1);
		var spPv    = AddSpin(hboxStats, "PV Máx",  1, 999, 20);
		var spPm    = AddSpin(hboxStats, "PM Máx",  0, 999, 3);

		// Botões OK / Cancelar
		var hboxBtns = new HBoxContainer();
		hboxBtns.AddThemeConstantOverride("separation", 10);
		hboxBtns.Alignment = BoxContainer.AlignmentMode.End;
		vbox.AddChild(hboxBtns);

		var btnCancelar = new Button { Text = "Cancelar" };
		var btnCriar    = new Button { Text = "✔ Criar Personagem" };
		btnCriar.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		hboxBtns.AddChild(btnCancelar);
		hboxBtns.AddChild(btnCriar);

		btnCancelar.Pressed += () => win.Hide();
		btnCriar.Pressed += () =>
		{
			string nome = leName.Text.Trim();
			if (string.IsNullOrEmpty(nome))
			{
				// Destaca campo obrigatório
				leName.PlaceholderText = "⚠️ Nome é obrigatório!";
				leName.Modulate = new Color(1f, 0.4f, 0.4f);
				return;
			}

			leName.Modulate = Colors.White;
			OnCriarPersonagem(
				nome,
				leClasse.Text.Trim().Length > 0 ? leClasse.Text.Trim() : "Aventureiro",
				leRaca.Text.Trim().Length > 0 ? leRaca.Text.Trim() : "Humano",
				leOrigem.Text.Trim().Length > 0 ? leOrigem.Text.Trim() : "",
				leDivindade.Text.Trim().Length > 0 ? leDivindade.Text.Trim() : "",
				(int)spNivel.Value,
				(int)spPv.Value,
				(int)spPm.Value
			);

			// Limpa campos
			leName.Text = leClasse.Text = leRaca.Text = leOrigem.Text = leDivindade.Text = "";
			spNivel.Value = 1; spPv.Value = 20; spPm.Value = 3;
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
		_btnViewFicha.Pressed   += () => ShowView("Ficha");
		_btnViewCombate.Pressed += () => ShowView("Combate");
		_btnViewDados.Pressed   += () => ShowView("Dados");
		_btnSalvarFichas.Pressed += OnSalvarFichasPressed;
		_btnVoltarMenu.Pressed   += () => GetTree().ChangeSceneToFile("res://Cenas/MainMenu.tscn");
		_btnNovoPersonagem.Pressed   += () => _dlgNovoPersonagem.Show();
		_btnExcluirPersonagem.Pressed += () =>
		{
			if (_selectedCharacter == null) return;
			_dlgConfirmarExcluir.DialogText =
				$"Deseja realmente excluir \"{_selectedCharacter.Name}\"?\nEsta ação não pode ser desfeita.";
			_dlgConfirmarExcluir.PopupCentered();
		};

		// Dropdown de personagens
		_optPersonagens.ItemSelected += (idx) => SelectCharacter((int)idx);

		// PV / PM
		_btnPvDano.Pressed  += () => ModificarRecurso(false, false);
		_btnPvCurar.Pressed += () => ModificarRecurso(false, true);
		_btnPmDano.Pressed  += () => ModificarRecurso(true, false);
		_btnPmCurar.Pressed += () => ModificarRecurso(true, true);

		// Sub-tabs da ficha
		_btnAtaques.Pressed += () => SelecionarSubTab("Ataques");
		_btnDefesa.Pressed  += () => SelecionarSubTab("Defesa");
		_btnPoderes.Pressed += () => SelecionarSubTab("Poderes");
		_btnMagias.Pressed  += () => SelecionarSubTab("Magias");
		_btnEquip.Pressed   += () => SelecionarSubTab("Equipamentos");

		// Perícias
		_txtBuscaPericia.TextChanged += (_) => RefreshPericiasUI();
		ConnectPericiaRows();

		// Pop-up perícia
		_btnFecharPopup.Pressed += () => _popupPericia.Visible = false;
		_popupPericia.GuiInput  += (e) => { if (e is InputEventMouseButton mb && mb.Pressed) _popupPericia.Visible = false; };

		// Combate
		_btnIniciarCombate.Pressed += OnIniciarCombatePressed;
		_btnProximoTurno.Pressed   += () => _combatManager.NextTurn();
		_btnAtacar.Pressed         += OnAtacarPressed;
		_btnMovimento.Pressed      += OnMovimentoPressed;

		_combatManager.TurnChanged += (cid, cname, round) =>
		{
			UpdateCombatStatusDisplay();
			RefreshInitiativeBar();
			PopularAlvoCombate();
		};

		_combatManager.CombatLog += (msg, hex) => AppendCombatLog(msg, hex);

		// Dados
		_btnRolar.Pressed       += () => RolarDadoExpressao(_txtExpressao.Text);
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
	// View switching
	// ════════════════════════════════════════════════════════════════════════
	private void ShowView(string view)
	{
		_viewFicha.Visible   = view == "Ficha";
		_viewCombate.Visible = view == "Combate";
		_viewDados.Visible   = view == "Dados";
	}

	// ════════════════════════════════════════════════════════════════════════
	// Criar / Excluir Personagens
	// ════════════════════════════════════════════════════════════════════════
	private void OnCriarPersonagem(string nome, string classe, string raca,
		string origem, string divindade, int nivel, int maxPv, int maxPm)
	{
		var novo = _combatManager.CreateCharacter(nome, classe, raca, origem, divindade, nivel, maxPv, maxPm);
		_combatManager.SaveCharacters();

		UpdateCharacterDropdown();
		int idx = _combatManager.Characters.IndexOf(novo);
		if (idx >= 0)
		{
			_optPersonagens.Select(idx);
			SelectCharacter(idx);
		}

		GD.Print($"[OpenTTRPG] Personagem criado: {nome}");
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

		// PV
		_lblPvValor.Text    = c.CurrentHp.ToString();
		_lblPvSubtexto.Text = $"PV {c.CurrentHp}/{c.MaxHp}";
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

		// Nível / Treino resumo
		int halfLevel = c.Level / 2;
		_lblResumoNivelTreino.Text = $"½ Nível: {halfLevel}    Treino: 2";
	}

	private void LimparSheetUI()
	{
		_lblCharName.Text  = "—";
		_lblCharClassRace.Text = "";
		_lblPvValor.Text   = "0";
		_lblPmValor.Text   = "0";
		foreach (Node child in _boxPericiasList.GetChildren().ToArray())
			child.QueueFree();	}

	private void SetAttrLabel(Label lbl, int mod)
	{
		lbl.Text = mod >= 0 ? $"+{mod}" : mod.ToString();
	}

	// ── Sub-tabs ─────────────────────────────────────────────────────────────
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

		switch (tab)
		{
			case "Ataques":
				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Weapon))
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "";
					_listSubItems.AddItem($"🗡️ {item.Name} — Dano: {item.Damage} ({item.Critical}) {eq}");
				}
				if (!c.Inventory.Any(i => i.Type == ItemType.Weapon))
					_listSubItems.AddItem("Nenhuma arma equipada.");
				break;

			case "Defesa":
				int def = c.GetDefense();
				_lblDefesaDetalhe.Text = $"Defesa Total: {def}";
				_lblDefesaDetalhe.Visible = true;
				foreach (var item in c.Inventory.Where(i => i.Type == ItemType.Armor || i.Type == ItemType.Shield))
					_listSubItems.AddItem($"🛡️ {item.Name} +{item.DefenseBonus} def");
				break;

			case "Poderes":
				foreach (var sk in c.Skills.Where(s => !s.IsSpell))
					_listSubItems.AddItem($"✨ {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
				if (!c.Skills.Any(s => !s.IsSpell))
					_listSubItems.AddItem("Nenhum poder aprendido.");
				break;

			case "Magias":
				foreach (var sk in c.Skills.Where(s => s.IsSpell))
					_listSubItems.AddItem($"🔮 {sk.Name} ({sk.PmCost} PM) — {sk.Description}");
				if (!c.Skills.Any(s => s.IsSpell))
					_listSubItems.AddItem("Nenhuma magia aprendida.");
				break;

			case "Equipamentos":
				foreach (var item in c.Inventory)
				{
					string eq = item.Equipped ? "[EQUIPADO]" : "[Mochila]";
					_listSubItems.AddItem($"🎒 {item.Name} {eq} ({item.Slots} esp.) — T$ {item.Price}");
				}
				if (c.Inventory.Count == 0)
					_listSubItems.AddItem("Inventário vazio.");
				break;
		}
	}

	// ── Perícias ──────────────────────────────────────────────────────────────
	private void ConnectPericiaRows() { /* Conexão dinâmica feita no RefreshPericiasUI */ }

	private void RefreshPericiasUI()
	{
		if (_selectedCharacter == null) return;

		// Remove filhos antigos
		foreach (Node child in _boxPericiasList.GetChildren().ToArray())
			child.QueueFree();

		string busca = _txtBuscaPericia?.Text?.ToLower() ?? "";

		foreach (T20SkillDefinition def in Tormenta20SkillRules.CanonicalSkills)
		{
			if (!string.IsNullOrEmpty(busca) && !def.Name.ToLower().Contains(busca))
				continue;

			Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, def.Name, out int totalMod, out _);
			bool treinada = _selectedCharacter.TrainedSkills.Contains(def.Name);
			string modStr = totalMod >= 0 ? $"+{totalMod}" : totalMod.ToString();

			// Row HBox
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 6);

			// Total
			var lblTotal = new Label { Text = modStr };
			lblTotal.CustomMinimumSize = new Vector2(40, 0);
			lblTotal.HorizontalAlignment = HorizontalAlignment.Center;
			lblTotal.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
			lblTotal.AddThemeFontSizeOverride("font_size", 12);
			row.AddChild(lblTotal);

			// Nome (botão clicável)
			var btnNome = new Button();
			btnNome.Text = def.Name;
			btnNome.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
			btnNome.Flat = true;
			btnNome.Alignment = HorizontalAlignment.Left;
			btnNome.AddThemeFontSizeOverride("font_size", 12);
			string nomeCopy = def.Name;
			btnNome.Pressed += () => OnRolarPericia(nomeCopy);
			row.AddChild(btnNome);

			// Treino
			var lblTreino = new Label { Text = treinada ? "✓" : "" };
			lblTreino.CustomMinimumSize = new Vector2(30, 0);
			lblTreino.HorizontalAlignment = HorizontalAlignment.Center;
			lblTreino.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.4f));
			row.AddChild(lblTreino);

			// Outros
			int misc = _selectedCharacter.SkillMiscBonuses.TryGetValue(def.Name, out int m) ? m : 0;
			var lblOutros = new Label { Text = misc != 0 ? misc.ToString() : "0" };
			lblOutros.CustomMinimumSize = new Vector2(30, 0);
			lblOutros.HorizontalAlignment = HorizontalAlignment.Center;
			lblOutros.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			row.AddChild(lblOutros);

			_boxPericiasList.AddChild(row);
		}
	}

	private void OnRolarPericia(string nomePericia)
	{
		if (_selectedCharacter == null) return;

		Tormenta20SkillRules.TryCalculateSkillModifier(_selectedCharacter, nomePericia, out int mod, out _);
		string expr = mod >= 0 ? $"1d20+{mod}" : $"1d20{mod}";

		if (!_combatManager.RollTest(expr, out RollResult res, out string erro)) return;

		bool critico = res.NaturalMaxCount > 0;

		_lblPopupTitulo.Text     = $"TESTE DE {nomePericia.ToUpper()}";
		_lblPopupPersonagem.Text = $"Personagem: {_selectedCharacter.Name}";
		_lblPopupResultado.Text  = res.Total.ToString();
		_lblPopupFormula.Text    = $"1d20 rolou {res.DiceTotal} + Mod {mod} = {res.Total}";
		_lblPopupDestaque.Visible = critico;
		if (critico)
			_lblPopupResultado.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0f));
		else
			_lblPopupResultado.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));

		// Fade-in
		_popupPericia.Modulate = new Color(1, 1, 1, 0);
		_popupPericia.Visible  = true;
		var tween = CreateTween();
		tween.TweenProperty(_popupPericia, "modulate", new Color(1, 1, 1, 1), 0.25f);

		AppendCombatLog($"🎲 {_selectedCharacter.Name} rolou {nomePericia}: {res.Total}", "#ffdd66");
	}

	// ── PV / PM modificar ────────────────────────────────────────────────────
	private void ModificarRecurso(bool isPm, bool cura)
	{
		if (_selectedCharacter == null) return;
		int delta = (int)(isPm ? _spinPmDelta.Value : _spinPvDelta.Value);

		if (!isPm)
		{
			if (cura) _selectedCharacter.CurrentHp = Math.Min(_selectedCharacter.MaxHp, _selectedCharacter.CurrentHp + delta);
			else      _selectedCharacter.CurrentHp = Math.Max(0, _selectedCharacter.CurrentHp - delta);
		}
		else
		{
			if (cura) _selectedCharacter.CurrentPm = Math.Min(_selectedCharacter.MaxPm, _selectedCharacter.CurrentPm + delta);
			else      _selectedCharacter.CurrentPm = Math.Max(0, _selectedCharacter.CurrentPm - delta);
		}

		RefreshSheetUI();
	}

	private void OnSalvarFichasPressed()
	{
		_combatManager.SaveCharacters();
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
			_lblStatusCombate.Text = $"⚔️ Rodada {round} — Vez de {nome}";
			_lblTurnoAtual.Text    = $"Combatente atual: {nome}";
			_btnProximoTurno.Disabled = false;
		}
		else
		{
			_lblStatusCombate.Text = "Fora de combate.";
			_lblTurnoAtual.Text    = "Clique em 'Iniciar' para começar.";
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
			btn.Text = _combatManager.CombatantIdToSheet.TryGetValue(cid, out var sh) ? sh.Name : $"ID {cid}";
			btn.Modulate = cid == current ? new Color(1f, 0.85f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);
			_boxIniciativa.AddChild(btn);
		}
	}

	private void PopularAlvoCombate()
	{
		_optAlvoCombate.Clear();
		if (!_combatManager.IsInCombat) return;

		int actorId = _combatManager.Session.CurrentTurn;
		int[] order = _combatManager.Session.GetInitiativeOrder();
		int actorId2 = _combatManager.Session.CurrentTurn;

		foreach (int cid in order)
		{
			if (!_combatManager.CombatantIdToSheet.TryGetValue(cid, out var sh)) continue;
			// Usa o HP em memória do sheet (mantido atualizado pelos ataques)
			int hp = sh.CurrentHp;
			string label = cid == actorId2
				? $"[Você] {sh.Name} (PV {hp})"
				: $"{sh.Name} (PV {hp})";
			_optAlvoCombate.AddItem(label);
			// Store cid as metadata via id trick: items are indexed 0..N
		}
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

		// Determina alvo via OptionButton
		int[] order = _combatManager.Session.GetInitiativeOrder();
		int targetId;
		int selectedIdx = _optAlvoCombate.Selected;
		if (selectedIdx >= 0 && selectedIdx < order.Length)
			targetId = order[selectedIdx];
		else
			targetId = order.FirstOrDefault(id => id != actorId);

		if (targetId == 0) targetId = actorId;
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

	// ════════════════════════════════════════════════════════════════════════
	// Dados
	// ════════════════════════════════════════════════════════════════════════
	private void RolarDadoExpressao(string expr)
	{
		if (string.IsNullOrWhiteSpace(expr)) expr = "1d20";

		if (_combatManager.RollTest(expr, out RollResult res, out string erro))
		{
			_lblResultado.Text  = res.Total.ToString();
			_lblDetalhes.Text   = $"Fórmula: {expr} | Dados: {res.DiceTotal} + Mod: {res.Modifier}";

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
