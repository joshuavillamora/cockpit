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

Each player has an ability meter under their health bar. `GameHud.SetMeter(int player, float ratio)` sets it: `player` is 0 for P1 and 1 for P2, `ratio` is 0 to 1. P1's meter fills left to right and P2's right to left. A full meter shows the gold fill and pulses (it swaps with the dark gold fill). It currently always shows full, so call `SetMeter` from your ability code once charge or cooldown exists. The HUD has no ability icon in the final design (`ui_ability_*` is imported but not shown).

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
