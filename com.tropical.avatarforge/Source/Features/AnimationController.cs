using System.Collections.Generic;

namespace Tropical.AvatarForge
{
    public class AnimationController : Feature
    {
        [System.Serializable]
        public struct Controller
        {
            public VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType layer;
            public UnityEngine.RuntimeAnimatorController controller;
        }
        public List<Controller> controllers = new List<Controller>();
    }
}