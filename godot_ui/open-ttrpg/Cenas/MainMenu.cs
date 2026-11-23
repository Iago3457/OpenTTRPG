using Godot;
using System;
using System.Linq;

public partial class MainMenu : MarginContainer
{
	[Export] private Button _btnIniciar;
	[Export] private Button _btnOpcoes;
	[Export] private Button _btnSair;

	private Window _dlgOpcoes;

	public override void _Ready()
	{
		_btnIniciar.Pressed += OnIniciarPressed;
		_btnSair.Pressed += () => GetTree().Quit();

		BuildOpcoesDialog();
		if (_btnOpcoes != null)
		{
			_btnOpcoes.Pressed += () => _dlgOpcoes.PopupCentered();
		}

		if (OS.GetCmdlineUserArgs().Contains("--self-test") || OS.GetCmdlineArgs().Contains("--self-test"))
		{
			GD.Print("[SELF-TEST] MainMenu carregado -> transitando automaticamente para MainUi.tscn...");
			CallDeferred(nameof(OnIniciarPressed));
		}
	}

	private void OnIniciarPressed()
	{
		GetTree().ChangeSceneToFile("res://Cenas/MainUi.tscn");
	}

	private void BuildOpcoesDialog()
	{
		_dlgOpcoes = new Window
		{
			Title = "Opções — OpenTTRPG (Tormenta20 JdA)",
			InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
			Size = new Vector2I(420, 260),
			Exclusive = true,
			Visible = false
		};

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 18);
		margin.AddThemeConstantOverride("margin_right", 18);
		margin.AddThemeConstantOverride("margin_top", 16);
		margin.AddThemeConstantOverride("margin_bottom", 16);
		_dlgOpcoes.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		margin.AddChild(vbox);

		var lblTitle = new Label
		{
			Text = "Configurações de Vídeo e Sessão",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		lblTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.8f, 0.35f));
		lblTitle.AddThemeFontSizeOverride("font_size", 15);
		vbox.AddChild(lblTitle);

		var chkFullscreen = new CheckBox
		{
			Text = "Tela Cheia (Fullscreen)",
			ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
		};
		chkFullscreen.Toggled += (bool pressed) =>
		{
			DisplayServer.WindowSetMode(pressed
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);
		};
		vbox.AddChild(chkFullscreen);

		var btnRecarregar = new Button
		{
			Text = "🔄 Recarregar Fichas Salvas (characters.json)"
		};
		btnRecarregar.Pressed += () =>
		{
			var cm = GetNodeOrNull<CombatManager>("/root/CombatManager");
			cm?.LoadCharacters();
		};
		vbox.AddChild(btnRecarregar);

		var lblInfo = new Label
		{
			Text = "Motor Core C++17 (AED1 UFSCar) + Regras Tormenta20 JdA em C#",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		lblInfo.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		lblInfo.AddThemeFontSizeOverride("font_size", 11);
		vbox.AddChild(lblInfo);

		var btnFechar = new Button
		{
			Text = "Fechar",
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(110, 32)
		};
		btnFechar.Pressed += () => _dlgOpcoes.Hide();
		_dlgOpcoes.CloseRequested += () => _dlgOpcoes.Hide();
		vbox.AddChild(btnFechar);

		AddChild(_dlgOpcoes);
	}
}
