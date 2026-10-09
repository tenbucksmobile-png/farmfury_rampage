using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmFuryRampage.Data
{
    /// <summary>One row of gates across the track. The herd passes exactly one panel.</summary>
    [Serializable]
    public sealed class GateRowDef
    {
        [Tooltip("Distance along the track in metres.")]
        public float distance;
        public GatePanelDef[] panels = Array.Empty<GatePanelDef>();
    }

    /// <summary>
    /// A group of one robot type: a block <see cref="columns"/> wide and <see cref="count"/> rows deep. One column is
    /// a single-file line; several columns make a tightly packed horde that rolls toward the herd as one mass.
    /// </summary>
    [Serializable]
    public sealed class WaveDef
    {
        [Tooltip("Distance along the track of the front row, in metres.")]
        public float distance;
        public RobotDef robot;
        [Tooltip("Track X of the group centre in metres (0 = middle).")]
        public float x;
        [Min(1), Tooltip("Rows (robots per column).")]
        public int count = 1;
        [Tooltip("Metres between rows along the track.")]
        public float spacing;
        [Min(1), Tooltip("Robots side by side in each row.")]
        public int columns = 1;
        [Tooltip("Metres between columns across the track.")]
        public float columnSpacing;
        [Tooltip("Random sideways offset (+/- metres) per robot, from the level seed.")]
        public float xJitter;
    }

    /// <summary>
    /// An unbroken, tightly packed horde from <see cref="startDistance"/> to <see cref="endDistance"/>: a row every
    /// <see cref="rowSpacing"/> metres, widening from <see cref="columnsStart"/> to <see cref="columnsEnd"/> robots.
    /// Bigger robots (elites, bosses) placed inside it clear their own space in the pack.
    /// </summary>
    [Serializable]
    public sealed class HordeStreamDef
    {
        public float startDistance;
        public float endDistance;
        public RobotDef robot;
        [Tooltip("Track X of the stream centre in metres.")]
        public float x;
        [Min(1)] public int columnsStart = 3;
        [Min(1)] public int columnsEnd = 3;
        [Tooltip("Metres between rows along the track.")]
        public float rowSpacing = 1f;
        [Tooltip("Metres between robots across the track.")]
        public float columnSpacing = 1f;
    }

    /// <summary>
    /// A level, authored only in track coordinates (x, distance). The LevelDef validation test rejects anything
    /// outside the track bounds or the level length.
    /// </summary>
    [CreateAssetMenu(menuName = "FarmFury Rampage/Level", fileName = "Level_")]
    public sealed class LevelDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("Track length in metres; reaching it wins the run (bosses arrive in Phase 2).")]
        public float length;
        [Min(1), Tooltip("Drives robot HP scaling (plan G10); not the level index.")]
        public int difficultyLevel = 1;
        [Min(1), Tooltip("Starting herd until the Herd Level exists.")]
        public int startingHerd = 5;
        [Tooltip("Seed for every random choice in the level (wave jitter).")]
        public uint seed = 1;
        public List<GateRowDef> gateRows = new();
        public List<WaveDef> waves = new();
        public List<HordeStreamDef> hordeStreams = new();
    }
}
