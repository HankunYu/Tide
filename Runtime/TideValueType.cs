namespace Tide
{
    /// <summary>What a channel holds, which also decides how clips mix on it.</summary>
    public enum TideValueType
    {
        /// <summary>Interpolates.</summary>
        Float,
        /// <summary>Switches halfway through a blend: counts, enum values.</summary>
        Int,
        /// <summary>Switches halfway through a blend.</summary>
        Bool,
        /// <summary>A colour as its owner holds it, may exceed 1 (HDR); interpolates per component.</summary>
        Color,
        Vector2,
        Vector3,
        Vector4,
        /// <summary>Switches halfway through a blend: gradients do not interpolate.</summary>
        Gradient,
    }
}
