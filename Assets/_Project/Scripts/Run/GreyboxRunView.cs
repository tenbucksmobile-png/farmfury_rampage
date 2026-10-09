using System.Collections.Generic;
using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using Unity.Mathematics;
using UnityEngine;

namespace FarmFuryRampage.Run
{
    /// <summary>
    /// Placeholder rendering for the Phase 1 greybox: coloured shapes and text, one GameObject per thing.
    /// Phase 2 replaces this with instanced herd/projectile rendering and real art. Reads the sim; never changes it.
    /// </summary>
    public sealed class GreyboxRunView : MonoBehaviour
    {
        // Sorting orders, back to front.
        const int TrackOrder = 0;
        const int StripeOrder = 1;
        const int GateOrder = 2;
        const int RobotOrder = 4;
        const int ProjectileOrder = 5;
        const int HerdOrder = 6;
        const int LabelOrder = 8;

        // Greybox look only; real art replaces all of these.
        static readonly Color TrackColor = new(0.45f, 0.62f, 0.30f);
        static readonly Color EdgeColor = new(0.36f, 0.25f, 0.15f);
        static readonly Color StripeColor = new(0.50f, 0.68f, 0.34f);
        static readonly Color FinishColor = new(1f, 1f, 1f, 0.8f);
        static readonly Color BlueGate = new(0.20f, 0.55f, 1f, 0.55f);
        static readonly Color RedGate = new(1f, 0.25f, 0.20f, 0.55f);
        static readonly Color PassedGate = new(0.5f, 0.5f, 0.5f, 0.25f);
        static readonly Color ProjectileColor = new(1f, 0.97f, 0.80f);
        const float StripeSpacing = 5f;
        const float EdgeWidth = 0.25f;
        const float ProjectileSize = 0.25f;
        const float GateLabelSize = 0.25f;
        const float CountLabelSize = 0.3f;
        const float RobotHpBarHeight = 0.12f;

        Camera cam;
        RunSim sim;
        RobotDef[] robotTypes;
        Font font;
        Sprite square;
        Sprite circle;

        Transform world;
        SpriteRenderer track;
        SpriteRenderer leftEdge;
        SpriteRenderer rightEdge;
        SpriteRenderer finish;
        readonly List<SpriteRenderer> stripes = new();
        readonly List<SpriteRenderer> herd = new();
        readonly List<SpriteRenderer> robots = new();
        readonly List<SpriteRenderer> robotBars = new();
        readonly List<SpriteRenderer> projectiles = new();
        readonly List<(SpriteRenderer panel, TextMesh label)> gatePanels = new();
        TextMesh countLabel;
        float countPop;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circle = Sprite.Create(MakeCircleTexture(32), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            world = new GameObject("GreyboxWorld").transform;
            world.SetParent(transform, false);
        }

        public void Bind(RunSim run, RobotDef[] types, HeroDef hero)
        {
            sim = run;
            robotTypes = types;
            cam = Camera.main;

            foreach (Transform child in world) Destroy(child.gameObject);
            stripes.Clear();
            herd.Clear();
            robots.Clear();
            robotBars.Clear();
            projectiles.Clear();
            gatePanels.Clear();

            RunConfig config = run.Config;
            track = MakeSprite("Track", square, TrackColor, TrackOrder);
            leftEdge = MakeSprite("EdgeL", square, EdgeColor, StripeOrder);
            rightEdge = MakeSprite("EdgeR", square, EdgeColor, StripeOrder);
            finish = MakeSprite("Finish", square, FinishColor, StripeOrder);

            int stripeCount = (int)math.ceil(config.track.viewWidth * 3f / StripeSpacing) + 2;
            for (int i = 0; i < stripeCount; i++) stripes.Add(MakeSprite("Stripe", square, StripeColor, StripeOrder));

            for (int i = 0; i < config.herd.drawnCap; i++)
                herd.Add(MakeSprite("Animal", circle, hero.greyboxColor, HerdOrder));

            for (int i = 0; i < run.Robots.Length; i++)
            {
                robots.Add(MakeSprite("Robot", square, Color.white, RobotOrder));
                robotBars.Add(MakeSprite("RobotHp", square, Color.red, RobotOrder + 1));
            }

            for (int i = 0; i < run.Projectiles.Length; i++)
                projectiles.Add(MakeSprite("Shot", circle, ProjectileColor, ProjectileOrder));

            foreach (GateRowState row in run.GateRows)
                for (int p = 0; p < row.panels.Length; p++)
                    gatePanels.Add((MakeSprite("Gate", square, BlueGate, GateOrder), MakeLabel("GateLabel", GateLabelSize)));

            countLabel = MakeLabel("HerdCount", CountLabelSize);
            countPop = 0f;
        }

        public void OnEvents(IReadOnlyList<RunEvent> events)
        {
            foreach (RunEvent e in events)
                if (e.type == RunEventType.GatePassed || e.type == RunEventType.AnimalsLost)
                    countPop = 1f;
        }

        void LateUpdate()
        {
            if (sim == null || cam == null) return;
            RunConfig config = sim.Config;

            FrameCamera(config);
            float halfWidth = config.HalfWidth;
            float viewHeight = cam.orthographicSize * 2f;
            float camY = cam.transform.position.y;

            Place(track, 0f, camY, config.track.width, viewHeight + 2f);
            Place(leftEdge, -halfWidth - EdgeWidth * 0.5f, camY, EdgeWidth, viewHeight + 2f);
            Place(rightEdge, halfWidth + EdgeWidth * 0.5f, camY, EdgeWidth, viewHeight + 2f);
            Place(finish, 0f, config.levelLength - sim.HerdDistance, config.track.width, EdgeWidth);

            float firstStripe = math.floor((sim.HerdDistance - viewHeight) / StripeSpacing) * StripeSpacing;
            for (int i = 0; i < stripes.Count; i++)
                Place(stripes[i], 0f, firstStripe + i * StripeSpacing - sim.HerdDistance, config.track.width, EdgeWidth * 0.5f);

            RenderHerd(config);
            RenderRobots(config);
            RenderProjectiles();
            RenderGates(config);
        }

        void FrameCamera(RunConfig config)
        {
            cam.orthographic = true;
            float aspect = (float)Screen.width / math.max(1, Screen.height);
            cam.orthographicSize = config.track.viewWidth / aspect * 0.5f;
            float centreAboveHerd = (0.5f - config.track.herdScreenY) * cam.orthographicSize * 2f;
            cam.transform.position = new Vector3(0f, centreAboveHerd, -10f);
        }

        void RenderHerd(RunConfig config)
        {
            int drawn = sim.DrawnCount;
            float size = config.herd.slotSpacing * 0.9f;
            for (int i = 0; i < herd.Count; i++)
            {
                bool show = i < drawn;
                herd[i].enabled = show;
                if (!show) continue;
                float2 offset = Formation.SlotOffset(i, config.herd.slotSpacing);
                Place(herd[i], sim.HerdX + offset.x, offset.y, size, size);
            }

            countPop = math.max(0f, countPop - Time.deltaTime * 4f);
            countLabel.text = sim.HerdCount.ToString();
            countLabel.characterSize = CountLabelSize * (1f + countPop * 0.5f);
            countLabel.transform.position = new Vector3(sim.HerdX, sim.HerdFootprint + 0.8f, 0f);
        }

        void RenderRobots(RunConfig config)
        {
            Robot[] state = sim.Robots;
            for (int i = 0; i < robots.Count; i++)
            {
                Robot robot = state[i];
                robots[i].enabled = robot.active;
                bool damaged = robot.active && robot.hp < robot.maxHp;
                robotBars[i].enabled = damaged;
                if (!robot.active) continue;

                float size = config.robotTypes[robot.type].radius * 2f;
                float y = robot.distance - sim.HerdDistance;
                robots[i].color = robotTypes[robot.type].greyboxColor;
                Place(robots[i], robot.x, y, size, size);
                if (damaged)
                {
                    float fraction = math.saturate(robot.hp / robot.maxHp);
                    Place(robotBars[i], robot.x - size * 0.5f * (1f - fraction), y + size * 0.5f + RobotHpBarHeight,
                        size * fraction, RobotHpBarHeight);
                }
            }
        }

        void RenderProjectiles()
        {
            Projectile[] state = sim.Projectiles;
            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile p = state[i];
                projectiles[i].enabled = p.active;
                if (p.active) Place(projectiles[i], p.x, p.distance - sim.HerdDistance, ProjectileSize, ProjectileSize);
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
                    bool chosen = row.passed && row.chosenPanel == p;
                    panel.color = row.passed && !chosen ? PassedGate : GateColor(state.kind);
                    Place(panel, x, y, panelWidth * 0.94f, depth);
                    label.text = GateLabel(state);
                    label.transform.position = new Vector3(x, y, 0f);
                }
            }
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

        static void Place(SpriteRenderer sr, float x, float y, float width, float height)
        {
            Transform t = sr.transform;
            t.position = new Vector3(x, y, 0f);
            t.localScale = new Vector3(width, height, 1f);
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
