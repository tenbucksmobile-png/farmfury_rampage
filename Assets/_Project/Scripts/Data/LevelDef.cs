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

    /// <summary>A group of one robot type, laid out along the track.</summary>
    [Serializable]
    public sealed class WaveDef
    {
        [Tooltip("Distance along the track of the first robot, in metres.")]
        public float distance;
        public RobotDef robot;
        [Tooltip("Track X of the group centre in metres (0 = middle).")]
        public float x;
        [Min(1)] public int count = 1;
        [Tooltip("Metres between robots along the track.")]
        public float spacing;
        [Tooltip("Random sideways offset (+/- metres) per robot, from the level seed.")]
        public float xJitter;
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
    }
}
