using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Tropical.AvatarForge
{
    public class SimpleFixesEditor : FeatureEditor<SimpleFixes>
    {
        public override void PreBuild()
        {
            //Nothing
        }
        public override void Build()
        {
            if(feature.expandBoundingBoxes)
                ExpandBoundingBoxes();
            if(feature.forceTextureCompression)
                ForceTextureCompression();
        }
        public override void PostBuild()
        {
            //Nothing
        }
        public override string helpURL { get => null; }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(target.FindPropertyRelative("expandBoundingBoxes"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("forceTextureCompression"));
        }

        void ExpandBoundingBoxes()
        {
            //Find all skinned mesh renderers
            Bounds maxBounds = new Bounds();
            var renderers = AvatarBuilder.AvatarDescriptor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var renderer in renderers)
            {
                maxBounds.Encapsulate(renderer.bounds);
            }

            //Update bounds
            foreach(var renderer in renderers)
            {
                if(renderer.rootBone == null)
                {
                    Debug.LogError($"ExpandBoundingBoxes, SkinnedMeshRenderer for '{renderer.gameObject.name}' has no root bone");
                    continue;
                }

                var center = renderer.rootBone.InverseTransformPoint(maxBounds.center);

                var extents = maxBounds.extents;
                var axisX = renderer.rootBone.InverseTransformVector(new Vector3(extents.x, 0f, 0f));
                var axisY = renderer.rootBone.InverseTransformVector(new Vector3(0f, extents.y, 0f));
                var axisZ = renderer.rootBone.InverseTransformVector(new Vector3(0f, 0f, extents.z));

                extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
                extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
                extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

                renderer.localBounds = new Bounds(center, extents * 2f);
                renderer.updateWhenOffscreen = false;
            }
        }
        void ForceTextureCompression()
        {
            //Find all textures
            List<Texture> textures = new List<Texture>();
            var renderers = AvatarBuilder.AvatarDescriptor.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                foreach(var material in materials)
                {
                    if(material == null || material.shader == null)
                        continue;
                    int count = material.shader.GetPropertyCount();
                    for(int i = 0; i < count; i++)
                    {
                        var type = material.shader.GetPropertyType(i);
                        if(type == UnityEngine.Rendering.ShaderPropertyType.Texture)
                        {
                            var texture = material.GetTexture(material.shader.GetPropertyNameId(i));
                            if(texture != null)
                                textures.Add(texture);
                        }
                    }
                }
            }

            //Force compression
            int forced = 0;
            foreach(var texture in textures)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer == null)
                    continue;
                if(importer.textureCompression == TextureImporterCompression.Uncompressed || importer.crunchedCompression == false)
                {
                    forced += 1;
                    importer.crunchedCompression = true;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.compressionQuality = 100;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            if(forced > 0)
                Debug.Log($"SimpleFixes, ForceTextureCompression: Force compression on {forced} textures");
        }
    }
}
