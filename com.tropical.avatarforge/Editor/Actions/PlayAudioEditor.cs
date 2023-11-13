using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace Tropical.AvatarForge
{
    public class PlayAudioEditor : ActionEditor<PlayAudio>
    {
        public override void OnInspectorGUI()
        {
            var targetProp = target.FindPropertyRelative("target");
            EditorGUILayout.PropertyField(targetProp);

            var audioClip = target.FindPropertyRelative("audioClip");
            var volume = target.FindPropertyRelative("volume");
            var spatial = target.FindPropertyRelative("spatial");
            var near = target.FindPropertyRelative("near");
            var far = target.FindPropertyRelative("far");

            //Property
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(audioClip);
                EditorGUILayout.TextField(audioClip.objectReferenceValue != null ? $"{((AudioClip)audioClip.objectReferenceValue).length:N2}" : "", GUILayout.Width(64));
            }
            EditorGUILayout.EndHorizontal();
            volume.floatValue = EditorGUILayout.Slider("Volume", volume.floatValue, 0f, 1f);
            spatial.boolValue = EditorGUILayout.Toggle("Spatial", spatial.boolValue);
            EditorGUI.BeginDisabledGroup(!spatial.boolValue);
            {
                near.floatValue = EditorGUILayout.FloatField("Near", near.floatValue);
                far.floatValue = EditorGUILayout.FloatField("Far", far.floatValue);
            }
            EditorGUI.EndDisabledGroup();
        }

        public override void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnabled)
        {
            //Is anything defined?
            if(action.target == null)
                return;

            //Find the path
            string objPath = AvatarForge.FindPropertyPath(action.target);
            if(objPath == null)
                return;

            AddKeyframes(animation, objPath, isEnabled);
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public void AddKeyframes(AnimationClip animation, string objPath, bool isEnabled)
        {
            if(action.audioClip == null)
                return;

            //Find/Create child object
            var name = $"Audio_{action.audioClip.name}";
            var child = action.target.transform.Find(name)?.gameObject;
            if(child == null)
            {
                child = new GameObject(name);
                child.transform.SetParent(action.target.transform, false);
            }
            child.SetActive(false); //Disable

            //Find/Create component
            var audioSource = child.GetComponent<AudioSource>();
            if(audioSource == null)
                audioSource = child.AddComponent<AudioSource>();
            audioSource.clip = action.audioClip;
            audioSource.volume = 0f; //Audio 0 by default

            //Spatial
            var spatialComp = child.GetComponent<VRCSpatialAudioSource>();
            if(spatialComp == null)
                spatialComp = child.AddComponent<VRCSpatialAudioSource>();
            spatialComp.EnableSpatialization = action.spatial;
            spatialComp.Near = action.near;
            spatialComp.Far = action.far;

            //Create curve
            var subPath = $"{objPath}/{name}";
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, action.volume));
                animation.SetCurve(subPath, typeof(AudioSource), $"m_Volume", curve);
            }
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, 1f));
                animation.SetCurve(subPath, typeof(GameObject), $"m_IsActive", curve);
            }
        }
    }
}
