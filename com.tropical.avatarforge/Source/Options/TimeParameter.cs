using System.Collections.Generic;

namespace Tropical.AvatarForge 
{
    public class TimeParameter : ActionOption
    {
        public string parameter;

        public override ActionOption Clone()
        {
            var result = new TimeParameter();
            result.parameter = new string(parameter);
            return result;
        }
    }
}
