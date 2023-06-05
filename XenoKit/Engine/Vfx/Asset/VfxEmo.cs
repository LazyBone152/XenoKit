using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EffectContainer;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Animation;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx.Asset
{
    public class VfxEmo : VfxAsset
    {
        private readonly Xv2CoreLib.EffectContainer.Asset Asset;

        private Xv2ModelFile Model;
        private List<Xv2ShaderEffect> Materials;
        private Xv2Texture[] Textures;
        private EMA_File MaterialAnimations;
        private EMA_File ObjAnimations;
        private EMM_File EmmFile;

        private ushort EmaIndex = ushort.MaxValue;
        private EMA_Animation MaterialAnimation;
        private EMA_Animation Animation;

        private EmaAnimationPlayer AnimationPlayer;
        private Xv2Skeleton Skeleton;

        private float Time = 0f;
        private ushort AnimationLoopEndFrame
        {
            get
            {
                if (EffectPart.EMA_LoopEndFrame != 0) return EffectPart.EMA_LoopEndFrame;
                return AnimationEndFrame;
            }
        }
        private ushort AnimationEndFrame
        {
            get
            {
                if (Animation != null) return Animation.EndFrame;
                if (MaterialAnimation != null) return MaterialAnimation.EndFrame;
                return 1000;
            }
        }

        //Animated Materials:
        public bool IsMaterialsAnimated => MaterialAnimation != null;
        public List<VfxEmaMaterialNode> MaterialNodes = new List<VfxEmaMaterialNode>();

        public VfxEmo(Matrix startWorld, Xv2CoreLib.EffectContainer.Asset asset, EffectPart effectPart, Actor actor, GameBase gameBase) : base(startWorld, effectPart, actor, gameBase)
        {
            Asset = asset;
            InitializeFiles();
            Transform = Matrix.Identity;
        }

        private void InitializeFiles()
        {
            Model = null;
            Materials = null;
            Textures = null;
            MaterialAnimations = null;
            ObjAnimations = null;
            MaterialAnimation = null;
            Animation = null;
            AnimationPlayer = null;
            Skeleton = null;
            EmmFile = null;

            foreach (EffectFile file in Asset.Files)
            {
                if (file.fileType == EffectFile.FileType.EMO)
                {
                    Model = (file.EmoFile != null) ? CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(file.EmoFile, GameBase) : null;
                    Skeleton = CompiledObjectManager.GetCompiledObject<Xv2Skeleton>(file.EmoFile.Skeleton, GameBase);
                    AnimationPlayer = new EmaAnimationPlayer(Skeleton);
                }
                else if (file.fileType == EffectFile.FileType.EMB)
                {
                    Textures = (file.EmbFile != null) ? Xv2Texture.LoadTextureArray(file.EmbFile, GameBase) : null;
                }
                else if (file.fileType == EffectFile.FileType.EMM)
                {
                    //Materials must be initialized after the EMO, so save this for later
                    EmmFile = file.EmmFile;
                }
                else if (file.fileType == EffectFile.FileType.EMA && file.EmaFile?.EmaType == EmaType.mat)
                {
                    MaterialAnimations = file.EmaFile;
                }
                else if (file.fileType == EffectFile.FileType.EMA && file.EmaFile?.EmaType == EmaType.obj)
                {
                    ObjAnimations = file.EmaFile;
                }
            }

            if (Model != null)
            {
                Materials = Model.InitializeMaterials(EmmFile);
            }
            else
            {
                Materials = null;
            }

            SetAnimation();
        }

        private void SetAnimation()
        {
            if (MaterialAnimations != null)
            {
                SetDefaultMaterialValues();
                MaterialAnimation = MaterialAnimations.Animations.FirstOrDefault(x => x.Index == EffectPart.EMA_AnimationIndex);
            }

            if (ObjAnimations != null)
            {
                Animation = ObjAnimations.Animations.FirstOrDefault(x => x.Index == EffectPart.EMA_AnimationIndex);
                AnimationPlayer?.PlayAnimation(ObjAnimations, EffectPart.EMA_AnimationIndex, true);
            }

            EmaIndex = EffectPart.EMA_AnimationIndex;
        }

        private void SetDefaultMaterialValues()
        {
            if (MaterialAnimation == null || EmmFile == null) return;

            for (int i = 0; i < MaterialNodes.Count; i++)
            {
                EmmMaterial material = EmmFile.GetMaterial(MaterialAnimation.Nodes[i].BoneName);
                if (material == null) continue;

                //Set default material values from EMM
                MaterialNodes[i].MatCol[0][0] = material.DecompiledParameters.MatCol0.R;
                MaterialNodes[i].MatCol[0][1] = material.DecompiledParameters.MatCol0.G;
                MaterialNodes[i].MatCol[0][2] = material.DecompiledParameters.MatCol0.B;
                MaterialNodes[i].MatCol[0][3] = material.DecompiledParameters.MatCol0.A;
                MaterialNodes[i].MatCol[1][0] = material.DecompiledParameters.MatCol1.R;
                MaterialNodes[i].MatCol[1][1] = material.DecompiledParameters.MatCol1.G;
                MaterialNodes[i].MatCol[1][2] = material.DecompiledParameters.MatCol1.B;
                MaterialNodes[i].MatCol[1][3] = material.DecompiledParameters.MatCol1.A;
                MaterialNodes[i].MatCol[2][0] = material.DecompiledParameters.MatCol2.R;
                MaterialNodes[i].MatCol[2][1] = material.DecompiledParameters.MatCol2.G;
                MaterialNodes[i].MatCol[2][2] = material.DecompiledParameters.MatCol2.B;
                MaterialNodes[i].MatCol[2][3] = material.DecompiledParameters.MatCol2.A;
                MaterialNodes[i].MatCol[3][0] = material.DecompiledParameters.MatCol3.R;
                MaterialNodes[i].MatCol[3][1] = material.DecompiledParameters.MatCol3.G;
                MaterialNodes[i].MatCol[3][2] = material.DecompiledParameters.MatCol3.B;
                MaterialNodes[i].MatCol[3][3] = material.DecompiledParameters.MatCol3.A;
                MaterialNodes[i].TexScrl[0][0] = material.DecompiledParameters.TexScrl0.U;
                MaterialNodes[i].TexScrl[0][1] = material.DecompiledParameters.TexScrl0.V;
                MaterialNodes[i].TexScrl[1][0] = material.DecompiledParameters.TexScrl1.U;
                MaterialNodes[i].TexScrl[1][1] = material.DecompiledParameters.TexScrl1.V;


                foreach (Xv2ShaderEffect compiledMaterial in Materials)
                {
                    if (compiledMaterial.Material.Name == material.Name)
                    {
                        compiledMaterial.SetMaterialAnimationValues(MaterialNodes[i]);
                    }
                }
            }
        }

        public override void Update()
        {
            Update(false);
        }

        private void Update(bool simulate)
        {
            base.Update();

            //Animation has been changed
            if (EffectPart.EMA_AnimationIndex != EmaIndex)
            {
                SetAnimation();
            }

            //SetDefaultValues();

            if (Time >= AnimationLoopEndFrame - 1 && EffectPart.EMA_Loop && !IsTerminating)
            {
                Time = EffectPart.EMA_LoopStartFrame;
                //ResetKeyframeIndex();
            }
            else if (Time >= AnimationEndFrame && !EffectPart.EMA_Loop)
            {
                IsFinished = true;
                return;
            }

            //Update animations
            if(Materials != null)
            {
                if (!simulate || VfxManager.ForceEffectUpdate)
                {
                    AnimationPlayer?.Update(Transform);

                    if (MaterialAnimation != null)
                    {
                        for (int i = 0; i < MaterialAnimation.Nodes.Count; i++)
                        {
                            if (MaterialNodes.Count <= i)
                                MaterialNodes.Add(new VfxEmaMaterialNode());

                            EmmMaterial material = EmmFile.GetMaterial(MaterialAnimation.Nodes[i].BoneName);
                            if (material == null) continue;

                            //Set default material values from EMM
                            MaterialNodes[i].MatCol[0][0] = material.DecompiledParameters.MatCol0.R;
                            MaterialNodes[i].MatCol[0][1] = material.DecompiledParameters.MatCol0.G;
                            MaterialNodes[i].MatCol[0][2] = material.DecompiledParameters.MatCol0.B;
                            MaterialNodes[i].MatCol[0][3] = material.DecompiledParameters.MatCol0.A;
                            MaterialNodes[i].MatCol[1][0] = material.DecompiledParameters.MatCol1.R;
                            MaterialNodes[i].MatCol[1][1] = material.DecompiledParameters.MatCol1.G;
                            MaterialNodes[i].MatCol[1][2] = material.DecompiledParameters.MatCol1.B;
                            MaterialNodes[i].MatCol[1][3] = material.DecompiledParameters.MatCol1.A;
                            MaterialNodes[i].MatCol[2][0] = material.DecompiledParameters.MatCol2.R;
                            MaterialNodes[i].MatCol[2][1] = material.DecompiledParameters.MatCol2.G;
                            MaterialNodes[i].MatCol[2][2] = material.DecompiledParameters.MatCol2.B;
                            MaterialNodes[i].MatCol[2][3] = material.DecompiledParameters.MatCol2.A;
                            MaterialNodes[i].MatCol[3][0] = material.DecompiledParameters.MatCol3.R;
                            MaterialNodes[i].MatCol[3][1] = material.DecompiledParameters.MatCol3.G;
                            MaterialNodes[i].MatCol[3][2] = material.DecompiledParameters.MatCol3.B;
                            MaterialNodes[i].MatCol[3][3] = material.DecompiledParameters.MatCol3.A;
                            MaterialNodes[i].TexScrl[0][0] = material.DecompiledParameters.TexScrl0.U;
                            MaterialNodes[i].TexScrl[0][1] = material.DecompiledParameters.TexScrl0.V;
                            MaterialNodes[i].TexScrl[1][0] = material.DecompiledParameters.TexScrl1.U;
                            MaterialNodes[i].TexScrl[1][1] = material.DecompiledParameters.TexScrl1.V;

                            foreach (EMA_Command command in MaterialAnimation.Nodes[i].Commands)
                            {
                                if (command.Parameter < 4)
                                {
                                    //MatCol
                                    MaterialNodes[i].MatCol[command.Parameter][command.Component] = command.GetKeyframeValue(Time);
                                }
                                else if (command.Parameter < 6)
                                {
                                    //TexScrl
                                    MaterialNodes[i].TexScrl[command.Parameter - 4][command.Component] = command.GetKeyframeValue(Time);
                                }
                            }

                            foreach (Xv2ShaderEffect compiledMaterial in Materials)
                            {
                                if (compiledMaterial.Material.Name == material.Name)
                                {
                                    compiledMaterial.SetMaterialAnimationValues(MaterialNodes[i]);
                                }
                            }
                        }
                    }
                }
            }

            //Update model (skinning)
            if (SettingsManager.Instance.Settings.XenoKit_VfxSimulation && (!simulate || VfxManager.ForceEffectUpdate))
            {
                Model?.Update(0, Skeleton);
            }

            if (simulate)
            {
                Time += 1f;
            }
            else if (SceneManager.IsPlaying)
            {
                Time += EffectPart.UseTimeScale ? SceneManager.MainAnimTimeScale * SceneManager.BacTimeScale : 1f;
            }

            AnimationPlayer?.SetFrame(Time);
        }

        public override void Simulate()
        {
            Update(true);
        }

        public override void Draw()
        {
            if (DrawThisFrame && Model != null)
            {
                Model.Draw(Matrix.CreateScale(Scale) * GetAdjustedTransform(), 0, Materials, Textures, null, 0, Skeleton);
            }
        }


    }

    public class VfxEmaMaterialNode
    {
        public float[][] MatCol;
        public float[][] TexScrl;

        public VfxEmaMaterialNode()
        {
            MatCol = new float[4][];
            TexScrl = new float[2][];
            MatCol[0] = new float[4];
            MatCol[1] = new float[4];
            MatCol[2] = new float[4];
            MatCol[3] = new float[4];
            TexScrl[0] = new float[4];
            TexScrl[1] = new float[4];
        }
    }
}
