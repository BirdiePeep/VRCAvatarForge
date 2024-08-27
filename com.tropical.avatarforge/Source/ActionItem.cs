using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public abstract class ActionItem
    {
        //Simple Data
        public bool enabled = true;
        public string name;

        //Actions
        [FormerlySerializedAs("actions")]
        [SerializeReference] public List<ActionOption> options = new List<ActionOption>();

        //Timing
        public float fadeIn = 0;
        public float hold = 0;
        public float fadeOut = 0;
        //public string timeParameter;

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
        
        public virtual IEnumerable<Trigger> GetTriggers(bool isEnter)
        {
            var subTriggers = GetSubTriggers(isEnter).ToArray();
            if(subTriggers.Length > 0)
            {
                //Return sub-triggers
                foreach(var trigger in GetSubTriggers(isEnter))
                    yield return trigger;
            }
            else if(isEnter)
            {
                //Return default enter
                yield return new Trigger();
            }
        }
        protected IEnumerable<Trigger> GetSubTriggers(bool isEnter)
        {
            foreach(var option in options)
            {
                if(option is ITriggersProvider provder)
                {
                    foreach(var item in provder.GetTriggers(isEnter))
                        yield return item;
                }
            }
        }
        protected IEnumerable<Trigger> GetTriggers(Trigger parent, bool isEnter)
        {
            //Return sub-triggers
            bool includedParent = false;
            var subTriggers = GetSubTriggers(isEnter);
            foreach(var item in subTriggers)
            {
                includedParent = true;

                var combined = new Trigger(item);
                combined.AddConditions(parent);
                yield return combined;

                /*if(item.inheritParentConditions)
                {
                    includedParent = true;

                    var combined = new Trigger(item);
                    combined.AddConditions(parent);
                    yield return combined;
                }*/
            }
            if(!includedParent)
                yield return parent;
        }

        public TYPE GetOption<TYPE>() where TYPE : ActionOption
        {
            foreach(var option in options)
            {
                if(option is TYPE result)
                    return result;
            }
            return null;
        }

        //Build
        public virtual string GetLayerGroup()
        {
            return null;
        }

        //Metadata
        public bool foldoutTiming = false;
        public bool foldoutOptions = false;
        public bool foldoutTriggers = false;        

        public virtual void CopyTo(ActionItem clone)
        {
            //Generic
            clone.name = string.Copy(this.name);
            clone.enabled = this.enabled;
            clone.fadeIn = fadeIn;
            clone.hold = hold;
            clone.fadeOut = fadeOut;
            //clone.timeParameter = timeParameter;

            //Options
            clone.options.Clear();
            foreach(var option in this.options)
                clone.options.Add(option.Clone());

            //Meta
            clone.foldoutTiming = this.foldoutTiming;
            clone.foldoutOptions = this.foldoutOptions;
            clone.foldoutTriggers = this.foldoutTriggers;
        }
    }
}