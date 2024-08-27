using System.Collections.Generic;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class CustomAction : ActionItem
    {
        public override bool HasExit()
        {
            foreach(var trigger in GetTriggers(false))
                return true;
            return false;
        }
    }
}