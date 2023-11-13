using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public struct ReorderablePropertyList
    {
        public string title;
        public bool foldout;
        public bool enableSelection;
        public bool showHover;
        public bool showHeader;
        public Color headerColor;
        public int padding;
        public string addName;

        public SerializedProperty list;
        public System.Func<int, SerializedProperty, bool> OnElementHeader;
        public System.Action<int, SerializedProperty> OnElementBody;
        public System.Func<int, SerializedProperty, bool> OnDelete;
        public System.Action<int, int> OnMove;
        public System.Func<bool> AllowAdd;
        public System.Func<SerializedProperty, SerializedProperty> OnPreAdd;      //Before an item is added, call this
        public System.Action<SerializedProperty> OnAdd;         //Once an item is added, call this
        public System.Action<SerializedProperty> OnRemove;
        public bool disableReorder;

        static bool initialized = false;
        static GUIContent upArrow;
        static GUIContent downArrow;
        static GUIStyle elementNormal;
        static GUIStyle elementSelected;
        static GUIContent removeButton;

        GUIContent addButton;

        public ReorderablePropertyList(string title, bool foldout=true, string addName=null)
        {
            this.title = title;
            this.foldout = foldout;
            this.selectedElement = -1;
            this.enableSelection = false;
            this.showHover = false;
            this.showHeader = false;
            this.headerColor = Color.white;
            this.padding = 12;
            this.addName = addName;
            this.addButton = null;

            this.list = null;
            this.OnElementHeader = null;
            this.OnElementBody = null;
            this.OnDelete = null;
            this.AllowAdd = null;
            this.OnPreAdd = null;
            this.OnAdd = null;
            this.OnRemove = null;
            this.OnMove = null;
            this.disableReorder = false;
        }

        public void OnInspectorGUI()
        {
            InitStyles();

            //Title
            if(!string.IsNullOrEmpty(title))
            {
                if(foldout)
                {
                    list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, title);
                    if(!list.isExpanded)
                        return;
                }
                else
                    EditorGUILayout.LabelField(title);
                //EditorGUI.indentLevel += 1;
            }

            //GUILayout.Space(padding);
            GUILayout.BeginHorizontal();
            //GUILayout.Space(padding);
            GUILayout.BeginVertical();

            //Draw elements
            if(list.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Contains No Elements", MessageType.None);
            }
            else
            {
                //Draw elements
                for(int i = 0; i < list.arraySize; i++)
                {
                    var action = DrawElement(i, list.GetArrayElementAtIndex(i));
                    switch(action)
                    {
                        case ElementAction.Remove:
                        {
                            //Store isExpanded state so we can resore it after deleting an element
                            var isExpanded = new List<bool>(list.arraySize);
                            for(int j = i + 1; j < list.arraySize; j++)
                                isExpanded.Add(list.GetArrayElementAtIndex(j).isExpanded);

                            //Delete
                            list.DeleteArrayElementAtIndex(i);
                            list.serializedObject.ApplyModifiedProperties();

                            //Resore isExpanded state
                            for(int j = 0; j < isExpanded.Count; j++)
                                list.GetArrayElementAtIndex(i + j).isExpanded = isExpanded[j];

                            i--;
                            break;
                        }
                        case ElementAction.MoveUp:
                        {
                            if(i > 0)
                            {
                                //Store isExpanded state so we can resore it after swapping
                                var isExpandedA = list.GetArrayElementAtIndex(i).isExpanded;
                                var isExpandedB = list.GetArrayElementAtIndex(i - 1).isExpanded;

                                //Swap
                                list.MoveArrayElement(i, i - 1);
                                list.serializedObject.ApplyModifiedProperties();

                                //Resore isExpanded state
                                list.GetArrayElementAtIndex(i).isExpanded = isExpandedB;
                                list.GetArrayElementAtIndex(i - 1).isExpanded = isExpandedA;

                                //Callback
                                OnMove?.Invoke(i, i - 1);
                            }
                            break;
                        }
                        case ElementAction.MoveDown:
                        {
                            if(i < list.arraySize - 1)
                            {
                                //Store isExpanded state so we can resore it after swapping
                                var isExpandedA = list.GetArrayElementAtIndex(i).isExpanded;
                                var isExpandedB = list.GetArrayElementAtIndex(i + 1).isExpanded;

                                //Swap
                                list.MoveArrayElement(i, i + 1);
                                list.serializedObject.ApplyModifiedProperties();

                                //Resore isExpanded state
                                list.GetArrayElementAtIndex(i).isExpanded = isExpandedB;
                                list.GetArrayElementAtIndex(i + 1).isExpanded = isExpandedA;

                                //Callback
                                OnMove?.Invoke(i, i + 1);
                            }
                            break;
                        }
                    }
                }
            }

            //Footer
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(AllowAdd != null ? !AllowAdd.Invoke() : false);
            if(GUILayout.Button(addButton, GUILayout.Width(128)))
            {
                //Pre-Add
                SerializedProperty element = null;
                if(OnPreAdd != null)
                    element = OnPreAdd.Invoke(list);
                else
                {
                    var size = list.arraySize;
                    list.InsertArrayElementAtIndex(size);
                    element = list.GetArrayElementAtIndex(size);
                    selectedElement = list.arraySize - 1;
                }

                //Add
                if(OnAdd != null)
                    OnAdd.Invoke(element);
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            //GUILayout.Space(padding);
            GUILayout.EndVertical();
            //GUILayout.Space(padding);
            GUILayout.EndHorizontal();

            if(!string.IsNullOrEmpty(title))
            {
                //EditorGUI.indentLevel -= 1;
            }

            //Apply
            list.serializedObject.ApplyModifiedProperties();
        }
        enum ElementAction
        {
            None,
            Remove,
            MoveUp,
            MoveDown
        }
        ElementAction DrawElement(int index, SerializedProperty element)
        {
            //Item Container
            var itemRect = EditorGUILayout.BeginVertical();
            GUILayout.Space(2);
            itemRect = EditorGUI.IndentedRect(itemRect);

            //Selected
            bool isSelected = enableSelection && index == selectedElement;
            EditorGUI.DrawRect(itemRect, isSelected ? selectColor : elementColor);

            //Hover
            bool isMouseOver = itemRect.Contains(Event.current.mousePosition);
            if(isMouseOver && showHover)
            {
                //Hover Color
                var rect = new Rect(itemRect);
                rect.width = 4;
                EditorGUI.DrawRect(rect, hoverColor);
            }

            //Select
            if(Event.current.type == EventType.MouseDown && isMouseOver && enableSelection)
            {
                selectedElement = index;
                Event.current.Use();
            }

            //Header
            var headerRect = EditorGUILayout.BeginHorizontal();
            if(showHeader)
            {
                var rect = new Rect(headerRect);
                EditorGUI.DrawRect(rect, headerColor);
            }
            bool showBody = true;
            ElementAction action = ElementAction.None;
            {
                GUILayout.Space(4);

                if(OnElementHeader != null)
                    showBody = OnElementHeader.Invoke(index, element);
                else
                    EditorGUILayout.LabelField($"Element {index}");

                if(!disableReorder)
                {
                    //Up
                    if(GUILayout.Button(upArrow, GUILayout.Width(24)))
                    {
                        action = ElementAction.MoveUp;
                    }

                    //Down
                    if(GUILayout.Button(downArrow, GUILayout.Width(24)))
                    {
                        action = ElementAction.MoveDown;
                    }
                }

                //Delete
                if(GUILayout.Button(removeButton, GUILayout.Width(24)))
                {
                    action = ElementAction.Remove;
                    if(OnDelete != null)
                    {
                        if(!OnDelete.Invoke(index, element))
                            action = ElementAction.None;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            //Body
            if(OnElementBody != null && showBody)
            {
                EditorBase.BeginPaddedArea(padding);
                OnElementBody.Invoke(index, element);
                EditorBase.EndPaddedArea(padding);
            }

            GUILayout.Space(2);
            EditorGUILayout.EndVertical();

            return action;
        }

        static Color hoverColor = new Color(1f, 1f, 1f, 0.3f);
        static Color selectColor = new Color(0.25f, 0.5f, 1.0f, 0.5f);
        static Color elementColor = new Color(0f, 0f, 0f, 0.1f);

        void InitStyles()
        {
            if(addButton == null)
            {
                addButton = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus"));
                addButton.text = addName;
            }

            if(initialized)
                return;
            initialized = true;

            upArrow = new GUIContent(Resources.Load<Texture2D>("Icons/icon-arrow-up"));
            downArrow = new GUIContent(Resources.Load<Texture2D>("Icons/icon-arrow-down"));
            removeButton = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));

            

            elementNormal = new GUIStyle();
            elementNormal.normal.background = EditorBase.MakeTex(1, 1, elementColor);

            elementSelected = new GUIStyle();
            elementSelected.normal.background = EditorBase.MakeTex(1, 1, selectColor);
        }

        //Selection
        int selectedElement;
        public void SetSelection(int element)
        {
            selectedElement = element;
        }
        public void ClearSelection()
        {
            selectedElement = -1;
        }
        public SerializedProperty GetSelection()
        {
            if(selectedElement < 0 || selectedElement >= list.arraySize)
                return null;
            return list.GetArrayElementAtIndex(selectedElement);
        }
        public int GetSelectionIndex()
        {
            return selectedElement;
        }
    }
}