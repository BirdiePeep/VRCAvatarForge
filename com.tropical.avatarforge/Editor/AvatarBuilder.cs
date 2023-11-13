using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Tropical.AvatarForge
{
    public class AvatarBuilder
    {
        public static bool BuildFailed = false;

        public static List<Feature> BuildFeatures = new List<Feature>();
        public static VRCAvatarDescriptor AvatarDescriptor = null;
        public static AvatarForge AvatarSetup = null;
        public static Animator Animator = null;
        public static AnimatorController ActionController;
        public static AnimatorController FxController;
        public static AnimatorController GetController(Globals.AnimationLayer layer)
        {
            switch(layer)
            {
                case Globals.AnimationLayer.Action:
                    return ActionController;
                case Globals.AnimationLayer.FX:
                    return FxController;
            }
            return null;
        }

        public static Dictionary<string, AnimationClip> GeneratedClips = new Dictionary<string, AnimationClip>();
        public static Dictionary<string, List<ActionMenu.Control>> ParameterToMenuActions = new Dictionary<string, List<ActionMenu.Control>>();

        public static void BuildAvatarCopy(VRCAvatarDescriptor desc, AvatarForge setup, string postFix)
        {
            //Delete old preview object
            string name = desc.gameObject.name + postFix;
            var newObj = GameObject.Find(name);
            if(newObj != null)
                GameObject.DestroyImmediate(newObj);

            //Copy original
            newObj = GameObject.Instantiate(desc.gameObject);
            newObj.name = desc.gameObject.name + postFix;

            //Build
            desc = newObj.GetComponent<VRCAvatarDescriptor>();
            setup = newObj.GetComponent<AvatarForge>();
            BuildAvatarDestructive(desc, setup);

            //Remove setup script
            GameObject.DestroyImmediate(setup);
        }
        public static bool BuildAvatarDestructive(VRCAvatarDescriptor desc, AvatarForge actionsDesc)
        {
            Debug.Log("Building Avatar");

            //Store
            AvatarDescriptor = desc;
            AvatarSetup = actionsDesc;
            Animator = desc.gameObject.GetComponent<Animator>();
            BuildFailed = false;
            BuildFeatures.Clear();

            //Combine all components
            var setups = desc.gameObject.GetComponentsInChildren<AvatarForge>(true);
            foreach(var setup in setups)
            {
                //Attach prefab
                if(setup != actionsDesc)
                {
                    AttachPrefab(setup.gameObject, AvatarDescriptor);
                }

                //Add features
                BuildFeatures.AddRange(setup.features);
            }

            //Prebuilding can modify this container, so we need to use a for loop
            for(int i=0; i<BuildFeatures.Count; i++) 
            {
                var editor = FeatureEditorBase.FindEditor(BuildFeatures[i]);
                if(editor != null)
                    editor.PreBuild();
            }

            //Setup
            BuildSetup();

            //Build Features
            foreach(var feature in BuildFeatures)
            {
                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor != null)
                    editor.Build();
            }

            //Postbuild
            foreach(var feature in BuildFeatures)
            {
                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor != null)
                    editor.PostBuild();
            }

            //Cleanup
            BuildCleanup();

            //Error
            if(BuildFailed)
            {
                EditorUtility.DisplayDialog("Build Failed", "Build has failed.", "Okay");
            }
            else
            {
                Debug.Log("Avatar Complete, Ready To Upload!");
            }
            
            return !BuildFailed;
        }

        public static void BuildSetup()
        {
            //BaseAction Controller
            AvatarDescriptor.customizeAnimationLayers = true;
            ActionController = GetController(VRCAvatarDescriptor.AnimLayerType.Action, "AnimationController_Action");
            FxController = GetController(VRCAvatarDescriptor.AnimLayerType.FX, "AnimationController_FX");
            Animator.runtimeAnimatorController = FxController;

            AnimatorController GetController(VRCAvatarDescriptor.AnimLayerType animLayerType, string name)
            {
                //Find desc layer
                var descLayer = new VRCAvatarDescriptor.CustomAnimLayer();
                int descLayerIndex = 0;
                foreach(var layer in AvatarDescriptor.baseAnimationLayers)
                {
                    if(layer.type == animLayerType)
                    {
                        descLayer = layer;
                        break;
                    }
                    descLayerIndex++;
                }

                //Find/Create Layer
                var controller = descLayer.animatorController as UnityEditor.Animations.AnimatorController;
                if(controller == null || descLayer.isDefault)
                {
                    //Dir Path
                    var dirPath = AvatarForge.GetSaveDirectory();
                    dirPath = $"{dirPath}/Generated";
                    System.IO.Directory.CreateDirectory(dirPath);

                    //Create
                    var path = $"{dirPath}/{name}.controller";
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

                    //Add base layer
                    controller.AddLayer("Base Layer");

                    //Save
                    descLayer.animatorController = controller;
                    descLayer.isDefault = false;
                    AvatarDescriptor.baseAnimationLayers[descLayerIndex] = descLayer;
                    EditorUtility.SetDirty(AvatarDescriptor);
                }

                //Cleanup Layers
                {
                    //Clean layers
                    for(int i = 0; i < controller.layers.Length; i++)
                    {
                        if(controller.layers[i].name == "Base Layer")
                            continue;

                        //Remove
                        controller.RemoveLayer(i);
                        i--;
                    }

                    //Clean parameters
                    for(int i = 0; i < controller.parameters.Length; i++)
                    {
                        //Remove
                        controller.RemoveParameter(i);
                        i--;
                    }
                }

                //Add defaults
                AddParameter(controller, "True", AnimatorControllerParameterType.Bool, 1);

                //Return
                return controller;
            }

            //Delete all generated animations
            GeneratedClips.Clear();
            /*{
                var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor.ReturnAnyScriptableObject());
                dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                var files = System.IO.Directory.GetFiles(dirPath);
                foreach (var file in files)
                {
                    if (file.Contains("_Generated"))
                        System.IO.File.Delete(file);
                }
            }*/

            //Parameters
            InitExpressionMenu();
            InitVRCExpressionParameters();
        }
        public static void BuildCleanup()
        {
            var components = AvatarDescriptor.gameObject.GetComponentsInChildren<ITemporaryComponent>();
            foreach(var comp in components)
                GameObject.DestroyImmediate(comp as MonoBehaviour);

            //Save
            EditorUtility.SetDirty(AvatarDescriptor);
            EditorUtility.SetDirty(AvatarBuilder.AvatarDescriptor.expressionsMenu);

            //Save Parameters
            {
                //Expression Parameters
                AvatarDescriptor.expressionParameters.parameters = BuildParameters.ToArray();

                //Controllers
                AddParameters(Globals.AnimationLayer.Action);
                AddParameters(Globals.AnimationLayer.FX);
                void AddParameters(Globals.AnimationLayer layer)
                {
                    var controller = GetController(layer);
                    if(controller != null)
                    {
                        foreach(var param in BuildParameters)
                        {
                            AnimatorControllerParameterType valueType;
                            switch(param.valueType)
                            {
                                case VRCExpressionParameters.ValueType.Bool: valueType = AnimatorControllerParameterType.Bool; break;
                                case VRCExpressionParameters.ValueType.Int: valueType = AnimatorControllerParameterType.Int; break;
                                default:
                                case VRCExpressionParameters.ValueType.Float: valueType = AnimatorControllerParameterType.Float; break;
                            }
                            AddParameter(controller, param.name, valueType, param.defaultValue);
                        }
                    }
                }

                //Check parameter count
                var parametersObject = AvatarDescriptor.expressionParameters;
                if(parametersObject.CalcTotalCost() > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCVRCExpressionParameters. Too many parameters defined.", "Okay");
                    return;
                }

                EditorUtility.SetDirty(AvatarDescriptor.expressionParameters);
            }

            //Save prefab
            AssetDatabase.SaveAssets();

            //Destroy Actions Descriptor
            //GameObject.DestroyImmediate(ActionsDescriptor.gameObject);
        }

        //Menu
        static void InitExpressionMenu()
        {
            //Create root menu if needed
            if(AvatarDescriptor.expressionsMenu == null)
            {
                AvatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AvatarDescriptor.expressionsMenu.name = "ExpressionsMenu_Root";
                SaveAsset(AvatarBuilder.AvatarDescriptor.expressionsMenu, AvatarForge.GetSaveDirectory(), "Generated");
            }
        }

        //Parameters
        public static Dictionary<string, VRCExpressionParameters.Parameter> AdditionalParameters = new Dictionary<string, VRCExpressionParameters.Parameter>();
        public static List<VRCExpressionParameters.Parameter> BuildParameters = new List<VRCExpressionParameters.Parameter>();
        static void InitVRCExpressionParameters()
        {
            //Check if parameter object exists
            var parametersObject = AvatarDescriptor.expressionParameters;
            if(AvatarDescriptor.expressionParameters == null || !AvatarDescriptor.customExpressions)
            {
                parametersObject = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                parametersObject.name = "VRCExpressionParameters";
                SaveAsset(parametersObject, AvatarForge.GetSaveDirectory(), "Generated");

                AvatarDescriptor.customExpressions = true;
                AvatarDescriptor.expressionParameters = parametersObject;
            }

            //Clear parameters
            BuildParameters.Clear();
            AdditionalParameters.Clear();

            //Receivers
            var recivers = AvatarDescriptor.gameObject.GetComponentsInChildren<VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver>(true);
            foreach(var item in recivers)
            {
                if(string.IsNullOrEmpty(item.parameter))
                    continue;
                switch(item.receiverType)
                {
                    case VRC.Dynamics.ContactReceiver.ReceiverType.Proximity:
                        DefineAdditional(item.parameter, VRCExpressionParameters.ValueType.Float);
                        break;
                    default:
                        DefineAdditional(item.parameter, VRCExpressionParameters.ValueType.Bool);
                        break;
                }
            }

            //PhysBones
            var physBones = AvatarDescriptor.gameObject.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>(true);
            foreach(var item in physBones)
            {
                if(string.IsNullOrEmpty(item.parameter))
                    continue;
                DefineAdditional($"{item.parameter}_IsGrabbed", VRCExpressionParameters.ValueType.Bool);
                DefineAdditional($"{item.parameter}_Angle", VRCExpressionParameters.ValueType.Float);
                DefineAdditional($"{item.parameter}_Stretch", VRCExpressionParameters.ValueType.Float);
            }

            void DefineAdditional(string name, VRCExpressionParameters.ValueType type)
            {
                if(AdditionalParameters.ContainsKey(name))
                    return;

                var param = new VRCExpressionParameters.Parameter();
                param.name = name;
                param.valueType = type;
                AdditionalParameters.Add(param.name, param);
            }
        }
        public static void DefineExpressionParamaeter(string name, VRCExpressionParameters.ValueType valueType, float defaultValue = 0, bool saved = false)
        {
            var param = new VRCExpressionParameters.Parameter();
            param.name = name;
            param.valueType = valueType;
            param.defaultValue = 0;
            param.saved = false;
            DefineExpressionParameter(param);
        }
        public static void DefineExpressionParameter(VRCExpressionParameters.Parameter parameter)
        {
            //Check if already exists
            foreach(var param in BuildParameters)
            {
                if(param.name == parameter.name)
                    return;
            }

            //Add
            BuildParameters.Add(parameter);
        }
        public static VRCExpressionParameters.Parameter FindExpressionParameter(string name)
        {
            //Build Params
            foreach(var item in BuildParameters)
            {
                if(item.name == name)
                    return item;
            }

            //Additional Params
            VRCExpressionParameters.Parameter param;
            if(AdditionalParameters.TryGetValue(name, out param))
                return param;

            //Fail
            return null;
        }

        public enum StateType
        {
            Waiting,    //Never Called
            Setup,      //Action Only
            Enable,
            Hold,
            Disable,
            Fadeout,
            Cleanup,
        }

        //Normal
        public static void BuildActionLayer(AnimatorController controller, IEnumerable<BehaviourItem> behaviourItems, string layerName, ActionMenu.Control parentAction, bool turnOffState = true, bool useWriteDefaults=true)
        {
            var layerType = Globals.AnimationLayer.Action;

            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(7, 0);

            //Animation Layer Weight
            int layerIndex = 0;
            for(int i = 0; i < controller.layers.Length; i++)
            {
                if(controller.layers[i].name == layer.name)
                {
                    layerIndex = i;
                    break;
                }
            }

            //Waiting state
            var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            waitingState.writeDefaultValues = !turnOffState && useWriteDefaults;

            //Actions
            int itemIter = 0;
            foreach(var item in behaviourItems)
            {
                UnityEditor.Animations.AnimatorState lastState;

                //Enter state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Setup", StatePosition(1, itemIter));
                    state.motion = GetAnimation(item, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    AddTransitions(item, controller, waitingState, state, 0, BehaviourItem.Trigger.Type.Enter, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                    playable.layer = VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.Action;
                    playable.goalWeight = 1.0f;
                    playable.blendDuration = item.fadeIn;

                    //Apply Actions
                    ApplyActionsToState(item, controller, state, StateType.Setup, layerType);

                    //Store
                    lastState = state;
                }

                //Enable state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Enable", StatePosition(2, itemIter));
                    state.motion = GetAnimation(item, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(!string.IsNullOrEmpty(item.timeParameter))
                    {
                        state.timeParameter = item.timeParameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, item.timeParameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = item.fadeIn;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Apply Actions
                    ApplyActionsToState(item, controller, state, StateType.Enable, layerType);

                    //Store
                    lastState = state;
                }

                //Hold
                if(item.hold > 0)
                {
                    //Hold
                    {
                        var state = layer.stateMachine.AddState(item.name + "_Hold", StatePosition(3, itemIter));
                        state.motion = GetAnimation(item, layerType, true);
                        state.writeDefaultValues = useWriteDefaults;
                        if(!string.IsNullOrEmpty(item.timeParameter))
                        {
                            state.timeParameter = item.timeParameter;
                            state.timeParameterActive = true;
                            AddParameter(controller, item.timeParameter, AnimatorControllerParameterType.Float, 0);
                        }

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = true;
                        transition.exitTime = item.hold;
                        transition.duration = 0;

                        //Apply Actions
                        ApplyActionsToState(item, controller, state, StateType.Hold, layerType);

                        //Store
                        lastState = state;
                    }
                }

                //Disable state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Disable", StatePosition(4, itemIter));
                    state.motion = GetAnimation(item, layerType, false);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    AddTransitions(item, controller, lastState, state, 0, BehaviourItem.Trigger.Type.Exit, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                    playable.goalWeight = 0.0f;
                    playable.blendDuration = item.fadeOut;

                    //Apply Actions
                    ApplyActionsToState(item, controller, state, StateType.Disable, layerType);

                    //Store
                    lastState = state;
                }

                //Fadeout state
                if(item.fadeOut > 0)
                {
                    var state = layer.stateMachine.AddState(item.name + "_Fadeout", StatePosition(5, itemIter));
                    state.motion = GetAnimation(item, layerType, false);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0;
                    transition.duration = item.fadeOut;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Apply Actions
                    ApplyActionsToState(item, controller, state, StateType.Fadeout, layerType);

                    //Store
                    lastState = state;
                }

                //Cleanup state
                if(ActionAffectsState(item, StateType.Cleanup, layerType))
                {
                    var state = layer.stateMachine.AddState(item.name + "_Cleanup", StatePosition(6, itemIter));
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Apply Actions
                    ApplyActionsToState(item, controller, state, StateType.Cleanup, layerType);

                    //Store
                    lastState = state;
                }

                //Exit transition
                {
                    var transition = lastState.AddExitTransition();
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }

                //Iterate
                itemIter += 1;
            }
        }
        public static void BuildNormalLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<BehaviourItem> actions, string layerName, Globals.AnimationLayer layerType, ActionMenu.Control parentAction, bool turnOffState = true, bool useWriteDefaults = true)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(6, 0);

            //Waiting
            AnimatorState waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            waitingState.writeDefaultValues = !turnOffState && useWriteDefaults;

            //Each action
            int actionIter = 0;
            foreach(var action in actions)
            {
                AnimatorState lastState = waitingState;

                //Enable
                {
                    var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                    state.motion = GetAnimation(action, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(!string.IsNullOrEmpty(action.timeParameter))
                    {
                        state.timeParameter = action.timeParameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, action.timeParameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    AddTransitions(action, controller, lastState, state, action.fadeIn, BehaviourItem.Trigger.Type.Enter, parentAction);

                    //Apply Actions
                    ApplyActionsToState(action, controller, state, StateType.Enable, layerType);

                    //Store
                    lastState = state;
                }

                //Hold
                if(action.hold > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(2, actionIter));
                    state.motion = GetAnimation(action, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(!string.IsNullOrEmpty(action.timeParameter))
                    {
                        state.timeParameter = action.timeParameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, action.timeParameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = true;
                    transition.exitTime = action.hold;
                    transition.duration = 0;

                    //Apply Actions
                    ApplyActionsToState(action, controller, state, StateType.Hold, layerType);

                    //Store
                    lastState = state;
                }

                //Exit
                if(action.HasExit() || parentAction != null)
                {
                    //Disable
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(3, actionIter));
                        state.motion = GetAnimation(action, layerType, false);
                        state.writeDefaultValues = useWriteDefaults;

                        //Transition
                        AddTransitions(action, controller, lastState, state, 0, BehaviourItem.Trigger.Type.Exit, parentAction);

                        //Apply Actions
                        ApplyActionsToState(action, controller, state, StateType.Disable, layerType);

                        //Store
                        lastState = state;
                    }

                    //Fadeout
                    if(action.fadeOut > 0)
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(4, actionIter));
                        state.writeDefaultValues = useWriteDefaults;

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = action.fadeOut;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Apply Actions
                        ApplyActionsToState(action, controller, state, StateType.Fadeout, layerType);

                        //Store
                        lastState = state;
                    }

                    //Cleanup
                    if(ActionAffectsState(action, StateType.Cleanup, layerType))
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(5, actionIter));
                        state.writeDefaultValues = useWriteDefaults;

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Apply Actions
                        ApplyActionsToState(action, controller, state, StateType.Cleanup, layerType);

                        //Store
                        lastState = state;
                    }

                    //Transition Exit
                    {
                        var transition = lastState.AddExitTransition();
                        transition.hasExitTime = false;
                        transition.exitTime = 0f;
                        transition.duration = 0f;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                    }
                }

                //Iterate
                actionIter += 1;
            }
        }
        public static void ApplyActionsToState(BehaviourItem baseAction, AnimatorController controller, AnimatorState state, StateType stateType, Globals.AnimationLayer layerType)
        {
            foreach(var action in baseAction.actions)
            {
                var editor = ActionEditorBase.FindEditor(action);
                if(editor != null && editor.AffectsState(stateType, layerType))
                {
                    editor.Apply(controller, state, stateType, layerType);
                }
            }
        }
        public static bool ActionAffectsState(BehaviourItem baseAction, StateType stateType, Globals.AnimationLayer layerType)
        {
            foreach(var action in baseAction.actions)
            {
                var editor = ActionEditorBase.FindEditor(action);
                if(editor != null && editor.AffectsState(stateType, layerType))
                    return true;
            }
            return false;
        }

        //Generated
        public static void BuildGroupedLayers(IEnumerable<BehaviourItem> sourceActions, Globals.AnimationLayer layerType, ActionMenu.Control parentAction, System.Func<BehaviourItem, bool> onCheck, System.Action<AnimatorController, string, List<BehaviourItem>> onBuild)
        {
            var controller = GetController(layerType);

            //Build layer groups
            List<string> layerGroups = new List<string>();
            foreach(var action in sourceActions)
            {
                var group = action.GetLayerGroup();
                if(!string.IsNullOrEmpty(group) && !layerGroups.Contains(group))
                    layerGroups.Add(group);
            }

            //Build grouped layers
            var layerActions = new List<BehaviourItem>();
            foreach(var group in layerGroups)
            {
                //Check if valid
                layerActions.Clear();
                foreach(var action in sourceActions)
                {
                    if(action.GetLayerGroup() != group)
                        continue;
                    if(!onCheck(action))
                        continue;
                    layerActions.Add(action);
                }
                if(layerActions.Count == 0)
                    continue;

                //Build
                onBuild(controller, group, layerActions);
            }

            //Build unsorted layers
            foreach(var action in sourceActions)
            {
                if(!string.IsNullOrEmpty(action.GetLayerGroup()))
                    continue;
                if(!onCheck(action))
                    continue;

                layerActions.Clear();
                layerActions.Add(action);
                onBuild(controller, action.name, layerActions);
            }
        }

        //Conditions
        public static void AddTriggerConditions(UnityEditor.Animations.AnimatorController controller, AnimatorStateTransition transition, IEnumerable<BehaviourItem.Condition> conditions)
        {
            foreach(var condition in conditions)
            {
                //Find parameter data
                string paramName = condition.GetParameter();
                AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Int;
                switch(condition.type)
                {
                    //Bool
                    case Globals.ParameterEnum.AFK:
                    case Globals.ParameterEnum.Seated:
                    case Globals.ParameterEnum.Grounded:
                    case Globals.ParameterEnum.MuteSelf:
                    case Globals.ParameterEnum.InStation:
                    case Globals.ParameterEnum.IsLocal:
                        paramType = AnimatorControllerParameterType.Bool;
                        break;
                    //Int
                    case Globals.ParameterEnum.Viseme:
                    case Globals.ParameterEnum.GestureLeft:
                    case Globals.ParameterEnum.GestureRight:
                    case Globals.ParameterEnum.VRMode:
                    case Globals.ParameterEnum.TrackingType:
                        paramType = AnimatorControllerParameterType.Int;
                        break;
                    //Float
                    case Globals.ParameterEnum.GestureLeftWeight:
                    case Globals.ParameterEnum.GestureRightWeight:
                    case Globals.ParameterEnum.AngularY:
                    case Globals.ParameterEnum.VelocityX:
                    case Globals.ParameterEnum.VelocityY:
                    case Globals.ParameterEnum.VelocityZ:
                        paramType = AnimatorControllerParameterType.Float;
                        break;
                    //Custom
                    case Globals.ParameterEnum.Custom:
                    {
                        bool found = false;

                        //Find
                        {
                            var param = FindExpressionParameter(condition.parameter);
                            if(param != null)
                            {
                                switch(param.valueType)
                                {
                                    default:
                                    case VRCExpressionParameters.ValueType.Int: paramType = AnimatorControllerParameterType.Int; break;
                                    case VRCExpressionParameters.ValueType.Float: paramType = AnimatorControllerParameterType.Float; break;
                                    case VRCExpressionParameters.ValueType.Bool: paramType = AnimatorControllerParameterType.Bool; break;
                                }
                                found = true;
                            }
                        }

                        //Find
                        if(!found)
                        {
                            foreach(var param in controller.parameters)
                            {
                                if(param.name == condition.parameter)
                                {
                                    paramType = param.type;
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if(!found)
                        {
                            Debug.LogError($"AddTriggerConditions, unable to find parameter named:{condition.parameter}");
                            BuildFailed = true;
                            return;
                        }
                        break;
                    }
                    default:
                    {
                        Debug.LogError("AddTriggerConditions, unknown parameter type for trigger condition.");
                        BuildFailed = true;
                        return;
                    }
                }

                //Add parameter
                AddParameter(controller, paramName, paramType, 0);

                //Add condition
                switch(paramType)
                {
                    case AnimatorControllerParameterType.Bool:
                    {
                        if(condition.value == 0)
                            transition.AddCondition(condition.logic == BehaviourItem.Condition.Logic.Equals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        else
                            transition.AddCondition(condition.logic == BehaviourItem.Condition.Logic.NotEquals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        break;
                    }
                    case AnimatorControllerParameterType.Int:
                        transition.AddCondition(condition.logic == BehaviourItem.Condition.Logic.NotEquals ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.Equals, condition.value, paramName);
                        break;
                    case AnimatorControllerParameterType.Float:
                        transition.AddCondition(condition.logic == BehaviourItem.Condition.Logic.LessThen ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, condition.value, paramName);
                        break;
                }
            }

            //Default true
            if(transition.conditions.Length == 0)
                transition.AddCondition(AnimatorConditionMode.If, 1f, "True");
        }

        //Controls
        public static ActionMenu.Control FindMenuControl(string controlPath)
        {
            foreach(var feature in BuildFeatures)
            {
                var actionMenu = feature as ActionMenu;
                if(actionMenu != null)
                {
                    var control = actionMenu.FindControl(controlPath);
                    if(control != null)
                        return control;
                }
            }
            return null;
        }

        //Helpers
        public static Vector3 StatePosition(int x, int y)
        {
            return new Vector3(x * 300, y * 100, 0);
        }
        public static int GetLayerIndex(AnimatorController controller, AnimatorControllerLayer layer)
        {
            for(int i = 0; i < controller.layers.Length; i++)
            {
                if(controller.layers[i].name == layer.name)
                {
                    return i;
                }
            }
            return -1;
        }
        public static AnimatorControllerLayer FindControllerLayer(AnimatorController controller, string name)
        {
            foreach(var item in controller.layers)
            {
                if(item.name == name)
                {
                    return item;
                }
            }
            return null;
        }
        public static AnimatorControllerLayer AddControllerLayer(AnimatorController controller, string name)
        {
            //Find unique name
            int count = 0;
            string finalName = name;
            while(FindControllerLayer(controller, finalName) != null)
            {
                count += 1;
                finalName = $"{name} {count}";
            }

            //Create
            var layer = new AnimatorControllerLayer();
            layer.name = finalName;
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if(AssetDatabase.GetAssetPath(controller) != "")
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));
            controller.AddLayer(layer);

            return layer;
        }
        public static AnimatorControllerLayer GetControllerLayer(AnimatorController controller, string name, bool checkForExisting = true)
        {
            //Check if exists
            if(checkForExisting)
            {
                foreach(var item in controller.layers)
                {
                    if(item.name == name)
                        return item;
                }
            }

            //Create
            var layer = new AnimatorControllerLayer();
            layer.name = name;
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if(AssetDatabase.GetAssetPath(controller) != "")
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));
            controller.AddLayer(layer);

            return layer;
        }
        public static AnimatorControllerParameter AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
        {
            //Clear
            for(int i = 0; i < controller.parameters.Length; i++)
            {
                if(controller.parameters[i].name == name)
                {
                    controller.RemoveParameter(i);
                    break;
                }
            }

            //Create
            var param = new AnimatorControllerParameter();
            param.name = name;
            param.type = type;
            param.defaultBool = value >= 1f;
            param.defaultInt = (int)value;
            param.defaultFloat = value;
            controller.AddParameter(param);

            return param;
        }
        public static void CopyCurves(AnimationClip source, AnimationClip dest)
        {
            if(source == null || dest == null)
                return;

            //Copy value curves
            foreach(var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                var destCurve = new AnimationCurve();
                foreach(var key in curve.keys)
                    destCurve.AddKey(key);
                AnimationUtility.SetEditorCurve(dest, binding, destCurve);
            }

            //Copy object curves
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(dest, binding, keys);
            }

            //Misc data
            dest.wrapMode = source.wrapMode;
        }

        public static bool SaveAsset(UnityEngine.Object asset, string dirPath, string subDir = null, bool checkIfExists = false)
        {
            //Check if exists
            if(AssetDatabase.Contains(asset))
                return true;

            //Dir Path
            if(string.IsNullOrEmpty(dirPath))
            {
                BuildFailed = true;
                EditorUtility.DisplayDialog("Build Error", "Unable to save asset, unknown asset path.", "Okay");
                return false;
            }
            if(!string.IsNullOrEmpty(subDir))
                dirPath += $"/{subDir}";
            System.IO.Directory.CreateDirectory(dirPath);

            //Path
            var path = $"{dirPath}/{asset.name}.asset";

            //Check if existing
            var existing = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if(checkIfExists && existing != null && existing != asset)
            {
                if(!EditorUtility.DisplayDialog("Replace Asset?", $"Another asset already exists at '{path}'.\nAre you sure you want to replace it?", "Replace", "Cancel"))
                    return false;
            }

            AssetDatabase.CreateAsset(asset, path);
            return true;
        }

        //Attachment
        static void AttachPrefab(GameObject instance, VRCAvatarDescriptor avatar)
        {
            //Attach each
            List<GameObject> newObjects = new List<GameObject>();
            var children = new GameObject[instance.transform.childCount];
            for(int i = 0; i < instance.transform.childCount; i++)
                children[i] = instance.transform.GetChild(i).gameObject;
            foreach(var child in children)
            {
                AttachPrefab("", child, avatar, newObjects);
            }

            //Attach bones
            foreach(var obj in newObjects)
            {
                var skinned = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach(var renderer in skinned)
                    AttachBones(avatar.transform, renderer);
            }

            //Process
            /*foreach(var obj in newObjects)
            {
                var processes = obj.GetComponentsInChildren<IOutfitProcess>(true);
                foreach(var process in processes)
                {
                    process.AttachOutfit(this, obj);
                    GameObject.DestroyImmediate((MonoBehaviour)process);
                }
            }*/

            //Destroy Instance garbage
            GameObject.DestroyImmediate(instance);
        }
        static void AttachPrefab(string path, GameObject instance, VRCAvatarDescriptor avatar, List<GameObject> newObjects)
        {
            //Path
            var parentPath = path;
            if(string.IsNullOrEmpty(path))
                path = instance.name;
            else
                path += "/" + instance.name;

            //Does this exist on the base prefab
            var existing = avatar.transform.Find(path);
            if(existing != null)
            {
                //Continue search
                var children = new GameObject[instance.transform.childCount];
                for(int i = 0; i < instance.transform.childCount; i++)
                    children[i] = instance.transform.GetChild(i).gameObject;
                foreach(var child in children)
                {
                    AttachPrefab(path, child, avatar, newObjects);
                }
            }
            else
            {
                //Does this already exist on the current model
                existing = avatar.transform.Find(path);
                if(existing != null)
                    return;

                //Add
                GameObject parent = string.IsNullOrEmpty(parentPath) ? avatar.gameObject : avatar.transform.Find(parentPath)?.gameObject;
                if(parent != null)
                {
                    instance.transform.SetParent(parent.transform, false);
                    newObjects.Add(instance);
                }
            }
        }
        static void AttachBones(Transform armature, SkinnedMeshRenderer dest)
        {
            //Root
            if(dest.rootBone != null)
                dest.rootBone = FindRecursive(armature, dest.rootBone.name);

            //Find bones
            var bones = (Transform[])dest.bones.Clone();
            for(int i = 0; i < dest.bones.Length; i++)
            {
                var boneName = bones[i].name;
                var sourceBone = FindRecursive(armature, boneName);
                if(sourceBone != null)
                    bones[i] = sourceBone;
                else
                    Debug.LogError($"Unable to find matching bone '{boneName}'");
            }
            dest.bones = bones;
        }
        static Transform FindRecursive(Transform self, string name)
        {
            //Find
            var result = self.Find(name);
            if(result != null)
                return result;

            //Recusive
            foreach(Transform child in self)
            {
                result = FindRecursive(child, name);
                if(result != null)
                    return result;
            }
            return null;
        }

        //Behaviours
        public static AnimationClip GetAnimation(BehaviourItem behaviour, Globals.AnimationLayer layer, bool enter = true)
        {
            if(AffectsLayer(behaviour, layer))
            {
                if(enter)
                    return FindOrGenerate(behaviour, behaviour.name + "_Generated", layer, enter);
                else
                    return FindOrGenerate(behaviour, behaviour.name + "_Generated_Exit", layer, enter);
            }
            else
                return null;
        }
        public static bool AffectsLayer(BehaviourItem behaviour, Globals.AnimationLayer layerType)
        {
            foreach(var action in behaviour.actions)
            {
                var editor = ActionEditorBase.FindEditor(action);
                if(editor != null)
                {
                    if(editor.AffectsLayer(layerType))
                        return true;
                }
            }
            return false;
        }
        static AnimationClip FindOrGenerate(BehaviourItem behaviour, string clipName, Globals.AnimationLayer layer, bool enter)
        {
            //Find/Generate
            AnimationClip generated = null;
            if(AvatarBuilder.GeneratedClips.TryGetValue(clipName, out generated))
                return generated;
            else
            {
                //Generate
                generated = BuildGeneratedAnimation(behaviour, clipName, layer, enter);
                if(generated != null)
                {
                    AvatarBuilder.GeneratedClips.Add(clipName, generated);
                    return generated;
                }
            }
            return null;
        }
        static AnimationClip BuildGeneratedAnimation(BehaviourItem behaviour, string clipName, Globals.AnimationLayer layer, bool enter)
        {
            try
            {
                //Create new animation
                AnimationClip animation = new AnimationClip();
                foreach(var action in behaviour.actions)
                {
                    //Apply action
                    var editor = ActionEditorBase.FindEditor(action);
                    if(editor != null)
                        editor.Apply(animation, layer, enter);
                }

                //Save
                animation.name = clipName;
                AvatarBuilder.SaveAsset(animation, AvatarForge.GetSaveDirectory(), "Generated");

                //Return
                return animation;
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Error while trying to generate animation '{clipName}'");
                return null;
            }
        }
        static void AddTransitions(BehaviourItem behaviour, UnityEditor.Animations.AnimatorController controller, UnityEditor.Animations.AnimatorState lastState, UnityEditor.Animations.AnimatorState state, float transitionTime, BehaviourItem.Trigger.Type triggerType, ActionMenu.Control parentAction)
        {
            //Find valid triggers
            var triggers = new List<BehaviourItem.Trigger>();
            foreach(var trigger in behaviour.triggers)
            {
                if(trigger.type == triggerType)
                    triggers.Add(trigger);
            }

            bool controlEquals = (triggerType != BehaviourItem.Trigger.Type.Exit);

            //Add triggers
            if(triggers.Count > 0)
            {
                //Add each transition
                foreach(var trigger in triggers)
                {
                    //Add
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = transitionTime;
                    AddConditions(behaviour, transition, controlEquals);

                    //Conditions
                    AvatarBuilder.AddTriggerConditions(controller, transition, trigger.conditions);

                    //Parent Conditions - Enter
                    if(triggerType == BehaviourItem.Trigger.Type.Enter && parentAction != null)
                        AddConditions(parentAction, transition, controlEquals);

                    //Finalize
                    Finalize(transition);
                }
            }
            else
            {
                if(triggerType == BehaviourItem.Trigger.Type.Enter)
                {
                    //Add single transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = transitionTime;
                    AddConditions(behaviour, transition, controlEquals);

                    //Parent Conditions
                    if(parentAction != null)
                        AddConditions(parentAction, transition, controlEquals);

                    //Finalize
                    Finalize(transition);
                }
                else if(triggerType == BehaviourItem.Trigger.Type.Exit && behaviour.HasExit())
                {
                    //Add single transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = transitionTime;
                    AddConditions(behaviour, transition, controlEquals);

                    //Finalize
                    Finalize(transition);
                }
            }

            //Parent Conditions - Exit
            if(triggerType == BehaviourItem.Trigger.Type.Exit && parentAction != null)
            {
                var transition = lastState.AddTransition(state);
                transition.hasExitTime = false;
                transition.duration = transitionTime;
                AddConditions(parentAction, transition, controlEquals);
            }

            void Finalize(AnimatorStateTransition transition)
            {
                if(transition.conditions.Length == 0)
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");
            }
        }
        static void AddConditions(BehaviourItem behaviour, AnimatorStateTransition transition, bool equals)
        {
            //This is a hack
            if(behaviour is ActionMenu.Control control)
            {
                ActionMenuEditor.AddCondition(control, transition, equals);
            }
        }
    }
}