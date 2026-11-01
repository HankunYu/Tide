using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tide
{
    /// <summary>
    /// Takes the channel from one value to another over the clip, along a 0–1
    /// shape curve. Discrete values (Int, Bool, Gradient) switch halfway.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "GaussianSplatting.Runtime", "GaussianSplatting", "GaussianChannelRampClip")]
    public class TideChannelRampClip : TideChannelClip
    {
        [SerializeField] private TideValue from = TideValue.Float(0f);
        [SerializeField] private TideValue to = TideValue.Float(1f);

        [Tooltip("Progress over the clip (0–1 in, 0–1 out); linear by default")]
        [SerializeField] private AnimationCurve shape = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public TideValue From
        {
            get => from;
            set => from = value;
        }

        public TideValue To
        {
            get => to;
            set => to = value;
        }

        public AnimationCurve Shape
        {
            get => shape;
            set => shape = value;
        }

        public override string DisplayName =>
            string.IsNullOrEmpty(Name) ? $"{from} → {to}" : Name;

        public override TideValue Evaluate(double localTime)
        {
            float t = (float)(localTime / Duration);
            if (shape != null && shape.length > 0)
                t = shape.Evaluate(t);
            return TideValue.Blend(from, to, t);
        }
    }
}
