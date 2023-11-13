using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class PlayAnimation : Action
    {
        public enum State
        {
            Enter,
            Exit,
        }

        [System.Serializable]
        public struct Animation
        {
            public AnimationClip clip;
            public Globals.AnimationLayer layer;
            public State state;
        }

        public Animation[] animations;

        public override Action Clone()
        {
            var result = new PlayAnimation();
            result.animations = animations;
            return result;
        }
        
    }
}