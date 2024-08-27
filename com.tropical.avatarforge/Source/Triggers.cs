using System.Collections.Generic;

namespace Tropical.AvatarForge
{
    //Triggers
    [System.Serializable]
    public class Trigger
    {
        public Trigger()
        {
        }
        public Trigger(Trigger source)
        {
            foreach(var item in source.conditions)
                this.conditions.Add(new Condition(item));
        }

        public bool AddConditions(Trigger parent)
        {
            foreach(var item in parent.conditions)
                conditions.Add(new Condition(item));
            return true;
        }

        public IEnumerable<Condition> GetConditions(bool isEnter)
        {
            foreach(var item in conditions)
                yield return item;
        }

        public List<Condition> conditions = new List<Condition>();
    }

    [System.Serializable]
    public class Condition
    {
        public Condition()
        {
        }
        public Condition(Condition source)
        {
            this.type = source.type;
            this.parameter = string.Copy(source.parameter);
            this.logic = source.logic;
            this.value = source.value;
        }
        public Condition(Globals.ParameterEnum type, string parameter, Logic logic, float value)
        {
            this.type = type;
            this.parameter = parameter;
            this.logic = logic;
            this.value = value;
        }
        public enum Logic
        {
            Equals = 0,
            NotEquals = 1,
            GreaterThen = 2,
            LessThen = 3,
        }
        public enum LogicEquals
        {
            Equals = 0,
            NotEquals = 1,
        }
        public enum LogicCompare
        {
            GreaterThen = 2,
            LessThen = 3,
        }

        public Condition GetInverse()
        {
            var result = new Condition(this);
            switch(logic)
            {
                case Logic.Equals:
                    result.logic = Logic.NotEquals;
                    break;
                case Logic.NotEquals:
                    result.logic = Logic.Equals;
                    break;
                case Logic.GreaterThen:
                    result.logic = Logic.LessThen;
                    break;
                case Logic.LessThen:
                    result.logic = Logic.GreaterThen;
                    break;
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
        public Logic logic = Logic.Equals;
        public float value = 1;
        public bool shared = false;
    }
}