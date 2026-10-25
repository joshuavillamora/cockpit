using Godot;
using System;
using System.Collections.Generic;

public partial class Fighters : CharacterBody2D {
	public const float Speed = 175.0f;
	public const float Acceleration = 800.0f;
	public const float Deceleration = 1800.0f;
	public const float JumpVelocity = -350.0f;
	public const float JumpCutMultiplier = 0.5f;

	public const float AttackAccelMultiplier = 0.1f;
	public const float AttackDecelMultiplier = 0.1f;
	private const float AttackBufferTime = 0.15f;

	[Signal] public delegate void AbilityChargeChangedEventHandler(float ratio);
	[Signal] public delegate void AbilityUsedEventHandler();

	[Export] public float MaxAbilityCharge = 100f;
	[Export] public float ChargePerHit = 10f;
	[Export] public int PlayerId = 1;
	[Export] public int MaxHealth = 100;
	[Export] public bool StartFacingRight = true;
	[Export] public float BlockDamageMultiplier = 0.2f;
	[Export] public float RunSpeed = 250f;
	[Export] public float FeetOffset = 32f;

	// UI / match flow hooks (HUD and MatchController listen to these)
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();
	public bool InputLocked { get; set; }
	public bool IsDead => Health <= 0;

	// ---------- Attack data (numbers come from the frame data workbook) ----------

	protected enum AttackType {
		NeutralAttack,
		SideAttack,
		UpAttack,
		DownAttack,
		NeutralAir,
		SideAir,
		UpAir,
		DownAir,
	}

	protected class AttackData {
		public string Animation;
		public Vector2 Size;
		public Vector2 Offset;
		public int StartFrame;
		public int EndFrame;
		public int Damage;
		public float Mobility;   // 0 = commits you, 1 = keeps nearly full movement
		public float MomentumKeep;
		public Vector2 Knockback;
		public float Hitstun; 
		public float Hitstop; 

		public AttackData(string animation, Vector2 size, Vector2 offset, int startFrame, int endFrame, int damage, float mobility, float momentumKeep, Vector2 knockback, float hitstun, float hitstop) {
			Animation = animation; Size = size; Offset = offset;
			StartFrame = startFrame; EndFrame = endFrame;
			Damage = damage; Mobility = mobility; MomentumKeep = momentumKeep; 
			Knockback = knockback; Hitstun = hitstun; Hitstop = hitstop;
		}
		public AttackData Clone() => (AttackData)MemberwiseClone();
	}

	private static readonly Dictionary<AttackType, AttackData> Defaults = new() {
		{ AttackType.NeutralAttack, new AttackData("neutral_attack", new Vector2(24, 16), new Vector2(24, 8),  1, 2, 4,  0.8f, 0.7f, new Vector2(100, -40),  0.50f, 0.10f) },
		{ AttackType.SideAttack,    new AttackData("side_attack",    new Vector2(40, 16), new Vector2(36, 12), 3, 6, 8,  0.4f, 0.5f, new Vector2(220, -120), 0.70f, 0.15f) },
		{ AttackType.UpAttack,      new AttackData("up_attack",      new Vector2(44, 30), new Vector2(0, -12), 3, 3, 7,  0.4f, 0.4f, new Vector2(40, -300),  0.80f, 0.15f) },
		{ AttackType.DownAttack,    new AttackData("down_attack",    new Vector2(32, 20), new Vector2(28, 22), 2, 7, 14, 0.1f, 0.2f, new Vector2(100, -150), 1.00f, 0.25f) },
		{ AttackType.NeutralAir,    new AttackData("neutral_air",    new Vector2(44, 36), new Vector2(12, 20),  2, 5, 5,  0.8f, 0.8f, new Vector2(150, -100), 0.60f, 0.15f) },
		{ AttackType.SideAir,       new AttackData("side_air",       new Vector2(32, 16), new Vector2(28, 24), 2, 7, 7,  0.4f, 0.6f, new Vector2(220, -120), 0.70f, 0.15f) },
		{ AttackType.UpAir,         new AttackData("up_air",         new Vector2(40, 30), new Vector2(0, -18), 3, 6, 7,  0.4f, 0.6f, new Vector2(40, -280),  0.70f, 0.15f) },
		{ AttackType.DownAir,       new AttackData("down_air",       new Vector2(40, 24), new Vector2(12, 28),  1, 6, 8, 0.1f, 0.3f, new Vector2(60, 250),  0.80f, 0.15f) },
	};

	protected Dictionary<AttackType, AttackData> Attacks;
	private AttackData currentAttack;
	protected virtual void ConfigureAttacks() { }

	private AnimatedSprite2D animatedSprite;
	private Area2D attackHitbox;
	private Sprite2D hitboxVisual;
	private CollisionShape2D hitboxShape;
	private RectangleShape2D hitboxRect;
	
	public int Health { get; private set; }
	private bool isAttacking = false;
	private readonly HashSet<Node> alreadyHit = new();
	private bool facingRight = true;
	private float attackBufferTimer = 0f;
	private StringName moveLeft, moveRight, jumpKey, attackKey, upKey, downKey, blockKey;
	private float hitstunTimer = 0f;
	private bool doubleJump = false;
	private float glideMultiplier = 0.1f;
	private const float BlockKnockbackMultiplier = 0.35f;
	private const float BlockWalkMultiplier = 0.35f;        // slow shuffle while guarding
	private static int hitstopGeneration;
	private static double previousTimeScale = 1.0;
	private bool isBlocking = false;
	private const float doubleTapWindow = 0.25f;
	private bool isRunning = false;
	private int runDir = 0;
	private int lastTapDir = 0;
	private double lastTapTime = -1.0;
	private float accelBonus = 200f;
	private Ability ability;
	private StringName abilityKey;
	private float abilityCharge = 0f;
	private Vector2 spriteBasePos;

	public bool FacingRight => facingRight;
	public void Face(bool right) => SetFacing(right);
	public Vector2 GetAimInput() => new Vector2(Input.GetAxis(moveLeft, moveRight), Input.GetAxis(upKey, downKey));
	public void SetTint(Color c) => animatedSprite.SelfModulate = c;
	public void PlayAnim(string anim) {
		if (animatedSprite.SpriteFrames.HasAnimation(anim)) animatedSprite.Play(anim);
	}
	
	public override void _Ready() {
		moveLeft = $"p{PlayerId}_move_left";
		moveRight = $"p{PlayerId}_move_right";
		jumpKey = $"p{PlayerId}_jump";
		attackKey = $"p{PlayerId}_attack";
		upKey = $"p{PlayerId}_up";
		downKey = $"p{PlayerId}_down";
		blockKey = $"p{PlayerId}_block";
		abilityKey = $"p{PlayerId}_ability";

		foreach (Node child in GetChildren()) {
			if (child is Ability a) { ability = a; a.Bind(this); }
		}

		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		attackHitbox = GetNode<Area2D>("AttackHitbox");
		hitboxShape = attackHitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		hitboxVisual = attackHitbox.GetNode<Sprite2D>("HitboxVisual");
		Health = MaxHealth;
		spriteBasePos = animatedSprite.Position;

		// Hitbox area
		hitboxRect = new RectangleShape2D();
		hitboxShape.Shape = hitboxRect;
		hitboxShape.Position = Vector2.Zero;
		hitboxVisual.Position = Vector2.Zero;
		hitboxVisual.Visible = false;

		attackHitbox.Monitoring = false;
		attackHitbox.AreaEntered += OnHitboxAreaEntered;
		animatedSprite.FrameChanged += OnFrameChanged;
		animatedSprite.AnimationFinished += OnAnimationFinished;

		Attacks = new Dictionary<AttackType, AttackData>();
		foreach (var kv in Defaults) Attacks[kv.Key] = kv.Value.Clone();
		ConfigureAttacks();
		currentAttack = Attacks[AttackType.NeutralAttack];

		SetFacing(StartFacingRight);
		animatedSprite.Play("idle");
	}
	
	public void SetSpriteScale(float s) {
		animatedSprite.Scale = new Vector2(s, s);
		animatedSprite.Position = spriteBasePos - new Vector2(0, (s - 1f) * FeetOffset);   // grow upward from the feet
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;

		// Gravity
		if (!IsOnFloor()) {
			bool isGliding = Input.IsActionPressed(jumpKey) && Velocity.Y > 0;
			float gravityMultiplier = isGliding? glideMultiplier : 1.0f;
			velocity += GetGravity() * gravityMultiplier * (float)delta;
		} else {
			doubleJump = true;
		}
		
		Velocity = velocity;

		// Match flow can freeze a fighter (round intro / KO) without removing it
		if (InputLocked) {
			Walk(Vector2.Zero, (float)delta);
			MoveAndSlide();
			return;
		}

		// right after the gravity block
		if (hitstunTimer > 0f) {
			hitstunTimer -= (float)delta;
			animatedSprite.Modulate = new Color(1f, 0.4f, 0.4f);
			if (hitstunTimer <= 0f) {
				animatedSprite.Modulate = Colors.White;
				animatedSprite.Play("idle");
			}
		}
		bool stunned = hitstunTimer > 0f;

		if (ability != null) {
			bool canStart = !stunned && !isAttacking && !isBlocking;
			if (Input.IsActionJustPressed(abilityKey)) {
				if (ability.Active) {
					ability.OnPressed();
				} else if (canStart && HasFullAbilityCharge()) {
					ConsumeAbilityCharge();
					ability.OnPressed();
				}
			}

			if (ability.Active) {
				ability.Tick((float)delta);
				MoveAndSlide();
				return;
			}
		}

		// Blocking
		Blocking(Input.IsActionPressed(blockKey) && IsOnFloor() && !isAttacking && !stunned);

		// Attack
		if (!stunned && !isBlocking && Input.IsActionJustPressed(attackKey) && !isAttacking) {
			attackBufferTimer = AttackBufferTime;
		}
		attackBufferTimer -= (float)delta;

		// Grounded Attacks
		if (!stunned && !isBlocking && attackBufferTimer > 0 && !isAttacking) {
			attackBufferTimer = 0;
			StartAttack();
		}

		if (Input.IsActionJustReleased(downKey)) {
			Position += new Vector2(0, 2);
		}

		// Horizontal movement
		if (stunned) {
			HitstunDrift((float)delta);
		} else {
			float direction = Input.GetAxis(moveLeft, moveRight);
			Vector2 dir = new Vector2(direction, 0);
			Walk(dir, (float)delta);
		}

		Run(stunned);

		// Jump
		if (!stunned) Jump();

		MoveAndSlide();
		if (IsOnFloor()) {
			if (isAttacking && currentAttack.Animation.EndsWith("_air")) {
				bool isActive = animatedSprite.Frame >= currentAttack.StartFrame
					&& animatedSprite.Frame <= currentAttack.EndFrame;

				if (!isActive) {
					CancelAttack();
					animatedSprite.Play("idle");
				}
			}

			if (!isAttacking && hitstunTimer <= 0f && !isBlocking && animatedSprite.Animation == "jump") {
				animatedSprite.Play("idle");
			}
		}
	}

	private void Walk(Vector2 dir, float delta) {
		Vector2 velocity = Velocity;

		float accel = Acceleration;
		float decel = Deceleration;
		float spd = isRunning? RunSpeed : Speed;

		if (isRunning) accel += accelBonus;

		if (isAttacking && IsOnFloor()) {
			accel *= AttackAccelMultiplier * currentAttack.Mobility;
			decel *= Mathf.Lerp(1f, AttackDecelMultiplier, currentAttack.Mobility);
		}

		if (isBlocking) spd *= BlockWalkMultiplier;

		if (dir.X != 0) {
			SetFacing(dir.X > 0);
			float rate = Mathf.Abs(velocity.X) > spd ? decel : accel;
			velocity.X = Mathf.MoveToward(velocity.X, dir.X * spd, rate * delta);
		} else {
			velocity.X = Mathf.MoveToward(velocity.X, 0, decel * delta);
		}


		if (IsOnFloor() && !isAttacking && !isBlocking && hitstunTimer <= 0f) {
			if (dir.X != 0) {
				string moveAnim = isRunning? "run" : "walk";
				if (animatedSprite.Animation != moveAnim) animatedSprite.Play(moveAnim);
			} else {
				if (animatedSprite.Animation != "idle") animatedSprite.Play("idle");
			}
		}

		Velocity = velocity;
	}

	private void Jump() {
		if (Input.IsActionJustPressed(jumpKey) && IsOnFloor()) {
			CancelAttack();
			Velocity = new Vector2(Velocity.X, JumpVelocity);
			animatedSprite.Play("jump");
		} else if (Input.IsActionJustPressed(jumpKey) && doubleJump) {
			CancelAttack();
			Velocity = new Vector2(Velocity.X, JumpVelocity);
			doubleJump = false;

			animatedSprite.SetFrameAndProgress(0, 0.0f);
			animatedSprite.Play("jump");
		}
		if (Input.IsActionJustReleased(jumpKey) && Velocity.Y < 0) {
			Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutMultiplier);
		}
	}

	private void Run(bool stunned) {
		if (stunned || isBlocking) { isRunning = false; return; }

		int tap = 0;
		if (Input.IsActionJustPressed(moveRight)) tap = 1;
		else if (Input.IsActionJustPressed(moveLeft)) tap = -1;

		if (tap != 0) {
			double now = Time.GetTicksMsec() / 1000.0;
			if (tap == lastTapDir && now - lastTapTime <= doubleTapWindow && IsOnFloor()) {
				isRunning = true;
				runDir = tap;
			}
			lastTapDir = tap;
			lastTapTime = now;
		}

		// Stop running when the direction is released or reversed
		float axis = Input.GetAxis(moveLeft, moveRight);
		int heldDir = Mathf.Abs(axis) > 0.1f ? (axis > 0 ? 1 : -1) : 0;
		if (heldDir != runDir) isRunning = false;
	}

	// Attack functions
	private AttackType ChooseAttack() {
		float direction = Input.GetAxis(moveLeft, moveRight);

		if (!IsOnFloor()) {
			if (Input.IsActionPressed(upKey)) return AttackType.UpAir;
			if (Input.IsActionPressed(downKey)) return AttackType.DownAir;
			if (Mathf.Abs(direction) > 0.5f) return AttackType.SideAir;
			return AttackType.NeutralAir;
		}

		if (Input.IsActionPressed(upKey)) return AttackType.UpAttack;
		if (Input.IsActionPressed(downKey)) return AttackType.DownAttack;
		if (Mathf.Abs(direction) > 0.5f) return AttackType.SideAttack;
		return AttackType.NeutralAttack;
	}

	private void StartAttack() {
		float direction = Input.GetAxis(moveLeft, moveRight);
		currentAttack = Attacks[ChooseAttack()];
		Velocity = new Vector2(Velocity.X * currentAttack.MomentumKeep, Velocity.Y);
		SetFacing(direction != 0 ? direction > 0 : facingRight);

		isAttacking = true;
		alreadyHit.Clear();

		// Shapes the hitbox
		hitboxRect.Size = currentAttack.Size;
		hitboxVisual.Scale = currentAttack.Size / hitboxVisual.Texture.GetSize();

		animatedSprite.Play(currentAttack.Animation);
	}

	private void EndAttack() {
		isAttacking = false;
		attackHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
		animatedSprite.Play("idle");
		hitboxVisual.Visible = false;
	}

	// Hitbox is only live on the damaging frames
	private void OnFrameChanged() {
		if (!isAttacking) return;
		bool active = animatedSprite.Frame >= currentAttack.StartFrame
				   && animatedSprite.Frame <= currentAttack.EndFrame;
		attackHitbox.SetDeferred(Area2D.PropertyName.Monitoring, active);
		hitboxVisual.Visible = active;
	}

	private void OnAnimationFinished() {
		if (isAttacking) {
			EndAttack();
		} else if (animatedSprite.Animation == "jump") {
			animatedSprite.SetFrameAndProgress(4, 1.0f);
			animatedSprite.Pause();
		}
	}

	private void OnHitboxAreaEntered(Area2D area) {
		Node target = area.GetParent();
		if (target == this || alreadyHit.Contains(target)) return; // one hit per swing
		alreadyHit.Add(target);

		if (target is Fighters victim) {
			Vector2 kb = currentAttack.Knockback;
			kb.X *= facingRight ? 1 : -1;
			victim.TakeHit(currentAttack.Damage, kb, currentAttack.Hitstun, currentAttack.Hitstop);
			AddAbilityCharge(ChargePerHit);
		} else if (target.HasMethod("TakeDamage")) {
			target.Call("TakeDamage", currentAttack.Damage);
		}
	}

	private void SetFacing(bool right) {
		if (isAttacking) return;
		
		facingRight = right;
		animatedSprite.FlipH = !right;

		Vector2 offset = currentAttack.Offset;
		attackHitbox.Position = new Vector2(right ? offset.X : -offset.X, offset.Y);
	}

	public void TakeDamage(int damage) => ApplyDamage(damage, true);   // still works for dummies

	private void ApplyDamage(int damage, bool flash) {
		if (Health <= 0) return;

		Health = Mathf.Max(Health - damage, 0);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
		GD.Print($"{Name} took {damage} damage ({Health}/{MaxHealth})");

		if (flash) {
			animatedSprite.Modulate = new Color(1f, 0.4f, 0.4f);
			CreateTween().TweenProperty(animatedSprite, "modulate", Colors.White, 0.15);
		}

		if (Health == 0) Die();
	}

	public void TakeHit(int damage, Vector2 knockback, float hitstun, float hitstop) {
		if (Health <= 0) return;

		if (ability != null) {
			if (ability.Invulnerable) return;   // dashing = can't be hit
			ability.Cancel();                   // hit while aiming interrupts it
		}

		if (isBlocking) {
			int chip = Mathf.Max(1, Mathf.RoundToInt(damage * BlockDamageMultiplier));
			Velocity = new Vector2(knockback.X * BlockKnockbackMultiplier, Velocity.Y);
			ApplyDamage(chip, false);
			return;                              // no hitstun, no interrupt
		}

		CancelAttack();
		attackBufferTimer = 0f;
		Velocity = knockback;
		hitstunTimer = hitstun;

		animatedSprite.Play("hurt");
		ApplyDamage(damage, true);
		Hitstop(hitstop);
	}

	private void CancelAttack() {
		if (!isAttacking) return;
		isAttacking = false;
		attackHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
		hitboxVisual.Visible = false;
	}
	
	private void HitstunDrift(float delta) {
		float friction = IsOnFloor() ? 800f : 100f;
		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, friction * delta), Velocity.Y);
	}

	private async void Hitstop(float duration) {
		int generation = ++hitstopGeneration;

		if (Engine.TimeScale > 0f) {
			previousTimeScale = Engine.TimeScale;
		}

		Engine.TimeScale = 0f;

		await ToSignal(GetTree().CreateTimer(duration, true, false, true),SceneTreeTimer.SignalName.Timeout);

		if (generation == hitstopGeneration) {
			Engine.TimeScale = previousTimeScale;
		}
	}

	private void Blocking(bool block) {
		if (block == isBlocking) return;
		isBlocking = block;

		if (block) {
			animatedSprite.Play("block");
		} else if (!block && animatedSprite.Animation == "block") {
			animatedSprite.Play("idle");
		}
	}

	public float AnimLength(string anim, float fallback) {
		SpriteFrames sf = animatedSprite.SpriteFrames;
		if (!sf.HasAnimation(anim)) return fallback;

		float total = 0f;
		for (int i = 0; i < sf.GetFrameCount(anim); i++) total += sf.GetFrameDuration(anim, i);
		return total / (float)sf.GetAnimationSpeed(anim);
	}

	private void AddAbilityCharge(float amount) {
		abilityCharge = Mathf.Clamp(
			abilityCharge + amount,
			0f,
			MaxAbilityCharge
		);

		EmitSignal(
			SignalName.AbilityChargeChanged,
			abilityCharge / MaxAbilityCharge
		);
	}

	private bool HasFullAbilityCharge() {
		return abilityCharge >= MaxAbilityCharge;
	}

	private void ConsumeAbilityCharge() {
		abilityCharge = 0f;

		EmitSignal(
			SignalName.AbilityChargeChanged,
			0f
		);

		EmitSignal(SignalName.AbilityUsed);
	}

	private void Die() {
		GD.Print($"{Name} was knocked out");

		// Stay in the tree (MatchController frees fighters between rounds) but stop fighting
		attackHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
		GetNode<Area2D>("Hurtbox").SetDeferred(Area2D.PropertyName.Monitorable, false);
		hitboxVisual.Visible = false;

		animatedSprite.FlipH = Velocity.X > 0;
		animatedSprite.Play("knockout");

		Velocity = new Vector2(1.0f, 0f);

		EmitSignal(SignalName.Died);
	}
}
