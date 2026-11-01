using System;
using UnityEditor;
using UnityEngine;

namespace Tide.Editor
{
    internal sealed class TideChannelRampClipEditor : TideClipEditor
    {
        public override Type ClipType => typeof(TideChannelRampClip);

        public override void OnToolsGUI(TideClipEditorContext context)
        {
            var clip = (TideChannelRampClip)context.Clip;
            if (!context.TryChannel(out _, out _))
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("From ← Scene"))
                {
                    context.ReadScene(() =>
                    {
                        if (context.TryReadChannel(out TideValue value))
                            context.Edit("Capture Value", () => clip.From = value);
                    });
                }
                if (GUILayout.Button("To ← Scene"))
                {
                    context.ReadScene(() =>
                    {
                        if (context.TryReadChannel(out TideValue value))
                            context.Edit("Capture Value", () => clip.To = value);
                    });
                }
            }
        }

        public override void Initialise(TideClipEditorContext context)
        {
            if (context.TryReadChannel(out TideValue value))
            {
                var clip = (TideChannelRampClip)context.Clip;
                clip.From = clip.To = value;
            }
        }
    }
}
