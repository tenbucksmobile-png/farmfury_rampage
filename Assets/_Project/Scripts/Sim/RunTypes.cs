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

    public struct Projectile
    {
        public bool active;
        public float x;
        public float distance;
        public float vx;
        public float vd;
        public float damage;
        public float lifetime;
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
        Won,
        Failed,
    }

    /// <summary>
    /// Something that happened during a tick, for the presentation layer (VFX, sound, haptics, analytics).
    /// <c>a</c>/<c>b</c> meaning depends on the type: GatePassed = herd before/after; GateImproved = row/panel;
    /// RobotKilled = robot type/scrap; AnimalsLost = animals lost/herd after.
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
