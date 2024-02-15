using UnityEngine;

namespace Tropical.AvatarForge
{
    public class SimpleFixes : Feature
    {
        [Tooltip("Grows bounding boxes of SkinnedMeshRenderers to occupy the maximum space of the avatar.  Also turns off UpdateWhileOffscreen to increase performance.")]
        public bool expandBoundingBoxes = false;

        [Tooltip("Forces all textures to be crunch compressed before uploading")]
        public bool forceTextureCompression = false;
    }
}

