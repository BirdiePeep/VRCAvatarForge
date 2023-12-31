namespace Tropical.AvatarForge
{
    [System.Serializable]
    public abstract class Action
    {
        public abstract Action Clone();
    }

    public interface IMenuInitialize
    {
        public void Initialize();
    }
}