using UnityEngine;

namespace FarmFuryRampage.Data
{
    /// <summary>Global run tuning. One asset; every gameplay number not owned by a hero, robot or level lives here.</summary>
    [CreateAssetMenu(menuName = "FarmFury Rampage/Game Tuning", fileName = "GameTuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Tooltip("Fixed simulation ticks per second.")]
        public int tickRate = 60;
        public TrackTuning track;
        public HerdTuning herd;
        public GateTuning gates;
        public CombatTuning combat;
    }
}
