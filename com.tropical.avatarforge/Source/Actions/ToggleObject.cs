using UnityEngine;

namespace Tropical.AvatarForge
{
    public class ToggleObject : Action
    {
        public GameObject[] targets;

        public override Action Clone()
        {
            var result = new ToggleObject();
            result.targets = (GameObject[])targets.Clone();
            return result;
        }
    }
}