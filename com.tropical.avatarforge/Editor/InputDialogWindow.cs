using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class InputDialogWindow : EditorWindow
    {
        public string message;
        public string input;
        public string okay;
        public string cancel;

        public void OnGUI()
        {
            maxSize = new Vector2(400, 200);
            minSize = new Vector2(400, 200);

            GUILayout.Label(message);
            input = EditorGUILayout.TextField(input);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button(okay))
            {
                Close();
            }
            if(GUILayout.Button(cancel))
            {
                input = null;
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }

        public static string ShowDialog(string title, string message, string input = "")
        {
            var window = new InputDialogWindow();
            window.titleContent = new GUIContent(title);
            window.message = message;
            window.input = input;
            window.okay = "Okay";
            window.cancel = "Cancel";
            window.ShowModal();
            window.position = new Rect(Input.mousePosition, Vector2.zero);
            return input;
        }
    }
}
