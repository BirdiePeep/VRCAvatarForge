using System.Collections.Generic;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class Gestures : Feature
    {
        [System.Serializable]
        public class GestureItem : CustomAction
        {
            public enum SideType
            {
                Left,
                Right,
                Either,
                Both
            }
            public SideType sides = SideType.Left;

            public Globals.GestureEnum left = Globals.GestureEnum.Neutral;
            public Globals.GestureEnum right = Globals.GestureEnum.Neutral;

            [System.Serializable]
            public struct GestureTable
            {
                public bool neutral;
                public bool fist;
                public bool openHand;
                public bool fingerPoint;
                public bool victory;
                public bool rockNRoll;
                public bool handGun;
                public bool thumbsUp;

                public bool GetValue(Globals.GestureEnum type)
                {
                    switch(type)
                    {
                        case Globals.GestureEnum.Neutral: return neutral;
                        case Globals.GestureEnum.Fist: return fist;
                        case Globals.GestureEnum.OpenHand: return openHand;
                        case Globals.GestureEnum.FingerPoint: return fingerPoint;
                        case Globals.GestureEnum.Victory: return victory;
                        case Globals.GestureEnum.RockNRoll: return rockNRoll;
                        case Globals.GestureEnum.HandGun: return handGun;
                        case Globals.GestureEnum.ThumbsUp: return thumbsUp;
                    }
                    return false;
                }
                public void SetValue(Globals.GestureEnum type, bool value)
                {
                    switch(type)
                    {
                        case Globals.GestureEnum.Neutral: neutral = value; break;
                        case Globals.GestureEnum.Fist: fist = value; break;
                        case Globals.GestureEnum.OpenHand: openHand = value; break;
                        case Globals.GestureEnum.FingerPoint: fingerPoint = value; break;
                        case Globals.GestureEnum.Victory: victory = value; break;
                        case Globals.GestureEnum.RockNRoll: rockNRoll = value; break;
                        case Globals.GestureEnum.HandGun: handGun = value; break;
                        case Globals.GestureEnum.ThumbsUp: thumbsUp = value; break;
                    }
                }

                public bool IsModified()
                {
                    return neutral || fist || openHand || fingerPoint || victory || rockNRoll || handGun || thumbsUp;
                }
            }
            public GestureTable gestureTable = new GestureTable();

            public override IEnumerable<Trigger> GetTriggers()
            {
                bool hadTriggers = false;
                foreach(var item in base.GetTriggers())
                {
                    hadTriggers = true;
                    foreach(var trigger in AddConditions(item))
                        yield return trigger;
                }
                if(!hadTriggers)
                {
                    foreach(var trigger in AddConditions(new Trigger()))
                        yield return trigger;
                }
                IEnumerable<Trigger> AddConditions(ActionItem.Trigger parent)
                {
                    if(sides == SideType.Either)
                    {
                        //Left trigger
                        var trigger = new ActionItem.Trigger(parent);
                        trigger.conditions.Add(new ActionItem.Condition(Globals.ParameterEnum.GestureLeft, "", ActionItem.Condition.Logic.Equals, (int)left));
                        yield return trigger;

                        //Right trigger
                        trigger = new ActionItem.Trigger(parent);
                        trigger.conditions.Add(new ActionItem.Condition(Globals.ParameterEnum.GestureRight, "", ActionItem.Condition.Logic.Equals, (int)right));
                        yield return trigger;
                    }
                    else
                    {
                        //Combined trigger
                        var trigger = new ActionItem.Trigger(parent);
                        if(sides != SideType.Right)
                            trigger.conditions.Add(new ActionItem.Condition(Globals.ParameterEnum.GestureLeft, "", ActionItem.Condition.Logic.Equals, (int)left));
                        if(sides != SideType.Left)
                            trigger.conditions.Add(new ActionItem.Condition(Globals.ParameterEnum.GestureRight, "", ActionItem.Condition.Logic.Equals, (int)right));
                        yield return trigger;
                    }
                }
            }
        }
        public List<GestureItem> gestures = new List<GestureItem>();

        public bool CheckGestureTypeUsed(Globals.GestureEnum type)
        {
            foreach(var action in gestures)
            {
                if(action.gestureTable.GetValue(type))
                    return false;
            }
            return true;
        }
    }
}