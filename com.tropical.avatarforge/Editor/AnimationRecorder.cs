using System;
using UnityEditor;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public static class AnimationRecorder
    {
        //State
        static AnimationRecorderProxy proxy;
        static Animator animator;
        static UnityEngine.Object[] prevFocus;

        //Reflection
        static Type stateType;
        static System.Reflection.PropertyInfo prop_recording;
        static object state;

        public static void DrawAnimationClip(string label, SerializedProperty property, GameObject target, string fileName)
        {
            EditorGUILayout.BeginHorizontal();
            {
                //Property
                EditorGUILayout.PropertyField(property, label != null ? new GUIContent(label) : GUIContent.none);

                //Recording
                if(!AnimationRecorder.IsRecording)
                {
                    if(GUILayout.Button("Record"))
                        AnimationRecorder.BeginRecording(property, target, fileName);
                }
                else
                {
                    if(GUILayout.Button("Stop Recording"))
                        AnimationRecorder.EndRecording();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        public static bool IsRecording { get { return proxy != null; } }
        public static void BeginRecording(SerializedProperty property, GameObject target, string fileName)
        {
            if(IsRecording)
                return;

            //Focus on target
            prevFocus = Selection.objects;
            Selection.activeObject = target;

            //Create clip
            AnimationClip clip = property.objectReferenceValue as AnimationClip;
            if(clip == null)
            {
                clip = new AnimationClip();
                clip.name = fileName;
                property.objectReferenceValue = clip;
                property.serializedObject.ApplyModifiedProperties();
                AvatarBuilder.SaveAsset(clip, AvatarForge.GetSaveDirectory(), "Animations");
            }

            //Begin
            Init(target, clip);
        }
        public static void EndRecording()
        {
            if(proxy != null)
            {
                Terminate();
                proxy = null;
            }
        }

        static void Init(GameObject target, AnimationClip clip)
        {
            //Find animator
            var animator = target.GetComponent<Animator>();
            if(animator == null)
            {
                Debug.LogError("Object doesn't have an Animator component");
                return;
            }

            //Create recording object
            var obj = new GameObject("Recording");
            obj.hideFlags = HideFlags.HideAndDontSave;
            proxy = obj.AddComponent<AnimationRecorderProxy>();
            AnimationRecorder.animator = animator;

            //Create temporary controller
            var controller = new UnityEditor.Animations.AnimatorController();
            controller.AddLayer("Base");
            controller.AddMotion(clip);
            animator.runtimeAnimatorController = controller;

            //Select ourselves
            Selection.objects = new UnityEngine.Object[] { target };

            //Open animation window and begin recording
            var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AnimationWindow");
            var window = EditorWindow.GetWindow(windowType);
            var prop_state = windowType.GetProperty("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            state = prop_state.GetValue(window);

            stateType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.AnimationWindowState");
            var prop_animationClip = stateType.GetProperty("activeAnimationClip");
            prop_animationClip.SetValue(state, clip);
            var recording = stateType.GetMethod("StartRecording");
            recording.Invoke(state, null);

            prop_recording = stateType.GetProperty("recording");

            EditorApplication.update += OnUpdate;
            Debug.Log("Begin Recording");
        }
        static void Terminate()
        {
            Debug.Log("End Recording");

            EditorApplication.update -= OnUpdate;

            //Close recording
            if(stateType != null)
            {
                var prop_animationClip = stateType.GetProperty("activeAnimationClip");
                prop_animationClip.SetValue(state, null);
                var recording = stateType.GetMethod("StopRecording");
                recording.Invoke(state, null);
            }

            //Reset
            if(animator != null)
                animator.runtimeAnimatorController = null;

            //Regain Focus
            Selection.objects = prevFocus;

            //Save
            AssetDatabase.SaveAssets();

            //Destroy
            GameObject.DestroyImmediate(proxy.gameObject);
            proxy = null;
        }
        static void OnUpdate()
        {
            var isRecording = prop_recording != null ? (bool)prop_recording.GetValue(state) : false;
            if(!isRecording)
                Terminate();
        }
    }
}