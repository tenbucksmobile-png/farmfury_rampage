# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

# Farm Fury: Rampage

Portrait mobile lane shooter ("gate runner") with a light farm-rebuilding meta. A herd of FarmFury animals (Cluck, Bessie, Horace, Ducky) holds a farm track against HARVEX robots, grows through maths gates, and rebuilds Fury Farm between runs. Crops planted on the farm appear as pickups on the next run's track.

- Design: `Docs/FarmFury Rampage — Game Design Document.docx` (GDD)
- Build plan with locked decisions: `Docs/FarmFury_Rampage_BuildPlan.md` — **§1 overrides the GDD where they differ** (launch = Worlds 1–3, uGUI only, no age gate, Supabase instead of Unity Cloud Save, "Endless Rampage", etc.). **§1.4 lists the owner's design changes** (constant robot horde, Tiller Tank bosses inside it, Cluck's egg grenade).
- Art prompts for every asset: `Docs/Kling_Art_Prompts.md`.

## Status (2026-10-09)

**Phase 1 (Prototype) in progress.** Greybox run is built, owner-playtested and revised (feedback so far: hordes should be one constant gap-free mass; boss robots inside it; "almost too easy" → late armour ramp; "it looks good"). First real art is wired in. 56 edit-mode + 1 play-mode tests pass in batch mode; a greedy bot clears all three prototype levels (71 / 106 / 220 animals left; Proto 3 sits just under a cliff — 16x armour loses).

Next:
1. Owner plays the art build and judges feel/size (chickens may still read small — `Hero_Cluck.artScale`, currently 3).
2. Gate art (prompts rewritten in `Docs/Kling_Art_Prompts.md` A8 after Kling rendered a photo barn door with a drinking glass), then Buzz Drone / Tiller Tank animation if wanted.
3. Close plan §13 WP 1.3 (tune G1–G7 in data) and WP 1.4 (2D crowd readability — effectively under way with the real Cluck art).
4. Phase 2 only after the Prototype gate (testers replay without being asked). Known Phase 2 balance item: the armour ramp has cliffs (big herds one-shot until armour crosses the egg damage), smooth it with the economy simulator.

Art in the build (`Assets/_Project/Art/`): Cluck run loop (8 frames from a Kling video) + egg; Bolt Walker ×3 variants, Buzz Drone ×2 variants, Tiller Tank ×1; ground; blast; feathers. **Missing: gates (greybox), animated robots.** Kept for later heroes: Milk1, WaterSpout, horseshoe, Percy_effect, `Heroes/Ducky/Ducky_back.png`.

## What exists

- `Scripts/Data` — `GameTuning`, `HeroDef` (stats + art: runFrames, projectileSprite, artScale, frameRate), `RobotDef` (stats + art: walkFrames, variants, artScale, frameRate), `LevelDef` (+ `GateRowDef`, `WaveDef`, `HordeStreamDef`), `RunArt` (ground, blast, feathers, gate frames), tuning structs.
- `Scripts/Sim` — `RunSim` (fixed 60 Hz, seeded, track coordinates): steering, herd count, phyllotaxis formation, staggered fire with damage scaling above the drawn cap / projectile budget; attack patterns `Straight` (first robot hit) and `Lob` (Cluck's egg grenade: aimed at the nearest robot that eggs already in the air won't kill, with lead + scatter, explodes and damages every robot in the blast; with no robot in range it is lobbed at the +/- gate in the herd's lane; eggs are held while nothing is in range, throw timings golden-ratio staggered); gates (+ − × ÷, shoot-to-improve with cap and cooldown, red→blue flip); Buzz Drone swarms; one gap-free, constant-width Bolt Walker horde per level (`LevelDef.hordeStreams`, HP flat early then ramping) with Tiller Tank bosses embedded (bigger robots clear their own space); bite; win at track end, fail at 0. `GateMath`, `Formation`, `RunConfigFactory`.
- **Robot distances in level data are where the robot MEETS the herd.** Robots roll from the start of the run (`RunConfigFactory` converts meet → start distance with `(pace + speed) / pace`; `RunSim.ScheduleSpawns` spawns them in the order they reach the horizon), so a packed horde stays packed on screen; row spacing is physical. (Spawning robots only when in range made rows drift apart to ~1.5 m.)
- `Scripts/Run` — `RunBootstrap`, `RunView` (draws assigned art, greybox shapes for anything missing; animation frames, variants with a rolling bob, armour tint, front-to-back overlap via depth; one GameObject per thing — Phase 2 moves herd/projectiles to instancing), `GreyboxHud` (uGUI legacy Text), `RunInputReader` (relative drag, A/D).
- `Scripts/Editor/RampageSetup` — menu **FarmFury Rampage > Setup > Run All** (project settings, prototype content, Run scene, art folders + RunArt wiring). Content is only created when missing, so Inspector tuning is never overwritten; **Reset Prototype Content** rewrites hero, robots and levels from the script (in place, GUIDs kept; GameTuning and art fields untouched); **Rebuild Run Scene** overwrites the scene.
- `Scripts/Editor/RampageArt` — drop-in art (below). Menu **FarmFury Rampage > Art**.
- Content: `ScriptableObjects/Tuning/GameTuning`, `Heroes/Hero_Cluck`, `Robots/Robot_BuzzDrone`, `Robot_BoltWalker`, `Robot_TillerTank`, `Levels/Level_Proto01..03`, `Art/RunArt`. Scene: `Scenes/Run.unity` (only scene in the build).
- The project was created from Hub's **Universal 3D** template and converted to the URP **2D** renderer (`Settings/UniversalRP` + `Renderer2D`, copied from Arcade's untouched template versions).

Play: open `Assets/_Project/Scenes/Run.unity`, set the Game view to a portrait phone resolution (e.g. 1080x2340), press Play. Drag or A/D to steer; R restart; N next level; tap/Space after a win or loss.

## Art pipeline

- **Drop-in:** PNGs under `Assets/_Project/Art/` are imported as sprites (first import only, so manual tweaks stick) and wired by folder + file name whenever anything there changes. Spec: `Assets/_Project/Art/README.md`.
  - `Heroes/<DisplayName>/run_*.png` + `egg.png` → `HeroDef.runFrames` / `projectileSprite`
  - `Robots/<DisplayNameNoSpaces>/walk_*.png` → `RobotDef.walkFrames`; **any other file = a variant look** (each robot gets one; static ones bob)
  - `Track/ground.png`, `Effects/blast.png`, `Effects/feather*.png`, `Gates/add|subtract|multiply|divide.png` → `RunArt`
  - Sprites are sized in metres (`artScale`), so pixel size doesn't matter, but transparent margins do — trim them.
- **Camera angles:** heroes are seen **from behind** (running up the screen), robots **from the front**. Look: glossy 3D cartoon heroes (Cluck reference: `FarmFury_Artwork/Rough_Characters/Cluck/Cluck_back.png`).
- **Tools (`Tools/art/`, run with the user's Python, without `-I`, since PIL lives in user site-packages):**
  - `kling_run_loop.py <video.mp4> <out_dir> <debug.png>` — Kling image-to-video → 8 keyed, drift-free loop frames. Keys by saturation (fine for colourful characters on Kling's grey background).
  - `trim_sprites.py <Art folder>` — crops transparent margins (frame sets share one box).
  - `seamless_ground.py <in.png> <ground.png>` — top-to-bottom seamless tile at 1024 wide.
  - Video frames: ffmpeg comes with `imageio_ffmpeg` (no system ffmpeg on this machine).
- **Kling source exports stay out of the repo:** `Desktop/FarmFury_Technical/FarmFury_Artwork/Rampage_Source/`.
- **Kling prompting lesson:** describe the whole game object and its job, say "cartoon mobile-game asset", and negate what it might invent ("glass pane" produced a drinking glass on a shelf).

## Stack

- Unity **6000.5.6f1** (same as Stampede), URP 2D 17.5, Linear colour space, portrait only
- Input System 1.20 only (no legacy input)
- Burst / Collections / Mathematics for the run simulation
- uGUI 2.5 + TextMeshPro for **all** UI, built in code (port Stampede's `UIKit`, `SafeAreaFitter`, overlay screens)
- LevelPlay 9.5 + AdMob, Unity IAP 5.4.2, UGS Analytics 6.3, UGS Remote Config — ported from Arcade
- Shared FarmFury Supabase backend (planned): identity, cloud save, receipt validation

## Architecture rules

- Folders: `Assets/_Project/Scripts/{Data,Core,Sim,Run,Farm,UI,Utilities,Editor}`, `Assets/_Project/ScriptableObjects/...`, `Assets/_Project/Art/...`, `Assets/_Project/Tests/{EditMode,PlayMode}`; non-Unity tools in `Tools/`.
- Namespaces `FarmFuryRampage.<Area>`, one assembly definition per area. Dependencies one way only: `Data ← Sim ← Run`, `Data ← Core ← Farm`, `UI` on top. **Run and Farm never reference each other**; crops reach the run through `RunLoadout`.
- `Sim` is pure C# (no MonoBehaviours, no `UnityEngine.Object` lookups): fixed 60 Hz tick, seeded RNG, event buffers read by the views.
- No singletons except the static `Services` locator; systems get dependencies via `Init(...)`. Ported Arcade/Stampede managers go *inside* service implementations.
- **No gameplay literals in code.** Every number comes from a ScriptableObject or Remote Config (view-only visual constants in `RunView` are the exception).
- Level content is authored only in track coordinates `(x, d)`; the herd stays fixed on screen and the world scrolls.

## Rules that must always hold

- **Hero balance:** all heroes deal 10 single-target DPS per animal; the balance test (herd of 20 vs 5 reference waves, which must include hordes) must keep every hero's time-to-clear within ±5%. Cluck's blast makes him far stronger against hordes, so the other heroes are balanced on time-to-clear, not DPS (plan §1.4). Upgrades live on the shared Herd Level; skins are cosmetic only.
- **LevelDef validation:** an edit-mode test fails if any gate, spawn, pickup or hazard is outside track bounds (FarmFury's levels 2–6 broke on a coordinate bug).
- **Every level must be clearable by the greedy bot** (`LevelClearabilityTests`); re-run it after any level or tuning change. Pick numbers with a temporary sweep test (sim runs in milliseconds) rather than guessing, then delete the sweep.
- **Every player is child-directed:** set LevelPlay `is_child_directed` and `is_deviceid_optout` metadata before `Init()`, plus the dashboard toggle. Arithmetic parental gate before every purchase.
- Celebration videos are rendered with the background baked in — no chroma key.

## Testing

Unity must be closed (batch mode can't open a project the editor has open — check with `tasklist | grep -i "^Unity.exe"`). Editor: `C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe`.
```
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results-edit.xml -logFile edit.log
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults results-play.xml -logFile play.log
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FarmFuryRampage.Editor.RampageSetup.RunAllBatch -logFile setup.log
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FarmFuryRampage.Editor.RampageSetup.ResetPrototypeContentBatch -logFile reset.log
RAMPAGE_CAPTURE_DIR=<dir> Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter RunCaptureTests -testResults cap.xml -logFile cap.log
```
The last one (no `-nographics`) saves portrait screenshots at 6/14/24 s — use it to check art and visuals without the editor. Write results/logs outside the repo. Run both test platforms before each commit. One feature per commit.

Tests: `GateMathTests`, `FormationTests`, `RunSimTests` (determinism, herd DPS = herd x hero DPS for 5..300 animals, packed rows keep their spacing, authored distance = meeting point), `LobAndHordeTests` (blast radius, lead, gate lobbing, horde/stream expansion, armour ramp, boss clears its space), `ContentValidationTests` (LevelDefs inside track bounds and level length incl. horde width; HeroDefs at the DPS target ±5%), `LevelClearabilityTests` (greedy bot wins every level), `ArtPipelineTests` (dropped PNGs import as sprites and get wired), `RunSceneSmokeTests` (play mode, fails on any logged error), `RunCaptureTests` (play mode, `[Explicit]` dev tool).

## Gotchas (learned the hard way)

- Batch entry points must **not** call `EditorApplication.Exit` — it crashed Unity's shutdown; pass `-quit` instead.
- `EditorSceneManager.OpenScene(..., Single)` unloads assets nothing references yet: load assets *after* opening the scene, and `MarkSceneDirty` before `SaveScene` or the save is skipped.
- `using System;` next to `[Range]` is ambiguous (`System.Range`); write `System.Array.Empty<T>()` instead of importing `System`.
- `WaitForEndOfFrame` never fires in batch-mode play tests; render cameras manually.
- Unity Hub's "link to GitHub" option creates a nested project and a stray GitHub repo — create projects without it.

## Working with this repo

- Standalone repo, remote `origin` = `github.com/tenbucksmobile-png/farmfury_rampage`, branch `main`. Plain command-line git; **never** use Unity's create-project/link-to-GitHub flow.
- The home directory is a separate shared git repo that ignores this folder; never run git commands for this project from the home directory.
- No Git LFS (same as Arcade and Stampede). Keep files under 100 MB; source art (PSD, Kling exports) stays outside the repo; never commit keystores, `.p8` keys or `.env` files.
- Build phases (plan §13) run one per session; start a phase only after the previous one is verified in the editor.
- Android: local Editor builds + USB/adb. iOS: Unity Build Automation — bump `buildNumber.iPhone` before every Cloud Build.

## Related FarmFury projects

- `Desktop/FarmFury_Arcade` — Pac-Man-style; source for Ads/IAP/Analytics managers, Cloud Build setup, web demo.
- `Desktop/FarmFury_Stampede` — platformer; source for `UIKit`, `SafeAreaFitter`, overlay screens, `ParentalGate`.
- `Desktop/FarmFury_Technical/FarmFury_Artwork` — shared art library (Rough_Characters, Rough_Effects, …) and `Rampage_Source/` for Rampage source exports.
- `Desktop/FarmFury` — the original game (shelved).
