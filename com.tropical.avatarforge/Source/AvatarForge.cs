using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    public partial class AvatarForge : MonoBehaviour, IEditorOnly, IPreprocessCallbackBehaviour
    {
        //Other
        public VRCAvatarDescriptor avatar
        {
            get { return gameObject.GetComponent<VRCAvatarDescriptor>(); }
        }

        //Features
        [SerializeReference] public List<Feature> features = new List<Feature>();

        //Build Options
        public bool mergeOriginalAnimators = false;

        [System.Serializable]
        public struct ParamDefault
        {
            public string name;
            public float value;
        }

        //Editor
        public bool foldoutBuildOptions = false;

        //Helper
        public static string GetSaveDirectory()
        {
            return System.IO.Path.GetDirectoryName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        public static GameObject FindPropertyObject(GameObject root, string path)
        {
            if(string.IsNullOrEmpty(path))
                return null;
            return root.transform.Find(path)?.gameObject;
        }
        public static string FindPropertyPath(GameObject obj)
        {
            string path = obj.name;
            while(true)
            {
                obj = obj.transform.parent?.gameObject;
                if(obj == null)
                    return "";
                if(obj.transform.parent == null) //Break on root obj
                    break;
                if(obj.GetComponent<VRCAvatarDescriptor>() != null) //Stop at the avatar descriptor
                    break;
                path = $"{obj.name}/{path}";
            }
            return path;
        }

        #region IPreprocessCallbackBehaviour
        public int PreprocessOrder => -1000;
        public static System.Func<AvatarForge, bool> OnPreprocessCallback;
        public bool OnPreprocess()
        {
            if(this == null)
                return true;

            if(OnPreprocessCallback != null)
                return OnPreprocessCallback.Invoke(this);
            return true;
        }
        #endregion
    }
}
