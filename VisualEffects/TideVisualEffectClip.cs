using System;

namespace Tide
{
    /// <summary>A span on a <see cref="TideVisualEffectTrack"/> during which the effect plays.</summary>
    [Serializable]
    public class TideVisualEffectClip : TideClip
    {
        public override string DisplayName => string.IsNullOrEmpty(Name) ? "Play" : Name;
    }
}
