using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Tide
{
    /// <summary>
    /// Exposes a visual effect's exposed properties as timeline channels:
    /// every float, int, bool, vector, colour and gradient property of its
    /// graph, by exposed name.
    /// </summary>
    [AddComponentMenu("Tide/Tide Visual Effect Channels")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VisualEffect))]
    public class TideVisualEffectChannels : MonoBehaviour, ITideChannelTarget
    {
        private VisualEffect _effect;
        // Not kept over a domain reload, which empties the readonly lists it keys
        [NonSerialized] private VisualEffectAsset _channelsAsset;
        private readonly List<VFXExposedProperty> _exposed = new();
        private readonly List<string> _names = new();
        private readonly List<TideValueType> _types = new();

        private VisualEffect Effect => _effect != null ? _effect : _effect = GetComponent<VisualEffect>();

        public int ChannelCount
        {
            get
            {
                RefreshChannels();
                return _names.Count;
            }
        }

        public string GetChannelName(int index)
        {
            RefreshChannels();
            return _names[index];
        }

        public TideValueType GetChannelType(int index)
        {
            RefreshChannels();
            return _types[index];
        }

        public TideValue GetChannelValue(int index)
        {
            RefreshChannels();
            VisualEffect effect = Effect;
            int id = Shader.PropertyToID(_names[index]);
            return _types[index] switch
            {
                TideValueType.Int => TideValue.Int(effect.GetInt(id)),
                TideValueType.Bool => TideValue.Bool(effect.GetBool(id)),
                TideValueType.Vector2 => TideValue.Vector2(effect.GetVector2(id)),
                TideValueType.Vector3 => TideValue.Vector3(effect.GetVector3(id)),
                TideValueType.Vector4 => TideValue.Vector4(effect.GetVector4(id)),
                TideValueType.Color => TideValue.Color(effect.GetVector4(id)),
                TideValueType.Gradient => TideValue.Gradient(effect.GetGradient(id)),
                _ => TideValue.Float(effect.GetFloat(id)),
            };
        }

        public void SetChannelValue(int index, TideValue value)
        {
            RefreshChannels();
            VisualEffect effect = Effect;
            int id = Shader.PropertyToID(_names[index]);
            value = value.ConvertTo(_types[index]);
            switch (_types[index])
            {
                case TideValueType.Int:
                    effect.SetInt(id, value.AsInt);
                    break;
                case TideValueType.Bool:
                    effect.SetBool(id, value.AsBool);
                    break;
                case TideValueType.Vector2:
                    effect.SetVector2(id, value.AsVector2);
                    break;
                case TideValueType.Vector3:
                    effect.SetVector3(id, value.AsVector3);
                    break;
                case TideValueType.Vector4:
                case TideValueType.Color:
                    effect.SetVector4(id, value.AsVector4);
                    break;
                case TideValueType.Gradient:
                    if (value.AsGradient != null)
                        effect.SetGradient(id, value.AsGradient);
                    break;
                default:
                    effect.SetFloat(id, value.AsFloat);
                    break;
            }
        }

        // The graph's exposed properties, rebuilt when the effect's asset changes
        private void RefreshChannels()
        {
            VisualEffectAsset asset = Effect != null ? Effect.visualEffectAsset : null;
            if (asset == _channelsAsset)
                return;
            _channelsAsset = asset;
            _names.Clear();
            _types.Clear();
            if (asset == null)
                return;
            _exposed.Clear();
            asset.GetExposedProperties(_exposed);
            foreach (VFXExposedProperty property in _exposed)
            {
                TideValueType? type = TypeOf(property.type);
                if (type == null)
                    continue;
                _names.Add(property.name);
                _types.Add(type.Value);
            }
        }

        private static TideValueType? TypeOf(Type type)
        {
            if (type == typeof(float))
                return TideValueType.Float;
            if (type == typeof(int) || type == typeof(uint))
                return TideValueType.Int;
            if (type == typeof(bool))
                return TideValueType.Bool;
            if (type == typeof(Vector2))
                return TideValueType.Vector2;
            if (type == typeof(Vector3))
                return TideValueType.Vector3;
            if (type == typeof(Vector4))
                return TideValueType.Vector4;
            if (type == typeof(Color))
                return TideValueType.Color;
            if (type == typeof(Gradient))
                return TideValueType.Gradient;
            return null;
        }
    }
}
