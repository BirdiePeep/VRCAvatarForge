using System;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Generic;
using static Tropical.AvatarForge.ActionMenu;
using static Tropical.AvatarForge.Globals;
using UnityEditor.Animations;
using System.Linq;
using Mono.Cecil;
using static VRC.SDKBase.VRCPlayerApi;

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
            Styles.Init();

            //Menu
            GUILayout.Label($"Menu: {parentName}");

            //Back
            EditorGUI.BeginDisabledGroup(parentMenu == null);
            if(GUILayout.Button(Styles.contentBackButton, GUILayout.Height(32)))
                SetSelectedMenu(parentMenu);
            EditorGUI.EndDisabledGroup();

            //Menu Path
            EditorGUI.BeginDisabledGroup(parentMenu != null);
            EditorGUILayout.PropertyField(target.FindPropertyRelative("menuPath"));
            EditorGUI.EndDisabledGroup();

            //Main List
            menuList.list = selectedMenu.FindPropertyRelative("controls");
            menuList.headerColor = AvatarForgeEditor.SubHeaderColor;
            menuList.showHeader = true;
            menuList.OnElementHeader = (index, element) =>
            {
                var control = ActionsEditor.GetManagedReferenceValue(element) as ActionMenu.Control;
                //GUILayout.Space(16);
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
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Toggle"), false, OnAdd, typeof(ActionMenu.Toggle));
                menu.AddItem(new GUIContent("Button"), false, OnAdd, typeof(ActionMenu.Button));
                menu.AddItem(new GUIContent("Slider"), false, OnAdd, typeof(ActionMenu.Slider));
                menu.AddItem(new GUIContent("Sub Menu"), false, OnAdd, typeof(ActionMenu.SubMenu));
                void OnAdd(object obj)
                {
                    var type = (Type)obj;
                    list.arraySize += 1;
                    var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    element.isExpanded = true;
                    element.managedReferenceValue = System.Activator.CreateInstance(type);
                    element.FindPropertyRelative("name").stringValue = $"New {type.Name}";
                    list.serializedObject.ApplyModifiedProperties();
                }
                menu.ShowAsContext();

                return null;
            };
            EditorBase.BeginPaddedArea(8);
            menuList.OnInspectorGUI();
            EditorBase.EndPaddedArea(8);

            if(menuList.list.arraySize >= VRCExpressionsMenu.MAX_CONTROLS)
                EditorGUILayout.HelpBox($"Max controls reached", MessageType.Info);
            }

        ActionsEditor actionEditor = new ActionsEditor();
        void DrawToggle(SerializedProperty property, ActionMenu.Toggle toggle)
        {
            //Default
            actionEditor.setup = setup;
            actionEditor.SetTarget(property);
            actionEditor.OnOptions = () =>
            {
                DrawControlGroup(property);

                //Non-Group Options
                var group = property.FindPropertyRelative("group");
                if(string.IsNullOrEmpty(group.stringValue))
                {
                    var defaultValue = property.FindPropertyRelative("defaultValue");
                    EditorGUILayout.PropertyField(defaultValue, new GUIContent("Default On"));
                    DrawParameterDropDown(property.FindPropertyRelative("parameter"), "Parameter", false);
                }
            };
            actionEditor.OnInspectorGUI();
        }
        void DrawButton(SerializedProperty action)
		{
			//Default
			actionEditor.setup = setup;
			actionEditor.SetTarget(action);
			actionEditor.OnOptions = () =>
			{
				DrawParameterDropDown(action.FindPropertyRelative("parameter"), "Parameter", false);
			};
			actionEditor.OnInspectorGUI();
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
            public List<IGroupedControl> controls = new List<IGroupedControl>();
            public IGroupedControl defaultControl;
            public IGroupedControl offState;

            public void SetDefault(IGroupedControl value)
            {
                defaultControl = value;
                foreach(var control in controls)
                    control.IsGroupDefault = control == value;
            }
            public void SetOffState(IGroupedControl value)
            {
                offState = value;
                foreach(var control in controls)
                    control.IsGroupOffState = control == value;
            }
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
                        if(toggle.HasGroup)
                            controlGroups.Add(new ValueTuple<string, System.Type>(toggle.group, typeof(Toggle)));
                    }
                }
            }

            //Build dropdown info
            int index = 1;
            groupNames = new string[controlGroups.Count + 1];
            groupNames[0] = "[None]";
            foreach(var group in controlGroups)
            {
                groupNames[index] = group.Item1;
                index++;
            }
        }
        ControlGroup FindControlGroup(string groupName)
        {
            //Find all controls
            IGroupedControl offState = null;
            IGroupedControl defaultControl = null;
            var controls = new List<IGroupedControl>();
            Build(rootMenu);
            void Build(ActionMenu menu)
            {
                if(menu == null)
                    return;
                foreach(var item in menu.controls)
                {
                    if(item is SubMenu subMenu)
                        Build(subMenu.subMenu);
                    else if(item is IGroupedControl control)
                    {
                        if(control.Group == groupName)
                        {
                            if(defaultControl == null && control.IsGroupDefault)
                                defaultControl = control;
                            if(offState == null && control.IsGroupOffState)
                                offState = control;
                            controls.Add(control);
                        }
                    }
                }
            }
            if(controls.Count == 0)
                return null;

            var group = new ControlGroup();
            group.controls = controls;
            group.defaultControl = defaultControl;
            group.offState = offState;
            return group;
        }

        void DrawControlGroup(SerializedProperty property)
        {
            var groupName = property.FindPropertyRelative("group");
            var controlGroup = FindControlGroup(groupName.stringValue);
            var control = (Control)property.managedReferenceValue;

            EditorGUILayout.BeginHorizontal();

            //Input field
            EditorGUI.BeginChangeCheck();
            groupName.stringValue = EditorGUILayout.TextField("Group", groupName.stringValue);
            if(EditorGUI.EndChangeCheck())
                BuildControlGroups();

            //Group dropdown
            EditorGUI.BeginChangeCheck();
            int index = string.IsNullOrEmpty(groupName.stringValue) ? 0 : ArrayUtility.IndexOf(groupNames, groupName.stringValue);
            index = EditorGUILayout.Popup(index, groupNames, GUILayout.MaxWidth(SecondDropdownSize));
            if(EditorGUI.EndChangeCheck())
                groupName.stringValue = index == 0 ? null : groupNames[index];

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel += 1;
            if(!string.IsNullOrEmpty(groupName.stringValue))
            {
                //Default Value
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Default On");
                if(GUILayout.Button(controlGroup.defaultControl != null ? ((Control)controlGroup.defaultControl).name : "[None]"))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("[None]"), controlGroup.defaultControl == null, () =>
                    {
                        controlGroup.SetDefault(null);
                        property.serializedObject.Update();
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                    });
                    foreach(var item in controlGroup.controls)
                    {
                        var groupControl = item as Control;
                        menu.AddItem(new GUIContent(groupControl.name), controlGroup.defaultControl == item, () =>
                        {
                            controlGroup.SetDefault(item);
                            property.serializedObject.Update();
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();

                //Off state
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Off State");
                if(GUILayout.Button(controlGroup.offState != null ? ((Control)controlGroup.offState).name : "[None]"))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("[None]"), controlGroup.offState == null, () =>
                    {
                        controlGroup.SetOffState(null);
                        property.serializedObject.Update();
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                    });
                    foreach(var item in controlGroup.controls)
                    {
                        var groupControl = item as Control;
                        menu.AddItem(new GUIContent(groupControl.name), controlGroup.offState == item, () =>
                        {
                            controlGroup.SetOffState(item);
                            property.serializedObject.Update();
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;
        }

        public override string helpURL => "";
        public override void PreBuild()
        {
            //if(CombinedMenu == null)
            //    CombinedMenu = new ActionMenu();
            //Combine(CombinedMenu, feature);
        }
        public override void Build()
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

            /*if(feature != CombinedMenu)
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
            }*/
        }
        public override void PostBuild() { }

        //public static ActionMenu CombinedMenu;
        public static void Combine(ActionMenu dest, ActionMenu source)
        {
            if(source == null)
                return;

            //Find the root menu path
            if(!string.IsNullOrEmpty(source.menuPath))
            {
                var path = source.menuPath.Split('/');
                foreach(var item in path)
                {
                    var control = dest.FindControl(source.menuPath) as SubMenu;
                    if(control == null)
                    {
                        control = new SubMenu();
                        control.name = item;
                        dest.controls.Add(control);
                    }
                    if(control.subMenu == null)
                        control.subMenu = new ActionMenu();
                    dest = control.subMenu;
                }
            }

            //Controls
            foreach(var control in source.controls)
            {
                var destAction = dest.FindMenuActionOfType(control.name, control.GetType(), false);
                if(destAction != null)
                {
                    //Recursive
                    if(control is ActionMenu.SubMenu sourceSubMenu)
                    {
                        var destSubMenu = destAction as ActionMenu.SubMenu;
                        if(destSubMenu.subMenu == null)
                            destSubMenu.subMenu = new ActionMenu();
                        Combine(destSubMenu.subMenu, sourceSubMenu.subMenu);
                    }
                    else
                        Debug.LogWarning($"Unable to add Action Menu Control \"{control.name}\" as it was already included by another source");
                }
                else
                {
                    //Copy the action
                    var newAction = (ActionMenu.Control)Activator.CreateInstance(control.GetType());
                    control.DeepCopyTo(newAction);
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
            foreach(var item in controls)
            {
                if(item is IGroupedControl control)
                {
                    if(control.HasGroup)
                    {
                        ControlGroup group;
                        if(!controlGroups.TryGetValue(control.Group, out group))
                        {
                            group = new ControlGroup();
                            controlGroups.Add(control.Group, group);
                        }

                        if(group.defaultControl == null && control.IsGroupDefault)
                            group.defaultControl = control;
                        if(group.offState == null && control.IsGroupOffState)
                            group.offState = control;
                        group.controls.Add(control);
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
                        var item = group.controls[i];
                        var control = (Control)item;
                        if(control != null)
                        {
                            control.parameter = paramName;
                            control.controlValue = item.IsGroupOffState ? 0 : i + 1;
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
                    if(group.controls[0] is IGroupedControl grouped)
                        grouped.Group = null;
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
                        if(toggle.HasGroup)
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
            var expressionsMenu = AvatarBuilder.AvatarDescriptor.expressionsMenu;

            //Find the root menu path
            if(!string.IsNullOrEmpty(rootMenu.menuPath))
            {
                var path = rootMenu.menuPath.Split('/');
                foreach(var item in path)
                {
                    var found = FindControl(expressionsMenu, item, VRCExpressionsMenu.Control.ControlType.SubMenu);
                    if(found == null)
                    {
                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = item;
                        control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                        control.subMenu = CreateMenuAsset(item);
                        control.subParameters = new VRCExpressionsMenu.Control.Parameter[0];
                        expressionsMenu.controls.Add(control);

                        expressionsMenu = control.subMenu; //Continue
                    }
                    else
                        expressionsMenu = found.subMenu; //Continue
                }
            }

            //Expressions
            CreateMenu(rootMenu, expressionsMenu);
        }
        static void CreateMenu(ActionMenu ourMenu, VRCExpressionsMenu expressionsMenu)
        {
            if(ourMenu == null || expressionsMenu == null)
                return;

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
                    control.subParameters = new VRCExpressionsMenu.Control.Parameter[0];
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
                    control.subParameters = new VRCExpressionsMenu.Control.Parameter[0];
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
                    //Find/Create Sub-Menu
                    var control = FindControl(expressionsMenu, action.name, VRCExpressionsMenu.Control.ControlType.SubMenu);
                    if(control == null)
                    {
                        control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                        control.subParameters = new VRCExpressionsMenu.Control.Parameter[0];
                        expressionsMenu.controls.Add(control);
                    }
                    if(control.subMenu == null)
                        control.subMenu = CreateMenuAsset(action.name);

                    //Populate sub-menu
                    CreateMenu(subMenu.subMenu, control.subMenu);
                }
            }

            //Save prefab
            EditorUtility.SetDirty(expressionsMenu);
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
            //Check if unique
            if(!FindParam(paramName))
                return paramName;

            //Attempt to create unique parameter name
            for(int i = 0; i < 100; i++)
            {
                var name = $"{paramName}_{i}";

                //Check if unique
                if(FindParam(name))
                    continue;

                return name;
            }

            bool FindParam(string name)
            {
                foreach(var other in allParameters)
                {
                    if(other.name == name)
                    {
                        return true;
                    }
                }
                return false;
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

        static VRCExpressionsMenu.Control FindControl(VRCExpressionsMenu menu, string name, VRCExpressionsMenu.Control.ControlType type)
        {
            foreach(var control in menu.controls)
            {
                if(control.name == name && control.type == type)
                    return control;
            }
            return null;
        }
        static VRCExpressionsMenu CreateMenuAsset(string name)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = "Menu_" + name;
            AvatarBuilder.SaveAsset(menu, AvatarForge.GetSaveDirectory(), "Generated");

            return menu;
        }
    }
}