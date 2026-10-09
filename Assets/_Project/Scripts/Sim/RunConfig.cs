using System;
using System.Collections.Generic;
using FarmFuryRampage.Data;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace FarmFuryRampage.Sim
{
    public struct GateRowConfig
    {
        public float distance;
        public GatePanelDef[] panels;
    }

    /// <summary>One robot to spawn, expanded from a <see cref="WaveDef"/>.</summary>
    public struct SpawnConfig
    {
        public int robotType;
        public float x;
        public float distance;
        /// <summary>HP multiplier for this robot on top of the level difficulty (0 is treated as 1).</summary>
        public float hpScale;
    }

    /// <summary>
    /// Plain-data snapshot of everything a run needs. Built from ScriptableObjects (and later Remote Config overrides)
    /// so the simulation never touches the assets themselves.
    /// </summary>
    public sealed class RunConfig
    {
        public int tickRate;
        public TrackTuning track;
        public HerdTuning herd;
        public GateTuning gates;
        public CombatTuning combat;
        public HeroStats hero;
        public RobotStats[] robotTypes = Array.Empty<RobotStats>();
        public float levelLength;
        public int difficultyLevel;
        public int startingHerd;
        public uint seed;
        public GateRowConfig[] gateRows = Array.Empty<GateRowConfig>();
        public SpawnConfig[] spawns = Array.Empty<SpawnConfig>();

        public float HalfWidth => track.width * 0.5f;
        public float TickSeconds => 1f / tickRate;
        public float RobotHpScale => 1f + combat.hpScalePerLevel * (difficultyLevel - 1);
    }

    public static class RunConfigFactory
    {
        /// <param name="robotTypes">Robot definitions in the order used by <see cref="SpawnConfig.robotType"/>.</param>
        public static RunConfig Create(GameTuning tuning, HeroDef hero, LevelDef level, out RobotDef[] robotTypes)
        {
            var types = new List<RobotDef>();
            var spawns = new List<SpawnConfig>();
            var random = new Random(level.seed == 0 ? 1u : level.seed);
            float halfWidth = tuning.track.width * 0.5f;

            int TypeOf(RobotDef robot)
            {
                int type = types.IndexOf(robot);
                if (type >= 0) return type;
                types.Add(robot);
                return types.Count - 1;
            }

            foreach (WaveDef wave in level.waves)
            {
                if (wave.robot == null) continue;
                int type = TypeOf(wave.robot);
                int columns = math.max(1, wave.columns);
                float firstColumn = -0.5f * (columns - 1) * wave.columnSpacing;
                for (int i = 0; i < wave.count; i++)
                for (int c = 0; c < columns; c++)
                {
                    float jitter = wave.xJitter > 0f ? random.NextFloat(-wave.xJitter, wave.xJitter) : 0f;
                    spawns.Add(new SpawnConfig
                    {
                        robotType = type,
                        x = math.clamp(wave.x + firstColumn + c * wave.columnSpacing + jitter, -halfWidth, halfWidth),
                        distance = wave.distance + i * wave.spacing,
                        hpScale = 1f,
                    });
                }
            }

            foreach (HordeStreamDef stream in level.hordeStreams)
            {
                if (stream.robot == null || stream.rowSpacing <= 0f) continue;
                int type = TypeOf(stream.robot);
                float span = stream.endDistance - stream.startDistance;
                int rowCount = (int)math.floor(span / stream.rowSpacing) + 1;
                for (int i = 0; i < rowCount; i++)
                {
                    float t = rowCount > 1 ? (float)i / (rowCount - 1) : 0f;
                    int columns = math.max(1, (int)math.round(math.lerp(stream.columnsStart, stream.columnsEnd, t)));
                    float rampStart = math.saturate(stream.hpRampStart);
                    float ramp = rampStart < 1f ? math.saturate((t - rampStart) / (1f - rampStart)) : 0f;
                    float hpScale = math.lerp(1f, math.max(1f, stream.hpMultiplierEnd), ramp);
                    float firstColumn = -0.5f * (columns - 1) * stream.columnSpacing;
                    for (int c = 0; c < columns; c++)
                    {
                        spawns.Add(new SpawnConfig
                        {
                            robotType = type,
                            x = math.clamp(stream.x + firstColumn + c * stream.columnSpacing, -halfWidth, halfWidth),
                            distance = stream.startDistance + i * stream.rowSpacing,
                            hpScale = hpScale,
                        });
                    }
                }
            }

            robotTypes = types.ToArray();
            var stats = new RobotStats[robotTypes.Length];
            for (int i = 0; i < stats.Length; i++) stats[i] = robotTypes[i].stats;

            spawns = ClearSpaceAroundBigRobots(spawns, stats);
            spawns.Sort((a, b) => a.distance.CompareTo(b.distance));

            var rows = new List<GateRowConfig>(level.gateRows.Count);
            foreach (GateRowDef row in level.gateRows)
                rows.Add(new GateRowConfig { distance = row.distance, panels = (GatePanelDef[])row.panels.Clone() });
            rows.Sort((a, b) => a.distance.CompareTo(b.distance));

            return new RunConfig
            {
                tickRate = tuning.tickRate,
                track = tuning.track,
                herd = tuning.herd,
                gates = tuning.gates,
                combat = tuning.combat,
                hero = hero.stats,
                robotTypes = stats,
                levelLength = level.length,
                difficultyLevel = level.difficultyLevel,
                startingHerd = level.startingHerd,
                seed = level.seed,
                gateRows = rows.ToArray(),
                spawns = spawns.ToArray(),
            };
        }

        /// <summary>
        /// Drops any robot that overlaps a bigger one, so elites and bosses placed inside a packed horde get their own
        /// space instead of sitting on top of the pack.
        /// </summary>
        static List<SpawnConfig> ClearSpaceAroundBigRobots(List<SpawnConfig> spawns, RobotStats[] stats)
        {
            float smallest = float.MaxValue;
            foreach (RobotStats s in stats) smallest = math.min(smallest, s.radius);

            var bigger = new List<SpawnConfig>();
            foreach (SpawnConfig s in spawns)
                if (stats[s.robotType].radius > smallest) bigger.Add(s);
            if (bigger.Count == 0) return spawns;

            var kept = new List<SpawnConfig>(spawns.Count);
            foreach (SpawnConfig s in spawns)
            {
                float radius = stats[s.robotType].radius;
                bool blocked = false;
                foreach (SpawnConfig b in bigger)
                {
                    float bigRadius = stats[b.robotType].radius;
                    if (bigRadius <= radius) continue;
                    float reach = bigRadius + radius;
                    float dd = b.distance - s.distance;
                    if (dd > reach || dd < -reach) continue;
                    float dx = b.x - s.x;
                    if (dx * dx + dd * dd >= reach * reach) continue;
                    blocked = true;
                    break;
                }
                if (!blocked) kept.Add(s);
            }
            return kept;
        }
    }
}
