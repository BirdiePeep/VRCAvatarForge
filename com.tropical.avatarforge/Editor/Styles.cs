using UnityEngine;

namespace Tropical.AvatarForge
{
    public static class Styles
    {
        static bool hasInit = false;
        public static void Init()
        {
            if(hasInit)
                return;
            hasInit = true;

            iconBack = Resources.Load<Texture2D>("Icons/icon-back");
            iconActionBasic = Resources.Load<Texture2D>("Icons/icon-action-basic");

            contentBackButton = new GUIContent("Back", iconBack);
        }

        //Textures
        public static Texture2D iconBack;
        public static Texture2D iconActionBasic;

        //Content
        public static GUIContent contentBackButton;
    }
}

