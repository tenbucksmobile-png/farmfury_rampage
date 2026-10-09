using System;
using UnityEngine;

namespace FarmFuryRampage.Data
{
    /// <summary>Track geometry and camera framing. 1 Unity unit = 1 metre.</summary>
    [Serializable]
    public struct TrackTuning
    {
        [Tooltip("Full track width in metres; track X runs from -width/2 to +width/2.")]
        public float width;
        [Tooltip("Herd forward speed in m/s.")]
        public float pace;
        [Tooltip("Visible world width in metres (portrait camera).")]
        public float viewWidth;
        [Range(0f, 1f), Tooltip("Herd position as a fraction of screen height from the bottom.")]
        public float herdScreenY;
        [Tooltip("Robots spawn when the herd is this many metres from their spawn distance.")]
        public float spawnAhead;
        [Tooltip("Robots this far behind the herd centre despawn without penalty.")]
        public float despawnBehind;
    }

    [Serializable]
    public struct HerdTuning
    {
        [Tooltip("Maximum herd size (GDD: 300).")]
        public int cap;
        [Tooltip("Maximum animals drawn individually (GDD: 60). Each drawn animal carries count/drawn damage.")]
        public int drawnCap;
        [Tooltip("Phyllotaxis spacing constant c: slot i sits at radius c*sqrt(i).")]
        public float slotSpacing;
        [Tooltip("Maximum lateral herd speed in m/s.")]
        public float steerSpeed;
        [Tooltip("Track widths moved per screen width of drag.")]
        public float dragSensitivity;
        [Tooltip("Steer speed multiplier when the steady-herd assist is on.")]
        public float steadySteerMultiplier;
    }

    [Serializable]
    public struct GateTuning
    {
        [Tooltip("Counted hits needed to raise a +/- gate by 1 (GDD: 2).")]
        public int hitsPerStep;
        [Tooltip("Maximum a +/- gate can be improved above its authored value (plan G1).")]
        public int maxImprove;
        [Tooltip("Seconds between counted hits on one panel (plan G1); extra shots are absorbed without counting.")]
        public float hitCooldown;
        [Tooltip("Depth of a gate panel along the track, in metres (shot collision band).")]
        public float panelDepth;
    }

    [Serializable]
    public struct CombatTuning
    {
        [Tooltip("Robot HP multiplier per difficulty level: x(1 + k*(level-1)). GDD: 0.12.")]
        public float hpScalePerLevel;
        [Tooltip("Maximum live projectiles (GDD performance target: 400).")]
        public int projectileCap;
        [Tooltip("Damage per second every hero must deal per animal (GDD section 4: 10).")]
        public float heroDpsPerAnimal;
        [Tooltip("Allowed hero DPS deviation as a fraction (GDD section 4: 0.05 = 5%).")]
        public float heroBalanceTolerance;
    }

    /// <summary>Firing behaviour of a hero. Every hero must come out at the same DPS per animal.</summary>
    [Serializable]
    public struct HeroStats
    {
        [Tooltip("Seconds between shots per animal.")]
        public float fireInterval;
        public int projectilesPerShot;
        [Tooltip("Total fan angle in degrees across all projectiles of one shot.")]
        public float spreadDegrees;
        public float damagePerHit;
        [Tooltip("Metres a projectile travels before expiring (plan G2: 12).")]
        public float range;
        public float projectileSpeed;
        public float projectileRadius;

        public float DamagePerSecond => fireInterval > 0f ? projectilesPerShot * damagePerHit / fireInterval : 0f;
    }

    [Serializable]
    public struct RobotStats
    {
        public float hp;
        [Tooltip("Speed toward the herd in m/s.")]
        public float speed;
        [Tooltip("Animals knocked out when this robot reaches the herd.")]
        public int bite;
        public float radius;
        [Range(0f, 1f), Tooltip("Fraction of speed used to drift sideways toward the herd (plan G3).")]
        public float homing;
        [Tooltip("Scrap dropped on destruction.")]
        public int scrap;
    }

    [Serializable]
    public struct GatePanelDef
    {
        public GateKind kind;
        [Min(0), Tooltip("Magnitude: +8 is Add 8, -10 is Subtract 10, x2 is Multiply 2.")]
        public int value;

        public GatePanelDef(GateKind kind, int value)
        {
            this.kind = kind;
            this.value = value;
        }
    }
}
