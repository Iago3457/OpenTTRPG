using Godot;

/// <summary>
/// Camada leve de partículas/brasas douradas e carmesim para ambientação RPG no menu principal.
/// </summary>
public partial class MainMenuAmbientFx : Control
{
	[Export] public bool Enabled { get; set; } = true;
	[Export] public int EmberCount { get; set; } = 28;

	private struct EmberParticle
	{
		public float XNorm;
		public float YNorm;
		public float SpeedY;
		public float Radius;
		public float Phase;
		public float DriftFreq;
		public float DriftAmp;
		public float BaseAlpha;
		public Color BaseColor;
	}

	private EmberParticle[] _embers = System.Array.Empty<EmberParticle>();
	private double _elapsed = 0.0;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		InitParticles();
	}

	private void InitParticles()
	{
		int count = Mathf.Max(0, EmberCount);
		_embers = new EmberParticle[count];

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		var goldWarm = new Color(0.98f, 0.80f, 0.34f, 1.0f);
		var emberAmber = new Color(0.95f, 0.56f, 0.22f, 1.0f);
		var crimsonGlow = new Color(0.86f, 0.24f, 0.20f, 1.0f);

		for (int i = 0; i < count; i++)
		{
			float roll = rng.Randf();
			Color chosen = roll < 0.55f ? goldWarm : (roll < 0.85f ? emberAmber : crimsonGlow);

			_embers[i] = new EmberParticle
			{
				XNorm = rng.RandfRange(0.02f, 0.98f),
				YNorm = rng.RandfRange(0.0f, 1.0f),
				SpeedY = rng.RandfRange(0.018f, 0.052f),
				Radius = rng.RandfRange(1.4f, 3.6f),
				Phase = rng.RandfRange(0.0f, Mathf.Tau),
				DriftFreq = rng.RandfRange(0.6f, 1.6f),
				DriftAmp = rng.RandfRange(0.008f, 0.024f),
				BaseAlpha = rng.RandfRange(0.28f, 0.70f),
				BaseColor = chosen
			};
		}
	}

	public override void _Process(double delta)
	{
		if (!Enabled || _embers.Length == 0) return;

		_elapsed += delta;
		float dt = (float)delta;

		for (int i = 0; i < _embers.Length; i++)
		{
			_embers[i].YNorm -= _embers[i].SpeedY * dt;
			if (_embers[i].YNorm < -0.04f)
			{
				_embers[i].YNorm = 1.04f;
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Enabled || _embers.Length == 0) return;

		Vector2 viewSize = Size;
		if (viewSize.X <= 1f || viewSize.Y <= 1f) return;

		for (int i = 0; i < _embers.Length; i++)
		{
			EmberParticle e = _embers[i];

			// Suaviza alpha próximo das bordas verticalmente (0..1)
			float verticalFade = Mathf.Clamp(Mathf.Sin(Mathf.Clamp(e.YNorm, 0f, 1f) * Mathf.Pi), 0f, 1f);
			float twinkle = 0.68f + 0.32f * Mathf.Sin((float)_elapsed * (e.DriftFreq * 1.7f) + e.Phase);
			float alpha = e.BaseAlpha * verticalFade * twinkle;
			if (alpha <= 0.02f) continue;

			float xOffset = Mathf.Sin((float)_elapsed * e.DriftFreq + e.Phase) * e.DriftAmp;
			Vector2 pos = new Vector2(
				(e.XNorm + xOffset) * viewSize.X,
				e.YNorm * viewSize.Y
			);

			Color glowColor = new Color(e.BaseColor.R, e.BaseColor.G, e.BaseColor.B, alpha * 0.28f);
			Color coreColor = new Color(
				Mathf.Min(1f, e.BaseColor.R + 0.12f),
				Mathf.Min(1f, e.BaseColor.G + 0.12f),
				Mathf.Min(1f, e.BaseColor.B + 0.12f),
				alpha
			);

			DrawCircle(pos, e.Radius * 2.4f, glowColor);
			DrawCircle(pos, e.Radius * 0.85f, coreColor);
		}
	}
}
