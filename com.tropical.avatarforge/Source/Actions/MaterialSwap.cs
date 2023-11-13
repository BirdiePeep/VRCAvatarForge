using UnityEngine;

namespace Tropical.AvatarForge
{
    public class MaterialSwap : Action
    {
        public Renderer target;
        public Material[] materials;

        public override Action Clone()
        {
            var result = new MaterialSwap();
            result.target = target;
            result.materials = materials != null ? (Material[])materials.Clone() : null;
            return result;
        }
    }
}