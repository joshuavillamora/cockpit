using Godot;
using System.Collections.Generic;

public partial class Wakwak : Ability {
	[Export] public float TransformTimeFallback = 0.3f;   // used until the animation exists
	[Export] public float RevertTimeFallback = 0.3f;
	[Export] public float AimTimeLimit = 1.2f;
	[Export] public float DashDistance = 240f;
	[Export] public float DashTime = 0.18f;
	[Export] public float Cooldown = 3.0f;
	[Export] public float ExitSpeed = 200f;
	[Export] public int Damage = 20;
	[Export] public float KnockbackForce = 400f;
	[Export] public float HitstunTime = 0.6f;
	[Export] public float HitstopTime = 0.12f;
	[Export] public Texture2D CrosshairAim;
	[Export] public Texture2D CrosshairConfirm;

	private enum State { Ready, Transforming, Aiming, Dashing, Reverting }
	private State state = State.Ready;
	private float timer;
	private float cooldownTimer;
	private Vector2 aimDir = Vector2.Right;
	private Area2D slashArea;
	private readonly HashSet<Node> hitTargets = new();
	private Sprite2D crosshair;

	private static readonly Vector2 BodyCenter = new(0, 12);
	private static readonly Color AimColor = new(1f, 0.3f, 0.2f, 0.9f);

	public override bool Invulnerable => state == State.Dashing;

	public override void _Ready() {
		slashArea = GetNode<Area2D>("SlashHitbox");
		slashArea.Monitoring = false;
		slashArea.AreaEntered += OnSlashEntered;

		crosshair = GetNodeOrNull<Sprite2D>("Crosshair");
		if (crosshair != null) {
			crosshair.TopLevel = true;   
			crosshair.ZIndex = 10;
			crosshair.Visible = false;
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (cooldownTimer > 0f) cooldownTimer -= (float)delta;
	}

	public override void OnPressed() {
		if (state == State.Ready && cooldownTimer <= 0f) StartTransform();
		else if (state == State.Aiming) StartDash();
		// presses during Transforming / Dashing / Reverting are ignored
	}

	// ---- Phase 1: transformation ----
	private void StartTransform() {
		state = State.Transforming;
		Active = true;
		timer = Fighter.AnimLength("wakwak_transform", TransformTimeFallback);
		aimDir = Fighter.FacingRight ? Vector2.Right : Vector2.Left;
		Fighter.Velocity = Vector2.Zero;
		Fighter.PlayAnim("wakwak_transform");
	}

	// ---- Phase 2: aiming ----
	private void StartAiming() {
		state = State.Aiming;
		ShowCrosshair(CrosshairAim);
		timer = AimTimeLimit;
		Fighter.PlayAnim("wakwak_aim");
		QueueRedraw();
	}

	// ---- Phase 3: hit phase (the dash) ----
	private void StartDash() {
		state = State.Dashing;
		ShowCrosshair(CrosshairConfirm);
		timer = DashTime;
		hitTargets.Clear();
		slashArea.SetDeferred(Area2D.PropertyName.Monitoring, true);
		Fighter.SetCollisionMaskValue(2, false);          // zip through platforms
		Fighter.PlayAnim("wakwak_dash");
		QueueRedraw();
	}

	// ---- Phase 4: revert to chicken ----
	private void StartRevert() {
		state = State.Reverting;
		timer = Fighter.AnimLength("wakwak_revert", RevertTimeFallback);
		slashArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
		Fighter.SetCollisionMaskValue(2, true);
		Fighter.Velocity = aimDir * ExitSpeed;
		Fighter.PlayAnim("wakwak_revert");
		HideCrosshair();
	}

	private void Finish(float cooldown) {
		state = State.Ready;
		Active = false;
		cooldownTimer = cooldown;
		slashArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
		Fighter.SetCollisionMaskValue(2, true);
		QueueRedraw();
		HideCrosshair();
	}

	private void ShowCrosshair(Texture2D tex) {
		if (crosshair == null) return;
		crosshair.Texture = tex;
		crosshair.Visible = true;
	}

	private void HideCrosshair() {
		if (crosshair != null) crosshair.Visible = false;
	}

	public override void Cancel() {
		if (state == State.Ready) return;
		Finish(Cooldown * 0.5f);
	}

	public override void Tick(float delta) {
		switch (state) {
			case State.Transforming:
				Fighter.Velocity = Vector2.Zero;          // hover while transforming
				timer -= delta;
				if (timer <= 0f) StartAiming();
				break;

			case State.Aiming:
				Fighter.Velocity = Vector2.Zero;          // hover while aiming
				Vector2 input = Fighter.GetAimInput();
				if (input.Length() > 0.3f) aimDir = SnapTo8(input);
				if (crosshair != null) {
					crosshair.GlobalPosition = Fighter.GlobalPosition + BodyCenter + aimDir * DashDistance;
				}
				if (Mathf.Abs(aimDir.X) > 0.1f) Fighter.Face(aimDir.X > 0);
				timer -= delta;
				if (timer <= 0f) StartDash();             // out of time: fire anyway
				QueueRedraw();
				break;

			case State.Dashing:
				Fighter.Velocity = aimDir * (DashDistance / DashTime);
				timer -= delta;
				if (timer <= 0f) StartRevert();
				break;

			case State.Reverting:
				// keep gravity on Y, bleed off X
				Fighter.Velocity = new Vector2(
					Mathf.MoveToward(Fighter.Velocity.X, 0f, 1500f * delta), Fighter.Velocity.Y);
				timer -= delta;
				if (timer <= 0f) Finish(Cooldown);
				break;
		}
	}

	private static Vector2 SnapTo8(Vector2 v) {
		float step = Mathf.Pi / 4f;
		float angle = Mathf.Round(v.Angle() / step) * step;
		return Vector2.Right.Rotated(angle);
	}

	private void OnSlashEntered(Area2D area) {
		Node target = area.GetParent();
		if (target == Fighter || hitTargets.Contains(target)) return;
		hitTargets.Add(target);

		if (target is Fighters victim) {
			Vector2 kb = aimDir * KnockbackForce + new Vector2(0, -80);
			victim.TakeHit(Damage, kb, HitstunTime, HitstopTime);
		}
	}

	public override void _Draw() {
		if (state != State.Aiming) return;
		Vector2 end = BodyCenter + aimDir * DashDistance;
		DrawLine(BodyCenter, end, AimColor, 2f);
		DrawCircle(end, 6f, AimColor);
	}
}
