using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Shader
{
    /*
    /// <summary>
    /// Parameters passed into a ShaderProgram from a Material to populate the CBUFFER structs.
    /// </summary>
    public class MaterialParameters
    {
        //Update each frame
        public Matrix World;
        public Matrix PrevWVP;

        //Static:
        public Vector4 g_vEdge = Vector4.One; //g_vEdge (VS/PS)
        public Vector4 g_EdgeColor = Vector4.One; //g_EdgeColor (VS/PS)
        public Vector4 g_vTone = Vector4.One; //g_vTone_PS
        public Vector4 g_AlphaFade_VS = new Vector4(0, 1, 0, 0);
        public Vector4 g_Gradient_VS = new Vector4(0, 1, 0, 0);
        public Vector4 g_Incidence = new Vector4(1, 0, 0, 0); //VS/PS
        public Vector4 g_GlareCoeff_VS = Vector4.One;
        public Vector4 g_vSpecular = Vector4.Zero; //PS/VS
        public Vector4 g_vRim = Vector4.Zero; //VS/PS
        public Vector4 g_Reflection = Vector4.Zero; //VS/PS
        public Vector4 g_vAlphaTest = Vector4.Zero; //VS/PS
        public Vector4 g_vEyePos = Vector4.One; //VS/PS
        public Vector4 g_vShadowMap_PS = new Vector4(2048f, 00049f, 0.001f, 0.85f); //Default shadow maps
        public Vector4 g_vGlare_PS = Vector4.Zero;

        //gLightDir, MatSpc, MatDif
        public Vector4 g_vLightVec = Vector4.One; //VS/PS
        public Vector4 g_vLightDif = Vector4.One; //VS/PS
        public Vector4 g_vLightSpc = Vector4.One; //VS/PS
        public float MatDifScale = 1f;

        //From XenoViewer:
        //xy: height_face and add to have max fog on 200 and start at 100 :
        //	([100, 200] - 100) / 100 <=> ([100, 200] / 100) - 1 <=> ([100, 200] * y) + x => [0, 1]
        //zw: same for distance (pos.w) : from 2000 to 3000: ([2000, 3000] / 1000) - 2 => [0, 1]
        public Vector4 g_vHeightFog_VS = new Vector4(-1.0f, 1 / 100, -2, 1 / 1000);
        //same as g_vHeightFog_VS.zw, but for multiply 
        public Vector4 g_vFog_VS = new Vector4(-2, 1 / 1000, -2, 1 / 1000);
        public Vector4 g_vFogMultiColor = new Vector4(1, 1, 1, 0.2f); //Fog color... todo test
        public Vector4 g_vFogAddColor_PS = Vector4.Zero;

        //bools
        public int VsFlag0 = 0; //g_bVersatile0_VS
        public int VsFlag1 = 0; //g_bVersatile1_VS
        public int VsFlag2 = 0; //g_bVersatile2_VS
        public int VsFlag3 = 0; //g_bVersatile3_VS
        public bool gTime = false;

        //MatCol
        public Vector4 MatCol0 = new Vector4(0,0,0,1); //g_MaterialCol0 (VS/PS)
        public Vector4 MatCol1 = new Vector4(0, 0, 0, 1);
        public Vector4 MatCol2 = new Vector4(0, 0, 0, 1);
        public Vector4 MatCol3 = new Vector4(0, 0, 0, 1);

        //MatScale
        public Vector4 MatScale0 = Vector4.One; //g_MaterialScale0 (VS/PS)
        public Vector4 MatScale1 = Vector4.One; //g_MaterialScale1 (VS/PS)

        //MatOffset
        public Vector4 MatOffset0 = Vector4.One; //g_MaterialOffset0 (VS/PS)
        public Vector4 MatOffset1 = Vector4.One; //g_MaterialOffset1 (VS/PS)

        //TexScroll
        public Vector4 TexScrl0; //g_TexScroll0_PS
        public Vector4 TexScrl1; //g_TexScroll1_PS

        //MatAmb
        public Vector4 MatAmb = Vector4.One;
        public float MatAmbScale = 1f;

        //Texture Settings:
        public float[] MipMapLod = new float[4]; //Each index is for a different texture, determined by index of EMD_TextureDefinition. 4 max, as that is the maximum amount of samplers/textures.

        //Rendering:
        public bool CullBackFaces = true;
        public BlendState AlphaBlend = new BlendState();
        public DepthStencilState DepthStencilState = new DepthStencilState();

        //vs_stage
        public Vector4 g_vScreen_VS = new Vector4((float)SceneManager.gameInstance.Width, (float)SceneManager.gameInstance.Height, 0.10f, 10106.85645f);
        public Vector4 g_SystemTime_VS = Vector4.Zero;

        public MaterialParameters(Xv2CoreLib.EMM.EmmMaterial material)
        {
            InitializeEmmParams(material);
        }

        private void InitializeEmmParams(Xv2CoreLib.EMM.EmmMaterial material)
        {
            //MatCol
            if(material.Parameters.Any(x => x.Name.Contains("MatCol0")))
                MatCol0 = GetVector4(material.GetColor("MatCol0"));

            if (material.Parameters.Any(x => x.Name.Contains("MatCol1")))
                MatCol1 = GetVector4(material.GetColor("MatCol1"));

            if (material.Parameters.Any(x => x.Name.Contains("MatCol2")))
                MatCol2 = GetVector4(material.GetColor("MatCol2"));

            if (material.Parameters.Any(x => x.Name.Contains("MatCol3")))
                MatCol3 = GetVector4(material.GetColor("MatCol3"));

            //MatScale
            if (material.Parameters.Any(x => x.Name.Contains("MatScale0")))
                MatScale0 = GetVector4(material.GetVector4("MatScale0"));

            if (material.Parameters.Any(x => x.Name.Contains("MatScale1")))
                MatScale1 = GetVector4(material.GetVector4("MatScale1"));

            //MatOffset
            if (material.Parameters.Any(x => x.Name.Contains("MatOffset0")))
                MatOffset0 = GetVector4(material.GetVector4("MatOffset0"));

            if (material.Parameters.Any(x => x.Name.Contains("MatOffset1")))
                MatOffset1 = GetVector4(material.GetVector4("MatOffset1"));

            //MatAmb
            if (material.Parameters.Any(x => x.Name.Contains("MatAmb")))
                MatAmb = GetVector4(material.GetColor("MatAmb"));

            //TexScrl
            if (material.Parameters.Any(x => x.Name.Contains("TexScrl0")))
                TexScrl0 = GetVector2(material.GetUV("TexScrl0"));

            if (material.Parameters.Any(x => x.Name.Contains("TexScrl1")))
                TexScrl1 = GetVector2(material.GetUV("TexScrl1"));

            //gScroll (alt name for TexScrl?)
            if (material.Parameters.Any(x => x.Name.Contains("gScrollU0")))
            {
                var scrollU = material.Parameters.FirstOrDefault(x => x.Name == "gScrollU0");
                var scrollV = material.Parameters.FirstOrDefault(x => x.Name == "gScrollV0");

                if(scrollU != null && scrollV != null)
                {
                    TexScrl0 = new Vector4(scrollU.Float, scrollV.Float, 0, 0);
                }
            }

            if (material.Parameters.Any(x => x.Name.Contains("gScrollU1")))
            {
                var scrollU = material.Parameters.FirstOrDefault(x => x.Name == "gScrollU1");
                var scrollV = material.Parameters.FirstOrDefault(x => x.Name == "gScrollV1");

                if (scrollU != null && scrollV != null)
                {
                    TexScrl1 = new Vector4(scrollU.Float, scrollV.Float, 0, 0);
                }
            }

            //Tone
            if (material.Parameters.Any(x => x.Name.Contains("Tone")))
                g_vTone = GetVector4(material.GetVector4("Tone"));

            //GlareCol
            if (material.Parameters.Any(x => x.Name.Contains("GlareCol")))
                g_GlareCoeff_VS = GetVector4(material.GetColor("GlareCol"));

            //gLightDir
            if (material.Parameters.Any(x => x.Name.Contains("gLightDir")))
                g_vLightVec = GetVector4(material.GetVector4("gLightDir"));

            //g_vLightDif
            if (material.Parameters.Any(x => x.Name.Contains("MatDif")))
                g_vLightDif = GetVector4(material.GetVector4("MatDif"));

            //g_vLightSpc
            if (material.Parameters.Any(x => x.Name.Contains("MatSpc")))
                g_vLightSpc = GetVector4(material.GetVector4("MatSpc"));

            //gCamPos
            if (material.Parameters.Any(x => x.Name.Contains("gCamPos")))
                g_vEyePos = GetVector4(material.GetVector4("gCamPos"));

            //Glare
            if (material.Parameters.Any(x => x.Name.Contains("Glare")))
                g_vGlare_PS = GetVector4(material.GetColor("Glare"));



            //All other values
            foreach (var parameter in material.Parameters)
            {
                int intValue = parameter.IntValue;

                if (parameter.Name == "VsFlag0" && intValue == 1)
                    VsFlag0 = 1;

                if (parameter.Name == "VsFlag1" && intValue == 1)
                    VsFlag1 = 1;

                if (parameter.Name == "VsFlag2" && intValue == 1)
                    VsFlag2 = 1;

                if (parameter.Name == "VsFlag3" && intValue == 1)
                    VsFlag3 = 1;

                if (parameter.Name == "NoEdge" && intValue == 1)
                {
                    g_vEdge = Vector4.Zero;
                }

                if (parameter.Name == "FadeInit")
                    g_AlphaFade_VS.X = parameter.Float;

                if(parameter.Name == "FadeSpeed")
                    g_AlphaFade_VS.Y = parameter.Float;

                if (parameter.Name == "GradientInit")
                    g_Gradient_VS.X = parameter.Float;

                if (parameter.Name == "GradientSpeed")
                    g_Gradient_VS.Y = parameter.Float;

                if (parameter.Name == "IncidencePower")
                    g_Incidence.X = parameter.Float;

                if (parameter.Name == "IncidenceAlphaBias")
                    g_Incidence.Y = parameter.Float;

                if (parameter.Name == "SpcCoeff")
                    g_vSpecular.X = parameter.Float;

                if (parameter.Name == "SpcPower")
                    g_vSpecular.Y = parameter.Float;

                if (parameter.Name == "RimCoeff")
                    g_vRim.X = parameter.Float;

                if (parameter.Name == "RimPower")
                    g_vRim.Y = parameter.Float;

                if (parameter.Name == "ReflectCoeff")
                    g_Reflection.X = parameter.Float;

                if (parameter.Name == "ReflectFresnelBias")
                    g_Reflection.Y = parameter.Float;

                if (parameter.Name == "ReflectFresnelCoeff")
                    g_Reflection.Z = parameter.Float;

                if (parameter.Name == "AlphaTest")
                    g_vAlphaTest = new Vector4(intValue / 255, intValue / 255, intValue / 255, 1f);

                if (parameter.Name == "gTime" && intValue == 1)
                    gTime = true;

                if (parameter.Name == "MatAmbScale")
                    MatAmbScale = parameter.Float;

                if (parameter.Name == "MatDifScale")
                    MatDifScale = parameter.Float;

                //Rendering:
                if ((parameter.Name == "BackFace" || parameter.Name == "TwoSidedRender") && intValue == 1)
                {
                    //Culling
                    CullBackFaces = false;
                }

                if (parameter.Name == "AlphaBlend" && intValue == 1)
                {
                    DepthStencilState.DepthBufferEnable = true;

                    var param2 = material.GetParameter("AlphaBlendType");

                    if (param2 != null)
                    {
                        switch (param2.IntValue)
                        {
                            case 0: //Normal
                                AlphaBlend = BlendState.NonPremultiplied;
                                break;
                            case 1: //Additive
                                AlphaBlend = BlendState.Additive;
                                break;
                            case 2: //Sub
                                AlphaBlend = BlendState.AlphaBlend;
                                AlphaBlend.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                                AlphaBlend.ColorSourceBlend = Blend.InverseDestinationColor;
                                AlphaBlend.ColorDestinationBlend = Blend.One;
                                AlphaBlend.AlphaSourceBlend = Blend.Zero;
                                AlphaBlend.AlphaDestinationBlend = Blend.One;
                                break;
                        }
                    }
                }

                if (parameter.Name == "AlphaSortMask" && intValue == 1)
                {
                    DepthStencilState.DepthBufferEnable = true;
                }

                if (parameter.Name == "ZWriteMask" && intValue == 1)
                {
                    DepthStencilState.DepthBufferWriteEnable = true;
                }

                if (parameter.Name == "ZTestMask" && intValue == 1)
                {
                    DepthStencilState.DepthBufferEnable = true;
                }

                //MipMapLods
                if (parameter.Name.Contains("MipMapLod0"))
                    MipMapLod[0] = parameter.Float;

                if (parameter.Name.Contains("MipMapLod1"))
                    MipMapLod[1] = parameter.Float;

                if (parameter.Name.Contains("MipMapLod2"))
                    MipMapLod[2] = parameter.Float;

                if (parameter.Name.Contains("MipMapLod3"))
                    MipMapLod[3] = parameter.Float;

            }
        }
    
        private Vector4 GetVector4(float[] values)
        {
            return new Vector4(values[0], values[1], values[2], values[3]);
        }

        private Vector4 GetVector2(float[] values)
        {
            return new Vector4(values[0], values[1], 0, 0);
        }

        public float[] ToFloatArray(Vector2 vector)
        {
            return new float[4] { vector.X, vector.Y, 0, 0 };
        }

        public float[] ToFloatArray(Vector3 vector)
        {
            return new float[4] { vector.X, vector.Y, vector.Z, 0 };
        }

        public float[] ToFloatArray(Vector4 vector)
        {
            return new float[4] { vector.X, vector.Y, vector.Z, vector.W };
        }
    }
    */
}
