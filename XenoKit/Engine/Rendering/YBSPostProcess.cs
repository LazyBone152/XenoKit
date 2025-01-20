using AForge.Imaging.Filters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Rendering
{
    public class YBSPostProcess : Entity
    {
        private RenderSystem renderSystem;
        private RenderTargetWrapper SceneRT;
        private RenderTargetWrapper GlareRT;

        private readonly List<RenderTargetWrapper> RenderTargets = new List<RenderTargetWrapper>();

        private RenderTargetWrapper COPY_RT;
        private RenderTargetWrapper DIM_RT;
        private RenderTargetWrapper COPY_REGION_RT;

        private RenderTargetWrapper SMOOTH_1_RT; //Input 0
        private RenderTargetWrapper GLARE_1_RT;
        private RenderTargetWrapper GLARE_2_RT; //Input 1

        private RenderTargetWrapper PRE_PASS1_PIXEL_RT;
        private RenderTargetWrapper PRE_PASS1_GLARE_1_RT;
        private RenderTargetWrapper PRE_PASS1_GLARE_2_RT;

        private RenderTargetWrapper PRE_PASS2_PIXEL_RT;
        private RenderTargetWrapper PRE_PASS2_GLARE_1_RT;
        private RenderTargetWrapper PRE_PASS2_GLARE_2_RT;

        private RenderTargetWrapper PASS1_PIXEL_RT;
        private RenderTargetWrapper PASS1_GLARE1_RT;
        private RenderTargetWrapper PASS1_GLARE2_RT;
        private RenderTargetWrapper PASS1_SMOOTH_RT;

        private RenderTargetWrapper PASS2_PIXEL_RT;
        private RenderTargetWrapper PASS2_GLARE1_RT;
        private RenderTargetWrapper PASS2_GLARE2_RT;
        private RenderTargetWrapper PASS2_SMOOTH_RT;

        private RenderTargetWrapper PASS3_PIXEL_RT;
        private RenderTargetWrapper PASS3_GLARE1_RT;
        private RenderTargetWrapper PASS3_GLARE2_RT;
        private RenderTargetWrapper PASS3_SMOOTH_RT;

        private RenderTargetWrapper PASS4_PIXEL_RT;
        private RenderTargetWrapper PASS4_GLARE1_RT;
        private RenderTargetWrapper PASS4_GLARE2_RT;
        private RenderTargetWrapper PASS4_SMOOTH_RT;

        private RenderTargetWrapper MERGE_RT; //Merge2, Merge5, Merge8
        private RenderTargetWrapper FINAL_SMOOTH_RT;
        private RenderTargetWrapper SCENE_MERGE_RT;

        private PostShaderEffect CopyShader;
        private PostShaderEffect DimShader;
        private PostShaderEffect CopyRegionShader;
        private PostShaderEffect SmoothShader;
        private PostShaderEffect GlareShader;
        private PostShaderEffect PixelShader;
        private PostShaderEffect Merge2Shader;
        private PostShaderEffect Merge5Shader;
        private PostShaderEffect Merge8Shader;
        private PostShaderEffect SceneMergeShader;
        private PostShaderEffect AxisCorrection;

        private Texture2D SceneMergeInput2, SceneMergeInput3;

        private List<YBSPostFilterStep> GlareFilterSteps = new List<YBSPostFilterStep>();
        private int currentGlareLevel = -1;
        private bool currentGlareHighRes = false;

        private YBSPostFilterStep AxisCorrectionFilter;

        public YBSPostProcess(GameBase game, RenderSystem renderSystem, RenderTargetWrapper sceneRT, RenderTargetWrapper glareRT) : base(game)
        {
            this.renderSystem = renderSystem;
            SceneRT = sceneRT;
            GlareRT = glareRT;
            InitializeRTs();
            InitializeShaders();
            InitializeTextures();
            InitializeGlareChain();

            AxisCorrectionFilter = new YBSPostFilterStep(AxisCorrection, null);

            SettingsManager.SettingsSaved += SettingsManager_SettingsSaved;
        }

        private void SettingsManager_SettingsSaved(object sender, EventArgs e)
        {
            if (SettingsManager.settings.XenoKit_GlareLevel != currentGlareLevel || SettingsManager.settings.XenoKit_GlareLevelHighRes != currentGlareHighRes)
            {
                InitializeGlareChain();
            }
        }

        private void InitializeRTs()
        {
            COPY_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Color, false, "YBS_COPY_RT");
            DIM_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Color, false, "YBS_DIM_RT");
            COPY_REGION_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Color, false, "YBS_COPY_REGION_RT");

            SMOOTH_1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_SMOOTH1_RT");
            GLARE_1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_GLARE1_RT");
            GLARE_2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_GLARE2_RT");

            PRE_PASS1_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS1_PIXEL_RT");
            PRE_PASS1_GLARE_1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS1_GLARE_1_RT");
            PRE_PASS1_GLARE_2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS1_GLARE_2_RT");
            PRE_PASS2_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS2_PIXEL_RT");
            PRE_PASS2_GLARE_1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS2_GLARE_1_RT");
            PRE_PASS2_GLARE_2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "PRE_PASS2_GLARE_2_RT");

            PASS1_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS1_PIXEL_RT");
            PASS1_GLARE1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS1_GLARE1_RT");
            PASS1_GLARE2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS1_GLARE2_RT");
            PASS1_SMOOTH_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS1_SMOOTH_RT");

            PASS2_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS2_PIXEL_RT");
            PASS2_GLARE1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS2_GLARE1_RT");
            PASS2_GLARE2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS2_GLARE2_RT");
            PASS2_SMOOTH_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS2_SMOOTH_RT");

            PASS3_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS3_PIXEL_RT");
            PASS3_GLARE1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS3_GLARE1_RT");
            PASS3_GLARE2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS3_GLARE2_RT");
            PASS3_SMOOTH_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS3_SMOOTH_RT");

            PASS4_PIXEL_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS4_PIXEL_RT");
            PASS4_GLARE1_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS4_GLARE1_RT");
            PASS4_GLARE2_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS4_GLARE2_RT");
            PASS4_SMOOTH_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_PASS4_SMOOTH_RT");

            MERGE_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_MERGE_RT");
            FINAL_SMOOTH_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Rgba1010102, false, "YBS_FINAL_SMOOTH_RT");
            SCENE_MERGE_RT = new RenderTargetWrapper(renderSystem, 1, SurfaceFormat.Color, false, "YBS_SCENE_MERGE_RT");

            RenderTargets.Add(COPY_RT);
            RenderTargets.Add(DIM_RT);
            RenderTargets.Add(COPY_REGION_RT);
            RenderTargets.Add(SMOOTH_1_RT);
            RenderTargets.Add(GLARE_1_RT);
            RenderTargets.Add(GLARE_2_RT);
            RenderTargets.Add(PRE_PASS1_PIXEL_RT);
            RenderTargets.Add(PRE_PASS1_GLARE_1_RT);
            RenderTargets.Add(PRE_PASS1_GLARE_2_RT);
            RenderTargets.Add(PRE_PASS2_PIXEL_RT);
            RenderTargets.Add(PRE_PASS2_GLARE_1_RT);
            RenderTargets.Add(PRE_PASS2_GLARE_2_RT);
            RenderTargets.Add(PASS1_PIXEL_RT);
            RenderTargets.Add(PASS1_GLARE1_RT);
            RenderTargets.Add(PASS1_GLARE2_RT);
            RenderTargets.Add(PASS1_SMOOTH_RT);
            RenderTargets.Add(PASS2_PIXEL_RT);
            RenderTargets.Add(PASS2_GLARE1_RT);
            RenderTargets.Add(PASS2_GLARE2_RT);
            RenderTargets.Add(PASS2_SMOOTH_RT);
            RenderTargets.Add(PASS3_PIXEL_RT);
            RenderTargets.Add(PASS3_GLARE1_RT);
            RenderTargets.Add(PASS3_GLARE2_RT);
            RenderTargets.Add(PASS3_SMOOTH_RT);
            RenderTargets.Add(PASS4_PIXEL_RT);
            RenderTargets.Add(PASS4_GLARE1_RT);
            RenderTargets.Add(PASS4_GLARE2_RT);
            RenderTargets.Add(PASS4_SMOOTH_RT);
            RenderTargets.Add(MERGE_RT);
            RenderTargets.Add(FINAL_SMOOTH_RT);
            RenderTargets.Add(SCENE_MERGE_RT);

            foreach(var rt in RenderTargets)
            {
                renderSystem.RegisterRenderTarget(rt);
            }
        }

        private void InitializeShaders()
        {
            AxisCorrection = GetShader("LB_AxisCorrection");
            CopyShader = GetShader("YBS_Copy");
            DimShader = GetShader("YBS_Dim");
            CopyRegionShader = GetShader("YBS_CopyRegion");
            SmoothShader = GetShader("YBS_Smooth");
            GlareShader = GetShader("YBS_Glare");
            PixelShader = GetShader("YBS_Pixel");
            Merge2Shader = GetShader("YBS_Merge2");
            Merge5Shader = GetShader("YBS_Merge5");
            Merge8Shader = GetShader("YBS_Merge8");
            SceneMergeShader = GetShader("YBS_SceneMerge");
        }

        private void InitializeTextures()
        {
            SceneMergeInput2 = TextureLoader.ConvertToTexture2D(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XenoKit/ExtShaders/SceneMergeInput2.dds"), GraphicsDevice);
            SceneMergeInput3 = TextureLoader.ConvertToTexture2D(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XenoKit/ExtShaders/SceneMergeInput3.dds"), GraphicsDevice);
        }

        private void InitializeGlareChain()
        {
            GlareFilterSteps.Clear();
            currentGlareLevel = SettingsManager.settings.XenoKit_GlareLevel;
            currentGlareHighRes = SettingsManager.settings.XenoKit_GlareLevelHighRes;

            if (SettingsManager.settings.XenoKit_GlareLevel == 0)
            {
                float baseResScale = currentGlareHighRes ? 1f : 0.125f;

                COPY_RT.ChangeResScale(baseResScale, 1f);
                DIM_RT.ChangeResScale(0.5f, baseResScale);
                COPY_REGION_RT.ChangeResScale(0.5f, baseResScale);
                SMOOTH_1_RT.ChangeResScale(0.5f, baseResScale);
                GLARE_1_RT.ChangeResScale(0.5f, baseResScale);
                GLARE_2_RT.ChangeResScale(0.5f, baseResScale);
                MERGE_RT.ChangeResScale(0.5f, baseResScale);
                FINAL_SMOOTH_RT.ChangeResScale(0.5f, baseResScale);
                SCENE_MERGE_RT.ChangeResScale(1f, 1f);

                YBSPostFilterStep copy = AddGlare(new YBSPostFilterStep(CopyShader, COPY_RT, SceneRT));
                YBSPostFilterStep dim = AddGlare(new YBSPostFilterStep(DimShader, DIM_RT, COPY_RT));
                YBSPostFilterStep copyRegion = AddGlare(new YBSPostFilterStep(CopyRegionShader, COPY_REGION_RT, DIM_RT, GlareRT));
                YBSPostFilterStep smooth1 = AddGlare(new YBSPostFilterStep(SmoothShader, SMOOTH_1_RT, COPY_REGION_RT)); //Input 1
                YBSPostFilterStep glare1 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_1_RT, SMOOTH_1_RT));
                YBSPostFilterStep glare2 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_2_RT, GLARE_1_RT)); //Input 2
                YBSPostFilterStep merge = AddGlare(new YBSPostFilterStep(Merge2Shader, MERGE_RT, SMOOTH_1_RT, GLARE_2_RT));
                YBSPostFilterStep finalSmooth = AddGlare(new YBSPostFilterStep(SmoothShader, FINAL_SMOOTH_RT, MERGE_RT));
                YBSPostFilterStep sceneMerge = AddGlare(new YBSPostFilterStep(SceneMergeShader, SCENE_MERGE_RT, SceneRT, FINAL_SMOOTH_RT, SceneMergeInput2, SceneMergeInput3));

                copy.ClearColor = Color.Transparent;
                dim.ClearColor = Color.Transparent;
                sceneMerge.ClearColor = Color.Transparent;

                copy.Parameters.SetCopyValues();
                dim.Parameters.SetModulate(1f, 0, 1);
                smooth1.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00208f, -0.0037f, 0, 0));
                finalSmooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00052f, -0.00093f, 0, 0));
                smooth1.Parameters.SetModulate(0.25f, 0, 3);
                finalSmooth.Parameters.SetModulate(0.25f, 0, 3);

                merge.Parameters.SetMergeModulate(2, new Vector4(3.36f));
                sceneMerge.Parameters.SetSceneMergeValues();

                glare1.Parameters.SetGlareModulate();
                glare2.Parameters.SetGlareModulate();

                glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00417f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00417f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00833f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.00833f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00417f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.0125f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.01667f, 0.00f, 0.00f, 0.00f);

                glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.00741f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.00741f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.01481f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.01481f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.02222f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.02222f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.02963f, 0.00f, 0.00f);

            }
            else if (SettingsManager.settings.XenoKit_GlareLevel == 1)
            {
                float baseResScale = currentGlareHighRes ? 1f : 0.25f;

                COPY_RT.ChangeResScale(baseResScale, 1f);
                DIM_RT.ChangeResScale(0.5f, baseResScale);
                COPY_REGION_RT.ChangeResScale(0.5f, baseResScale);
                SMOOTH_1_RT.ChangeResScale(0.5f, baseResScale);
                GLARE_1_RT.ChangeResScale(0.5f, baseResScale);
                GLARE_2_RT.ChangeResScale(0.5f, baseResScale);
                PASS1_PIXEL_RT.ChangeResScale(0.5f, baseResScale);
                PASS1_GLARE1_RT.ChangeResScale(0.5f, baseResScale);
                PASS1_GLARE2_RT.ChangeResScale(0.5f, baseResScale);
                PASS1_SMOOTH_RT.ChangeResScale(0.5f, baseResScale);
                PASS2_PIXEL_RT.ChangeResScale(0.25f, baseResScale);
                PASS2_GLARE1_RT.ChangeResScale(0.25f, baseResScale);
                PASS2_GLARE2_RT.ChangeResScale(0.25f, baseResScale);
                PASS2_SMOOTH_RT.ChangeResScale(0.25f, baseResScale);
                PASS3_PIXEL_RT.ChangeResScale(0.125f, baseResScale);
                PASS3_GLARE1_RT.ChangeResScale(0.125f, baseResScale);
                PASS3_GLARE2_RT.ChangeResScale(0.125f, baseResScale);
                PASS3_SMOOTH_RT.ChangeResScale(0.125f, baseResScale);
                MERGE_RT.ChangeResScale(0.5f, baseResScale);
                FINAL_SMOOTH_RT.ChangeResScale(0.5f, baseResScale);
                SCENE_MERGE_RT.ChangeResScale(1f, 1f);

                YBSPostFilterStep copy = AddGlare(new YBSPostFilterStep(CopyShader, COPY_RT, SceneRT));
                YBSPostFilterStep dim = AddGlare(new YBSPostFilterStep(DimShader, DIM_RT, COPY_RT));
                YBSPostFilterStep copyRegion = AddGlare(new YBSPostFilterStep(CopyRegionShader, COPY_REGION_RT, DIM_RT, GlareRT));
                YBSPostFilterStep smooth1 = AddGlare(new YBSPostFilterStep(SmoothShader, SMOOTH_1_RT, COPY_REGION_RT)); //Input 1
                YBSPostFilterStep glare1 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_1_RT, SMOOTH_1_RT));
                YBSPostFilterStep glare2 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_2_RT, GLARE_1_RT)); //Input 2
                YBSPostFilterStep pass1_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS1_PIXEL_RT, GLARE_2_RT));
                YBSPostFilterStep pass1_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS1_GLARE1_RT, PASS1_PIXEL_RT));
                YBSPostFilterStep pass1_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS1_GLARE2_RT, PASS1_GLARE1_RT));
                YBSPostFilterStep pass1_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS1_SMOOTH_RT, PASS1_GLARE2_RT)); //Input 3
                YBSPostFilterStep pass2_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS2_PIXEL_RT, PASS1_GLARE2_RT));
                YBSPostFilterStep pass2_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS2_GLARE1_RT, PASS2_PIXEL_RT));
                YBSPostFilterStep pass2_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS2_GLARE2_RT, PASS2_GLARE1_RT));
                YBSPostFilterStep pass2_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS2_SMOOTH_RT, PASS2_GLARE2_RT)); //Input 4
                YBSPostFilterStep pass3_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS3_PIXEL_RT, PASS2_GLARE2_RT));
                YBSPostFilterStep pass3_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS3_GLARE1_RT, PASS3_PIXEL_RT));
                YBSPostFilterStep pass3_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS3_GLARE2_RT, PASS3_GLARE1_RT));
                YBSPostFilterStep pass3_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS3_SMOOTH_RT, PASS3_GLARE2_RT)); //Input 5
                YBSPostFilterStep merge = AddGlare(new YBSPostFilterStep(Merge5Shader, MERGE_RT, SMOOTH_1_RT, GLARE_2_RT, PASS1_SMOOTH_RT, PASS2_SMOOTH_RT, PASS3_SMOOTH_RT));
                YBSPostFilterStep finalSmooth = AddGlare(new YBSPostFilterStep(SmoothShader, FINAL_SMOOTH_RT, MERGE_RT));
                YBSPostFilterStep sceneMerge = AddGlare(new YBSPostFilterStep(SceneMergeShader, SCENE_MERGE_RT, SceneRT, FINAL_SMOOTH_RT, SceneMergeInput2, SceneMergeInput3));

                copy.ClearColor = Color.Transparent;
                dim.ClearColor = Color.Transparent;
                sceneMerge.ClearColor = Color.Transparent;

                copy.Parameters.SetCopyValues();
                dim.Parameters.SetModulate(1f, 0, 1);
                pass1_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass2_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass3_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                smooth1.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00104f, -0.00185f, 0, 0));
                pass1_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00156f, -0.00278f, 0, 0));
                pass2_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00313f, -0.00551f, 0, 0));
                pass3_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00625f, -0.01103f, 0, 0));
                finalSmooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00026f, -0.00046f, 0, 0));
                smooth1.Parameters.SetModulate(0.25f, 0, 3);
                pass1_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                pass2_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                pass3_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                finalSmooth.Parameters.SetModulate(0.25f, 0, 3);

                merge.Parameters.SetMergeModulate(5, new Vector4(3.36f));
                sceneMerge.Parameters.SetSceneMergeValues();

                glare1.Parameters.SetGlareModulate();
                glare2.Parameters.SetGlareModulate();
                pass1_Glare1.Parameters.SetGlareModulate();
                pass1_Glare2.Parameters.SetGlareModulate();
                pass2_Glare1.Parameters.SetGlareModulate();
                pass2_Glare2.Parameters.SetGlareModulate();
                pass3_Glare1.Parameters.SetGlareModulate();
                pass3_Glare2.Parameters.SetGlareModulate();

                glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.0037f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.0037f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.00741f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.00741f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.01111f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.01111f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.01481f, 0.00f, 0.00f);

                glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.0037f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.0037f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.00741f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.00741f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.01111f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.01111f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.01481f, 0.00f, 0.00f);

                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.00741f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.00741f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.01481f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.01481f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.02222f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.02222f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.02963f, 0.00f, 0.00f);

                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00417f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00417f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00833f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.00833f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.0125f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.0125f, 0, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.01667f, 0f, 0.00f, 0.00f);

                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00833f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00833f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.01667f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.01667f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.025f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.025f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.03333f, 0.00f, 0.00f, 0.00f);

                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0f, 0.01471f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0, -0.01471f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0, 0.02941f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0, -0.02941f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0, 0.04412f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0, -0.04412f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0f, 0.05882f, 0.00f, 0.00f);

                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.01667f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.01667f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.03333f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.03333f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.05f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.05f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.06667f, 0.00f, 0.00f, 0.00f);

                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.02941f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.02941f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.05882f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.05882f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.08824f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.08824f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.11765f, 0.00f, 0.00f);
            }
            else if(SettingsManager.settings.XenoKit_GlareLevel == 2)
            {
                float baseResScale = currentGlareHighRes ? 1f : 0.5f;

                COPY_RT.ChangeResScale(baseResScale, 1f);
                DIM_RT.ChangeResScale(0.75f, baseResScale);
                COPY_REGION_RT.ChangeResScale(0.75f, baseResScale);

                SMOOTH_1_RT.ChangeResScale(0.75f, baseResScale);

                GLARE_1_RT.ChangeResScale(0.75f, baseResScale);
                GLARE_2_RT.ChangeResScale(0.75f, baseResScale);

                PRE_PASS1_PIXEL_RT.ChangeResScale(0.5f, baseResScale);
                PRE_PASS1_GLARE_1_RT.ChangeResScale(0.5f, baseResScale);
                PRE_PASS1_GLARE_2_RT.ChangeResScale(0.5f, baseResScale);

                PRE_PASS2_PIXEL_RT.ChangeResScale(0.25f, baseResScale);
                PRE_PASS2_GLARE_1_RT.ChangeResScale(0.25f, baseResScale);
                PRE_PASS2_GLARE_2_RT.ChangeResScale(0.25f, baseResScale);

                PASS1_PIXEL_RT.ChangeResScale(0.25f, baseResScale);
                PASS1_GLARE1_RT.ChangeResScale(0.25f, baseResScale);
                PASS1_GLARE2_RT.ChangeResScale(0.25f, baseResScale);
                PASS1_SMOOTH_RT.ChangeResScale(0.25f, baseResScale);

                PASS2_PIXEL_RT.ChangeResScale(0.125f, baseResScale);
                PASS2_GLARE1_RT.ChangeResScale(0.125f, baseResScale);
                PASS2_GLARE2_RT.ChangeResScale(0.125f, baseResScale);
                PASS2_SMOOTH_RT.ChangeResScale(0.125f, baseResScale);

                PASS3_PIXEL_RT.ChangeResScale(0.0625f, baseResScale);
                PASS3_GLARE1_RT.ChangeResScale(0.0625f, baseResScale);
                PASS3_GLARE2_RT.ChangeResScale(0.0625f, baseResScale);
                PASS3_SMOOTH_RT.ChangeResScale(0.0625f, baseResScale);

                PASS4_PIXEL_RT.ChangeResScale(0.03125f, baseResScale);
                PASS4_GLARE1_RT.ChangeResScale(0.03125f, baseResScale);
                PASS4_GLARE2_RT.ChangeResScale(0.03125f, baseResScale);
                PASS4_SMOOTH_RT.ChangeResScale(0.03125f, baseResScale);

                MERGE_RT.ChangeResScale(0.75f, baseResScale);
                FINAL_SMOOTH_RT.ChangeResScale(1f, baseResScale);
                SCENE_MERGE_RT.ChangeResScale(1f, 1f);

                YBSPostFilterStep copy = AddGlare(new YBSPostFilterStep(CopyShader, COPY_RT, SceneRT));
                YBSPostFilterStep dim = AddGlare(new YBSPostFilterStep(DimShader, DIM_RT, COPY_RT));
                YBSPostFilterStep copyRegion = AddGlare(new YBSPostFilterStep(CopyRegionShader, COPY_REGION_RT, DIM_RT, GlareRT));
                YBSPostFilterStep smooth1 = AddGlare(new YBSPostFilterStep(SmoothShader, SMOOTH_1_RT, COPY_REGION_RT)); //Input 1
                YBSPostFilterStep glare1 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_1_RT, SMOOTH_1_RT));
                YBSPostFilterStep glare2 = AddGlare(new YBSPostFilterStep(GlareShader, GLARE_2_RT, GLARE_1_RT)); //Input 2
                YBSPostFilterStep prepass1_pixel = AddGlare(new YBSPostFilterStep(PixelShader, PRE_PASS1_PIXEL_RT, GLARE_2_RT));
                YBSPostFilterStep prepass1_glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PRE_PASS1_GLARE_1_RT, PRE_PASS1_PIXEL_RT));
                YBSPostFilterStep prepass1_glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PRE_PASS1_GLARE_2_RT, PRE_PASS1_GLARE_1_RT)); //Input 3
                YBSPostFilterStep prepass2_pixel = AddGlare(new YBSPostFilterStep(PixelShader, PRE_PASS2_PIXEL_RT, PRE_PASS1_GLARE_2_RT));
                YBSPostFilterStep prepass2_glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PRE_PASS2_GLARE_1_RT, PRE_PASS2_PIXEL_RT));
                YBSPostFilterStep prepass2_glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PRE_PASS2_GLARE_2_RT, PRE_PASS2_GLARE_1_RT)); //Input 4

                YBSPostFilterStep pass1_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS1_PIXEL_RT, PRE_PASS2_GLARE_2_RT));
                YBSPostFilterStep pass1_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS1_GLARE1_RT, PASS1_PIXEL_RT));
                YBSPostFilterStep pass1_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS1_GLARE2_RT, PASS1_GLARE1_RT));
                YBSPostFilterStep pass1_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS1_SMOOTH_RT, PASS1_GLARE2_RT)); //Input 5
                YBSPostFilterStep pass2_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS2_PIXEL_RT, PASS1_GLARE2_RT));
                YBSPostFilterStep pass2_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS2_GLARE1_RT, PASS2_PIXEL_RT));
                YBSPostFilterStep pass2_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS2_GLARE2_RT, PASS2_GLARE1_RT));
                YBSPostFilterStep pass2_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS2_SMOOTH_RT, PASS2_GLARE2_RT)); //Input 6
                YBSPostFilterStep pass3_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS3_PIXEL_RT, PASS2_GLARE2_RT));
                YBSPostFilterStep pass3_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS3_GLARE1_RT, PASS3_PIXEL_RT));
                YBSPostFilterStep pass3_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS3_GLARE2_RT, PASS3_GLARE1_RT));
                YBSPostFilterStep pass3_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS3_SMOOTH_RT, PASS3_GLARE2_RT)); //Input 7
                YBSPostFilterStep pass4_Pixel = AddGlare(new YBSPostFilterStep(PixelShader, PASS4_PIXEL_RT, PASS3_GLARE2_RT));
                YBSPostFilterStep pass4_Glare1 = AddGlare(new YBSPostFilterStep(GlareShader, PASS4_GLARE1_RT, PASS4_PIXEL_RT));
                YBSPostFilterStep pass4_Glare2 = AddGlare(new YBSPostFilterStep(GlareShader, PASS4_GLARE2_RT, PASS4_GLARE1_RT));
                YBSPostFilterStep pass4_Smooth = AddGlare(new YBSPostFilterStep(SmoothShader, PASS4_SMOOTH_RT, PASS4_GLARE2_RT)); //Input 8

                YBSPostFilterStep merge = AddGlare(new YBSPostFilterStep(Merge8Shader, MERGE_RT, SMOOTH_1_RT, GLARE_2_RT, PRE_PASS1_GLARE_2_RT, PRE_PASS2_GLARE_2_RT, PASS1_SMOOTH_RT, PASS2_SMOOTH_RT, PASS3_SMOOTH_RT, PASS4_SMOOTH_RT));
                YBSPostFilterStep finalSmooth = AddGlare(new YBSPostFilterStep(SmoothShader, FINAL_SMOOTH_RT, MERGE_RT));
                YBSPostFilterStep sceneMerge = AddGlare(new YBSPostFilterStep(SceneMergeShader, SCENE_MERGE_RT, SceneRT, FINAL_SMOOTH_RT, SceneMergeInput2, SceneMergeInput3));

                copy.ClearColor = Color.Transparent;
                dim.ClearColor = Color.Transparent;
                sceneMerge.ClearColor = Color.Transparent;

                copy.Parameters.SetCopyValues();
                dim.Parameters.SetModulate(1f, 0, 1);
                prepass1_pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                prepass2_pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass1_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass2_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass3_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                pass4_Pixel.Parameters.afRGBA_Modulate[0] = new Vector4(1);
                smooth1.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00104f, -0.00185f, 0, 0));
                pass1_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00208f, -0.00368f, 0, 0));
                pass2_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00417f, -0.00735f, 0, 0));
                pass3_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00833f, -0.01442f, 0, 0));
                pass4_Smooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.0163f, -0.02885f, 0, 0));
                finalSmooth.Parameters.SetSmoothTexCoordV16(new Vector4(-0.00026f, -0.00046f, 0, 0));
                smooth1.Parameters.SetModulate(0.25f, 0, 3);
                pass1_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                pass2_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                pass3_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                pass4_Smooth.Parameters.SetModulate(0.25f, 0, 3);
                finalSmooth.Parameters.SetModulate(0.25f, 0, 3);

                merge.Parameters.SetMergeModulate(8, new Vector4(3.36f));
                sceneMerge.Parameters.SetSceneMergeValues();

                glare1.Parameters.SetGlareModulate();
                glare2.Parameters.SetGlareModulate();
                prepass1_glare1.Parameters.SetGlareModulate();
                prepass2_glare1.Parameters.SetGlareModulate();
                prepass1_glare2.Parameters.SetGlareModulate();
                prepass2_glare2.Parameters.SetGlareModulate();
                pass1_Glare1.Parameters.SetGlareModulate();
                pass1_Glare2.Parameters.SetGlareModulate();
                pass2_Glare1.Parameters.SetGlareModulate();
                pass2_Glare2.Parameters.SetGlareModulate();
                pass3_Glare1.Parameters.SetGlareModulate();
                pass3_Glare2.Parameters.SetGlareModulate();
                pass4_Glare1.Parameters.SetGlareModulate();
                pass4_Glare2.Parameters.SetGlareModulate();

                glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00069f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00069f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00139f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.00139f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00208f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.00208f, 0.00f, 0.00f, 0.00f);
                glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00278f, 0.00f, 0.00f, 0.00f);

                glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.00123f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.00123f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.00247f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.00247f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.0037f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.0037f, 0.00f, 0.00f);
                glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.00494f, 0.00f, 0.00f);

                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00139f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00139f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00278f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.00278f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00417f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.00417f, 0.00f, 0.00f, 0.00f);
                prepass1_glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00556f, 0.00f, 0.00f, 0.00f);

                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.00247f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.00247f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.00494f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.00494f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.00741f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.00741f, 0.00f, 0.00f);
                prepass1_glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.00988f, 0.00f, 0.00f);

                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00278f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00278f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00556f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.00556f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00833f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.00833f, 0.00f, 0.00f, 0.00f);
                prepass2_glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.01111f, 0.00f, 0.00f, 0.00f);

                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.00493f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.00493f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.00985f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.00985f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.01478f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.01478f, 0.00f, 0.00f);
                prepass2_glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.0197f, 0.00f, 0.00f);

                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00556f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.00556f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.01111f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.01111f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.01667f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.01667f, 0.00f, 0.00f, 0.00f);
                pass1_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.02222f, 0.00f, 0.00f, 0.00f);

                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.0098f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.0098f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.01961f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.01961f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.02941f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.02941f, 0.00f, 0.00f);
                pass1_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.03922f, 0.00f, 0.00f);

                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.01111f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.01111f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.02222f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.02222f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.03333f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.03333f, 0.00f, 0.00f, 0.00f);
                pass2_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.04444f, 0.00f, 0.00f, 0.00f);

                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.01961f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.01961f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.03922f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.03922f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.05882f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.05882f, 0.00f, 0.00f);
                pass2_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.07843f, 0.00f, 0.00f);

                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.02222f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.02222f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.04444f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.04444f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.06667f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.06667f, 0.00f, 0.00f, 0.00f);
                pass3_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.08889f, 0.00f, 0.00f, 0.00f);

                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.03846f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.03846f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.07692f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.07692f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.11538f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.11538f, 0.00f, 0.00f);
                pass3_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.15385f, 0.00f, 0.00f);

                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.04348f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(-0.04348f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.08696f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(-0.08696f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.13043f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(-0.13043f, 0.00f, 0.00f, 0.00f);
                pass4_Glare1.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.17391f, 0.00f, 0.00f, 0.00f);

                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[1] = new Vector4(0.00f, 0.07692f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[2] = new Vector4(0.00f, -0.07692f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[3] = new Vector4(0.00f, 0.15385f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[4] = new Vector4(0.00f, -0.15385f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[5] = new Vector4(0.00f, 0.23077f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[6] = new Vector4(0.00f, -0.23077f, 0.00f, 0.00f);
                pass4_Glare2.Parameters.afUV_TexCoordOffsetV16[7] = new Vector4(0.00f, 0.30769f, 0.00f, 0.00f);
            }
            else
            {
                //Default fallback for when "DISABLE ENITRELY" is selected
                //Just copies the SceneRT into the SCENE_MERGE_RT
                SCENE_MERGE_RT.ChangeResScale(1f, 1f);
                YBSPostFilterStep copy = AddGlare(new YBSPostFilterStep(CopyShader, SCENE_MERGE_RT, SceneRT));

                copy.ClearColor = Color.Transparent;
                copy.Parameters.SetCopyValues();
            }
            

        }

        public override void Draw()
        {
            foreach(var postFilter in GlareFilterSteps)
            {
                postFilter.Apply(renderSystem);
            }
        }

        public void ApplyAxisCorrection(Vector4 defaultColor)
        {
            if(AxisCorrectionFilter != null)
            {
                AxisCorrectionFilter.Parameters.DefaultColor = defaultColor;
                AxisCorrectionFilter.Apply(renderSystem);
            }
        }

        private PostShaderEffect GetShader(string name)
        {
            return CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetExtShaderProgram(name), GameBase, ShaderType.PostFilter);
        }

        private YBSPostFilterStep AddGlare(YBSPostFilterStep filter)
        {
            GlareFilterSteps.Add(filter);
            return filter;
        }
    
        public RenderTargetWrapper GetRenderTarget()
        {
            return SCENE_MERGE_RT;
        }
    
    }

}
