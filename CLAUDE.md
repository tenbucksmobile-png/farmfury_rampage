# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

# Farm Fury: Rampage

Portrait mobile lane shooter ("gate runner") with a light farm-rebuilding meta. A herd of FarmFury animals (Cluck, Bessie, Horace, Ducky) holds a farm track against HARVEX robots, grows through maths gates, and rebuilds Fury Farm between runs. Crops planted on the farm appear as pickups on the next run's track.

- Design: `Docs/FarmFury Rampage — Game Design Document.docx` (GDD)
- Build plan with locked decisions: `Docs/FarmFury_Rampage_BuildPlan.md` — **§1 overrides the GDD where they differ** (launch = Worlds 1–3, uGUI only, no age gate, Supabase instead of Unity Cloud Save, "Endless Rampage", etc.)

## Status

**Phase 1 greybox built (2026-10-09), revised the same day after playtest feedback — gap-free constant horde with a late armour ramp, Tiller Tank bosses inside it, Cluck's egg grenade (plan §1.4): 56 edit-mode + 1 play-mode tests passing in batch mode, and a greedy bot clears all three prototype levels (71 / 106 / 220 animals left; Proto 3 sits just under a cliff at 16x armour); not yet hand-playtested.** Next: plan §13 WP 1.3 — playtest in the editor and tune `GameTuning` / level assets (G1–G7 defaults), then the 2D crowd art test (WP 1.4). Phase 2 starts only after the Prototype gate (testers replay without being asked).

What exists:
- `Scripts/Data` — `GameTuning`, `HeroDef`, `RobotDef`, `LevelDef` (+ `GateRowDef`, `WaveDef`) and tuning structs.
- `Scripts/Sim` — `RunSim` (fixed 60 Hz, seeded, track coordinates): steering, herd count, phyllotaxis formation, staggered fire with damage scaling above the drawn cap / projectile budget; two attack patterns — `Straight` (first robot hit) and `Lob` (Cluck's egg grenade: aimed at the nearest robot that eggs already in the air won't kill, with lead + scatter, explodes and damages every robot in the blast; with no robot in range it is lobbed at the +/- gate in the herd's lane); gates (+ − × ÷, shoot-to-improve with cap and cooldown, red→blue flip), Buzz Drone swarms, a gap-free, constant-width Bolt Walker horde per level (`LevelDef.hordeStreams`; HP flat early then ramping, armoured walkers tinted red) with Tiller Tank bosses embedded in it (bigger robots clear their own space), bite, win at track end, fail at 0. `GateMath`, `Formation`, `RunConfigFactory`.
- `Scripts/Run` — `RunBootstrap`, `RunView` (draws assigned art, greybox shapes for anything missing; one GameObject per thing, Phase 2 moves herd/projectiles to instancing), `GreyboxHud` (uGUI legacy Text), `RunInputReader` (relative drag, A/D).
- **Drop-in art** (`Scripts/Editor/RampageArt.cs`, spec in `Assets/_Project/Art/README.md`): PNGs under `Assets/_Project/Art/` import as sprites (first import only, so manual tweaks stick) and are wired by folder + file name whenever anything there changes: `Heroes/<DisplayName>/run_*.png` + `egg.png` → `HeroDef.runFrames`/`projectileSprite`; `Robots/<DisplayNameNoSpaces>/walk_*.png` → `RobotDef.walkFrames`; `Track/ground.png`, `Effects/blast.png`, `Gates/add|subtract|multiply|divide.png` → `ScriptableObjects/Art/RunArt` (referenced by the Run scene). Menu: **FarmFury Rampage > Art**. The view sizes sprites in metres (`artScale`), so pixel size doesn't matter. Robot folders: `walk_*` = animation frames, any other file = a **variant** look (each robot gets one; static ones bob). `Effects/feather*` = puff on animal losses. `ArtPipelineTests` covers it. Dev tool: `RunCaptureTests` (play mode, [Explicit]) saves portrait screenshots — run without -nographics with `-testFilter RunCaptureTests`, output to `RAMPAGE_CAPTURE_DIR`. Kling source exports live outside the repo in `Desktop/FarmFury_Technical/FarmFury_Artwork/Rampage_Source/`.
- **Robot distances in level data are where the robot MEETS the herd.** Robots roll from the start of the run (`RunConfigFactory` converts meet → start distance; `RunSim` spawns them in the order they reach the horizon), so a packed horde stays packed on screen; row spacing is physical.
- `Scripts/Editor/RampageSetup` — menu **FarmFury Rampage > Setup > Run All** (project settings, prototype content, Run scene). Content is only created when missing, so Inspector tuning is never overwritten; **Reset Prototype Content** rewrites the hero, robots and levels from the script (in place, GUIDs kept; GameTuning untouched); **Rebuild Run Scene** overwrites the scene.
- Design changes from the GDD are listed in plan §1.4 (owner decisions): constant robot horde, Tiller Tank bosses inside it, Cluck's egg grenade.
- Content: `ScriptableObjects/Tuning/GameTuning`, `Heroes/Hero_Cluck`, `Robots/Robot_BuzzDrone`, `Robot_BoltWalker`, `Robot_TillerTank`, `Levels/Level_Proto01..03`. Scene: `Scenes/Run.unity` (only scene in the build).
- The project was created from Hub's **Universal 3D** template; it was converted to the URP **2D** renderer (`Settings/UniversalRP` + `Renderer2D`, copied from Arcade's untouched template versions) and the 3D template assets removed.

Play: open `Assets/_Project/Scenes/Run.unity`, set the Game view to a portrait phone resolution (e.g. 1080x2340), press Play. Drag or A/D to steer; R restart; N next level; tap/Space after a win or loss.

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

- **Hero balance:** all heroes deal 10 single-target DPS per animal; the balance test (herd of 20 vs 5 reference waves, which must include hordes) must keep every hero's time-to-clear within ±5%. Cluck's blast makes him far stronger against hordes, so the other heroes are balanced on time-to-clear, not DPS (plan §1.4). Upgrades live on the shared Herd Level; skins are cosmetic only.
- **LevelDef validation:** an edit-mode test fails if any gate, spawn, pickup or hazard is outside track bounds (FarmFury's levels 2–6 broke on a coordinate bug).
- **Every player is child-directed:** set LevelPlay `is_child_directed` and `is_deviceid_optout` metadata before `Init()`, plus the dashboard toggle. Arithmetic parental gate before every purchase.
- Celebration videos are rendered with the background baked in — no chroma key.

## Testing

Unity must be closed (batch mode can't open a project the editor has open). Editor: `C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe`.
```
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results-edit.xml -logFile edit.log
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults results-play.xml -logFile play.log
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FarmFuryRampage.Editor.RampageSetup.RunAllBatch -logFile setup.log
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FarmFuryRampage.Editor.RampageSetup.ResetPrototypeContentBatch -logFile reset.log
```
Write results/logs outside the repo (or delete them). Run both test platforms before each commit. One feature per commit.

Tests: `GateMathTests`, `FormationTests`, `RunSimTests` (incl. determinism and herd DPS = herd x hero DPS for 5..300 animals), `ContentValidationTests` (every LevelDef, incl. horde width, inside track bounds and level length; every HeroDef at the DPS target ±5%), `LobAndHordeTests` (blast radius, lead, gate lobbing, horde expansion), `LevelClearabilityTests` (greedy bot must win every level — run it after changing levels or tuning), `RunSceneSmokeTests` (play mode, fails on any logged error).

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
