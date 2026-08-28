using Xv2CoreLib.BSA;

namespace XenoKit.Engine.Scripting.BSA
{
    internal static class BsaHitboxGeometry
    {
        public static bool UsesDistanceRelativeGeometry(BSA_Type3 hitbox)
        {
            return hitbox != null && hitbox.I_00 == 1 && (hitbox.I_04 & 0x0001) != 0;
        }
    }
}
