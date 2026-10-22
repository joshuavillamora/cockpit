using Godot;

// A punching bag for the lobby. A fighter's attack hitbox calls TakeDamage() on the parent
// of any hurtbox it touches, so this node only needs that method and a "Hurtbox" Area2D child.
// The lobby shows the hit count and last damage on its HITS plate (the Hit signal).
public partial class TrainingDummy : Node2D {
	[Signal] public delegate void HitEventHandler(int hits, int damage);

	private Node2D body;
	private int hits;
	private bool leanRight = true;

	public override void _Ready() {
		body = GetNode<Node2D>("Body");
	}

	public void TakeDamage(int damage) {
		hits++;
		EmitSignal(SignalName.Hit, hits, damage);

		// Lean away, then spring back (the body pivots at the dummy's feet)
		body.Rotation = leanRight ? 0.35f : -0.35f;
		leanRight = !leanRight;
		CreateTween().TweenProperty(body, "rotation", 0f, 0.35)
			.SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
	}
}
