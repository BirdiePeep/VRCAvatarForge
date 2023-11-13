using UnityEngine;

namespace Tropical.AvatarForge
{
    [System.Serializable]
    public class MaterialVariation : Action
    {
        public string toggle;
        public Material material;
        public Material outputMaterial;
        public Layer[] layers;

        public enum BlendMode
        {
            Normal,
            Additive,
            Multiply,
        }
        [System.Serializable]
        public struct Channel
        {
            public string name;
            public Texture2D texture;
            public Color color;
            public BlendMode blendMode;
        }
        [System.Serializable]
        public struct Layer
        {
            public string name;
            public Texture2D mask;
            [Range(0, 1)]public float opacity;
            public Channel[] channels;
        }

        //Action
        public override Action Clone()
        {
            var clone = new MaterialVariation();
            clone.toggle = toggle;
            clone.material = material;
            clone.outputMaterial = outputMaterial;
            clone.layers = (Layer[])layers.Clone();
            return clone;
        }
    }
}