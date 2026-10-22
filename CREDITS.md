# Credits

## Music

| Track | Artist | Used for | File |
|---|---|---|---|
| I Want It All | Peachwood | Background music (menus, lobby, matches) | `assets/audio/music/main.mp3` |

> Before submission: record which service and licence this track was downloaded under (for example Epidemic Sound and the plan used), so the game's use of it is covered.

## Sound effects

**JDSherbert, Ultimate UI SFX Pack (FREE).** Licensed for commercial and non-commercial projects with credit to the author. [jdsherbert.itch.io](https://jdsherbert.itch.io/)

**Nathan Gibson, Universal UI Soundpack.** [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). [nathangibson.myportfolio.com](https://nathangibson.myportfolio.com)

**Rooster squawk** generated with [ElevenLabs](https://elevenlabs.io) Sound Effects.

**[SFXMint](https://sfxmint.com).** [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), no credit required (listed anyway): Solid Punch Impact 70 (`impact-punch-70`), Small Bird Wings Flapping 03 (`animal-wing-flap-03`), Soft Body Heavy Thud 04 (`impact-thud-04`), Flock Wingbeats 01 (`flock-wingbeats-01`).

The clips were trimmed, peak-normalized, pitch-shifted or layered in a few cases, and converted to Ogg. Each file in `assets/audio/sfx/` is named after its role in the game:

| File | Plays when | Source clip |
|---|---|---|
| `bakawk` | Loading finishes and PRESS START appears | ElevenLabs rooster squawk (whole call) |
| `menu_move` | Focus moves between buttons, slider steps | Nathan Gibson, Wood Block 1 |
| `menu_confirm` | A button is pressed | JDSherbert, Select 1 |
| `menu_back` | Esc / back, Quit, Back to menu | JDSherbert, Cancel 1 |
| `popup_open` / `popup_close` | Settings, Controls, Pause open and close | JDSherbert, Popup Open 1 / Popup Close 1 |
| `select_move` | Character and map select cursors | Nathan Gibson, Wood Block 2 |
| `ready` / `unready` | A player confirms / cancels their chicken | JDSherbert, Select 2 / Cancel 2 |
| `error` | Enter on character select before both players are ready | JDSherbert, Error 1 |
| `match_start` | Both players ready (character select), both in the lobby door | Nathan Gibson, Retro 10 |
| `tip` / `tips_toggle` | Lobby: next tip (Enter) / hide tips (Tab) | JDSherbert, Swipe 1 / Swipe 2 |
| `door` | A chicken walks into the lobby door | Nathan Gibson, Retro 3 |
| `round_start` | ROUND 1 / 2 / 3 banner | Nathan Gibson, Retro 8 |
| `deathmatch` | DEATHMATCH banner | Nathan Gibson, Retro 6 (lower pitch) |
| `fight` | FIGHT! banner | ElevenLabs rooster squawk (first squawk) |
| `hit` | A fighter or the lobby dummy takes a hit | SFXMint Solid Punch Impact 70, layered with Small Bird Wings Flapping 03 (feathers) |
| `block` | A blocked hit (chip damage) | SFXMint Soft Body Heavy Thud 04 |
| `ko` | KO banner | ElevenLabs rooster squawk (final crow, lower pitch), layered with SFXMint Flock Wingbeats 01 (feather burst) |
| `draw` | DRAW banner | JDSherbert, Error 1 (lower pitch) |
| `victory` | Victory screen | Nathan Gibson, Modern 16 |
