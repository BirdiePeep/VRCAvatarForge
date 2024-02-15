
namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class BodyOverrides : ActionOption
    {
        public bool head;
        public bool leftHand;
        public bool rightHand;
        public bool hip;
        public bool leftFoot;
        public bool rightFoot;
        public bool leftFingers;
        public bool rightFingers;
        public bool eyes;
        public bool mouth;

        public override ActionOption Clone()
        {
            var result = new BodyOverrides();
            result.head = head;
            result.leftHand = leftHand;
            result.rightHand = rightHand;
            result.hip = hip;
            result.leftFoot = leftFoot;
            result.rightFoot = rightFoot;
            result.leftFingers = leftFingers;
            result.rightFingers = rightFingers;
            result.eyes = eyes;
            result.mouth = mouth;
            return result;
        }
        
    }
}