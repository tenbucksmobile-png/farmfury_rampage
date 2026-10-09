using UnityEngine;

namespace FarmFuryRampage.Data
{
    [CreateAssetMenu(menuName = "FarmFury Rampage/Hero", fileName = "Hero_")]
    public sealed class HeroDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public HeroStats stats;
        [Tooltip("Placeholder colour until hero art exists.")]
        public Color greyboxColor = Color.white;

        [Header("Art (auto-filled from Assets/_Project/Art/Heroes/<DisplayName>/)")]
        [Tooltip("Back view, running up the screen. Files named run_00, run_01, ...")]
        public Sprite[] runFrames = System.Array.Empty<Sprite>();
        [Tooltip("The thrown egg. File named egg.")]
        public Sprite projectileSprite;
        [Tooltip("Drawn width of one animal as a multiple of the herd slot spacing (above 1 they overlap into a crowd).")]
        public float artScale = 1.6f;
        [Tooltip("Run animation frames per second.")]
        public float frameRate = 10f;
    }
}
