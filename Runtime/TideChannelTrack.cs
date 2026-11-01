using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Scripting.APIUpdating;

namespace Tide
{
    /// <summary>
    /// Drives one named channel of an <see cref="ITideChannelTarget"/>
    /// (a material parameter, a light's intensity, an effect's weight...).
    /// Overlapping clips mix by weight; where their weights add up to less
    /// than one the channel's own value makes up the rest. Outside every clip
    /// the channel gets its own value back once and is then left alone.
    /// </summary>
    [Serializable]
    [TideDisplay("Channel", 0.27f, 0.62f, 0.60f)]
    [MovedFrom(false, "GaussianSplatting.Runtime", "GaussianSplatting", "GaussianChannelTrack")]
    public class TideChannelTrack : TideClipTrack
    {
        [Tooltip("Name of the channel on the bound target")]
        [SerializeField] private string channel;

        public string Channel
        {
            get => channel;
            set => channel = value;
        }

        public override Type BindingType => typeof(Component);

        public override Type ClipType => typeof(TideChannelClip);

        public override bool CanBind(Object target)
        {
            return target is Component && target is ITideChannelTarget;
        }

        /// <summary>Index of <paramref name="channelName"/> on <paramref name="target"/>, or -1.</summary>
        public static int FindChannel(ITideChannelTarget target, string channelName)
        {
            for (int i = 0; i < target.ChannelCount; i++)
            {
                if (target.GetChannelName(i) == channelName)
                    return i;
            }
            return -1;
        }

        public override TideTrackInstance CreateInstance(TidePlayer player, Object binding)
        {
            var target = (ITideChannelTarget)binding;
            int index = FindChannel(target, channel);
            if (index < 0)
            {
                Debug.LogWarning($"Timeline track '{Name}': {binding.name} has no channel '{channel}'", binding);
                return null;
            }
            return new Instance(this, player, target, binding, index);
        }

        private sealed class Instance : TideTrackInstance
        {
            private readonly TideChannelTrack _track;
            private readonly TidePlayer _player;
            private readonly ITideChannelTarget _target;
            private readonly Object _targetObject;
            private readonly int _channel;
            private readonly TideValueType _type;
            private readonly TideValue _ownValue;
            private readonly List<TideClipSample> _samples = new();
            private bool _driving;
            // The value this track last gave the channel, valid while _hasWritten
            private TideValue _written;
            private bool _hasWritten;

            public Instance(TideChannelTrack track, TidePlayer player, ITideChannelTarget target, Object targetObject, int channel)
            {
                _track = track;
                _player = player;
                _target = target;
                _targetObject = targetObject;
                _channel = channel;
                _type = target.GetChannelType(channel);
                _ownValue = target.GetChannelValue(channel).ConvertTo(_type);
            }

            public override void Evaluate(in TideEvaluation evaluation)
            {
                if (_targetObject == null)
                    return;
                _track.Gather(evaluation, _samples);
                // Playback may skip a value already written; any other evaluation writes it again
                bool force = !evaluation.Continuous || !_player.SkipUnchangedChannels;
                if (_samples.Count == 0)
                {
                    if (_driving)
                        Write(_ownValue, force);
                    _driving = false;
                    return;
                }

                // Mix pairwise in the channel's type, so discrete channels switch
                TideValue value = _ownValue;
                float total = 0f;
                foreach (TideClipSample sample in _samples)
                {
                    if (sample.Weight <= 0f)
                        continue;
                    TideValue clipValue = ((TideChannelClip)_track.Clips[sample.ClipIndex]).Evaluate(sample.LocalTime).ConvertTo(_type);
                    total += sample.Weight;
                    value = total == sample.Weight ? clipValue : TideValue.Blend(value, clipValue, sample.Weight / total);
                }
                if (total < 1f)
                    value = TideValue.Blend(_ownValue, value, total);
                Write(value, force);
                _driving = true;
            }

            public override void Restore()
            {
                _hasWritten = false;
                if (_targetObject != null)
                    _target.SetChannelValue(_channel, _ownValue);
            }

            private void Write(TideValue value, bool force)
            {
                if (!force && _hasWritten && value.Equals(_written))
                    return;
                _target.SetChannelValue(_channel, value);
                _written = value;
                _hasWritten = true;
            }

            public override void CollectPhaseTargets(List<ITidePhaseTarget> targets)
            {
                if (_targetObject is ITidePhaseTarget target)
                    targets.Add(target);
            }
        }
    }
}
