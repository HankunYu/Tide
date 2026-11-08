using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tide
{
    /// <summary>
    /// Plays FMOD events in step with the timeline: the heaviest clip's event
    /// plays from its clip-local time, at its volume times its weight, and a
    /// 3D event follows the bound transform. The event is only placed when the
    /// timeline jumps (play, seek, pause, loop); during playback it runs on its
    /// own, so it may drift slightly but never stutters. Plays only in Play mode.
    /// </summary>
    [Serializable]
    [TideDisplay("FMOD", 0.92f, 0.52f, 0.30f)]
    public class TideFmodTrack : TideClipTrack
    {
        public override Type BindingType => typeof(Transform);

        public override Type ClipType => typeof(TideFmodClip);

        public override TideTrackInstance CreateInstance(TidePlayer player, Object binding)
        {
            return new Instance(this, (Transform)binding);
        }

        private sealed class Instance : TideTrackInstance
        {
            private const int NoClip = -1;

            private readonly TideFmodTrack _track;
            private readonly Transform _transform;
            private readonly List<TideClipSample> _samples = new();
            private EventInstance _event;
            private int _clipIndex = NoClip;
            // Event length in seconds, 0 when it has none (e.g. it loops)
            private double _length;
            private bool _paused;
            private float _volume;

            public Instance(TideFmodTrack track, Transform transform)
            {
                _track = track;
                _transform = transform;
            }

            public override void Evaluate(in TideEvaluation evaluation)
            {
                if (_transform == null || !Application.isPlaying)
                    return;
                _track.Gather(evaluation, _samples);
                int strongest = Strongest(_samples);
                TideClipSample sample = strongest >= 0 ? _samples[strongest] : default;
                var clip = strongest >= 0 ? _track.Clips[sample.ClipIndex] as TideFmodClip : null;
                if (clip == null || clip.Event.IsNull)
                {
                    StopEvent();
                    _clipIndex = NoClip;
                    return;
                }

                if (sample.ClipIndex != _clipIndex)
                {
                    StopEvent();
                    _clipIndex = sample.ClipIndex;
                    _length = EventLength(clip.Event);
                }
                // Past the end of the event: stay silent until a jump back
                if (_length > 0.0 && sample.LocalTime >= _length)
                {
                    StopEvent();
                    return;
                }

                bool started = !_event.isValid();
                if (started)
                {
                    _event = RuntimeManager.CreateInstance(clip.Event);
                    _event.set3DAttributes(_transform.To3DAttributes());
                    RuntimeManager.AttachInstanceToGameObject(_event, _transform);
                }

                float volume = clip.Volume * sample.Weight;
                if (started || volume != _volume)
                {
                    _event.setVolume(volume);
                    _volume = volume;
                }
                // Place the event only when the timeline jumped (or it just started)
                if (started || !evaluation.Continuous || evaluation.Wrapped)
                    _event.setTimelinePosition(ToMilliseconds(sample.LocalTime));
                // Seeks and pauses hold it; the next playback frame lets it run
                bool paused = !evaluation.Continuous;
                if (started || paused != _paused)
                {
                    _event.setPaused(paused);
                    _paused = paused;
                }
                if (started)
                    _event.start();
            }

            public override void Restore()
            {
                StopEvent();
                _clipIndex = NoClip;
            }

            private void StopEvent()
            {
                if (!_event.isValid())
                    return;
                // Skipped while quitting: FMOD drops dead attachments itself
                if (RuntimeManager.IsInitialized)
                    RuntimeManager.DetachInstanceFromGameObject(_event);
                _event.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _event.release();
                _event.clearHandle();
            }

            private static double EventLength(EventReference reference)
            {
                EventDescription description = RuntimeManager.GetEventDescription(reference);
                if (!description.isValid() || description.getLength(out int milliseconds) != FMOD.RESULT.OK)
                    return 0.0;
                return milliseconds / 1000.0;
            }

            private static int ToMilliseconds(double seconds)
            {
                return (int)Math.Round(Math.Max(0.0, seconds) * 1000.0);
            }
        }
    }
}
