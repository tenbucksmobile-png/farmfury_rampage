using UnityEngine;

namespace FarmFuryRampage.Data
{
    /// <summary>
    /// Shared run art (ground, effects, gate frames). Auto-filled from Assets/_Project/Art/{Track,Effects,Gates}/ by
    /// FarmFury Rampage > Art > Assign Art From Folders, which also runs whenever files in Assets/_Project/Art change.
    /// Anything left empty is drawn as a greybox shape.
    /// </summary>
    [CreateAssetMenu(menuName = "FarmFury Rampage/Run Art", fileName = "RunArt")]
    public sealed class RunArt : ScriptableObject
    {
        [Tooltip("Track ground, tiled up the screen. Should span the full track width. File: Track/ground.")]
        public Sprite ground;
        [Tooltip("Egg-grenade explosion. File: Effects/blast.")]
        public Sprite blast;
        [Tooltip("Puff where animals are knocked out of the herd. File: Effects/feather (e.g. FeatherBurst.png).")]
        public Sprite feathers;
        [Tooltip("Gate panel frames. Files: Gates/add, Gates/subtract, Gates/multiply, Gates/divide.")]
        public Sprite gateAdd;
        public Sprite gateSubtract;
        public Sprite gateMultiply;
        public Sprite gateDivide;
    }
}
