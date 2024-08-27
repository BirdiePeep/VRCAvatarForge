using System.Collections.Generic;

namespace Tropical.AvatarForge 
{
    public class Conditions : ActionOption, ITriggersProvider
    {
        public bool requireAllExitConditions = false;

        [System.Serializable]
        public struct ConditionData
        {
            public ConditionData(ConditionData source)
            {
                this.mode = source.mode;
                this.type = source.type;
                this.parameter = string.Copy(source.parameter);
                this.logic = source.logic;
                this.value = source.value;
            }
            public ConditionData(ConditionData.Mode mode, Globals.ParameterEnum type, string parameter, Condition.Logic logic, float value)
            {
                this.mode = mode;
                this.type = type;
                this.parameter = parameter;
                this.logic = logic;
                this.value = value;
            }

            public Condition Build(bool inverse)
            {
                var result = new Condition();
                result.type = this.type;
                result.parameter = this.parameter;
                result.logic = this.logic;
                result.value = this.value;

                //Inverse
                if(inverse)
                {
                    switch(result.logic)
                    {
                        case Condition.Logic.Equals:
                            result.logic = Condition.Logic.NotEquals;
                            break;
                        case Condition.Logic.NotEquals:
                            result.logic = Condition.Logic.Equals;
                            break;
                        case Condition.Logic.GreaterThen:
                            result.logic = Condition.Logic.LessThen;
                            break;
                        case Condition.Logic.LessThen:
                            result.logic = Condition.Logic.GreaterThen;
                            break;
                    }
                }

                return result;
            }

            public string GetParameter()
            {
                if(type == Globals.ParameterEnum.Custom)
                    return parameter;
                else
                    return type.ToString();
            }

            public enum Mode
            {
                Simple = 0,
                OnEnter = 1,
                OnExit = 2,
            }
            public Mode mode;

            //Condition data
            public Globals.ParameterEnum type;
            public string parameter;
            public Condition.Logic logic;
            public float value;
        }
        public List<Condition> conditions = new List<Condition>();

        public override ActionOption Clone()
        {
            var result = new Conditions();
            foreach(var condition in conditions)
                result.conditions.Add(new Condition(condition));
            return result;
        }
        public IEnumerable<Trigger> GetTriggers(bool isEnter)
        {
            yield break;
            /*if(isEnter)
            {
                if(trigger.HasEnterConditions())
                    yield return trigger;
            }
            else
            {
                if(trigger.HasExitConditions())
                {
                    if(requireAllExitConditions)
                        yield return trigger;
                    else
                    {
                        foreach(var condition in trigger.conditions)
                        {
                            if(condition.mode == Condition.Mode.OnExit || condition.mode == Condition.Mode.Simple)
                            {
                                var newTrigger = new Trigger();
                                newTrigger.conditions.Add(condition);
                                yield return newTrigger;
                            }
                        }
                    }
                }
            }*/
        }
    }
}
