using System.Collections.Generic;

namespace Tropical.AvatarForge 
{
    public class Triggers : ActionOption, ITriggersProvider
    {
        public TriggerData[] triggers;

        [System.Serializable]
        public struct TriggerData
        {
            public TriggerData(TriggerData source)
            {
                this.type = source.type;
                this.conditions = (ConditionData[])source.conditions?.Clone();
                this.foldout = source.foldout;
            }

            public enum Type
            {
                Simple = 0,
                Enter = 1,
                Exit = 2,
            }
            public bool HasEnterConditions()
            {
                return type == Type.Simple || type == Type.Enter;
            }
            public bool HasExitConditions()
            {
                return type == Type.Simple || type == Type.Exit;
            }

            public Type type;
            public ConditionData[] conditions;
            public bool foldout;
        }

        [System.Serializable]
        public struct ConditionData
        {
            public ConditionData(ConditionData source)
            {
                this.type = source.type;
                this.parameter = string.Copy(source.parameter);
                this.logic = source.logic;
                this.value = source.value;
            }
            public ConditionData(Globals.ParameterEnum type, string parameter, Condition.Logic logic, float value)
            {
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

            public Globals.ParameterEnum type;
            public string parameter;
            public Condition.Logic logic;
            public float value;
        }

        public override ActionOption Clone()
        {
            var result = new Triggers();
            result.triggers = new TriggerData[triggers.Length];
            for(int i = 0; i < triggers.Length; i++)
            {
                result.triggers[i] = new TriggerData(triggers[i]);
            }
            return result;
        }
        public IEnumerable<Trigger> GetTriggers(bool isEnter)
        {
            foreach(var trigger in triggers)
            {
                //Validate if it has conditions
                if(isEnter ? !trigger.HasEnterConditions() : !trigger.HasExitConditions())
                    continue;

                if(trigger.type == TriggerData.Type.Simple && !isEnter)
                {
                    foreach(var condition in trigger.conditions)
                    {
                        var newTrigger = new Trigger();
                        newTrigger.conditions.Add(condition.Build(true));
                        yield return newTrigger;
                    }
                }
                else
                {
                    var newTrigger = new Trigger();
                    foreach(var condition in trigger.conditions)
                    {
                        newTrigger.conditions.Add(condition.Build(false));
                    }
                    yield return newTrigger;
                }
            }
        }
    }
}
