using System.Collections.Generic;

namespace Tropical.AvatarForge 
{
    public class Triggers : Action, ITriggerProvider
    {
        public BehaviourItem.Trigger[] triggers;

        public override Action Clone()
        {
            var result = new Triggers();
            result.triggers = (BehaviourItem.Trigger[])triggers.Clone();
            return result;
        }
        public IEnumerable<BehaviourItem.Trigger> GetTriggers()
        {
            foreach(var trigger in triggers)
                yield return trigger;
        }
    }

    public interface ITriggerProvider
    {
        public IEnumerable<BehaviourItem.Trigger> GetTriggers();
    }
}
