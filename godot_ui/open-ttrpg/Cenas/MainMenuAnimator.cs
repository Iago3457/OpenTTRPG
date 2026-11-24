using Godot;
using System;

/// <summary>
/// Orquestra as animações via Tween do menu principal: entrada coreografada,
/// loops ociosos (dado d20 e respiro do fundo), crossfade de fundo e transição de cena.
/// </summary>
public partial class MainMenuAnimator : Node
{
	[ExportGroup("Tempos de Animação")]
	[Export] public float EntranceFadeDuration { get; set; } = 0.50f;
	[Export] public float TitleEntranceDuration { get; set; } = 0.70f;
	[Export] public float ButtonStaggerDelay { get; set; } = 0.10f;
	[Export] public float BackgroundCrossfadeDuration { get; set; } = 0.65f;
	[Export] public float SceneTransitionDuration { get; set; } = 0.65f;
	[Export] public bool EnableBackgroundBreathing { get; set; } = true;
	[Export] public bool EnableD20IdleAnimation { get; set; } = true;

	private Tween _entranceTween;
	private Tween _d20IdleTween;
	private Tween _d20SpinTween;
	private Tween _bgBreathTween;
	private Tween _bgCrossfadeTween;
	private Tween _transitionTween;

	public void PrepareEntranceHidden(
		ColorRect transitionOverlay,
		Label transitionLabel,
		Control titleRow,
		Control ornamentRow,
		Control subtitle,
		Control menuCard,
		Control[] buttons,
		Control footerBar)
	{
		if (transitionOverlay != null)
		{
			transitionOverlay.Visible = true;
			transitionOverlay.Modulate = Colors.White;
			transitionOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
		}
		if (transitionLabel != null)
		{
			transitionLabel.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		SetAlpha(titleRow, 0f);
		SetAlpha(ornamentRow, 0f);
		SetAlpha(subtitle, 0f);
		SetAlpha(menuCard, 0f);
		SetAlpha(footerBar, 0f);

		if (buttons != null)
		{
			foreach (Control btn in buttons)
			{
				SetAlpha(btn, 0f);
			}
		}
	}

	public void PlayEntrance(
		ColorRect transitionOverlay,
		Control titleRow,
		TextureRect d20Icon,
		Control ornamentRow,
		Control subtitle,
		Control menuCard,
		Control[] buttons,
		Control footerBar)
	{
		KillTween(ref _entranceTween);

		CenterPivot(titleRow);
		CenterPivot(d20Icon);
		CenterPivot(ornamentRow);
		CenterPivot(menuCard);
		CenterPivot(footerBar);

		if (titleRow != null) titleRow.Scale = new Vector2(0.90f, 0.90f);
		if (d20Icon != null)
		{
			d20Icon.Scale = new Vector2(0.72f, 0.72f);
			d20Icon.RotationDegrees = -22.0f;
		}
		if (ornamentRow != null) ornamentRow.Scale = new Vector2(0.25f, 1.0f);
		if (menuCard != null) menuCard.Scale = new Vector2(0.94f, 0.94f);

		if (buttons != null)
		{
			foreach (Control btn in buttons)
			{
				if (btn == null) continue;
				CenterPivot(btn);
				btn.Scale = new Vector2(0.90f, 0.90f);
			}
		}

		_entranceTween = CreateTween();
		_entranceTween.SetParallel(true);

		if (transitionOverlay != null)
		{
			_entranceTween
				.TweenProperty(transitionOverlay, "modulate:a", 0.0f, EntranceFadeDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);

			_entranceTween
				.TweenCallback(Callable.From(() =>
				{
					if (IsInstanceValid(transitionOverlay))
					{
						transitionOverlay.Visible = false;
						transitionOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
					}
				}))
				.SetDelay(EntranceFadeDuration);
		}

		if (titleRow != null)
		{
			_entranceTween
				.TweenProperty(titleRow, "modulate:a", 1.0f, TitleEntranceDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
			_entranceTween
				.TweenProperty(titleRow, "scale", Vector2.One, TitleEntranceDuration)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}

		if (d20Icon != null)
		{
			_entranceTween
				.TweenProperty(d20Icon, "rotation_degrees", 0.0f, 0.85f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
			_entranceTween
				.TweenProperty(d20Icon, "scale", Vector2.One, 0.85f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
			_entranceTween
				.TweenCallback(Callable.From(() => StartD20IdleLoop(d20Icon)))
				.SetDelay(0.86f);
		}

		if (ornamentRow != null)
		{
			_entranceTween
				.TweenProperty(ornamentRow, "scale:x", 1.0f, 0.55f)
				.SetDelay(0.18f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
			_entranceTween
				.TweenProperty(ornamentRow, "modulate:a", 1.0f, 0.55f)
				.SetDelay(0.18f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}

		if (subtitle != null)
		{
			_entranceTween
				.TweenProperty(subtitle, "modulate:a", 1.0f, 0.50f)
				.SetDelay(0.24f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}

		if (menuCard != null)
		{
			_entranceTween
				.TweenProperty(menuCard, "scale", Vector2.One, 0.45f)
				.SetDelay(0.22f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
			_entranceTween
				.TweenProperty(menuCard, "modulate:a", 1.0f, 0.45f)
				.SetDelay(0.22f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}

		if (buttons != null)
		{
			for (int i = 0; i < buttons.Length; i++)
			{
				Control btn = buttons[i];
				if (btn == null) continue;
				float delay = 0.28f + i * ButtonStaggerDelay;

				_entranceTween
					.TweenProperty(btn, "modulate:a", 1.0f, 0.38f)
					.SetDelay(delay)
					.SetTrans(Tween.TransitionType.Cubic)
					.SetEase(Tween.EaseType.Out);
				_entranceTween
					.TweenProperty(btn, "scale", Vector2.One, 0.38f)
					.SetDelay(delay)
					.SetTrans(Tween.TransitionType.Back)
					.SetEase(Tween.EaseType.Out);
			}
		}

		if (footerBar != null)
		{
			_entranceTween
				.TweenProperty(footerBar, "modulate:a", 1.0f, 0.45f)
				.SetDelay(0.48f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}
	}

	public void StartD20IdleLoop(TextureRect d20Icon)
	{
		if (!EnableD20IdleAnimation || d20Icon == null || !IsInstanceValid(d20Icon)) return;

		KillTween(ref _d20IdleTween);
		CenterPivot(d20Icon);

		_d20IdleTween = CreateTween();
		_d20IdleTween.SetLoops();

		_d20IdleTween
			.TweenProperty(d20Icon, "rotation_degrees", 4.5f, 1.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_d20IdleTween
			.Parallel()
			.TweenProperty(d20Icon, "scale", new Vector2(1.045f, 1.045f), 1.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		_d20IdleTween
			.TweenProperty(d20Icon, "rotation_degrees", -4.5f, 2.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_d20IdleTween
			.Parallel()
			.TweenProperty(d20Icon, "scale", new Vector2(0.995f, 0.995f), 2.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		_d20IdleTween
			.TweenProperty(d20Icon, "rotation_degrees", 0.0f, 1.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		_d20IdleTween
			.Parallel()
			.TweenProperty(d20Icon, "scale", Vector2.One, 1.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	public void PlayD20RollSpin(TextureRect d20Icon)
	{
		if (d20Icon == null || !IsInstanceValid(d20Icon)) return;

		KillTween(ref _d20IdleTween);
		KillTween(ref _d20SpinTween);
		CenterPivot(d20Icon);

		_d20SpinTween = CreateTween();
		_d20SpinTween.SetParallel(true);
		_d20SpinTween
			.TweenProperty(d20Icon, "rotation_degrees", d20Icon.RotationDegrees + 360.0f, 0.65f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		_d20SpinTween
			.TweenProperty(d20Icon, "scale", new Vector2(1.16f, 1.16f), 0.18f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_d20SpinTween
			.TweenProperty(d20Icon, "scale", Vector2.One, 0.47f)
			.SetDelay(0.18f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);

		_d20SpinTween.Chain().TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(d20Icon))
			{
				d20Icon.RotationDegrees = 0.0f;
				StartD20IdleLoop(d20Icon);
			}
		}));
	}

	public void StartBackgroundBreathing(Control bgStage)
	{
		if (!EnableBackgroundBreathing || bgStage == null || !IsInstanceValid(bgStage)) return;

		KillTween(ref _bgBreathTween);
		CenterPivot(bgStage);
		bgStage.Scale = new Vector2(1.04f, 1.04f);

		_bgBreathTween = CreateTween();
		_bgBreathTween.SetLoops();
		_bgBreathTween
			.TweenProperty(bgStage, "scale", new Vector2(1.09f, 1.09f), 7.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_bgBreathTween
			.TweenProperty(bgStage, "scale", new Vector2(1.04f, 1.04f), 7.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	public void CrossfadeBackground(TextureRect bgCurrent, TextureRect bgNext, Texture2D newTexture, bool animate)
	{
		if (bgCurrent == null || newTexture == null) return;

		if (bgNext != null && bgNext.Visible && bgNext.Texture != null && bgNext.Modulate.A > 0.5f)
		{
			bgCurrent.Texture = bgNext.Texture;
		}
		KillTween(ref _bgCrossfadeTween);

		if (!animate || bgNext == null || bgCurrent.Texture == null)
		{
			bgCurrent.Texture = newTexture;
			if (bgNext != null)
			{
				bgNext.Visible = false;
				bgNext.Modulate = new Color(1f, 1f, 1f, 0f);
			}
			return;
		}

		bgNext.Texture = newTexture;
		bgNext.Modulate = new Color(1f, 1f, 1f, 0f);
		bgNext.Visible = true;

		_bgCrossfadeTween = CreateTween();
		_bgCrossfadeTween
			.TweenProperty(bgNext, "modulate:a", 1.0f, BackgroundCrossfadeDuration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
		_bgCrossfadeTween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(bgCurrent))
			{
				bgCurrent.Texture = newTexture;
			}
			if (IsInstanceValid(bgNext))
			{
				bgNext.Visible = false;
				bgNext.Modulate = new Color(1f, 1f, 1f, 0f);
			}
		}));
	}

	public void PlayTransitionToMainUi(
		ColorRect transitionOverlay,
		Label transitionLabel,
		Control contentRoot,
		TextureRect d20Icon,
		Control bgStage,
		Action onComplete)
	{
		KillTween(ref _entranceTween);
		KillTween(ref _d20IdleTween);
		KillTween(ref _d20SpinTween);
		KillTween(ref _bgBreathTween);
		KillTween(ref _transitionTween);

		CenterPivot(contentRoot);
		CenterPivot(d20Icon);
		CenterPivot(bgStage);

		if (transitionOverlay != null)
		{
			transitionOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
			transitionOverlay.Visible = true;
			transitionOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		if (transitionLabel != null)
		{
			transitionLabel.Text = "Preparando a Mesa...";
			transitionLabel.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		_transitionTween = CreateTween();
		_transitionTween.SetParallel(true);

		if (d20Icon != null)
		{
			_transitionTween
				.TweenProperty(d20Icon, "rotation_degrees", d20Icon.RotationDegrees + 360.0f, SceneTransitionDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
			_transitionTween
				.TweenProperty(d20Icon, "scale", new Vector2(1.22f, 1.22f), SceneTransitionDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
		}

		if (contentRoot != null)
		{
			_transitionTween
				.TweenProperty(contentRoot, "scale", new Vector2(1.04f, 1.04f), 0.42f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
			_transitionTween
				.TweenProperty(contentRoot, "modulate:a", 0.0f, 0.42f)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}

		if (bgStage != null)
		{
			_transitionTween
				.TweenProperty(bgStage, "scale", new Vector2(1.15f, 1.15f), SceneTransitionDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
		}

		if (transitionOverlay != null)
		{
			_transitionTween
				.TweenProperty(transitionOverlay, "modulate:a", 1.0f, SceneTransitionDuration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.InOut);
		}

		if (transitionLabel != null)
		{
			_transitionTween
				.TweenProperty(transitionLabel, "modulate:a", 1.0f, 0.30f)
				.SetDelay(0.22f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}

		_transitionTween.Chain().TweenInterval(0.08f);
		_transitionTween.Chain().TweenCallback(Callable.From(() => onComplete?.Invoke()));
	}

	public void PlayQuitTransition(ColorRect transitionOverlay, Control contentRoot, Action onComplete)
	{
		KillTween(ref _entranceTween);
		KillTween(ref _d20IdleTween);
		KillTween(ref _d20SpinTween);
		KillTween(ref _bgBreathTween);
		KillTween(ref _transitionTween);

		if (transitionOverlay != null)
		{
			transitionOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
			transitionOverlay.Visible = true;
			transitionOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		_transitionTween = CreateTween();
		_transitionTween.SetParallel(true);

		if (contentRoot != null)
		{
			_transitionTween
				.TweenProperty(contentRoot, "modulate:a", 0.0f, 0.35f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.InOut);
		}

		if (transitionOverlay != null)
		{
			_transitionTween
				.TweenProperty(transitionOverlay, "modulate:a", 1.0f, 0.35f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.InOut);
		}

		_transitionTween.Chain().TweenCallback(Callable.From(() => onComplete?.Invoke()));
	}

	public static void CenterPivot(Control control)
	{
		if (control != null && IsInstanceValid(control))
		{
			control.PivotOffset = control.Size * 0.5f;
		}
	}

	private static void SetAlpha(Control control, float alpha)
	{
		if (control != null && IsInstanceValid(control))
		{
			Color c = control.Modulate;
			c.A = alpha;
			control.Modulate = c;
		}
	}

	private static void KillTween(ref Tween tween)
	{
		if (tween != null && IsInstanceValid(tween))
		{
			tween.Kill();
		}
		tween = null;
	}

	public override void _ExitTree()
	{
		KillTween(ref _entranceTween);
		KillTween(ref _d20IdleTween);
		KillTween(ref _d20SpinTween);
		KillTween(ref _bgBreathTween);
		KillTween(ref _bgCrossfadeTween);
		KillTween(ref _transitionTween);
	}
}
