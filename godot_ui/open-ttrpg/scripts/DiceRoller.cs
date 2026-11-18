using Godot;
using System;
using OpenTTRPG.Native;

public partial class DiceRoller : Node
{
	private CombatManager _combatManager;

	[Signal]
	public delegate void DiceRolledEventHandler(string expression, int total, bool isNaturalCrit, bool isNaturalFumble);

	public override void _Ready()
	{
		_combatManager = GetNode<CombatManager>("/root/CombatManager");
	}

	public bool Roll(string expression, out RollResult result, out string error)
	{
		if (_combatManager == null)
		{
			_combatManager = GetNode<CombatManager>("/root/CombatManager");
		}

		bool success = _combatManager.RollTest(expression, out result, out error);
		if (success)
		{
			bool isCrit = result.NaturalMaxCount > 0 && expression.Contains("d20");
			bool isFumble = result.NaturalMinCount > 0 && expression.Contains("d20");
			EmitSignal(SignalName.DiceRolled, expression, result.Total, isCrit, isFumble);
		}
		return success;
	}
}
