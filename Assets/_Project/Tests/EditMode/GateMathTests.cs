using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using NUnit.Framework;

namespace FarmFuryRampage.Tests
{
    public sealed class GateMathTests
    {
        const int Cap = 300;

        [TestCase(GateKind.Add, 8, 5, 13)]
        [TestCase(GateKind.Subtract, 3, 5, 2)]
        [TestCase(GateKind.Subtract, 10, 5, 0)]
        [TestCase(GateKind.Multiply, 2, 7, 14)]
        [TestCase(GateKind.Multiply, 3, 200, Cap)]
        [TestCase(GateKind.Divide, 2, 7, 3)]
        [TestCase(GateKind.Divide, 2, 1, 0)]
        [TestCase(GateKind.Add, 10, 295, Cap)]
        public void Apply_GivesExpectedHerd(GateKind kind, int value, int herd, int expected)
        {
            Assert.AreEqual(expected, GateMath.Apply(kind, value, herd, Cap));
        }

        [TestCase(-4.4f, 2, 0)]
        [TestCase(-0.1f, 2, 0)]
        [TestCase(0.1f, 2, 1)]
        [TestCase(99f, 2, 1)]
        [TestCase(-99f, 3, 0)]
        [TestCase(0f, 3, 1)]
        [TestCase(4f, 3, 2)]
        public void PanelIndex_SplitsTrackEvenly(float x, int panels, int expected)
        {
            Assert.AreEqual(expected, GateMath.PanelIndex(x, panels, 9f));
        }

        static GateTuning Tuning(int maxImprove = 10, float cooldown = 0f) =>
            new() { hitsPerStep = 2, maxImprove = maxImprove, hitCooldown = cooldown, panelDepth = 0.6f };

        [Test]
        public void RedGate_ShotUp_FlipsBlue()
        {
            var panel = GatePanelState.From(new GatePanelDef(GateKind.Subtract, 3));
            GateTuning tuning = Tuning();

            // GDD: every 2 hits raise the value by 1, so 8 hits take -3 to +1.
            for (int i = 0; i < 8; i++) GateMath.RegisterHit(ref panel, i, tuning);

            Assert.AreEqual(GateKind.Add, panel.kind);
            Assert.AreEqual(1, panel.value);
        }

        [Test]
        public void Improve_StopsAtMaxImprove()
        {
            var panel = GatePanelState.From(new GatePanelDef(GateKind.Subtract, 3));
            GateTuning tuning = Tuning(maxImprove: 5);

            for (int i = 0; i < 100; i++) GateMath.RegisterHit(ref panel, i, tuning);

            Assert.AreEqual(GateKind.Add, panel.kind);
            Assert.AreEqual(2, panel.value, "-3 + 5 = +2");
        }

        [Test]
        public void Improve_IgnoresHitsInsideCooldown()
        {
            var panel = GatePanelState.From(new GatePanelDef(GateKind.Add, 4));
            GateTuning tuning = Tuning(cooldown: 0.15f);

            for (int i = 0; i < 20; i++) GateMath.RegisterHit(ref panel, 1f, tuning);

            Assert.AreEqual(4, panel.value);
            Assert.AreEqual(1, panel.hits);
        }

        [TestCase(GateKind.Multiply)]
        [TestCase(GateKind.Divide)]
        public void MultiplyAndDivide_AreFixed(GateKind kind)
        {
            var panel = GatePanelState.From(new GatePanelDef(kind, 2));

            for (int i = 0; i < 20; i++) Assert.IsFalse(GateMath.RegisterHit(ref panel, i, Tuning()));

            Assert.AreEqual(kind, panel.kind);
            Assert.AreEqual(2, panel.value);
        }
    }
}
