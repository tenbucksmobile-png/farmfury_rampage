using Unity.Mathematics;

namespace FarmFuryRampage.Sim
{
    /// <summary>Phyllotaxis (sunflower) herd formation: slot i sits at radius c*sqrt(i), angle i * golden angle.</summary>
    public static class Formation
    {
        /// <summary>The golden angle in radians, pi * (3 - sqrt(5)). A maths constant, not a tuning value.</summary>
        public const float GoldenAngle = 2.39996323f;

        /// <summary>Offset of slot <paramref name="index"/> from the herd centre, as (x, distance).</summary>
        public static float2 SlotOffset(int index, float spacing)
        {
            float radius = spacing * math.sqrt(index);
            float angle = index * GoldenAngle;
            return new float2(radius * math.cos(angle), radius * math.sin(angle));
        }

        /// <summary>Radius of the outermost slot centre for a formation of <paramref name="drawn"/> animals.</summary>
        public static float Radius(int drawn, float spacing) => drawn <= 1 ? 0f : spacing * math.sqrt(drawn - 1);

        /// <summary>Radius of the whole herd including the animals' own size (half a slot).</summary>
        public static float FootprintRadius(int drawn, float spacing) => Radius(drawn, spacing) + spacing * 0.5f;
    }
}
