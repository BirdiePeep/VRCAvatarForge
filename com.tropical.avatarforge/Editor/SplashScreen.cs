using UnityEngine;

namespace Tropical.AvatarForge
{
    [CreateAssetMenu(menuName = "Avatar Forge/Splash Screen")]
    public class SplashScreen : ScriptableObject
    {
        public string title;
        public Texture2D logo;

        [System.Serializable]
        public struct Link
        {
            public string name;
            public string url;
            public Object asset;
        }
        public Link[] links;
    }
}