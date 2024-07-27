
namespace Tropical.AvatarForge
{
    [System.Serializable]
    public abstract class Feature
    {
        public int beginningOrder = 0;
        public virtual int BuildPriority => 0;
    }
}

