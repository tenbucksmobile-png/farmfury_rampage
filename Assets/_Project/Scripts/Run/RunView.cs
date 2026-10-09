using System.Collections.Generic;
using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using Unity.Mathematics;
using UnityEngine;

namespace FarmFuryRampage.Run
{
    /// <summary>
    /// Draws the run: hero, robot, effect and gate art where it has been assigned (see <see cref="RunArt"/> and the
    /// art fields on <see cref="HeroDef"/>/<see cref="RobotDef"/>), greybox shapes for anything without art.
    /// One GameObject per thing for now; Phase 2 moves the herd and projectiles to instanced rendering.
    /// Reads the sim; never changes it.
    /// </summary>
    public sealed class RunView : MonoBehaviour
    {
        // Sorting orders, back to front.
        const int TrackOrder = 0;
        const int StripeOrder = 1;
        const int GateOrder = 2;
        const int RobotOrder = 4;
        const int ProjectileOrder = 5;
        const int HerdOrder = 6;
        const int LabelOrder = 8;

        // Greybox look, used wherever art is missing.
        static readonly Color TrackColor = new(0.45f, 0.62f, 0.30f);
        static readonly Color EdgeColor = new(0.36f, 0.25f, 0.15f);
        static readonly Color StripeColor = new(0.50f, 0.68f, 0.34f);
        static readonly Color FinishColor = new(1f, 1f, 1f, 0.8f);
        static readonly Color BlueGate = new(0.20f, 0.55f, 1f, 0.55f);
        static readonly Color RedGate = new(1f, 0.25f, 0.20f, 0.55f);
        static readonly Color PassedGate = new(0.5f, 0.5f, 0.5f, 0.25f);
        static readonly Color PassedGateArt = new(1f, 1f, 1f, 0.35f);
        static readonly Color ProjectileColor = new(1f, 0.97f, 0.80f);
        static readonly Color BlastColor = new(1f, 0.75f, 0.25f, 0.85f);
        static readonly Color ArmouredColor = new(0.55f, 0.08f, 0.08f);
        /// <summary>HP multiple at which an armoured robot shows the full armoured tint.</summary>
        const float FullArmourTint = 16f;
        const float StripeSpacing = 5f;
        const float EdgeWidth = 0.25f;
        const float ProjectileSize = 0.25f;
        const float EggSize = 0.35f;
        const float EggApexScale = 0.6f;
        const int BlastPoolSize = 64;
        const float BlastSeconds = 0.35f;
        const float GateLabelSize = 0.25f;
        const float CountLabelSize = 0.3f;
        const float RobotHpBarHeight = 0.12f;
        const float GreyboxAnimalSize = 0.9f;
        /// <summary>Offsets each animal's/robot's animation so a crowd doesn't move in lockstep.</summary>
        const float FramePhaseStep = 0.37f;
        /// <summary>Depth offset per metre up the screen (camera looks along +z), used for front-to-back overlap.</summary>
        const float DepthPerMetre = 0.001f;
        /// <summary>Single-image robots bob as they roll so the horde does not look frozen.</summary>
        const float RobotBobHeight = 0.05f;
        const float RobotBobSpeed = 12f;
        const int FeatherPoolSize = 32;
        const float FeatherSeconds = 0.6f;
        const float FeatherSize = 1.2f;
        const float FeatherRise = 0.8f;

        Camera cam;
        RunSim sim;
        HeroDef hero;
        RobotDef[] robotTypes;
        RunArt art;
        Font font;
        Sprite square;
        Sprite circle;

        Transform world;
        SpriteRenderer track;
        SpriteRenderer leftEdge;
        SpriteRenderer rightEdge;
        SpriteRenderer finish;
        readonly List<SpriteRenderer> stripes = new();
        readonly List<SpriteRenderer> groundTiles = new();
        readonly List<SpriteRenderer> herd = new();
        readonly List<SpriteRenderer> robots = new();
        readonly List<SpriteRenderer> robotBars = new();
        readonly List<SpriteRenderer> projectiles = new();
        readonly List<(SpriteRenderer panel, TextMesh label)> gatePanels = new();
        readonly List<SpriteRenderer> blasts = new();
        readonly float[] blastAge = new float[BlastPoolSize];
        readonly float2[] blastPosition = new float2[BlastPoolSize];
        int blastCursor;
        readonly List<SpriteRenderer> featherPuffs = new();
        readonly float[] featherAge = new float[FeatherPoolSize];
        readonly float2[] featherPosition = new float2[FeatherPoolSize];
        int featherCursor;
        TextMesh countLabel;
        float countPop;

        bool HasHeroArt => hero.runFrames != null && hero.runFrames.Length > 0;
        Sprite Ground => art != null ? art.ground : null;
        Sprite BlastSprite => art != null ? art.blast : null;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circle = Sprite.Create(MakeCircleTexture(32), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            world = new GameObject("RunWorld").transform;
            world.SetParent(transform, false);
        }

        public void Bind(RunSim run, RobotDef[] types, HeroDef runHero, RunArt runArt)
        {
            sim = run;
            hero = runHero;
            robotTypes = types;
            art = runArt;
            cam = Camera.main;

            foreach (Transform child in world) Destroy(child.gameObject);
            stripes.Clear();
            groundTiles.Clear();
            herd.Clear();
            robots.Clear();
            robotBars.Clear();
            projectiles.Clear();
            gatePanels.Clear();
            blasts.Clear();
            featherPuffs.Clear();

            RunConfig config = run.Config;
            track = MakeSprite("Track", square, TrackColor, TrackOrder);
            leftEdge = MakeSprite("EdgeL", square, EdgeColor, StripeOrder);
            rightEdge = MakeSprite("EdgeR", square, EdgeColor, StripeOrder);
            finish = MakeSprite("Finish", square, FinishColor, StripeOrder);

            if (Ground == null)
            {
                int stripeCount = (int)math.ceil(config.track.viewWidth * 3f / StripeSpacing) + 2;
                for (int i = 0; i < stripeCount; i++) stripes.Add(MakeSprite("Stripe", square, StripeColor, StripeOrder));
            }

            Sprite animal = HasHeroArt ? hero.runFrames[0] : circle;
            Color animalColor = HasHeroArt ? Color.white : hero.greyboxColor;
            for (int i = 0; i < config.herd.drawnCap; i++)
                herd.Add(MakeSprite("Animal", animal, animalColor, HerdOrder));

            Sprite shot = hero.projectileSprite != null ? hero.projectileSprite : circle;
            Color shotColor = hero.projectileSprite != null ? Color.white : ProjectileColor;
            for (int i = 0; i < run.Projectiles.Length; i++)
                projectiles.Add(MakeSprite("Shot", shot, shotColor, ProjectileOrder));

            foreach (GateRowState row in run.GateRows)
                for (int p = 0; p < row.panels.Length; p++)
                    gatePanels.Add((MakeSprite("Gate", square, BlueGate, GateOrder), MakeLabel("GateLabel", GateLabelSize)));

            Sprite blast = BlastSprite != null ? BlastSprite : circle;
            for (int i = 0; i < BlastPoolSize; i++)
            {
                blasts.Add(MakeSprite("Blast", blast, BlastColor, ProjectileOrder + 1));
                blasts[i].enabled = false;
                blastAge[i] = BlastSeconds;
            }

            Sprite feathers = art != null ? art.feathers : null;
            for (int i = 0; i < FeatherPoolSize && feathers != null; i++)
            {
                featherPuffs.Add(MakeSprite("Feathers", feathers, Color.white, HerdOrder + 1));
                featherPuffs[i].enabled = false;
                featherAge[i] = FeatherSeconds;
            }

            countLabel = MakeLabel("HerdCount", CountLabelSize);
            countPop = 0f;
        }

        public void OnEvents(IReadOnlyList<RunEvent> events)
        {
            foreach (RunEvent e in events)
            {
                if (e.type == RunEventType.GatePassed || e.type == RunEventType.AnimalsLost)
                    countPop = 1f;
                if (e.type == RunEventType.AnimalsLost && featherPuffs.Count > 0)
                {
                    // Knocked-out animals tumble away in a puff of feathers where the robot hit the herd.
                    featherAge[featherCursor] = 0f;
                    featherPosition[featherCursor] = new float2(e.x, e.distance);
                    featherCursor = (featherCursor + 1) % featherPuffs.Count;
                }
                if (e.type == RunEventType.Explosion)
                {
                    blastAge[blastCursor] = 0f;
                    blastPosition[blastCursor] = new float2(e.x, e.distance);
                    blastCursor = (blastCursor + 1) % BlastPoolSize;
                }
            }
        }

        void LateUpdate()
        {
            if (sim == null || cam == null) return;
            RunConfig config = sim.Config;

            FrameCamera(config);
            RenderTrack(config);
            RenderHerd(config);
            RenderRobots(config);
            RenderProjectiles(config);
            RenderBlasts(config);
            RenderFeathers();
            RenderGates(config);
        }

        void RenderFeathers()
        {
            for (int i = 0; i < featherPuffs.Count; i++)
            {
                featherAge[i] += Time.deltaTime;
                float t = featherAge[i] / FeatherSeconds;
                featherPuffs[i].enabled = t < 1f;
                if (t >= 1f) continue;

                featherPuffs[i].color = new Color(1f, 1f, 1f, 1f - t * t);
                float y = featherPosition[i].y - sim.HerdDistance + FeatherRise * t;
                PlaceFit(featherPuffs[i], featherPosition[i].x, y, FeatherSize * (0.6f + 0.4f * t));
            }
        }

        void FrameCamera(RunConfig config)
        {
            cam.orthographic = true;
            float aspect = math.max(0.01f, cam.aspect); // follows the screen, or a render target when capturing
            cam.orthographicSize = config.track.viewWidth / aspect * 0.5f;
            float centreAboveHerd = (0.5f - config.track.herdScreenY) * cam.orthographicSize * 2f;
            cam.transform.position = new Vector3(0f, centreAboveHerd, -10f);
        }

        void RenderTrack(RunConfig config)
        {
            float halfWidth = config.HalfWidth;
            float viewHeight = cam.orthographicSize * 2f;
            float camY = cam.transform.position.y;
            Place(finish, 0f, config.levelLength - sim.HerdDistance, config.track.width, EdgeWidth);

            Sprite ground = Ground;
            bool greybox = ground == null;
            track.enabled = leftEdge.enabled = rightEdge.enabled = greybox;
            if (greybox)
            {
                Place(track, 0f, camY, config.track.width, viewHeight + 2f);
                Place(leftEdge, -halfWidth - EdgeWidth * 0.5f, camY, EdgeWidth, viewHeight + 2f);
                Place(rightEdge, halfWidth + EdgeWidth * 0.5f, camY, EdgeWidth, viewHeight + 2f);
                float firstStripe = math.floor((sim.HerdDistance - viewHeight) / StripeSpacing) * StripeSpacing;
                for (int i = 0; i < stripes.Count; i++)
                    Place(stripes[i], 0f, firstStripe + i * StripeSpacing - sim.HerdDistance, config.track.width, EdgeWidth * 0.5f);
                return;
            }

            // Ground art: tiles stacked up the screen, scrolling with the herd. The tile spans the view width.
            Vector2 bounds = ground.bounds.size;
            float tileWidth = config.track.viewWidth;
            float tileHeight = tileWidth * bounds.y / bounds.x;
            int needed = (int)math.ceil(viewHeight / tileHeight) + 2;
            while (groundTiles.Count < needed) groundTiles.Add(MakeSprite("Ground", ground, Color.white, TrackOrder));
            float bottom = camY - viewHeight * 0.5f;
            float first = math.floor((sim.HerdDistance + bottom) / tileHeight) * tileHeight - sim.HerdDistance;
            for (int i = 0; i < groundTiles.Count; i++)
                Place(groundTiles[i], 0f, first + (i + 0.5f) * tileHeight, tileWidth, tileHeight);
        }

        void RenderHerd(RunConfig config)
        {
            int drawn = sim.DrawnCount;
            bool hasArt = HasHeroArt;
            float width = config.herd.slotSpacing * (hasArt ? hero.artScale : GreyboxAnimalSize);
            for (int i = 0; i < herd.Count; i++)
            {
                SpriteRenderer animal = herd[i];
                bool show = i < drawn;
                animal.enabled = show;
                if (!show) continue;
                if (hasArt) animal.sprite = Frame(hero.runFrames, hero.frameRate, i);
                float2 offset = Formation.SlotOffset(i, config.herd.slotSpacing);
                PlaceFit(animal, sim.HerdX + offset.x, offset.y, width);
            }

            countPop = math.max(0f, countPop - Time.deltaTime * 4f);
            countLabel.text = sim.HerdCount.ToString();
            countLabel.characterSize = CountLabelSize * (1f + countPop * 0.5f);
            countLabel.transform.position = new Vector3(sim.HerdX, sim.HerdFootprint + 0.8f, 0f);
        }

        /// <summary>Draws live robots from a pool that grows on demand (a level-long horde has far more robots than are ever alive).</summary>
        void RenderRobots(RunConfig config)
        {
            Robot[] state = sim.Robots;
            int used = 0;
            for (int i = 0; i < state.Length; i++)
            {
                Robot robot = state[i];
                if (!robot.active) continue;
                if (used == robots.Count)
                {
                    robots.Add(MakeSprite("Robot", square, Color.white, RobotOrder));
                    robotBars.Add(MakeSprite("RobotHp", square, Color.red, RobotOrder + 1));
                }

                SpriteRenderer body = robots[used];
                SpriteRenderer bar = robotBars[used];
                used++;

                RobotDef def = robotTypes[robot.type];
                bool animated = def.walkFrames != null && def.walkFrames.Length > 0;
                bool hasVariants = def.variants != null && def.variants.Length > 0;
                bool hasArt = animated || hasVariants;
                float diameter = config.robotTypes[robot.type].radius * 2f;
                float y = robot.distance - sim.HerdDistance;
                body.enabled = true;
                if (animated) body.sprite = Frame(def.walkFrames, def.frameRate, i);
                else if (hasVariants) body.sprite = def.variants[i % def.variants.Length];
                else body.sprite = square;
                float bob = !animated && hasVariants ? RobotBobHeight * math.abs(math.sin(Time.time * RobotBobSpeed + i)) : 0f;

                // Armoured robots (horde HP ramp) shade toward dark red so tougher stretches read at a glance.
                float baseHp = config.robotTypes[robot.type].hp * config.RobotHpScale;
                float armour = baseHp > 0f ? math.saturate(math.log2(math.max(1f, robot.maxHp / baseHp)) / math.log2(FullArmourTint)) : 0f;
                body.color = Color.Lerp(hasArt ? Color.white : def.greyboxColor, ArmouredColor, armour);
                if (hasArt) PlaceFit(body, robot.x, y + bob, diameter * def.artScale);
                else Place(body, robot.x, y, diameter, diameter);

                bool damaged = robot.hp < robot.maxHp;
                bar.enabled = damaged;
                if (!damaged) continue;
                float fraction = math.saturate(robot.hp / robot.maxHp);
                Place(bar, robot.x - diameter * 0.5f * (1f - fraction), y + diameter * 0.5f + RobotHpBarHeight,
                    diameter * fraction, RobotHpBarHeight);
            }

            for (int i = used; i < robots.Count; i++)
            {
                robots[i].enabled = false;
                robotBars[i].enabled = false;
            }
        }

        void RenderProjectiles(RunConfig config)
        {
            Projectile[] state = sim.Projectiles;
            bool hasArt = hero.projectileSprite != null;
            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile p = state[i];
                projectiles[i].enabled = p.active;
                if (!p.active) continue;

                float y = p.distance - sim.HerdDistance;
                if (!p.lob)
                {
                    if (hasArt) PlaceFit(projectiles[i], p.x, y, ProjectileSize);
                    else Place(projectiles[i], p.x, y, ProjectileSize, ProjectileSize);
                    continue;
                }

                // Three-quarter view: the throw's height shows as a lift up the screen and a bigger egg at the apex.
                float t = math.saturate(p.FlightFraction);
                float height = 4f * t * (1f - t);
                float size = EggSize * (1f + EggApexScale * height);
                float lift = config.hero.arcHeight * height;
                if (hasArt) PlaceFit(projectiles[i], p.x, y + lift, size);
                else Place(projectiles[i], p.x, y + lift, size, size * 1.25f);
            }
        }

        void RenderBlasts(RunConfig config)
        {
            float diameter = config.hero.blastRadius * 2f;
            bool hasArt = BlastSprite != null;
            for (int i = 0; i < blasts.Count; i++)
            {
                blastAge[i] += Time.deltaTime;
                float t = blastAge[i] / BlastSeconds;
                blasts[i].enabled = t < 1f;
                if (t >= 1f) continue;

                float size = diameter * (0.5f + 0.5f * t);
                Color color = hasArt ? Color.white : BlastColor;
                color.a *= 1f - t * t;
                blasts[i].color = color;
                float y = blastPosition[i].y - sim.HerdDistance;
                if (hasArt) PlaceFit(blasts[i], blastPosition[i].x, y, size);
                else Place(blasts[i], blastPosition[i].x, y, size, size);
            }
        }

        void RenderGates(RunConfig config)
        {
            int n = 0;
            float depth = config.gates.panelDepth;
            foreach (GateRowState row in sim.GateRows)
            {
                float panelWidth = config.track.width / row.panels.Length;
                float y = row.distance - sim.HerdDistance;
                for (int p = 0; p < row.panels.Length; p++, n++)
                {
                    (SpriteRenderer panel, TextMesh label) = gatePanels[n];
                    GatePanelState state = row.panels[p];
                    float x = -config.HalfWidth + panelWidth * (p + 0.5f);
                    bool faded = row.passed && row.chosenPanel != p;
                    Sprite gateArt = GateSprite(state.kind);
                    if (gateArt != null)
                    {
                        panel.sprite = gateArt;
                        panel.color = faded ? PassedGateArt : Color.white;
                        PlaceFit(panel, x, y, panelWidth * 0.94f);
                    }
                    else
                    {
                        panel.sprite = square;
                        panel.color = faded ? PassedGate : GateColor(state.kind);
                        Place(panel, x, y, panelWidth * 0.94f, depth);
                    }
                    label.text = GateLabel(state);
                    label.transform.position = new Vector3(x, y, 0f);
                }
            }
        }

        Sprite GateSprite(GateKind kind)
        {
            if (art == null) return null;
            return kind switch
            {
                GateKind.Add => art.gateAdd,
                GateKind.Subtract => art.gateSubtract,
                GateKind.Multiply => art.gateMultiply,
                GateKind.Divide => art.gateDivide,
                _ => null,
            };
        }

        static Sprite Frame(Sprite[] frames, float frameRate, int phaseIndex)
        {
            int frame = (int)math.floor(Time.time * frameRate + phaseIndex * FramePhaseStep);
            return frames[((frame % frames.Length) + frames.Length) % frames.Length];
        }

        static Color GateColor(GateKind kind) =>
            kind == GateKind.Add || kind == GateKind.Multiply ? BlueGate : RedGate;

        static string GateLabel(GatePanelState state) => state.kind switch
        {
            GateKind.Add => "+" + state.value,
            GateKind.Subtract => "−" + state.value,
            GateKind.Multiply => "×" + state.value,
            GateKind.Divide => "÷" + state.value,
            _ => state.value.ToString(),
        };

        SpriteRenderer MakeSprite(string objectName, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(world, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        TextMesh MakeLabel(string objectName, float size)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(world, false);
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 64;
            text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = font.material;
            meshRenderer.sortingOrder = LabelOrder;
            return text;
        }

        /// <summary>
        /// Sizes a sprite to exactly width x height world units, whatever its pixel size. Things lower on screen get a
        /// slightly nearer depth, so within a sorting layer the front of a crowd draws over the back.
        /// </summary>
        static void Place(SpriteRenderer sr, float x, float y, float width, float height)
        {
            Vector2 bounds = sr.sprite.bounds.size;
            Transform t = sr.transform;
            t.position = new Vector3(x, y, y * DepthPerMetre);
            t.localScale = new Vector3(width / bounds.x, height / bounds.y, 1f);
        }

        /// <summary>Sizes a sprite to the given width in world units, keeping the art's aspect ratio.</summary>
        static void PlaceFit(SpriteRenderer sr, float x, float y, float width)
        {
            Vector2 bounds = sr.sprite.bounds.size;
            Place(sr, x, y, width, width * bounds.y / bounds.x);
        }

        static Texture2D MakeCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = math.length(new float2(x + 0.5f - r, y + 0.5f - r));
                byte a = (byte)(math.saturate(r - d) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
