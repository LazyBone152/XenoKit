using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.App;
using XenoKit.Editor;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.Shader;

namespace XenoKit.Engine.Textures
{
    public class CustomColorManager : EngineObject
    {
        private readonly List<CustomColorCanvas> _customColors = new List<CustomColorCanvas>(32);
        private readonly VertexPositionColorTexture[] _vertices;
        private readonly PostShaderParameters Parameters = new PostShaderParameters();

        private readonly PostShaderEffect DTMAP_BLEND_CST_YUV_MODIFIED;

        private bool _invalidateAllColors = false;
        private bool _dumpAllColors = false;

        public CustomColorManager()
        {
            DTMAP_BLEND_CST_YUV_MODIFIED = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetShaderProgram("DTMAP_BLEND_CST_YUV_MODIFIED"));

            //The game uses a TriangleStrip for the BCS color shader, but a TriangleList is easier to work with so we'll use that instead
            const float width = 128f;
            const float height = 32f;
            _vertices = new VertexPositionColorTexture[192];

            for (int row = 0; row < 32; row++)
            {
                int nextRow = row + 1;
                int vertIdx = row * 6;

                var topLeft = new VertexPositionColorTexture(new Vector3(0, nextRow, 0), Color.Transparent, new Vector2(0, nextRow / height));
                var bottomLeft = new VertexPositionColorTexture(new Vector3(0, row, 0), Color.Transparent, new Vector2(0, row / height));
                var bottomRight = new VertexPositionColorTexture( new Vector3(width, row, 0), Color.Transparent, new Vector2(1, row / height));
                var topRight = new VertexPositionColorTexture(new Vector3(width, nextRow, 0), Color.Transparent, new Vector2(1, nextRow / height));

                _vertices[vertIdx + 0] = topLeft;
                _vertices[vertIdx + 1] = bottomLeft;
                _vertices[vertIdx + 2] = bottomRight;
                _vertices[vertIdx + 3] = topLeft;
                _vertices[vertIdx + 4] = bottomRight;
                _vertices[vertIdx + 5] = topRight;

            }
        }

        public void RegisterCanvas(CustomColorCanvas canvas)
        {
            _customColors.Add(canvas);
        }

        public override void Draw()
        {
            bool recreateAll = _invalidateAllColors;

            for (int i = 0; i < _customColors.Count; i++)
            {
                if (_customColors[i].ParentIsAlive() && (_customColors[i].IsDirty || recreateAll))
                {
                    ApplyCustomColor(_customColors[i]);
                }
            }

            if (recreateAll)
                _invalidateAllColors = false;

            if (_dumpAllColors)
            {
                _dumpAllColors = false;
                SaveToDisk();
            }
        }

        public override void DelayedUpdate()
        {
            //Handle clearing up old instances

            for (int i = _customColors.Count - 1; i >= 0; i--)
            {
                if (!_customColors[i].ParentIsAlive() || _customColors[i].IsDiscarded)
                {
                    _customColors[i].Canvas.Dispose();
                    _customColors.RemoveAt(i);
                }
            }
        }

        private void ApplyCustomColor(CustomColorCanvas customColor)
        {
            ResetVertexColor();

            CharaPart part = customColor.GetCharaPart();

            if (part is null || part.Dyts == null || part.Dyts.Length == 0 || part.EmmFile == null)
            {
                customColor.IsDirty = false;
                customColor.HasCustomColors = false;
                return;
            }

            //Span<bool> enabled = stackalloc bool[8];
            customColor.HasCustomColors = false;
            bool[] enabled = new bool[8];
            BCS_File bcsFile = part.ParentActor.CharacterData.BcsFile.File;
            EMM_File emmFile = part.EmmFile;
            int dytIdx = customColor.DytIndex >= part.Dyts.Length ? 0 : customColor.DytIndex;

            foreach (var color in part.GetColorGroups())
            {
                if (color.Color != -1 && color.Group != -1)
                {
                    PartColor colorGroup = bcsFile.PartColors?.FirstOrDefault(x => x.ID == color.Group);
                    if (colorGroup is null) continue;

                    foreach(var material in emmFile.Materials)
                    {
                        if (CharaPart.IsCustomColorMaterial(material.Name, colorGroup.Name))
                        {
                            int blockIdx = (int)material.DecompiledParameters.MatScale1.X;

                            var bcsColor = colorGroup.ColorsList.FirstOrDefault(x => x.ID == color.Color);
                            if (bcsColor is null) continue;

                            if(blockIdx >= 0 && blockIdx < 8)
                            {
                                customColor.HasCustomColors = true;
                                enabled[blockIdx] = true;
                                int rowIdx = blockIdx * 4;

                                SetVertexColor(rowIdx, bcsColor.Color1.ToXna());
                                SetVertexColor(rowIdx + 1, bcsColor.Color2.ToXna());
                                SetVertexColor(rowIdx + 2, bcsColor.Color3.ToXna());
                                SetVertexColor(rowIdx + 3, bcsColor.Color4.ToXna());
                            }
                        }
                    }
                }
            }

            GraphicsDevice.SetRenderTarget(customColor.Canvas);
            GraphicsDevice.Clear(Color.Black);
            RenderSystem.SetTextures(part.Dyts[customColor.DytIndex].Texture, part.Dyts[customColor.DytIndex].Texture);

            foreach(var pass in DTMAP_BLEND_CST_YUV_MODIFIED.CurrentTechnique.Passes)
            {
                Parameters.g_vParam0_VS = new Vector4(enabled[0] ? 1f : 0, enabled[1] ? 1f : 0, enabled[2] ? 1f : 0, enabled[3] ? 1f : 0);
                Parameters.g_vParam1_VS = new Vector4(enabled[4] ? 1f : 0, enabled[5] ? 1f : 0, enabled[6] ? 1f : 0, enabled[7] ? 1f : 0);
                Parameters.g_vParam0_PS = Vector4.Zero;

                DTMAP_BLEND_CST_YUV_MODIFIED.SetParameters(Parameters);
                pass.Apply();

                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length / 3);
            }

            customColor.IsDirty = false;
        }

        private void ResetVertexColor()
        {
            for(int row = 0; row < 32; row++)
            {
                int vertIdx = row * 6;

                for(int i = 0; i < 6; i++)
                {
                    _vertices[vertIdx + i].Color = Color.Transparent;
                }
            }
        }

        private void SetVertexColor(int row, Color color)
        {
            if (row >= 32 || row < 0)
            {
                Log.Add("CustomColorManager.SetVertexColor: row was outside the valid range.", LogType.Debug);
                return;
            }

            int vertIdx = row * 6;

            for (int i = 0; i < 6; i++)
            {
                _vertices[vertIdx + i].Color = color;
            }
        }
        
        public void InvalidateAllCustomColors()
        {
            _invalidateAllColors = true;
        }
    
        public void DumpAllCustomColors()
        {
            _dumpAllColors = true;
        }

        private void SaveToDisk()
        {
            Directory.CreateDirectory(SettingsManager.Instance.GetAbsPathInAppFolder("CC_Dump"));

            foreach (var canvas in _customColors)
            {
                CharaPart part = canvas.GetCharaPart();
                if (part is null) continue;

                string name = part.partType.ToString();

                using (MemoryStream ms = new MemoryStream())
                {
                    canvas.Canvas.SaveAsPng(ms, canvas.Canvas.Width, canvas.Canvas.Height);

                    try
                    {
                        File.WriteAllBytes(SettingsManager.Instance.GetAbsPathInAppFolder($"CC_Dump/{name}.dyt.png"), ms.ToArray());
                        File.WriteAllBytes(SettingsManager.Instance.GetAbsPathInAppFolder($"CC_Dump/{name}.dyt.dds"), part.DytFile.Entry[canvas.DytIndex].Data);
                    }
                    catch (IOException)
                    {
                        Log.Add("Dump failed, close the file first", LogType.Error);
                        return;
                    }
                }
            }
        }

    }
}
