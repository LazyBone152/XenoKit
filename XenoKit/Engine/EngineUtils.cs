﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;

namespace XenoKit.Engine
{
    public static class EngineUtils
    {

        //Mouse Picking
        public static Ray CalculateRay(Vector2 mouseLocation, GameBase gameBase)
        {
            mouseLocation.X = gameBase.GraphicsDevice.Viewport.Width - mouseLocation.X;

            Vector3 nearPoint = gameBase.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.0f),
                    gameBase.ActiveCameraBase.ProjectionMatrix,
                    gameBase.ActiveCameraBase.ViewMatrix,
                    Matrix.Identity);

            Vector3 farPoint = gameBase.GraphicsDevice.Viewport.Unproject(new Vector3(mouseLocation.X,
                    mouseLocation.Y, 0.5f),
                    gameBase.ActiveCameraBase.ProjectionMatrix,
                    gameBase.ActiveCameraBase.ViewMatrix,
                    Matrix.Identity);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            //Log.Add($"Near: {nearPoint}, Far: {farPoint}", LogType.Info);

            return new Ray(nearPoint, direction);
        }

        public static float? IntersectDistance(BoundingSphere sphere, Vector2 mouseLocation, GameBase gameBase)
        {
            Ray mouseRay = CalculateRay(mouseLocation, gameBase);
            return mouseRay.Intersects(sphere);
        }

        public static float? IntersectDistance(BoundingBox box, Vector2 mouseLocation, GameBase gameBase)
        {
            Ray mouseRay = CalculateRay(mouseLocation, gameBase);
            return mouseRay.Intersects(box);
        }

    }
}
