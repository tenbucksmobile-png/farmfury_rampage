using System.Collections.Generic;
using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FarmFuryRampage.Tests
{
    /// <summary>
    /// Plan section 12.3: a simple bot (steer into whichever panel of the next gate row gives the biggest herd) must
    /// clear every authored level with the authored hero. Catches impossible or broken levels before a playtest.
    /// </summary>
    public sealed class LevelClearabilityTests
    {
        static T LoadFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            Assert.IsNotEmpty(guids, $"No {typeof(T).Name} asset.");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static IEnumerable<LevelDef> Levels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDef"))
                yield return AssetDatabase.LoadAssetAtPath<LevelDef>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [TestCaseSource(nameof(Levels))]
        public void GreedyBot_ClearsTheLevel(LevelDef level)
        {
            RunConfig config = RunConfigFactory.Create(LoadFirst<GameTuning>(), LoadFirst<HeroDef>(), level, out _);
            var sim = new RunSim(config);

            int maxTicks = (int)(config.levelLength / config.track.pace * config.tickRate) + config.tickRate;
            for (int i = 0; i < maxTicks && sim.Phase == RunPhase.Running; i++)
                sim.Tick(new RunInput { targetX = BestPanelX(sim) });

            Debug.Log($"[Clearability] {level.name}: {sim.Phase}, herd {sim.HerdCount} (peak {sim.PeakHerd}), kills {sim.Kills}, scrap {sim.Scrap}");
            Assert.AreEqual(RunPhase.Won, sim.Phase, $"{level.name}: herd {sim.HerdCount} at {sim.HerdDistance:F0} m");
        }

        static float BestPanelX(RunSim sim)
        {
            RunConfig config = sim.Config;
            foreach (GateRowState row in sim.GateRows)
            {
                if (row.passed) continue;
                int best = 0;
                int bestHerd = -1;
                for (int p = 0; p < row.panels.Length; p++)
                {
                    int herd = GateMath.Apply(row.panels[p].kind, row.panels[p].value, sim.HerdCount, config.herd.cap);
                    if (herd <= bestHerd) continue;
                    bestHerd = herd;
                    best = p;
                }
                float panelWidth = config.track.width / row.panels.Length;
                return -config.HalfWidth + panelWidth * (best + 0.5f);
            }
            return 0f;
        }
    }
}
