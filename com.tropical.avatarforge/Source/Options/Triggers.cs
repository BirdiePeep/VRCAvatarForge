using System.Collections.Generic;

namespace Tropical.AvatarForge 
{
    public class Triggers : ActionOption, ITriggerProvider
    {
        public ActionItem.Trigger[] triggers;

        public override ActionOption Clone()
        {
            var result = new Triggers();
            result.triggers = (ActionItem.Trigger[])triggers.Clone();
            return result;
        }
        public IEnumerable<ActionItem.Trigger> GetTriggers()
        {
            foreach(var trigger in triggers)
                yield return trigger;
        }
    }

    public interface ITriggerProvider
    {
        public IEnumerable<ActionItem.Trigger> GetTriggers();
    }
}
