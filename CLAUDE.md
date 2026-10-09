# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

# Farm Fury: Rampage

Portrait mobile lane shooter ("gate runner") with a light farm-rebuilding meta. A herd of FarmFury animals (Cluck, Bessie, Horace, Ducky) holds a farm track against HARVEX robots, grows through maths gates, and rebuilds Fury Farm between runs. Crops planted on the farm appear as pickups on the next run's track.

- Design: `Docs/FarmFury Rampage — Game Design Document.docx` (GDD)
- Build plan with locked decisions: `Docs/FarmFury_Rampage_BuildPlan.md` — **§1 overrides the GDD where they differ** (launch = Worlds 1–3, uGUI only, no age gate, Supabase instead of Unity Cloud Save, "Endless Rampage", etc.)

## Status

Planning only (2026-10-09). No Unity project yet. Next: Phase 1 (plan §13) — create the Unity 6000.5.6f1 Universal 2D project **in this folder**, then the greybox `RunSim`.

## Stack

- Unity **6000.5.6f1** (same as Stampede), URP 2D 17.5, Linear colour space, portrait only
- Input System 1.20 only (no legacy input)
- Burst / Collections / Mathematics for the run simulation
- uGUI 2.5 + TextMeshPro for **all** UI, built in code (port Stampede's `UIKit`, `SafeAreaFitter`, overlay screens)
- LevelPlay 9.5 + AdMob, Unity IAP 5.4.2, UGS Analytics 6.3, UGS Remote Config — ported from Arcade
- Shared FarmFury Supabase backend (planned): identity, cloud save, receipt validation

## Architecture rules

- Folders: `Assets/_Project/Scripts/{Data,Core,Sim,Run,Farm,UI,Utilities,Editor}`, `Assets/_Project/ScriptableObjects/...`, `Assets/_Project/Tests/{EditMode,PlayMode}`.
- Namespaces `FarmFuryRampage.<Area>`, one assembly definition per area. Dependencies one way only: `Data ← Sim ← Run`, `Data ← Core ← Farm`, `UI` on top. **Run and Farm never reference each other**; crops reach the run through `RunLoadout`.
- `Sim` is pure C# (no MonoBehaviours, no `UnityEngine.Object` lookups): fixed 60 Hz tick, seeded RNG, event buffers read by the views.
- No singletons except the static `Services` locator; systems get dependencies via `Init(...)`. Ported Arcade/Stampede managers go *inside* service implementations.
- **No gameplay literals in code.** Every number comes from a ScriptableObject or Remote Config.
- Level content is authored only in track coordinates `(x, d)`; the herd stays fixed on screen and the world scrolls.

## Rules that must always hold

- **Hero balance:** all heroes deal 10 DPS per animal; the balance test (herd of 20 vs 5 reference waves) must keep every hero's time-to-clear within ±5%. Upgrades live on the shared Herd Level; skins are cosmetic only.
- **LevelDef validation:** an edit-mode test fails if any gate, spawn, pickup or hazard is outside track bounds (FarmFury's levels 2–6 broke on a coordinate bug).
- **Every player is child-directed:** set LevelPlay `is_child_directed` and `is_deviceid_optout` metadata before `Init()`, plus the dashboard toggle. Arithmetic parental gate before every purchase.
- Celebration videos are rendered with the background baked in — no chroma key.

## Testing

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results-edit.xml
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results-play.xml
```
Run both before each commit once the project exists. One feature per commit.

## Working with this repo

- Standalone repo, remote `origin` = `github.com/tenbucksmobile-png/farmfury_rampage`, branch `main`. Plain command-line git; **never** use Unity's create-project/link-to-GitHub flow (it makes a new default project and a differently named repo).
- The home directory is a separate shared git repo that ignores this folder; never run git commands for this project from the home directory.
- No Git LFS (same as Arcade and Stampede). Keep files under 100 MB; source art (PSD, Kling exports) stays outside the repo; never commit keystores, `.p8` keys or `.env` files.
- Build phases (plan §13) run one per session; start a phase only after the previous one is verified in the editor.
- Android: local Editor builds + USB/adb. iOS: Unity Build Automation — bump `buildNumber.iPhone` before every Cloud Build.

## Related FarmFury projects

- `Desktop/FarmFury_Arcade` — Pac-Man-style; source for Ads/IAP/Analytics managers, Cloud Build setup, web demo.
- `Desktop/FarmFury_Stampede` — platformer; source for `UIKit`, `SafeAreaFitter`, overlay screens, `ParentalGate`.
- `Desktop/FarmFury` — the original game (shelved).
