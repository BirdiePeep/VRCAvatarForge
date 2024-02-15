using UnityEngine;

namespace Tropical.AvatarForge
{
    public class MaterialSwap : ActionOption
    {
        public Renderer target;
        public Material[] materials;

        public override ActionOption Clone()
        {
            var result = new MaterialSwap();
            result.target = target;
            result.materials = materials != null ? (Material[])materials.Clone() : null;
            return result;
        }
    }
}