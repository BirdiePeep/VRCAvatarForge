using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Tropical.AvatarForge
{
    public interface ISubActionsProvider
    {
        void GetSubActions(List<AvatarForge> actions);
    }

    [CustomEditor(typeof(AvatarForge))]
    public class AvatarForgeEditor : Editor
    {
        AvatarForge setup;

        //Styles
        public static Color HeaderColor = new Color(46f / 255f, 101f / 255f, 112f / 255f);
        public static Color SubHeaderColor = new Color(46f / 255f, 101f / 255f, 112f / 255f, 0.5f);
        static int HEADER_HEIGHT = 24;
        public static GUIStyle styleHeader;
        public static GUIStyle centerLabel;
        public static GUIContent helpButton;
        static bool initStyles = false;
        void InitStyles()
        {
            if(initStyles)
                return;
            initStyles = true;

            //Header
            styleHeader = new GUIStyle(GUI.skin.label);
            styleHeader.fontSize = HEADER_HEIGHT;
            styleHeader.fixedHeight = HEADER_HEIGHT;
            styleHeader.stretchHeight = false;

            //Center Label
            centerLabel = GUI.skin.GetStyle("Label");
            centerLabel.alignment = TextAnchor.UpperCenter;

            helpButton = EditorGUIUtility.IconContent("_Help");
        }

        public void Awake()
        {
            setup = target as AvatarForge;
        }
        public override void OnInspectorGUI()
        {
            setup = target as AvatarForge;
            var avatar = setup.avatar;

            InitStyles();

            if(Application.isPlaying)
            {
                DrawRuntimeUI();
                return;
            }

            var rootComponent = PrefabUtility.GetCorrespondingObjectFromOriginalSource(setup);
            if(rootComponent != null)
            {
                EditorGUILayout.HelpBox("You are viewing a prefab varient for this component, please modify the original component", MessageType.Warning);
                if(GUILayout.Button("Open Original Prefab", GUILayout.Height(32)))
                {
                    var path = AssetDatabase.GetAssetPath(rootComponent);
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)));
                }
            }
            EditorGUI.BeginDisabledGroup(rootComponent != null);
            EditorGUI.BeginChangeCheck();
            {
                //Features
                DrawFeatures();

                //Build Options & Data
                if(avatar != null)
                {
                    EditorBase.Divider();

                    if(BeginCategory("Build Options", ref setup.foldoutBuildOptions))
                    {
                        DrawBuildOptions();
                    }
                    EndCategory();

                    //Build
                    /*EditorGUI.BeginDisabledGroup(EasyAvatarSetup.ReturnSavePath() == null || script.avatar == null);
                    if(GUILayout.Button("Build Avatar Copy", GUILayout.Height(32)))
                    {
                        AvatarBuilder.BuildAvatarCopy(script.avatar, script, " (Generated)");
                    }
                    EditorGUI.EndDisabledGroup();*/
                }
            }
            if(EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
        }

        bool BeginCategory(string title, ref bool foldout)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foldout = EditorGUILayout.Foldout(foldout, title);
            EditorGUI.indentLevel += 1;
            return foldout;
        }
        void EndCategory()
        {
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        //Features
        ReorderablePropertyList featureList = new ReorderablePropertyList(null, foldout: false, addName:"Feature", largeAddButton: true);
        void DrawFeatures()
        {
            FeatureEditorBase.InitEditors();

            var featuresProp = serializedObject.FindProperty("features");

            featureList.list = featuresProp;
            featureList.showHeader = true;
            featureList.headerColor = HeaderColor;
            featureList.OnElementHeader = (index, element) =>
            {
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);
                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor == null)
                {
                    return false;
                }
                else
                {
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(editor.displayName));
                }

                //Help
                /*if(GUILayout.Button(helpButton, GUILayout.Width(32)))
                {
                    EditorUtility.DisplayDialog("Help", "Help URLs not yet implemented", "Ok");
                    //Application.OpenURL(feature.helpURL);
                }*/

                return element.isExpanded;
            };
            featureList.OnElementBody = (index, element) =>
            {
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);

                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor != null)
                {
                    editor.editor = this;
                    editor.setup = setup;
                    editor.SetTarget(element);
                    editor.OnInspectorGUI();
                }
            };
            featureList.OnDelete = (index, element) =>
            {
                //Check if we want to delete objects
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);
                if(feature != null)
                {
                    if(!EditorUtility.DisplayDialog("Remove Feature?", $"Are you sure you want to remove the '{feature.GetType().Name}' feature?", "Yes", "No"))
                        return false;
                }
                return true;
            };
            featureList.OnPreAdd = (list) =>
            {
                var menu = new GenericMenu();
                for(int i = 0; i < FeatureEditorBase.editorTypes.Count; i++)
                    menu.AddItem(new GUIContent(FeatureEditorBase.editorNames[i]), false, OnAdd, FeatureEditorBase.editorTypes[i]);
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
            featureList.OnInspectorGUI();
        }
        void DrawBuildOptions()
        {
            //Options
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mergeAnimators"));
        }

        //Runtime
        Stack<VRCExpressionsMenu> menuStack = new Stack<VRCExpressionsMenu>();
        Stack<string> menuNameStack = new Stack<string>();
        VRCExpressionsMenu currentMenu;
        string currentMenuName;
        Dictionary<int, AnimatorControllerParameter> paramsLookup;

        bool rawParamsFoldout = false;

        void DrawRuntimeUI()
        {
            Styles.Init();

            if(currentMenu == null)
            {
                menuStack.Clear();
                menuNameStack.Clear();
                currentMenu = setup.avatar?.expressionsMenu;
                currentMenuName = null;
            }
            if(currentMenu == null)
            {
                EditorGUILayout.HelpBox("No VRCExpressionsMenu found", MessageType.Error);
                return;
            }

            //Menu
            if(menuStack.Count == 0)
                GUILayout.Label("Menu: Root", GUILayout.ExpandWidth(true));
            else
                GUILayout.Label($"Menu: {currentMenuName}", GUILayout.ExpandWidth(true));

            //Back
            EditorGUI.BeginDisabledGroup(menuStack.Count == 0);
            {
                if(GUILayout.Button(Styles.contentBackButton, GUILayout.Height(32)))
                {
                    currentMenu = menuStack.Pop();
                    currentMenuName = menuNameStack.Pop();
                    Repaint();
                }
            }
            EditorGUI.EndDisabledGroup();

            var animator = setup.GetComponent<Animator>();
            if(paramsLookup == null)
            {
                paramsLookup = new Dictionary<int, AnimatorControllerParameter>();
                var parameters = animator.parameters;
                foreach(var item in parameters)
                {
                    paramsLookup.Add(item.nameHash, item);
                }
            }

            foreach(var control in currentMenu.controls)
            {
                if(control == null)
                    continue;

                EditorGUILayout.BeginHorizontal();

                //Icon
                var iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), control.icon != null ? control.icon : Styles.iconActionBasic);

                //Name
                GUILayout.Label(control.name, new GUILayoutOption[] { GUILayout.Width(128), GUILayout.Height(32) } );
                
                //Control
                switch(control.type)
                {
                    case VRCExpressionsMenu.Control.ControlType.SubMenu:
                    {
                        if(GUILayout.Button(control.name, GUILayout.Height(32)))
                        {
                            if(control.subMenu != null)
                            {
                                menuStack.Push(currentMenu);
                                menuNameStack.Push(currentMenuName);
                                currentMenu = control.subMenu;
                                currentMenuName = control.name;
                                Repaint();
                            }
                        }
                        break;
                    }
                    case VRCExpressionsMenu.Control.ControlType.Toggle:
                    {
                        var value = FindControlValue(control.parameter.hash);
                        bool isEnabled = value == control.value;
                        if(GUILayout.Button(isEnabled ? "Enabled" : "Disabled", GUILayout.Height(32)))
                        {
                            SetControlValue(control.parameter.hash, isEnabled ? 0 : (int)control.value);
                        }
                        break;
                    }
                    case VRCExpressionsMenu.Control.ControlType.Button:
                    {
                        /*if(GUILayout.Button(isEnabled ? "Disable" : "Enable"))
                        {
                            animator.SetInteger(control.parameter.hash, isEnabled ? 0 : (int)control.value);
                        }*/
                        break;
                    }
                    case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                    {
                        var nameHash = control.subParameters[0].hash;
                        var value = FindControlValue(nameHash);
                        EditorGUI.BeginChangeCheck();
                        value = EditorGUILayout.Slider(value, 0f, 1f);
                        if(EditorGUI.EndChangeCheck())
                        {
                            SetControlValue(nameHash, value);
                        }
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            //Draw misc avatar parameters
            rawParamsFoldout = EditorGUILayout.Foldout(rawParamsFoldout, "Raw Parameters");
            if(rawParamsFoldout)
            {
                foreach(var param in animator.parameters)
                {
                    switch(param.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                        {
                            EditorGUI.BeginChangeCheck();
                            bool value = FindControlValue(param.nameHash) != 0;
                            value = EditorGUILayout.Toggle(param.name, value);
                            if(EditorGUI.EndChangeCheck())
                            {
                                SetControlValue(param.nameHash, value ? 1f : 0f);
                            }
                            break;
                        }
                        case AnimatorControllerParameterType.Int:
                        {
                            EditorGUI.BeginChangeCheck();
                            int value = (int)FindControlValue(param.nameHash);
                            value = EditorGUILayout.IntField(param.name, value);
                            if(EditorGUI.EndChangeCheck())
                            {
                                SetControlValue(param.nameHash, value);
                            }
                            break;
                        }
                        case AnimatorControllerParameterType.Float:
                        {
                            EditorGUI.BeginChangeCheck();
                            float value = FindControlValue(param.nameHash);
                            value = EditorGUILayout.FloatField(param.name, value);
                            if(EditorGUI.EndChangeCheck())
                            {
                                SetControlValue(param.nameHash, value);
                            }
                            break;
                        }
                    }
                }
            }

            float FindControlValue(int nameHash)
            {
                AnimatorControllerParameter parameter;
                if(paramsLookup.TryGetValue(nameHash, out parameter))
                {
                    switch(parameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            return animator.GetBool(nameHash) ? 1f : 0f;
                        case AnimatorControllerParameterType.Float:
                            return animator.GetFloat(nameHash);
                        case AnimatorControllerParameterType.Int:
                            return animator.GetInteger(nameHash);
                    }
                }
                return 0;
            }
            void SetControlValue(int nameHash, float value)
            {
                AnimatorControllerParameter parameter;
                if(paramsLookup.TryGetValue(nameHash, out parameter))
                {
                    switch(parameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            animator.SetBool(nameHash, value != 0f);
                            break;
                        case AnimatorControllerParameterType.Float:
                            animator.SetFloat(nameHash, value);
                            break;
                        case AnimatorControllerParameterType.Int:
                            animator.SetInteger(nameHash, (int)value);
                            break;
                    }
                }
            }
            void DrawParamBool(int hash, string label)
            {
                EditorGUI.BeginChangeCheck();
                bool value = FindControlValue(hash) != 0;
                value = EditorGUILayout.Toggle(label, value);
                if(EditorGUI.EndChangeCheck())
                {
                    SetControlValue(hash, value ? 1f : 0f);
                }
            }
        }
    }
}