using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.View;
using XenoKit.Engine.Scripting.BAC;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EAN;
using XenoKit.Controls;
using XenoKit.Engine.Character;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Shader;

namespace XenoKit.Engine
{
    public class Actor : Entity, ISkinned
    {
        //TODO: Clean this mess up!
        public override Matrix AbsoluteTransform => Transform;
        public override EntityType EntityType => EntityType.Actor;

        public int Team => ActorSlot == 1 ? 1 : 0;
        public int ActorSlot = 0;
        public CharaPartSet PartSet;
        public int ForceDytOverride = -1;
        public bool IsVisible = true;

        //TimeScale:
        public int BdmTimeScaleDuration = 0;
        public float BdmTimeScale = 1f;
        public float BacTimeScale = 1f;
        public float AnimationTimeScale = 1f;
        public float ActiveTimeScale
        {
            get
            {
                return (SceneManager.CurrentSceneState == EditorTabs.Action) ? BacTimeScale * AnimationTimeScale * BdmTimeScale : 1f;
            }
        }

        //Components:
        private DebugSkeleton _debugSkeleton;
        public VisualSkeleton _visualSkeleton;
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
        public AnimationPlayer AnimationPlayer { get; private set; }
        public ActionControl ActionControl { get; private set; }
        public ActorController Controller { get; private set; }

        //Hitbox:
        public BoundingBox Hitbox { get; set; }
        public float[] AABB { get; private set; }
        public bool HitboxEnabled = true;

        //Shader Effects
        public ActorShaderExtraParameters ShaderParameters;

        //Character Source Files:
        public Xv2Character CharacterData { get; private set; }
        public Move Moveset { get; private set; }
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

        //Voice:
        public int Voice { get; set; }
        /// <summary>
        /// A character code alias to use for skill voice lines.
        /// </summary>
        public string SkillVoiceAlias { get; set; }

        //Misc
        public string ShortName => CharacterData?.CmsEntry?.ShortName;
        public AnimationTabView AnimationViewInstance => AnimationTabView.Instance; //Required for the ISkinned interface. Associates a GUI view with the underlying skinned object.

        //Eye UV Scroll. (These are sized 4 arrays because thats what the shaders expect, though we only need the first 2 values)
        public float[] EyeIrisLeft_UV { get; set; } = new float[4];
        public float[] EyeIrisRight_UV { get; set; } = new float[4];
        public bool BacEyeMovementUsed = false;

        public Actor(GameBase gameBase, Xv2Character character, int initialPartSet = 0) : base(gameBase)
        {
            GameBase = gameBase;
            Skeleton = CompiledObjectManager.GetCompiledObject<Xv2Skeleton>(character.EskFile.File, GameBase);
            Name = character.Name[0];
            ResetPosition();
            ActionControl = new ActionControl(this, gameBase);
            Controller = new ActorController(this);
            Moveset = new Move(character);
            CharacterData = character;
            AABB = new float[6];

            _debugSkeleton = new DebugSkeleton(gameBase);
            _visualSkeleton = new VisualSkeleton(this, gameBase);

            //Create animation player
            AnimationPlayer = new AnimationPlayer(Skeleton, this);

            //Load PartSet
            PartSet = new CharaPartSet(gameBase, this, initialPartSet);
        }

        #region Transform
        public override Matrix Transform => RootMotionTransform * ActionMovementTransform * BaseTransform;

        public Matrix RootMotionTransform = Matrix.Identity;
        public Matrix ActionMovementTransform = Matrix.Identity;
        public Matrix BaseTransform = Matrix.Identity;

        /// <summary>
        /// Called when an BAC Animation is finished. All movement from that animation will be added onto the BaseTransform and preserved, while discarding all other Root Motion.
        /// </summary>
        public void MergeTransforms()
        {
            BaseTransform = ActionMovementTransform * BaseTransform;
            ActionMovementTransform = Matrix.Identity;
            RootMotionTransform = Matrix.Identity;
        }

        public void ApplyTranslation(Vector3 translation)
        {
            BaseTransform *= Matrix.CreateTranslation(translation);
        }

        public void ResetPosition()
        {
            Vector3 pos;
            Matrix rotation;

            switch (ActorSlot)
            {
                case 1:
                    pos = new Vector3(0, 0, -SceneManager.VictimDistance);
                    rotation = SceneManager.VictimIsFacingPrimary ? Matrix.CreateRotationY(MathHelper.Pi) : Matrix.Identity;
                    break;
                default:
                    pos = Vector3.Zero;
                    rotation = Matrix.Identity;
                    break;
            }

            BaseTransform = Matrix.Identity * rotation * Matrix.CreateTranslation(pos);
            ActionMovementTransform = Matrix.Identity;
            RootMotionTransform = Matrix.Identity;
        }

        public void ResetState(bool keepAnimation = false)
        {
            ShaderParameters.ShaderPath = ActorShaderPath.Default;
            BdmTimeScaleDuration = 0;
            BdmTimeScale = 1f;
            BacTimeScale = 1f;
            Controller.ResetState(keepAnimation);

            bool retainActionPosition = SceneManager.RetainActionMovement && SceneManager.IsOnTab(EditorTabs.Action);

            //In some cases we might not want to reset animations, such as Animation > Camera (and vice versa) tab changes.
            //TODO: Check for a better place to put this. Dont want UI related stuff mixed in here
            if (!keepAnimation)
            {
                AnimationPlayer.ClearCurrentAnimation(true, !retainActionPosition);
            }

            if (!retainActionPosition)
                ResetPosition();
        }

        #endregion

        #region GameLoop
        public override void Update()
        {
            DrawThisFrame = true;
            IsVisible = true;
            HitboxEnabled = true;

            //Reset Eye positions to their defaults
            EyeIrisLeft_UV[0] = EyeIrisLeft_UV[1] = EyeIrisRight_UV[0] = EyeIrisRight_UV[1] = 0;
            BacEyeMovementUsed = false;

            //Disable the ActorController for the main actor, and enable it for the victim
            //Will need to adjust this when adding BDM preview support
            if(ActorSlot == 0 && Controller.State != ActorState.Null)
            {
                Controller.State = ActorState.Null;
            }
            else if(ActorSlot == 1 && Controller.State == ActorState.Null)
            {
                Controller.State = ActorState.Idle;
            }

            if (GameBase.IsPlaying)
                UpdateBdmTimeScale();

            Controller.Update();
            ActionControl.Update();

            if (AnimationPlayer != null && Skeleton != null)
                AnimationPlayer.Update(Matrix.Identity);

            _visualSkeleton.Update(Skeleton.Bones);

            PartSet.Update();

            //Update hitbox
            if (SceneManager.IsOnTab(EditorTabs.Action))
            {
                Hitbox = new BoundingBox(new Vector3(AABB[0], AABB[1], AABB[2]) + Transform.Translation, new Vector3(AABB[3], AABB[4], AABB[5]) + Transform.Translation);
            }
        }

        public override void DelayedUpdate()
        {
            ActionControl.DelayedUpdate();
        }

        /// <summary>
        /// Simulate a frame. 
        /// </summary>
        public void Simulate(bool fullAnimUpdate, bool advance)
        {
#if DEBUG
            if (GameBase.IsPlaying) throw new InvalidOperationException("invalid operation, cannot do this while GameBase.IsPlaying is true.");
#else
            if (GameBase.IsPlaying)
            {
                Log.Add("Character.Simulate(): invalid operation, cannot do this while GameBase.IsPlaying is true.", LogType.Error);
                return;
            }
#endif
            HitboxEnabled = true;

            UpdateBdmTimeScale();
            Controller.Simulate();

            if (ActorSlot != 0)
            {
                ActionControl.Update();
            }

            if (AnimationPlayer != null && Skeleton != null)
            {
                AnimationPlayer.Simulate(fullAnimUpdate, advance);
            }

            //Update hitbox
            if (SceneManager.IsOnTab(EditorTabs.Action))
            {
                Hitbox = new BoundingBox(new Vector3(AABB[0], AABB[1], AABB[2]) + Transform.Translation, new Vector3(AABB[3], AABB[4], AABB[5]) + Transform.Translation);
            }
        }

        public override void Draw()
        {
            DrawThisFrame = false;
            if (!GameBase.RenderCharacters || !IsVisible) return;

            ActionControl.Draw();
            PartSet.Draw();

            if (AnimationPlayer != null && SceneManager.ShowDebugBones)
                _debugSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);

            if (AnimationPlayer != null)
                _visualSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);
        }

        public override void DrawPass(bool normalPass)
        {
            if (!GameBase.RenderCharacters || !IsVisible) return;
            PartSet.DrawSimple(normalPass);
        }

        #endregion

        #region TimeScale
        private void UpdateBdmTimeScale()
        {
            if(BdmTimeScaleDuration > 0)
            {
                BdmTimeScaleDuration--;
            }
            else if(BdmTimeScaleDuration == 0)
            {
                BdmTimeScale = 1f;
            }
        }

        public void SetBacTimeScale(float timeScale, bool reset)
        {
            if (reset) BacTimeScale = 1f;
            BacTimeScale *= timeScale;
        }

        #endregion

        public bool HitTest(BacHitbox hitbox)
        {
            if (!HitboxEnabled || Controller.InvulnerabilityFrames > 0 || Controller.FreezeActionFrames > 0) return false;

            if (Hitbox.Intersects(hitbox.BoundingBox))
            {
                Xv2CoreLib.BDM.BDM_File bdm = Files.Instance.GetBdmFile(hitbox.Hitbox.bdmFile, hitbox.BacEntry.SkillMove, hitbox.BacEntry.User, false);

                if(bdm != null)
                {
                    Controller.ApplyDamageState(bdm.GetEntry(hitbox.Hitbox.BdmEntryID), hitbox.GetRelativeDirection(Transform), hitbox);
                }

                return true;
            }

            return false;
        }

        public Matrix GetAbsoluteBoneMatrix(int index)
        {
            //All b_C_Base movement is moved onto Transform. The bone is always at Identity, so this is needed to get the correct matrix.

            //b_C_Hand is a special bone that is a combination of b_R_Hand and b_L_Hand
            if (index == Xv2Skeleton.b_C_Hand_Magic)
                return Skeleton.GetHandBarycenter() * Transform;

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
