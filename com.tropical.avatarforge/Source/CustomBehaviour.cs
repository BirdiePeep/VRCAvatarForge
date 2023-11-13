namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class CustomBehaviour : BehaviourItem
    {
        public override bool HasExit()
        {
            //Check for exit transition
            foreach(var trigger in triggers)
            {
                if(trigger.type == Trigger.Type.Exit)
                    return true;
            }
            return false;
        }
    }
}