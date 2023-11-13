using UnityEngine;
using UnityEditor;

namespace Tropical.AvatarForge
{
    public class MaterialSwapEditor : ActionEditor<MaterialSwap>
    {
        public override void OnInspectorGUI()
        {
            var targetProp = target.FindPropertyRelative("target");
            EditorGUILayout.PropertyField(targetProp);

            var materials = target.FindPropertyRelative("materials");

            //Get object materials
            Renderer renderer = (Renderer)targetProp.objectReferenceValue;
            if(renderer == null)
                return;

            //Materials
            var meshMaterials = renderer.sharedMaterials;
            if(meshMaterials != null)
            {
                //Create/Resize
                if(materials.arraySize != meshMaterials.Length)
                    materials.arraySize = meshMaterials.Length;

                //Materials
                for(int i = 0; i < materials.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"Material {i}", GUILayout.MaxWidth(120));
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(meshMaterials[i], typeof(Material), false);
                        EditorGUI.EndDisabledGroup();

                        var materialRef = materials.GetArrayElementAtIndex(i);
                        materialRef.objectReferenceValue = EditorGUILayout.ObjectField(materialRef.objectReferenceValue, typeof(Material), false);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        public override void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnabled)
        {
            if(action.target == null)
                return;
            string path = AvatarForge.FindPropertyPath(action.target.gameObject);
            if(path == null)
                return;
            AddKeyframes(animation, path, isEnabled);
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public void AddKeyframes(AnimationClip animation, string objPath, bool isEnabled)
        {
            for(int i = 0; i < action.materials.Length; i++)
            {
                AddKeyframe(animation, action.materials[i], objPath, i);
            }
        }
        public static void AddKeyframe(AnimationClip animation, Material material, string objPath, int materialIndex)
        {
            if(material == null || string.IsNullOrEmpty(objPath))
                return;

            //Create curve
            var keyframes = new ObjectReferenceKeyframe[1];
            var keyframe = new ObjectReferenceKeyframe();
            keyframe.time = 0;
            keyframe.value = material;
            keyframes[0] = keyframe;
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(objPath, typeof(Renderer), $"m_Materials.Array.data[{materialIndex}]");
            AnimationUtility.SetObjectReferenceCurve(animation, binding, keyframes);
        }
    }
}
