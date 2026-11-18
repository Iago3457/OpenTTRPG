using Godot;
using System;

public partial class MainMenu : MarginContainer
{
  [Export] private Button _btnIniciar;
  [Export] private Button _btnOpcoes;
  [Export] private Button _btnSair;
  
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_btnIniciar.Pressed += OnIniciarPressed;
		_btnSair.Pressed += () => GetTree().Quit();
	}
  
  private void OnIniciarPressed(){
	GetTree().ChangeSceneToFile("res://Cenas/MainUi.tscn");
  }
}
