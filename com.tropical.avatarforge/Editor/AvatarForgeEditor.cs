﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace Tropical.AvatarForge
{
    public interface ISubActionsProvider
    {
        void GetSubActions(List<AvatarForge> actions);
    }

    [CustomEditor(typeof(AvatarForge))]
    public class AvatarForgeEditor : Editor
    {
        AvatarForge setup;

        //Styles
        public static Color HeaderColor = new Color(46f / 255f, 101f / 255f, 112f / 255f);
        public static Color SubHeaderColor = new Color(46f / 255f, 101f / 255f, 112f / 255f, 0.5f);
        static int HEADER_HEIGHT = 24;
        public static GUIStyle styleHeader;
        public static GUIStyle centerLabel;
        public static GUIContent helpButton;
        static bool initStyles = false;
        void InitStyles()
        {
            if(initStyles)
                return;
            initStyles = true;

            //Header
            styleHeader = new GUIStyle(GUI.skin.label);
            styleHeader.fontSize = HEADER_HEIGHT;
            styleHeader.fixedHeight = HEADER_HEIGHT;
            styleHeader.stretchHeight = false;

            //Center Label
            centerLabel = GUI.skin.GetStyle("Label");
            centerLabel.alignment = TextAnchor.UpperCenter;

            helpButton = EditorGUIUtility.IconContent("_Help");
        }

        public void Awake()
        {
            setup = target as AvatarForge;
        }
        public override void OnInspectorGUI()
        {
            setup = target as AvatarForge;
            var avatar = setup.avatar;

            InitStyles();

            var rootComponent = PrefabUtility.GetCorrespondingObjectFromOriginalSource(setup);
            if(rootComponent != null)
            {
                EditorGUILayout.HelpBox("You are viewing a prefab varient for this component, please modify the original component", MessageType.Warning);
                if(GUILayout.Button("Open Original Prefab"))
                {
                    var path = AssetDatabase.GetAssetPath(rootComponent);
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)));
                }
            }
            EditorGUI.BeginDisabledGroup(rootComponent != null);
            EditorGUI.BeginChangeCheck();
            {
                //Features
                DrawFeatures();

                //Build Options & Data
                if(avatar != null)
                {
                    EditorBase.Divider();

                    if(BeginCategory("Build Options", ref setup.foldoutBuildOptions))
                    {
                        DrawBuildOptions();
                    }
                    EndCategory();

                    //Build
                    /*EditorGUI.BeginDisabledGroup(EasyAvatarSetup.ReturnSavePath() == null || script.avatar == null);
                    if(GUILayout.Button("Build Avatar Copy", GUILayout.Height(32)))
                    {
                        AvatarBuilder.BuildAvatarCopy(script.avatar, script, " (Generated)");
                    }
                    EditorGUI.EndDisabledGroup();*/
                }
            }
            if(EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
        }

        bool BeginCategory(string title, ref bool foldout)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foldout = EditorGUILayout.Foldout(foldout, title);
            EditorGUI.indentLevel += 1;
            return foldout;
        }
        void EndCategory()
        {
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        //Features
        ReorderablePropertyList featureList = new ReorderablePropertyList(null, foldout: false, addName:"Feature");
        void DrawFeatures()
        {
            FeatureEditorBase.InitEditors();

            var featuresProp = serializedObject.FindProperty("features");

            featureList.list = featuresProp;
            featureList.showHeader = true;
            featureList.headerColor = HeaderColor;
            featureList.OnElementHeader = (index, element) =>
            {
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);
                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor == null)
                {
                    return false;
                }
                else
                {
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(editor.displayName));
                }

                //Help
                /*if(GUILayout.Button(helpButton, GUILayout.Width(32)))
                {
                    EditorUtility.DisplayDialog("Help", "Help URLs not yet implemented", "Ok");
                    //Application.OpenURL(feature.helpURL);
                }*/

                return element.isExpanded;
            };
            featureList.OnElementBody = (index, element) =>
            {
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);

                var editor = FeatureEditorBase.FindEditor(feature);
                if(editor != null)
                {
                    editor.editor = this;
                    editor.setup = setup;
                    editor.SetTarget(element);
                    editor.OnInspectorGUI();
                }
            };
            featureList.OnDelete = (index, element) =>
            {
                //Check if we want to delete objects
                var feature = (Feature)ActionsEditor.GetManagedReferenceValue(element);
                if(feature != null)
                {
                    if(!EditorUtility.DisplayDialog("Remove Feature?", $"Are you sure you want to remove the '{feature.GetType().Name}' feature?", "Yes", "No"))
                        return false;
                }
                return true;
            };
            featureList.OnPreAdd = (list) =>
            {
                var menu = new GenericMenu();
                for(int i = 0; i < FeatureEditorBase.editorTypes.Count; i++)
                    menu.AddItem(new GUIContent(FeatureEditorBase.editorNames[i]), false, OnAdd, FeatureEditorBase.editorTypes[i]);
                void OnAdd(object obj)
                {
                    var type = (System.Type)obj;
                    list.arraySize += 1;
                    var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    element.isExpanded = true;
                    element.managedReferenceValue = System.Activator.CreateInstance(type);
                    list.serializedObject.ApplyModifiedProperties();
                }
                menu.ShowAsContext();

                return null;
            };
            featureList.OnInspectorGUI();
        }
        void DrawBuildOptions()
        {
            //Options
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mergeOriginalAnimators"));
        }
    }
}