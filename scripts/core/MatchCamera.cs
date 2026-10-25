using Godot;

// Match camera. On a 640x360 arena it stays still. On a bigger arena (The Pit is 1280x720) it follows
// the middle of the two fighters, at normal size while they are close, and zooms out smoothly as they
// move apart (down to the whole arena), then back in when they come together. It never shows outside the arena.
// The game normally draws at 640x360 and scales that picture up, so zooming out would throw away art pixels
// and look blurry. While a big arena is on screen the window draws 2D at its real resolution instead
// (canvas_items stretch), and switches back when the match ends.
public partial class MatchCamera : Camera2D {
	private static readonly Vector2 ScreenSize = new(640, 360);
	private const float Margin = 160f;        // room kept around the fighters, in world pixels
	private const float FollowSpeed = 6f;     // higher = snappier
	private const float LookUp = 40f;         // frame a little above the fighters (the floor fills the bottom)
	private const float ZoomDeadZone = 0.03f; // ignore tiny zoom changes so the picture doesn't shimmer

	private Vector2 arenaSize = ScreenSize;
	private Node2D[] targets = System.Array.Empty<Node2D>();
	private float minZoom = 1f;
	private float zoomGoal = 1f;
	private Window.ContentScaleModeEnum previousScaleMode;
	private bool sharpScaling;

	public void Setup(Vector2 size) {
		arenaSize = size;
		// Zoomed all the way out, the whole arena fits the screen
		minZoom = Mathf.Min(1f, Mathf.Min(ScreenSize.X / size.X, ScreenSize.Y / size.Y));
		Zoom = Vector2.One;
		Position = size / 2;
		PositionSmoothingEnabled = false;
		if (size != ScreenSize && !sharpScaling) {
			Window root = GetTree().Root;
			previousScaleMode = root.ContentScaleMode;
			root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
			sharpScaling = true;
		}
	}

	public override void _ExitTree() {
		if (sharpScaling) GetTree().Root.ContentScaleMode = previousScaleMode;
		sharpScaling = false;
	}

	public void Track(params Node2D[] fighters) {
		targets = fighters;
		if (arenaSize != ScreenSize) Position = Clamp(Target(), Zoom.X);
	}

	// Fighters move in the physics step, so the camera does too; following them at the screen's frame rate
	// made it judder a step behind them
	public override void _PhysicsProcess(double delta) {
		if (arenaSize == ScreenSize || targets.Length == 0) return;

		float wanted = WantedZoom();
		if (Mathf.Abs(wanted - zoomGoal) > ZoomDeadZone || wanted >= 1f || wanted <= minZoom) zoomGoal = wanted;
		float weight = 1f - Mathf.Exp(-FollowSpeed * (float)delta);
		float zoom = Mathf.Lerp(Zoom.X, zoomGoal, weight);
		Zoom = new Vector2(zoom, zoom);
		Position = Position.Lerp(Clamp(Target(), zoom), weight);
	}

	private Vector2 Target() {
		Vector2 sum = Vector2.Zero;
		int count = 0;
		foreach (Node2D target in targets) {
			if (!IsInstanceValid(target)) continue;
			sum += target.GlobalPosition;
			count++;
		}
		return count == 0 ? Position : sum / count - new Vector2(0, LookUp);
	}

	// Zoom 1 while both fighters fit with a margin; less (zoomed out) the further apart they are
	private float WantedZoom() {
		Rect2 box = new();
		bool first = true;
		foreach (Node2D target in targets) {
			if (!IsInstanceValid(target)) continue;
			box = first ? new Rect2(target.GlobalPosition, Vector2.Zero) : box.Expand(target.GlobalPosition);
			first = false;
		}
		Vector2 needed = box.Size + new Vector2(Margin * 2, Margin * 1.5f);
		float zoom = Mathf.Min(ScreenSize.X / needed.X, ScreenSize.Y / needed.Y);
		return Mathf.Clamp(zoom, minZoom, 1f);
	}

	// Keep the view inside the arena
	private Vector2 Clamp(Vector2 center, float zoom) {
		Vector2 half = ScreenSize / zoom / 2;
		return new Vector2(
			Mathf.Clamp(center.X, half.X, Mathf.Max(half.X, arenaSize.X - half.X)),
			Mathf.Clamp(center.Y, half.Y, Mathf.Max(half.Y, arenaSize.Y - half.Y)));
	}
}
