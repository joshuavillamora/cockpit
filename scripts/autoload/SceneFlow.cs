using Godot;
using System.Threading.Tasks;

// Screen changes with a fade. Always go through GoTo() so transitions stay consistent.
public partial class SceneFlow : CanvasLayer {
	private const float FadeTime = 0.2f;

	public static SceneFlow Instance { get; private set; }

	private ColorRect fade;
	private bool busy;
	private bool switched;   // the scene has already been changed in the running transition
	private string target;
	private string queued;

	public override void _EnterTree() {
		Instance = this;
	}

	public override void _Ready() {
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		fade = new ColorRect {
			Color = new Color(0, 0, 0, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(fade);
	}

	// A request that arrives during a fade is not dropped (that would leave the caller's screen with
	// its input already switched off): while fading out it just changes the destination, and once
	// the new screen is up it is queued and runs when the fade-in ends.
	public async void GoTo(string path) {
		if (busy) {
			if (switched) queued = path;
			else target = path;
			return;
		}
		busy = true;
		switched = false;
		target = path;
		fade.MouseFilter = Control.MouseFilterEnum.Stop;

		await Fade(1f);
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(target);
		switched = true;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await Fade(0f);

		fade.MouseFilter = Control.MouseFilterEnum.Ignore;
		busy = false;

		if (queued != null) {
			string next = queued;
			queued = null;
			GoTo(next);
		}
	}

	private async Task Fade(float alpha) {
		Tween tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process);
		tween.TweenProperty(fade, "color:a", alpha, FadeTime);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
