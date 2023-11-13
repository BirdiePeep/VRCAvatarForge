using System;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Generic;
using static Tropical.AvatarForge.ActionMenu;
using static Tropical.AvatarForge.Globals;
using UnityEditor.Animations;

namespace Tropical.AvatarForge
{
    public class ActionMenuEditor : FeatureEditor<ActionMenu>
    {
        ReorderablePropertyList menuList = new ReorderablePropertyList(null, foldout:false, addName:"Control");

        SerializedProperty selectedMenu;
        public ActionMenu rootMenu
        {
            get { return feature as ActionMenu; }
        }
        SerializedProperty parentMenu;
        string parentName;
        public override void SetTarget(SerializedProperty target)
        {
            base.SetTarget(target);
            selectedMenu = null;
            if(!string.IsNullOrEmpty(rootMenu.selectedMenuPath))
            {
                selectedMenu = target.FindPropertyRelative(rootMenu.selectedMenuPath);
                if(selectedMenu == null)
                {
                    Debug.Log("OurPath " + target.propertyPath);
                    Debug.Log("SelectedPath" + rootMenu.selectedMenuPath);
                }
            }
                
            SetSelectedMenu(selectedMenu != null ? selectedMenu : target);
        }
        public void SetSelectedMenu(SerializedProperty subMenu)
        {
            rootMenu.selectedMenuPath = subMenu.propertyPath == target.propertyPath ? "" : subMenu.propertyPath.Substring(target.propertyPath.Length + 1);
            selectedMenu = subMenu;

            //Parent Data
            var index = subMenu.propertyPath.LastIndexOf(".controls");
            if(index > -1)
            {
                //Parent Menu
                var path = subMenu.propertyPath.Substring(0, index);
                parentMenu = subMenu.serializedObject.FindProperty(path);

                //Parent Action
                path = subMenu.propertyPath.Substring(0, subMenu.propertyPath.LastIndexOf(".subMenu"));
                var action = subMenu.serializedObject.FindProperty(path);
                parentName = action.FindPropertyRelative("name").stringValue;
            }
            else
            {
                parentMenu = null;
                parentName = "Root";
            }
            menuList.ClearSelection();

            BuildControlGroups();
        }

        //Options
        public override void Inspector_Body()
        {
            //Back
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(parentMenu == null);
            if(GUILayout.Button("Back", GUILayout.Width(128)))
            {
                SetSelectedMenu(parentMenu);
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Label($"Menu: {parentName}");
            EditorGUILayout.EndHorizontal();

            //Main List
            menuList.list = selectedMenu.FindPropertyRelative("controls");
            menuList.headerColor = AvatarForgeEditor.SubHeaderColor;
            menuList.showHeader = true;
            menuList.OnElementHeader = (index, element) =>
            {
                var control = ActionsEditor.GetManagedReferenceValue(element) as ActionMenu.Control;
                GUILayout.Space(16);
                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(control.name));

                //Sub-Menu
                if(control is ActionMenu.SubMenu subMenu)
                {
                    if(GUILayout.Button("Sub Menu", GUILayout.Width(96)))
                        EditSubMenu(element);
                }
                else
                    GUILayout.Label(control.GetType().Name, AvatarForgeEditor.centerLabel, GUILayout.Width(96));

                return element.isExpanded;
            };
            menuList.OnElementBody = (index, element) =>
            {
                var control = ActionsEditor.GetManagedReferenceValue(element);

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 48;
                EditorGUILayout.PropertyField(element.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("icon"), GUIContent.none);
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                //Details
                var menuType = element.FindPropertyRelative("menuType");
                switch(control)
                {
                    case ActionMenu.Button button:
                        DrawButton(element);
                        break;
                    case ActionMenu.Toggle toggle:
                        DrawToggle(element, toggle);
                        break;
                    case ActionMenu.Slider slider:
                        DrawSlider(element);
                        break;
                    case ActionMenu.SubMenu subMenu:
                        DrawSubMenu(element);
                        break;
                }
            };
            menuList.AllowAdd = () =>
            {
                return menuList.list.arraySize < VRCExpressionsMenu.MAX_CONTROLS;
            };
            menuList.OnPreAdd = (list) =>
            {
                var popup = new AddListItemPopup();
                popup.list = list;
                popup.size = new Vector2(150, 200);
                popup.options = new AddListItemPopup.Option[]
                {
                    new AddListItemPopup.Option("Toggle", typeof(ActionMenu.Toggle)),
                    new AddListItemPopup.Option("Button", typeof(ActionMenu.Button)),
                    new AddListItemPopup.Option("Slider", typeof(ActionMenu.Slider)),
                    new AddListItemPopup.Option("Sub Menu", typeof(ActionMenu.SubMenu)),
                };
                popup.OnAdd = (element, obj) =>
                {
                    element.FindPropertyRelative("name").stringValue = $"New {obj.GetType().Name}";
                };
                popup.Show();

                return null;
            };
            EditorBase.BeginPaddedArea(8);
            menuList.OnInspectorGUI();
            EditorBase.EndPaddedArea(8);

            if(menuList.list.arraySize >= VRCExpressionsMenu.MAX_CONTROLS)
                EditorGUILayout.HelpBox($"Max controls reached", MessageType.Info);
            }

        ActionsEditor actionEditor = new ActionsEditor();
        void DrawToggle(SerializedProperty action, ActionMenu.Toggle toggle)
        {
            //Default
            actionEditor.setup = setup;
            actionEditor.SetTarget(action);
            actionEditor.OnInspectorGUI();

            if(BeginCategory("Additional Options", action.FindPropertyRelative("foldoutOptions")))
            {
                var defaultValue = action.FindPropertyRelative("defaultValue");
                var isOffState = action.FindPropertyRelative("isOffState");
                var group = action.FindPropertyRelative("group");
                EditorGUI.BeginChangeCheck();
                DrawControlGroup(group);
                if(EditorGUI.EndChangeCheck())
                {
                    defaultValue.boolValue = false;
                    isOffState.boolValue = false;
                }

                //Options
                if(string.IsNullOrEmpty(group.stringValue))
                {
                    EditorGUILayout.PropertyField(defaultValue);
                }
                else
                {
                    //DefaultValue
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(defaultValue);
                    if(EditorGUI.EndChangeCheck() && defaultValue.boolValue)
                    {
                        //Turn off other defaults
                        selectedMenu.serializedObject.ApplyModifiedProperties();
                        var toggles = FindControlGroup(group.stringValue);
                        foreach(var item in toggles)
                        {
                            if(item == toggle)
                                continue;
                            item.defaultValue = false;
                        }
                        selectedMenu.serializedObject.Update();
                    }

                    //IsOffState
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(isOffState);
                    if(EditorGUI.EndChangeCheck() && isOffState.boolValue)
                    {
                        //Turn off other defaults
                        selectedMenu.serializedObject.ApplyModifiedProperties();
                        var toggles = FindControlGroup(group.stringValue);
                        foreach(var item in toggles)
                        {
                            if(item == toggle)
                                continue;
                            item.isOffState = false;
                        }
                        selectedMenu.serializedObject.Update();
                    }
                }

                EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(group.stringValue));
                DrawParameterDropDown(action.FindPropertyRelative("parameter"), "Parameter", false);
                EditorGUI.EndDisabledGroup();
            }
            EndCategory();
        }
        void DrawButton(SerializedProperty action)
        {
            //Default
            actionEditor.setup = setup;
            actionEditor.SetTarget(action);
            actionEditor.OnInspectorGUI();

            if(BeginCategory("Additional Options", action.FindPropertyRelative("foldoutOptions")))
            {
                DrawParameterDropDown(action.FindPropertyRelative("parameter"), "Parameter", false);
            }
            EndCategory();
        }
        void DrawSlider(SerializedProperty action)
        {
            var name = action.FindPropertyRelative("name");
            DrawAnimationReference("Animation", action.FindPropertyRelative("clip"), name.stringValue + "_Slider");
            EditorGUILayout.PropertyField(action.FindPropertyRelative("defaultValue"));
            DrawParameterDropDown(action.FindPropertyRelative("parameter"), "Parameter", false);
        }
        void DrawSubMenu(SerializedProperty action)
        {
        }
        void EditSubMenu(SerializedProperty action)
        {
            var subMenu = action.FindPropertyRelative("subMenu");
            if(EditorBase.GetManagedReferenceValue(subMenu) == null)
            {
                subMenu.managedReferenceValue = new ActionMenu();
                action.serializedObject.ApplyModifiedProperties();
            }

            SetSelectedMenu(subMenu);
        }

        //Control Groups
        public class ControlGroup
        {
            public System.Type type;
            public List<ActionMenu.Control> controls = new List<ActionMenu.Control>();
        }

        HashSet<ValueTuple<string, System.Type>> controlGroups = new HashSet<ValueTuple<string, Type>>();
        string[] groupNames;
        void BuildControlGroups()
        {
            controlGroups.Clear();
            Build(rootMenu);
            void Build(ActionMenu menu)
            {
                if(menu == null)
                    return;
                foreach(var item in menu.controls)
                {
                    if(item is SubMenu subMenu)
                        Build(subMenu.subMenu);
                    else if(item is Toggle toggle)
                    {
                        if(!string.IsNullOrEmpty(toggle.group))
                            controlGroups.Add(new ValueTuple<string, System.Type>(toggle.group, typeof(Toggle)));
                    }
                }
            }

            //Build dropdown info
            int index = 0;
            groupNames = new string[controlGroups.Count];
            foreach(var group in controlGroups)
            {
                groupNames[index] = group.Item1;
                index++;
            }
        }
        IEnumerable<ActionMenu.Toggle> FindControlGroup(string groupName)
        {
            List<Toggle> toggles = new List<Toggle>();
            Build(rootMenu);
            void Build(ActionMenu menu)
            {
                if(menu == null)
                    return;
                foreach(var item in menu.controls)
                {
                    if(item is SubMenu subMenu)
                        Build(subMenu.subMenu);
                    else if(item is Toggle toggle)
                    {
                        if(toggle.group == groupName)
                            toggles.Add(toggle);
                    }
                }
            }
            return toggles;
        }

        void DrawControlGroup(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();

            //Input field
            EditorGUI.BeginChangeCheck();
            property.stringValue = EditorGUILayout.TextField(property.displayName, property.stringValue);
            if(EditorGUI.EndChangeCheck())
                BuildControlGroups();

            //Group dropdown
            EditorGUI.BeginChangeCheck();
            int index = ArrayUtility.IndexOf(groupNames, property.stringValue);
            index = EditorGUILayout.Popup(index, groupNames, GUILayout.MaxWidth(SecondDropdownSize));
            if(EditorGUI.EndChangeCheck())
                property.stringValue = groupNames[index];

            EditorGUILayout.EndHorizontal();
        }

        public override string helpURL => "";
        public override void PreBuild()
        {
            if(CombinedMenu == null)
                CombinedMenu = new ActionMenu();
            Combine(CombinedMenu, feature);
        }
        public override void Build()
        {
            if(feature != CombinedMenu)
            {
                if(CombinedMenu != null)
                {
                    var editor = new ActionMenuEditor();
                    editor.SetFeature(CombinedMenu);
                    editor.Build();
                    CombinedMenu = null;
                }
            }
            else
            {
                //Collect all menu actions
                var validControls = new List<ActionMenu.Control>();
                CollectValidMenuActions(feature, validControls);

                //Expression Parameters
                BuildExpressionParameters(validControls);
                if(AvatarBuilder.BuildFailed)
                    return;

                //Expressions Menu
                BuildExpressionsMenu(feature);

                //Build controls
                foreach(var action in validControls)
                {
                    switch(action)
                    {
                        case Toggle toggle:
                            BuildToggle(toggle);
                            break;
                        case Button button:
                            BuildButton(button);
                            break;
                        case Slider slider:
                            BuildSlider(slider);
                            break;
                    }
                }
            }
        }
        public override void PostBuild() { }

        public static ActionMenu CombinedMenu;
        public static void Combine(ActionMenu dest, ActionMenu source)
        {
            if(source == null)
                return;

            foreach(var sourceAction in source.controls)
            {
                var destAction = dest.FindMenuActionOfType(sourceAction.name, sourceAction.GetType(), false);
                if(destAction != null)
                {
                    //Recursive
                    if(sourceAction is ActionMenu.SubMenu sourceSubMenu)
                    {
                        var destSubMenu = destAction as ActionMenu.SubMenu;
                        if(destSubMenu.subMenu == null)
                            destSubMenu.subMenu = new ActionMenu();
                        Combine(destSubMenu.subMenu, sourceSubMenu.subMenu);
                    }
                    else
                        Debug.LogWarning($"Unable to add Action Menu Control \"{sourceAction.name}\" as it was already included by another source");
                }
                else
                {
                    //Copy the action
                    var newAction = (ActionMenu.Control)Activator.CreateInstance(sourceAction.GetType());
                    sourceAction.DeepCopyTo(newAction);
                    dest.controls.Add(newAction);
                }
            }
        }
        static void CollectValidMenuActions(ActionMenu actionMenu, List<ActionMenu.Control> output)
        {
            //Add our actions
            int selfAdded = 0;
            foreach(var action in actionMenu.controls)
            {
                //Enabled
                if(!action.ShouldBuild())
                    continue;

                //Check type
                if(action is SubMenu subMenu)
                {
                    //Sub-Menus
                    if(subMenu.subMenu != null)
                        CollectValidMenuActions(subMenu.subMenu, output);
                }
                else
                {
                    //Add
                    output.Add(action);
                }

                //Increment
                selfAdded += 1;
            }

            //Validate
            if(selfAdded > VRCExpressionsMenu.MAX_CONTROLS)
            {
                AvatarBuilder.BuildFailed = true;
                EditorUtility.DisplayDialog("Build Failed", $"Action Menu has too many actions defined. {selfAdded}/{VRCExpressionsMenu.MAX_CONTROLS}", "Okay");
            }
        }

        void BuildExpressionParameters(List<Control> controls)
        {
            var parameters = new List<VRCExpressionParameters.Parameter>();

            //Build control groups
            var controlGroups = new Dictionary<string, ControlGroup>();
            foreach(var control in controls)
            {
                if(control is Toggle toggle)
                {
                    if(!string.IsNullOrEmpty(toggle.group))
                    {
                        ControlGroup group;
                        if(!controlGroups.TryGetValue(toggle.group, out group))
                        {
                            group = new ControlGroup();
                            controlGroups.Add(toggle.group, group);
                        }
                        group.controls.Add(toggle);
                    }
                }
            }
            foreach(var iter in controlGroups)
            {
                var group = iter.Value;
                if(group.controls.Count > 1)
                {
                    var paramName = GenerateUniqueParameter(iter.Key, parameters);
                    Control defaultControl = null;
                    for(int i = 0; i < group.controls.Count; i++)
                    {
                        var control = group.controls[i];
                        if(control is Toggle toggle)
                        {
                            toggle.parameter = paramName;
                            toggle.controlValue = toggle.isOffState ? 0 : i + 1;
                            if(toggle.defaultValue == true)
                                defaultControl = toggle;
                        }
                    }

                    var parameter = new VRCExpressionParameters.Parameter();
                    parameter.name = paramName;
                    parameter.valueType = VRCExpressionParameters.ValueType.Int;
                    parameter.defaultValue = defaultControl != null ? defaultControl.controlValue : 0;
                    parameters.Add(parameter);
                }
                else
                {
                    //Remove group
                    var control = group.controls[0];
                    if(control is Toggle toggle)
                    {
                        toggle.group = null;
                    }
                }
            }

            //Find all unique menu parameters
            foreach(var action in controls)
            {
                //Generate parameter name
                if(string.IsNullOrEmpty(action.parameter))
                    action.parameter = GenerateUniqueParameter(action.name, parameters);

                //Define parameter
                var parameter = new VRCExpressionParameters.Parameter();
                parameter.name = action.parameter;
                switch(action)
                {
                    case Button button:
                        parameter.valueType = VRCExpressionParameters.ValueType.Bool;
                        button.controlValue = 1;
                        break;
                    case Toggle toggle:
                        if(!string.IsNullOrEmpty(toggle.group))
                            parameter = null;
                        else
                        {
                            parameter.valueType = VRCExpressionParameters.ValueType.Bool;
                            parameter.defaultValue = toggle.defaultValue ? 1f : 0f;
                            toggle.controlValue = 1;
                        }
                        break;
                    case Slider slider:
                        parameter.valueType = VRCExpressionParameters.ValueType.Float;
                        parameter.defaultValue = slider.defaultValue;
                        break;
                }
                if(parameter != null)
                    parameters.Add(parameter);
            }

            //Add
            foreach(var param in parameters)
                AvatarBuilder.DefineExpressionParameter(param);
        }
        static void BuildExpressionsMenu(ActionMenu rootMenu)
        {
            //Expressions
            CreateMenu(rootMenu, AvatarBuilder.AvatarDescriptor.expressionsMenu);
            void CreateMenu(ActionMenu ourMenu, VRCExpressionsMenu expressionsMenu)
            {
                //Record old controls
                var oldControls = expressionsMenu.controls.ToArray();
                if(!AvatarBuilder.AvatarSetup.mergeOriginalAnimators)
                    expressionsMenu.controls.Clear();

                //Check size
                if(expressionsMenu.controls.Count + ourMenu.controls.Count > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    AvatarBuilder.BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Failed", $"Expressions Menu has too many actions defined. {expressionsMenu.controls.Count + ourMenu.controls.Count}/{VRCExpressionsMenu.MAX_CONTROLS}", "Okay");
                    return;
                }

                //Create controls from actions
                foreach(var action in ourMenu.controls)
                {
                    if(!action.ShouldBuild())
                        continue;

                    if(action is Button)
                    {
                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.Button;
                        control.parameter = new VRCExpressionsMenu.Control.Parameter();
                        control.parameter.name = action.parameter;
                        control.value = action.controlValue;
                        expressionsMenu.controls.Add(control);
                    }
                    else if(action is Toggle)
                    {
                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                        control.parameter = new VRCExpressionsMenu.Control.Parameter();
                        control.parameter.name = action.parameter;
                        control.value = action.controlValue;
                        expressionsMenu.controls.Add(control);
                    }
                    else if(action is Slider)
                    {
                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                        control.subParameters = new VRCExpressionsMenu.Control.Parameter[1];
                        control.subParameters[0] = new VRCExpressionsMenu.Control.Parameter();
                        control.subParameters[0].name = action.parameter;
                        control.value = action.controlValue;
                        expressionsMenu.controls.Add(control);
                    }
                    else if(action is SubMenu subMenu)
                    {
                        //Recover old sub-menu
                        VRCExpressionsMenu expressionsSubMenu = null;
                        foreach(var controlIter in oldControls)
                        {
                            if(controlIter.name == action.name)
                            {
                                expressionsSubMenu = controlIter.subMenu;
                                break;
                            }
                        }

                        //Create sub-menu
                        bool wasCreated = false;
                        if(expressionsSubMenu == null)
                        {
                            expressionsSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                            expressionsSubMenu.name = "ExpressionsMenu_" + action.name;
                            wasCreated = true;
                        }

                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                        control.subMenu = expressionsSubMenu;
                        expressionsMenu.controls.Add(control);

                        //Populate sub-menu
                        CreateMenu(subMenu.subMenu, expressionsSubMenu);

                        //Save
                        if(wasCreated)
                            AvatarBuilder.SaveAsset(expressionsSubMenu, AvatarForge.GetSaveDirectory(), "Generated");
                    }
                }

                //Save prefab
                EditorUtility.SetDirty(expressionsMenu);
            }

            //Save all assets
            AssetDatabase.SaveAssets();
        }

        //Normal
        static void BuildNormalLayers(List<Control> controls, string layerName)
        {
            BuildNormalLayers(controls, layerName, AnimationLayer.Action);
            BuildNormalLayers(controls, layerName, AnimationLayer.FX);
        }
        static void BuildNormalLayers(List<Control> controls, string layerName, Globals.AnimationLayer layerType)
        {
            var controller = AvatarBuilder.GetController(layerType);

            //Find all controls for this layer
            var layerActions = new List<Control>();
            foreach(var control in controls)
            {
                if(!control.NeedsControlLayer())
                    continue;
                if(!AvatarBuilder.GetAnimation(control, layerType, true))
                    continue;
                layerActions.Add(control);
            }
            if(layerActions.Count == 0)
                return;

            //Check of off state
            Control offAction = null;
            foreach(var action in layerActions)
            {
                if(action.controlValue == 0)
                {
                    offAction = action;
                    break;
                }
            }

            //Build
            bool turnOffState = offAction != null;
            if(layerType == AnimationLayer.Action)
                AvatarBuilder.BuildActionLayer(controller, layerActions, layerName, null, turnOffState);
            else
                AvatarBuilder.BuildNormalLayer(controller, layerActions, layerName, layerType, null, turnOffState);
        }
        static void BuildToggle(Toggle toggle)
        {
            //Build layers
            List<Control> layerActions = new List<Control>();
            layerActions.Add(toggle);
            BuildNormalLayers(layerActions, toggle.name);
        }
        static void BuildButton(Button button)
        {
            List<Control> layerActions = new List<Control>();
            layerActions.Add(button);
            BuildNormalLayers(layerActions, button.name);
        }
        static void BuildSlider(Slider slider)
        {
            var layerType = AnimationLayer.FX;
            var controller = AvatarBuilder.GetController(layerType);

            //Prepare layer
            var layer = AvatarBuilder.AddControllerLayer(controller, slider.name);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = AvatarBuilder.StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = AvatarBuilder.StatePosition(-1, 1);
            layer.stateMachine.exitPosition = AvatarBuilder.StatePosition(-1, 2);

            //Blend state
            var state = layer.stateMachine.AddState("Blend", AvatarBuilder.StatePosition(0, 0));
            state.motion = slider.clip;
            state.timeParameter = slider.parameter;
            state.timeParameterActive = true;
        }
        static string GenerateUniqueParameter(string paramName, List<VRCExpressionParameters.Parameter> allParameters)
        {
            //Attempt to create unique parameter name
            for(int i = 0; i < 100; i++)
            {
                var name = $"{paramName} [{UnityEngine.Random.Range(0, 10)}{UnityEngine.Random.Range(0, 10)}{UnityEngine.Random.Range(0, 10)}{UnityEngine.Random.Range(0, 10)}]";

                //Check if unique
                bool found = false;
                foreach(var other in allParameters)
                {
                    if(other.name == name)
                    {
                        found = true;
                        break;
                    }
                }
                if(found)
                    continue;

                return name;
            }

            //Error
            Debug.LogError($"Unable to create unique parameter name from: {paramName}");
            return paramName;
        }

        public static void AddCondition(ActionMenu.Control control, AnimatorStateTransition transition, bool equals)
        {
            if(string.IsNullOrEmpty(control.parameter))
                return;

            //Is parameter bool?
            AnimatorConditionMode mode;
            var param = AvatarBuilder.FindExpressionParameter(control.parameter);
            if(param.valueType == VRCExpressionParameters.ValueType.Bool)
                mode = equals ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
            else if(param.valueType == VRCExpressionParameters.ValueType.Int)
                mode = equals ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;
            else
            {
                AvatarBuilder.BuildFailed = true;
                EditorUtility.DisplayDialog("Build Error", "Parameter value type is not as expected.", "Okay");
                return;
            }

            //Set
            transition.AddCondition(mode, control.controlValue, control.parameter);
        }
    }
}