using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tide
{
    /// <summary>
    /// Exposes the parameters of one of a renderer's materials as timeline
    /// channels: every float, range, int, colour and vector property the
    /// shader shows in the inspector, by property name. Values are written
    /// through a MaterialPropertyBlock, so the material asset is never
    /// changed (an edit-mode preview leaves no trace), and properties back at
    /// the material's own value are dropped from the block; with none left
    /// the block is removed. The block replaces any other block on that
    /// renderer and material, and renderers using one leave the SRP Batcher.
    /// </summary>
    [AddComponentMenu("Tide/Tide Material Channels")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class TideMaterialChannels : MonoBehaviour, ITideChannelTarget
    {
        [Tooltip("Which of the renderer's materials the channels drive")]
        [SerializeField, Min(0)] private int materialIndex;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        // Not kept over a domain reload, which empties the readonly lists it keys
        [NonSerialized] private Shader _channelsShader;
        private readonly List<string> _names = new();
        private readonly List<TideValueType> _types = new();
        private readonly Dictionary<int, TideValue> _overrides = new();

        public int MaterialIndex
        {
            get => materialIndex;
            set
            {
                ClearOverrides();
                materialIndex = Mathf.Max(0, value);
            }
        }

        private Material SharedMaterial
        {
            get
            {
                if (_renderer == null)
                    _renderer = GetComponent<Renderer>();
                Material[] materials = _renderer != null ? _renderer.sharedMaterials : null;
                return materials != null && materialIndex < materials.Length ? materials[materialIndex] : null;
            }
        }

        private void OnDisable()
        {
            ClearOverrides();
        }

        private void OnValidate()
        {
            ClearOverrides();
        }

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
            int id = Shader.PropertyToID(_names[index]);
            return _overrides.TryGetValue(id, out TideValue value) ? value : MaterialValue(id, _types[index]);
        }

        public void SetChannelValue(int index, TideValue value)
        {
            RefreshChannels();
            int id = Shader.PropertyToID(_names[index]);
            TideValueType type = _types[index];
            value = value.ConvertTo(type);
            if (value.Equals(MaterialValue(id, type)))
                _overrides.Remove(id);
            else
                _overrides[id] = value;
            ApplyOverrides();
        }

        // The shader's channels, rebuilt when the material's shader changes
        private void RefreshChannels()
        {
            Material material = SharedMaterial;
            Shader shader = material != null ? material.shader : null;
            if (shader == _channelsShader)
                return;
            ClearOverrides();
            _channelsShader = shader;
            _names.Clear();
            _types.Clear();
            if (shader == null)
                return;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;
                TideValueType? type = shader.GetPropertyType(i) switch
                {
                    ShaderPropertyType.Float or ShaderPropertyType.Range => TideValueType.Float,
                    ShaderPropertyType.Int => TideValueType.Int,
                    ShaderPropertyType.Color => TideValueType.Color,
                    ShaderPropertyType.Vector => TideValueType.Vector4,
                    _ => null,
                };
                if (type == null)
                    continue;
                _names.Add(shader.GetPropertyName(i));
                _types.Add(type.Value);
            }
        }

        private TideValue MaterialValue(int id, TideValueType type)
        {
            Material material = SharedMaterial;
            if (material == null)
                return TideValue.Default(type);
            return type switch
            {
                TideValueType.Int => TideValue.Int(material.GetInteger(id)),
                TideValueType.Color => TideValue.Color(material.GetColor(id)),
                TideValueType.Vector4 => TideValue.Vector4(material.GetVector(id)),
                _ => TideValue.Float(material.GetFloat(id)),
            };
        }

        private void ApplyOverrides()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            if (_renderer == null)
                return;
            if (_overrides.Count == 0)
            {
                _renderer.SetPropertyBlock(null, materialIndex);
                return;
            }
            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            foreach (KeyValuePair<int, TideValue> entry in _overrides)
            {
                TideValue value = entry.Value;
                switch (value.Type)
                {
                    case TideValueType.Int:
                        _block.SetInteger(entry.Key, value.AsInt);
                        break;
                    case TideValueType.Color:
                        _block.SetColor(entry.Key, value.AsColor);
                        break;
                    case TideValueType.Vector4:
                        _block.SetVector(entry.Key, value.AsVector4);
                        break;
                    default:
                        _block.SetFloat(entry.Key, value.AsFloat);
                        break;
                }
            }
            _renderer.SetPropertyBlock(_block, materialIndex);
        }

        private void ClearOverrides()
        {
            if (_overrides.Count == 0)
                return;
            _overrides.Clear();
            ApplyOverrides();
        }
    }
}
