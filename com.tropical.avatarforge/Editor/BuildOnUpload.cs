using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Tropical.AvatarForge
{
    //Build on upload
    public class BuildOnUpload : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -1000;
        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            var desc = avatarGameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            var setup = avatarGameObject.GetComponent<AvatarForge>();
            if(setup != null)
                AvatarBuilder.BuildAvatarDestructive(desc, setup);
            return true;
        }
    }

    //Build on playmode
    [InitializeOnLoad]
    public static class PlayModeBuild
    {
        static PlayModeBuild()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AvatarForge.OnPreprocessCallback += OnScriptPreprocess;
        }
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredPlayMode)
            {
                var avatar = GameObject.FindObjectOfType<VRCAvatarDescriptor>();
                if(avatar != null)
                {
                    var setup = avatar.GetComponent<AvatarForge>();
                    if(setup != null)
                        AvatarBuilder.BuildAvatarDestructive(avatar, setup);
                }
            }
        }
        static bool OnScriptPreprocess(AvatarForge script)
        {
            var avatar = script.gameObject.GetComponent<VRCAvatarDescriptor>();
            if(avatar != null)
                return AvatarBuilder.BuildAvatarDestructive(avatar, script);
            return true;
        }
    }
}