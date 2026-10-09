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

        [Header("Art (auto-filled from Assets/_Project/Art/Robots/<DisplayName without spaces>/)")]
        [Tooltip("Front view, rolling down the screen toward the herd. Files named walk_00, walk_01, ...")]
        public Sprite[] walkFrames = System.Array.Empty<Sprite>();
        [Tooltip("Drawn width as a multiple of the robot's collision diameter.")]
        public float artScale = 1.25f;
        [Tooltip("Walk animation frames per second.")]
        public float frameRate = 6f;
    }
}
