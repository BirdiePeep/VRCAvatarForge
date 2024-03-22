using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace Tropical.AvatarForge
{
    public class AvatarBuilder
    {
        public static bool BuildFailed = false;

        public static List<Feature> BuildFeatures = new List<Feature>();
        public static VRCAvatarDescriptor AvatarDescriptor = null;
        public static AvatarForge AvatarSetup = null;
        public static Animator Animator = null;

        static AnimatorController BaseController;
        static AnimatorController AdditiveController;
        static AnimatorController GestureController;
        static AnimatorController ActionController;
        static AnimatorController FxController;
        static AnimatorController SittingController;
        static AnimatorController TPoseController;
        static AnimatorController IKPoseController;
        public static AnimatorController GetController(Globals.AnimationLayer layer)
        {
            switch(layer)
            {
                case Globals.AnimationLayer.Base:
                    return GetController(ref BaseController);
                case Globals.AnimationLayer.Additive:
                    return GetController(ref AdditiveController);
                case Globals.AnimationLayer.Gesture:
                    return GetController(ref GestureController);
                case Globals.AnimationLayer.Action:
                    return GetController(ref ActionController);
                case Globals.AnimationLayer.FX:
                    return GetController(ref FxController);
                case Globals.AnimationLayer.Sitting:
                    return GetController(ref SittingController);
                case Globals.AnimationLayer.TPose:
                    return GetController(ref TPoseController);
                case Globals.AnimationLayer.IKPose:
                    return GetController(ref IKPoseController);
            }
            AnimatorController GetController(ref AnimatorController controller)
            {
                if(controller == null)
                    controller = CreateController((VRCAvatarDescriptor.AnimLayerType)layer, $"AnimationController_{layer.ToString()}");
                return controller;
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
                //Remove from the hierarchy before merging
                setup.transform.SetParent(null, false);

                //Add features
                BuildFeatures.AddRange(setup.features);
            }

            //Find all attachable gameobjects
            var attachments = new HashSet<GameObject>();
            foreach(var setup in setups)
            {
                if(setup.gameObject == null || setup.gameObject == desc.gameObject)
                    continue;
                attachments.Add(setup.gameObject);
            }
            foreach(var obj in attachments)
            {
                //Attach prefab, this will delete setups
                AttachPrefab(obj, AvatarDescriptor);
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
            ActionController = null;
            BaseController = null;
            AdditiveController = null;
            GestureController = null;
            ActionController = null;
            FxController = null;
            Animator.runtimeAnimatorController = null;

            //Delete all generated animations
            GeneratedClips.Clear();

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
                var buildParams = BuildParameters.ToArray();
                if(AvatarDescriptor.expressionParameters?.parameters != null && AvatarDescriptor.customExpressions && AvatarSetup.mergeAnimators)
                    ArrayUtility.AddRange(ref buildParams, AvatarDescriptor.expressionParameters.parameters);
                AvatarDescriptor.expressionParameters.parameters = buildParams;

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

            //Apply animator
            Animator.runtimeAnimatorController = FxController;

            //Save prefab
            AssetDatabase.SaveAssets();

            //Destroy Actions Descriptor
            //GameObject.DestroyImmediate(ActionsDescriptor.gameObject);
        }

        //Controllers
        static AnimatorController CreateController(VRCAvatarDescriptor.AnimLayerType animLayerType, string name)
        {
            //Define layer
            var descLayer = new VRCAvatarDescriptor.CustomAnimLayer();
            descLayer.mask = null;
            descLayer.isDefault = false;
            descLayer.type = animLayerType;

            //Dir Path
            var dirPath = AvatarForge.GetSaveDirectory();
            dirPath = $"{dirPath}/Generated";
            System.IO.Directory.CreateDirectory(dirPath);

            //Create
            var path = $"{dirPath}/{name}.controller";

            //Copy animation controller
            if(AvatarSetup.mergeAnimators)
            {
                var layer = GetDescriptorLayer(animLayerType);
                if(!layer.isDefault && layer.animatorController != null)
                {
                    descLayer.animatorController = Object.Instantiate(layer.animatorController);
                    descLayer.animatorController.name = name;
                    AssetDatabase.CreateAsset(descLayer.animatorController, path);
                }
            }

            //Create controller if needed
            if(descLayer.animatorController == null)
            {
                var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
                controller.RemoveLayer(0); //Remove base layer
                descLayer.animatorController = controller;
            }

            //Save
            SetDescriptorLayer(descLayer);
            EditorUtility.SetDirty(AvatarDescriptor);

            //Add defaults
            AddParameter((AnimatorController)descLayer.animatorController, "True", AnimatorControllerParameterType.Bool, 1);

            //Return
            return (AnimatorController)descLayer.animatorController;
        }
        static VRCAvatarDescriptor.CustomAnimLayer GetDescriptorLayer(VRCAvatarDescriptor.AnimLayerType animLayerType)
        {
            switch(animLayerType)
            {
                case VRCAvatarDescriptor.AnimLayerType.Action:
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                case VRCAvatarDescriptor.AnimLayerType.Base:
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                case VRCAvatarDescriptor.AnimLayerType.FX:
                {
                    foreach(var item in AvatarDescriptor.baseAnimationLayers)
                    {
                        if(item.type == animLayerType)
                            return item;
                    }
                    break;
                }
                case VRCAvatarDescriptor.AnimLayerType.Sitting:
                case VRCAvatarDescriptor.AnimLayerType.TPose:
                case VRCAvatarDescriptor.AnimLayerType.IKPose:
                {
                    foreach(var item in AvatarDescriptor.specialAnimationLayers)
                    {
                        if(item.type == animLayerType)
                            return item;
                    }
                    break;
                }
            }

            return default;
        }
        static void SetDescriptorLayer(VRCAvatarDescriptor.CustomAnimLayer desc)
        {
            switch(desc.type)
            {
                case VRCAvatarDescriptor.AnimLayerType.Action:
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                case VRCAvatarDescriptor.AnimLayerType.Base:
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                case VRCAvatarDescriptor.AnimLayerType.FX:
                {
                    for(int i = 0; i < AvatarDescriptor.baseAnimationLayers.Length; i++)
                    {
                        if(AvatarDescriptor.baseAnimationLayers[i].type == desc.type)
                        {
                            AvatarDescriptor.baseAnimationLayers[i] = desc;
                            break;
                        }
                    }
                    break;
                }
                case VRCAvatarDescriptor.AnimLayerType.Sitting:
                case VRCAvatarDescriptor.AnimLayerType.TPose:
                case VRCAvatarDescriptor.AnimLayerType.IKPose:
                {
                    for(int i = 0; i < AvatarDescriptor.specialAnimationLayers.Length; i++)
                    {
                        if(AvatarDescriptor.specialAnimationLayers[i].type == desc.type)
                        {
                            AvatarDescriptor.specialAnimationLayers[i] = desc;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        //Menu
        static void InitExpressionMenu()
        {
            var oldMenu = AvatarDescriptor.customExpressions ? AvatarDescriptor.expressionsMenu : null;

            //Create root menu
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = "Menu_Root";
            AvatarDescriptor.expressionsMenu = menu;

            //Merge in existing
            if(oldMenu != null && AvatarSetup.mergeAnimators)
                MergeMenu(oldMenu);

            //Save
            SaveAsset(menu, AvatarForge.GetSaveDirectory(), "Generated");
        }

        public static void MergeMenu(VRCExpressionsMenu menu)
        {
            if(menu == null)
                return;

            foreach(var source in menu.controls)
            {
                //Copy
                var newControl = Clone(source);
                newControl.subMenu = CopyMenu(source.subMenu);

                //Add
                AvatarDescriptor.expressionsMenu.controls.Add(newControl);
            }

            VRCExpressionsMenu CopyMenu(VRCExpressionsMenu item)
            {
                if(item == null)
                    return null;

                //Copy
                var result = Object.Instantiate(item);
                result.name = $"Menu_{item.name}";

                foreach(var control in result.controls)
                {
                    if(control.type != VRCExpressionsMenu.Control.ControlType.SubMenu)
                        continue;
                    control.subMenu = CopyMenu(control.subMenu);
                }

                SaveAsset(result, AvatarForge.GetSaveDirectory(), "Generated");
                return result;
            }
        }
        public static void MergeParameters(VRCExpressionParameters source)
        {
            if(source == null)
                return;

            //Copy
            for(int i = 0; i < source.parameters.Length; i++)
                BuildParameters.Add(Clone(source.parameters[i]));
        }
        public static void MergeController(RuntimeAnimatorController source, Globals.AnimationLayer animLayer)
        {
            if(source == null)
                return;

            var controller = GetController(animLayer);
            var toMerge = (AnimatorController)source;

            //Combine variables
            foreach(var variable in toMerge.parameters)
            {
                float value = 0;
                switch(variable.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        value = variable.defaultBool ? 1f : 0f; break;
                    case AnimatorControllerParameterType.Float:
                        value = variable.defaultFloat; break;
                    case AnimatorControllerParameterType.Int:
                        value = variable.defaultInt; break;
                }
                AddParameter(controller, variable.name, variable.type, value);
            }

            //Add layers
            foreach(var layer in toMerge.layers)
            {
                var newLayer = new AnimatorControllerLayer();
                newLayer.name = layer.name;
                newLayer.stateMachine = layer.stateMachine;
                newLayer.avatarMask = layer.avatarMask;
                newLayer.blendingMode = layer.blendingMode;
                newLayer.syncedLayerIndex = layer.syncedLayerIndex;
                newLayer.iKPass = layer.iKPass;
                newLayer.defaultWeight = layer.defaultWeight;
                newLayer.syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;
                controller.AddLayer(newLayer);
            }
        }
        static VRCExpressionsMenu.Control Clone(VRCExpressionsMenu.Control source)
        {
            var item = new VRCExpressionsMenu.Control();
            item.name = source.name;
            item.type = source.type;
            item.icon = source.icon;
            item.parameter = source.parameter;
            item.value = source.value;
            item.style = source.style;
            item.subMenu = source.subMenu;
            item.subParameters = source.subParameters;
            item.labels = source.labels;
            return item;
        }
        static VRCExpressionParameters.Parameter Clone(VRCExpressionParameters.Parameter source)
        {
            var item = new VRCExpressionParameters.Parameter();
            item.name = source.name;
            item.valueType = source.valueType;
            item.defaultValue = source.defaultValue;
            item.saved = source.saved;
            item.networkSynced = source.networkSynced;
            return item;
        }

        //Parameters
        public static Dictionary<string, VRCExpressionParameters.Parameter> AdditionalParameters = new Dictionary<string, VRCExpressionParameters.Parameter>();
        public static List<VRCExpressionParameters.Parameter> BuildParameters = new List<VRCExpressionParameters.Parameter>();
        static void InitVRCExpressionParameters()
        {
            var oldParams = AvatarDescriptor.customExpressions ? AvatarDescriptor.expressionParameters : null;

            //Check if parameter object exists
            var parametersObject = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            AvatarDescriptor.customExpressions = true;
            AvatarDescriptor.expressionParameters = parametersObject;
            parametersObject.name = "VRCExpressionParameters";

            //Merge existing
            if(oldParams != null && AvatarDescriptor.customExpressions && AvatarSetup.mergeAnimators)
                MergeParameters(oldParams);

            SaveAsset(parametersObject, AvatarForge.GetSaveDirectory(), "Generated");

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
        public static void BuildActionLayer(AnimatorController controller, IEnumerable<ActionItem> behaviourItems, string layerName, ActionMenu.Control parentAction, bool turnOffState = true, bool useWriteDefaults=true)
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
            waitingState.writeDefaultValues = useWriteDefaults;

            //Actions
            int itemIter = 0;
            foreach(var item in behaviourItems)
            {
                UnityEditor.Animations.AnimatorState lastState;
                var timeParam = item.GetOption<TimeParameter>();

                //Enter state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Setup", StatePosition(1, itemIter));
                    state.motion = GetAnimation(item, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    AddTransitions(item, controller, waitingState, state, 0, true, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                    playable.layer = VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.Action;
                    playable.goalWeight = 1.0f;
                    playable.blendDuration = item.fadeIn;

                    //Apply Actions
                    ApplyActionToState(item, controller, state, StateType.Setup, layerType);

                    //Store
                    lastState = state;
                }

                //Enable state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Enable", StatePosition(2, itemIter));
                    state.motion = GetAnimation(item, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(timeParam != null)
                    {
                        state.timeParameter = timeParam.parameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, timeParam.parameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = item.fadeIn;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Apply Actions
                    ApplyActionToState(item, controller, state, StateType.Enable, layerType);

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
                        if(timeParam != null)
                        {
                            state.timeParameter = timeParam.parameter;
                            state.timeParameterActive = true;
                            AddParameter(controller, timeParam.parameter, AnimatorControllerParameterType.Float, 0);
                        }

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = true;
                        transition.exitTime = item.hold;
                        transition.duration = 0;

                        //Apply Actions
                        ApplyActionToState(item, controller, state, StateType.Hold, layerType);

                        //Store
                        lastState = state;
                    }
                }

                //Disable state
                {
                    var state = layer.stateMachine.AddState(item.name + "_Disable", StatePosition(4, itemIter));
                    state.motion = GetAnimation(item, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    AddTransitions(item, controller, lastState, state, 0, false, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCPlayableLayerControl>();
                    playable.goalWeight = 0.0f;
                    playable.blendDuration = item.fadeOut;

                    //Apply Actions
                    ApplyActionToState(item, controller, state, StateType.Disable, layerType);

                    //Store
                    lastState = state;
                }

                //Fadeout state
                if(item.fadeOut > 0)
                {
                    var state = layer.stateMachine.AddState(item.name + "_Fadeout", StatePosition(5, itemIter));
                    state.motion = GetEmptyClip();//GetAnimation(item, layerType, false);
                    state.writeDefaultValues = useWriteDefaults;

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0;
                    transition.duration = item.fadeOut;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Apply Actions
                    ApplyActionToState(item, controller, state, StateType.Fadeout, layerType);

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
                    ApplyActionToState(item, controller, state, StateType.Cleanup, layerType);

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
        public static void BuildNormalLayer(UnityEditor.Animations.AnimatorController controller, IEnumerable<ActionItem> actions, string layerName, Globals.AnimationLayer layerType, ActionMenu.Control parentAction, bool turnOffState = true, bool useWriteDefaults = true)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            if(layer == null || layer.stateMachine == null)
            {
                Debug.LogError("wut");
            }
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(6, 0);

            //Waiting
            AnimatorState waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            waitingState.writeDefaultValues = useWriteDefaults;

            //Each action
            int maxStatePosition = 1;
            int actionIter = 0;
            foreach(var action in actions)
            {
                AnimatorState lastState = waitingState;
                int statePosition = 1;

                var timeParam = action.GetOption<TimeParameter>();

                //Enable
                {
                    var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(statePosition++, actionIter));
                    state.motion = GetAnimation(action, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(timeParam != null)
                    {
                        state.timeParameter = timeParam.parameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, timeParam.parameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    AddTransitions(action, controller, lastState, state, action.fadeIn, true, parentAction);

                    //Apply Actions
                    ApplyActionToState(action, controller, state, StateType.Enable, layerType);

                    //Store
                    lastState = state;
                }

                //Hold
                if(action.hold > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(statePosition++, actionIter));
                    state.motion = GetAnimation(action, layerType, true);
                    state.writeDefaultValues = useWriteDefaults;
                    if(timeParam != null)
                    {
                        state.timeParameter = timeParam.parameter;
                        state.timeParameterActive = true;
                        AddParameter(controller, timeParam.parameter, AnimatorControllerParameterType.Float, 0);
                    }

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = true;
                    transition.exitTime = action.hold;
                    transition.duration = 0;

                    //Apply Actions
                    ApplyActionToState(action, controller, state, StateType.Hold, layerType);

                    //Store
                    lastState = state;
                }

                //Exit
                if(action.HasExit() || parentAction != null)
                {
                    //Disable
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(statePosition++, actionIter));
                        state.motion = GetEmptyClip();
                        state.writeDefaultValues = useWriteDefaults;

                        //Transition
                        AddTransitions(action, controller, lastState, state, action.fadeOut, false, parentAction);

                        //Apply Actions
                        ApplyActionToState(action, controller, state, StateType.Disable, layerType);

                        //Store
                        lastState = state;
                    }

                    //Fadeout
                    /*if(action.fadeOut > 0)
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(statePosition++, actionIter));
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
                    }*/

                    //Cleanup
                    if(ActionAffectsState(action, StateType.Cleanup, layerType))
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(statePosition++, actionIter));
                        state.writeDefaultValues = useWriteDefaults;

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Apply Actions
                        ApplyActionToState(action, controller, state, StateType.Cleanup, layerType);

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
                maxStatePosition = Mathf.Max(maxStatePosition, statePosition);
                actionIter += 1;
            }

            //Exit
            layer.stateMachine.exitPosition = StatePosition(maxStatePosition, 0);
        }
        public static void ApplyActionToState(ActionItem baseAction, AnimatorController controller, AnimatorState state, StateType stateType, Globals.AnimationLayer layerType)
        {
            foreach(var option in baseAction.options)
            {
                var editor = ActionEditorBase.FindEditor(option);
                if(editor != null && editor.AffectsState(stateType, layerType))
                {
                    editor.Apply(controller, state, stateType, layerType);
                }
            }
        }
        public static bool ActionAffectsState(ActionItem baseAction, StateType stateType, Globals.AnimationLayer layerType)
        {
            foreach(var option in baseAction.options)
            {
                var editor = ActionEditorBase.FindEditor(option);
                if(editor != null && editor.AffectsState(stateType, layerType))
                    return true;
            }
            return false;
        }

        //Generated
        public static void BuildGroupedLayers(IEnumerable<ActionItem> sourceActions, Globals.AnimationLayer layerType, ActionMenu.Control parentAction, System.Func<ActionItem, bool> onCheck, System.Action<AnimatorController, string, List<ActionItem>> onBuild)
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
            var layerActions = new List<ActionItem>();
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
        public static void AddTriggerConditions(UnityEditor.Animations.AnimatorController controller, AnimatorStateTransition transition, IEnumerable<ActionItem.Condition> conditions)
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
                    case Globals.ParameterEnum.Earmuffs:
                    case Globals.ParameterEnum.IsOnFriendsList:
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
                            transition.AddCondition(condition.logic == ActionItem.Condition.Logic.Equals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        else
                            transition.AddCondition(condition.logic == ActionItem.Condition.Logic.NotEquals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        break;
                    }
                    case AnimatorControllerParameterType.Int:
                        transition.AddCondition(condition.logic == ActionItem.Condition.Logic.NotEquals ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.Equals, condition.value, paramName);
                        break;
                    case AnimatorControllerParameterType.Float:
                        transition.AddCondition(condition.logic == ActionItem.Condition.Logic.LessThen ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, condition.value, paramName);
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
                {
                    bones[i] = sourceBone;
                }
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
        public static AnimationClip GetAnimation(ActionItem behaviour, Globals.AnimationLayer layer, bool enter = true)
        {
            if(AffectsLayer(behaviour, layer))
            {
                if(enter)
                    return FindOrGenerate(behaviour, behaviour.name, layer, enter);
                else
                    return FindOrGenerate(behaviour, behaviour.name + "_Exit", layer, enter);
            }
            else
                return null;
        }
        public static bool AffectsLayer(ActionItem behaviour, Globals.AnimationLayer layerType)
        {
            foreach(var option in behaviour.options)
            {
                var editor = ActionEditorBase.FindEditor(option);
                if(editor != null)
                {
                    if(editor.AffectsLayer(layerType))
                        return true;
                }
            }
            return false;
        }

        public static AnimationClip GetEmptyClip()
        {
            AnimationClip clip;
            if(!GeneratedClips.TryGetValue("_Empty", out clip))
            {
                clip = new AnimationClip();
                clip.name = "_Empty";
                SaveAsset(clip, AvatarForge.GetSaveDirectory(), "Generated");
                GeneratedClips.Add(clip.name, clip);
            }
            return clip;
        }

        static AnimationClip FindOrGenerate(ActionItem behaviour, string clipName, Globals.AnimationLayer layer, bool isEnter)
        {
            clipName = $"{clipName}_{layer}";
            //Find/Generate
            AnimationClip generated = null;
            if(GeneratedClips.TryGetValue(clipName, out generated))
                return generated;
            else
            {
                //Generate
                generated = BuildGeneratedAnimation(behaviour, clipName, layer, isEnter);
                if(generated != null)
                {
                    GeneratedClips.Add(clipName, generated);
                    return generated;
                }
            }
            return null;
        }
        static AnimationClip BuildGeneratedAnimation(ActionItem behaviour, string clipName, Globals.AnimationLayer layer, bool isEnter)
        {
            try
            {
                //Create new animation
                AnimationClip animation = new AnimationClip();
                bool isLooping = false;
                foreach(var option in behaviour.options)
                {
                    //Apply action
                    var editor = ActionEditorBase.FindEditor(option);
                    if(editor != null)
                    {
                        editor.Apply(animation, layer, isEnter);
                        isLooping = editor.RequiresAnimationLoop() ? true : isLooping;
                    }
                }

                //Looping
                if(isLooping)
                {
                    var data = new SerializedObject(animation);
                    data.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = true;
                    data.ApplyModifiedProperties();
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
        static void AddTransitions(ActionItem behaviour, UnityEditor.Animations.AnimatorController controller, UnityEditor.Animations.AnimatorState lastState, UnityEditor.Animations.AnimatorState state, float transitionTime, bool isEnter, ActionMenu.Control parentAction)
        {
            //Find valid triggers
            var triggers = new List<ActionItem.Trigger>();
            foreach(var trigger in behaviour.GetTriggers())
            {
                switch(trigger.type)
                {
                    case ActionItem.Trigger.Type.Simple:
                    {
                        if(isEnter)
                            triggers.Add(trigger);
                        else
                        {
                            //Perform exit as an OR evaluation
                            foreach(var condition in trigger.conditions)
                            {
                                var newTrigger = new ActionItem.Trigger();
                                newTrigger.conditions.Add(condition);
                                triggers.Add(newTrigger);
                            }
                        }
                        break;
                    }
                    case ActionItem.Trigger.Type.Enter:
                    {
                        if(isEnter)
                            triggers.Add(trigger);
                        break;
                    }
                    case ActionItem.Trigger.Type.Exit:
                    {
                        if(!isEnter)
                            triggers.Add(trigger);
                        break;
                    }
                }
            }
            bool controlEquals = isEnter;

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
                    AvatarBuilder.AddTriggerConditions(controller, transition, trigger.GetConditions(isEnter));

                    //Parent Conditions - Enter
                    if(isEnter && parentAction != null)
                        AddConditions(parentAction, transition, controlEquals);

                    //Finalize
                    Finalize(transition);
                }
            }
            else
            {
                if(isEnter)
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
                else if(behaviour.HasExit())
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
            if(!isEnter && parentAction != null)
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
        static void AddConditions(ActionItem behaviour, AnimatorStateTransition transition, bool equals)
        {
            //This is a hack
            if(behaviour is ActionMenu.Control control)
            {
                ActionMenuEditor.AddCondition(control, transition, equals);
            }
        }
    }
}