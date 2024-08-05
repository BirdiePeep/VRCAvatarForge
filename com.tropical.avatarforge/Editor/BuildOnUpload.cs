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
                return AvatarBuilder.BuildAvatarDestructive(avatar);
            return true;
        }

        //OnPlayMode
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnPlayModeChanged()
        {
            var avatars = GameObject.FindObjectsByType<VRCAvatarDescriptor>(FindObjectsSortMode.None);
            foreach(var avatar in avatars)
            {
                AvatarBuilder.BuildAvatarDestructive(avatar);
            }
        }
    }
}