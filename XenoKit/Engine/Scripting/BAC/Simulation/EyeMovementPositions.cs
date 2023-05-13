using LB_Common.Numbers;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public static class EyeMovementPositions
    {
        public static CustomVector4[] EyePositions = new CustomVector4[]
        {
            new CustomVector4(0.1f, -0.1f, 0, 0), //Left, Up
            new CustomVector4(0, -0.1f, 0, 0), //Up
            new CustomVector4(-0.1f, -0.1f, 0, 0), //Right, Up
            new CustomVector4(0.1f, 0, 0, 0), //Left
            new CustomVector4(0, 0, 0, 0), //Default position
            new CustomVector4(-0.1f, 0, 0, 0), //Right
            new CustomVector4(0.1f, 0.1f, 0, 0), //Left, Down
            new CustomVector4(0, 0.1f, 0, 0), //Down
            new CustomVector4(-0.1f, 0.1f, 0, 0), //Right, Down
        };
    }
}
