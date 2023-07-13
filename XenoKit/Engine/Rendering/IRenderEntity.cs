namespace XenoKit.Engine.Rendering
{
    public interface IRenderEntity
    {
        DrawEntityType DrawEntityType { get; }
        bool DrawThisFrame { get; }
        int AlphaBlendType { get; }
    }

    public enum DrawEntityType
    {
        Actor,
        Stage,
        VFX
    }
}
