using UnityEngine;

namespace FarmFuryRampage.Data
{
    [CreateAssetMenu(menuName = "FarmFury Rampage/Robot", fileName = "Robot_")]
    public sealed class RobotDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [Range(1, 5)] public int tier = 1;
        public RobotStats stats;
        [Tooltip("Placeholder colour until robot art exists.")]
        public Color greyboxColor = Color.grey;
    }
}
