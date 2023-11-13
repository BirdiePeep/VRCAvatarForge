using System.Collections.Generic;
using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class BehaviourItem
    {
        //Simple Data
        public bool enabled = true;
        public string name;

        //Actions
        [SerializeReference] public List<Action> actions = new List<Action>();

        //Timing
        public float fadeIn = 0;
        public float hold = 0;
        public float fadeOut = 0;
        public string timeParameter;

        //Build
        public virtual bool HasExit()
        {
            return true;
        }
        public virtual bool ShouldBuild()
        {
            if(!enabled)
                return false;
            return true;
        }

        //Triggers
        [System.Serializable]
        public class Trigger
        {
            public Trigger()
            {
            }
            public Trigger(Trigger source)
            {
                this.type = source.type;
                this.foldout = source.foldout;
                foreach(var item in source.conditions)
                    this.conditions.Add(new Condition(item));
            }
            public enum Type
            {
                Enter = 0,
                Exit = 1,
            }
            public Type type;
            //public bool useDefaultCondition = true;
            public List<Condition> conditions = new List<Condition>();
            public bool foldout = true;
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
                this.shared = source.shared;
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
        public List<Trigger> triggers = new List<Trigger>();
        public List<Condition> conditions = new List<Condition>();

        //Build
        public virtual string GetLayerGroup()
        {
            return null;
        }

        //Metadata
        public bool foldoutTiming = false;
        public bool foldoutOptions = false;
        public bool foldoutTriggers = false;        

        public virtual void CopyTo(BehaviourItem clone)
        {
            //Generic
            clone.name = string.Copy(this.name);
            clone.enabled = this.enabled;
            clone.fadeIn = fadeIn;
            clone.hold = hold;
            clone.fadeOut = fadeOut;
            clone.timeParameter = timeParameter;

            //Actions
            clone.actions.Clear();
            foreach(var action in this.actions)
                clone.actions.Add(action.Clone());

            //Triggers
            clone.triggers.Clear();
            foreach(var trigger in this.triggers)
                clone.triggers.Add(new BehaviourItem.Trigger(trigger));

            //Meta
            clone.foldoutTiming = this.foldoutTiming;
            clone.foldoutOptions = this.foldoutOptions;
            clone.foldoutTriggers = this.foldoutTriggers;
        }
    }
}