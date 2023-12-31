using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using static Tropical.AvatarForge.Globals;

namespace Tropical.AvatarForge
{
    public class NonMenuBehavioursEditor : FeatureEditor<NonMenuBehaviours>
    {
        ReorderablePropertyList behaviourList = new ReorderablePropertyList(null, foldout: false, addName:"Behaviour");
        public override void OnInspectorGUI()
        {
            behaviourList.list = target.FindPropertyRelative("behaviours");
            behaviourList.showHeader = true;
            behaviourList.OnElementHeader = (index, element) =>
            {
                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, $"Behaviour - {element.FindPropertyRelative("name").stringValue}");
                return element.isExpanded;
            };
            behaviourList.OnElementBody = (index, element) =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("name"));

                ActionsEditor actionEditor = new ActionsEditor();
                actionEditor.SetTarget(element);
                actionEditor.editor = editor;
                actionEditor.setup = setup;
                actionEditor.OnInspectorGUI();
            };
            behaviourList.OnAdd = (element) =>
            {
                element.managedReferenceValue = new CustomBehaviour();
                element.isExpanded = true;
            };
            behaviourList.OnInspectorGUI();
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            BuildLayers(feature.behaviours, AnimationLayer.Action, null);
            BuildLayers(feature.behaviours, AnimationLayer.FX, null);
        }
        public override void PostBuild() { }
        void BuildLayers(IEnumerable<CustomBehaviour> sourceActions, AnimationLayer layerType, ActionMenu.Control parentControl)
        {
            //Build normal
            AvatarBuilder.BuildGroupedLayers(sourceActions, layerType, parentControl,
            delegate (BehaviourItem behaviour)
            {
                if(!AvatarBuilder.AffectsLayer(behaviour, layerType))
                    return false;
                return true;
            },
            delegate (AnimatorController controller, string layerName, List<BehaviourItem> actions)
            {
                //Name
                if(parentControl != null)
                    layerName = $"{parentControl.name}_{layerName}_SubActions";

                //Build layer
                if(layerType == AnimationLayer.Action)
                    AvatarBuilder.BuildActionLayer(controller, actions, layerName, parentControl);
                else
                    AvatarBuilder.BuildNormalLayer(controller, actions, layerName, layerType, parentControl);
            });
        }
    }
}