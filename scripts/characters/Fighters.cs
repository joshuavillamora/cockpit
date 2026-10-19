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

		public AttackData(string animation, Vector2 size, Vector2 offset,
						  int startFrame, int endFrame, int damage, float mobility) {
			Animation = animation; Size = size; Offset = offset;
			StartFrame = startFrame; EndFrame = endFrame;
			Damage = damage; Mobility = mobility;
		}
	}

	private static readonly Dictionary<AttackType, AttackData> Attacks = new() {
		{ AttackType.NeutralAttack, new AttackData("neutral_attack", new Vector2(24, 16), new Vector2(36, 8), 1, 1, 4, 1.0f) },
		{ AttackType.SideAttack, new AttackData("side_attack", new Vector2(34, 16), new Vector2(82, 12), 1, 1, 8, 0.6f) },
		{ AttackType.UpAttack, new AttackData("up_attack", new Vector2(44, 32), new Vector2(0, -14), 1, 1, 7, 0.6f) },
		{ AttackType.DownAttack, new AttackData("down_attack", new Vector2(56, 12), new Vector2(0, 32), 1, 1, 14, 0.2f) },
		{ AttackType.NeutralAir, new AttackData("neutral_air", new Vector2(52, 44), new Vector2(0, 12), 1, 1, 5, 1.0f) },
		{ AttackType.SideAir, new AttackData("side_air", new Vector2(36, 18), new Vector2(50, 10), 1, 1, 7, 0.6f) },
		{ AttackType.UpAir, new AttackData("up_air", new Vector2(40, 30), new Vector2(0, -18), 1, 1, 6, 0.6f) },
		{ AttackType.DownAir, new AttackData("down_air", new Vector2(40, 24), new Vector2(0, 40), 1, 1, 10, 0.2f) },
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
	
	public override void _Ready() {
		moveLeft = $"p{PlayerId}_move_left";
		moveRight = $"p{PlayerId}_move_right";
		jumpKey = $"p{PlayerId}_jump";
		attackKey = $"p{PlayerId}_attack";
		upKey = $"p{PlayerId}_up";
		downKey = $"p{PlayerId}_down";

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
			velocity += GetGravity() * (float)delta;
		}
		
		Velocity = velocity;

		// Match flow can freeze a fighter (round intro / KO) without removing it
		if (InputLocked) {
			Walk(Vector2.Zero, (float)delta);
			MoveAndSlide();
			return;
		}

		// Attack
		if (Input.IsActionJustPressed(attackKey) && !isAttacking) {
			attackBufferTimer = AttackBufferTime;
		}
		attackBufferTimer -= (float)delta;

		// Grounded Attacks
		if (attackBufferTimer > 0 && !isAttacking) {
			attackBufferTimer = 0;
			StartAttack();
		}

		// Jump
		Jump();

		// Horizontal movement
		float direction = Input.GetAxis(moveLeft, moveRight);
		Vector2 dir = new Vector2(direction, 0);

		Walk(dir, (float)delta);
		MoveAndSlide();
	}

	private void Walk(Vector2 dir, float delta) {
		Vector2 velocity = Velocity;

		float accel = Acceleration;
		float decel = Deceleration;
		if (isAttacking) {
			accel *= AttackAccelMultiplier * currentAttack.Mobility;
			decel *= Mathf.Lerp(1f, AttackDecelMultiplier, currentAttack.Mobility);
		}

		if (dir.X != 0) {
			SetFacing(dir.X > 0);
			velocity.X = Mathf.MoveToward(velocity.X, dir.X * Speed, accel * delta);
		} else {
			velocity.X = Mathf.MoveToward(velocity.X, 0, decel * delta);
		}

		Velocity = velocity;
	}

	private void Jump() {
		if (Input.IsActionJustPressed(jumpKey) && IsOnFloor()) {
			Velocity = new Vector2(Velocity.X, JumpVelocity);
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

		if (target.HasMethod("TakeDamage")) {
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

	public void TakeDamage(int damage) {
		if (Health <= 0) return;   // already knocked out

		Health = Mathf.Max(Health - damage, 0);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
		GD.Print($"{Name} took {damage} damage ({Health}/{MaxHealth})");

		// Quick red flash so you can see hits land
		animatedSprite.Modulate = new Color(1f, 0.4f, 0.4f);
		CreateTween().TweenProperty(animatedSprite, "modulate", Colors.White, 0.15);

		if (Health == 0) {
			Die();
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
