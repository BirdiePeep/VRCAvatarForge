using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.Components;
using System.Reflection;

namespace Tropical.AvatarForge
{
    public class EditorBase
    {
        public SerializedProperty target;
        public AvatarForgeEditor editor;
        public AvatarForge setup;

        public virtual void SetTarget(SerializedProperty target)
        {
            this.target = target;
        }

        public virtual void OnInspectorGUI()
        {
            InitStyles();
            Inspector_Header();
            Inspector_Body();
        }

        public virtual void Inspector_Header()
        {
        }
        public virtual void Inspector_Body()
        {
        }

        public void Repaint()
        {
            editor.Repaint();
        }

        public bool BeginCategory(string name, SerializedProperty toggle, bool isModified = false)
        {
            EditorGUILayout.BeginVertical();
            toggle.boolValue = EditorGUILayout.Foldout(toggle.boolValue, isModified ? $"{name}*" : name);
            EditorGUI.indentLevel += 1;
            return toggle.boolValue;
        }
        public void EndCategory()
        {
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        protected const float SmallButtonSize = 48f;
        protected const float SecondDropdownSize = 150f;

#region Parameters
        protected static string[] ParameterNames =
        {
            "[None]",
            "Stage1",
            "Stage2",
            "Stage3",
            "Stage4",
            "Stage5",
            "Stage6",
            "Stage7",
            "Stage8",
            "Stage9",
            "Stage10",
            "Stage11",
            "Stage12",
            "Stage13",
            "Stage14",
            "Stage15",
            "Stage16",
        };
        protected static List<string> popupCache = new List<string>();

        public static void DrawSimpleParameterDropDown(SerializedProperty parameter, string label, bool required, VRCAvatarDescriptor avatarDescriptor)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if(avatarDescriptor != null)
                {
                    //Text field
                    if(!string.IsNullOrEmpty(label))
                        EditorGUILayout.PrefixLabel(label);
                    parameter.stringValue = GUILayout.TextField(parameter.stringValue);

                    //Dropdown
                    int currentIndex;
                    if(string.IsNullOrEmpty(parameter.stringValue))
                    {
                        currentIndex = -1;
                    }
                    else
                    {
                        currentIndex = -2;
                        for(int i = 0; i < avatarDescriptor.GetExpressionParameterCount(); i++)
                        {
                            var item = avatarDescriptor.GetExpressionParameter(i);
                            if(item.name == parameter.stringValue)
                            {
                                currentIndex = i;
                                break;
                            }
                        }
                    }
                    EditorGUI.BeginDisabledGroup(avatarDescriptor.expressionParameters == null);
                    {
                        EditorGUI.BeginChangeCheck();
                        currentIndex = EditorGUILayout.Popup(currentIndex + 1, ParameterNames, GUILayout.MaxWidth(SecondDropdownSize));
                        if(EditorGUI.EndChangeCheck())
                        {
                            if(currentIndex == 0)
                                parameter.stringValue = "";
                            else
                                parameter.stringValue = avatarDescriptor.GetExpressionParameter(currentIndex - 1).name;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(0, new string[0]);
                    EditorGUI.EndDisabledGroup();
                }


            }
            EditorGUILayout.EndHorizontal();

            /*if(string.IsNullOrEmpty(parameter.stringValue) && required)
            {
                EditorGUILayout.HelpBox("Parameter required.", MessageType.Error);
            }*/
        }
        public static void DrawParameterDropDown(SerializedProperty parameter, string label, bool required, VRCAvatarDescriptor avatarDescriptor)
        {
            EditorGUILayout.BeginHorizontal();
            {
                //Text field
                EditorGUILayout.PrefixLabel(label + (!required ? " (Optional)" : ""));
                parameter.stringValue = GUILayout.TextField(parameter.stringValue);

                if (avatarDescriptor != null)
                {
                    //Dropdown
                    int currentIndex;
                    if (string.IsNullOrEmpty(parameter.stringValue))
                    {
                        currentIndex = -1;
                    }
                    else
                    {
                        currentIndex = -2;
                        for (int i = 0; i < avatarDescriptor.GetExpressionParameterCount(); i++)
                        {
                            var item = avatarDescriptor.GetExpressionParameter(i);
                            if (item.name == parameter.stringValue)
                            {
                                currentIndex = i;
                                break;
                            }
                        }
                    }
                    EditorGUI.BeginDisabledGroup(avatarDescriptor.expressionParameters == null);
                    {
                        EditorGUI.BeginChangeCheck();
                        currentIndex = EditorGUILayout.Popup(currentIndex + 1, ParameterNames, GUILayout.MaxWidth(SecondDropdownSize));
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (currentIndex == 0)
                                parameter.stringValue = "";
                            else
                                parameter.stringValue = avatarDescriptor.GetExpressionParameter(currentIndex - 1).name;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(0, new string[0], GUILayout.MaxWidth(SecondDropdownSize));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(parameter.stringValue) && required)
            {
                EditorGUILayout.HelpBox("Parameter required.", MessageType.Error);
            }
        }
        protected void DrawSimpleParameterDropDown(SerializedProperty parameter, string label, bool required = true)
        {
            DrawSimpleParameterDropDown(parameter, label, required, setup.avatar);
        }
        protected void DrawParameterDropDown(SerializedProperty parameter, string label, bool required=true)
        {
            DrawParameterDropDown(parameter, label, required, setup.avatar);
        }
        protected int GetExpressionParametersCount()
        {
            if (setup.avatar != null && setup.avatar.expressionParameters != null && setup.avatar.expressionParameters.parameters != null)
                return setup.avatar.expressionParameters.parameters.Length;
            return 0;
        }
        protected ExpressionParameters.Parameter GetExpressionParameter(int i)
        {
            if (setup.avatar != null)
                return setup.avatar.GetExpressionParameter(i);
            return null;
        }
        #endregion

#region Styles
        public static GUIStyle boxUnselected;
        public static GUIStyle boxSelected;
        public static GUIStyle boxDisabled;
        public static GUIStyle buttonEnabled;
        public static GUIContent upArrow;
        public static GUIContent downArrow;
        public static GUIContent gear;
        
        public static void InitStyles()
        {
            if(boxUnselected == null)
                boxUnselected = new GUIStyle(GUI.skin.box);

            if(boxSelected == null)
            {
                boxSelected = new GUIStyle(GUI.skin.box);
                boxSelected.normal.background = MakeTex(2, 2, new Color(0.0f, 0.5f, 1f, 0.5f));
            }

            if(boxDisabled == null)
            {
                boxDisabled = new GUIStyle(GUI.skin.box);
                boxDisabled.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.25f));
            }

            if(buttonEnabled == null)
            {
                buttonEnabled = new GUIStyle(GUI.skin.box);
                buttonEnabled.normal.background = MakeTex(2, 2, new Color(0f, 1f, 0f, 1f));
            }

            upArrow = new GUIContent(Resources.Load<Texture2D>("Icons/icon-arrow-up"));
            downArrow = new GUIContent(Resources.Load<Texture2D>("Icons/icon-arrow-down"));
            gear = EditorGUIUtility.IconContent("Settings");
        }
#endregion

#region Helper Methods
		public static void Divider()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static object GetManagedReferenceValue(SerializedProperty prop)
        {
            if(prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach(var element in elements)
            {
                if(element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
        private static object GetValue_Imp(object source, string name)
        {
            if(source == null)
                return null;
            var type = source.GetType();

            while(type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if(f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if(p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }
        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if(enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for(int i = 0; i <= index; i++)
            {
                if(!enm.MoveNext()) return null;
            }
            return enm.Current;
        }


        #endregion

        #region HelperMethods
        public static string Title(string name, bool isModified)
        {
            return name + (isModified ? "*" : "");
        }

        public static void DrawObjectReference(GameObject root, string name, SerializedProperty property)
        {
            var obj = AvatarForge.FindPropertyObject(root, property.stringValue);
            EditorGUI.BeginChangeCheck();
            obj = (GameObject)EditorGUILayout.ObjectField(name, obj, typeof(GameObject), true, null);
            if(EditorGUI.EndChangeCheck())
            {
                property.stringValue = obj != null ? AvatarForge.FindPropertyPath(obj) : "";
            }
        }

        public static void BeginPaddedArea(float padding)
        {
            if(padding > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(padding);
                EditorGUILayout.BeginVertical();
                GUILayout.Space(padding);
            }
            
        }
        public static void EndPaddedArea(float padding)
        {
            if(padding > 0)
            {
                GUILayout.Space(padding);
                EditorGUILayout.EndVertical();
                GUILayout.Space(padding);
                EditorGUILayout.EndVertical();
            }
        }

        public static bool FoldoutButton(bool foldout)
        {
            if(GUILayout.Button(foldout ? "-" : "+", GUILayout.Width(32)))
                foldout = !foldout;
            return foldout;
        }
        public void DrawAnimationReference(string label, SerializedProperty clip, string newAssetName)
        {
            AnimationRecorder.DrawAnimationClip(label, clip, setup.gameObject, newAssetName);
        }

        #endregion
    }
}