using UnityEngine;

namespace Tropical.AvatarForge
{
    public class SetBlendshape : Action, IMenuInitialize
    {
        public SkinnedMeshRenderer target;

        //Options
        public bool inverted = false;

        [System.Serializable]
        public struct Keyframe
        {
            public Keyframe(string name, float weight)
            {
                this.name = name;
                this.weight = weight;
            }

            public string name;
            public float weight;
        }
        public Keyframe[] blendshapes;
        
        public override Action Clone()
        {
            var result = new SetBlendshape();
            result.target = target;
            result.blendshapes = (Keyframe[])blendshapes.Clone();
            result.inverted = inverted;
            return result;
        }
        public void Initialize()
        {
            blendshapes = new Keyframe[1];
            blendshapes[0] = new Keyframe(null, 100f);
        }
    }
}