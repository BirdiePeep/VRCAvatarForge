using UnityEngine;

namespace Tropical.AvatarForge
{
    public class AttachObject : Feature
    {
        [Tooltip("What object to attach.  If left blank the object where this script exists will be used.")]
        public Transform source;
        public enum Target
        {
            Root = 6245,
            Path = 5687,
            HumanoidBone = 5265,
        }
        public Target attachTarget = Target.Root;
        public string path;
        public HumanBodyBones humanBone;

        [Tooltip("When enabled the attached object's transforms will be compared to any existing transforms via their name.  Only new transforms will be attached, all others are ignored.  This feature is useful when combing two similar armatures.")]
        public bool mergeTransforms = true;
        [Tooltip("When enabled attached objects will stay at their original world position/rotation/scale regardless of where they are attached.")]
        public bool keepWorldPosition = false;
        [Tooltip("When enabled only childen from the source object will be attached to the destination.  The source object will be ignored and destroyed.")]
        public bool attachChildrenInstead = false;
    }
}
