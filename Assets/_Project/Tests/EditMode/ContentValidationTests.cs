using System.Collections.Generic;
using FarmFuryRampage.Data;
using NUnit.Framework;
using UnityEditor;

namespace FarmFuryRampage.Tests
{
    /// <summary>
    /// Checks every authored asset. FarmFury's levels 2-6 broke on a coordinate bug; these tests make that kind of
    /// mistake fail before it reaches a playtest.
    /// </summary>
    public sealed class ContentValidationTests
    {
        static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
                list.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }

        static GameTuning Tuning()
        {
            List<GameTuning> all = LoadAll<GameTuning>();
            Assert.AreEqual(1, all.Count, "Exactly one GameTuning asset expected.");
            return all[0];
        }

        static IEnumerable<LevelDef> Levels() => LoadAll<LevelDef>();

        [Test]
        public void AtLeastOneLevelExists()
        {
            Assert.IsNotEmpty(LoadAll<LevelDef>());
        }

        [TestCaseSource(nameof(Levels))]
        public void Level_IsInsideTrackBounds(LevelDef level)
        {
            GameTuning tuning = Tuning();
            float halfWidth = tuning.track.width * 0.5f;
            var errors = new List<string>();

            if (string.IsNullOrEmpty(level.id)) errors.Add("missing id");
            if (level.length <= 0f) errors.Add("length must be > 0");
            if (level.startingHerd < 1 || level.startingHerd > tuning.herd.cap) errors.Add($"startingHerd {level.startingHerd} out of range");

            for (int r = 0; r < level.gateRows.Count; r++)
            {
                GateRowDef row = level.gateRows[r];
                if (row.distance <= 0f || row.distance >= level.length) errors.Add($"gate row {r} at {row.distance} m is outside 0..{level.length}");
                if (row.panels.Length < 2 || row.panels.Length > 3) errors.Add($"gate row {r} has {row.panels.Length} panels (GDD: 2 or 3)");
                foreach (GatePanelDef panel in row.panels)
                {
                    bool fixedOp = panel.kind == GateKind.Multiply || panel.kind == GateKind.Divide;
                    int min = fixedOp ? 2 : 1;
                    if (panel.value < min) errors.Add($"gate row {r} has {panel.kind} {panel.value} (min {min})");
                }
            }

            for (int w = 0; w < level.waves.Count; w++)
            {
                WaveDef wave = level.waves[w];
                if (wave.robot == null) errors.Add($"wave {w} has no robot");
                if (wave.count < 1) errors.Add($"wave {w} count {wave.count}");
                float last = wave.distance + (wave.count - 1) * wave.spacing;
                if (wave.distance <= 0f || last >= level.length) errors.Add($"wave {w} spans {wave.distance}..{last} m, outside 0..{level.length}");
                if (wave.columns < 1) errors.Add($"wave {w} columns {wave.columns}");
                float halfBlock = 0.5f * (System.Math.Max(1, wave.columns) - 1) * wave.columnSpacing + wave.xJitter;
                if (wave.x - halfBlock < -halfWidth || wave.x + halfBlock > halfWidth)
                    errors.Add($"wave {w} x {wave.x} +/- {halfBlock} leaves the track (+/-{halfWidth} m)");
            }

            for (int h = 0; h < level.hordeStreams.Count; h++)
            {
                HordeStreamDef stream = level.hordeStreams[h];
                if (stream.robot == null) errors.Add($"horde stream {h} has no robot");
                if (stream.rowSpacing <= 0f) errors.Add($"horde stream {h} rowSpacing must be > 0");
                if (stream.columnsStart < 1 || stream.columnsEnd < 1) errors.Add($"horde stream {h} needs at least 1 column");
                if (stream.startDistance <= 0f || stream.endDistance >= level.length || stream.endDistance < stream.startDistance)
                    errors.Add($"horde stream {h} spans {stream.startDistance}..{stream.endDistance} m, outside 0..{level.length}");
                int widest = System.Math.Max(stream.columnsStart, stream.columnsEnd);
                float halfBlock = 0.5f * (widest - 1) * stream.columnSpacing;
                if (stream.x - halfBlock < -halfWidth || stream.x + halfBlock > halfWidth)
                    errors.Add($"horde stream {h} x {stream.x} +/- {halfBlock} leaves the track (+/-{halfWidth} m)");
            }

            Assert.IsEmpty(errors, $"{level.name}:\n" + string.Join("\n", errors));
        }

        static IEnumerable<HeroDef> Heroes() => LoadAll<HeroDef>();

        /// <summary>GDD section 4: every hero deals the same DPS per animal, within the tolerance.</summary>
        [TestCaseSource(nameof(Heroes))]
        public void Hero_DpsMatchesTheBalanceTarget(HeroDef hero)
        {
            CombatTuning combat = Tuning().combat;
            Assert.AreEqual(combat.heroDpsPerAnimal, hero.stats.DamagePerSecond,
                combat.heroDpsPerAnimal * combat.heroBalanceTolerance, hero.displayName);
        }
    }
}
