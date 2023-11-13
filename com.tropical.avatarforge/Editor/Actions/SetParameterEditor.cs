using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    public class SetParameterEditor : ActionEditor<SetParameter>
    {
        ReorderablePropertyList listDrawer = new ReorderablePropertyList("Parameters", foldout: false, addName:"Parameter");
        public override void OnInspectorGUI()
        {
            listDrawer.list = target.FindPropertyRelative("parameters");
            listDrawer.OnElementHeader = (index, element) =>
            {
                DrawSimpleParameterDropDown(element.FindPropertyRelative("parameter"), null, required:false);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("changeType"), GUIContent.none);

                return true;
            };
            listDrawer.OnElementBody = (index, element) =>
            {
                var changeType = element.FindPropertyRelative("changeType");
                var type = (VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType)changeType.intValue;
                if(type == VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random)
                {
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("valueMin"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("valueMax"));
                }
                else if(type == VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Copy)
                {
                    DrawSimpleParameterDropDown(element.FindPropertyRelative("parameter"), "Destination");
                }
                else
                {
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("value"));
                }
                EditorGUILayout.PropertyField(element.FindPropertyRelative("resetOnExit"));
            };
            listDrawer.OnAdd = (element) =>
            {
                element.managedReferenceValue = new SetParameter.Parameter();
            };
            listDrawer.OnInspectorGUI();
        }

        //Building
        public override void Apply(AnimatorController controller, AnimatorState state, AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType)
        {
            var script = action as SetParameter;

            if(script.parameters.Count == 0)
                return;

            //Apply
            var driverBehaviour = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driverBehaviour.localOnly = true;
            foreach(var driver in script.parameters)
            {
                if(string.IsNullOrEmpty(driver.parameter))
                    continue;

                //Check for exit
                if(stateType == AvatarBuilder.StateType.Enable)
                {
                    //Build param
                    var param = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
                    param.name = driver.parameter;
                    param.source = driver.source;
                    param.value = driver.value;
                    param.type = driver.changeType;
                    param.valueMin = driver.valueMin;
                    param.valueMax = driver.valueMax;
                    param.chance = driver.chance;
                    /*if(driver.type == Parameter.Type.RawValue)
                    {
                        param.name = driver.name;
                        param.value = driver.value;
                        param.type = driver.changeType;
                        param.valueMin = driver.valueMin;
                        param.valueMax = driver.valueMax;
                        param.chance = driver.chance;
                    }
                    else if(driver.type == Parameter.Type.MenuToggle)
                    {
                        Debug.LogError("TODO");
                        //Search for menu action
                        var drivenAction = ActionsDescriptor.menuActions.FindMenuAction(driver.name);
                        if(drivenAction == null || drivenAction.menuType != ActionMenu.MenuItem.MenuType.Toggle)
                        {
                            BuildFailed = true;
                            EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find menu toggle named '{driver.name}' for a parameter driver.  Build Failed.", "Okay");
                            return;
                        }
                        param.name = drivenAction.parameter;
                        param.value = driver.value == 0 ? 0 : drivenAction.controlValue;
                    }
                    else if(driver.type == Parameter.Type.MenuRandom)
                    {
                        //Find max values    
                        List<ActionMenu.MenuItem> list;
                        if(AvatarBuilder.ParameterToMenuActions.TryGetValue(driver.name, out list))
                        {
                            param.name = driver.name;
                            param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random;
                            param.value = 0;
                            param.valueMin = driver.isZeroValid ? 1 : 0;
                            param.valueMax = list.Count;
                            param.chance = 0.5f;
                        }
                        else
                        {
                            AvatarBuilder.BuildFailed = true;
                            EditorUtility.DisplayDialog("Build Error", $"SetParameter: Unable to find any menu actions driven by parameter '{driver.name} for a parameter driver'.  Build Failed.", "Okay");
                            return;
                        }
                    }*/
                    driverBehaviour.parameters.Add(param);
                }
                else if(stateType == AvatarBuilder.StateType.Disable)
                {
                    //Reset on exit
                    if(driver.resetOnExit)
                    {
                        var param = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
                        param.name = driver.parameter;
                        param.value = 0;
                        param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
                        driverBehaviour.parameters.Add(param);

                        /*if(driver.type == BaseAction.ParameterDriver.Type.MenuToggle)
                        {
                            var drivenAction = ActionsDescriptor.menuActions.FindMenuAction(driver.name);
                            if(drivenAction == null || drivenAction.menuType != ActionMenu.MenuItem.MenuType.Toggle)
                            {
                                BuildFailed = true;
                                EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find menu toggle named '{driver.name}' for a parameter driver.  Build Failed.", "Okay");
                                return;
                            }
                            param.name = drivenAction.parameter;
                        }

                        driverBehaviour.parameters.Add(param);*/
                    }
                }

            }
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public override bool AffectsState(AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX && (stateType == AvatarBuilder.StateType.Enable || stateType == AvatarBuilder.StateType.Disable);
        }
    }
}
