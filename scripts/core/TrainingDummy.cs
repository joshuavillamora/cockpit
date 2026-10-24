using Godot;

// A punching bag for the lobby. A fighter's attack hitbox calls TakeDamage() on the parent
// of any hurtbox it touches, so this node only needs that method and a "Hurtbox" Area2D child.
// It counts hits above its head and pops each hit's damage as a rising number.
public partial class TrainingDummy : Node2D {
	[Signal] public delegate void HitEventHandler(int hits, int damage);

	private const float PopupRise = 18f;
	private const float PopupSeconds = 0.6f;
	private static readonly Color PopupColor = new(1f, 0.85f, 0.3f);

	private Node2D body;
	private Label hitLabel;
	private int hits;
	private bool leanRight = true;

	public override void _Ready() {
		body = GetNode<Node2D>("Body");
		hitLabel = GetNode<Label>("HitLabel");
	}

	public void TakeDamage(int damage) {
		hits++;
		hitLabel.Text = $"HITS {hits}";
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
