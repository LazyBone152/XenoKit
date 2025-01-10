using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;

namespace XenoKit.Engine.Rendering
{
    public class YBSPostFilterStep
    {
        public PostShaderEffect Shader;
        public RenderTargetWrapper RenderTarget;
        private RenderTargetWrapper[] RTInputs;
        private Texture2D[] TextureInputs;
        public Color ClearColor;

        private Vector2[] TexCoords1;
        private Vector2[] TexCoords2;
        private Vector2[] TexCoords3;
        private Vector2[] TexCoords4;

        public YBSShaderParameters Parameters = YBSShaderParameters.Default();

        public YBSPostFilterStep(PostShaderEffect shader, RenderTargetWrapper renderTarget, params object[] textures)
        {
            Shader = shader;
            RenderTarget = renderTarget;
            ClearColor = Color.Black;

            TextureInputs = new Texture2D[textures.Length];
            RTInputs = new RenderTargetWrapper[textures.Length];

            for(int i = 0; i < textures.Length; i++)
            {
                if (textures[i] is Texture2D)
                {
                    TextureInputs[i] = (Texture2D)textures[i];
                }
                else if (textures[i] is RenderTargetWrapper)
                {
                    RTInputs[i] = (RenderTargetWrapper)textures[i];
                }
            }
        }
    
        public void SetTexCoords(Vector2[] coords, int index)
        {
            switch(index)
            {
                case 1:
                    TexCoords1 = coords; 
                    break;
                case 2:
                    TexCoords2 = coords;
                    break;
                case 3:
                    TexCoords3 = coords;
                    break;
                case 4:
                    TexCoords4 = coords;
                    break;
            }
        }

        public void Apply(RenderSystem renderSystem)
        {
            renderSystem.SetRenderTargets(RenderTarget.RenderTarget);
            renderSystem.GraphicsDevice.Clear(ClearColor);

            for (int i = 0; i < RTInputs.Length; i++)
            {
                if (RTInputs[i] != null)
                {
                    renderSystem.SetTexture(RTInputs[i].RenderTarget, i);
                }
                else
                {
                    renderSystem.SetTexture(TextureInputs[i], i);
                }
            }

            if (TexCoords1 != null)
                renderSystem.PostFilter.SetTextureCoordinates(TexCoords1, 1);
            else
                renderSystem.PostFilter.SetDefaultTexCord2();

            if (TexCoords2 != null)
                renderSystem.PostFilter.SetTextureCoordinates(TexCoords2, 2);

            if (TexCoords3 != null)
                renderSystem.PostFilter.SetTextureCoordinates(TexCoords3, 3);

            if (TexCoords4 != null)
                renderSystem.PostFilter.SetTextureCoordinates(TexCoords4, 4);

            Shader.SetParameters(Parameters);
            renderSystem.PostFilter.Apply(Shader);
        }
    }
}
