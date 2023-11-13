using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.IO;

namespace Tropical.AvatarForge
{
    public static class TextureBuilder
    {
        public enum BlendMode
        {
            Normal,
            Additive,
            Multiply,
        }
        public struct Layer
        {
            public Texture2D texture;
            public Texture2D mask;
            public Color color;
            public BlendMode blendMode;
        }

        public static Texture2D Build(Texture2D sourceTexture, List<Layer> layers, bool isNormalMap=false)
        {
            InitShaders();

            //Get source texture
            string texName = Path.GetRandomFileName();
            int width = 2024;
            int height = 2024;
            TextureWrapMode wrapMode = TextureWrapMode.Clamp;
            FilterMode filterMode = FilterMode.Bilinear;
            int mipCount = 0;
            if(sourceTexture != null)
            {
                texName = sourceTexture.name;
                width = sourceTexture.width;
                height = sourceTexture.height;
                wrapMode = sourceTexture.wrapMode;
                filterMode = sourceTexture.filterMode;
                mipCount = sourceTexture.mipmapCount;
            }

            //Create copy of texture
            var renderDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_SRGB, 0);
            renderDesc.useMipMap = false;
            renderDesc.mipCount = 0;
            renderDesc.sRGB = !isNormalMap;
            var renderTarget = RenderTexture.GetTemporary(renderDesc);
            Graphics.SetRenderTarget(renderTarget);
            if(sourceTexture != null)
            {
                BlitWithMask.SetTexture("_MainTex", sourceTexture);
                BlitWithMask.SetTexture("_MaskTex", null);
                BlitWithMask.SetFloat("_SrcMode", (float)UnityEngine.Rendering.BlendMode.One);
                BlitWithMask.SetFloat("_DstMode", (float)UnityEngine.Rendering.BlendMode.Zero);
                BlitWithMask.SetColor("_Color", Color.white);
                BlitTexture(sourceTexture, isNormalMap);
            }

            //Combine layers
            foreach(var layer in layers)
            {
                //Blit
                var layerTexture = layer.texture != null ? layer.texture : Texture2D.whiteTexture;
                BlitWithMask.SetTexture("_MainTex", layerTexture);
                BlitWithMask.SetTexture("_MaskTex", layer.mask != null ? layer.mask : Texture2D.whiteTexture);
                var blendMode = isNormalMap ? BlendMode.Normal : layer.blendMode;
                switch(blendMode)
                {
                    case BlendMode.Normal:
                        BlitWithMask.SetFloat("_SrcMode", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        BlitWithMask.SetFloat("_DstMode", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                    case BlendMode.Additive:
                        BlitWithMask.SetFloat("_SrcMode", (float)UnityEngine.Rendering.BlendMode.One);
                        BlitWithMask.SetFloat("_DstMode", (float)UnityEngine.Rendering.BlendMode.One);
                        break;
                    case BlendMode.Multiply:
                        BlitWithMask.SetFloat("_SrcMode", (float)UnityEngine.Rendering.BlendMode.DstColor);
                        BlitWithMask.SetFloat("_DstMode", (float)UnityEngine.Rendering.BlendMode.Zero);
                        break;
                }
                BlitWithMask.SetColor("_Color", new Color(layer.color.r, layer.color.g, layer.color.b, layer.color.a));
                BlitTexture(layerTexture, isNormalMap);
            }

            void BlitTexture(Texture2D tex, bool isNorm)
            {
                if(isNorm)
                {
                    BlitWithMask.SetInt("_IsNormalMap", 1);
                    Graphics.Blit(tex, renderTarget, BlitWithMask);
                }
                else
                {
                    BlitWithMask.SetInt("_IsNormalMap", 0);
                    Graphics.Blit(tex, renderTarget, BlitWithMask);
                }
            }

            //Read final texture
            var combined = new Texture2D(width, height, isNormalMap ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB, mipCount, TextureCreationFlags.None);
            combined.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            combined.Apply();

            //Cleanup
            RenderTexture.ReleaseTemporary(renderTarget);

            //Return
            return combined;
        }

        static Material BlitWithMask;
        static void InitShaders()
        {
            if(BlitWithMask == null)
            {
                BlitWithMask = new Material(Shader.Find("Hidden/EasyAvatarSetup/BlitWithMask"));
            }
        }
    }

}