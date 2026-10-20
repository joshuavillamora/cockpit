# Game Design

## Overview

| | |
|---|---|
| **Title** | Cockpit |
| **Genre** | 2D arena battler (platform fighter style) |
| **Players** | 2, local multiplayer on one keyboard |
| **Combat** | HP-based, with directional ground and aerial attacks |
| **Setting** | An enclosed Filipino backyard arena surrounded by spectators |
| **Look** | Cartoon pixel art at 640 × 360, with exaggerated chickens and readable attack poses |
| **Win condition** | Reduce the opponent's HP to zero |

## Screen flow

```
Loading ─► Main Menu ─► Character Select ─► Map Select ─► Match ─► Victory
(press     │ Play        (4 chickens left,    (2 arenas)    │        ├─ Rematch
 start)    │ Settings     4 right, VS,                      │        └─ Back to Menu
           │ Quit         picks in the middle)              ├─ Round intro ("ROUND N" / "FIGHT!")
           └─ Settings overlay (volume, fullscreen, Controls)  ├─ K.O. screen, then the next round
                                                            ├─ Deathmatch screen before the last round
                                                            └─ Pause (Esc/P): Resume, Settings, Quit
```

## Match rules

- A match is won by the first player to win **2 rounds**.
- If the score is 1-1, the last round is the **Deathmatch**.
- A double KO is a draw, and the round is replayed.
- The dots under the health bars show round wins in the current match.
- The number in the middle of the HUD is the **match score**: how many matches each player has won since the last visit to the menu. It keeps counting across rematches.

## Controls (default)

| Action | Player 1 | Player 2 |
|--------|----------|----------|
| Move left / right | `A` / `D` | `←` / `→` |
| Up / Down (attack direction, menus) | `W` / `S` | `↑` / `↓` |
| Jump | `K` or `Space` | `N` |
| Attack | `J` or `E` | `M` or `/` |
| Pause | `Esc` or `P` | `Esc` or `P` |
| Block / Air dash | planned | planned |
| Ability | planned | planned |

Hold a direction while pressing Attack to choose the attack: neutral, side, up or down, on the ground or in the air.

**Menus:** `W`/`S` or the arrow keys move, `Enter` or `Space` selects, `Esc` goes back. On character select each player uses their own keys (up/down to choose, attack to confirm, jump to cancel), then `Enter` goes on to map select. On map select either player moves with left/right and `Enter` starts the match. `F11` toggles fullscreen.

## Characters

| Chicken | Style | Ability (planned) |
|---------|-------|-------------------|
| **Banaba** | Master of aerial attacks, stays in the air longer, weaker on the ground | *Wak-a-Doodle-Doo*: aim at an area and slice through platforms and opponents |
| **Paraokan** | Tall and intimidating, slow, decent glider | *Chickbalang*: stretches into a controllable horse head with strong beak attacks |
| **Joloano** | Long legs, strong ground attacks, slow, good range | *Bernardo's Feet*: grows for a short time with stronger ground attacks and knockback |
| **Da Chik** | Small, fast, good wall mobility, lower health | *Zooooom*: dashes in any direction, keeping momentum; resets on landing |

Banaba and Joloano are playable now. Paraokan and Da Chik appear as "SOON" on character select.

## Moves

**Grounded:** neutral, side, up, down, running attack.
**Aerial:** neutral, side, up, down.
**Block:** reduces damage on the ground. In the air the same button performs a mini dash.
**Movement:** walk, run, jump, double jump, glide, wall slide, wall bounce.

See [Status and Roadmap](status-and-roadmap.md) for which of these exist today.

## Stage

An arena has a main floor, solid side walls, a ceiling, and one-way platforms that players can drop through. Spectators fill the background.
