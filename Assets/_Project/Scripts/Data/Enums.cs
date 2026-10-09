namespace FarmFuryRampage.Data
{
    /// <summary>Gate panel operations. Add/Subtract can be shot to improve; Multiply/Divide are fixed and let shots through.</summary>
    public enum GateKind
    {
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    /// <summary>How a hero's shots travel.</summary>
    public enum AttackPattern
    {
        /// <summary>Flies straight and hits the first robot it touches.</summary>
        Straight,
        /// <summary>Thrown in an arc at a target, then explodes and damages every robot in the blast radius (Cluck's egg grenade).</summary>
        Lob,
    }
}
