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
    }
}
