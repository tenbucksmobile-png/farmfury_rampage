# FarmFury: Rampage — Kling AI art prompts

Every image the game needs, as copy-paste prompts. **Part A is what the current build uses** (drop the results into
`Assets/_Project/Art/` — see `Assets/_Project/Art/README.md`). Parts B–I are the rest of the launch set (Worlds 1–3,
farm meta, UI, store), in rough build order.

---

## 0. How to get game-ready sprites out of Kling

1. **Style lock.** Paste the *Style block* (below) at the start of every prompt and the *Negative prompt* into Kling's
   negative-prompt field. Generate several variations and keep the best.
2. **Character consistency.** For Cluck, upload `FarmFury_Artwork/Rough_Characters/Cluck/Cluck_back.png` (and
   `Cluck_front.png`) as the **reference image**. Do the same for any later hero once you have one good image of it.
3. **Clean background.** Kling doesn't output transparency. Every prompt asks for a *plain flat background*; remove it
   in Photopea (or remove.bg) and export PNG with transparency. Crop tight, keep the subject centred.
4. **Animations (run / walk loops).**
   - Generate the still first (the *Still* prompt).
   - Then **Kling Video → Image to Video** with that still and the *Motion* prompt (5 s, static camera).
   - Extract frames: `ffmpeg -i run.mp4 -vf "fps=8" run_%02d.png`, pick one clean loop (4–8 frames where the last
     flows into the first), remove the background from each, and **put every frame on the same canvas size** with the
     subject in the same spot.
   - Name them `run_00.png, run_01.png, …` / `walk_00.png, …` as in the Art README.
5. **Angles matter most.** Camera is behind and above the herd. **Heroes = seen from behind** (running away from
   camera). **Robots = seen from the front** (rolling toward camera). Both at a three-quarter top-down angle.
6. Recommended Kling aspect ratio is given per item (`1:1` for most sprites).

### Style block (prefix every prompt)

```
Glossy 3D cartoon render in the style of a modern family animated film, soft studio lighting, smooth rounded shapes,
saturated warm colours, clean readable silhouette, mobile game asset, single subject centred, full body in frame,
plain flat light grey background, no shadow on the background,
```

### Negative prompt (every image)

```
text, letters, watermark, logo, signature, frame, border, multiple subjects, cropped, cut off, blurry, noisy,
photorealistic, gore, blood, scary, horror, busy background, scenery, ground shadow, drop shadow, vignette
```

Colour rules (from the GDD): farm side = straw yellow, barn red, grass green, sky blue. HARVEX robots = steel grey,
chrome and warning orange, with **eye colour by tier**: white (1), yellow (2), orange (3), red (4), purple (boss).

---

## Part A — Current build (prototype) — make these first

### A1. Cluck, running, from behind → `Art/Heroes/Cluck/run_00…07.png` · 1:1 · use Cluck_back.png as reference

Still:
```
[Style block] Cluck the cartoon chicken seen from directly behind and slightly above, three-quarter top-down view,
running away from the camera mid-stride, round orange body, yellow head, bright red comb, small wings pumping,
orange legs and feet, tiny red bandana knot visible at the back of the neck, brave and determined energy, same
character as the reference image
```
Motion (Kling Video, image-to-video, 5 s):
```
The chicken runs on the spot away from the camera in a fast cheerful loop, legs pumping, wings flapping, body
bobbing, camera completely static, background stays plain grey, no camera movement, seamless loop
```

### A2. Egg grenade → `Art/Heroes/Cluck/egg.png` · 1:1

```
[Style block] A single cartoon chicken egg used as a grenade, cream-white glossy shell with a small cartoon lit fuse
spark on top and a cheeky crack line, slightly tilted, three-quarter top-down view, plain flat mid-blue background
```

### A3. Bolt Walker (horde grunt), front → `Art/Robots/BoltWalker/walk_00…05.png` · 1:1

Still:
```
[Style block] A small boxy cartoon farm-invader robot seen from the front and slightly above, three-quarter top-down
view, rolling toward the camera on chunky caterpillar treads, steel grey and chrome body with warning-orange hazard
stripes, square head with two glowing WHITE round eyes and an angry brow plate, stubby arms, HARVEX corporate badge
on the chest, roughly as wide as it is tall, cute but menacing, toy-like
```
Motion:
```
The robot rolls forward toward the camera on the spot, treads turning, body jittering slightly, arms swinging,
eyes flicker, camera static, plain grey background, seamless loop
```

### A4. Tiller Tank (boss inside the horde), front → `Art/Robots/TillerTank/walk_00…05.png` · 1:1

Still:
```
[Style block] A big heavy cartoon robot tank seen from the front and slightly above, three-quarter top-down view,
wide armoured body on huge treads, a giant rotating plough-tiller drum with spiky blades across the front, steel grey
and dented chrome armour plates with warning-orange stripes and rivets, a small cockpit head with two glowing ORANGE
eyes, exhaust pipes puffing, HARVEX logo plate, imposing boss presence but still family-friendly cartoon
```
Motion:
```
The tank rolls slowly toward the camera on the spot, tiller drum spinning, treads turning, exhaust puffs, heavy
rocking motion, camera static, plain grey background, seamless loop
```

### A5. Buzz Drone, front → `Art/Robots/BuzzDrone/walk_00…03.png` · 1:1

Still:
```
[Style block] A tiny round cartoon flying robot drone seen from the front and slightly above, three-quarter top-down
view, chrome ball body with a single big glowing WHITE eye, four small spinning rotor arms, warning-orange stripe,
little antenna, buzzing and cheeky
```
Motion:
```
The drone hovers and wobbles in place, rotors spinning fast, eye blinking, camera static, plain grey background,
seamless loop
```

### A6. Egg blast → `Art/Effects/blast.png` · 1:1

```
[Style block] A cartoon explosion seen from directly above, round burst of bright yellow and orange fire puff with
white egg-yolk splats, eggshell fragments and little springs and bolts flying outward, comic "poof" style, no smoke
column, circular overall shape, plain flat dark grey background
```
(Alternative: reuse `FarmFury_Artwork/Rough_Effects/Explosion.png`, renamed `blast.png`.)

### A7. Track ground → `Art/Track/ground.png` · 9:16

```
[Style block] Seamless top-down view of a farm dirt track for a vertical scrolling game, straight ploughed field
running from the bottom to the top of the image, warm brown soil with subtle furrow lines, scattered small pebbles and
grass tufts, a narrow strip of green grass verge with a low wooden fence along the left and right edges, even lighting,
no objects in the middle, top and bottom edges tile seamlessly
```
Tip: if the top/bottom don't tile, use Photopea *Filter → Other → Offset* (half the height) and clone out the seam.

### A8. Gate frames → `Art/Gates/add.png`, `multiply.png`, `subtract.png`, `divide.png` · 16:9

Add (+):
```
[Style block] A wide wooden farm gate panel seen from slightly above, chunky rounded-arch wooden frame with a glowing
BLUE glass pane in the middle, the centre of the glass left completely empty and clear, friendly and inviting,
nails and rope details, plain flat light grey background
```
Multiply (×):
```
[Style block] A wide wooden farm gate panel seen from slightly above, extra-thick double wooden frame with golden corner
brackets and a glowing BLUE glass pane, small sparkles on the frame, the centre of the glass left completely empty,
plain flat light grey background
```
Subtract (−):
```
[Style block] A wide wooden farm gate panel seen from slightly above, jagged splintered wooden frame with metal spikes
and a glowing RED glass pane, cheeky warning feel, the centre of the glass left completely empty, plain flat light
grey background
```
Divide (÷):
```
[Style block] A wide wooden farm gate panel seen from slightly above, cracked wooden frame held together with metal
bands and bolts, a glowing RED glass pane with a crack across one corner, the centre of the glass left completely
empty, plain flat light grey background
```

---

## Part B — The other heroes (seen from behind) + their shots

Same method as A1 (still → image-to-video → frames). Run frames go in `Art/Heroes/<Name>/run_*.png`.

### B1. Bessie (cow) · 1:1
```
[Style block] Bessie the cartoon dairy cow seen from directly behind and slightly above, three-quarter top-down view,
trotting away from the camera, chubby black-and-white patched body, two small curved horns, a big brass cowbell on a
red collar visible at the neck, swishing tail, calm but unstoppable energy
```
Shot — milk splash `milk.png`:
```
[Style block] A short burst of white milk stream with round droplets, glossy, top-down view, plain mid-blue background
```

### B2. Horace (horse) · 1:1
```
[Style block] Horace the proud cartoon show horse seen from directly behind and slightly above, three-quarter
top-down view, galloping away from the camera, chestnut brown coat, flowing blond mane and tail with a blue ribbon,
shiny hooves, proud posture
```
Shot — horseshoe `horseshoe.png`:
```
[Style block] A single shiny silver horseshoe spinning, motion streak behind it, top-down view, plain mid-blue background
```

### B3. Ducky (duck) · 1:1
```
[Style block] Ducky the small cheeky cartoon duckling seen from directly behind and slightly above, three-quarter
top-down view, waddle-running away from the camera, bright yellow fluffy body, a little tuft of feathers on top of the
head, orange webbed feet, tiny flapping wings, fast and twitchy energy
```
Shot — water glob `water.png`:
```
[Style block] A single round glossy blue water glob with a small splash trail, top-down view, plain light grey background
```

### B4. Percy (pig, first rescue hero) · 1:1
```
[Style block] Percy the cartoon piglet seen from directly behind and slightly above, three-quarter top-down view,
running away from the camera, round pink body, curly tail, floppy ears, a little mud on the legs, plucky energy
```
Shot — mud bomb `mud.png`:
```
[Style block] A round cartoon mud ball with drips and a squelchy splat edge, top-down view, plain light grey background
```

### B5. Hero portraits (front, for UI / picker / results) · 1:1 — one per hero
```
[Style block] <Hero name and description from B1–B4 / Cluck> seen from the front, waist-up heroic pose, confident
smile, looking at the camera, bright rim light
```

### B6. Herd-count badge icons (hero heads) · 1:1 — one per hero
```
[Style block] Simple head-only icon of <hero> facing the camera, bold silhouette, readable at very small size
```

---

## Part C — Robots for Worlds 1–3 (front view) and bosses

Robots: still + walk loop like A3, folder `Art/Robots/<NameNoSpaces>/`.

### C1. Rust Hound (tier 2, yellow eyes) · 1:1
```
[Style block] A robot dog seen from the front and slightly above, three-quarter top-down view, lean chrome and steel
body with rusty orange patches, pointy metal ears, wagging antenna tail, two glowing YELLOW eyes, running toward the
camera on four spring legs, playful but fast
```

### C2. Shield Bot (tier 2, yellow eyes) · 1:1
```
[Style block] A sturdy cartoon robot seen from the front and slightly above, three-quarter top-down view, holding a
big round riveted riot shield in front of its body, steel grey and warning orange, two glowing YELLOW eyes peeking over
the shield, rolling toward the camera on treads
```

### C3. Seeder Bot (tier 3, orange eyes) · 1:1
```
[Style block] A round pot-bellied cartoon robot seen from the front and slightly above, three-quarter top-down view,
body shaped like a seed hopper with three tiny drones docked on its back, chrome and orange, two glowing ORANGE eyes,
rolling toward the camera
```

### C4. Scare-Bot (tier 4, red eyes — World 4, later) · 1:1
```
[Style block] A tall scarecrow-shaped robot sniper seen from the front and slightly above, straw-stuffed metal frame,
pumpkin-shaped metal head with two glowing RED eyes, a zapper rod in its hands, standing still, comical not scary
```

### C5. Fix-It Bot (tier 4, red eyes — World 5, later) · 1:1
```
[Style block] A small medic repair robot seen from the front and slightly above, wrench and welding arms, green cross
light on its chest, chrome and orange, two glowing RED eyes, rolling toward the camera
```

### C6. Boss — Harvex-9000 Combine (World 1) · 1:1 (and a damaged variant)
```
[Style block] A giant cartoon combine-harvester robot boss seen from the front and slightly above, three-quarter
top-down view, enormous spinning reel of blades at the front, a big grain tank on top that can open (glowing
weak point inside), chrome and warning orange with HARVEX branding, two huge glowing PURPLE eyes in the cab windows,
smoke stacks, menacing but family friendly
```
Variants: `…with the grain tank lid open showing a glowing purple core` · `…battered, dented and smoking, about to break`.

### C7. Boss — Mega Milker (World 2) · 1:1
```
[Style block] A giant cartoon milking-machine robot boss seen from the front and slightly above, round steel tank
body, four long flexible suction hoses reaching forward (each a separate breakable part), pressure gauges, chrome and
orange, glowing PURPLE eyes, silly and greedy look
```

### C8. Boss — Silo Spider (World 3) · 1:1
```
[Style block] A cartoon grain-silo robot boss seen from the front and slightly above, a tall metal silo body walking on
six long mechanical spider legs, hatch doors dropping small robots, chrome and orange, glowing PURPLE eyes on the dome,
stomping
```

### C9. Mini-boss variants (level 10 of each world)
Re-use C6–C8 prompts with: `smaller, half-size prototype version, less armour, one weapon`.

### C10. Robot death burst — `Art/Effects/burst.png` · 1:1
```
[Style block] Cartoon robot breaking apart seen from above, a pop of springs, bolts, nuts and little brass cogs flying
outward, comic "sproing" energy, no fire, no gore, plain flat dark grey background
```

---

## Part D — Pickups, crates and rescues (seen from slightly above) · 1:1 each

```
[Style block] A round golden hay bale crate pickup, three-quarter top-down view
[Style block] A burlap feed sack tied with rope, three-quarter top-down view
[Style block] A dented metal milk churn, three-quarter top-down view
[Style block] A wooden crate full of eggs with straw, three-quarter top-down view
```
Crop pickups (from the farm fields):
```
[Style block] A glowing orange pumpkin bomb with a short lit fuse, three-quarter top-down view
[Style block] A red-and-white striped popcorn crate overflowing with popcorn, three-quarter top-down view
[Style block] A cheerful sunflower turret mounted on a small wooden base, three-quarter top-down view
[Style block] A big hay bale on its side ready to roll, three-quarter top-down view
[Style block] A carrot shaped like a cartoon rocket with a little flame at the back, three-quarter top-down view
```
Rescue cages (carried by robots):
```
[Style block] A small metal cage with a worried cartoon <piglet | lamb | baby goat | gosling> inside, padlock, three-quarter top-down view
```

---

## Part E — Hazards (Worlds 1–3), top-down · 1:1

```
[Style block] A cartoon pothole dug by a robot in a dirt track, top-down view, broken soil edges, plain dark grey background
[Style block] A horizontal robot laser fence across a track, two metal posts with a glowing red laser beam between them, top-down view
[Style block] A big industrial crusher piston slamming down, seen from above at three-quarter angle, steel with hazard stripes
[Style block] A section of factory conveyor belt with arrows, seen from directly above, seamless vertical tiling
```

---

## Part F — Effects · 1:1, plain dark grey background

```
[Style block] A cartoon puff of chicken feathers flying outward, top-down view
[Style block] A small cartoon dust cloud puff, top-down view
[Style block] A yellow egg-yolk splat on the ground, top-down view
[Style block] A single shiny brass cog coin, slightly tilted (Scrap currency)
[Style block] Sticky yellow custard splat dripping (combo effect)
[Style block] A small cartoon steam cloud puff (combo effect)
[Style block] An orange-brown rust patch splatter (combo effect)
[Style block] A spinning tornado ring of silver horseshoes seen from above (Horace Fury)
[Style block] A dark cartoon storm cloud with lightning and rain seen from above (Ducky Fury)
[Style block] A rolling white milk wave seen from above (Bessie Fury)
[Style block] Raining eggs falling from the sky seen from above (Cluck Fury)
```

---

## Part G — World tracks and scenery (Worlds 1–3) · 9:16, seamless top-to-bottom

Use the A7 prompt, swapping the middle sentence:
```
World 1 Fury Fields: ploughed brown farm field track with green verges and wooden fences
World 2 Dairy Valley: lush green pasture track with daisies, stone walls along the edges, milking sheds in the far verge
World 3 Grain Silos: dusty golden wheat-stubble track with metal grain-silo bases and conveyor rails along the edges
```
Side props (1:1 each, for verges): `[Style block] A <wooden fence section | haystack | small tree | milk shed | grain silo | windmill>, three-quarter top-down view`

---

## Part H — Fury Farm (meta game)

### H1. Farm map background · 9:16
```
[Style block] A cosy hand-crafted farm seen from a high three-quarter top-down angle, empty grassy plots ready for
buildings, dirt paths connecting them, a pond, trees around the edges, warm afternoon light, no buildings, no text,
background fills the whole image
```

### H2. Buildings · 1:1 — three looks each: *wrecked by robots*, *repaired*, *upgraded*
```
[Style block] A <Farmhouse | Training Barn | Workshop | Lucky Horseshoe Shrine | Rescue Pen | Windmill | Scarecrow Tower>
on a farm, three-quarter top-down view, <wrecked and flattened by robots, broken planks | freshly repaired, clean |
upgraded, bigger, shiny, flags and lights>
```
Fields (4 plots): `[Style block] A small square farm field plot, three-quarter top-down view, <empty soil | sprouting seedlings | fully grown <pumpkins | corn | sunflowers | hay | carrots>>`

---

## Part I — UI, icons and store

### I1. Logo · 16:9
```
Game logo for "FarmFury: Rampage", chunky bold cartoon lettering in straw yellow with barn-red outline and wooden
plank texture, a tiny chicken comb on the F, cracked egg and spring bolts around it, transparent-ready plain grey background
```
(Kling often garbles text: generate the badge/shape without text, then add the lettering in Photopea if needed.)

### I2. Currency icons · 1:1
```
[Style block] Game icon of a shiny brass cog (Scrap)
[Style block] Game icon of a small basket of vegetables (Produce)
[Style block] Game icon of a blue-and-gold show rosette ribbon (Rosettes)
[Style block] Game icon of a glowing golden egg (premium Golden Eggs)
```

### I3. Buttons and panels · 16:9 / 1:1
```
[Style block] A big chunky wooden plank game button with rope ties, empty centre for text, slightly 3D press edge
[Style block] A round wooden game button, empty centre for an icon
[Style block] A round glowing purple "Fury" power button with an egg-and-lightning emblem, no text
[Style block] A wooden sign panel for game menus, empty centre for text, nails in the corners
```

### I4. Store / ad key art · 9:16 and 16:9
```
[Style block] Epic cartoon key art: a stampede of dozens of chickens seen from behind charging up a farm track toward a
massive packed horde of chrome robots with glowing eyes, eggs exploding in the robot ranks, a giant combine-harvester
robot boss looming at the top, golden sunset sky, dynamic, exciting, family friendly, no text
```
(GDD "honest ads": store art must show the game as it actually plays — use real gameplay captures alongside this.)

---

## Checklist (Part A = needed now)

| # | File | Status |
|---|---|---|
| A1 | `Heroes/Cluck/run_00…` | ☐ |
| A2 | `Heroes/Cluck/egg.png` | ☐ |
| A3 | `Robots/BoltWalker/walk_00…` | ☐ |
| A4 | `Robots/TillerTank/walk_00…` | ☐ |
| A5 | `Robots/BuzzDrone/walk_00…` | ☐ |
| A6 | `Effects/blast.png` | ☐ |
| A7 | `Track/ground.png` | ☐ |
| A8 | `Gates/add, multiply, subtract, divide.png` | ☐ |
