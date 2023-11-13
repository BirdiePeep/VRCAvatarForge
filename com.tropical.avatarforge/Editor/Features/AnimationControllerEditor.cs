using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    public class AnimationControllerEditor : FeatureEditor<AnimationController>
    {
        ReorderablePropertyList animControllerList = new ReorderablePropertyList(null, foldout: false, addName:"Controller");
        public override void OnInspectorGUI()
        {
            animControllerList.list = target.FindPropertyRelative("controllers");
            animControllerList.OnElementHeader = (index, element) =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("controller"), GUIContent.none);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("layer"), GUIContent.none);
                return true;
            };
            animControllerList.OnAdd = (element) =>
            {
                element.FindPropertyRelative("layer").intValue = (int)VRCAvatarDescriptor.AnimLayerType.FX;
            };
            animControllerList.OnInspectorGUI();
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            foreach(var item in feature.controllers)
            {
                var controller = AvatarBuilder.GetController(Globals.AnimationLayer.FX);

                //Combine variables
                var sourceController = item.controller as AnimatorController;
                foreach(var variable in sourceController.parameters)
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
                    AvatarBuilder.AddParameter(controller, variable.name, variable.type, value);
                }

                //Add layers
                bool isFirstLayer = true;
                foreach(var layer in sourceController.layers)
                {
                    var newLayer = new AnimatorControllerLayer();
                    newLayer.name = layer.name;
                    newLayer.stateMachine = Object.Instantiate(layer.stateMachine) as AnimatorStateMachine;
                    newLayer.avatarMask = layer.avatarMask;
                    newLayer.blendingMode = layer.blendingMode;
                    newLayer.syncedLayerIndex = layer.syncedLayerIndex;
                    newLayer.iKPass = layer.iKPass;
                    newLayer.defaultWeight = isFirstLayer ? 1f : layer.defaultWeight;
                    newLayer.syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming;
                    controller.AddLayer(newLayer);

                    isFirstLayer = false;
                }
            }
        }
        public override void PostBuild() { }
    }
}