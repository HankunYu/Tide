using System;
using UnityEngine;

namespace Tide
{
    /// <summary>
    /// A channel value of any <see cref="TideValueType"/>. Scalars, colours
    /// and vectors share one Vector4; a gradient is held by reference. A float
    /// converts to a Float value implicitly, so float channels read naturally.
    /// </summary>
    [Serializable]
    public struct TideValue : IEquatable<TideValue>
    {
        [SerializeField] private TideValueType type;
        [SerializeField] private Vector4 vector;
        [SerializeField] private Gradient gradient;

        private TideValue(TideValueType type, Vector4 vector, Gradient gradient)
        {
            this.type = type;
            this.vector = vector;
            this.gradient = gradient;
        }

        public TideValueType Type => type;

        public float AsFloat => vector.x;

        public int AsInt => Mathf.RoundToInt(vector.x);

        public bool AsBool => vector.x >= 0.5f;

        public Color AsColor => new(vector.x, vector.y, vector.z, vector.w);

        public Vector2 AsVector2 => vector;

        public Vector3 AsVector3 => vector;

        public Vector4 AsVector4 => vector;

        /// <summary>The gradient, or null for other types.</summary>
        public Gradient AsGradient => gradient;

        public static TideValue Float(float value) => new(TideValueType.Float, new Vector4(value, 0f, 0f, 0f), null);

        public static TideValue Int(int value) => new(TideValueType.Int, new Vector4(value, 0f, 0f, 0f), null);

        public static TideValue Bool(bool value) => new(TideValueType.Bool, new Vector4(value ? 1f : 0f, 0f, 0f, 0f), null);

        public static TideValue Color(Color value) => new(TideValueType.Color, new Vector4(value.r, value.g, value.b, value.a), null);

        public static TideValue Vector2(Vector2 value) => new(TideValueType.Vector2, value, null);

        public static TideValue Vector3(Vector3 value) => new(TideValueType.Vector3, value, null);

        public static TideValue Vector4(Vector4 value) => new(TideValueType.Vector4, value, null);

        public static TideValue Gradient(Gradient value) => new(TideValueType.Gradient, default, value);

        /// <summary>The default value of a type: zero, black, or an empty gradient.</summary>
        public static TideValue Default(TideValueType type) =>
            type == TideValueType.Gradient ? Gradient(new Gradient()) : new TideValue(type, default, null);

        public static implicit operator TideValue(float value) => Float(value);

        /// <summary>
        /// <paramref name="t"/> of the way from <paramref name="from"/> to
        /// <paramref name="to"/>, in <paramref name="to"/>'s type: continuous
        /// types interpolate (unclamped), discrete ones switch at 0.5.
        /// </summary>
        public static TideValue Blend(TideValue from, TideValue to, float t)
        {
            if (from.type != to.type)
                from = from.ConvertTo(to.type);
            switch (to.type)
            {
                case TideValueType.Int:
                case TideValueType.Bool:
                case TideValueType.Gradient:
                    return t < 0.5f ? from : to;
                default:
                    return new TideValue(to.type, UnityEngine.Vector4.LerpUnclamped(from.vector, to.vector, t), null);
            }
        }

        /// <summary>
        /// The value as another type: components carry over (a scalar fills a
        /// colour's rgb with alpha 1), and a gradient converts from and to its
        /// colour at the start.
        /// </summary>
        public TideValue ConvertTo(TideValueType target)
        {
            if (target == type)
                return this;
            if (target == TideValueType.Gradient)
            {
                var single = new Gradient();
                Color c = ToColor();
                single.SetKeys(new[] { new GradientColorKey(c, 0f) }, new[] { new GradientAlphaKey(c.a, 0f) });
                return Gradient(single);
            }
            switch (target)
            {
                case TideValueType.Int:
                    return Int(Mathf.RoundToInt(Scalar()));
                case TideValueType.Bool:
                    return Bool(Scalar() >= 0.5f);
                case TideValueType.Float:
                    return Float(Scalar());
                case TideValueType.Color:
                    return Color(ToColor());
                default:
                    Vector4 v = type == TideValueType.Gradient ? (Vector4)ToColor() : vector;
                    return new TideValue(target, v, null);
            }
        }

        /// <summary>One number standing for the value, e.g. to plot it: the first component, or a colour's luminance.</summary>
        public float Scalar()
        {
            switch (type)
            {
                case TideValueType.Color:
                case TideValueType.Gradient:
                    Color c = ToColor();
                    return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                default:
                    return vector.x;
            }
        }

        private Color ToColor()
        {
            switch (type)
            {
                case TideValueType.Gradient:
                    return gradient != null ? gradient.Evaluate(0f) : UnityEngine.Color.white;
                case TideValueType.Color:
                case TideValueType.Vector4:
                    return new Color(vector.x, vector.y, vector.z, vector.w);
                case TideValueType.Vector3:
                case TideValueType.Vector2:
                    return new Color(vector.x, vector.y, vector.z, 1f);
                default:
                    return new Color(vector.x, vector.x, vector.x, 1f);
            }
        }

        public bool Equals(TideValue other) =>
            type == other.type && vector == other.vector && ReferenceEquals(gradient, other.gradient);

        public override bool Equals(object obj) => obj is TideValue other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(type, vector, gradient);

        public override string ToString()
        {
            switch (type)
            {
                case TideValueType.Float:
                    return vector.x.ToString("0.###");
                case TideValueType.Int:
                    return AsInt.ToString();
                case TideValueType.Bool:
                    return AsBool ? "On" : "Off";
                case TideValueType.Color:
                    return $"RGBA({vector.x:0.##}, {vector.y:0.##}, {vector.z:0.##}, {vector.w:0.##})";
                case TideValueType.Vector2:
                    return $"({vector.x:0.##}, {vector.y:0.##})";
                case TideValueType.Vector3:
                    return $"({vector.x:0.##}, {vector.y:0.##}, {vector.z:0.##})";
                case TideValueType.Vector4:
                    return $"({vector.x:0.##}, {vector.y:0.##}, {vector.z:0.##}, {vector.w:0.##})";
                default:
                    return "Gradient";
            }
        }
    }
}
