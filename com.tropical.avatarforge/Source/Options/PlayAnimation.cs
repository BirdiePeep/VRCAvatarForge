using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class PlayAnimation : ActionOption, IMenuInitialize
    {
        /*public enum State
        {
            Enter,
            Exit,
        }*/

        [System.Serializable]
        public struct Animation
        {
            public Animation(AnimationClip clip, Globals.AnimationLayer layer)
            {
                this.clip = clip;
                this.layer = layer;
            }

            public AnimationClip clip;
            public Globals.AnimationLayer layer;
            //public State state;
        }

        public Animation[] animations;

        public override ActionOption Clone()
        {
            var result = new PlayAnimation();
            result.animations = (Animation[])animations.Clone();
            return result;
        }

        public void Initialize()
        {
            animations = new Animation[1];
            animations[0] = new Animation(null, Globals.AnimationLayer.FX);
        }
    }
}