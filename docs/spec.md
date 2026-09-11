# Waterline — Spec

Source: `Waterline_GDD_Summary.md` (repo root), plus the Level Editor mockup
screenshots reviewed alongside it. This spec is the source of truth for
`/plan-level-editor` and `/plan-core-gameplay` — implementation details
(class names, framework choices) belong in those plans, not here.

## 1. One-line pitch

A grid-based pipe-connection puzzle ("Where's My Water?"-style): drag 2–3
colored pipe anchors across an empty board until every pipe meets at the same
cell, before an optional timer runs out.

## 2. Core loop

1. Level loads with a board of empty/obstacle cells and 2 or 3 pipe anchors
   (Pipe A, Pipe B, optional Pipe C), each starting with a fixed move budget.
2. Player drags any anchor into an adjacent empty cell, extending that pipe
   one cell per drag step (orthogonal only). Dragging back over the pipe's own
   last-placed cell retracts it, returning the spent move.
3. The timer (if the level has one) starts on the player's *first* move, not
   on level load.
4. Player repeats step 2 across any of the pipes, in any order, until either:
   - **Win** — every pipe occupies the same single cell simultaneously, and
     (if the level has any Valve) every Valve has been routed over.
   - **Lose** — the timer reaches the level's limit before all pipes meet.
5. On win: input locks, all pipe knots seal to a closed socket, a water-flow
   animation plays from each anchor to the meeting cell, the crocodile bathtub
   fills, and a "Level Cleared" overlay shows completion time. On lose: an
   "Out of Time" overlay offers an immediate retry from the original layout.

## 3. Entities & state

- **Board** — a `Columns` × `Rows` grid of cells, rendered at a fixed
  reference scale regardless of level size.
- **Cell** — one grid position; one of: empty ground, Rock, Boulder, Valve,
  +1 Pipe, or occupied by a pipe segment/anchor. A cell's obstacle type and
  its "claimed"/"open"/"pushed" runtime state are tracked separately (see
  below) — the board doesn't have a single fixed type per cell forever.
- **Pipe (anchor)** — one of `A`, `B`, optional `C`. Has: a starting cell, a
  move budget (remaining moves, shown as a white digit on its anchor flange),
  and an ordered list of cells it currently occupies (its laid path). Pipe C
  is not a special case — it follows the exact same rules as A/B.
- **Knot** — the visual tip of a pipe's current path end: half-open socket
  (flat cut facing travel direction) while unconnected, full closed circle
  once every pipe shares the meeting cell.
- **Obstacle — Rock**: permanently impassable; static for the whole attempt.
- **Obstacle — Boulder**: passable-by-push. Entering it shoves it one cell
  further in the pipe's travel direction if that next cell is free; if not
  free, the step is blocked instead. A pushed Boulder keeps its new position
  even if the push move is later retracted, so a Boulder's *current* cell can
  differ from its level-authored starting cell during an attempt.
  A cell occupied by another Boulder counts as "not free" — pushing into a
  Boulder-behind-a-Boulder blocks the step; there is no chain-push.
- **Obstacle — Valve**: passable; starts closed, flips to open (visually: a
  glowing droplet) the first time any pipe routes over it, and never closes
  again during the attempt. If a level contains one or more Valves, *all* of
  them must be open for the level to be won, in addition to the pipes
  meeting.
- **+1 Pipe cell** (level-authored): not an obstacle — never blocks movement.
  The first pipe to route through it is refunded that step and permanently
  gains +1 move ("claimed"). Visually gold+"+1" while unclaimed, dulled
  gray+checkmark once claimed. If the claiming pipe later retracts back
  through it, the claim and the bonus move are released and it reverts to
  unclaimed.
- **Meeting cell**: not a designer-authored/fixed cell — it's emergent,
  whichever single cell all pipes happen to end up sharing satisfies the win
  check. (Confirmed: the level content model below has no "goal cell" field.)
- **Timer**: per-level fail-condition clock, or absent ("Off"/untimed). Per
  the Level Editor mockup, an untimed level is still scored by elapsed time
  (for comparison/best-time purposes) even though it has no fail condition.
- **Boosters (player-held, not per-level authored)**:
  - *Time Freeze* — manually activated, pauses the countdown for a fixed
    duration; only relevant/visible on timed levels.
    > **OPEN QUESTION:** exact pause duration (GDD: "TBD").
    > **OPEN QUESTION:** charge economy — how the player acquires/spends
    > charges (GDD: "TBD", but explicitly "not unlimited").
  - *Placeable +1 Pipe* ("Pipe Extender", placeholder name) — player drops it
    on any empty, not-yet-drawn cell during an attempt; reuses the exact same
    claim/retract logic as the level-authored +1 Pipe.
    > **OPEN QUESTION:** final display name and charge economy (GDD: "TBD").

## 4. Player input & mechanics

- **Extend pipe**: drag an anchor/pipe-end into an orthogonally-adjacent
  empty cell. Costs 1 move from that pipe's remaining budget.
- **Retract pipe**: drag back over the pipe's own most-recently-placed cell;
  returns the spent move. (Retracting through a Boulder's push does not undo
  the push; retracting through a claimed +1 Pipe cell does release the claim.)
- **Movement legality**: orthogonal only (no diagonals); a pipe may never
  re-enter a cell it has already used; a pipe may never enter a cell held by
  another pipe *except* at the single shared meeting cell; a pipe may never
  enter a Rock cell; a pipe may enter a Boulder cell only if pushing it
  succeeds (see Boulder rule above); entering a Valve or +1 Pipe cell is
  always allowed (no extra move cost beyond the normal 1-move step, and the
  +1 Pipe step is itself refunded on first claim).
- **Activate Time Freeze**: explicit player button tap (not board input).
- **Place Placeable +1 Pipe**: player taps/drops it onto a chosen empty cell
  during an attempt.
- **Retry**: from the "Out of Time" overlay, resets the level to its original
  authored layout (all runtime state — pipe paths, boulder positions, valve
  states, +1 Pipe claims — reverts).

## 5. Win / lose / scoring conditions

- **Win**: every pipe (2 or 3, per level) simultaneously occupies the same
  cell, **and** every Valve on the board (if any) is open. Both conditions
  must hold together — there's no partial/sequential win.
- **Lose**: only fail condition is the timer reaching the level's limit
  before the win condition is met. There is no move-based fail condition
  (running out of a pipe's move budget just prevents further extension of
  that pipe, it isn't itself a loss — though this makes an un-winnable board
  possible if a pipe runs out of moves before reaching the meeting cell).
  > **OPEN QUESTION:** the GDD doesn't state what happens if a pipe's move
  > budget hits 0 before it can reach a shared cell — is there a stuck/soft-
  > lock detection, or does the player just retry manually?
- **Scoring**: no scoring system beyond completion time ("Piped in mm:ss" on
  win). Untimed levels still record elapsed time for comparison, per the
  editor mockup's stepper caption, even without a fail threshold.
- **Accessibility**: players with `prefers-reduced-motion` skip straight to
  the end-state on win (skip the water-flow animation).

## 6. Level / content model

Everything a designer authors per level, via the in-app Level Editor:

| Field | Description |
|---|---|
| Columns, Rows | Grid dimensions, each set via a bounded min/max stepper. |
| Time limit | Stepper; can be set to "Off" (untimed) or a specific duration. |
| Pipe A / Pipe B | Always present; each has a starting cell (via "Place A"/"Place B" tool) and a move-budget stepper. |
| Pipe C (optional) | Added/removed via a dedicated control; when present, has its own starting cell ("Place C") and length stepper. Removing it reverts the level to a plain two-pipe layout. |
| Obstacle cells | Any number of Rock / Boulder / Valve cells, painted via a tool palette (tap a tool, then tap board cells). |
| +1 Pipe cells | Any number, painted via its own tool (shares the obstacle palette visually but isn't an obstacle). |

Level Editor UI/flow (from the mockup and GDD "Level Editor" section):
- Dimension row (Columns/Rows steppers) and budget row (Pipe A/B/C length
  steppers, time-limit stepper, add/remove-third-anchor control) sit above a
  live board canvas that reflects edits immediately.
- A tool palette (Place A, Place B, Place C, Rock, Boulder, Valve, +1 Pipe)
  selects what tapping a board cell does; exactly one tool is active at a
  time.
- Levels are stored in a local "saved levels" list (name + save; browse/load
  existing ones).
- Export dumps all saved levels as copyable JSON; Import merges pasted JSON
  into the saved-levels list, reassigning IDs on collision.
- "Play this level" jumps straight into Play mode using the current in-editor
  layout, without requiring a save first.

## 7. Progression / difficulty

> **OPEN QUESTION:** the GDD describes only the Level Editor's local
> saved-levels list (design-time authoring/export/import) — it does not
> describe a player-facing level sequence, unlock gating, level-select
> screen, or any difficulty scaling/tiering between levels. Whether Waterline
> has a `LevelDatabase`-style sequential progression (as bubble-pond-
> association does) or some other structure needs explicit design input
> before the Level/Core Gameplay plans can address it.

## 8. Open questions

1. Boulder chain-pushing: does pushing into a cell occupied by another
   Boulder chain-push it, or count as blocked? (§3)
2. Time Freeze: exact pause duration. (§3)
3. Time Freeze: charge economy (how acquired/spent). (§3)
4. Placeable +1 Pipe ("Pipe Extender"): final name and charge economy. (§3)
5. Stuck-board handling: what happens if a pipe's move budget is exhausted
   before it can reach a shared cell — soft-lock detection, or manual retry
   only? (§5)
6. Player-facing progression/difficulty model: sequential level list,
   unlocking, or difficulty tiers — none of this is described in the GDD.
   (§7)
