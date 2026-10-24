using Godot;
using System.Collections.Generic;

public partial class JoloanoStomp : Ability {
	[Export] public float GrowScale = 12f;
	[Export] public float GrowTimeFallback = 0.6f;     // used until the animation exists
	[Export] public float RevertTimeFallback = 0.5f;
	[Export] public float StompWindup = 0.25f;         // time from stomp start to the impact
	[Export] public float StompActive = 0.15f;         // how long the shockwave can hit
	[Export] public float StompRecover = 0.3f;         // pause after the impact
	[Export] public float Cooldown = 5f;
	[Export] public Vector2 StompSize = new Vector2(220, 60);
	[Export] public int Damage = 20;
	[Export] public Vector2 Knockback = new Vector2(350, -450);
	[Export] public float HitstunTime = 0.8f;
	[Export] public float HitstopTime = 0.2f;

	private enum State { Ready, Growing, Stomping, Reverting }
	private State state = State.Ready;
	private float timer;          // counts down in Growing / Reverting
	private float phaseTime;      // length of the current Growing / Reverting phase
	private float stompClock;     // counts up during Stomping
	private bool hitOn;
	private float cooldownTimer;
	private bool hitFired;

	private Area2D stompArea;
	private RectangleShape2D stompRect;
	private readonly HashSet<Node> hitTargets = new();

	public override void _Ready() {
		stompArea = GetNode<Area2D>("StompHitbox");
		CollisionShape2D shape = stompArea.GetNode<CollisionShape2D>("CollisionShape2D");
		stompRect = new RectangleShape2D();
		shape.Shape = stompRect;
		stompArea.Monitoring = false;
		stompArea.AreaEntered += OnStompEntered;
	}

	public override void _PhysicsProcess(double delta) {
		if (cooldownTimer > 0f) cooldownTimer -= (float)delta;
	}

	public override void OnPressed() {
		if (state == State.Ready && cooldownTimer <= 0f && Fighter.IsOnFloor()) StartGrow();
	}

	// ---- Phase 1: grow ----
	private void StartGrow() {
		state = State.Growing;
		Active = true;
		phaseTime = Mathf.Max(Fighter.AnimLength("ability_grow", GrowTimeFallback), 0.05f);
		timer = phaseTime;
		Fighter.PlayAnim("idle");
		Fighter.PlayAnim("ability_grow");
	}

	// ---- Phase 2: stomp ----
	private void StartStomp() {
		state = State.Stomping;
		stompClock = 0f;
		hitOn = false;
		Fighter.PlayAnim("ability_stomp");
	}

	// ---- Phase 3: shrink back ----
	private void StartRevert() {
		state = State.Reverting;
		phaseTime = Mathf.Max(Fighter.AnimLength("ability_shrink", RevertTimeFallback), 0.05f);
		timer = phaseTime;
		SetHit(false);
		Fighter.PlayAnim("ability_shrink");
	}

	private void Finish(float cooldown) {
		state = State.Ready;
		Active = false;
		cooldownTimer = cooldown;
		SetHit(false);
		Fighter.SetSpriteScale(1f);
		QueueRedraw();
	}

	public override void Cancel() {
		if (state == State.Ready) return;
		Finish(Cooldown * 0.5f);
	}

	public override void Tick(float delta) {
		// stays planted: bleed off horizontal speed, keep gravity on Y
		Fighter.Velocity = new Vector2(
			Mathf.MoveToward(Fighter.Velocity.X, 0f, 2000f * delta), Fighter.Velocity.Y);

		switch (state) {
			case State.Growing:
				timer -= delta;
				float g = Smooth(1f - timer / phaseTime);
				Fighter.SetSpriteScale(Mathf.Lerp(1f, GrowScale, g));
				if (timer <= 0f) StartStomp();
				break;

			case State.Stomping:
				stompClock += delta;
				if (!hitFired && stompClock >= StompWindup) { hitFired = true; SetHit(true); }
				if (hitOn && stompClock >= StompWindup + StompActive) SetHit(false);
				if (stompClock >= StompWindup + StompActive + StompRecover) StartRevert();
				break;

			case State.Reverting:
				timer -= delta;
				float r = Smooth(1f - timer / phaseTime);
				Fighter.SetSpriteScale(Mathf.Lerp(GrowScale, 1f, r));
				if (timer <= 0f) Finish(Cooldown);
				break;
		}
	}

	private static float Smooth(float t) {
		t = Mathf.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	// The shockwave: a wide, low box at his feet
	private void SetHit(bool on) {
		hitOn = on;
		if (on) {
			hitTargets.Clear();
			stompRect.Size = StompSize;
			stompArea.Position = new Vector2(0, Fighter.FeetOffset - StompSize.Y / 2f);
		}
		stompArea.SetDeferred(Area2D.PropertyName.Monitoring, on);
		QueueRedraw();
	}

	private void OnStompEntered(Area2D area) {
		Node target = area.GetParent();
		if (target == Fighter || hitTargets.Contains(target)) return;
		hitTargets.Add(target);

		if (target is Fighters victim) {
			float dirX = victim.GlobalPosition.X >= Fighter.GlobalPosition.X ? 1f : -1f;
			Vector2 kb = new Vector2(Knockback.X * dirX, Knockback.Y);   // knocked away from Joloano
			victim.TakeHit(Damage, kb, HitstunTime, HitstopTime);
		}
	}

	public override void _Draw() {
		if (state != State.Stomping) return;

		Vector2 center = new Vector2(0, Fighter.FeetOffset - StompSize.Y / 2f);
		Rect2 rect = new Rect2(center - StompSize / 2f, StompSize);

		Color fill = hitOn ? new Color(1f, 0.2f, 0.2f, 0.45f) : new Color(1f, 1f, 0f, 0.15f);
		DrawRect(rect, fill, true);
		DrawRect(rect, new Color(1f, 0.2f, 0.2f, 0.9f), false, 2f);
	}
}
