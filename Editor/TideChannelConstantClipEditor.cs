using System;
using UnityEngine;

namespace Tide.Editor
{
    internal sealed class TideChannelConstantClipEditor : TideClipEditor
    {
        public override Type ClipType => typeof(TideChannelConstantClip);

        public override void OnToolsGUI(TideClipEditorContext context)
        {
            var clip = (TideChannelConstantClip)context.Clip;
            if (!context.TryChannel(out _, out _))
                return;
            if (GUILayout.Button("Value ← Scene"))
            {
                context.ReadScene(() =>
                {
                    if (context.TryReadChannel(out TideValue value))
                        context.Edit("Capture Value", () => clip.Value = value);
                });
            }
        }

        public override void Initialise(TideClipEditorContext context)
        {
            if (context.TryReadChannel(out TideValue value))
                ((TideChannelConstantClip)context.Clip).Value = value;
        }
    }
}
