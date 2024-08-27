using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class ActionsEditor : EditorBase
    {
        public System.Action OnOptions;

        ActionItem behaviour;
        ReorderablePropertyList optionsList = new ReorderablePropertyList(null, foldout: false, addName:"Option");
        public override void Inspector_Body()
        {
            behaviour = (ActionItem)GetManagedReferenceValue(target);

            //Options
            OnOptions?.Invoke();

            //Timing
            var foldoutTiming = target.FindPropertyRelative("foldoutTiming");
            //if(BeginCategory("Timing", foldoutTiming))
            DrawTiming();
            //EndCategory();

            //Actions
            ActionEditorBase.InitEditors();
            optionsList.list = target.FindPropertyRelative("options");
            optionsList.headerColor = AvatarForgeEditor.SubHeaderColor;
            optionsList.showHeader = true;
            optionsList.OnPreAdd = (list) =>
            {
                var menu = new GenericMenu();
                for(int i = 0; i < ActionEditorBase.editorTypes.Count; i++)
                    menu.AddItem(new GUIContent(ActionEditorBase.editorNames[i]), false, OnAdd, ActionEditorBase.editorTypes[i]);
                void OnAdd(object obj)
                {
                    var type = (System.Type)obj;
                    list.arraySize += 1;
                    var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    element.isExpanded = true;
                    element.managedReferenceValue = System.Activator.CreateInstance(type);
                    list.serializedObject.ApplyModifiedProperties();
                }
                menu.ShowAsContext();

                return null;
            };
            optionsList.OnElementHeader = (index, element) =>
            {
                var action = (ActionOption)GetManagedReferenceValue(element);
                var editor = ActionEditorBase.FindEditor(action);
                if(editor == null)
                {
                    EditorGUILayout.LabelField($"Unable to find editor for action '{action?.GetType().FullName}'");
                    return false;
                }
                else
                {
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(editor.displayName));
                }

                return element.isExpanded;
            };
            optionsList.OnElementBody = (index, element) =>
            {
                var action = (ActionOption)GetManagedReferenceValue(element);

                ActionEditorBase editor = ActionEditorBase.FindEditor(action);
                if(editor != null)
                {
                    editor.editor = this.editor;
                    editor.setup = this.setup;
                    editor.behaviour = behaviour;
                    editor.SetTarget(element);
                    editor.OnInspectorGUI();
                }
            };
            optionsList.OnInspectorGUI();
        }

        void DrawTiming()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Timing");

                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                GUILayout.Label("Fade In");
                EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeIn"), GUIContent.none);
                GUILayout.Label("Hold");
                EditorGUILayout.PropertyField(target.FindPropertyRelative("hold"), GUIContent.none);
                GUILayout.Label("Fade Out");
                EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeOut"), GUIContent.none);

                EditorGUI.indentLevel = indent;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

