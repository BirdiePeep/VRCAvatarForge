using UnityEngine;

namespace Tropical.AvatarForge
{
    public class ToggleObject : ActionOption, IMenuInitialize
    {
        public GameObject[] targets;

        public override ActionOption Clone()
        {
            var result = new ToggleObject();
            result.targets = (GameObject[])targets.Clone();
            return result;
        }
        public void Initialize()
        {
            targets = new GameObject[1];
        }
    }
}