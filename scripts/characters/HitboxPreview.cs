using Godot;

[Tool]
public partial class HitboxPreview : Node2D
{
	[Export] public Vector2 HitboxSize = new(24, 16);
	[Export] public Vector2 HitboxOffset = new(24, 8);

	public override void _Draw()
	{
		Rect2 rect = new Rect2(
			HitboxOffset - HitboxSize / 2,
			HitboxSize
		);

		DrawRect(rect, new Color(1, 0, 0, 0.25f), true);
		DrawRect(rect, Colors.Red, false);
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}
}
