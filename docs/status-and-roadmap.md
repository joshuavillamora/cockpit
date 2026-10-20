# Status and Roadmap

Competition submission: **October 25, 2026**. Feature freeze: **October 23**.

## What works today

- **Full screen flow:** loading, main menu, settings, controls, character select, map select, match, pause, victory with rematch.
- **Training lobby:** after map select, both players test movement and attacks on a dummy, then walk into the door to start the match.
- **Match flow:** rounds, K.O. and Deathmatch screens, double-KO draws, round dots and match score.
- **Characters:** Banaba and Joloano with walk, jump, eight attacks, HP, knockback and hitstun.
- **Stage:** floor, walls, and one-way platforms.
- **Settings:** volumes and fullscreen, saved between sessions.

## Placeholders

| Item | Notes |
|------|-------|
| UI art | The designer's wireframe skin: logo, buttons, panels, portraits, map thumbnails. Replace the PNGs in `assets/ui/` and keep the filenames. |
| Menu backgrounds | Flat sky and field colors (`scenes/ui/components/menu_background.tscn`). |
| Menu roosters | Plain boxes. |
| Second arena | Uses the first arena's background with a tint. Set `Background` in `resources/arenas/*.tres` when new arenas exist. |
| Paraokan, Da Chik | Shown as "SOON". They need sprites and scenes. |
| Ability icons on the HUD | Always show "ready" until abilities exist. |
| Lobby | The door and dummy are plain colored boxes. Tips cover movement and attacks only; add block and ability tips (`LobbyController.Tips`) once they exist. |

## Not built yet

- **Movement:** run, double jump, glide, wall slide, wall bounce, air dash.
- **Combat:** block, running attack.
- **Abilities:** all four.
- **Audio:** music, attack and hit sounds, menu sounds, Banaba's distance-based wakwak.
- **Effects:** hit flashes, feather bursts, landing dust, screen shake.
- **Feathery box outline** from the UI design (needs a transparent 9-slice PNG).
- **Controller support.**

## Open items before submission

- [ ] Replace placeholder art with final art as it arrives.
- [ ] Add block and ability tips to the lobby once those features exist.
- [ ] Add audio (original or properly licensed) and list its source.
- [ ] Confirm the license of the NES Cyrillic font.
- [ ] Fill in the asset and AI usage disclosure and the waiver.
- [ ] Two-player playtest of every matchup, then a bug-fix pass.
- [ ] Export a stable Windows build and test it on a second machine.
- [ ] Record the trailer (1–2 min) and the gameplay demo (3–5 min).

## Timeline

| Date | Goal |
|------|------|
| Oct 19–21 | Core build: make the game playable |
| Oct 22–23 | Integration, then feature freeze on Oct 23 |
| Oct 24 | Playtest and fix bugs |
| Oct 25 | Final verification and submission |
