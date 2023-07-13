using Microsoft.Xna.Framework.Graphics;
using System;
using Xv2CoreLib.EMM;

namespace XenoKit.Engine.Shader
{
    public class CharaShaderEffect : Xv2ShaderEffect
    {
        private CharaShaderType Type;

        public CharaShaderEffect(EmmMaterial material, CharaShaderType type, GameBase game) : base(material, true, game)
        {
            Type = type;
            InitTechnique();
        }

        public void SetType(CharaShaderType type)
        {
            Type = type;
            InitTechnique();
        }

        public override BlendState GetBlendState()
        {
            if(Type == CharaShaderType.Normals)
            {
                BlendState blendState = new BlendState();
                blendState.IndependentBlendEnable = true;
                blendState.ApplyNone(0);
                blendState.ApplyNone(1);

                return blendState;
            }
            else
            {
                return base.GetBlendState();
            }
        }

        public override DepthStencilState GetDepthState()
        {
            DepthStencilState depth = new DepthStencilState();

            if(Type == CharaShaderType.Normals)
            {
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = true;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = false;
                depth.StencilWriteMask = 255;
                depth.StencilMask = 255;
                depth.ReferenceStencil = 255;
            }
            else
            {
                //Default character depth buffer
                depth.DepthBufferEnable = true;
                depth.DepthBufferWriteEnable = true;
                depth.DepthBufferFunction = CompareFunction.LessEqual;
                depth.StencilEnable = true;
                depth.StencilWriteMask = 80;
                depth.StencilMask = 80;
                depth.ReferenceStencil = 80;
                depth.CounterClockwiseStencilPass = StencilOperation.Replace;
                depth.StencilPass = StencilOperation.Replace;
                depth.StencilFunction = CompareFunction.Always;
            }

            return depth;
        }
    }

    public enum CharaShaderType
    {
        Default,
        Normals
    }
}
