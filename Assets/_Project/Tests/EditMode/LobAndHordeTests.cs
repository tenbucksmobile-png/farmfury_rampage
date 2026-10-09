using System.Collections.Generic;
using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace FarmFuryRampage.Tests
{
    /// <summary>Cluck's egg grenade (lob + blast) and packed horde waves.</summary>
    public sealed class LobAndHordeTests
    {
        const float BlastRadius = 1.5f;
        const float EggDamage = 12f;

        static RunConfig LobConfig(int herd)
        {
            return new RunConfig
            {
                tickRate = 60,
                track = new TrackTuning { width = 9f, pace = 0f, viewWidth = 10f, herdScreenY = 0.3f, spawnAhead = 16f, despawnBehind = 8f },
                herd = new HerdTuning { cap = 300, drawnCap = 60, slotSpacing = 0.28f, steerSpeed = 8f, dragSensitivity = 1f, steadySteerMultiplier = 0.5f },
                gates = new GateTuning { hitsPerStep = 2, maxImprove = 5, hitCooldown = 0.15f, panelDepth = 0.6f },
                combat = new CombatTuning { hpScalePerLevel = 0.12f, projectileCap = 400, heroDpsPerAnimal = 10f, heroBalanceTolerance = 0.05f },
                hero = new HeroStats
                {
                    pattern = AttackPattern.Lob, fireInterval = 1.2f, projectilesPerShot = 1, damagePerHit = EggDamage,
                    range = 11f, blastRadius = BlastRadius, flightTime = 0.7f, scatter = 0f, arcHeight = 2.5f,
                },
                robotTypes = new[] { new RobotStats { hp = 100f, speed = 0f, bite = 0, radius = 0.42f, homing = 0f, scrap = 1 } },
                levelLength = 1000f,
                difficultyLevel = 1,
                startingHerd = herd,
                seed = 7,
            };
        }

        static void RunFor(RunSim sim, float seconds)
        {
            int ticks = (int)math.round(seconds * sim.Config.tickRate);
            for (int i = 0; i < ticks; i++) sim.Tick(new RunInput());
        }

        [Test]
        public void EggGrenade_DamagesEveryRobotInsideTheBlast_AndNoneOutside()
        {
            RunConfig config = LobConfig(herd: 1);
            config.spawns = new[]
            {
                new SpawnConfig { robotType = 0, x = -0.95f, distance = 8f },
                new SpawnConfig { robotType = 0, x = 0f, distance = 8f },
                new SpawnConfig { robotType = 0, x = 0.95f, distance = 8f },
                new SpawnConfig { robotType = 0, x = 3.5f, distance = 8f },
            };
            var sim = new RunSim(config);

            // One throw at the start lands at 0.7 s; the next isn't thrown until 1.2 s.
            RunFor(sim, 1f);

            Assert.AreEqual(100f - EggDamage, sim.Robots[0].hp, 1e-3f);
            Assert.AreEqual(100f - EggDamage, sim.Robots[1].hp, 1e-3f);
            Assert.AreEqual(100f - EggDamage, sim.Robots[2].hp, 1e-3f);
            Assert.AreEqual(100f, sim.Robots[3].hp, 1e-3f, "outside the blast");
        }

        [Test]
        public void EggGrenade_SingleTargetDps_IsHerdSizeTimesHeroDps()
        {
            const int herd = 20;
            RunConfig config = LobConfig(herd);
            config.robotTypes[0].hp = 1e5f;
            config.spawns = new[] { new SpawnConfig { robotType = 0, x = 0f, distance = 8f } };
            var sim = new RunSim(config);

            RunFor(sim, 2f);
            float hpBefore = sim.Robots[0].hp;
            const float window = 6f;
            RunFor(sim, window);
            float dps = (hpBefore - sim.Robots[0].hp) / window;

            float expected = herd * EggDamage / config.hero.fireInterval;
            Assert.AreEqual(expected, dps, expected * 0.05f);
        }

        [Test]
        public void EggGrenade_LeadsAMovingRobot()
        {
            RunConfig config = LobConfig(herd: 1);
            config.hero.blastRadius = 0.2f;
            config.robotTypes[0].speed = 2f;
            config.spawns = new[] { new SpawnConfig { robotType = 0, x = 0f, distance = 9f } };
            var sim = new RunSim(config);

            RunFor(sim, 1f);

            Assert.Less(sim.Robots[0].hp, 100f, "a small blast only hits if the throw led the robot");
        }

        [Test]
        public void WithNoRobotsInRange_EggsShootUpTheGateInTheHerdsLane()
        {
            RunConfig config = LobConfig(herd: 10);
            config.gateRows = new[] { new GateRowConfig { distance = 6f, panels = new[] { new GatePanelDef(GateKind.Subtract, 3), new GatePanelDef(GateKind.Subtract, 3) } } };
            var sim = new RunSim(config);

            RunFor(sim, 4f);

            GatePanelState lane = sim.GateRows[0].panels[GateMath.PanelIndex(0f, 2, config.track.width)];
            Assert.Greater(GateMath.ToSigned(lane.kind, lane.value), -3);
        }

        [Test]
        public void HordeStream_IsConstantAndWidensAlongTheLevel()
        {
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            tuning.track.width = 9f;
            var hero = ScriptableObject.CreateInstance<HeroDef>();
            var walker = ScriptableObject.CreateInstance<RobotDef>();
            walker.stats.radius = 0.42f;
            var level = ScriptableObject.CreateInstance<LevelDef>();
            level.length = 100f;
            level.hordeStreams = new List<HordeStreamDef>
            {
                new() { startDistance = 10f, endDistance = 20f, robot = walker, columnsStart = 3, columnsEnd = 7, rowSpacing = 1f, columnSpacing = 0.95f },
            };

            try
            {
                RunConfig config = RunConfigFactory.Create(tuning, hero, level, out _);

                var perRow = new SortedDictionary<float, int>();
                foreach (SpawnConfig s in config.spawns)
                    perRow[s.distance] = perRow.TryGetValue(s.distance, out int n) ? n + 1 : 1;

                Assert.AreEqual(11, perRow.Count, "a row every metre from 10 to 20, no gaps");
                int previous = 0;
                foreach (int columns in perRow.Values)
                {
                    Assert.GreaterOrEqual(columns, previous, "never narrows");
                    previous = columns;
                }
                Assert.AreEqual(3, perRow[10f]);
                Assert.AreEqual(7, perRow[20f]);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(walker);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void BossRobotInsideTheHorde_ClearsItsOwnSpace()
        {
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            tuning.track.width = 9f;
            var hero = ScriptableObject.CreateInstance<HeroDef>();
            var walker = ScriptableObject.CreateInstance<RobotDef>();
            walker.stats.radius = 0.42f;
            var tank = ScriptableObject.CreateInstance<RobotDef>();
            tank.stats.radius = 1.1f;
            var level = ScriptableObject.CreateInstance<LevelDef>();
            level.length = 100f;
            level.hordeStreams = new List<HordeStreamDef>
            {
                new() { startDistance = 10f, endDistance = 30f, robot = walker, columnsStart = 5, columnsEnd = 5, rowSpacing = 1f, columnSpacing = 0.95f },
            };
            level.waves = new List<WaveDef> { new() { distance = 20f, robot = tank, x = 0f, count = 1, columns = 1 } };

            try
            {
                RunConfig config = RunConfigFactory.Create(tuning, hero, level, out RobotDef[] types);
                int tankType = System.Array.IndexOf(types, tank);

                int tanks = 0;
                foreach (SpawnConfig s in config.spawns)
                {
                    if (s.robotType == tankType)
                    {
                        tanks++;
                        continue;
                    }
                    float gap = math.length(new float2(s.x, s.distance - 20f));
                    Assert.GreaterOrEqual(gap, 1.1f + 0.42f - 1e-4f, $"walker at ({s.x}, {s.distance}) overlaps the tank");
                }
                Assert.AreEqual(1, tanks);
                Assert.Less(config.spawns.Length, 21 * 5 + 1, "some walkers were removed to make room");
            }
            finally
            {
                Object.DestroyImmediate(tuning);
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(walker);
                Object.DestroyImmediate(tank);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void HordeWave_ExpandsToAPackedBlockCentredOnX()
        {
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            tuning.track.width = 9f;
            var hero = ScriptableObject.CreateInstance<HeroDef>();
            var robot = ScriptableObject.CreateInstance<RobotDef>();
            var level = ScriptableObject.CreateInstance<LevelDef>();
            level.length = 100f;
            level.waves = new List<WaveDef>
            {
                new() { distance = 20f, robot = robot, x = 1f, count = 3, spacing = 0.95f, columns = 4, columnSpacing = 0.95f },
            };

            try
            {
                RunConfig config = RunConfigFactory.Create(tuning, hero, level, out RobotDef[] types);

                Assert.AreEqual(12, config.spawns.Length);
                Assert.AreEqual(1, types.Length);
                float minX = float.MaxValue, maxX = float.MinValue, maxD = 0f;
                foreach (SpawnConfig s in config.spawns)
                {
                    minX = math.min(minX, s.x);
                    maxX = math.max(maxX, s.x);
                    maxD = math.max(maxD, s.distance);
                }
                Assert.AreEqual(1f - 1.425f, minX, 1e-4f);
                Assert.AreEqual(1f + 1.425f, maxX, 1e-4f);
                Assert.AreEqual(20f + 2 * 0.95f, maxD, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(robot);
                Object.DestroyImmediate(level);
            }
        }
    }
}
