using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.View
{
    /// <summary>
    /// A simplified camera lacking any animation support. Can only be manually controlled.
    /// </summary>
    public class SimpleCamera : CameraBase
    {
        public SimpleCamera(GameBase gameInstance) : base(gameInstance)
        {

        }

        public override void Update()
        {
            ProcessCameraControl();
            RecalculateMatrices();
        }
    }
}
