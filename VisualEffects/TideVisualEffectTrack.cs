using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Tide
{
    /// <summary>
    /// Plays a visual effect during its clips: entering a clip sends Play,
    /// leaving it sends Stop, so spawning ends and live particles finish
    /// their lives. Outside every clip the effect stays stopped. Seeking into
    /// a clip restarts the effect and simulates it up to the time reached
    /// there, so a scrub shows the particles playback would (as far as the
    /// effect's randomness allows); in edit mode the track steps the
    /// simulation itself.
    /// </summary>
    [Serializable]
    [TideDisplay("Visual Effect", 0.35f, 0.70f, 0.95f)]
    public class TideVisualEffectTrack : TideClipTrack
    {
        // Prewarming a seek simulates at most this many steps
        private const int MaxPrewarmSteps = 120;
        private const float PrewarmStep = 1f / 60f;
        // An edit-mode step longer than this counts as a jump
        private const double MaxEditStep = 0.25;

        public override Type BindingType => typeof(VisualEffect);

        public override Type ClipType => typeof(TideVisualEffectClip);

        public override TideTrackInstance CreateInstance(TidePlayer player, Object binding)
        {
            return new Instance(this, (VisualEffect)binding);
        }

        private sealed class Instance : TideTrackInstance
        {
            private const int Unknown = -2;

            private readonly TideVisualEffectTrack _track;
            private readonly VisualEffect _effect;
            private readonly bool _ownPause;
            private readonly List<TideClipSample> _samples = new();
            private int _activeClip = Unknown;

            public Instance(TideVisualEffectTrack track, VisualEffect effect)
            {
                _track = track;
                _effect = effect;
                _ownPause = effect.pause;
            }

            public override void Evaluate(in TideEvaluation evaluation)
            {
                if (_effect == null)
                    return;
                bool editMode = !Application.isPlaying;
                // In edit mode the track steps the simulation itself
                if (editMode)
                    _effect.pause = true;

                _track.Gather(evaluation, _samples);
                int strongest = Strongest(_samples);
                int clip = strongest >= 0 ? _samples[strongest].ClipIndex : -1;
                double local = strongest >= 0 ? _samples[strongest].LocalTime : 0.0;
                double step = evaluation.Time - evaluation.PreviousTime;
                // Edit-mode scrubs and window playback arrive as seeks: small
                // forward steps still count as playing on
                bool continuous = _activeClip != Unknown &&
                                  (evaluation.Continuous || (editMode && step > 0.0 && step <= MaxEditStep));

                if (clip >= 0)
                {
                    if (!continuous)
                    {
                        _effect.Reinit();
                        _effect.Play();
                        Simulate(local);
                    }
                    else
                    {
                        if (clip != _activeClip)
                            _effect.Play();
                        if (editMode)
                            Simulate(step);
                    }
                }
                else if (!continuous)
                {
                    _effect.Reinit();
                    _effect.Stop();
                }
                else
                {
                    if (_activeClip >= 0)
                        _effect.Stop();
                    if (editMode)
                        Simulate(step);
                }
                _activeClip = clip;
            }

            public override void Restore()
            {
                if (_effect == null)
                    return;
                _effect.pause = _ownPause;
                if (!Application.isPlaying)
                    _effect.Reinit();
            }

            private void Simulate(double seconds)
            {
                if (seconds <= 0.0)
                    return;
                float step = Mathf.Max(PrewarmStep, (float)(seconds / MaxPrewarmSteps));
                uint count = (uint)Math.Max(1, Math.Round(seconds / step));
                _effect.Simulate(step, count);
            }
        }
    }
}
