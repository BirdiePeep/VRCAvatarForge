using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using static VRC.SDKBase.VRC_AnimatorTrackingControl;

namespace Tropical.AvatarForge
{
    public class BodyOverridesEditor : ActionEditor<BodyOverrides>
    {
        public override void OnInspectorGUI()
        {
            var head = target.FindPropertyRelative("head");
            var leftHand = target.FindPropertyRelative("leftHand");
            var rightHand = target.FindPropertyRelative("rightHand");
            var hip = target.FindPropertyRelative("hip");
            var leftFoot = target.FindPropertyRelative("leftFoot");
            var rightFoot = target.FindPropertyRelative("rightFoot");
            var leftFingers = target.FindPropertyRelative("leftFingers");
            var rightFingers = target.FindPropertyRelative("rightFingers");
            var eyes = target.FindPropertyRelative("eyes");
            var mouth = target.FindPropertyRelative("mouth");

            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUI.IndentedRect(new Rect());
            GUILayout.Space(rect.x);
            GUILayout.Space(16);
            if(GUILayout.Button("Toggle On", GUILayout.Width(200)))
            {
                SetAll(true);
            }
            if(GUILayout.Button("Toggle Off", GUILayout.Width(200)))
            {
                SetAll(false);
            }
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();

            void SetAll(bool state)
            {
                head.boolValue = state;
                leftHand.boolValue = state;
                rightHand.boolValue = state;
                hip.boolValue = state;
                leftFoot.boolValue = state;
                rightFoot.boolValue = state;
                leftFingers.boolValue = state;
                rightFingers.boolValue = state;
                eyes.boolValue = state;
                mouth.boolValue = state;
            }

            EditorGUILayout.PropertyField(head);
            EditorGUILayout.PropertyField(leftHand);
            EditorGUILayout.PropertyField(rightHand);
            EditorGUILayout.PropertyField(hip);
            EditorGUILayout.PropertyField(leftFoot);
            EditorGUILayout.PropertyField(rightFoot);
            EditorGUILayout.PropertyField(leftFingers);
            EditorGUILayout.PropertyField(rightFingers);
            EditorGUILayout.PropertyField(eyes);
            EditorGUILayout.PropertyField(mouth);
        }

        public override void Apply(AnimatorController controller, AnimatorState state, AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType)
        {
            if(stateType == AvatarBuilder.StateType.Enable)
                Apply(state, TrackingType.Animation);
            else if(stateType == AvatarBuilder.StateType.Cleanup)
                Apply(state, TrackingType.Tracking);
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public override bool AffectsState(AvatarBuilder.StateType stateType, Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX && (stateType == AvatarBuilder.StateType.Enable || stateType == AvatarBuilder.StateType.Cleanup);
        }

        void Apply(AnimatorState state, TrackingType trackingType)
        {
            var tracking = state.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            tracking.trackingHead = action.head ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftHand = action.leftHand ? trackingType : TrackingType.NoChange;
            tracking.trackingRightHand = action.rightHand ? trackingType : TrackingType.NoChange;
            tracking.trackingHip = action.hip ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftFoot = action.leftFoot ? trackingType : TrackingType.NoChange;
            tracking.trackingRightFoot = action.rightFoot ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftFingers = action.leftFingers ? trackingType : TrackingType.NoChange;
            tracking.trackingRightFingers = action.rightFingers ? trackingType : TrackingType.NoChange;
            tracking.trackingEyes = action.eyes ? trackingType : TrackingType.NoChange;
            tracking.trackingMouth = action.mouth ? trackingType : TrackingType.NoChange;
        }
    }
}
