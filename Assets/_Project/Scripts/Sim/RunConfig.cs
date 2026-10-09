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

            foreach (WaveDef wave in level.waves)
            {
                if (wave.robot == null) continue;
                int type = types.IndexOf(wave.robot);
                if (type < 0)
                {
                    type = types.Count;
                    types.Add(wave.robot);
                }

                for (int i = 0; i < wave.count; i++)
                {
                    float jitter = wave.xJitter > 0f ? random.NextFloat(-wave.xJitter, wave.xJitter) : 0f;
                    spawns.Add(new SpawnConfig
                    {
                        robotType = type,
                        x = math.clamp(wave.x + jitter, -halfWidth, halfWidth),
                        distance = wave.distance + i * wave.spacing,
                    });
                }
            }
            spawns.Sort((a, b) => a.distance.CompareTo(b.distance));

            var rows = new List<GateRowConfig>(level.gateRows.Count);
            foreach (GateRowDef row in level.gateRows)
                rows.Add(new GateRowConfig { distance = row.distance, panels = (GatePanelDef[])row.panels.Clone() });
            rows.Sort((a, b) => a.distance.CompareTo(b.distance));

            robotTypes = types.ToArray();
            var stats = new RobotStats[robotTypes.Length];
            for (int i = 0; i < stats.Length; i++) stats[i] = robotTypes[i].stats;

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
    }
}
