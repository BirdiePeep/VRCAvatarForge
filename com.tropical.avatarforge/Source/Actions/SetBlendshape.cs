using UnityEngine;

namespace Tropical.AvatarForge
{
    public class SetBlendshape : Action
    {
        public SkinnedMeshRenderer target;

        [System.Serializable]
        public struct Keyframe
        {
            public string name;
            public float weight;
        }
        public Keyframe[] blendshapes;
        
        public override Action Clone()
        {
            var result = new SetBlendshape();
            result.target = target;
            result.blendshapes = (Keyframe[])blendshapes.Clone();
            return result;
        }
    }
}