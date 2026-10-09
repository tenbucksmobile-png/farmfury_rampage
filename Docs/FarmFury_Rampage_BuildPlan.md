# FarmFury: Rampage — Build Plan

Source: `Docs/FarmFury Rampage — Game Design Document.docx` (Oct 8, 2026). Engine: Unity 6000.5.6f1.
Repo: standalone, `github.com/tenbucksmobile-png/farmfury_rampage`.
Status: **plan only — no Unity project or game code yet.** Decisions are locked (§1, 2026-10-09). Section numbers like "GDD §4" point back to the design document.

---

## 0. How to read this plan

| Part | What it covers |
|---|---|
| 1 | Locked decisions: GDD open questions, gaps and contradictions, plus FarmFury-line alignment |
| 2 | Project setup (repo, Unity version, packages, folders, assemblies) |
| 3 | Runtime architecture (layers, scenes, services, simulation vs presentation) |
| 4 | Data model (every ScriptableObject, Remote Config overlay, save schema) |
| 5 | Gameplay requirements, system by system, with the numbers |
| 6 | Meta game (farm, economy, progression) |
| 7 | UX flows |
| 8 | UI spec (screens, HUD, wireframes, style, accessibility) |
| 9 | Art, audio, VFX and feel production lists |
| 10 | Monetisation, analytics, compliance |
| 11 | Performance budgets and rendering approach |
| 12 | Testing, tools and CI |
| 13 | Phased schedule with work packages and exit gates |
| 14 | Risks (GDD's plus new ones) |

---

## 1. Locked decisions (2026-10-09)

Rule used: take the plan's recommended option, and where the FarmFury line (Arcade, Stampede) already has a proven way of doing something, use that.

### 1.1 GDD open questions — decided
| Question (GDD §14) | Decision |
|---|---|
| Standalone or mode inside FarmFury? | **Standalone app and standalone repo** (`farmfury_rampage`), like Arcade and Stampede. |
| Percy: first rescue or launch hero? | **First rescue hero**, unlocked mid-World 1 (around level 12–15). Launch roster = the four equal heroes. |
| 2D or low-poly 3D crowd? | **2D sprites**, drawn as instanced quads. Same art pipeline as the rest of the line (Kling AI + Photopea; Suno/Pixabay/Freesound audio). |
| Soft-launch countries | 1–2 English-speaking, low-CPI markets; final pick at the start of Phase 4 from current UA data. |
| Real-weather link? | **No.** Farm weather is in-game only; no location permission. |

### 1.2 GDD contradictions — decided
| # | Conflict | Decision |
|---|---|---|
| C1 | 5 worlds/100 levels (§8) vs Worlds 1–3 at launch (roadmap) | **Launch = Worlds 1–3, 60 levels.** Worlds 4–5 are the first two quarterly updates. |
| C2 | Level-5 twists use v1.1 features | Worlds 1–3 level 5 = **all-Guest-gate twist**. Night and hay-wagon twists come with v1.1. |
| C3 | Farm Raid unlocked by a launch building | Scarecrow Tower ships **locked, "coming soon"**; Farm Raid is post-launch. |
| C4 | "Endless Stampede" clashes with FarmFury: Stampede | Mode is called **Endless Rampage**. |
| C5 | Unity version | **6000.5.6f1**, the same editor as Stampede (Arcade is on 6000.5.0f1). Same URP 17.5 / Input System 1.20 / LevelPlay 9.5 / IAP 5.4.2 / Analytics 6.3 as the line. |
| C6 | Browser prototype not found | **Prototype as a Unity greybox.** The pure-C# `RunSim` (§3.4) is written in Phase 1 and kept, so the prototype is not throwaway. |

### 1.3 GDD gaps — defaults adopted (all are data values, tunable in the Prototype)
| # | Gap | Adopted rule |
|---|---|---|
| G1 | Shoot-to-improve rate | Hits count, with a per-gate `maxImprove` (default +5 over base) and a per-gate hit cooldown. Prototype may switch to damage-based (every 40 dmg = +1) — same data fields. |
| G2 | Hero range | 12 m base for all; Bessie's beam 10 m (offset by her pierce). Range buff +15% multiplies it. |
| G3 | Robots that miss the herd | `RobotDef.homing` (0–1); robots passing the herd's back line despawn without penalty. Only the boss can cause a boundary fail. |
| G4 | Hazard losses above 60 animals | A drawn animal hit by a hazard costs `ceil(count / drawn)` animals. |
| G5 | Fury fill amount | Fill target = 3 s × 10 DPS × reference herd 20 × difficulty HP scale (≈ 30 s of play). Upgrades change fill rate, not the target. |
| G6 | Combo window | Second attack type within 1.0 s of the first; per-robot combo cooldown 1.5 s. |
| G7 | Rounding / zero herd | × and ÷ round down; floor 0; a gate that takes the herd to 0 fails the run (revive allowed). |
| G8 | Stars | Independent: ★ clear, ★ finish with 50+, ★ no hazard losses. Rosettes = stars. |
| G9 | Herd Level stacking | Damage additive (+75% at Lv 60). Bite block interval 10 → 6 across its 15 steps. |
| G10 | Robot HP "level" | `LevelDef.difficultyLevel` (sawtooth), fitted by the economy simulator — not the raw level index. |
| G11 | Guests and Fury | Fury = lead hero's ability. Guests count toward the 300 cap and get proportional drawn slots (min 1). |
| G12 | Beam + pierce buff | Pierce +1 = one more robot hit at half damage. |
| G13 | Boss length | Boss HP per `LevelDef`, tuned so the expected herd clears it in 25–40 s. |
| G14 | Ads before consent | Session-1 revives are free; the ad SDK isn't initialised until after the first session. |

### 1.4 Design changes from the GDD (owner decisions)
| Date | Change | Replaces |
|---|---|---|
| 2026-10-09 | **Robots attack as a horde**: tightly packed blocks (several columns wide, many rows deep) rolling slowly toward the herd as one mass. `WaveDef` has `columns`/`columnSpacing`. Bolt Walker: 1.2 m/s and **20 HP** (two eggs at difficulty 1–2), so blasts demolish chunks of the horde. Herd pace 3 → **2.5 m/s** to give time to throw before contact (runs 72–96 s). | GDD §5 single robots / small groups; Bolt Walker 60 HP at 1.8 m/s |
| 2026-10-09 | **The horde is constant and gap-free**: each level has one unbroken `HordeStreamDef` from start to finish, rows packed shoulder to shoulder (0.95 m both ways) at a **constant width** per level (5 / 6 / 6). Walkers are 10 HP (one egg kills) at 1.2 m/s; herd pace 2 m/s (runs 90–120 s). To stop a herd that snowballs through the gates from steamrolling it, the stream's **armour ramps**: 1× HP for the first 40–50% of the level, then up to 7–16× at the end (`hpRampStart`, `hpMultiplierEnd`); armoured walkers are tinted dark red. | Discrete hordes; then a sparser widening stream (same day, "gaps" and "almost too easy" in playtest) |
| 2026-10-09 | **Boss robots inside the horde**: heavy robots embedded in the stream need sustained fire — first is the GDD's **Tiller Tank** (900 HP, 1 m/s, bite 8, 1.1 m radius; gate-ploughing comes later). Big robots clear their own space in the pack. Blasts on the pack around a tank also hit it; steering toward a tank focuses it, steering away dodges it. These are mid-level heavies; the end-of-world boss (Harvex-9000) is still Phase 2. | GDD §3 single "elite push" robot at 60–80 s |
| 2026-10-09 | **Cluck's egg is a grenade**: thrown in an arc at the nearest robot (leading it), explodes on landing and damages every robot in a 1.5 m blast. With no robot in range, eggs are lobbed at the +/- gate in the herd's lane. Prototype values: 12 damage, every 1.2 s per animal, 11 m range, 0.7 s flight, 0.6 m scatter. Each chicken holds its egg while nothing is in range and throw timings are golden-ratio staggered, so the herd throws a steady stream, not volleys. Throwers skip robots that eggs already in the air will kill, so throws spread across the front of the horde instead of overkilling one robot. | GDD §4 Cluck "3 eggs in a fan every 0.6 s, 2 per egg" |

Balance consequence: Cluck keeps the 10 DPS-per-animal **single-target** number, but against packed hordes the blast multiplies it. Bessie, Horace and Ducky must therefore be balanced on the GDD's real rule — time-to-clear the reference waves (which must now include hordes) within ±5% — when they are built in Phase 2, not on DPS alone. Their attacks may need area effects too (e.g. Bessie's stream piercing a whole column, Horace's boomerang sweeping a row).

### 1.5 FarmFury-line alignment (what Rampage reuses)
| Area | Line convention (source) | Rampage |
|---|---|---|
| Repo | Standalone repo per game, plain command-line git, `origin` on `tenbucksmobile-png`, no Git LFS; never link through Unity's GitHub flow (it creates a new default project and repo) (Stampede) | Same. Remote `farmfury_rampage`. Home-directory repo ignores the folder. |
| Folders / namespaces | `Assets/_Project/Scripts/<Area>`, `Assets/_Project/ScriptableObjects/<Type>`, namespace `FarmFury<Game>.<Area>` (Stampede) | `Assets/_Project/...`, namespace `FarmFuryRampage.<Area>`. Assembly definitions per layer kept from the GDD. |
| Build order | One phase per Claude Code session; next phase only after the previous is verified in the editor (Stampede/Rush) | Same; phases in §13. |
| UI | **uGUI + TextMeshPro only, built in code** (`UIKit`, `SafeAreaFitter`, overlay screens) (Stampede, ported from Arcade) | **uGUI everywhere** — the GDD's UI Toolkit menus are dropped. Port `UIKit`, `SafeAreaFitter`, `OverlayScreen`, Settings/Shop/Legal/ParentalGate/Leaderboard overlays. |
| Ads | LevelPlay + AdMob; **every player treated as child-directed**: dashboard toggle *and* `is_child_directed` / `is_deviceid_optout` metadata before `Init()` (Arcade) | Same; no age gate needed for ads, no personalised ads anywhere. |
| IAP | Unity IAP 5 via `IAPManager` + `Store` facade; arithmetic **parental gate** before every purchase; Restore Purchases (Arcade, Stampede) | Same pattern, Rampage's own product ids. |
| Analytics | UGS Analytics 6.3, typed events, dev-build `RECORDED`/`DROPPED` logging, dashboard names must match code exactly (Arcade) | Same; event list in §10.4. |
| Backend / cloud save | Shared **Supabase** backend planned across the line for player identity, cloud save, cross-promo, shared cosmetics (Stampede) | Supabase for cloud save, identity and receipt validation (edge function). Save schema kept compatible with the line. Unity Cloud Save dropped. |
| Remote tuning | — | UGS Remote Config (gate values, robot HP, ad frequency), as in the GDD. |
| Builds | Android: local Editor builds, USB + adb testing. iOS: Unity Build Automation (no Mac); **bump `buildNumber.iPhone` before every Cloud Build** (Arcade) | Same. GitHub Actions only runs tests (no GameCI builds). |
| Web | WebGL demo on farmfurygames.com/play and YouTube Playables (Arcade `web-demo`) | WebGL playtest build hosted the same way; Ads/IAP stubbed. |
| Legal | Privacy/terms on farmfurygames.com (Arcade) | Add Rampage pages there. |
| Rendering | URP 2D, Linear colour space (Arcade celebration-ghosting fix) | Same. |
| Video | Celebration clips rendered with background baked in, no chroma key (FarmFury) | Same. |
| Level data | Coordinate bug broke FarmFury L02–L06 | Track-coordinate authoring + LevelDef validator in tests (§12.2). |

One deliberate difference: the line uses manager singletons (`GameManager`, `SaveManager`, `AdManager`…). Rampage follows the GDD's single `Services` locator with interfaces, so the run simulation can be tested headless. Arcade/Stampede manager code is ported *into* those service implementations rather than copied as singletons.

## 2. Project setup (Week 1)

### 2.1 Repository
1. ✅ `git init` inside `Desktop/FarmFury_Rampage`, branch `main`, remote `origin` = `github.com/tenbucksmobile-png/farmfury_rampage` (plain command-line git; never Unity's GitHub-link flow).
2. ✅ `Desktop/FarmFury_Rampage/` added to the **home repo's** `.gitignore` so the nested repo is never added as a gitlink.
3. ✅ Unity `.gitignore` (merged from Arcade + Stampede, plus keystores/keys never committed). **No Git LFS**, like the rest of the line (the GDD suggested LFS; Arcade/Stampede ship without it and Unity Build Automation pulls plain git). Keep each file under GitHub's 100 MB limit; raw PSD/Kling source art stays outside the repo.
4. ✅ GDD and this plan in `Docs/`; `CLAUDE.md` at the root.
5. ✅ Unity 6000.5.6f1 Universal 2D project at the repo root (2026-10-09). Next: GitHub Actions for tests (§12.5).

### 2.2 Unity project
- Editor **6000.5.6f1** (same as Stampede). Template: **Universal 2D (URP)**. Portrait only (`Screen.orientation = Portrait`, auto-rotation off).
- Player settings: IL2CPP, ARM64, min Android API per current Play policy, target 60 fps (`Application.targetFrameRate = 60`), Vulkan + GLES3 fallback, Linear colour space (FarmFury lesson: Linear RT fixed the celebration ghosting).
- Active Input Handling: **Input System only**.

### 2.3 Packages (same versions as Stampede/Arcade where they overlap)
| Package | Purpose |
|---|---|
| Universal RP 17.5 | 2D renderer, SRP Batcher |
| Input System 1.20 | Relative drag, editor keyboard/mouse |
| Burst, Collections, Mathematics, Jobs | Herd/projectile/robot simulation |
| 2D Sprite, 2D Animation, PSD Importer, Aseprite Importer, Sprite Atlas v2 | Art pipeline |
| uGUI 2.5 + TextMeshPro | All UI (HUD and menus), built in code like Stampede's `UIKit` |
| Localization | Language setting, string tables |
| Addressables | Per-world content loading; keeps install < 150 MB |
| Test Framework 1.7 + Performance Testing | Edit/Play-mode tests, frame-time benchmarks |
| Services: Analytics 6.3, LevelPlay 9.5 (+ AdMob adapter), Purchasing 5.4.2, Remote Config, Leaderboards (Endless) | Live services (as Arcade) |
| Supabase C# client | Shared FarmFury backend: identity, cloud save, receipt validation |
| Mobile Notifications | Crop/building ready notifications (opt-in) |
| Nice Vibrations or custom native haptics wrapper | Light/medium/heavy haptics |
| (Optional) Unity MCP editor bridge | Lets Claude Code read Console, run tests, inspect scenes |

Remove unused defaults (Visual Scripting, Multiplayer Center, Collab proxy).

### 2.4 Folder layout
```
Assets/_Project/
  Scripts/
    Data/          ScriptableObject class definitions + enums      FarmFuryRampage.Data      (asmdef)
    Core/          Services locator, service interfaces & impls     FarmFuryRampage.Core      (asmdef)
    Sim/           Pure C# run simulation, no MonoBehaviours        FarmFuryRampage.Sim       (asmdef)
    Run/           Run scene presentation, input, views             FarmFuryRampage.Run       (asmdef)
    Farm/          Farm scene systems + views                       FarmFuryRampage.Farm      (asmdef)
    UI/            uGUI screens built in code (UIKit port)           FarmFuryRampage.UI        (asmdef)
    Utilities/     Pools, maths helpers                             FarmFuryRampage.Utilities (asmdef)
    Editor/        Level editor, validators, build scripts          FarmFuryRampage.Editor    (Editor-only)
  ScriptableObjects/  Heroes/ Robots/ Bosses/ Gates/ Levels/W1..W5/ Worlds/ Crops/ Combos/ Buildings/ Tuning/
  Tests/EditMode/  Tests/PlayMode/
  Sprites/ Audio/ VFX/ Video/ Fonts/ Prefabs/
  Scenes/          Boot.unity, Run.unity, Farm.unity, Bench.unity (perf test)
```

### 2.5 Assembly dependency rules (one direction only)
```
Data  ←  Sim  ←  Run  ─┐
  ↑        ↑            ├→ UI
  └──── Core ← Farm ────┘
Editor → everything.   Tests → everything.   Run ⇸ Farm and Farm ⇸ Run (never).
```
- `Sim` references only `Data`, Unity.Mathematics/Collections/Burst. No `UnityEngine.Object` lookups — this is what makes the balance test fast and deterministic.
- The one designed cross-scene link (crops → run pickups) goes through a `RunLoadout` object built by Services, never a direct reference.

---

## 3. Runtime architecture

### 3.1 Scenes
| Scene | Contents | Notes |
|---|---|---|
| **Boot** | `ServicesRoot` (DontDestroyOnLoad), splash, async service init | GDD names two scenes; a tiny Boot scene keeps service init out of both. |
| **Run** | Track, herd, robots, gates, HUD (uGUI), pause/revive/results overlays | Loaded with a `RunLoadout`. |
| **Farm** | Farm map, buildings, fields, menus (uGUI): hero picker, world map, recipe book, shop, settings | Home screen. |
| **Bench** (dev only) | 300 animals / 400 projectiles / 80 robots stress scene | Used by the performance test. |

Scene flow: `Boot → (first launch) Run[tutorial] → Farm ⇄ Run`. Transitions via `SceneService` with a short barn-door wipe; run-to-farm ≤ 1.5 s on mid-range Android.

### 3.2 Services layer (the only "singleton")
`Services` is a static locator populated once in Boot; systems receive what they need via `Init(...)`.

| Service | Responsibility | Backing |
|---|---|---|
| `ISaveService` | Load/save `PlayerSave` JSON, versioned migrations, cloud sync, conflict resolution (highest progress wins, currencies merged by transaction log) | Local JSON + Supabase (shared FarmFury schema) |
| `IEconomyService` | Currencies (Scrap, Produce, Rosettes, Golden Eggs), atomic transactions, cost lookup | SO + Remote Config |
| `IProgressService` | Levels, stars, worlds, Herd Level, rescues, recipe book, unlocks | Save |
| `IFarmService` | Buildings, timers, fields, windmill offline income | Save + `ITimeService` |
| `ITimeService` | Trusted UTC (server time when online, monotonic fallback) to stop clock-change cheats | Supabase / HTTPS Date header |
| `IConfigService` | Merges Remote Config overrides onto SO values at boot | Remote Config |
| `IAdsService` | Rewarded/interstitial, frequency caps, always child-directed | LevelPlay + AdMob, ported from Arcade's `AdManager` (stub on WebGL/editor) |
| `IIapService` | Products, purchase, restore, receipt validation, parental gate | Unity IAP, ported from Arcade's `IAPManager`/`Store` (stub on WebGL) |
| `IAnalyticsService` | Typed events (§10.4) | UGS Analytics, ported from Arcade's `AnalyticsManager` |
| `IAuthService` | Anonymous sign-in; shared FarmFury player identity | Supabase Auth (UGS anonymous auth only for Analytics/Remote Config) |
| `IAudioService` | Music layers, SFX pooling, mixer snapshots | AudioMixer |
| `IHapticsService` | Light/medium/heavy | Native wrapper |
| `IConsentService` | Child-directed state (always on), analytics consent where required | Save |
| `ISceneService` | Scene loading, transitions, `RunLoadout` hand-off | SceneManager |
| `ILocalizationService` | Language, string lookup | Localization pkg |
| `INotificationService` | Opt-in "crops ready" | Mobile Notifications |

Every service has an interface and a **fake** implementation for tests and WebGL.

### 3.3 Communication
- Within the Run scene: the `RunSim` exposes **event buffers** (gate passed, robot killed, animals lost, combo triggered, rescue, fury fired). Presentation reads them each frame. No C# events from Sim into views, so Sim stays deterministic.
- Between scenes: `RunLoadout` (in) and `RunResult` (out), both plain data.
- UI ↔ systems: view-models with simple change notifications; UI never edits save data directly, only via services.

### 3.4 Run simulation vs presentation (key architectural choice)
Split the run into:
- **`RunSim` (Sim assembly)**: pure C#, fixed 60 Hz tick, seeded RNG, NativeArrays for herd/projectiles/robots, Burst jobs for movement & hit tests. Input = steer target X + Fury pressed. Output = state arrays + event buffers.
- **`RunView` (Run assembly)**: renders state (instanced quads for herd/projectiles, pooled SpriteRenderers for robots/gates/pickups), plays VFX/SFX/haptics from events, camera.

Payoffs: the hero-balance test runs headless in milliseconds; Ad Challenges and ghost herds (v1.2) are replayable from seed + input log; bugs reproduce exactly.

### 3.5 Coordinate system (prevents the FarmFury coordinate bug)
- 1 Unity unit = 1 metre. Track X ∈ [−halfWidth, +halfWidth] (default track width 9 m), distance D along the track ∈ [0, levelLength].
- **The herd stays at a fixed screen position; the world scrolls.** Every track object stores `(x, d)`; view Y = `d − herdDistance + herdScreenOffset`. No floating-point drift, no world-space level data.
- `LevelDef` content is authored **only** in `(x, d)` track coordinates; the validator (§12.2) rejects anything outside bounds.

### 3.6 Run-scene system list
| System | Responsibility |
|---|---|
| `RunDirector` | Phase machine: Hatch → Gate Stretch → Elite Push → Last Stand → Win/Fail; spawns `LevelDef` events by distance; places crop pickups from `RunLoadout` |
| `TrackScroller` | Advances `herdDistance` at `pace` (stops at Last Stand); world chunks/tiles per world |
| `HerdSim` | Count (0–300), composition (lead + guests), steer X, formation (phyllotaxis), drawn slots (≤60), damage multiplier |
| `WeaponSim` | Per-hero fire patterns from `HeroDef`; emitters = drawn animals; buff modifiers |
| `ProjectileSim` | Pool of ≤400; Burst move; spatial-hash hit test vs robots, gates, pickups, cages |
| `BeamSim` | Bessie's stream: per-emitter 10 Hz ray, first robot full, next half |
| `RobotSim` | Movement (straight / zig-zag / drift / stop-at-range / tank), HP, shields, status effects, bite on contact, splits, heals, sniping |
| `GateSim` | Gate rows, panel selection by herd centre X, operations, shoot-to-improve, red→blue flip, Tiller Tank ploughing |
| `PickupSim` | Crates (hay bale, feed sack, milk churn, egg crate) + crop pickups; HP; rewards |
| `HazardSim` | Potholes, laser fences, crusher pistons, conveyors, oil slicks, buzzsaw drones |
| `StatusSim` | Slow, short (stun), wet, rust (+15% dmg taken), custard (sticky), blind |
| `ComboResolver` | Per-robot attack-type history → `ComboDef` → effect; reports discoveries |
| `FurySim` | Meter fill from damage dealt; fires hero Fury ability |
| `BossSim` | Per-boss state machine (phases, weak points, attacks) |
| `RescueSim` | Cage HP on carrier robots; rescue on break; counts per species |
| `RunEconomy` | Scrap/Produce collected; rescues; stars; builds `RunResult` |
| `RunView` + `HerdRenderer` + `ProjectileRenderer` | Instanced drawing |
| `FeedbackDirector` | Hit flash, bursts, shake, slow-mo, haptics, count pop |
| `AdaptiveMusic` | Layers by herd size |
| `CameraRig` | Fixed portrait framing, safe-area aware, shake |

### 3.7 Farm-scene system list
| System | Responsibility |
|---|---|
| `FarmManager` | 8 building slots, levels, upgrade timers, Farmhouse cap |
| `FieldSystem` | 4 plots; plant/grow/harvest; produces `CropLoadout` for next run |
| `WindmillSystem` | Offline Produce accumulation (capped) |
| `WorkshopSystem` | Gadgets (Fury charge, revive shield) |
| `ShrineSystem` | Gate-luck modifiers |
| `RescuePen` | Species bars, hero unlocks, animal idle wander |
| `HeroPicker` | Hero select, skins, test-fire preview (runs a tiny `RunSim` in a render texture) |
| `WorldMapController` | Fence-post level road, stars, Rosette gates |
| `RecipeBook` | Discovered combos, silhouettes |
| `FarmCamera` | Pan/zoom over the hand-placed farm |

---

## 4. Data model

### 4.1 ScriptableObject definitions
All gameplay numbers live here (GDD rule). Each has a stable string `id` used by save data and Remote Config.

| Def | Key fields |
|---|---|
| `HeroDef` | id, name, attackType (Egg/Milk/Horseshoe/Water/Mud…), fireInterval, projectilesPerShot, spreadAngle, damagePerHit, returnHit (Horace), pierce, beam (Bessie), range, projectileSpeed, trait (`StatusEffectDef` + chance/duration), furyAbility (`FuryDef`), silhouette sprites, icon, barks, SFX set, skins[] |
| `FuryDef` | type (EggStorm/MilkFlood/HorseshoeHurricane/Monsoon/…), duration, damage budget, pushback, shortDuration, VFX |
| `RobotDef` | id, tier (1–5), hp, shield (front, amount), speed, bite, behaviour enum + params (zigzag amplitude, stopDistance, healRadius/rate, splitInto/count, ploughGates), homing, scrap drop, eye colour, prefab/sprite set, death VFX |
| `BossDef` | hp curve per level, phases[] (attack patterns, weak points, thresholds), intro taunt clip, celebration video |
| `GateDef` | kind (Add/Sub/Mul/Div/Buff/Fury/Guest/Hack), value, buff type+amount, guest hero, improvable, hitsPerStep, maxImprove, frame shape, colour |
| `PickupDef` | crate/crop kind, HP, reward (animals, Scrap, buff, area damage, turret, block) |
| `CropDef` | id, grow time, Produce cost, field level required, pickup produced (`PickupDef`), pickups per run |
| `ComboDef` | attack type A + B, name, effect (`StatusEffectDef` or special), icon, Recipe Book text |
| `StatusEffectDef` | slow %, stun, damage-taken %, duration, stack rule |
| `HazardDef` | kind, footprint, timing pattern, kills animals (Y/N), robots affected (Y/N) |
| `LevelDef` | world, index, length (m), pace, difficultyLevel, phase distances, **events[]** (gate rows, waves, crates, hazards, cages, boss) all in `(x, d)`, boss HP, twist flag, star thresholds, seed |
| `WaveDef` | spawns[] (robotDef, x, dOffset, count, formation), carries cage? |
| `WorldDef` | id, name, levels[20], new robot, new hazard, boss, tileset/backdrop, music, Rosettes to unlock |
| `BuildingDef` | id, levels[] (cost Scrap/Produce/Rosettes, timer, Farmhouse level required, effect values) |
| `HerdLevelTable` | 60 rows: cost, stat, value |
| `EconomyTuning` | Scrap per robot tier, level rewards, revive cost, ad multipliers, windmill rate/cap, timer-skip price curve |
| `RescueSpeciesDef` | species, rescues needed, hero unlocked |
| `WeatherDef` (v1.1) | robot speed %, explosion every N kills, drone drift, spawn multiplier |
| `ShopProductDef` | IAP id, price tier, contents |
| `SkinDef` | hero, sprites, price |
| `AudioCueDef` / `MusicLayerDef` | clips, volume, herd thresholds |
| `GameTuning` (singleton asset) | track width, camera view, herd cap 300, drawn cap 60, projectile cap 400, formation spacing, fury tuning, combo window, interstitial rules |

### 4.2 Remote Config overlay
`IConfigService` reads keys like `robot.bolt_walker.hp`, `gate.improve.hitsPerStep`, `ads.interstitial.minLevel`, `ads.interstitial.cooldownSec`, `economy.herdLevel.costMultiplier`, applies them to runtime copies of the SOs (never mutates the assets on disk). Keys are generated from the Def `id`s by an editor tool so they can't drift.

### 4.3 Save schema (`PlayerSave`, JSON, versioned)
```
version, createdUtc, lastSavedUtc, deviceId
consent { childDirected (always true), analyticsConsent }
playerId (shared FarmFury identity, Supabase)
currencies { scrap, produce, rosettes, goldenEggs }
progress { levels: {id: {stars, bestHerd, cleared}}, worldsUnlocked, herdLevel, endlessBest }
heroes { selected, unlocked[], skinsOwned[], skinEquipped{} }
rescues { species: count }
recipeBook { discovered[] }
farm { buildings: {id: {level, upgradeEndUtc}}, fields: [{crop, plantedUtc}], windmillLastCollectUtc }
gadgets {}
settings { music, sfx, haptics, reducedShake, leftHanded, colourBlindShapes, steadyHerd, showBite, language }
monetisation { adFree, purchases[], lastInterstitialUtc, dailyChestUtc, revivesUsed... }
tutorial { stepsDone[] }
```
Saved on every results screen and every building action (GDD §12). Migration functions `vN → vN+1` with edit-mode tests.

---

## 5. Gameplay requirements (run)

### 5.1 Camera, view, controls
- Portrait, three-quarter top-down; herd in **lower third**; robots/gates readable ~3 s before arrival.
- Framing check (to verify in Prototype): with view width 9 m at 19.5:9 the view is ~19.5 m tall; herd at 30% height → ~13.6 m ahead visible. At pace 3 m/s vs a Bolt Walker 1.8 m/s that's 2.8 s warning. Rust Hounds (3.5 m/s) give ~2.1 s; acceptable because they telegraph with zig-zag. **Pace, view width and herd height are all `GameTuning` values.**
- **Steer:** one-finger relative drag; herd target X = start X + drag Δ × sensitivity, clamped to track; herd follows with max lateral speed (lower when "steady herd" assist is on). Editor: A/D or mouse drag.
- **Fury button:** single tap when full. Bottom-right (mirrored when left-handed). Nothing else is tappable during a run except pause.
- Auto-pause on focus loss.

### 5.2 Run structure (per `LevelDef`, defaults)
| Phase | Time | Content |
|---|---|---|
| Hatch | 0–10 s | Herd = starting size (5 + Herd Level bonus, max 20). One easy gate, scout bots, the level's twist introduced. |
| Gate stretch | 10–60 s | Gate rows every 5–7 s; waves between; crates and crop pickups. |
| Elite push | 60–80 s | One tier-3/4 elite + one hazard section. |
| Last stand | 80–120 s | Track stops at farm boundary; boss enters; herd holds and steers to dodge. |

Level length ≈ 80 s × pace before Last Stand (≈ 240 m at 3 m/s).

### 5.3 Herd
- Count 0–300 (cap in `GameTuning`). Starting count from Herd Level.
- **Formation:** phyllotaxis (sunflower) around the herd centre: slot i at radius `c·√i`, angle `i·137.5°`; c ≈ 0.25 m gives ~1.9 m radius at 60.
- **Drawn animals:** `min(count, 60)`. Above 60 the crowd shows a "stampede" dust layer and each drawn animal carries `count / drawn` damage.
- **Composition:** lead hero + guest groups; drawn slots split proportionally (at least 1 per guest group).
- **Damage rule:** herd DPS = count × 10 × modifiers (identical for every hero).
- **Losses:** knocked-out animals tumble back with feathers/dust and are removed from the back of the formation; count badge pops.
- Count badge over the herd (speech bubble with hero icon).

### 5.4 Heroes (all 10 DPS per animal; side effects equal value)
| Hero | Pattern | Dmg | Trait | Fury |
|---|---|---|---|---|
| Cluck | **Egg grenade** (§1.4): lobbed at the nearest robot every 1.2 s, 1.5 m blast | 12 to every robot in the blast | Yolk splat: slow 10% for 1 s (not built yet) | Egg Storm: eggs rain on whole track 3 s |
| Bessie | Continuous stream, 0.1 s ticks | 1/tick | Passes through first robot at half damage | Milk Flood: wave pushes robots back 2 m + damage |
| Horace | Boomerang every 1.0 s | 5 out + 5 back | Hits every robot both ways | Horseshoe Hurricane: orbiting ring 4 s |
| Ducky | Glob every 0.4 s | 4/glob | 10% chance to short 0.5 s | Monsoon: shorts every robot 1.5 s |
| Percy (rescue) | Design in Phase 3 (e.g. mud bombs: AoE) | — | Same 10 DPS rule | — |

Requirements: Fury fill identical for all; Herd Level shared; skins cosmetic; **balance test** — scripted herd of 20 vs 5 reference waves, time-to-clear within ±5% of target for every hero, runs on every commit.

### 5.5 Gates
| Kind | Colour / frame shape | Values | Rule |
|---|---|---|---|
| Add | Blue / rounded arch | +3…+30 | Grows herd; shoot to improve |
| Subtract | Red / jagged frame | −3…−30 | Shrinks; shoot up toward +; flips blue at >0 |
| Multiply | Blue / double-thick frame + "×" | ×2, ×3 | Fixed; lets shots through |
| Divide | Red / cracked frame + "÷" | ÷2 | Fixed; lets shots through |
| Buff | Gold / star-topped | fire rate +20%, range +15%, pierce +1 | Rest of run; stacks per `GameTuning` cap |
| Fury | Purple / diamond | — | Instant full meter |
| Guest | Green / leafy arch | +3 Horace etc. | Adds guests of another hero |
| Hack (v1.1) | Grey / hex | — | Next robot through becomes a Farmbot ally |

- Rows of 2 or 3 panels spanning the track; herd passes exactly one (panel under the herd centre X).
- Number flies from gate to the count badge.
- Tiller Tank touching a gate turns it into a − gate.
- Shrine upgrades shift generation odds (higher + values, fewer reds) — applied at level load to gates marked `luckAffected`.

### 5.6 Robots (HP at difficulty 1; scales ×(1 + 0.12 × (difficulty − 1)))
| Tier | Robot | HP | Speed | Bite | Behaviour |
|---|---|---|---|---|---|
| 1 (white eyes) | Buzz Drone | 20 | 3.0 | 1 | Swarm of 5–10, drifts toward herd |
| 1 | Bolt Walker | 60 | 1.8 | 2 | Walks straight |
| 2 (yellow) | Rust Hound | 120 | 3.5 | 3 | Zig-zags across lanes |
| 2 | Shield Bot | 150 + 100 front shield | 1.5 | 3 | Shield soaks front hits (side hits bypass) |
| 3 (orange) | Tiller Tank | 600 | 1.0 | 8 | Ploughs gates into − gates |
| 3 | Seeder Bot | 300 | 1.6 | 4 | Splits into 3 Buzz Drones on death |
| 4 (red) | Scare-Bot | 400 | stops at 12 m | 1 per 3 s at range | Sniper |
| 4 | Fix-It Bot | 250 | 1.2 | 2 | Heals robots within 3 m, 20 HP/s |
| 5 (purple) | Bosses | 3,000–20,000 | varies | herd wipe | One per world (+ mini at level 10) |

Formation (§1.4): grunts (Bolt Walkers, later Shield Bots) arrive as packed hordes — blocks up to ~9 wide at 0.95 m spacing, rolling slowly as one mass; Buzz Drones stay loose swarms. Rules: robot reaching the herd removes `bite` animals then explodes; health bar appears once damaged; optional bite number ("show bite" setting); death = springs/bolts/cogs burst, cogs fly to the Scrap counter; some robots carry cages (§5.9).

### 5.7 Bosses
| World | Boss | Mechanic to build |
|---|---|---|
| 1 | Harvex-9000 Combine | Reel launches Buzz Drones; grain tank opens periodically → weak point ×2 damage |
| 2 | Mega Milker | Suction pulls animals out of herd; 4 hoses are separate breakable parts |
| 3 | Silo Spider | 6 legs stomp lanes (telegraphed); safe lane moves; drops Seeder Bots |
| 4 | Crop Duster X | Rust cloud on one half; player must alternate sides |
| 5 | Tractor Titan | 3-phase finale remixing earlier patterns |

Shared boss framework: `BossDef` phases, parts with own HP, telegraph durations, weak-point windows, top progress bar turns into boss HP bar, intro taunt line, 0.3 s slow-mo on the final hit, celebration video (background baked in, **no chroma key**). Mini-boss = scaled-down world boss with fewer phases.

### 5.8 Pickups, crates and crops
- Crates: hay bale, feed sack, milk churn, egg crate → +animals, Scrap, short buff. Shoot to open (HP).
- Crop pickups from fields (GDD §6.1): Pumpkin bomb (AoE), Popcorn crate (+1 animal pickups burst), Sun turret (fires seeds 5 s), Hay bale (blocks robots; rolls forward when shot), Carrot rocket (flies up lane, pierces).
- `RunDirector` inserts crop pickups at authored "crop slots" in each `LevelDef` (so crops never land in invalid spots), filling slots from the player's harvested crops; empty slots stay empty.

### 5.9 Rescues
- Carrier robots have a cage (separate HP); break it before the carrier leaves the screen → animal rescued (piglet, sheep, goat, goose).
- Rescues add to the species bar; full bar unlocks the hero (Percy first). Results screen shows rescues.

### 5.10 Recipe combos
| Mix | Combo | Effect |
|---|---|---|
| Egg + Milk | Custard | Slow 40% 2 s |
| Egg + Water | Poached | Steam blinds Scare-Bots/turrets 2 s |
| Egg + Horseshoe | Scramble | Horseshoe sprays egg shrapnel on return |
| Milk + Water | Slip 'n' Slide | Robot slides sideways into neighbours |
| Milk + Horseshoe | Butter Churn | Horseshoe spins in place 1 s |
| Water + Horseshoe | Rust | +15% damage taken 3 s |
Requires a Guest gate to get two attack types. First trigger → "New recipe!" toast; Recipe Book records it. All combos tuned to equal value (add to balance test: lead+guest pairs within ±5%).

### 5.11 Hazards (World introduced)
Potholes (W1), laser fences (W2), crusher pistons + conveyor belts (W3), oil slicks + buzzsaw drones (W4), remix (W5). Each: clear telegraph, kills animals it touches (G4 rule), counts against the 3★ badge.

### 5.12 Fail, revive, win
- Fail: herd = 0, or boss reaches the boundary.
- Revive once per run: rewarded ad or 1 Golden Egg (free in session 1). Revive restores herd to max(10, 50% of peak) and clears nearby robots.
- Win: boss breaks. Stars per G8.
- "Upgrade herd" becomes the main results action after 2 consecutive fails on the same level.

### 5.13 Modes
| Mode | Launch? | Requirements |
|---|---|---|
| Campaign | Yes | Worlds 1–3 at launch (C1) |
| Endless (rename, C4) | Yes, after World 1 | Procedural track from chunk templates, scaling difficulty, weekly UGS leaderboard |
| Daily Ad Challenge | Yes (day 2) | One hand-built `LevelDef` per day from a remote list; Golden Egg reward; "Ad Challenges" list of all past ones (GDD §6.8) |
| Harvest Festival | Live ops | Event `WorldDef` with themed robot, own reward track |
| Farm Raid | Post-launch (C3) | Tower-defence on farm paths |

### 5.14 Post-launch features (v1.1 / v1.2)
Weather (robot-only modifiers), Hack gate, Hay wagon ride, Ghost herds (needs friends list + run replays: seed + input log, see §3.4), Harvest Pass.

---

## 6. Meta game: rebuilding Fury Farm

### 6.1 Currencies
| Currency | Earned | Spent |
|---|---|---|
| Scrap (cogs) | Robots, level rewards | Buildings, Herd Level |
| Produce | Harvest, windmill | Planting, decorations, Recipe Book unlocks |
| Rosettes | Stars | Unlock worlds, building levels |
| Golden Eggs | IAP, achievements, events, daily chest | Revives, skins, timer skips |
No energy system.

### 6.2 Buildings (8, hand-placed)
| Building | Effect on runs |
|---|---|
| Farmhouse | Caps other buildings; unlocks gate types |
| Training Barn | Herd Level |
| Fields ×4 | Crops → track pickups |
| Workshop | Gadgets: Fury charge, revive shield |
| Lucky Horseshoe Shrine | Gate luck |
| Rescue Pen | Houses rescues, unlocks heroes |
| Windmill | Offline Produce |
| Scarecrow Tower | Farm Raid (locked at launch, C3) |
Timers: < 2 h until Farmhouse 10, never > 8 h. Rewarded ad can finish a timer; Golden Eggs can skip.

### 6.3 Herd Level
60 levels at launch; fixed rotation Damage +5% → Starting herd +1 (cap 20) → Fury +4% → Bite resistance; costs in `HerdLevelTable`. Shared by all heroes.

### 6.4 Economy pacing deliverable
Build an **economy simulator** (edit-mode tool) that plays the campaign with an "average player" model: Scrap per run, Herd Level reached per world, expected level clear rate. Output a CSV and fail the test if any world needs more than N replays of the last level to clear. Used to set `difficultyLevel` (G10) and costs.

---

## 7. UX flows

### 7.1 First-time user experience (target: shooting within 5 s)
1. Splash (≤2 s, logo) → **no menus, no sign-in**.
2. Scripted Run with Cluck: Hatch with 5 animals; robots appear; drag hand-pointer prompt ("Drag to steer").
3. First gate: guaranteed ×2 with pointer.
4. Deliberate bite: a Bolt Walker slips through → herd loses 2 → caption "Robots knock animals out!".
5. Crate tutorial ("Shoot crates for stuff").
6. Fury fills → pulsing button + pointer.
7. Intro boss flattens the farmhouse in a cutscene (the reason to rebuild).
8. Farm scene: Training Barn highlighted → first upgrade (instant) → hero picker unlocked (4 heroes) → "Rampage!" button.
9. After the first session: data-consent prompt where required and optional account link (shared FarmFury identity). Ads SDK initialises only after this (G14); every player is child-directed, so there is no age gate.
Later tutorials appear just-in-time: first red gate, first Guest gate (combo), first crop, first cage, first hazard, first boss.

### 7.2 Core loop
Farm (collect, upgrade, plant) → Rampage! (pre-run: hero picker if changed) → Run → Results → Farm. **Every screen is at most two taps from the next run.**

### 7.3 Screen map
```
Boot ─► [first launch] Tutorial Run ─► Farm(Home)
Farm(Home) ─┬─ Rampage! ─► Run ─► Results ─► Next level ─► Run
            │                          └──► Farm
            ├─ Hero picker (bottom tab)  ─► Skins
            ├─ World map (bottom tab)    ─► Level card ─► Run
            ├─ Recipe Book (bottom tab)
            ├─ Shop (bottom tab)
            ├─ Building tap ─► Upgrade sheet
            ├─ Field tap    ─► Plant / Harvest sheet
            └─ Settings (gear, top-right)
Run ─ Pause ─► Resume / Settings / Quit to farm
Run ─ Fail  ─► Revive offer (ad / 1 Golden Egg / no) ─► Results
```

### 7.4 Interaction rules
- Bottom-tab navigation in the Farm (thumb zone); primary CTA always bottom-centre.
- Modal sheets slide up from the bottom; dismiss by swipe down or tap outside.
- Red badge dots for: harvest ready, upgrade done, new recipe, free chest.
- Interstitials never after a loss, never in the first 8 levels, ≥ 3 min apart, never on the way *into* a run.
- Rewarded ads always opt-in with the reward shown on the button.

---

## 8. UI specification

### 8.1 Tech split (GDD §12)
- **All UI is uGUI + TextMeshPro, built in code**, matching the FarmFury line (replaces the GDD's UI Toolkit menus).
- Port from Stampede/Arcade: `UIKit` (programmatic panels, buttons, text, toggles), `SafeAreaFitter`, `OverlayScreen` base, Settings, Shop, coin-purchase, Legal, `ParentalGate`, Leaderboards overlays. Reskin them; don't rewrite them.
- HUD on its own canvas (fast-changing); menus on a separate canvas so HUD updates don't rebuild menu layouts.
- Art-first screens like Stampede: painted backdrops fitted without cropping, round wooden buttons at a shared size.
- Reference resolution 1080 × 2340 portrait; Safe Area wrapper on every root panel; min touch target 48 dp.

### 8.2 Visual language
- Farm side: straw yellow, barn red, grass green, sky blue, wood textures, hand-painted.
- HARVEX side: steel grey, chrome, warning orange; tier eye colours white/yellow/orange/red/purple.
- UI frames: chunky wood planks with rope/nail details; buttons with a 6 px "press-down" offset.
- Font: one rounded display face for numbers/titles, one readable sans for body (check licence + localisation glyph coverage).
- Numbers are the hero of the HUD: herd count and gate values in big outlined digits.

### 8.3 In-run HUD wireframe
```
┌──────────────────────────────────┐
│ [II]  W1-07  ▓▓▓▓▓▓░░░░ 🤖  ⚙ 1,240│  ← pause | level | progress→boss HP | Scrap
│                                  │
│        ┌──┬──┐                   │
│        │×2│+8│   ← gate row      │
│        └──┴──┘                   │
│     🤖   🤖      🤖               │
│                                  │
│            ┌─────┐               │
│            │🐔 47│ ← count badge │
│           ·:·:·:·:·              │
│           :·:·:·:·:  herd        │
│                                  │
│                          (FURY)  │  ← bottom-right, glows when full
└──────────────────────────────────┘
```
Left-handed mode mirrors pause↔Scrap and the Fury button.

### 8.4 Farm (home) wireframe
```
┌──────────────────────────────────┐
│ ⚙1,240  🌽380  🏵12  🥚55      ⚙️ │  ← currencies + settings
│                                  │
│   [Farmhouse]      [Windmill]    │
│  [Barn]  [Field][Field]  [Pen]   │
│  [Workshop] [Field][Field]       │
│        [Shrine]   [Scarecrow🔒]  │
│                                  │
│        ┌──────────────────┐      │
│        │   RAMPAGE!  W1-08│      │  ← primary CTA
│        └──────────────────┘      │
│ [Farm] [Heroes] [Map] [Recipes] [Shop]
└──────────────────────────────────┘
```

### 8.5 Other screens
| Screen | Key elements |
|---|---|
| Hero picker | Carousel of 4 (+rescues); live test-fire preview vs dummy robots; trait + Fury blurb; skin row; "Same power, different feel" note |
| World map | Farm road with fence-post levels, stars per post, boss posts larger, Rosette lock on next world |
| Level card | Level number, new-robot/hazard icon, best stars, crops that will appear, Play |
| Results | Stars animate in, Scrap + Produce counters, rescues, "New recipe!", ×2 Scrap (rewarded ad), primary: Next level / Upgrade herd |
| Revive | Herd count before death, "Watch ad" / "1 Golden Egg" / "No thanks" (10 s timer, no dark-pattern countdown pressure) |
| Upgrade sheet | Current → next effect, cost, timer, Farmhouse cap message |
| Field sheet | Crop choices with grow time + resulting track pickup; harvest-all |
| Recipe Book | 6 combo cards; undiscovered as silhouettes with a hint |
| Shop | Starter pack, ad-free, Golden Egg bundles, skins, daily free chest; arithmetic parental gate before every purchase (Arcade/Stampede `ParentalGate`) |
| Settings | Music, SFX, haptics, reduced shake, left-handed, colour-blind shapes, steady herd, show bite, language, restore purchases, privacy, credits, parental controls |

### 8.6 Accessibility requirements
- Gate type readable by **shape and icon**, not colour only (always on; "colour-blind shapes" setting adds stronger patterns).
- Left-handed mirror, reduced shake, steady herd assist, show bite numbers.
- Text minimum 28 px at reference resolution; numbers outlined for contrast on busy tracks.
- Haptics toggle; no flashing faster than 3 Hz (Monsoon/boss VFX checked).

---

## 9. Content production lists

### 9.1 Art
| Item | Count (launch: Worlds 1–3) |
|---|---|
| Hero crowd sprites (back view, run cycle 6–8 frames, fire pose, knocked-out tumble) | 4 + Percy |
| Hero front/portrait art for UI, picker, results | 5 |
| Skins (crowd + portrait) | 4–6 |
| Robots (walk, hit flash, death burst parts) | 8 types (tier 1–4) |
| Bosses (multi-part rigs via 2D Animation) | 3 + 3 mini variants |
| Gates (frame per kind, glass panel, value font) | 8 kinds |
| Crates + crop pickups | 4 + 5 |
| Hazards | ~6 |
| Track tilesets + side scenery per world | 3 |
| Farm map + 8 buildings × visual tiers (3 looks each) | 24 |
| Rescue animals (pen idle) | 4 species |
| UI kit (frames, buttons, icons, currency icons, badges) | 1 kit |
| Celebration videos (bg baked in) | 1 per boss per hero = up to 15 (or 1 per hero reused) |
| Store/ad creatives built from real gameplay | ongoing |

### 9.2 VFX
Hit flash (shader), bolt-and-spring burst, cog fly-to-HUD, feather/dust tumble, gate number fly, egg splat, milk spray, horseshoe trail, water splash, four Fury effects, six combo effects, boss telegraphs, stampede dust (>60).

### 9.3 Audio
- Adaptive music per world: banjo (1–20), + fiddle (21–80), + drums/brass (80+); boss variant; farm theme.
- Hero SFX: thwop / psssh + cowbell / whirr-clang / pt-pt-pt — with voice limiting (max simultaneous instances, random pitch) so 60 shooters aren't noise.
- Gates: rising chime, sad trombone, ka-ching for ×3. Robots: clanks, servo whines, "sproing". Boss taunts (VO). Barks: "BAWK!", bell, trick-spin whoosh, quack tune.
- UI clicks, rewards, timer done.

### 9.4 Game-feel checklist (acceptance criteria)
Hit flash every hit; burst every kill; count pop + wobble; number fly; shake only on boss hits/tank kills/herd losses; 0.3 s slow-mo on final boss hit; haptics light (gate), medium (loss), heavy (boss kill).

---

## 10. Monetisation, analytics, compliance

### 10.1 Rewarded ads
Revive (1/run), ×2 run Scrap, finish a timer, daily Golden Egg chest.

### 10.2 Interstitials
After level 8 only, ≥ 3 min apart, never after a loss, removed by Ad-free.

### 10.3 IAP catalogue
Starter Pack $1.99, Ad-free $4.99, Golden Egg bundles $0.99–$19.99, skins $1.99–$4.99, Harvest Pass $4.99/season (v1.2). Restore purchases (iOS requirement). Server-side receipt validation before v1.0 via a Supabase edge function (shared FarmFury backend). Product ids follow Arcade's naming pattern with a `rampage` prefix.

### 10.4 Analytics events (names must match the dashboard exactly — Arcade lesson)
`level_start`, `level_complete`, `level_failed`, `gate_chosen` (kind, value, herd_before/after), `revive_offered`/`revive_used`, `fury_used`, `combo_discovered`, `rescue`, `building_upgrade`, `crop_planted`, `ad_shown`, `purchase`, `tutorial_step`, `session_length`. Dev builds log every event (Arcade's `RECORDED`/`DROPPED` pattern).

### 10.5 Compliance
- **Every player is treated as child-directed** (Arcade policy): no personalised ads for anyone, `is_child_directed`/`is_deviceid_optout` LevelPlay metadata **before** `Init()` plus the dashboard "targeted to children" toggle (Arcade lesson: both are needed). No age gate.
- Arithmetic parental gate before every purchase; Families policy (Google Play) and Kids/age-rating questionnaire (App Store); COPPA, GDPR-K. Checked at the gate before Soft launch.
- Privacy policy, terms and data-deletion path on farmfurygames.com (Play requirement), alongside the Arcade pages.

---

## 11. Performance and rendering

### 11.1 Budgets (mid-range 2021 Android, e.g. Galaxy A52 / Redmi Note 10 class)
| Metric | Target |
|---|---|
| Frame rate | 60 fps sustained, worst frame < 25 ms |
| Entities | 300 herd (60 drawn), 400 projectiles, 80 robots |
| Draw calls | < 60 |
| Sim CPU | < 4 ms/frame |
| GC alloc in run | 0 B/frame |
| Install | < 150 MB (Addressables for worlds 2+ and videos) |
| Cold start | < 5 s |
| Memory | < 600 MB |

### 11.2 Approach
- Herd and projectiles: **instanced quads** (`Graphics.RenderMeshInstanced`) from one atlas material; per-instance UV/frame + tint via MaterialPropertyBlock arrays. Not 400 SpriteRenderers.
- Robots, gates, pickups: pooled SpriteRenderers with SRP Batcher, one atlas per world.
- Hit detection: uniform spatial hash in a Burst job (cell ≈ 1 m); no Physics2D in the run.
- Y-sorting via the sort axis on the 2D renderer; herd drawn in one layer.
- WebGL: Burst/Jobs run single-threaded; acceptable for playtests; Ads/IAP stubbed.
- Performance test in CI on the Bench scene (editor timing) plus a manual on-device check each milestone.

---

## 12. Testing, tools and CI

### 12.1 Edit-mode tests
- Gate maths (all ops, rounding, clamps, red→blue flip, Tiller ploughing, improve cap).
- Damage, DPS parity per hero (analytic), status effects, combo resolution matrix.
- Formation bounds (no slot outside track at any count, any steer X).
- Economy: transactions atomic, can't go negative, costs from data.
- Save migrations and round-trip.
- Remote Config overlay applies only to runtime copies.
- **No-literal check:** a Roslyn analyzer or test that flags numeric literals in `Sim`/`Run` gameplay code outside allowed constants.

### 12.2 LevelDef validator (FarmFury lesson)
For every `LevelDef`: all events inside track bounds; distances increasing and inside level length; gate rows don't overlap hazards; crop slots reachable; boss present on boss levels; difficulty sawtooth holds across worlds; seed set. Fails the build.

### 12.3 Play-mode / simulation tests
- **Hero balance:** herd of 20 × 5 reference waves × 4 heroes; time-to-clear within ±5% (runs headless on `RunSim`).
- **Combo parity:** each lead+guest pair within ±5%.
- **Level clearability:** a bot player (greedy gate chooser, centre-steer) must clear each level at the expected Herd Level.
- **Economy simulator** (§6.4).
- **Bench:** frame-time with 300/400/80.

### 12.4 Editor tools
- **Level Editor window:** track lane view in `(x, d)`, drag events, timeline scrubber, simulate-to-here using `RunSim`, validation errors inline.
- **Wave/gate palettes** for fast authoring; "duplicate level as template".
- **Tuning dashboard:** table of all Defs, export/import CSV for spreadsheet balancing.
- **Remote Config key exporter.**
- **Run replay viewer** (seed + input log).
- Batch-mode test command: `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml` (and PlayMode).

### 12.5 CI/CD
Same split as Arcade:
- **Tests:** GitHub Actions on push → edit-mode + play-mode + LevelDef validator + balance tests (batch mode). Locally, Claude Code runs the same batch-mode command before each commit.
- **Android:** local Editor builds, tested on a USB phone via adb (Arcade's "Android device testing" flow); release AAB signed with the line's keystore process.
- **iOS:** Unity Build Automation (no Mac). **Bump `buildNumber.iPhone` before every Cloud Build** (Arcade lost two runs to this). Reuse Arcade's `GADApplicationIdentifier` setup and Cloud Build scripts.
- **Web:** WebGL playtest build published like Arcade's web demo (farmfurygames.com/play).

---

## 13. Schedule (solo developer + Claude Code, ~30 weeks)

Each phase ends at a gate; no phase starts until its gate passes. Each feature follows the GDD §13 cycle: plan → build (C# + SO + test) → batch-mode test → playtest → tune in data → one commit.

### Phase 1 — Prototype (Weeks 1–2)
| WP | Work |
|---|---|
| 1.1 | ✅ Repo, `.gitignore`, CLAUDE.md, docs, Unity 6000.5.6f1 project (converted to URP 2D), packages, asmdefs, portrait/IL2CPP/ARM64 settings. **Pending:** GitHub Actions test CI (needs a Unity licence secret in the repo) (§2) |
| 1.2 | ✅ (2026-10-09, tests pass; hand-playtest pending) Unity greybox (C6): first `RunSim` + placeholder views — track scroll, drag steer, herd count, gates (+/−/×/÷), Bolt Walker + Buzz Drone, Cluck |
| 1.3 | Playtest the adopted G1–G7 defaults; adjust numbers in data; record in a tuning sheet |
| 1.4 | 2D crowd art test: 60 Cluck backs at phone scale (silhouette readability) |
**Gate:** testers replay without being asked. Tuning sheet becomes first SO values.

### Phase 2 — Vertical slice (Weeks 3–8)
| Week | Work |
|---|---|
| 3 | `Data` Defs (Hero, Robot, Gate, Level, Wave, GameTuning); `RunSim` skeleton with fixed tick, seeded RNG; track coordinates; LevelDef validator test |
| 4 | Herd sim + phyllotaxis + drawn-slot logic; instanced herd renderer; input; projectile pool + spatial hash (Burst) |
| 5 | All 4 heroes (patterns, traits, beam, boomerang); Fury meter + 4 Fury abilities; **balance test passing** |
| 6 | Gates complete (incl. buff, fury, guest, shoot-to-improve); crates; robots tiers 1–2; bite/losses; fail/revive (stub); stars |
| 7 | Boss framework + Harvex-9000; Level Editor window v1; 5 levels authored; HUD (uGUI); results screen (basic) |
| 8 | Game feel pass (§9.4), adaptive music v1, Bench scene + on-device perf pass, Android dev build |
**Gate:** 60 fps on the reference phone with 300/400/80; balance test passes in CI.

### Phase 3 — World 1 MVP (Weeks 9–16)
| Week | Work |
|---|---|
| 9 | Services layer: Save (JSON, versioned), Economy, Progress, Config (local), Time, Scene, Audio, Haptics; Boot scene |
| 10 | Farm scene: farm map, FarmManager, 8 building slots, upgrade sheets, timers, Training Barn → Herd Level |
| 11 | Fields + CropDef + crop pickups in runs (the designed cross-scene link); Windmill offline income |
| 12 | Guest gates → ComboResolver + 6 combos + Recipe Book; combo parity test |
| 13 | Rescues: cages, Rescue Pen, Percy as first unlock; Workshop + Shrine effects |
| 14 | World map, level card, hero picker with test-fire preview, settings + all accessibility options; port Stampede UIKit/overlays and reskin |
| 15 | Author World 1 (20 levels incl. L5 twist, L10 mini-boss, L20 boss); FTUE scripted run + intro cutscene; just-in-time tutorials |
| 16 | Economy simulator + tuning pass; Endless mode (chunk-based) if on track; localisation scaffolding; store-readiness pass |
**Gate:** store-ready build; compliance checklist complete (§10.5).

### Phase 4 — Soft launch (Weeks 17–22)
| Week | Work |
|---|---|
| 17 | Port Arcade `AdManager` (LevelPlay + AdMob, child-directed) and `IAPManager`/`Store` + `ParentalGate` behind the service interfaces; rewarded + interstitial rules; shop, Starter Pack, Ad-free |
| 18 | Supabase identity + cloud save (shared schema), receipt-validation edge function; Remote Config overlay; Analytics events + dashboard registration |
| 19 | World 2 (Rust Hound, Shield Bot, laser fences, Mega Milker) |
| 20 | Daily Ad Challenge + Ad Challenges list; Endless leaderboard; notifications |
| 21 | Soft launch Android in 1–2 countries; monitor crashes/ANRs, funnels |
| 22 | Tune via Remote Config (gate values, robot HP, ad frequency); fix top FTUE drop-offs |
**Gate:** D1 ≥ 40%, D7 ≥ 15% (validate these benchmarks against current market data first).

### Phase 5 — Global launch (Weeks 23–30)
| Week | Work |
|---|---|
| 23–25 | World 3 (Tiller Tank, Seeder Bot, crushers/conveyors, Silo Spider) |
| 26–27 | iOS via Unity Build Automation (bump build number each run), App Store compliance (Kids category decision, restore purchases, privacy labels), iOS perf pass |
| 28 | Live-ops tooling: Harvest Festival event framework, first themed robot |
| 29 | Localisation of launch languages; store listings; creatives from real gameplay (honest ads) |
| 30 | Global launch iOS + Android |

### Post-launch roadmap
- v1.1: Weather, Hack gate, Hay wagon ride (+ night/hay-wagon twist levels), World 4.
- v1.2: Ghost herds, Harvest Pass, World 5 + Tractor Titan, Farm Raid.
- Quarterly: new world or rescued hero; monthly Harvest Festival.

---

## 14. Risks

| Risk | Mitigation |
|---|---|
| Crowded genre / looks like an ad clone | Lead marketing with crops-on-track, combos, robot-turning |
| 300-animal herd too slow on low-end phones | 60 drawn cap, instanced rendering, Bench gate before Phase 3 |
| Equal heroes feel samey | Strong feel/sound/Fury differences; combos reward mixing |
| Child-audience ad/data rules | Everyone child-directed (Arcade policy), parental gate, compliance gate |
| Scope creep from the meta | 8 buildings, no PvP/alliances, Farm Raid deferred (C3) |
| Level data bugs (FarmFury L02–L06) | Track-coordinate-only authoring + validator in CI |
| **Repo size without LFS** (new) | Compress audio (OGG), videos ≤ 720p, source art outside the repo |
| **100 levels in 30 weeks solo** (new) | Launch with 60 (C1); Level Editor + templates; bot-clearability test catches broken levels early |
| **Shoot-to-improve breaks at high herd counts** (new) | G1 cap; tested in Prototype |
| **Economy too fast/slow** (new) | Economy simulator + Remote Config |
| **Clock cheating on timers** (new) | `ITimeService` trusted time |
| **WebGL lacks Ads/IAP/threads** (new) | Service fakes; WebGL used only for playtests |
| **Shared Supabase backend not built yet** (new) | Local JSON save works alone; cloud save is additive in Phase 4; keep the schema compatible with Arcade/Stampede |
| **Ported Arcade/Stampede code assumes singletons** (new) | Wrap ported managers inside service implementations; don't import their `GameManager` coupling |
