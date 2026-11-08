using System;
using FMODUnity;
using UnityEngine;

namespace Tide
{
    /// <summary>A clip on a <see cref="TideFmodTrack"/>: an FMOD event and its volume. Easing fades the volume.</summary>
    [Serializable]
    public class TideFmodClip : TideClip
    {
        [SerializeField] private EventReference fmodEvent;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        public EventReference Event
        {
            get => fmodEvent;
            set => fmodEvent = value;
        }

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        public override string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;
                if (fmodEvent.IsNull)
                    return "No Event";
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(fmodEvent.Path))
                    return fmodEvent.Path.Substring(fmodEvent.Path.LastIndexOf('/') + 1);
#endif
                return "FMOD Event";
            }
        }
    }
}
