using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using NUnit.Framework;
using Unity.Mathematics;

namespace FarmFuryRampage.Tests
{
    public sealed class RunSimTests
    {
        const float HeroDps = 10f;

        /// <summary>A self-contained config so these tests never depend on tuned asset values.</summary>
        static RunConfig Config(int herd = 5, float pace = 3f, float length = 100f)
        {
            return new RunConfig
            {
                tickRate = 60,
                track = new TrackTuning { width = 9f, pace = pace, viewWidth = 10f, herdScreenY = 0.3f, spawnAhead = 16f, despawnBehind = 8f },
                herd = new HerdTuning { cap = 300, drawnCap = 60, slotSpacing = 0.28f, steerSpeed = 8f, dragSensitivity = 1f, steadySteerMultiplier = 0.5f },
                gates = new GateTuning { hitsPerStep = 2, maxImprove = 5, hitCooldown = 0.15f, panelDepth = 0.6f },
                combat = new CombatTuning { hpScalePerLevel = 0.12f, projectileCap = 400, heroDpsPerAnimal = HeroDps, heroBalanceTolerance = 0.05f },
                hero = new HeroStats { fireInterval = 0.6f, projectilesPerShot = 3, spreadDegrees = 30f, damagePerHit = 2f, range = 12f, projectileSpeed = 14f, projectileRadius = 0.12f },
                robotTypes = new[] { new RobotStats { hp = 60f, speed = 1.8f, bite = 2, radius = 0.45f, homing = 0f, scrap = 2 } },
                levelLength = length,
                difficultyLevel = 1,
                startingHerd = herd,
                seed = 1,
            };
        }

        static void RunFor(RunSim sim, float seconds, float targetX = 0f)
        {
            int ticks = (int)math.round(seconds * sim.Config.tickRate);
            for (int i = 0; i < ticks; i++) sim.Tick(new RunInput { targetX = targetX });
        }

        [Test]
        public void ReachingTheEnd_WinsTheRun()
        {
            var sim = new RunSim(Config(length: 9f));

            RunFor(sim, 3.1f);

            Assert.AreEqual(RunPhase.Won, sim.Phase);
        }

        [Test]
        public void Herd_PassesThePanelItSteersInto()
        {
            RunConfig config = Config(herd: 5);
            config.hero.fireInterval = 0f; // no shooting, so the +3 panel can't be shot up before it is crossed
            config.gateRows = new[] { new GateRowConfig { distance = 10f, panels = new[] { new GatePanelDef(GateKind.Multiply, 2), new GatePanelDef(GateKind.Add, 3) } } };

            var left = new RunSim(config);
            RunFor(left, 4f, targetX: -3f);
            var right = new RunSim(config);
            RunFor(right, 4f, targetX: 3f);

            Assert.AreEqual(10, left.HerdCount, "x2 on the left");
            Assert.AreEqual(8, right.HerdCount, "+3 on the right");
        }

        [Test]
        public void RobotReachingHerd_KnocksOutItsBite()
        {
            RunConfig config = Config(herd: 10);
            config.hero.damagePerHit = 0f;
            config.spawns = new[] { new SpawnConfig { robotType = 0, x = 0f, distance = 8f } };
            var sim = new RunSim(config);

            RunFor(sim, 3f);

            Assert.AreEqual(8, sim.HerdCount);
        }

        [Test]
        public void LosingTheWholeHerd_FailsTheRun()
        {
            RunConfig config = Config(herd: 1);
            config.hero.damagePerHit = 0f;
            config.spawns = new[] { new SpawnConfig { robotType = 0, x = 0f, distance = 8f } };
            var sim = new RunSim(config);

            RunFor(sim, 3f);

            Assert.AreEqual(RunPhase.Failed, sim.Phase);
            Assert.AreEqual(0, sim.HerdCount);
        }

        [Test]
        public void SameConfigAndInput_GivesIdenticalRuns()
        {
            RunConfig config = Config(herd: 12, length: 60f);
            config.gateRows = new[] { new GateRowConfig { distance = 20f, panels = new[] { new GatePanelDef(GateKind.Subtract, 3), new GatePanelDef(GateKind.Multiply, 2) } } };
            config.spawns = new[]
            {
                new SpawnConfig { robotType = 0, x = -2f, distance = 25f },
                new SpawnConfig { robotType = 0, x = 1f, distance = 30f },
                new SpawnConfig { robotType = 0, x = 3f, distance = 40f },
            };
            var a = new RunSim(config);
            var b = new RunSim(config);

            for (int i = 0; i < 1500; i++)
            {
                var input = new RunInput { targetX = 3f * math.sin(i * 0.01f) };
                a.Tick(input);
                b.Tick(input);
            }

            Assert.AreEqual(a.HerdCount, b.HerdCount);
            Assert.AreEqual(a.HerdX, b.HerdX);
            Assert.AreEqual(a.Scrap, b.Scrap);
            Assert.AreEqual(a.Phase, b.Phase);
        }

        /// <summary>GDD section 3: herd DPS = herd size x hero DPS, including above the 60 drawn animals.</summary>
        [TestCase(5)]
        [TestCase(20)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(300)]
        public void HerdDps_IsHerdSizeTimesHeroDps(int herd)
        {
            // A wide track and a huge stationary target, so every shot of the fan lands and none leave the track.
            RunConfig config = Config(herd: herd, pace: 0f, length: 1000f);
            config.track.width = 30f;
            config.robotTypes = new[] { new RobotStats { hp = 1e5f, speed = 0f, bite = 0, radius = 7f, homing = 0f, scrap = 0 } };
            config.spawns = new[] { new SpawnConfig { robotType = 0, x = 0f, distance = 10f } };
            var sim = new RunSim(config);

            RunFor(sim, 2f);
            float hpBefore = sim.Robots[0].hp;
            const float window = 6f;
            RunFor(sim, window);
            float dps = (hpBefore - sim.Robots[0].hp) / window;

            Assert.AreEqual(herd * HeroDps, dps, herd * HeroDps * 0.05f);
        }

        [Test]
        public void ShootingARedGate_ImprovesIt()
        {
            RunConfig config = Config(herd: 20, pace: 0f, length: 1000f);
            config.gateRows = new[] { new GateRowConfig { distance = 6f, panels = new[] { new GatePanelDef(GateKind.Subtract, 3), new GatePanelDef(GateKind.Subtract, 3) } } };
            var sim = new RunSim(config);

            RunFor(sim, 3f);

            GatePanelState panel = sim.GateRows[0].panels[0];
            Assert.AreEqual(GateKind.Add, panel.kind);
            Assert.AreEqual(2, panel.value, "capped at authored -3 + maxImprove 5");
        }
    }
}
