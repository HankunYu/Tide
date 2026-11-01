using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tide.Editor
{
    // Active-on-top ordering and the row motion that animates order changes
    public partial class TideEditorWindow
    {
        // How long a sudden change (a static clip's edge, a marker) keeps its track counted as active
        private const double ChangeHold = 1.0;
        private const float RowMotionTime = 0.2f;

        private sealed class RowGroup
        {
            // The GameObject the group's tracks drive (null = unbound)
            public GameObject Group;
            public List<TideTrack> Tracks;
            public bool Collapsed;
        }

        private sealed class RowMotion
        {
            public float From;
            public float To;
            public double Start;
        }

        private static readonly object UnboundKey = new();
        private static GUIContent _activeOnTopContent;

        [SerializeField] private bool activeOnTop;

        // Per active track, when its current activity began (later = higher)
        private readonly Dictionary<TideTrack, double> _activity = new();
        private readonly Dictionary<TideTrack, int> _displayIndex = new();
        private readonly Dictionary<object, RowMotion> _rowMotion = new();
        private readonly HashSet<object> _seenRows = new();

        private static GUIContent ActiveOnTopContent => _activeOnTopContent ??= EditorGUIUtility.TrIconContent(
            "CustomSorting", "Active tracks on top: groups and tracks that are changing at the playhead move up, " +
            "the most recently started first. Held values count only around their edges.");

        private bool IsActive(TideTrack track) => activeOnTop && _activity.ContainsKey(track);

        /// <summary>
        /// Puts the groups and tracks that are active at the playhead first, the
        /// latest started on top. While the user drags, the order they see is kept,
        /// so rows don't move under the mouse.
        /// </summary>
        private void SortByActivity(List<RowGroup> groups)
        {
            if (_drag != DragMode.None)
            {
                foreach (RowGroup group in groups)
                    group.Tracks = group.Tracks.OrderBy(DisplayIndex).ToList();
                groups.Sort((a, b) => GroupDisplayIndex(a).CompareTo(GroupDisplayIndex(b)));
                return;
            }

            _activity.Clear();
            foreach (RowGroup group in groups)
            {
                foreach (TideTrack track in group.Tracks)
                {
                    if (ActiveSince(track, time, out double since))
                        _activity[track] = since;
                }
            }

            // OrderBy is stable, so idle tracks and groups keep their own order
            foreach (RowGroup group in groups)
                group.Tracks = group.Tracks.OrderByDescending(ActivityKey).ToList();
            List<RowGroup> ordered = groups.OrderByDescending(g => g.Tracks.Count > 0 ? ActivityKey(g.Tracks[0]) : double.NegativeInfinity).ToList();
            groups.Clear();
            groups.AddRange(ordered);

            _displayIndex.Clear();
            foreach (RowGroup group in groups)
            {
                foreach (TideTrack track in group.Tracks)
                    _displayIndex[track] = _displayIndex.Count;
            }
        }

        private double ActivityKey(TideTrack track) =>
            _activity.TryGetValue(track, out double since) ? since : double.NegativeInfinity;

        private int DisplayIndex(TideTrack track) =>
            _displayIndex.TryGetValue(track, out int index) ? index : int.MaxValue;

        private int GroupDisplayIndex(RowGroup group) =>
            group.Tracks.Count > 0 ? group.Tracks.Min(DisplayIndex) : int.MaxValue;

        /// <summary>
        /// Whether <paramref name="track"/> is changing at time <paramref name="t"/>,
        /// and since when: inside a clip whose output varies, around the edges and
        /// over the eases of a clip that holds one state, or just after a marker.
        /// </summary>
        private static bool ActiveSince(TideTrack track, double t, out double since)
        {
            since = double.NegativeInfinity;
            if (track == null || track.Muted)
                return false;

            if (track is TideClipTrack clipTrack)
            {
                foreach (TideClip clip in clipTrack.Clips)
                {
                    if (clip == null || t < clip.Start)
                        continue;
                    if (!clip.IsStatic)
                    {
                        if (t < clip.End)
                            since = System.Math.Max(since, clip.Start);
                        continue;
                    }
                    if (t < clip.Start + System.Math.Max(clip.EaseIn, ChangeHold))
                        since = System.Math.Max(since, clip.Start);
                    double fadeOut = clip.End - clip.EaseOut;
                    if (t >= fadeOut && t < clip.End + ChangeHold)
                        since = System.Math.Max(since, fadeOut);
                }
            }
            else if (track is TideMarkerTrack markerTrack)
            {
                foreach (TideMarker marker in markerTrack.Markers)
                {
                    if (marker != null && t >= marker.Time && t < marker.Time + ChangeHold)
                        since = System.Math.Max(since, marker.Time);
                }
            }
            return !double.IsNegativeInfinity(since);
        }

        /// <summary>
        /// Moves each row from where it was drawn towards its new place instead
        /// of jumping there, and keeps repainting until all have arrived.
        /// </summary>
        private void AnimateRows()
        {
            double now = EditorApplication.timeSinceStartup;
            bool moving = false;
            _seenRows.Clear();
            foreach (Row row in _rows)
            {
                object key = row.IsGroup ? row.Group != null ? row.Group : UnboundKey : row.Track;
                _seenRows.Add(key);
                if (!_rowMotion.TryGetValue(key, out RowMotion motion))
                {
                    // New rows appear in place
                    _rowMotion.Add(key, new RowMotion { From = row.Y, To = row.Y });
                    continue;
                }
                if (!Mathf.Approximately(motion.To, row.Y))
                {
                    motion.From = MotionY(motion, now);
                    motion.To = row.Y;
                    motion.Start = now;
                }
                row.Y = MotionY(motion, now);
                moving |= !Mathf.Approximately(row.Y, motion.To);
            }

            if (_rowMotion.Count > _seenRows.Count)
            {
                foreach (object key in _rowMotion.Keys.Where(k => !_seenRows.Contains(k)).ToList())
                    _rowMotion.Remove(key);
            }
            if (moving)
                Repaint();
        }

        private static float MotionY(RowMotion motion, double now)
        {
            float t = Mathf.Clamp01((float)((now - motion.Start) / RowMotionTime));
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            return Mathf.LerpUnclamped(motion.From, motion.To, eased);
        }

        private void DrawActiveOnTopToggle(Rect rect)
        {
            bool value = GUI.Toggle(rect, activeOnTop, ActiveOnTopContent, EditorStyles.toolbarButton);
            if (value == activeOnTop)
                return;
            activeOnTop = value;
            if (!activeOnTop)
                _activity.Clear();
            Repaint();
        }
    }
}
