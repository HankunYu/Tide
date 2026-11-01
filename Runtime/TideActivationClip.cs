using System;
using UnityEngine.Scripting.APIUpdating;

namespace Tide
{
    /// <summary>A span during which a <see cref="TideActivationTrack"/> keeps its GameObject active.</summary>
    [Serializable]
    [MovedFrom(false, "GaussianSplatting.Runtime", "GaussianSplatting", "GaussianActivationClip")]
    public class TideActivationClip : TideClip
    {
        public override bool IsStatic => true;

        public override string DisplayName => string.IsNullOrEmpty(Name) ? "Active" : Name;
    }
}
