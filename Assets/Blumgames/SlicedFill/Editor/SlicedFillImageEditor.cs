using UnityEditor;
using UnityEngine;

namespace Blumgames.UI.Editor
{
    /// <summary>Extends the standard Image inspector without replacing the Image component.</summary>
    [CustomEditor(typeof(UnityEngine.UI.Image)), CanEditMultipleObjects]
    public sealed class SlicedFillImageEditor : UnityEditor.UI.ImageEditor
    {
        static readonly GUIContent ToggleLabel = new GUIContent("Sliced Fill",
            "Preserve sprite borders while filling. Works with the existing Image and Fill Amount.");
        static readonly GUIContent PixelsLabel = new GUIContent("Pixels Per Unit Multiplier",
            "Higher values make the borders thinner. Uses the same setting as a standard Sliced Image.");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            foreach (UnityEngine.UI.Image image in targets)
                if (image.type != UnityEngine.UI.Image.Type.Filled) return;

            bool first = IsSlicedFillEnabled((UnityEngine.UI.Image)target);
            bool mixed = false;
            bool missingBorder = false;
            bool preserveAspect = false;
            foreach (UnityEngine.UI.Image image in targets)
            {
                mixed |= IsSlicedFillEnabled(image) != first;
                missingBorder |= !image.hasBorder;
                preserveAspect |= image.preserveAspect;
            }
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.showMixedValue = mixed;
                EditorGUI.BeginChangeCheck();
                bool next = EditorGUILayout.Toggle(ToggleLabel, first);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    int group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Toggle Sliced Fill");
                    foreach (UnityEngine.UI.Image image in targets) SetSlicedFillEnabled(image, next);
                    Undo.CollapseUndoOperations(group);
                    first = next;
                    mixed = false;
                }
                if (first && !mixed)
                {
                    serializedObject.Update();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillCenter"));
                    var pixels = serializedObject.FindProperty("m_PixelsPerUnitMultiplier");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(pixels, PixelsLabel);
                    if (EditorGUI.EndChangeCheck()) pixels.floatValue = Mathf.Max(.01f, pixels.floatValue);
                    EditorGUI.indentLevel--;
                    serializedObject.ApplyModifiedProperties();
                    if (missingBorder)
                        EditorGUILayout.HelpBox("Set a Border on the Source Image in Sprite Editor and click Apply. " +
                            "Images without borders keep their standard fill.", MessageType.Warning);
                    if (preserveAspect)
                        EditorGUILayout.HelpBox("Preserve Aspect is ignored while Sliced Fill is enabled, " +
                            "just like a standard Sliced Image.", MessageType.Info);
                    EditorGUILayout.LabelField("Created by blumgames", EditorStyles.miniLabel);
                }
            }
        }

        public static bool IsSlicedFillEnabled(UnityEngine.UI.Image image)
        {
            var effect = image.GetComponent<SlicedFill>();
            return effect != null && effect.enabled;
        }

        /// <summary>Editor-only setup with Undo and prefab support. Does not change Image Type or fill values.</summary>
        public static void SetSlicedFillEnabled(UnityEngine.UI.Image image, bool value)
        {
            var effect = image.GetComponent<SlicedFill>();
            if (effect == null)
            {
                if (!value) return;
                effect = Undo.AddComponent<SlicedFill>(image.gameObject);
            }
            Undo.RecordObject(effect, "Toggle Sliced Fill");
            effect.enabled = value;
            effect.hideFlags |= HideFlags.HideInInspector;
            PrefabUtility.RecordPrefabInstancePropertyModifications(effect);
            if (value)
                while (HasEarlierEffect(effect) && UnityEditorInternal.ComponentUtility.MoveComponentUp(effect)) { }
            image.SetVerticesDirty();
        }

        static bool HasEarlierEffect(SlicedFill effect)
        {
            foreach (Component component in effect.GetComponents<Component>())
            {
                if (component == effect) return false;
                if (component is UnityEngine.UI.IMeshModifier) return true;
            }
            return false;
        }
    }
}
