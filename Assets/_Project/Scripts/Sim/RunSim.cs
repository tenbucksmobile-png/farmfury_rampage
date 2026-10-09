using System.Collections.Generic;
using FarmFuryRampage.Data;
using Unity.Mathematics;

namespace FarmFuryRampage.Sim
{
    /// <summary>
    /// The run as pure, deterministic C#: fixed tick, no UnityEngine objects. Coordinates are track space:
    /// x across the track (0 = centre), distance along it. The herd sits at <see cref="HerdDistance"/> and the
    /// presentation scrolls the world past it. Same config + same inputs = same result.
    /// </summary>
    public sealed class RunSim
    {
        readonly RunConfig config;
        readonly float tickSeconds;
        readonly Robot[] robots;
        readonly Projectile[] projectiles;
        readonly GateRowState[] gateRows;
        readonly float[] emitterTimers;
        readonly List<RunEvent> events = new(64);

        int nextSpawn;
        int nextRow;
        int projectileCursor;

        public RunConfig Config => config;
        public RunPhase Phase { get; private set; } = RunPhase.Running;
        public int TickCount { get; private set; }
        public float Time { get; private set; }
        public float HerdX { get; private set; }
        public float HerdDistance { get; private set; }
        public int HerdCount { get; private set; }
        public int PeakHerd { get; private set; }
        public int Scrap { get; private set; }
        public int Kills { get; private set; }

        public int DrawnCount => math.min(HerdCount, config.herd.drawnCap);
        public float HerdFootprint => Formation.FootprintRadius(DrawnCount, config.herd.slotSpacing);
        public float Progress => config.levelLength > 0f ? HerdDistance / config.levelLength : 1f;

        public Robot[] Robots => robots;
        public Projectile[] Projectiles => projectiles;
        public GateRowState[] GateRows => gateRows;
        /// <summary>Events raised by the most recent <see cref="Tick"/>. Cleared at the start of each tick.</summary>
        public IReadOnlyList<RunEvent> Events => events;

        public RunSim(RunConfig config)
        {
            this.config = config;
            tickSeconds = config.TickSeconds;

            robots = new Robot[math.max(1, config.spawns.Length)];
            projectiles = new Projectile[math.max(1, config.combat.projectileCap)];

            gateRows = new GateRowState[config.gateRows.Length];
            for (int r = 0; r < gateRows.Length; r++)
            {
                GateRowConfig row = config.gateRows[r];
                var panels = new GatePanelState[row.panels.Length];
                for (int p = 0; p < panels.Length; p++) panels[p] = GatePanelState.From(row.panels[p]);
                gateRows[r] = new GateRowState { distance = row.distance, panels = panels };
            }

            // Stagger the animals' shots so the herd fires a steady stream rather than volleys.
            emitterTimers = new float[math.max(1, config.herd.drawnCap)];
            for (int i = 0; i < emitterTimers.Length; i++)
                emitterTimers[i] = config.hero.fireInterval * i / emitterTimers.Length;

            HerdCount = math.clamp(config.startingHerd, 0, config.herd.cap);
            PeakHerd = HerdCount;
        }

        public void Tick(in RunInput input)
        {
            events.Clear();
            if (Phase != RunPhase.Running) return;

            TickCount++;
            Time = TickCount * tickSeconds;

            Steer(input);
            HerdDistance = math.min(HerdDistance + config.track.pace * tickSeconds, config.levelLength);
            SpawnRobots();
            Fire();
            MoveProjectiles();
            MoveRobots();
            PassGates();

            if (HerdCount <= 0)
            {
                Phase = RunPhase.Failed;
                events.Add(new RunEvent(RunEventType.Failed, HerdX, HerdDistance));
            }
            else if (HerdDistance >= config.levelLength)
            {
                Phase = RunPhase.Won;
                events.Add(new RunEvent(RunEventType.Won, HerdX, HerdDistance, HerdCount));
            }
        }

        /// <summary>Furthest the herd centre can steer from the middle without leaving the track.</summary>
        public float SteerLimit => math.max(0f, config.HalfWidth - HerdFootprint);

        void Steer(in RunInput input)
        {
            float limit = SteerLimit;
            float target = math.clamp(input.targetX, -limit, limit);
            float speed = config.herd.steerSpeed * (input.steadyHerd ? config.herd.steadySteerMultiplier : 1f);
            float step = speed * tickSeconds;
            HerdX = math.clamp(HerdX + math.clamp(target - HerdX, -step, step), -limit, limit);
        }

        void SpawnRobots()
        {
            float horizon = HerdDistance + config.track.spawnAhead;
            float hpScale = config.RobotHpScale;
            while (nextSpawn < config.spawns.Length && config.spawns[nextSpawn].distance <= horizon)
            {
                SpawnConfig spawn = config.spawns[nextSpawn];
                float hp = config.robotTypes[spawn.robotType].hp * hpScale;
                robots[nextSpawn] = new Robot
                {
                    active = true,
                    type = spawn.robotType,
                    x = spawn.x,
                    distance = spawn.distance,
                    hp = hp,
                    maxHp = hp,
                };
                nextSpawn++;
            }
        }

        void Fire()
        {
            HeroStats hero = config.hero;
            if (HerdCount <= 0 || hero.fireInterval <= 0f || hero.projectileSpeed <= 0f) return;

            // Above the drawn cap, or when the projectile budget is tight, fewer emitters fire and each shot
            // carries more damage, so herd DPS stays exactly HerdCount x hero DPS.
            float lifetime = hero.range / hero.projectileSpeed;
            int perShot = math.max(1, hero.projectilesPerShot);
            int budgetEmitters = (int)math.floor(projectiles.Length * hero.fireInterval / (perShot * lifetime));
            int emitters = math.max(1, math.min(DrawnCount, budgetEmitters));
            float damage = hero.damagePerHit * HerdCount / emitters;

            float spread = math.radians(hero.spreadDegrees);
            for (int i = 0; i < emitters; i++)
            {
                emitterTimers[i] -= tickSeconds;
                if (emitterTimers[i] > 0f) continue;
                emitterTimers[i] += hero.fireInterval;

                float2 origin = new float2(HerdX, HerdDistance) + Formation.SlotOffset(i, config.herd.slotSpacing);
                for (int k = 0; k < perShot; k++)
                {
                    float angle = perShot == 1 ? 0f : spread * ((float)k / (perShot - 1) - 0.5f);
                    SpawnProjectile(origin, angle, damage, lifetime);
                }
            }
        }

        void SpawnProjectile(float2 origin, float angle, float damage, float lifetime)
        {
            for (int n = 0; n < projectiles.Length; n++)
            {
                int index = (projectileCursor + n) % projectiles.Length;
                if (projectiles[index].active) continue;

                float speed = config.hero.projectileSpeed;
                projectiles[index] = new Projectile
                {
                    active = true,
                    x = origin.x,
                    distance = origin.y,
                    vx = math.sin(angle) * speed,
                    vd = math.cos(angle) * speed,
                    damage = damage,
                    lifetime = lifetime,
                };
                projectileCursor = (index + 1) % projectiles.Length;
                return;
            }
        }

        void MoveProjectiles()
        {
            float hitRadius = config.hero.projectileRadius;
            float halfDepth = config.gates.panelDepth * 0.5f;

            for (int i = 0; i < projectiles.Length; i++)
            {
                ref Projectile p = ref projectiles[i];
                if (!p.active) continue;

                p.x += p.vx * tickSeconds;
                p.distance += p.vd * tickSeconds;
                p.lifetime -= tickSeconds;
                if (p.lifetime <= 0f || math.abs(p.x) > config.HalfWidth)
                {
                    p.active = false;
                    continue;
                }

                if (HitGate(ref p, halfDepth)) continue;

                for (int r = 0; r < robots.Length; r++)
                {
                    ref Robot robot = ref robots[r];
                    if (!robot.active) continue;

                    float reach = hitRadius + config.robotTypes[robot.type].radius;
                    float dx = robot.x - p.x;
                    float dd = robot.distance - p.distance;
                    if (dx * dx + dd * dd > reach * reach) continue;

                    p.active = false;
                    robot.hp -= p.damage;
                    if (robot.hp <= 0f) KillRobot(ref robot);
                    break;
                }
            }
        }

        /// <summary>Shots that reach a +/- panel are absorbed and count toward improving it; x and / let them through.</summary>
        bool HitGate(ref Projectile p, float halfDepth)
        {
            for (int r = nextRow; r < gateRows.Length; r++)
            {
                GateRowState row = gateRows[r];
                if (row.distance - halfDepth > p.distance) break;
                if (row.passed || math.abs(row.distance - p.distance) > halfDepth) continue;

                int panel = GateMath.PanelIndex(p.x, row.panels.Length, config.track.width);
                if (!GateMath.IsImprovable(row.panels[panel].kind)) return false;

                p.active = false;
                if (GateMath.RegisterHit(ref row.panels[panel], Time, config.gates))
                    events.Add(new RunEvent(RunEventType.GateImproved, p.x, row.distance, r, panel));
                return true;
            }
            return false;
        }

        void KillRobot(ref Robot robot)
        {
            robot.active = false;
            int scrap = config.robotTypes[robot.type].scrap;
            Scrap += scrap;
            Kills++;
            events.Add(new RunEvent(RunEventType.RobotKilled, robot.x, robot.distance, robot.type, scrap));
        }

        void MoveRobots()
        {
            float footprint = HerdFootprint;
            for (int i = 0; i < nextSpawn; i++)
            {
                ref Robot robot = ref robots[i];
                if (!robot.active) continue;

                RobotStats stats = config.robotTypes[robot.type];
                float step = stats.speed * tickSeconds;
                robot.distance -= step;
                float drift = stats.homing * step;
                robot.x += math.clamp(HerdX - robot.x, -drift, drift);

                float reach = footprint + stats.radius;
                float ahead = robot.distance - HerdDistance;
                if (ahead <= reach && ahead >= -reach && math.abs(robot.x - HerdX) <= reach)
                {
                    robot.active = false;
                    int lost = math.min(stats.bite, HerdCount);
                    HerdCount -= lost;
                    events.Add(new RunEvent(RunEventType.AnimalsLost, robot.x, robot.distance, lost, HerdCount));
                    continue;
                }

                if (ahead < -config.track.despawnBehind) robot.active = false;
            }
        }

        void PassGates()
        {
            while (nextRow < gateRows.Length && gateRows[nextRow].distance <= HerdDistance)
            {
                GateRowState row = gateRows[nextRow];
                int panel = GateMath.PanelIndex(HerdX, row.panels.Length, config.track.width);
                GatePanelState state = row.panels[panel];
                int before = HerdCount;
                HerdCount = GateMath.Apply(state.kind, state.value, HerdCount, config.herd.cap);
                PeakHerd = math.max(PeakHerd, HerdCount);
                row.passed = true;
                row.chosenPanel = panel;
                events.Add(new RunEvent(RunEventType.GatePassed, HerdX, row.distance, before, HerdCount));
                nextRow++;
            }
        }
    }
}
