using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tropical.AvatarForge
{
    public class MaterialVariationEditor : ActionEditor<MaterialVariation>
    {
        bool isPreviewMode = false;

        ReorderablePropertyList channelsList = new ReorderablePropertyList("Textures", foldout: false, addName: "Texture");
        ReorderablePropertyList layersList = new ReorderablePropertyList("Layers", foldout: false, addName: "Layer");
        public override void OnInspectorGUI()
        {
            var inputMaterial = target.FindPropertyRelative("material");

            //Material
            EditorGUILayout.PropertyField(inputMaterial);

            //Preview
            if(GUILayout.Button(isPreviewMode ? "Exit Preview" : "Preview"))
            {
                isPreviewMode = !isPreviewMode;
                SetPreview(setup.gameObject, isPreviewMode);
            }

            //Texture Targets
            var material = inputMaterial.objectReferenceValue as Material;
            EditorGUI.BeginDisabledGroup(inputMaterial.objectReferenceValue == null);
            {
                //Layers
                layersList.list = target.FindPropertyRelative("layers");
                layersList.OnElementHeader = (index, element) =>
                {
                    var name = element.FindPropertyRelative("name");

                    EditorGUIUtility.labelWidth = 48;

                    GUILayout.Space(16);
                    element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(name.stringValue));
                    name.stringValue = EditorGUILayout.TextField(name.stringValue);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("opacity"), GUIContent.none);

                    EditorGUIUtility.labelWidth = 0;

                    return element.isExpanded;
                };
                layersList.OnElementBody = (index, element) =>
                {
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("mask"));
                    DrawChannels(element.FindPropertyRelative("channels"));
                };
                layersList.OnAdd = (element) =>
                {
                    element.isExpanded = true;
                    element.FindPropertyRelative("name").stringValue = "New Layer";
                    element.FindPropertyRelative("opacity").floatValue = 1;
                };
                layersList.OnInspectorGUI();
            }
            EditorGUI.EndDisabledGroup();
        }
        void DrawChannels(SerializedProperty channels)
        {
            channelsList.list = channels;
            channelsList.OnElementHeader = (index, element) =>
            {
                //Config
                var name = element.FindPropertyRelative("name");
                if(GUILayout.Button(gear, GUILayout.Width(24)))
                {
                    //Find all available textures
                    var options = new List<AddTexturePopup.Option>();
                    var material = target.FindPropertyRelative("material").objectReferenceValue as Material;
                    for(int i = 0; i < material.shader.GetPropertyCount(); i++)
                    {
                        var propertyName = material.shader.GetPropertyName(i);
                        var propertyType = material.shader.GetPropertyType(i);
                        if(propertyType == UnityEngine.Rendering.ShaderPropertyType.Texture)
                        {
                            var tex = material.GetTexture(propertyName);
                            options.Add(new AddTexturePopup.Option(propertyName, null, tex));
                        }
                    }

                    //Init popup
                    var popup = new AddTexturePopup();
                    popup.size = new Vector2(300, 400);
                    popup.options = options.ToArray();
                    popup.onConfirm = (selectedName, obj) =>
                    {
                        name.stringValue = selectedName;
                        name.serializedObject.ApplyModifiedProperties();
                    };
                    popup.Show();
                }

                //Name
                GUILayout.Label(name.stringValue, GUILayout.Width(128));

                //Texture
                EditorGUILayout.PropertyField(element.FindPropertyRelative("texture"), GUIContent.none);

                //Color
                EditorGUILayout.PropertyField(element.FindPropertyRelative("color"), GUIContent.none, GUILayout.Width(64));

                //Alpha
                //var alpha = element.FindPropertyRelative("alpha");
                //alpha.floatValue = GUILayout.HorizontalSlider(alpha.floatValue, 0f, 1f, GUILayout.Width(64));
                //alpha.floatValue = EditorGUILayout.FloatField(alpha.floatValue, GUILayout.Width(40));

                //Blend
                EditorGUILayout.PropertyField(element.FindPropertyRelative("blendMode"), GUIContent.none, GUILayout.Width(80));

                return true;
            };
            channelsList.OnAdd = (element) =>
            {
                element.FindPropertyRelative("name").stringValue = "New Channel";
                element.FindPropertyRelative("color").colorValue = Color.white;
            };
            channelsList.OnInspectorGUI();
        }

        //Preview
        public void SetPreview(GameObject root, bool enabled)
        {
            if(enabled)
            {
                BuildMaterial(false);
                ReplaceMaterials(root, action.material, action.outputMaterial);
            }
            else
            {
                ReplaceMaterials(root, action.outputMaterial, action.material);
            }
        }
        public void ReplaceMaterials(GameObject root, Material source, Material dest)
        {
            if(source == null || dest == null)
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                bool wasModified = false;
                var sharedMaterials = renderer.sharedMaterials;
                for(int i = 0; i < sharedMaterials.Length; i++)
                {
                    if(sharedMaterials[i] == source)
                    {
                        sharedMaterials[i] = dest;
                        wasModified = true;
                    }
                }
                if(wasModified)
                    renderer.sharedMaterials = sharedMaterials;
            }
        }

        //Build
        public override void Apply(AnimationClip animation, Globals.AnimationLayer layer, bool isEnter)
        {
            if(isEnter)
            {
                //Build materials
                BuildMaterial(true);

                //Add Keyframes
                var renderers = AvatarBuilder.AvatarSetup.GetComponentsInChildren<Renderer>(true);
                foreach(var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for(int i = 0; i < materials.Length; i++)
                    {
                        if(materials[i] == action.material)
                        {
                            string objPath = AvatarForge.FindPropertyPath(renderer.gameObject);
                            MaterialSwapEditor.AddKeyframe(animation, action.outputMaterial, objPath, i);
                        }
                    }
                }
            }
        }
        public override bool AffectsLayer(Globals.AnimationLayer layerType)
        {
            return layerType == Globals.AnimationLayer.FX;
        }
        public void BuildMaterial(bool saveAssets)
        {
            if(action.material == null)
                return;

            //Copy material data
            if(action.outputMaterial != null)
                action.outputMaterial.CopyPropertiesFromMaterial(action.material);
            else
                action.outputMaterial = new Material(action.material);

            //Setup texture builder
            var layersLookup = new Dictionary<string, List<TextureBuilder.Layer>>();
            foreach(var layer in action.layers)
            {
                //Build channels
                foreach(var channel in layer.channels)
                {
                    //Find blit layers
                    List<TextureBuilder.Layer> blitLayers = null;
                    if(!layersLookup.TryGetValue(channel.name, out blitLayers))
                    {
                        blitLayers = new List<TextureBuilder.Layer>();
                        layersLookup.Add(channel.name, blitLayers);
                    }

                    //Create layer
                    var blitLayer = new TextureBuilder.Layer();
                    blitLayer.texture = channel.texture;
                    blitLayer.mask = layer.mask;
                    blitLayer.color = channel.color * new Color(1f, 1f, 1f, layer.opacity);
                    switch(channel.blendMode)
                    {
                        case MaterialVariation.BlendMode.Additive:
                            blitLayer.blendMode = TextureBuilder.BlendMode.Additive;
                            break;
                        case MaterialVariation.BlendMode.Multiply:
                            blitLayer.blendMode = TextureBuilder.BlendMode.Multiply;
                            break;
                        default:
                            blitLayer.blendMode = TextureBuilder.BlendMode.Normal;
                            break;
                    }
                    blitLayers.Add(blitLayer);
                }
            }

            //Build
            foreach(var item in layersLookup)
            {
                //Reverse layers, so we paint from the bottom up
                item.Value.Reverse();

                //Build texture
                var sourceTexture = action.material.GetTexture(item.Key) as Texture2D;
                var index = action.material.shader.FindPropertyIndex(item.Key);
                bool isNormalMap = action.material.shader.GetPropertyTextureDefaultName(index) == "bump";
                var result = TextureBuilder.Build(sourceTexture, item.Value, isNormalMap);

                //Save
                if(saveAssets)
                {
                    string texName = Path.GetRandomFileName();
                    TextureWrapMode wrapMode = TextureWrapMode.Repeat;
                    FilterMode filterMode = FilterMode.Bilinear;
                    int mipCount = 1;
                    int maxSize = 2048;
                    if(sourceTexture != null)
                    {
                        texName = sourceTexture.name;
                        wrapMode = sourceTexture.wrapMode;
                        filterMode = sourceTexture.filterMode;
                        mipCount = sourceTexture.mipmapCount;
                        maxSize = sourceTexture.width;
                    }

                    var path = $"{AvatarForge.GetSaveDirectory()}/Generated/{texName}.png";
                    var bytes = result.EncodeToPNG();
                    Texture.DestroyImmediate(result);

                    AssetDatabase.DeleteAsset(path);
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                    //Load texture
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    action.outputMaterial.SetTexture(item.Key, texture);

                    //Set texture settings
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.sRGBTexture = !isNormalMap;
                    importer.wrapMode = wrapMode;
                    importer.filterMode = filterMode;
                    importer.mipmapEnabled = mipCount > 1;
                    importer.maxTextureSize = maxSize;
                    //importer.textureCompression = TextureImporterCompression.Compressed;
                    //importer.compressionQuality = 100;
                    //importer.crunchedCompression = true;
                    importer.alphaIsTransparency = !isNormalMap;
                    importer.SaveAndReimport();
                }
                else
                {
                    action.outputMaterial.SetTexture(item.Key, result);
                }
            }

            //Save material
            if(saveAssets)
                AvatarBuilder.SaveAsset(action.outputMaterial, AvatarForge.GetSaveDirectory(), "Generated");
        }
    }
}