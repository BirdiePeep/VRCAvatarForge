using System.Collections.Generic;
using System.Linq;
using static Tropical.AvatarForge.ActionItem;
using static UnityEditor.Progress;

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

            public float minWeightLeft = 0.5f;
            public float minWeightRight = 0.5f;

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

            public override IEnumerable<Trigger> GetTriggers(bool isEnter)
            {
                //Get triggers
                var triggers = GetTriggersInternal(isEnter);

                //Return sub-triggers
                foreach(var trigger in triggers)
                {
                    foreach(var item in GetTriggers(trigger, isEnter))
                        yield return item;
                }
            }
            IEnumerable<Trigger> GetTriggersInternal(bool isEnter)
            {
                if(sides == SideType.Left)
                {
                    var trigger = new Trigger();
                    AddConditionsL(trigger);
                    yield return trigger;
                }
                else if(sides == SideType.Right)
                {
                    var trigger = new Trigger();
                    AddConditionsR(trigger);
                    yield return trigger;
                }
                else if(sides == SideType.Both)
                {
                    if(isEnter)
                    {
                        //Check both L & R
                        var trigger = new Trigger();
                        AddConditionsL(trigger);
                        AddConditionsR(trigger);
                        yield return trigger;
                    }
                    else
                    {
                        //Either results in false

                        //Left
                        {
                            var trigger = new Trigger();
                            AddConditionsL(trigger);
                            yield return trigger;
                        }

                        //Right
                        {
                            var trigger = new Trigger();
                            AddConditionsR(trigger);
                            yield return trigger;
                        }
                    }
                }
                else if(sides == SideType.Either)
                {
                    if(isEnter) //Either allows entering
                    {
                        //Left
                        {
                            var trigger = new Trigger();
                            AddConditionsL(trigger);
                            yield return trigger;
                        }

                        //Right
                        {
                            var trigger = new Trigger();
                            AddConditionsR(trigger);
                            yield return trigger;
                        }
                    }
                    else //Both requried to exit
                    {
                        //Create combined trigger
                        var trigger = new Trigger();
                        AddConditionsL(trigger);
                        AddConditionsR(trigger);
                        yield return trigger;
                    }
                }
                void AddConditionsL(Trigger trigger)
                {
                    trigger.conditions.Add(new Condition(Globals.ParameterEnum.GestureLeft, "", isEnter ? Condition.Logic.Equals : Condition.Logic.NotEquals, (int)left));
                    if(left == Globals.GestureEnum.Fist)
                        trigger.conditions.Add(new Condition(Globals.ParameterEnum.GestureLeftWeight, "", isEnter ? Condition.Logic.GreaterThen : Condition.Logic.LessThen, minWeightLeft));
                }
                void AddConditionsR(Trigger trigger)
                {
                    trigger.conditions.Add(new Condition(Globals.ParameterEnum.GestureRight, "", isEnter ? Condition.Logic.Equals : Condition.Logic.NotEquals, (int)right));
                    if(left == Globals.GestureEnum.Fist)
                        trigger.conditions.Add(new Condition(Globals.ParameterEnum.GestureRightWeight, "", isEnter ? Condition.Logic.GreaterThen : Condition.Logic.LessThen, minWeightRight));
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