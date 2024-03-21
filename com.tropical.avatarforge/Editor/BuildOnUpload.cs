using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    //Build on upload
    [InitializeOnLoad]
    public class BuildOnUpload
    {
        //OnUpload
        static BuildOnUpload()
        {
            AvatarForge.OnPreprocessCallback += OnScriptPreprocess;
        }
        static bool OnScriptPreprocess(AvatarForge script)
        {
            var avatar = script.gameObject.GetComponent<VRCAvatarDescriptor>();
            if(avatar != null)
                return AvatarBuilder.BuildAvatarDestructive(avatar, script);
            return true;
        }

        //OnPlayMode
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnPlayModeChanged()
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
}