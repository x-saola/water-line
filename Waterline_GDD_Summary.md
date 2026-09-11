# Waterline — GDD Summary

## Overview
**Waterline** is a grid-based pipe-connection puzzle game for mobile, styled in the vein of "Where's My Water?". Each level presents a dirt-textured board with two colored pipe anchors — Pipe A (teal) and Pipe B (orange) — and, on levels the designer opts into via the level editor, an optional third anchor, Pipe C (gold).

**Core concept:** The player drags each anchor across empty ground to lay pipe. All pipes on the level must meet at the same single cell before the timer runs out, without crossing rocks/boulders or their own or another pipe's path. On completion, water flows from every anchor to the meeting point and pours into an ornamental crocodile bathtub as a win celebration.

## Theme & Setting
The game has gone through three visual passes:
1. **Functional pass** — plain lines on a neutral grid (mechanic validation only).
2. **Casual pass** — bright rope-and-anchor motif.
3. **Current pass** — dirt/pipe re-theme: packed-dirt board, metal pipes with flange anchors, rock/boulder/valve obstacles, and a crocodile bathtub win prop.

The re-theme is purely cosmetic — it does not affect the core mechanic, move budgets, or obstacle rules.

## Core Gameplay Loop

### Anchors, Pipes & Retraction
- Every level has at least Pipe A and Pipe B, each with a fixed move budget shown on its anchor flange.
- An optional Pipe C follows the exact same rules as A and B — it is not a special case.
- The player drags an anchor into an adjacent empty cell to extend the pipe one cell at a time; dragging back over the pipe's own last cell retracts it.
- A pipe cannot re-enter a cell it has already used, and cannot enter a cell occupied by another pipe — except at the single shared meeting cell.
- Move budgets can change during play via the **+1 Pipe** booster (see Boosters section).

### Connection Feedback (Open & Closed Sockets)
- Each pipe's tip ("knot") renders as a **half-open socket** while unconnected, with the flat cut facing the direction of travel.
- Once every pipe occupies the meeting cell, all knots snap to a **fully closed circle**, cueing the win state just before the water animation plays.

### Obstacles
- **Rock** — permanently impassable cell (jagged visual).
- **Boulder** — passable by pushing: entering a Boulder cell shoves it one cell further in the pipe's direction of travel, provided that next cell is free; if that cell is not free, the step is blocked instead. A pushed Boulder stays at its new position even if the push move is later retracted.
- **Valve** — if any Valve exists on the board, it must be in the **open** state for the level to be won. A Valve opens automatically once a pipe routes over it, and stays closed otherwise.
- The **+1 Pipe** cell shares the obstacle palette but is not an obstacle — it never blocks movement.

### Timer & Fail State
- Each level has a time limit shown as a running clock, starting on the player's first move (not on level load).
- Reaching the time limit before all pipes meet triggers an "Out of Time" fail state, with an immediate retry option from the original layout.

## Movement & Connection Rules
- Pipes extend one cell per drag step, orthogonally only (no diagonals).
- Retracting removes the most recently placed segment and returns the spent move.
- No self-crossing; no entering a cell held by another pipe except at the shared meeting cell.
- No entering Rock cells. Entering a Boulder cell pushes it one cell further in the pipe's direction of travel if that cell is free; otherwise the step is blocked. A pushed Boulder remains at its new position even if the pushing move is retracted.
- Routing through a Valve opens it at no extra move cost. If the level contains any Valve, all Valves must be open for the level to be won.
- Routing through an unclaimed +1 Pipe cell refunds that step and grants a permanent extra move to the claiming pipe; retracting through it releases the claim and withdraws the bonus.
- The win condition scales to however many pipes a level actually has (2 or 3).

## Win / Lose Conditions
- **Win:** The instant every pipe on the level occupies the same cell simultaneously **and** every Valve on the board (if any) is open. Input locks, knots seal, a water-flow animation runs from each anchor to the meeting cell, the croc's bathtub fills, and a "Level Cleared" overlay shows completion time ("Piped in mm:ss"). Players with `prefers-reduced-motion` enabled skip straight to the end-state.
- **Lose:** Timer reaches the level's limit before all pipes meet. An "Out of Time" overlay appears with a "Try Again" action that resets the level.

## Boosters

### 1. +1 Pipe (level-authored, pre-shown)
- A level-editor-placed bonus cell that behaves like open ground except for a one-time reward: the first pipe to arrive permanently gains +1 move.
- First-come, one-time consumption — spent for the whole attempt once claimed.
- Claim is released (and the bonus withdrawn) if the claiming pipe retracts back through the cell.
- Visual: glows gold with "+1" when unclaimed; dulls to gray with a checkmark when claimed.

### 2. Time Freeze
- A player-held (not board-placed) booster, manually activated via a button tap.
- Pauses the countdown timer for a fixed duration (exact seconds TBD); all other rules (move budgets, +1 Pipe claims) continue normally.
- Only relevant on timed levels — hidden/disabled on untimed levels.
- Limited-charge consumable; exact economy is TBD, but intentionally not unlimited, since the timer is currently the game's only fail condition.

### 3. Placeable +1 Pipe
- A player-held consumable version of the +1 Pipe booster; the player chooses where to drop it (on any empty, not-yet-drawn cell) during an attempt.
- Reuses the exact same claim/retract logic as the level-authored +1 Pipe.
- Needs a distinct UI name from the level-authored version (placeholder: "Pipe Extender"). Final naming and charge economy TBD.

## UI Layout
| Element | Description |
|---|---|
| Top bar | Reset button (left), running clock + valve counter (center), edit-level button (right) |
| Board | Dirt-textured grid, rendered at a fixed reference scale regardless of level size |
| In-progress routing | Partially drawn pipes with live joint rendering; valves light up once routed through |
| Third anchor & boosters | Pipe C (when present) behaves exactly like A/B; +1 Pipe cells can appear on any level |
| Win/Lose overlays | Full-screen overlay: croc's bathtub + completion time on win, "Out of Time" + retry on loss |

## Audio Design
The current prototype has **no audio system**. The GDD sketches intended event hooks for a future pass (file names, exact triggers, and haptics are all TBD):

| Event | Description | Haptic |
|---|---|---|
| Pipe extend | Plays as a segment is laid | Yes (light) |
| Pipe retract | Plays as a segment is undone | Yes (light) |
| Valve open | Plays when a valve flips open | No |
| Level win | Plays as the water animation reaches the far anchor | No |
| Level lose | Plays when the timer runs out | No |
| Button/UI tap | General UI feedback | Yes (light) |
| Background music | Ambient loop during play | No |

## Level Editor
### Layout
- **Header** — back-to-play/cancel control.
- **Dimension row** — Columns/Rows steppers (bounded min/max).
- **Budget row** — Pipe A/B length steppers, Time limit stepper, Add third anchor (C) control (reveals Pipe C length stepper + remove control once added).
- **Board canvas** — live preview; tapping applies the currently selected tool.
- **Tool palette** — Place A / Place B / Place C, Rock, Boulder, Valve, +1 Pipe.
- **Save row** — name/save, or browse and load saved levels.
- **Export/Import row** — Export dumps all saved levels as copyable JSON; Import merges pasted JSON into the saved-levels list (reassigning IDs on collision).
- **Action row** — "Play this level" jumps straight into Play mode from the current in-editor layout without requiring a save.

### Key Controls
| Control | Function |
|---|---|
| Columns/Rows steppers | Set grid dimensions (re-scaled to a fixed reference size) |
| Pipe A/B length steppers | Set each anchor's move budget |
| Time limit stepper | Set the level's fail-timer duration |
| Add third anchor (C) | Adds Pipe C, its length stepper, and the Place C tool |
| Remove third anchor (×) | Removes Pipe C and reverts to a plain two-pipe layout |
| Place A/B/C | Move an anchor's starting position |
| Rock/Boulder/Valve | Toggle obstacle on a cell |
| +1 Pipe | Toggle a bonus-move cell |
| Save | Stores the level to the local saved-levels list |
| Export/Import levels | Serialize/deserialize saved levels as JSON |
| Play this level | Jump into Play mode with the current layout |

## Board Elements Reference
| Element | Rule |
|---|---|
| Rock | Permanently blocked; jagged visual; no pipe may ever enter |
| Boulder | Passable by pushing; rounded visual; entering the cell pushes it one cell further in the pipe's direction of travel if that cell is free, otherwise the step is blocked; stays at its new position even if the push is retracted |
| Valve (closed) | Passable; ring-and-spoke wheel; does not block movement; must be opened for the level to be won |
| Valve (open) | Same cell after a pipe has routed through it; glowing droplet; required (along with all other Valves) for the win condition |
| +1 Pipe (unclaimed) | Passable; glows gold with "+1"; grants +1 move to the first pipe to arrive |
| +1 Pipe (claimed) | Dulled gray with checkmark; reverts to unclaimed if the claiming pipe retracts through it |
| Anchor (A/B/C) | Bolted metal flange; white digit shows remaining move budget; C only exists on levels with a third anchor |
| Knot — closed socket | Full circle; shown once every pipe has met at the same cell |
| Knot — open socket | Half-open circle, flat cut facing travel direction; shown while unconnected |

## Key Design Notes
- The third anchor (Pipe C) is a **per-level opt-in**, not a difficulty tier — levels without it behave exactly like classic two-pipe levels.
- The dirt/pipe re-theme is cosmetic only; it does not touch the underlying mechanic, budgets, or obstacle rules.
- The croc's bathtub is a fixed win-state celebration prop, not a placeable per-level object; any future "deliver water to a character" system would need its own separate GDD scope.
- No scoring system beyond the completion timer — design intent is calm, tactile routing rather than twitch execution.
