using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.View;
using XenoKit.Engine.Scripting.BAC;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EAN;

namespace XenoKit.Engine
{
    public class Actor : Entity
    {
        //TODO: Clean this mess up!

        public int ActorSlot { get; set; }
        public CharaPartSet PartSet;
        public int ForceDytOverride = -1;

        //Animation:
        private Xv2Skeleton _skeleton = null;
        public Xv2Skeleton Skeleton
        {
            get
            {
                return _skeleton;
            }
            set
            {
                if (_skeleton != value)
                {
                    _skeleton = value;
                    AnimationPlayer = new AnimationPlayer(_skeleton, this); //Skeleton was changed, so animationPlayer needs to be reloaded
                }
            }
        }
        public AnimationPlayer AnimationPlayer { get; set; }
        public EAN_File FceEanFile
        {
            get
            {
                //PartSet defined fce.ean file
                if (PartSet?.FceEan != null)
                    return PartSet.FceEan;

                //Default fce.ean for this character
                return CharacterData?.MovesetFiles?.GetFaceEanFile();
            }
        }
        public EAN_File FceEyeEanFile
        {
            get
            {
                //PartSet defined fce.ean file
                if (PartSet?.FceForeheadEan != null)
                    return PartSet.FceForeheadEan;

                //Default fce.ean for this character
                return CharacterData?.MovesetFiles?.GetFaceEanFile();
            }
        }

        //Visual Skeletons:
        private DebugSkeleton debugSkeleton;
        public VisualSkeleton visualSkeleton;

        //Action:
        public ActionControl ActionControl { get; set; }

        //Character Source Files:
        public Xv2Character CharacterData { get; set; }
        public Move Moveset { get; set; }

        //Misc Settings:
        private Vector3 DefaultPosition
        {
            get
            {
                switch (ActorSlot)
                {
                    case 0:
                        return new Vector3();
                    case 1:
                        return new Vector3(0, 0, -5);
                    default:
                        return new Vector3();
                }
            }
        }
        public string ShortName => CharacterData?.CmsEntry?.ShortName;

        //Eye UV Scroll. (These are sized 4 arrays because thats what the shaders expect, though we only need the first 2 values)
        public float[] EyeIrisLeft_UV { get; set; } = new float[4];
        public float[] EyeIrisRight_UV { get; set; } = new float[4];
        public bool BacEyeMovementUsed = false;

        public Actor(GameBase gameBase, Xv2Character character, int initialPartSet = 0) : base(gameBase)
        {
            GameBase = gameBase;
            //Skeleton = new Xv2Skeleton(character.EskFile.File);
            Skeleton = CompiledObjectManager.Instance.GetCompiledObject<Xv2Skeleton>(character.EskFile.File, GameBase);
            Name = character.Name[0];
            BaseTransform = Matrix.CreateWorld(DefaultPosition, Vector3.Forward, Vector3.Up);
            ActionControl = new ActionControl(this, gameBase);
            Moveset = new Move(character);
            CharacterData = character;

            debugSkeleton = new DebugSkeleton(gameBase);
            visualSkeleton = new VisualSkeleton(this, gameBase);

            //Create animation player
            AnimationPlayer = new AnimationPlayer(Skeleton, this);

            //Load PartSet
            PartSet = new CharaPartSet(gameBase, this, initialPartSet);
        }

        #region State
        public override Matrix Transform => RootMotionTransform * ActionMovementTransform * BaseTransform;

        public Matrix RootMotionTransform = Matrix.Identity;
        public Matrix ActionMovementTransform = Matrix.Identity;
        public Matrix BaseTransform = Matrix.Identity;

        /// <summary>
        /// Called when an BAC Animation is finished. All movement from that animation will be added onto the BaseTransform and preserved, while discarding all other Root Motion.
        /// </summary>
        public void MergeTransforms()
        {
            BaseTransform *= ActionMovementTransform;
            ActionMovementTransform = Matrix.Identity;
            RootMotionTransform = Matrix.Identity;
        }

        public void ResetPosition()
        {
            BaseTransform = Matrix.Identity * Matrix.CreateTranslation(DefaultPosition);
            ActionMovementTransform = Matrix.Identity;
            RootMotionTransform = Matrix.Identity;
        }

        public void ResetState(bool resetAnimations = true)
        {
            bool retainActionPosition = SceneManager.RetainActionMovement && SceneManager.IsOnTab(EditorTabs.Action);

            //In some cases we might not want to reset animations, such as Animation > Camera (and vice versa) tab changes.
            if (resetAnimations)
            {
                AnimationPlayer.ClearCurrentAnimation(true, !retainActionPosition);

                if (retainActionPosition)
                {
                    MergeTransforms();
                }
                else
                {
                    ActionMovementTransform = Matrix.Identity;
                    RootMotionTransform = Matrix.Identity;
                }
            }

            if(!retainActionPosition)
                ResetPosition();
        }

        #endregion

        #region GameLoop
        public override void Update()
        {
            //Reset Eye positions to their defaults
            EyeIrisLeft_UV[0] = EyeIrisLeft_UV[1] = EyeIrisRight_UV[0] = EyeIrisRight_UV[1] = 0;
            BacEyeMovementUsed = false;

            ActionControl.Update();

            if (AnimationPlayer != null && Skeleton != null)
                AnimationPlayer.Update(Matrix.Identity);

            visualSkeleton.Update(Skeleton.Bones);

            PartSet.Update();
        }

        /// <summary>
        /// Simulate a frame. 
        /// </summary>
        public void Simulate(bool fullAnimUpdate, bool advance)
        {
#if DEBUG
            if (SceneManager.IsPlaying) throw new InvalidOperationException("invalid operation, cannot do this while SceneManager.IsPlaying is true.");
#else
            if (SceneManager.IsPlaying)
            {
                Log.Add("Character.Simulate(): invalid operation, cannot do this while SceneManager.IsPlaying is true.", LogType.Error);
                return;
            }
#endif

            if (AnimationPlayer != null && Skeleton != null)
            {
                AnimationPlayer.Simulate(fullAnimUpdate, advance);
            }
        }

        public override void Draw()
        {
            if (!GameBase.RenderCharacters) return;

            PartSet.Draw();

            if (AnimationPlayer != null && SceneManager.ShowDebugBones)
                debugSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);

            if (AnimationPlayer != null)
                visualSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);
        }

        #endregion

        public Matrix GetAbsoluteBoneMatrix(int index)
        {
            //All b_C_Base movement is moved onto Transform. The bone is always at Identity, so this is needed to get the correct matrix.

            return Skeleton.Bones[index].AbsoluteAnimationMatrix * Transform;
        }

        public Vector3 GetBoneCurrentAbsolutePosition(string name)
        {
            Vector3 pos = AnimationPlayer.GetCurrentAbsoluteMatrix(name).Translation;

            return pos + Transform.Translation;
        }
    
        public Xv2Character ConvertToXv2Character()
        {
            if (Moveset == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No moveset found.");
            if (CharacterData == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No characterData found.");

            CharacterData.MovesetFiles = Moveset.Files;
            return CharacterData;
        }
    }
}
