using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    public static class Globals
    {
        //Types
        public enum GestureEnum
        {
            Neutral,
            Fist,
            OpenHand,
            FingerPoint,
            Victory,
            RockNRoll,
            HandGun,
            ThumbsUp,
        }
        public enum VisemeEnum
        {
            Sil,
            PP,
            FF,
            TH,
            DD,
            KK,
            CH,
            SS,
            NN,
            RR,
            AA,
            E,
            I,
            O,
            U
        }
        public enum TrackingTypeEnum
        {
            Generic = 1,
            ThreePoint = 3,
            FourPoint = 4,
            FullBody = 6,
        }
        public enum ParameterEnum
        {
            Custom = 0,
            GestureLeft = 1380,
            GestureRight = 5952,
            GestureLeftWeight = 4777,
            GestureRightWeight = 1712,
            Viseme = 9866,
            AFK = 4804,
            VRMode = 1291,
            TrackingType = 9993,
            MuteSelf = 1566,
            Earmuffs = 4422,
            AngularY = 5775,
            VelocityX = 6015,
            VelocityY = 1208,
            VelocityZ = 7440,
            Upright = 4434,
            Grounded = 5912,
            Seated = 1173,
            InStation = 1247,
            IsLocal = 7648,
            IsOnFriendsList = 6893,
        }
        public enum ParameterType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            Trigger = 3
        }
        public enum AnimationLayer
        {
            FX = VRCAvatarDescriptor.AnimLayerType.FX,
            Action = VRCAvatarDescriptor.AnimLayerType.Action,
        }
        public enum OnOffEnum
        {
            On = 1,
            Off = 0
        }
    }
}
