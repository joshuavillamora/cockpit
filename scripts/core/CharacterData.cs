using Godot;

// One playable (or upcoming) chicken. Edit the .tres files in resources/characters/.
[GlobalClass]
public partial class CharacterData : Resource {
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public Texture2D Portrait;
	[Export] public Texture2D AbilityReady;
	[Export] public Texture2D AbilityCooling;
	[Export] public Texture2D AbilityUnavailable;
	[Export] public PackedScene Scene;       // fighter scene to spawn in a match
	[Export] public bool Playable = true;    // false = shown as "SOON" on character select
}
