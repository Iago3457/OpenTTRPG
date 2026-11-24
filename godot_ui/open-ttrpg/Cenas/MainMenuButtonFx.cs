using Godot;

/// <summary>
/// Controla foco, hover, clique e destaque visual por Tween em botões do menu RPG.
/// </summary>
public partial class MainMenuButtonFx : Button
{
	[ExportGroup("Tweening & Highlight")]
	[Export] public bool IsPrimary { get; set; } = false;
	[Export] public Vector2 HoverScale { get; set; } = new Vector2(1.055f, 1.055f);
	[Export] public Vector2 PressScale { get; set; } = new Vector2(0.97f, 0.97f);
	[Export] public float TweenDuration { get; set; } = 0.18f;
	[Export] public bool EnableFocusIndicators { get; set; } = true;

	[Export] public Color NormalBgColor { get; set; } = new Color(0.10f, 0.08f, 0.12f, 0.88f);
	[Export] public Color PrimaryBgColor { get; set; } = new Color(0.30f, 0.08f, 0.11f, 0.93f);
	[Export] public Color HighlightBgColor { get; set; } = new Color(0.46f, 0.11f, 0.14f, 0.97f);
	[Export] public Color NormalBorderColor { get; set; } = new Color(0.58f, 0.43f, 0.20f, 0.88f);
	[Export] public Color HighlightBorderColor { get; set; } = new Color(0.98f, 0.82f, 0.36f, 1.0f);
	[Export] public Color NormalFontColor { get; set; } = new Color(0.93f, 0.87f, 0.76f, 1.0f);
	[Export] public Color HighlightFontColor { get; set; } = new Color(1.0f, 0.96f, 0.76f, 1.0f);

	public bool InputLocked { get; set; } = false;

	private StyleBoxFlat _runtimeStyle;
	private Control _fxClip;
	private ColorRect _leftBar;
	private ColorRect _rightBar;
	private ColorRect _shimmer;
	private Label _leftDiamond;
	private Label _rightDiamond;

	private Tween _fxTween;
	private Tween _shimmerTween;
	private Tween _pressTween;

	private Color _baseBgColor;
	private Color _currentFontColor;
	private bool _isHighlighted = false;

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;

		_baseBgColor = IsPrimary ? PrimaryBgColor : NormalBgColor;
		_currentFontColor = NormalFontColor;

		float horizontalPadding = EnableFocusIndicators ? 44.0f : 20.0f;
		float verticalPadding = EnableFocusIndicators ? 10.0f : 6.0f;
		_runtimeStyle = new StyleBoxFlat
		{
			BgColor = _baseBgColor,
			BorderColor = NormalBorderColor,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6,
			ShadowSize = 6,
			ShadowColor = new Color(0f, 0f, 0f, 0.55f),
			ContentMarginLeft = horizontalPadding,
			ContentMarginTop = verticalPadding,
			ContentMarginRight = horizontalPadding,
			ContentMarginBottom = verticalPadding
		};

		AddThemeStyleboxOverride("normal", _runtimeStyle);
		AddThemeStyleboxOverride("hover", _runtimeStyle);
		AddThemeStyleboxOverride("pressed", _runtimeStyle);
		AddThemeStyleboxOverride("disabled", _runtimeStyle);
		AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		ApplyFontColor(NormalFontColor);

		BuildFxOverlays();

		Resized += UpdatePivots;
		CallDeferred(nameof(UpdatePivots));

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
		FocusEntered += OnFocusEntered;
		FocusExited += OnFocusExited;
		ButtonDown += OnButtonDown;
		ButtonUp += OnButtonUp;
	}

	private void BuildFxOverlays()
	{
		_fxClip = new Control
		{
			Name = "FxClip",
			MouseFilter = MouseFilterEnum.Ignore,
			ClipContents = true
		};
		_fxClip.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(_fxClip);

		var moltenGold = new Color(0.98f, 0.82f, 0.36f, 1.0f);

		_leftBar = new ColorRect
		{
			Name = "LeftBar",
			MouseFilter = MouseFilterEnum.Ignore,
			Color = moltenGold,
			Modulate = new Color(1f, 1f, 1f, 0f),
			Scale = new Vector2(1f, 0f),
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = 2f,
			OffsetRight = 5f,
			OffsetBottom = -2f
		};
		_fxClip.AddChild(_leftBar);

		_rightBar = new ColorRect
		{
			Name = "RightBar",
			MouseFilter = MouseFilterEnum.Ignore,
			Color = moltenGold,
			Modulate = new Color(1f, 1f, 1f, 0f),
			Scale = new Vector2(1f, 0f),
			AnchorLeft = 1f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -5f,
			OffsetTop = 2f,
			OffsetRight = 0f,
			OffsetBottom = -2f
		};
		_fxClip.AddChild(_rightBar);

		_shimmer = new ColorRect
		{
			Name = "Shimmer",
			MouseFilter = MouseFilterEnum.Ignore,
			Color = new Color(1f, 0.92f, 0.65f, 0.22f),
			Visible = false,
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 0f,
			AnchorBottom = 1f,
			OffsetLeft = -64f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f
		};
		_fxClip.AddChild(_shimmer);

		if (EnableFocusIndicators)
		{
			_leftDiamond = CreateDiamondLabel("LeftDiamond", true, moltenGold);
			_fxClip.AddChild(_leftDiamond);

			_rightDiamond = CreateDiamondLabel("RightDiamond", false, moltenGold);
			_fxClip.AddChild(_rightDiamond);
		}
	}

	private Label CreateDiamondLabel(string nodeName, bool isLeft, Color color)
	{
		var lbl = new Label
		{
			Name = nodeName,
			Text = "❖",
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0f),
			Scale = new Vector2(0.6f, 0.6f),
			AnchorTop = 0.5f,
			AnchorBottom = 0.5f,
			OffsetTop = -14f,
			OffsetBottom = 14f
		};

		if (isLeft)
		{
			lbl.AnchorLeft = 0f;
			lbl.AnchorRight = 0f;
			lbl.OffsetLeft = 14f;
			lbl.OffsetRight = 36f;
		}
		else
		{
			lbl.AnchorLeft = 1f;
			lbl.AnchorRight = 1f;
			lbl.OffsetLeft = -36f;
			lbl.OffsetRight = -14f;
		}

		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeFontSizeOverride("font_size", 18);
		return lbl;
	}

	private void UpdatePivots()
	{
		PivotOffset = Size * 0.5f;

		if (_leftBar != null)
		{
			_leftBar.PivotOffset = new Vector2(0f, _leftBar.Size.Y * 0.5f);
		}
		if (_rightBar != null)
		{
			_rightBar.PivotOffset = new Vector2(_rightBar.Size.X, _rightBar.Size.Y * 0.5f);
		}
		if (_leftDiamond != null)
		{
			_leftDiamond.PivotOffset = _leftDiamond.Size * 0.5f;
		}
		if (_rightDiamond != null)
		{
			_rightDiamond.PivotOffset = _rightDiamond.Size * 0.5f;
		}
	}

	private void OnMouseEntered()
	{
		if (Disabled || InputLocked) return;
		GrabFocus();
		SetHighlight(true);
	}

	private void OnMouseExited()
	{
		if (Disabled || InputLocked) return;
		if (HasFocus())
		{
			ReleaseFocus();
		}
		SetHighlight(false);
	}

	private void OnFocusEntered()
	{
		if (Disabled || InputLocked) return;
		SetHighlight(true);
	}

	private void OnFocusExited()
	{
		SetHighlight(false);
	}

	private void OnButtonDown()
	{
		if (Disabled || InputLocked) return;
		UpdatePivots();
		KillTween(ref _pressTween);
		_pressTween = CreateTween();
		_pressTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		_pressTween.TweenProperty(this, "scale", PressScale, 0.07f);
	}

	private void OnButtonUp()
	{
		if (Disabled || InputLocked) return;
		UpdatePivots();
		KillTween(ref _pressTween);
		_pressTween = CreateTween();
		_pressTween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_pressTween.TweenProperty(this, "scale", _isHighlighted ? HoverScale : Vector2.One, 0.12f);
	}

	public void ForceUnhighlight()
	{
		if (HasFocus())
		{
			ReleaseFocus();
		}
		SetHighlight(false);
	}

	public void SetHighlight(bool active)
	{
		if (_isHighlighted == active) return;
		_isHighlighted = active;

		UpdatePivots();
		KillTween(ref _fxTween);
		KillTween(ref _pressTween);

		Color targetBg = active ? HighlightBgColor : _baseBgColor;
		Color targetBorder = active ? HighlightBorderColor : NormalBorderColor;
		int targetShadowSize = active ? 18 : 6;
		Color targetShadowColor = active
			? new Color(0.95f, 0.72f, 0.22f, 0.45f)
			: new Color(0f, 0f, 0f, 0.55f);
		Color targetFontColor = active ? HighlightFontColor : NormalFontColor;

		_fxTween = CreateTween();
		_fxTween.SetParallel(true);
		_fxTween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

		_fxTween.TweenProperty(this, "scale", active ? HoverScale : Vector2.One, TweenDuration);
		_fxTween.TweenProperty(_runtimeStyle, "bg_color", targetBg, TweenDuration);
		_fxTween.TweenProperty(_runtimeStyle, "border_color", targetBorder, TweenDuration);
		_fxTween.TweenProperty(_runtimeStyle, "shadow_size", targetShadowSize, TweenDuration);
		_fxTween.TweenProperty(_runtimeStyle, "shadow_color", targetShadowColor, TweenDuration);

		_fxTween.TweenMethod(
			Callable.From<Color>(ApplyFontColor),
			_currentFontColor,
			targetFontColor,
			TweenDuration
		);

		_fxTween.TweenProperty(_leftBar, "scale:y", active ? 1.0f : 0.0f, TweenDuration);
		_fxTween.TweenProperty(_leftBar, "modulate:a", active ? 1.0f : 0.0f, TweenDuration);
		_fxTween.TweenProperty(_rightBar, "scale:y", active ? 1.0f : 0.0f, TweenDuration);
		_fxTween.TweenProperty(_rightBar, "modulate:a", active ? 1.0f : 0.0f, TweenDuration);

		if (EnableFocusIndicators && _leftDiamond != null && _rightDiamond != null)
		{
			Vector2 targetDiamondScale = active ? Vector2.One : new Vector2(0.6f, 0.6f);
			float targetDiamondAlpha = active ? 1.0f : 0.0f;

			_fxTween.TweenProperty(_leftDiamond, "modulate:a", targetDiamondAlpha, TweenDuration);
			_fxTween.TweenProperty(_leftDiamond, "scale", targetDiamondScale, TweenDuration);
			_fxTween.TweenProperty(_rightDiamond, "modulate:a", targetDiamondAlpha, TweenDuration);
			_fxTween.TweenProperty(_rightDiamond, "scale", targetDiamondScale, TweenDuration);
		}

		if (active)
		{
			TriggerShimmerSweep();
		}
	}

	private void TriggerShimmerSweep()
	{
		if (_shimmer == null || _fxClip == null) return;

		KillTween(ref _shimmerTween);
		float width = Mathf.Max(Size.X, 120f);
		_shimmer.Position = new Vector2(-64f, 0f);
		_shimmer.Size = new Vector2(64f, _fxClip.Size.Y);
		_shimmer.Visible = true;
		_shimmer.Modulate = new Color(1f, 1f, 1f, 1f);

		_shimmerTween = CreateTween();
		_shimmerTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		_shimmerTween.TweenProperty(_shimmer, "position:x", width + 16f, 0.42f);
		_shimmerTween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(_shimmer))
			{
				_shimmer.Visible = false;
			}
		}));
	}

	private void ApplyFontColor(Color color)
	{
		_currentFontColor = color;
		AddThemeColorOverride("font_color", color);
		AddThemeColorOverride("font_hover_color", color);
		AddThemeColorOverride("font_focus_color", color);
		AddThemeColorOverride("font_pressed_color", color);
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
		KillTween(ref _fxTween);
		KillTween(ref _shimmerTween);
		KillTween(ref _pressTween);
	}
}
