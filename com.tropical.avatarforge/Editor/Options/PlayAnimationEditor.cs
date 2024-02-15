using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class PlayAnimationEditor : ActionEditor<PlayAnimation>
    {
        ReorderablePropertyList animationList = new ReorderablePropertyList(null, foldout: false, addName:"Animation");
        public override void OnInspectorGUI()
        {
            animationList.list = target.FindPropertyRelative("animations");
            animationList.OnElementHeader = (index, element) =>
            {
                GUILayout.Label("Animation");

                DrawAnimationReference(null, element.FindPropertyRelative("clip"), $"Animation_{Random.Range(1000, 9999)}");

                //EditorGUILayout.PropertyField(element.FindPropertyRelative("clip"), GUIContent.none);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("layer"), GUIContent.none);
                //EditorGUILayout.PropertyField(element.FindPropertyRelative("state"), GUIContent.none);

                return true;
            };
            animationList.OnAdd = (element) =>
            {
                element.FindPropertyRelative("clip").objectReferenceValue = null;
                element.FindPropertyRelative("layer").intValue = (int)Globals.AnimationLayer.FX;
                //element.FindPropertyRelative("state").intValue = (int)PlayAnimation.State.Enter;
            };
            animationList.OnInspectorGUI();

            /*switch((Globals.AnimationLayer)type.intValue)
            {
                case Globals.AnimationLayer.Action:
                    EditorGUILayout.HelpBox("Action layer is only used for transfoming humanoid bones, everything else should be modified in the FX layer.  You will need to use Body Overrides to disable IK control of any humanoid body parts you wish to animate.", MessageType.Info);
                    break;
                case Globals.AnimationLayer.FX:
                    EditorGUILayout.HelpBox("FX layer used for modifying everything except for the humanoid bones.", MessageType.Info);
                    break;
            }*/
        }

        public override void Apply(AnimationClip clip, Globals.AnimationLayer layer, bool isEnter)
        {
            foreach(var animation in action.animations)
            {
                if(animation.clip != null && animation.layer == layer && isEnter)// && (isEnter ? PlayAnimation.State.Enter : PlayAnimation.State.Exit) == animation.state)
                {
                    AvatarBuilder.CopyCurves(animation.clip, clip);
                }
            }
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            foreach(var animation in action.animations)
            {
                if(animation.clip != null && animation.layer == layerType)
                {
                    return true;
                }
            }
            return false;
        }
        public override bool RequiresAnimationLoop()
        {
            foreach(var animation in action.animations)
            {
                if(animation.clip != null && animation.clip.isLooping)
                    return true;
            }
            return false;
        }
    }
}
