using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tide.Editor
{
    // Filtering the tracks by name: Ctrl+F focuses the field, Escape clears it
    public partial class TideEditorWindow
    {
        [SerializeField] private string trackFilter = "";

        private SearchField _filterField;
        private int _shownTracks;
        private int _totalTracks;

        private bool IsFiltering => !string.IsNullOrWhiteSpace(trackFilter);

        private void DrawFilterField()
        {
            _filterField ??= new SearchField();
            string value = _filterField.OnToolbarGUI(trackFilter, GUILayout.Width(180f));
            if (value == trackFilter)
                return;
            trackFilter = value;
            Repaint();
        }

        /// <summary>
        /// True when every word of the filter appears (ignoring case) in the
        /// track's name, its kind or the name of the object it drives.
        /// </summary>
        private bool MatchesFilter(TideTrack track, GameObject boundObject)
        {
            if (!IsFiltering)
                return true;
            string text = track.Name + " " + TideStyles.KindName(track.GetType()) + " " +
                          (boundObject != null ? boundObject.name : "");
            foreach (string word in trackFilter.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        // Ctrl/Cmd+F and Escape; true when the event was used
        private bool HandleFilterKeys(Event e)
        {
            if (e.type != EventType.KeyDown)
                return false;
            if (e.keyCode == KeyCode.F && (e.control || e.command))
            {
                _filterField?.SetFocus();
                e.Use();
                Repaint();
                return true;
            }
            if (e.keyCode == KeyCode.Escape && _filterField != null && _filterField.HasFocus())
            {
                trackFilter = "";
                GUI.FocusControl(null);
                e.Use();
                Repaint();
                return true;
            }
            return false;
        }

        private string FilterSummary => IsFiltering ? $"  {_shownTracks}/{_totalTracks}" : "";

        private void DrawNoMatch(Rect rowsView)
        {
            if (!IsFiltering || _shownTracks > 0)
                return;
            var rect = new Rect(rowsView.x + 12f, rowsView.y + 12f, Mathf.Min(rowsView.width - 24f, HeaderWidth + 300f), 20f);
            GUI.Label(rect, $"No tracks match \"{trackFilter.Trim()}\" (Escape clears the filter)", EditorStyles.miniLabel);
        }
    }
}
