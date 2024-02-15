using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    [InitializeOnLoad]
    public class StartupProcess
    {
        static StartupProcess()
        {
            if(!EditorPrefs.GetBool(AvatarSplashScreenWindow.ShowStartupOption))
                EditorApplication.update += Startup;
        }

        [MenuItem("Avatar Forge/Splash Screen")]
        static void Startup()
        {
            EditorApplication.update -= Startup;
            if(Application.isPlaying)
                return;

            //Search for startup window
            var items = AssetDatabase.FindAssets("t:SplashScreen");
            if(items == null || items.Length == 0)
                return;

            SplashScreen data = null;
            foreach(var item in items)
            {
                data = AssetDatabase.LoadAssetAtPath<SplashScreen>(AssetDatabase.GUIDToAssetPath(items[0]));
                if(data == null)
                    continue;

                //Open window
                var window = (AvatarSplashScreenWindow)EditorWindow.GetWindow(typeof(AvatarSplashScreenWindow), true, "");
                window.data = data;

                //Center window
                Rect main = EditorGUIUtility.GetMainWindowPosition();
                Rect pos = new Rect(0, 0, 300, 600);
                float centerWidth = (main.width - pos.width) * 0.5f;
                float centerHeight = (main.height - pos.height) * 0.5f;
                pos.x = main.x + centerWidth;
                pos.y = main.y + centerHeight;
                window.position = pos;

                window.Show();
                window.Focus();

                //Only show one for now
                break;
            }
        }
    }

    public class AvatarSplashScreenWindow : EditorWindow
    {
        public const string ShowStartupOption = "AvatarForge_ShowStartup";

        public SplashScreen data;
        Vector2 scrollPos;
        bool hasRun = false;

        void OnGUI()
        {
            if(!hasRun)
            {
                hasRun = true;
                titleContent = new GUIContent($"{data.title} - Splash Screen");
            }

            //Logo
            if(data.logo != null)
            {
                BeginCenter();
                GUILayout.Box(data.logo, GUILayout.Width(256), GUILayout.Height(256));
                EndCenter();
            }

            //Startup Option
            BeginCenter();
            EditorGUI.BeginChangeCheck();
            bool showStartup = EditorPrefs.GetBool(ShowStartupOption);
            showStartup = !EditorGUILayout.Toggle("Show Window At Startup", !showStartup);
            if(EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(ShowStartupOption, showStartup);
            }
            EndCenter();

            //Links
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if(data.links != null)
            {
                foreach(var link in data.links)
                {
                    if(GUILayout.Button(link.name, GUILayout.Height(32)))
                    {
                        //URL
                        if(!string.IsNullOrEmpty(link.url))
                        {
                            Application.OpenURL(link.url);
                        }

                        //Asset
                        if(link.asset != null)
                        {
                            AssetDatabase.OpenAsset(link.asset);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void BeginCenter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }
        void EndCenter()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
