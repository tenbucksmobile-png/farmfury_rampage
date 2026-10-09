# Drop-in art for FarmFury: Rampage

Drop PNGs (transparent background) into these folders. Unity imports them as sprites and wires them into the game
automatically by folder and file name. Anything missing stays a greybox shape, so art can arrive piece by piece.
Re-run by hand with **FarmFury Rampage > Art > Assign Art From Folders**.

The game sizes everything in metres itself, so exact pixel sizes don't matter. Keep the canvas tight around the
subject and **every frame of an animation on the same canvas size** with the subject in the same place.

Camera: portrait, three-quarter top-down. The herd runs **up** the screen (we see the animals from **behind**);
robots roll **down** toward them (we see robots from the **front**). Look: warm hand-painted farm vs cold chrome robots.

| Folder | Files | What | Suggested canvas |
|---|---|---|---|
| `Heroes/Cluck/` | `run_00.png`, `run_01.png`, … (4–8 frames, loop) | Cluck from **behind**, running away from camera. Strong silhouette (comb, bandana) so 60 of them still read as chickens. | 256 x 256 |
| `Heroes/Cluck/` | `egg.png` | The thrown egg grenade | 128 x 128 |
| `Robots/BoltWalker/` | `walk_00.png`, … (2–6 frames) **or** any number of single-image looks (any other file name) | Basic horde grunt from the **front**, white eyes (tier 1). Packed shoulder to shoulder, so keep it roughly as wide as it is tall. | 256 x 256 |
| `Robots/TillerTank/` | `walk_00.png`, … | Heavy boss tank from the front, orange eyes (tier 3). Drawn about 2.6x a Bolt Walker. | 512 x 512 |
| `Robots/BuzzDrone/` | `walk_00.png`, … | Small flying drone swarm unit, white eyes | 256 x 256 |
| `Effects/` | `blast.png` | Egg-grenade explosion (seen from above). It is scaled up and faded out. | 512 x 512 |
| `Effects/` | `feather*.png` (e.g. FeatherBurst.png) | Puff where animals are knocked out of the herd | 512 x 512 |
| `Track/` | `ground.png` | Track ground, **tiles seamlessly top-to-bottom**. Spans the whole screen width: the middle 90% is the field the herd runs on, ~5% each side is verge/fence. | 1080 x 1080 or taller |
| `Gates/` | `add.png`, `subtract.png`, `multiply.png`, `divide.png` | Gate panel frames (wooden frame + glass). Leave the centre clear — the game draws the number. Colour **and** frame shape must differ (blue +/x, red -/÷). | 512 x 200 |

Robot folders: files named `walk_*` are animation frames of one robot; every other file is a separate **variant** look, and each robot in the horde gets one of them (a mixed horde). Single-image robots bob as they roll.

Priority: Cluck + egg → Bolt Walker → ground → blast → Tiller Tank → Buzz Drone → gates.

Already usable from existing FarmFury art: `FarmFury_Artwork/Rough_Effects/Explosion.png` (rename to `Effects/blast.png`).
Most platformer character and robot art is side-on, but `Rough_Characters/Cluck/Cluck_back.png` (and `Cluck_front.png`) are the right angles: use them as the Kling reference image for Cluck. `Rough_Characters/Robot/DriftRobot_Front.png` shows the treaded-robot idea from the front.

Per-asset tuning (Inspector): `HeroDef.artScale` / `frameRate`, `RobotDef.artScale` / `frameRate`.

Kling AI prompts for every item above (and the full launch set): `Docs/Kling_Art_Prompts.md` in the repo root.
