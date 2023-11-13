using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Tropical.AvatarForge
{
    public class AddParametersEditor : FeatureEditor<AddParameters>
    {
        GUIStyle label;
        ReorderablePropertyList parametersList = new ReorderablePropertyList(null, foldout: false, addName:"Parameter");
        public override void OnInspectorGUI()
        {
            if(label == null)
            {
                label = new GUIStyle(GUI.skin.label);
                label.alignment = TextAnchor.MiddleLeft;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name", label);
            GUILayout.Label("Type", label, GUILayout.Width(64));
            GUILayout.Label("Default", label, GUILayout.Width(64));
            GUILayout.Label("Saved", label, GUILayout.Width(50));
            GUILayout.Label("Synced", label, GUILayout.Width(50));
            GUILayout.Space(96);
            EditorGUILayout.EndHorizontal();

            parametersList.list = target.FindPropertyRelative("parameters");
            parametersList.OnElementHeader = (index, element) =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("name"), GUIContent.none);
                var type = element.FindPropertyRelative("valueType");
                EditorGUILayout.PropertyField(type, GUIContent.none, GUILayout.Width(64));

                var defaultValue = element.FindPropertyRelative("defaultValue");
                if(type.intValue == (int)VRCExpressionParameters.ValueType.Bool)
                    defaultValue.floatValue = EditorGUILayout.Toggle(defaultValue.floatValue > 0, GUILayout.Width(64)) ? 1f : 0f;
                else
                    EditorGUILayout.PropertyField(defaultValue, GUIContent.none, GUILayout.Width(64));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("saved"), GUIContent.none, GUILayout.Width(50));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("networkSynced"), GUIContent.none, GUILayout.Width(50));
                return true;
            };
            parametersList.OnInspectorGUI();
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            foreach(var parameter in feature.parameters)
            {
                AvatarBuilder.DefineExpressionParameter(parameter);
            }
        }
        public override void PostBuild() { }
    }
}