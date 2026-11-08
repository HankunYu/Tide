using System;
using FMODUnity;
using UnityEngine;

namespace Tide.Editor
{
    internal sealed class TideFmodClipEditor : TideClipEditor
    {
        public override Type ClipType => typeof(TideFmodClip);

        public override void OnToolsGUI(TideClipEditorContext context)
        {
            var clip = (TideFmodClip)context.Clip;
            double length = EventLength(clip.Event);
            if (length > 0.0 && GUILayout.Button("Fit To Event"))
                context.Edit("Fit Clip", () => clip.Duration = length);
        }

        public override void Initialise(TideClipEditorContext context)
        {
            if (context.Binding is not Transform transform ||
                !transform.TryGetComponent(out StudioEventEmitter emitter) || emitter.EventReference.IsNull)
                return;
            var clip = (TideFmodClip)context.Clip;
            clip.Event = emitter.EventReference;
            double length = EventLength(clip.Event);
            if (length > 0.0)
                clip.Duration = length;
        }

        // Length in seconds from the editor's event cache, 0 when unknown or unbounded
        private static double EventLength(EventReference reference)
        {
            if (reference.IsNull)
                return 0.0;
            EditorEventRef eventRef = EventManager.EventFromGUID(reference.Guid);
            return eventRef != null ? eventRef.Length / 1000.0 : 0.0;
        }
    }
}
