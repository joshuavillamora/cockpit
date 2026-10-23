# Cockpit

**A local two-player 2D arena brawler starring feisty Filipino native chickens.**

Pick your chicken, choose an arena, and fight a friend on the same keyboard. Land ground and aerial attacks, win rounds, and take the match. Cockpit is built with Godot 4 and C# in pixel art, as our entry to the **Level Up 3.0 Esports Game Dev Challenge** (Philippine Games and Sports theme).

> **Status:** work in progress. The complete game flow is playable with two chickens. Art is partly placeholder, and some moves and abilities are not built yet. See [Current status](#current-status).

## Contents

- [About the game](#about-the-game)
- [How to play](#how-to-play)
- [Controls](#controls)
- [Characters](#characters)
- [Game screens](#game-screens)
- [Run the game](#run-the-game)
- [Current status](#current-status)
- [Project layout](#project-layout)
- [Documentation](#documentation)
- [Built with](#built-with)
- [Team](#team)

## About the game

| | |
|---|---|
| **Genre** | 2D arena battler (platform fighter style) |
| **Players** | 2, local multiplayer on one keyboard |
| **Goal** | Win the match by winning rounds. A round is won by knocking out your opponent (reducing their HP to zero). |
| **Setting** | An enclosed Filipino backyard arena with spectators around it |
| **Look** | Cartoon pixel art at 640 × 360, with exaggerated chickens and readable attack poses |
| **Engine** | Godot 4.7 (.NET), written in C# |

The four fighters are inspired by real Philippine native chicken breeds. Each one has its own speed, size and attack style, plus a signature ability (planned).

## How to play

1. **Start:** press any key on the loading screen, then choose **Play** on the main menu.
2. **Choose your chicken:** each player picks from their own column of chickens and confirms. When both are ready, press `Enter`.
3. **Choose an arena:** pick a map and press `Enter` to start.
4. **Fight:** a match is **first to 2 round wins**.
   - Move, jump, and attack. Hold a direction while attacking to choose the attack: neutral, side, up or down, on the ground or in the air.
   - Reduce your opponent's health bar to zero to win the round. The health bars are at the top of the screen.
   - The dots under each health bar show how many rounds that player has won in this match.
   - If the score is 1-1, the last round is the **Deathmatch**.
   - If both fighters are knocked out at the same time, the round is a draw and is replayed.
5. **After the match:** choose **Rematch** to play again with the same chickens, or **Back to Menu**. The number in the middle of the HUD is the **match score**, counting matches each player has won since the last visit to the menu, so it keeps growing across rematches.

Press `Esc` or `P` at any time during a match to pause.

## Controls

| Action | Player 1 | Player 2 |
|--------|----------|----------|
| Move left / right | `A` / `D` | `←` / `→` |
| Aim attack up / down | `W` / `S` | `↑` / `↓` |
| Jump | `K` or `Space` | `N` |
| Attack | `J` or `E` | `M` or `/` |
| Block (on the ground) | `H` | `,` |
| Pause | `Esc` or `P` | `Esc` or `P` |
| Air dash | coming soon | coming soon |
| Ability | coming soon | coming soon |

**Menus:** `W`/`S` or the arrow keys move, `Enter` or `Space` selects, `Esc` goes back.
**Character select:** each player uses their own keys. Up/down chooses, Attack confirms, Jump cancels. Press `Enter` when both are ready.
**Map select:** either player moves with left/right, and `Enter` continues to the lobby.
**Lobby:** test movement and attacks, and hit the training dummy. Walk both chickens into the door to start the match. `Enter` shows the next tip, `Tab` hides the tips, `Esc` goes back to map select.
**In the air:** press Jump again to double jump, hold Jump while falling to glide.
**Fullscreen:** `F11`. The same controls list is available in-game under **Settings → Controls**.

## Characters

| Chicken | Style | Signature ability (planned) |
|---------|-------|-----------------------------|
| **Banaba** | Master of aerial attacks, stays in the air longer, weaker on the ground | *Wak-a-Doodle-Doo*: aim at an area and slice through platforms and opponents |
| **Paraokan** | Tall and intimidating, slow, decent glider | *Chickbalang*: stretches into a controllable horse head with strong beak attacks |
| **Joloano** | Long legs, strong ground attacks, slow, good range | *Bernardo's Feet*: grows for a short time with stronger ground attacks and knockback |
| **Da Chik** | Small, fast, good wall mobility, lower health | *Zooooom*: dashes in any direction, keeping momentum; resets on landing |

**Playable now:** Banaba and Joloano. Paraokan and Da Chik appear as "SOON" on the character select screen.

## Game screens

```
Loading ─► Main Menu ─► Character Select ─► Map Select ─► Lobby ─► Match ─► Victory
```

- **Loading:** preloads the game, then "PRESS START".
- **Main Menu:** Play, Settings (volume, fullscreen, Controls) and Quit.
- **Character Select:** 4 chickens on each side; each player's pick is shown large in the middle.
- **Map Select:** choose the arena.
- **Lobby:** a training room. Test movement and attacks, hit the dummy, then walk both players into the door to start the match.
- **Match:** round intro, K.O. screen, a Deathmatch screen before the last round, and Pause (Esc / P).
- **Victory:** Rematch (skips the lobby) or Back to Menu.

## Run the game

### What you need

- [Godot Engine, **.NET** edition](https://godotengine.org/download) 4.7.x (the standard build cannot open this project)
- [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or newer

### Steps

1. Clone this repository.
2. Open Godot (.NET), choose **Import**, and select `project.godot`.
3. Press **Build** (the hammer icon, top right) once so the C# code compiles.
4. Press **F5** to run. The game starts fullscreen; press `F11` for windowed mode.

To jump straight into a match while developing, open `scenes/gameplay/game.tscn` and press **F6**.

## Current status

**Working today**
- Full screen flow with rounds, K.O. and Deathmatch screens, pause, rematch and match score
- Two playable chickens with walk, jump, eight attacks, health, knockback and hitstun
- One-way platforms, two arenas, and saved volume and fullscreen settings

**Placeholder or not built yet**
- Final art: the UI and menus use a placeholder skin, and the second arena is the first with a tint
- Audio: background music, menu and banner sounds, and hit / block / KO sounds. Jump, attack swing and glide are still silent. Sources are listed in [CREDITS.md](CREDITS.md).
- Moves: run, double jump, glide, wall moves, air dash, block, and the four abilities
- Paraokan and Da Chik, controller support, and a tutorial screen

Deadlines: feature freeze **October 23, 2026**, submission **October 25, 2026**. The full list is in [Status and Roadmap](docs/status-and-roadmap.md).

## Project layout

```
assets/       Art, fonts and audio from the design team
resources/    Data files: UI theme, chickens, arenas
scenes/       Screen and gameplay layouts (Godot scenes)
scripts/      C# code: autoload systems, UI screens, match flow, fighters
docs/         Extra documentation for the team
project.godot  Godot project settings and input map
```

## Documentation

This README covers the game. The team documents live in [`docs/`](docs/README.md):

| Document | For |
|----------|-----|
| [Game Design](docs/game-design.md) | Full design notes: moves, stage, rules |
| [Contributing](docs/contributing.md) | Setup, branches, commit style |
| [Art Guidelines](docs/art-guidelines.md) | Sizes, naming and folders for artists |
| [Project Structure](docs/project-structure.md) | Where files live and what to edit |
| [Frontend Guide](docs/frontend-guide.md) | How the UI code works |
| [Backend Integration](docs/backend-integration.md) | What the UI needs from the gameplay code |
| [Status and Roadmap](docs/status-and-roadmap.md) | Progress and what is next |

## Built with

- [Godot Engine](https://godotengine.org/) 4.7 (.NET) and C#
- Pixel art and UI by the Cockpit design team
- NES Cyrillic font for in-game text

## Credits

Music: "I Want It All" by Peachwood. UI sounds: JDSherbert (Ultimate UI SFX Pack) and Nathan Gibson (Universal UI Soundpack, CC BY 4.0). Rooster sounds generated with ElevenLabs. Hit, block and feather sounds from SFXMint (CC0). Full list in [CREDITS.md](CREDITS.md).

## Team

| Role | Area |
|------|------|
| Character artist | Chickens and animations |
| Environment artist | Arenas, platforms, crowd |
| UI / VFX artist | HUD, menus, icons, effects |
| Backend developer | Gameplay: movement, combat, abilities |
| Frontend developer | Asset integration, UI, screen flow |
