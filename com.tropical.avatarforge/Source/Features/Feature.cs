using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public abstract class Feature
    {
        //Build info
        public GameObject gameObject;
        public int beginningOrder;
        public int buildOrder;
    }
}

