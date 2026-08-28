using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacHitboxVisual : HitboxVisual
    {
        public BacHitboxVisual(Color fillColor, Color wireColor)
            : base(fillColor, wireColor)
        {
        }

        public void Update(BAC_Type1 hitbox, float cbsScaling)
        {
            Clear();

            if (hitbox == null)
                return;

            Vector3 position = new Vector3(hitbox.PositionX, hitbox.PositionY, hitbox.PositionZ);

            switch ((int)hitbox.BoundingBoxType)
            {
                case 0:
                    SetSphere(position, Math.Abs(hitbox.Size * cbsScaling));
                    break;
                case 1:
                    Vector3 start = new Vector3(
                        hitbox.PositionX + hitbox.MinX * cbsScaling,
                        hitbox.PositionY + hitbox.MinY * cbsScaling,
                        hitbox.PositionZ + hitbox.MinZ * cbsScaling);
                    Vector3 end = new Vector3(
                        hitbox.PositionX + hitbox.MaxX * cbsScaling,
                        hitbox.PositionY + hitbox.MaxY * cbsScaling,
                        hitbox.PositionZ + hitbox.MaxZ * cbsScaling);
                    SetCapsule(start, end, Math.Abs(hitbox.Size * cbsScaling));
                    break;
                case 2:
                    Vector3 halfExtents = new Vector3(
                        hitbox.Size,
                        hitbox.MinX,
                        hitbox.MinY) * cbsScaling;
                    SetBox(position, halfExtents);
                    break;
            }
        }

    }
}
