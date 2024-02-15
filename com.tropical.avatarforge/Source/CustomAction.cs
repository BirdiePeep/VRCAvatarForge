namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class CustomAction : ActionItem
    {
        public override bool HasExit()
        {
            //Check for exit transition
            foreach(var trigger in GetTriggers())
            {
                if(trigger.HasExit())
                    return true;
            }
            return false;
        }
    }
}