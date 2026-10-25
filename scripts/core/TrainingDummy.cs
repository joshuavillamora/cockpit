using Godot;

// A punching bag for the lobby. A fighter's attack hitbox calls TakeDamage() on the parent
// of any hurtbox it touches, so this node only needs that method and a "Hurtbox" Area2D child.
// Above its head it shows the current combo (hits within a second of each other) and the damage per
// second over the last two seconds, and it pops each hit's damage as a rising number.
public partial class TrainingDummy : Node2D {
	[Signal] public delegate void HitEventHandler(int hits, int damage);

	private const float PopupRise = 18f;
	private const float PopupSeconds = 0.6f;
	private const ulong ComboWindowMs = 1000;
	private const ulong DpsWindowMs = 2000;
	private static readonly Color PopupColor = new(1f, 0.85f, 0.3f);

	private readonly System.Collections.Generic.Queue<(ulong At, int Damage)> recent = new();
	private Node2D body;
	private Label hitLabel;
	private Label dpsLabel;
	private int hits;
	private int combo;
	private ulong lastHitMs;
	private bool leanRight = true;

	public override void _Ready() {
		body = GetNode<Node2D>("Body");
		hitLabel = GetNode<Label>("HitLabel");
		dpsLabel = new Label {
			Position = hitLabel.Position - new Vector2(0, 14),
			Size = hitLabel.Size,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		dpsLabel.AddThemeColorOverride("font_color", PopupColor);
		AddChild(dpsLabel);
	}

	// DPS keeps updating (and drops back to nothing) after the hits stop
	public override void _Process(double delta) {
		ulong now = Time.GetTicksMsec();
		while (recent.Count > 0 && now - recent.Peek().At > DpsWindowMs) recent.Dequeue();
		int total = 0;
		foreach (var hit in recent) total += hit.Damage;
		dpsLabel.Text = total > 0 ? $"DPS {Mathf.RoundToInt(total * 1000f / DpsWindowMs)}" : "";
		if (combo > 0 && now - lastHitMs > ComboWindowMs) {
			combo = 0;
			hitLabel.Text = "HIT ME!";
		}
	}

	public void TakeDamage(int damage) {
		ulong now = Time.GetTicksMsec();
		hits++;
		combo = now - lastHitMs <= ComboWindowMs ? combo + 1 : 1;
		lastHitMs = now;
		recent.Enqueue((now, damage));
		hitLabel.Text = combo > 1 ? $"{combo} HITS" : "1 HIT";   // short: the dummy stands in a narrow gap
		EmitSignal(SignalName.Hit, hits, damage);
		PopDamage(damage);

		// Lean away, then spring back (the body pivots at the dummy's feet)
		body.Rotation = leanRight ? 0.35f : -0.35f;
		leanRight = !leanRight;
		CreateTween().TweenProperty(body, "rotation", 0f, 0.35)
			.SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
	}

	// The damage number floats up from the dummy's chest and fades out
	private void PopDamage(int damage) {
		var popup = new Label {
			Text = damage.ToString(),
			Position = new Vector2(leanRight ? 6 : -22, -40),
			Size = new Vector2(16, 16),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		popup.AddThemeColorOverride("font_color", PopupColor);
		AddChild(popup);

		Tween tween = popup.CreateTween().SetParallel();
		tween.TweenProperty(popup, "position:y", popup.Position.Y - PopupRise, PopupSeconds).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(popup, "modulate:a", 0f, PopupSeconds).SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(popup.QueueFree));
	}
}
