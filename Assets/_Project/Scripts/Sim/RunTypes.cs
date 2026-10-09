using FarmFuryRampage.Data;

namespace FarmFuryRampage.Sim
{
    public enum RunPhase
    {
        Running,
        Won,
        Failed,
    }

    /// <summary>Player input for one tick: where the herd should steer to, in track X.</summary>
    public struct RunInput
    {
        public float targetX;
        public bool steadyHerd;
    }

    public struct Robot
    {
        public bool active;
        public int type;
        public float x;
        public float distance;
        public float hp;
        public float maxHp;
    }

    /// <summary>
    /// A shot in flight. Straight shots move by (vx, vd) and hit the first robot. Lobbed shots travel from start to
    /// target over <see cref="flightTime"/> (x/distance are the ground position under the arc) and explode on landing.
    /// </summary>
    public struct Projectile
    {
        public bool active;
        public bool lob;
        public float x;
        public float distance;
        public float vx;
        public float vd;
        public float damage;
        public float lifetime;
        public float startX;
        public float startDistance;
        public float targetX;
        public float targetDistance;
        public float elapsed;
        public float flightTime;

        /// <summary>0 at the throw, 1 at the explosion (lobs only).</summary>
        public float FlightFraction => flightTime > 0f ? elapsed / flightTime : 1f;
    }

    public sealed class GateRowState
    {
        public float distance;
        public GatePanelState[] panels;
        public bool passed;
        public int chosenPanel = -1;
    }

    public enum RunEventType
    {
        GatePassed,
        GateImproved,
        RobotKilled,
        AnimalsLost,
        Explosion,
        Won,
        Failed,
    }

    /// <summary>
    /// Something that happened during a tick, for the presentation layer (VFX, sound, haptics, analytics).
    /// <c>a</c>/<c>b</c> meaning depends on the type: GatePassed = herd before/after; GateImproved = row/panel;
    /// RobotKilled = robot type/scrap; AnimalsLost = animals lost/herd after; Explosion = robots hit.
    /// </summary>
    public readonly struct RunEvent
    {
        public readonly RunEventType type;
        public readonly float x;
        public readonly float distance;
        public readonly int a;
        public readonly int b;

        public RunEvent(RunEventType type, float x, float distance, int a = 0, int b = 0)
        {
            this.type = type;
            this.x = x;
            this.distance = distance;
            this.a = a;
            this.b = b;
        }
    }
}
