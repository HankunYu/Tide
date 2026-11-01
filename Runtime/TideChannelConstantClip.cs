using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tide
{
    /// <summary>Holds the channel at one value; ease it in to get there gradually.</summary>
    [Serializable]
    [MovedFrom(false, "GaussianSplatting.Runtime", "GaussianSplatting", "GaussianChannelConstantClip")]
    public class TideChannelConstantClip : TideChannelClip
    {
        [SerializeField] private TideValue value = TideValue.Float(0f);

        public TideValue Value
        {
            get => value;
            set => this.value = value;
        }

        public override string DisplayName => string.IsNullOrEmpty(Name) ? value.ToString() : Name;

        public override TideValue Evaluate(double localTime)
        {
            return value;
        }
    }
}
