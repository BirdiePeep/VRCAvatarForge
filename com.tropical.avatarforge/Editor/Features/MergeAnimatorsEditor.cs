using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Tropical.AvatarForge
{
    public class MergeAnimatorsEditor : FeatureEditor<MergeAnimators>
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(target.FindPropertyRelative("baseController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("additiveController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("gestureController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("actionController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("fxController"));

            EditorGUILayout.Space(12);

            EditorGUILayout.PropertyField(target.FindPropertyRelative("sittingController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("tposeController"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("ikposeController"));

            EditorGUILayout.Space(12);

            EditorGUILayout.PropertyField(target.FindPropertyRelative("menu"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("parameters"));
        }

        public override string helpURL => "";
        public override void PreBuild() { }
        public override void Build()
        {
            AvatarBuilder.MergeController(feature.baseController, Globals.AnimationLayer.Base);
            AvatarBuilder.MergeController(feature.additiveController, Globals.AnimationLayer.Additive);
            AvatarBuilder.MergeController(feature.gestureController, Globals.AnimationLayer.Gesture);
            AvatarBuilder.MergeController(feature.actionController, Globals.AnimationLayer.Action);
            AvatarBuilder.MergeController(feature.fxController, Globals.AnimationLayer.FX);
            AvatarBuilder.MergeController(feature.sittingController, Globals.AnimationLayer.Sitting);
            AvatarBuilder.MergeController(feature.tposeController, Globals.AnimationLayer.TPose);
            AvatarBuilder.MergeController(feature.ikposeController, Globals.AnimationLayer.IKPose);
            AvatarBuilder.MergeMenu(feature.menu);
            AvatarBuilder.MergeParameters(feature.parameters);
        }
        public override void PostBuild() { }
        public override int BuildOrder => (int)AvatarBuilder.BuildPriority.MergeAnimators;
    }
}