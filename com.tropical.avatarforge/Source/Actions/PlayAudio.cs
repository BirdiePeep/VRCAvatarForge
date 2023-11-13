using UnityEngine;

namespace Tropical.AvatarForge
{
    public class PlayAudio : Action
    {
        public GameObject target;
        public AudioClip audioClip;
        public bool spatial = true;
        public float volume = 1f;
        public float near = 6f;
        public float far = 20f;

        public override Action Clone()
        {
            var result = new PlayAudio();
            result.target = target;
            result.audioClip = audioClip;
            result.spatial = spatial;
            result.volume = volume;
            result.near = near;
            result.far = far;
            return result;
        }
    }
}