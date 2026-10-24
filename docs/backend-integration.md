# Backend Integration

For the gameplay programmer. This page lists what the UI expects from the fighter code, and how to add content that the menus and match flow pick up automatically.

## What `Fighters.cs` must keep providing

The HUD and match flow depend on these members of `scripts/characters/Fighters.cs`:

| Member | Used by | Purpose |
|--------|---------|---------|
| `HealthChanged(int current, int max)` signal | `GameHud` | Drives the health bars. Emit it whenever health changes. |
| `Died` signal | `MatchController` | Ends the round. |
| `IsDead` (bool) | `MatchController` | Decides the winner, including double KOs. |
| `InputLocked` (bool) | `MatchController` | Freezes a fighter during the round intro and after a KO. |
| `StartFacingRight` (bool) | `MatchController` | Sets which way the fighter faces at the start of a round. |
| `PlayerId` (int) | `MatchController` | Picks the `p1_*` or `p2_*` input actions. Set before the fighter enters the scene tree. |

On death the fighter stays in the scene tree (it fades out and its collisions are disabled). `MatchController` frees it when the next round starts, so do not call `QueueFree()` in `Die()`.

## How a match uses your code

1. `MatchController` creates two fighters from the scenes chosen on the character select screen (`CharacterData.Scene`).
2. It sets `PlayerId`, `StartFacingRight` and `InputLocked = true`, then adds them at `SpawnP1` and `SpawnP2` in `game.tscn`.
3. After the "ROUND N / FIGHT!" intro it sets `InputLocked = false`.
4. When `Died` fires it waits 0.1 s (to catch a double KO), records the result in `GameManager`, then starts the next round or shows the victory screen.

## The lobby also uses your code

The training lobby (`LobbyController`) spawns the same fighters before the match, with `MaxHealth = 9999` so nobody is knocked out, and uses `PlayerId`, `StartFacingRight` and `InputLocked` the same way. It also has a training dummy that is not a `Fighters` node: keep the `HasMethod("TakeDamage")` branch in `OnHitboxAreaEntered`, because the dummy relies on it. When block and abilities exist, tell the frontend so the lobby tips can explain them.

## Ability meter on the HUD

Each player has an ability meter under their health bar. P1's meter fills left to right and P2's right to left; a full meter shows the gold fill and pulses. The HUD has no ability icon in the final design (`ui_ability_*` is imported but not shown).

**The ability key already exists:** `p1_ability` (default `L`) and `p2_ability` (default `.`), rebindable like every other key. Read it in `Fighters.cs` the same way as the other actions: `$"p{PlayerId}_ability"`.

**To connect your abilities to the HUD, add two signals to `Fighters` and emit them; nothing else is needed:**

```csharp
[Signal] public delegate void AbilityChargeChangedEventHandler(float ratio);   // 0 = empty, 1 = ready
[Signal] public delegate void AbilityUsedEventHandler();

EmitSignal(SignalName.AbilityChargeChanged, charge / chargeMax);   // whenever the charge or cooldown changes
EmitSignal(SignalName.AbilityUsed);                                 // when the ability fires
```

`GameHud.Bind` checks for these signals every round. While they don't exist, the meter stays full as a placeholder. Once they do, the meter starts empty and follows `AbilityChargeChanged`. The `ability_ready` sound plays when it fills and `ability_use` when `AbilityUsed` fires (both are silent until their files are added to `assets/audio/sfx/`). `GameHud.SetMeter(int player, float ratio)` still works if you'd rather drive the meter directly.

The HUD now uses y 0-76 of the screen (chicken names sit above the bars). Keep the stage ceiling, platforms and anything important below y 76.

## Sounds for moves (optional)

Hit, block and KO sounds already play from the HUD, so nothing is required. To add sounds for moves, call the audio autoload from `Fighters.cs`, one line each, for example:

```csharp
AudioManager.Instance?.PlaySfx("jump", 0.05f);     // in Jump(), where the jump velocity is set
AudioManager.Instance?.PlaySfx("swing", 0.1f);     // in StartAttack()
AudioManager.Instance?.PlaySfx("glide");           // when gliding starts
```

Then drop `jump.ogg`, `swing.ogg` and `glide.ogg` into `assets/audio/sfx/`. Until a file exists the call does nothing, so the line can go in first. List any new sound's source in `CREDITS.md`.

## Adding a chicken

1. Create the fighter scene: right-click `scenes/characters/fighter.tscn` → **New Inherited Scene**, save as `scenes/characters/<name>.tscn`.
2. Assign a `SpriteFrames` resource to `AnimatedSprite2D` using the animation names in the [art guidelines](art-guidelines.md).
3. Adjust the `CollisionShape2D` and `AttackHitbox` offsets to fit the sprite.
4. Create `resources/characters/<name>.tres` (New Resource → `CharacterData`) and set `DisplayName`, `Portrait` (64×64), `HudPortrait` (32×32), `Scene`, and `Playable = true`. Fill `AbilityName`, `AbilityText`, `PassiveName` and `PassiveText` for the character select info panel: each fits 20 characters (a name, then a one-line description). The ability icon fields are not used yet.
5. Add the path to `CharacterSelect.CharacterPaths` (`scripts/ui/CharacterSelect.cs`) and to `LoadingScreen.Preload` (`scripts/ui/LoadingScreen.cs`).

Chickens with `Playable = false` appear as "SOON" and cannot be picked.

## Adding an arena

1. Create `resources/arenas/<name>.tres` (New Resource → `ArenaData`) and set `DisplayName`, `Thumbnail`, `Background`, and optionally `BackgroundTint`.
2. Add the path to `MapSelect.ArenaPaths` (`scripts/ui/MapSelect.cs`) and to `LoadingScreen.Preload`.

Floor, walls and platforms are currently shared by all arenas and live in `scenes/gameplay/game.tscn`.

## Match rules and where to change them

| Rule | Where |
|------|-------|
| Rounds needed to win a match | `GameManager.RoundsToWin` (currently 2) |
| Deathmatch round | Played when both players are one win short (1-1 with the current setting) |
| Double KO | Counts as a draw; the round is replayed |
| Round wins (HUD dots) | `GameManager.Wins` |
| Match score (HUD center number) | `GameManager.MatchWins`, kept across rematches and cleared when returning to the menu |

## Coordination tips

- Keep edits to `fighter.tscn` and `game.tscn` small and announce them in the team chat. Scene files merge badly.
- If you rename a node that a UI script looks up (for example `HUD`, `Background`, `SpawnP1`, `VictoryOverlay`), update the script too.
