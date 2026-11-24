using Godot;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Renderizador visual procedural de Dados Poliedrais (d4, d6, d8, d10, d12, d20, d100)
/// utilizando as APIs visuais nativas do Godot 4:
/// - CanvasItem._Draw() (polígonos facetados 2D/3D, chanfros, brilhos e linhas internas)
/// - Tween API (rotação com física elástica, salto de escala, tremor em falha crítica)
/// - CPUParticles2D (explosão de partículas douradas no Crítico Natural e brasas na Falha Crítica)
/// </summary>
public partial class DiceVisualCanvas : Control
{
	public enum RollState
	{
		Idle,
		Rolling,
		NormalResult,
		CriticalHit,
		CriticalFumble
	}

	private int _dieSides = 20;
	private string _displayText = "d20";
	private string _subLabelText = "PRONTO";
	private RollState _state = RollState.Idle;

	private float _dieRotation = 0f;
	private float _dieScale = 1.0f;
	private Vector2 _shakeOffset = Vector2.Zero;
	private float _glowPulse = 0f;
	private float _rollTimer = 0f;
	private float _rollDuration = 0f;
	private int _finalTotal = 20;
	private int _finalNatural = 20;
	private RollState _targetState = RollState.NormalResult;

	private Tween _activeTween;
	private CpuParticles2D _particlesCrit;
	private CpuParticles2D _particlesNormal;
	private readonly RandomNumberGenerator _rng = new();

	public event Action<int> RollAnimationFinished;

	public override void _Ready()
	{
		_rng.Randomize();
		CustomMinimumSize = new Vector2(180, 175);
		MouseFilter = MouseFilterEnum.Ignore;
		SetupParticleEmitters();
	}

	private void SetupParticleEmitters()
	{
		// Emissor de partículas de Crítico / Explosão Dourada
		_particlesCrit = new CpuParticles2D
		{
			Emitting = false,
			OneShot = true,
			Amount = 42,
			Lifetime = 0.95f,
			Explosiveness = 0.92f,
			Randomness = 0.4f,
			Direction = new Vector2(0, -1),
			Spread = 180f,
			Gravity = new Vector2(0, 140f),
			InitialVelocityMin = 90f,
			InitialVelocityMax = 220f,
			ScaleAmountMin = 3.5f,
			ScaleAmountMax = 7.5f,
			Color = new Color(1f, 0.86f, 0.25f, 1f)
		};
		AddChild(_particlesCrit);

		// Emissor de partículas arcanas / impacto normal
		_particlesNormal = new CpuParticles2D
		{
			Emitting = false,
			OneShot = true,
			Amount = 20,
			Lifetime = 0.65f,
			Explosiveness = 0.88f,
			Direction = new Vector2(0, -1),
			Spread = 180f,
			Gravity = new Vector2(0, 90f),
			InitialVelocityMin = 55f,
			InitialVelocityMax = 135f,
			ScaleAmountMin = 2.5f,
			ScaleAmountMax = 5.0f,
			Color = new Color(0.45f, 0.82f, 1f, 0.9f)
		};
		AddChild(_particlesNormal);
	}

	public static int DetectDieSides(string expression)
	{
		if (string.IsNullOrWhiteSpace(expression)) return 20;
		var m = Regex.Match(expression, @"d(\d+)", RegexOptions.IgnoreCase);
		if (m.Success && int.TryParse(m.Groups[1].Value, out int sides))
		{
			if (sides is 4 or 6 or 8 or 10 or 12 or 20 or 100)
				return sides;
		}
		return 20;
	}

	/// <summary>
	/// Inicia a animação visual de rolagem do dado poliedral e revela o resultado ao final.
	/// </summary>
	private string _customSubLabel = null;

	public void PlayRollAnimation(string expression, int finalTotal, int naturalDiceSum, bool isCrit, bool isFumble, float duration = 0.42f, string customSubLabel = null)
	{
		_dieSides = DetectDieSides(expression);
		_finalTotal = finalTotal;
		_finalNatural = naturalDiceSum;
		_customSubLabel = customSubLabel;
		_targetState = isCrit ? RollState.CriticalHit : (isFumble ? RollState.CriticalFumble : RollState.NormalResult);

		_state = RollState.Rolling;
		_rollDuration = Math.Max(0.15f, duration);
		_rollTimer = 0f;
		_subLabelText = $"ROLANDO d{_dieSides}...";

		_activeTween?.Kill();
		_activeTween = CreateTween();
		_dieRotation = -Mathf.Pi * 1.6f;
		_dieScale = 0.68f;

		// Animação de giro e salto elástico usando a Tween API do Godot 4
		_activeTween.SetParallel(true);
		_activeTween.TweenMethod(Callable.From<float>(r => { _dieRotation = r; QueueRedraw(); }),
			-Mathf.Pi * 1.75f, 0f, _rollDuration)
			.SetTrans(Tween.TransitionType.Quart)
			.SetEase(Tween.EaseType.Out);

		_activeTween.TweenMethod(Callable.From<float>(s => { _dieScale = s; QueueRedraw(); }),
			0.65f, 1.0f, _rollDuration)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);

		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!IsVisibleInTree())
		{
			if (_state == RollState.Rolling)
			{
				FinishRollAnimation();
			}
			return;
		}

		float dt = (float)delta;
		_glowPulse += dt * 4.2f;

		Vector2 center = Size * 0.5f;
		if (_particlesCrit != null) _particlesCrit.Position = center;
		if (_particlesNormal != null) _particlesNormal.Position = center;

		if (_state == RollState.Rolling)
		{
			_rollTimer += dt;
			// Cicla números aleatórios nas faces enquanto o dado gira
			int fakeNum = _rng.RandiRange(1, Math.Max(2, _dieSides));
			_displayText = fakeNum.ToString();
			_shakeOffset = new Vector2(_rng.RandfRange(-3.5f, 3.5f), _rng.RandfRange(-3.5f, 3.5f));

			if (_rollTimer >= _rollDuration)
			{
				FinishRollAnimation();
			}
			QueueRedraw();
		}
		else if (_state is RollState.CriticalHit or RollState.CriticalFumble)
		{
			QueueRedraw();
		}
	}

	private void FinishRollAnimation()
	{
		_state = _targetState;
		_displayText = _finalTotal.ToString();
		_shakeOffset = Vector2.Zero;
		_dieRotation = 0f;

		if (_state == RollState.CriticalHit)
		{
			_subLabelText = !string.IsNullOrEmpty(_customSubLabel) ? _customSubLabel : $"CRÍTICO NATURAL (d{_dieSides})";
			_particlesCrit.Color = new Color(1f, 0.86f, 0.22f, 1f);
			_particlesCrit.Restart();
			_particlesCrit.Emitting = true;

			// Pulso de impacto dourado
			_activeTween?.Kill();
			_activeTween = CreateTween();
			_activeTween.TweenMethod(Callable.From<float>(s => { _dieScale = s; QueueRedraw(); }), 1.28f, 1.0f, 0.35f)
				.SetTrans(Tween.TransitionType.Elastic)
				.SetEase(Tween.EaseType.Out);
		}
		else if (_state == RollState.CriticalFumble)
		{
			_subLabelText = !string.IsNullOrEmpty(_customSubLabel) ? _customSubLabel : "FALHA CRÍTICA (1 NATURAL)";
			_particlesCrit.Color = new Color(0.95f, 0.22f, 0.22f, 1f);
			_particlesCrit.Restart();
			_particlesCrit.Emitting = true;

			// Tremor horizontal de falha crítica
			_activeTween?.Kill();
			_activeTween = CreateTween();
			_activeTween.TweenMethod(Callable.From<float>(t =>
			{
				_shakeOffset = new Vector2(Mathf.Sin(t * 36f) * (1f - t) * 11f, 0f);
				QueueRedraw();
			}), 0f, 1f, 0.45f);
		}
		else
		{
			_subLabelText = !string.IsNullOrEmpty(_customSubLabel) ? _customSubLabel : $"d{_dieSides} (Dado: {_finalNatural})";
			_particlesNormal.Color = new Color(0.45f, 0.82f, 1f, 0.9f);
			_particlesNormal.Restart();
			_particlesNormal.Emitting = true;

			_activeTween?.Kill();
			_activeTween = CreateTween();
			_activeTween.TweenMethod(Callable.From<float>(s => { _dieScale = s; QueueRedraw(); }), 1.15f, 1.0f, 0.22f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}

		RollAnimationFinished?.Invoke(_finalTotal);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = (Size * 0.5f) + _shakeOffset;
		float radius = Mathf.Min(Size.X, Size.Y) * 0.38f * _dieScale;

		// Define paleta de cores conforme o estado (Crítico Dourado, Falha Carmesim, Rolando Ciano, Padrão Rubi/Arton)
		Color primaryFill;
		Color facetFill;
		Color edgeColor;
		Color glowColor;

		switch (_state)
		{
			case RollState.CriticalHit:
				primaryFill = new Color(0.38f, 0.27f, 0.04f, 0.98f);
				facetFill   = new Color(0.54f, 0.39f, 0.07f, 0.98f);
				edgeColor   = new Color(1.0f, 0.86f, 0.28f, 1.0f);
				glowColor   = new Color(1.0f, 0.82f, 0.15f, 0.24f + 0.10f * Mathf.Sin(_glowPulse));
				break;
			case RollState.CriticalFumble:
				primaryFill = new Color(0.32f, 0.06f, 0.07f, 0.98f);
				facetFill   = new Color(0.46f, 0.09f, 0.11f, 0.98f);
				edgeColor   = new Color(1.0f, 0.32f, 0.32f, 1.0f);
				glowColor   = new Color(0.95f, 0.18f, 0.18f, 0.22f + 0.08f * Mathf.Sin(_glowPulse));
				break;
			case RollState.Rolling:
				primaryFill = new Color(0.12f, 0.20f, 0.32f, 0.95f);
				facetFill   = new Color(0.18f, 0.30f, 0.48f, 0.95f);
				edgeColor   = new Color(0.48f, 0.86f, 1.0f, 1.0f);
				glowColor   = new Color(0.35f, 0.78f, 1.0f, 0.20f);
				break;
			default:
				primaryFill = new Color(0.34f, 0.09f, 0.12f, 0.96f);
				facetFill   = new Color(0.48f, 0.13f, 0.17f, 0.96f);
				edgeColor   = new Color(0.92f, 0.74f, 0.38f, 1.0f);
				glowColor   = new Color(0.85f, 0.65f, 0.28f, 0.14f);
				break;
		}

		// Halo luminoso de fundo
		DrawCircle(center, radius * 1.24f, glowColor);
		DrawArc(center, radius * 1.15f, 0f, Mathf.Tau, 48, new Color(edgeColor.R, edgeColor.G, edgeColor.B, 0.28f), 1.5f, true);

		// Desenha a geometria poliedral específica do dado (d4, d6, d8, d10, d12, d20, d100)
		switch (_dieSides)
		{
			case 4:
				DrawD4(center, radius, primaryFill, facetFill, edgeColor);
				break;
			case 6:
				DrawD6(center, radius, primaryFill, facetFill, edgeColor);
				break;
			case 8:
				DrawD8(center, radius, primaryFill, facetFill, edgeColor);
				break;
			case 10:
			case 100:
				DrawD10(center, radius, primaryFill, facetFill, edgeColor);
				break;
			case 12:
				DrawD12(center, radius, primaryFill, facetFill, edgeColor);
				break;
			default:
				DrawD20(center, radius, primaryFill, facetFill, edgeColor);
				break;
		}

		// Renderiza o número central do dado
		Font font = ThemeDB.FallbackFont;
		int fontSize = _displayText.Length >= 3 ? 26 : 34;
		Vector2 textSize = font.GetStringSize(_displayText, HorizontalAlignment.Center, -1, fontSize);
		Vector2 textPos = new Vector2(center.X - textSize.X * 0.5f, center.Y + textSize.Y * 0.32f);

		// Sombra do número
		DrawString(font, textPos + new Vector2(2, 2), _displayText, HorizontalAlignment.Left, -1, fontSize, new Color(0, 0, 0, 0.75f));

		Color numberColor = _state switch
		{
			RollState.CriticalHit => new Color(1f, 0.92f, 0.45f),
			RollState.CriticalFumble => new Color(1f, 0.48f, 0.48f),
			_ => Colors.White
		};
		DrawString(font, textPos, _displayText, HorizontalAlignment.Left, -1, fontSize, numberColor);

		// Subtítulo na base do canvas
		if (!string.IsNullOrEmpty(_subLabelText))
		{
			int subSize = 12;
			Vector2 subTextSize = font.GetStringSize(_subLabelText, HorizontalAlignment.Center, -1, subSize);
			Vector2 subPos = new Vector2((Size.X - subTextSize.X) * 0.5f, Size.Y - 6f);
			DrawString(font, subPos, _subLabelText, HorizontalAlignment.Left, -1, subSize, edgeColor);
		}
	}

	private Vector2 Polar(Vector2 center, float r, float angleRad)
	{
		float a = angleRad + _dieRotation;
		return center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
	}

	private void DrawClosedOutline(Vector2[] pts, Color color, float width = 2.2f)
	{
		var closed = new Vector2[pts.Length + 1];
		Array.Copy(pts, closed, pts.Length);
		closed[pts.Length] = pts[0];
		DrawPolyline(closed, color, width, true);
	}

	// d20: Icosaedro (Hexágono externo + Triângulo central + 6 arestas radiantes)
	private void DrawD20(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		Vector2[] outer = new Vector2[6];
		for (int i = 0; i < 6; i++)
			outer[i] = Polar(c, r, -Mathf.Pi / 2f + i * Mathf.Tau / 6f);

		Vector2[] inner = new Vector2[3];
		for (int i = 0; i < 3; i++)
			inner[i] = Polar(c, r * 0.56f, -Mathf.Pi / 2f + i * Mathf.Tau / 3f);

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		// Arestas das facetas do icosaedro
		DrawLine(inner[0], outer[0], edge, 1.6f, true);
		DrawLine(inner[0], outer[1], edge, 1.4f, true);
		DrawLine(inner[0], outer[5], edge, 1.4f, true);

		DrawLine(inner[1], outer[2], edge, 1.6f, true);
		DrawLine(inner[1], outer[1], edge, 1.4f, true);
		DrawLine(inner[1], outer[3], edge, 1.4f, true);

		DrawLine(inner[2], outer[4], edge, 1.6f, true);
		DrawLine(inner[2], outer[3], edge, 1.4f, true);
		DrawLine(inner[2], outer[5], edge, 1.4f, true);

		DrawClosedOutline(inner, edge, 2.0f);
		DrawClosedOutline(outer, edge, 2.6f);
	}

	// d12: Dodecaedro (Decágono externo + Pentágono central + 5 arestas)
	private void DrawD12(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		Vector2[] outer = new Vector2[10];
		for (int i = 0; i < 10; i++)
			outer[i] = Polar(c, r, -Mathf.Pi / 2f + i * Mathf.Tau / 10f);

		Vector2[] inner = new Vector2[5];
		for (int i = 0; i < 5; i++)
			inner[i] = Polar(c, r * 0.55f, -Mathf.Pi / 2f + i * Mathf.Tau / 5f);

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		for (int i = 0; i < 5; i++)
			DrawLine(inner[i], outer[i * 2], edge, 1.7f, true);

		DrawClosedOutline(inner, edge, 2.0f);
		DrawClosedOutline(outer, edge, 2.5f);
	}

	// d10 / d100: Trapezoedro Pentagonal (Pipa / Losango de 6 vértices com coroa facetada)
	private void DrawD10(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		Vector2 top = Polar(c, r * 1.05f, -Mathf.Pi / 2f);
		Vector2 bottom = Polar(c, r * 1.05f, Mathf.Pi / 2f);
		Vector2 upperRight = Polar(c, r * 0.92f, -Mathf.Pi / 7f);
		Vector2 lowerRight = Polar(c, r * 0.92f, Mathf.Pi / 7f);
		Vector2 lowerLeft = Polar(c, r * 0.92f, Mathf.Pi - Mathf.Pi / 7f);
		Vector2 upperLeft = Polar(c, r * 0.92f, -Mathf.Pi + Mathf.Pi / 7f);

		Vector2[] outer = { top, upperRight, lowerRight, bottom, lowerLeft, upperLeft };
		Vector2[] inner =
		{
			Polar(c, r * 0.56f, -Mathf.Pi / 2f),
			Polar(c, r * 0.52f, 0f),
			Polar(c, r * 0.56f, Mathf.Pi / 2f),
			Polar(c, r * 0.52f, Mathf.Pi)
		};

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		DrawLine(top, inner[0], edge, 1.6f, true);
		DrawLine(bottom, inner[2], edge, 1.6f, true);
		DrawLine(upperRight, inner[1], edge, 1.5f, true);
		DrawLine(lowerRight, inner[1], edge, 1.5f, true);
		DrawLine(upperLeft, inner[3], edge, 1.5f, true);
		DrawLine(lowerLeft, inner[3], edge, 1.5f, true);

		DrawClosedOutline(inner, edge, 1.8f);
		DrawClosedOutline(outer, edge, 2.5f);
	}

	// d8: Octaedro (Losango com triângulo equatorial central)
	private void DrawD8(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		Vector2 top = Polar(c, r * 1.06f, -Mathf.Pi / 2f);
		Vector2 right = Polar(c, r * 0.94f, 0f);
		Vector2 bottom = Polar(c, r * 1.06f, Mathf.Pi / 2f);
		Vector2 left = Polar(c, r * 0.94f, Mathf.Pi);

		Vector2[] outer = { top, right, bottom, left };
		Vector2[] inner = { top, right, left };

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		DrawLine(left, right, edge, 2.0f, true);
		DrawLine(top, bottom, new Color(edge.R, edge.G, edge.B, 0.45f), 1.4f, true);
		DrawClosedOutline(outer, edge, 2.6f);
	}

	// d6: Cubo Chanfrado com face quadrada interna
	private void DrawD6(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		float s = r * 0.88f;
		Vector2[] outer = new Vector2[4];
		Vector2[] inner = new Vector2[4];
		for (int i = 0; i < 4; i++)
		{
			float ang = -Mathf.Pi / 4f + i * Mathf.Tau / 4f;
			outer[i] = Polar(c, s * 1.12f, ang);
			inner[i] = Polar(c, s * 0.78f, ang);
		}

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		for (int i = 0; i < 4; i++)
			DrawLine(inner[i], outer[i], edge, 1.6f, true);

		DrawClosedOutline(inner, edge, 2.0f);
		DrawClosedOutline(outer, edge, 2.6f);
	}

	// d4: Tetraedro (Pirâmide triangular com 3 facetas convergentes)
	private void DrawD4(Vector2 c, float r, Color outerFill, Color innerFill, Color edge)
	{
		Vector2[] outer = new Vector2[3];
		Vector2[] inner = new Vector2[3];
		for (int i = 0; i < 3; i++)
		{
			float ang = -Mathf.Pi / 2f + i * Mathf.Tau / 3f;
			outer[i] = Polar(c, r * 1.08f, ang);
			inner[i] = Polar(c, r * 0.56f, ang);
		}

		DrawColoredPolygon(outer, outerFill);
		DrawColoredPolygon(inner, innerFill);

		for (int i = 0; i < 3; i++)
			DrawLine(outer[i], inner[i], edge, 1.8f, true);

		DrawClosedOutline(inner, edge, 1.8f);
		DrawClosedOutline(outer, edge, 2.6f);
	}
}
