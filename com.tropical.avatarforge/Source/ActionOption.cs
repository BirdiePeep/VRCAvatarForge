namespace Tropical.AvatarForge
{
    [System.Serializable]
    public abstract class ActionOption
    {
        public abstract ActionOption Clone();
    }

    public interface IMenuInitialize
    {
        public void Initialize();
    }
}