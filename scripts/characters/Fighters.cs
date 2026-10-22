using Godot;
using System;
using System.Collections.Generic;

public partial class Fighters : CharacterBody2D {
	public const float Speed = 225.0f;
	public const float Acceleration = 1000.0f;
	public const float Deceleration = 2500.0f;
	public const float JumpVelocity = -350.0f;
	public const float JumpCutMultiplier = 0.5f;

	public const float AttackAccelMultiplier = 0.1f;
	public const float AttackDecelMultiplier = 0.1f;
	private const float AttackBufferTime = 0.15f;

	[Export] public int PlayerId = 1;
	[Export] public int MaxHealth = 100;
	[Export] public bool StartFacingRight = true;
	[Export] public float BlockDamageMultiplier = 0.2f;

	// UI / match flow hooks (HUD and MatchController listen to these)
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();
	public bool InputLocked { get; set; }
	public bool IsDead => Health <= 0;

	// ---------- Attack data (numbers come from the frame data workbook) ----------

	private enum AttackType {
		NeutralAttack,
		SideAttack,
		UpAttack,
		DownAttack,
		NeutralAir,
		SideAir,
		UpAir,
		DownAir,
	}

	private class AttackData {
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

		public AttackData(string animation, Vector2 size, Vector2 offset, int startFrame, int endFrame, int damage, float mobility, float momentumKeep, Vector2 knockback, float hitstun) {
			Animation = animation; Size = size; Offset = offset;
			StartFrame = startFrame; EndFrame = endFrame;
			Damage = damage; Mobility = mobility; MomentumKeep = momentumKeep; Knockback = knockback; Hitstun = hitstun;
		}
	}

	private static readonly Dictionary<AttackType, AttackData> Attacks = new() {
		{ AttackType.NeutralAttack, new AttackData("neutral_attack", new Vector2(24, 16), new Vector2(36, 8),  1, 1, 4,  1.0f, 0.9f, new Vector2(200, -80),  0.50f) },
		{ AttackType.SideAttack,    new AttackData("side_attack",    new Vector2(52, 16), new Vector2(52, 12), 1, 1, 8,  0.6f, 0.7f, new Vector2(440, -240), 0.70f) },
		{ AttackType.UpAttack,      new AttackData("up_attack",      new Vector2(44, 32), new Vector2(0, -14), 1, 1, 7,  0.6f, 0.6f, new Vector2(80, -600),  0.80f) },
		{ AttackType.DownAttack,    new AttackData("down_attack",    new Vector2(28, 20), new Vector2(40, 22), 1, 1, 14, 0.2f, 0.3f, new Vector2(400, -300), 1.00f) },
		{ AttackType.NeutralAir,    new AttackData("neutral_air",    new Vector2(52, 44), new Vector2(0, 12),  1, 1, 5,  1.0f, 1.0f, new Vector2(280, -200), 0.60f) },
		{ AttackType.SideAir,       new AttackData("side_air",       new Vector2(36, 18), new Vector2(50, 10), 1, 1, 7,  0.6f, 0.8f, new Vector2(440, -240), 0.70f) },
		{ AttackType.UpAir,         new AttackData("up_air",         new Vector2(40, 30), new Vector2(0, -18), 1, 1, 6,  0.6f, 0.8f, new Vector2(80, -560),  0.70f) },
		{ AttackType.DownAir,       new AttackData("down_air",       new Vector2(40, 24), new Vector2(0, 40),  1, 1, 10, 0.2f, 0.5f, new Vector2(120, 500),  0.80f) },
	};

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
	private AttackData currentAttack = Attacks[AttackType.NeutralAttack];
	private StringName moveLeft, moveRight, jumpKey, attackKey, upKey, downKey;
	private float hitstunTimer = 0f;
	private bool doubleJump = false;
	private float glideMultiplier = 0.1f;
	private const float BlockKnockbackMultiplier = 0.35f;
	private const float BlockWalkMultiplier = 0.35f;        // slow shuffle while guarding

	private StringName blockKey;
	private bool isBlocking = false;
	
	
	public override void _Ready() {
		moveLeft = $"p{PlayerId}_move_left";
		moveRight = $"p{PlayerId}_move_right";
		jumpKey = $"p{PlayerId}_jump";
		attackKey = $"p{PlayerId}_attack";
		upKey = $"p{PlayerId}_up";
		downKey = $"p{PlayerId}_down";
		blockKey = $"p{PlayerId}_block";

		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		attackHitbox = GetNode<Area2D>("AttackHitbox");
		hitboxShape = attackHitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		hitboxVisual = attackHitbox.GetNode<Sprite2D>("HitboxVisual");
		Health = MaxHealth;

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

		SetFacing(StartFacingRight);
		animatedSprite.Play("idle");
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
			if (hitstunTimer <= 0f) animatedSprite.Play("idle");
		}
		bool stunned = hitstunTimer > 0f;

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

		// Jump
		if (!stunned) Jump();

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
		MoveAndSlide();
	}

	private void Walk(Vector2 dir, float delta) {
		Vector2 velocity = Velocity;

		float accel = Acceleration;
		float decel = Deceleration;
		float spd = Speed;

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

		Velocity = velocity;
	}

	private void Jump() {
		if (Input.IsActionJustPressed(jumpKey) && IsOnFloor()) {
			Velocity = new Vector2(Velocity.X, JumpVelocity);
		} else if (Input.IsActionJustPressed(jumpKey) && doubleJump) {
			Velocity = new Vector2(Velocity.X, JumpVelocity);
			doubleJump = false;
		}
		if (Input.IsActionJustReleased(jumpKey) && Velocity.Y < 0) {
			Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutMultiplier);
		}
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
		if (isAttacking) EndAttack();
	}

	private void OnHitboxAreaEntered(Area2D area) {
		Node target = area.GetParent();
		if (target == this || alreadyHit.Contains(target)) return; // one hit per swing
		alreadyHit.Add(target);

		if (target is Fighters victim) {
			Vector2 kb = currentAttack.Knockback;
			kb.X *= facingRight ? 1 : -1;
			victim.TakeHit(currentAttack.Damage, kb, currentAttack.Hitstun);
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

	public void TakeHit(int damage, Vector2 knockback, float hitstun) {
		if (Health <= 0) return;

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

		animatedSprite.Play("idle");
		ApplyDamage(damage, true);
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

	private void Blocking(bool block) {
		if (block == isBlocking) return;
		isBlocking = block;

		animatedSprite.SelfModulate = block ? new Color(0.6f, 0.8f, 1f) : Colors.White;

		if (block && animatedSprite.SpriteFrames.HasAnimation("block")) {
			animatedSprite.Play("block");
		} else if (!block && animatedSprite.Animation == "block") {
			animatedSprite.Play("idle");
		}
	}

	private void Die() {
		GD.Print($"{Name} was knocked out");

		// Stay in the tree (MatchController frees fighters between rounds) but stop fighting
		SetPhysicsProcess(false);
		attackHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
		GetNode<Area2D>("Hurtbox").SetDeferred(Area2D.PropertyName.Monitorable, false);
		GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		hitboxVisual.Visible = false;
		CreateTween().TweenProperty(animatedSprite, "modulate:a", 0.0f, 0.4f);

		EmitSignal(SignalName.Died);
	}
}
