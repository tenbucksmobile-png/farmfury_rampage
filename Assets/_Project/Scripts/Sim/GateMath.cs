using FarmFuryRampage.Data;

namespace FarmFuryRampage.Sim
{
    /// <summary>Live state of one gate panel. +/- panels can change kind when shot (a red -3 can become a blue +1).</summary>
    public struct GatePanelState
    {
        public GateKind kind;
        public int value;
        /// <summary>Authored value as a signed number (+8 or -10); the improve cap is measured from here.</summary>
        public int authoredSigned;
        public int hits;
        public float lastCountedHitTime;

        public static GatePanelState From(GatePanelDef def)
        {
            return new GatePanelState
            {
                kind = def.kind,
                value = def.value,
                authoredSigned = GateMath.IsImprovable(def.kind) ? GateMath.ToSigned(def.kind, def.value) : 0,
                hits = 0,
                lastCountedHitTime = float.NegativeInfinity,
            };
        }
    }

    public static class GateMath
    {
        /// <summary>Applies a gate to the herd count. x and / round down; the result is clamped to [0, cap] (plan G7).</summary>
        public static int Apply(GateKind kind, int value, int count, int cap)
        {
            long result = kind switch
            {
                GateKind.Add => (long)count + value,
                GateKind.Subtract => (long)count - value,
                GateKind.Multiply => (long)count * value,
                GateKind.Divide => value <= 0 ? count : count / value,
                _ => count,
            };
            if (result < 0) return 0;
            if (result > cap) return cap;
            return (int)result;
        }

        /// <summary>Only + and - gates can be shot to improve; x and / are fixed and let shots through.</summary>
        public static bool IsImprovable(GateKind kind) => kind == GateKind.Add || kind == GateKind.Subtract;

        public static int ToSigned(GateKind kind, int value) => kind == GateKind.Subtract ? -value : value;

        /// <summary>Signed value back to a panel. Zero and above show as a blue + gate (red gates flip at 0).</summary>
        public static void FromSigned(int signed, out GateKind kind, out int value)
        {
            if (signed >= 0)
            {
                kind = GateKind.Add;
                value = signed;
            }
            else
            {
                kind = GateKind.Subtract;
                value = -signed;
            }
        }

        /// <summary>
        /// Registers a shot hitting a panel. Every <see cref="GateTuning.hitsPerStep"/> counted hits raise the value by 1,
        /// up to <see cref="GateTuning.maxImprove"/> above the authored value; hits inside the cooldown are absorbed
        /// without counting (plan G1). Returns true when the panel's value changed.
        /// </summary>
        public static bool RegisterHit(ref GatePanelState panel, float time, in GateTuning tuning)
        {
            if (!IsImprovable(panel.kind)) return false;
            if (time - panel.lastCountedHitTime < tuning.hitCooldown) return false;

            panel.lastCountedHitTime = time;
            panel.hits++;
            int hitsPerStep = tuning.hitsPerStep < 1 ? 1 : tuning.hitsPerStep;
            if (panel.hits < hitsPerStep) return false;
            panel.hits = 0;

            int signed = ToSigned(panel.kind, panel.value);
            if (signed >= panel.authoredSigned + tuning.maxImprove) return false;

            FromSigned(signed + 1, out panel.kind, out panel.value);
            return true;
        }

        /// <summary>Which panel of a row covers track position x. Panels split the track width evenly.</summary>
        public static int PanelIndex(float x, int panelCount, float trackWidth)
        {
            if (panelCount <= 1) return 0;
            float panelWidth = trackWidth / panelCount;
            int index = (int)System.Math.Floor((x + trackWidth * 0.5f) / panelWidth);
            if (index < 0) return 0;
            if (index >= panelCount) return panelCount - 1;
            return index;
        }
    }
}
