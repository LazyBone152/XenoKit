using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Graphics
{
    public interface IEffect
    {
        GraphicsDevice GraphicsDevice { get; set; }
        EffectTechnique CurrentTechnique { get; set; }
        ConstantBuffer[] ConstantBuffers { get; set; }
        EffectParameterCollection Parameters { get; set; }
        void OnApply();
    }
}
