using System;
using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class BehavioursEditor : ActionsEditor
    {
        ReorderablePropertyList menuList = new ReorderablePropertyList("Behaviours", foldout:false);

        //Options
        public override void Inspector_Body()
        {
            //Main List
            menuList.list = target;
            menuList.enableSelection = true;
            menuList.showHeader = true;
            menuList.OnElementHeader = (index, element) =>
            {
                //Title
                var name = element.FindPropertyRelative("name").stringValue;
                EditorGUILayout.LabelField($"{name}");
                return true;
            };
            menuList.OnDelete = (index, element) =>
            {
                var name = element.FindPropertyRelative("name").stringValue;
                return EditorUtility.DisplayDialog("Delete Behaviour?", $"Delete the behaviour '{name}'?", "Delete", "Cancel");
            };
            menuList.OnPreAdd = (list) =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Generic"), false, OnAdd, typeof(CustomBehaviour));
                menu.AddItem(new GUIContent("Gestures"), false, OnAdd, typeof(Gestures.GestureItem));
                void OnAdd(object obj)
                {
                    var type = (System.Type)obj;
                    list.arraySize += 1;
                    var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    element.managedReferenceValue = System.Activator.CreateInstance(type);
                }
                menu.ShowAsContext();

                return null;
            };
            menuList.OnInspectorGUI();

            //Selected
            DrawSelected();
        }

        ActionsEditor actionEditor = new ActionsEditor();
        void DrawSelected()
        {
            EditorGUILayout.LabelField("Selected");
            EditorGUI.indentLevel += 1;

            var action = menuList.GetSelection();
            if(action == null)
            {
                EditorGUILayout.HelpBox("No item currently selected.", MessageType.None);
            }
            else
            {
                //Name
                EditorGUILayout.PropertyField(action.FindPropertyRelative("name"));

                //Default
                actionEditor.setup = setup;
                actionEditor.SetTarget(action);
                actionEditor.OnInspectorGUI();
            }

            EditorGUI.indentLevel -= 1;
        }
    }

    public class AddTexturePopup : PopupWindowContent
    {
        public Vector2 size;
        Vector2 scrollPosition;
        public Option[] options;
        public System.Action<string, object> onConfirm;

        public struct Option
        {
            public Option(string name, object value, Texture texture=null)
            {
                this.name = name;
                this.value = value;
                this.texture = texture;
            }

            public string name;
            public object value;
            public Texture texture;
        }

        public override void OnGUI(Rect rect)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical();

            for(int i = 0; i < options.Length; i++)
            {
                var option = options[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(option.texture, typeof(Texture2D), false, new GUILayoutOption[] { GUILayout.Height(64), GUILayout.Width(64) });
                EditorGUI.EndDisabledGroup();
                if(GUILayout.Button(option.name, new GUILayoutOption[] { GUILayout.Height(64), GUILayout.Width(size.x - 80) }))
                {
                    //Set value
                    onConfirm?.Invoke(option.name, option.value);

                    //Close
                    editorWindow.Close();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public void Show()
        {
            var rect = new Rect(Event.current.mousePosition, Vector2.zero);
            PopupWindow.Show(rect, this);
        }
    }
}