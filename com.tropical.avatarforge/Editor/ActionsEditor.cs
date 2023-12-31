using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class ActionsEditor : EditorBase
    {
        public System.Action OnOptions;

        BehaviourItem behaviour;
        ReorderablePropertyList actionList = new ReorderablePropertyList(null, foldout: false, addName:"Option");
        public override void Inspector_Body()
        {
            behaviour = (BehaviourItem)GetManagedReferenceValue(target);

            //Options
            OnOptions?.Invoke();

            //Timing
            var foldoutTiming = target.FindPropertyRelative("foldoutTiming");
            if(BeginCategory("Timing", foldoutTiming))
                DrawTiming();
            EndCategory();

            //Actions
            ActionEditorBase.InitEditors();
            actionList.list = target.FindPropertyRelative("actions");
            actionList.headerColor = AvatarForgeEditor.SubHeaderColor;
            actionList.showHeader = true;
            actionList.OnPreAdd = (list) =>
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
            actionList.OnElementHeader = (index, element) =>
            {
                var action = (Action)GetManagedReferenceValue(element);
                var editor = ActionEditorBase.FindEditor(action);
                if(editor == null)
                {
                    EditorGUILayout.LabelField($"Unable to find editor for action '{action.GetType().FullName}'");
                    return false;
                }
                else
                {
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(editor.displayName));
                }

                return element.isExpanded;
            };
            actionList.OnElementBody = (index, element) =>
            {
                var action = (Action)GetManagedReferenceValue(element);

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
            actionList.OnInspectorGUI();
        }

        void DrawTiming()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Fade");

                EditorGUIUtility.labelWidth = 40;
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                //GUILayout.Label("In", GUILayout.Width(48));
                EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeIn"), new GUIContent("In"));
                //GUILayout.Label("Hold", GUILayout.Width(48));
                EditorGUILayout.PropertyField(target.FindPropertyRelative("hold"), new GUIContent("Hold"));
                //GUILayout.Label("Out", GUILayout.Width(48));
                EditorGUILayout.PropertyField(target.FindPropertyRelative("fadeOut"), new GUIContent("Out"));

                EditorGUIUtility.labelWidth = 0;
                EditorGUI.indentLevel = indent;
            }
            EditorGUILayout.EndHorizontal();

            DrawParameterDropDown(target.FindPropertyRelative("timeParameter"), "Time Parameter", required:false);
        }
    }
}

