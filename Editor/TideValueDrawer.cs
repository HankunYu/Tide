using UnityEditor;
using UnityEngine;

namespace Tide.Editor
{
    /// <summary>A <see cref="TideValue"/> as one field of its type, with the type in a small popup beside it.</summary>
    [CustomPropertyDrawer(typeof(TideValue))]
    internal sealed class TideValueDrawer : PropertyDrawer
    {
        private const float TypeWidth = 72f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty type = property.FindPropertyRelative("type");
            SerializedProperty vector = property.FindPropertyRelative("vector");
            SerializedProperty gradient = property.FindPropertyRelative("gradient");

            EditorGUI.BeginProperty(position, label, property);
            Rect field = EditorGUI.PrefixLabel(position, label);
            var typeRect = new Rect(field.xMax - TypeWidth, field.y, TypeWidth, field.height);
            field.width -= TypeWidth + 4f;

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Vector4 v = vector.vector4Value;
            EditorGUI.BeginChangeCheck();
            switch ((TideValueType)type.enumValueIndex)
            {
                case TideValueType.Int:
                    v.x = EditorGUI.IntField(field, Mathf.RoundToInt(v.x));
                    break;
                case TideValueType.Bool:
                    v.x = EditorGUI.Toggle(field, v.x >= 0.5f) ? 1f : 0f;
                    break;
                case TideValueType.Color:
                    v = EditorGUI.ColorField(field, GUIContent.none, new Color(v.x, v.y, v.z, v.w), true, true, true);
                    break;
                case TideValueType.Vector2:
                    v = EditorGUI.Vector2Field(field, GUIContent.none, v);
                    break;
                case TideValueType.Vector3:
                    v = EditorGUI.Vector3Field(field, GUIContent.none, v);
                    break;
                case TideValueType.Vector4:
                    v = EditorGUI.Vector4Field(field, GUIContent.none, v);
                    break;
                case TideValueType.Gradient:
                    EditorGUI.PropertyField(field, gradient, GUIContent.none);
                    break;
                default:
                    v.x = EditorGUI.FloatField(field, v.x);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
                vector.vector4Value = v;

            EditorGUI.PropertyField(typeRect, type, GUIContent.none);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
