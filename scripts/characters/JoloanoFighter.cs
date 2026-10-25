using Godot;

public partial class JoloanoFighter : Fighters {
	protected override void ConfigureAttacks() {
		Attacks[AttackType.SideAttack].Damage = 12;
		Attacks[AttackType.SideAttack].Knockback = new Vector2(260, -100);
		Attacks[AttackType.DownAir].Size = new Vector2(48, 28);
	}
}
