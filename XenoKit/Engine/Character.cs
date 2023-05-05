using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMD;
using Xv2CoreLib.ESK;
using XenoKit.Engine.Animation;
using XenoKit.Engine.View;
using Xv2CoreLib.Resource;
using XenoKit.Engine.Scripting;
using static Xv2CoreLib.BAC.BAC_Type0;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CSO;
using Xv2CoreLib.ERS;
using Xv2CoreLib.BAI;
using Xv2CoreLib;
using Xv2CoreLib.AMK;

namespace XenoKit.Engine
{
    public class Character : Entity
    {
        #region Fields
        private CharacterSkeleton _skeleton = null;
        private DebugSkeleton debugSkeleton;
        public VisualSkeleton visualSkeleton;
        private Vector3 DefaultPosition;
        #endregion

        #region Properties
        public CharacterSkeleton Skeleton
        {
            get
            {
                return _skeleton;
            }
            set
            {
                if(_skeleton != value)
                {
                    _skeleton = value;
                    animationPlayer = new AnimationPlayer(_skeleton, this); //Skeleton was changed, so animationPlayer needs to be reloaded
                }
            }
        }
        public AnimationPlayer animationPlayer { get; set; }
        public List<EmdFile> Models { get; private set; }
        public List<PhysicsObject> PhysicsModels { get; private set; }
        public Matrix[] skinningMatrices
        {
            get
            {
                return (animationPlayer != null) ? animationPlayer.GetSkinningMatrices() : null;
            }
        }
        public BacPlayer bacPlayer { get; set; }

        #endregion

        #region Settings
        public short Type { get; set; } //1 = CaC, 2 = Cast (Used for bac playback)
        public string ShortName { get; set; }
        #endregion

        //Files
        public Xv2Character characterData { get; set; }
        public Move Moveset { get; set; }
        public CMS_Entry CmsEntry { get { return characterData?.CmsEntry; } }

        #region Transform
        public new Matrix Transform
        {
            get
            {
                return animatedTransform * baseTransform;
            }
        }

        public Matrix animatedTransform = Matrix.Identity;
        public Matrix baseTransform = Matrix.Identity;

        public void MergeTransforms()
        {
            baseTransform *= animatedTransform;
            animatedTransform = Matrix.Identity;
        }
        #endregion

        public Character(CharacterSkeleton skeleton, GraphicsDevice _graphicsDevice, Vector3 position, string name = null)
        {
            graphicsDevice = _graphicsDevice;
            Skeleton = skeleton;
            Name = (name != null) ? name : "Unknown Character";
            Models = new List<EmdFile>();
            PhysicsModels = new List<PhysicsObject>();
            baseTransform = Matrix.CreateWorld(position, Vector3.Forward, Vector3.Up);
            bacPlayer = new BacPlayer(this);
            DefaultPosition = position;

            debugSkeleton = new DebugSkeleton();
            visualSkeleton = new VisualSkeleton(this);

            //Create animation player
            if (skeleton != null)
                animationPlayer = new AnimationPlayer(skeleton, this);
        }
        
        public void ResetPosition()
        {
            baseTransform = Matrix.Identity * Matrix.CreateTranslation(DefaultPosition);
            animatedTransform = Matrix.Identity;
        }

        public void ResetState(bool resetAnimations = true)
        {
            //In some cases we might not want to reset animations, such as Animation > Camera (and vice versa) tab changes.
            if (resetAnimations)
            {
                animationPlayer.ClearCurrentAnimation(true, true);
                animatedTransform = Matrix.Identity;
            }

            ResetPosition();
        }

        #region AddMethods
        public void AddModel(string emdPath)
        {
            Models.Add(EmdFile.Load(emdPath, Skeleton, graphicsDevice));
        }

        public void AddModel(byte[] emdBytes, string name = null)
        {
            Models.Add(EmdFile.Load(emdBytes, graphicsDevice, Skeleton, name));
        }
        
        public void AddPhysicsModel(string bone, byte[] emdBytes)
        {
            PhysicsModels.Add(new PhysicsObject(bone, emdBytes, graphicsDevice, null));
        }
        #endregion

        #region GameLoop
        public override void Update(GameTime time)
        {
            bacPlayer.Update();

            if (animationPlayer != null && Skeleton.BoneAbsoluteMatrices != null)
                animationPlayer.Update(Matrix.Identity);

            visualSkeleton.Update(animationPlayer.GetDebugBoneMatrices());
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

            if (animationPlayer != null && Skeleton.BoneAbsoluteMatrices != null)
            {
                animationPlayer.Simulate(fullAnimUpdate, advance);
            }
        }

        public override void Draw(Camera camera)
        {
            foreach (var model in Models)
                model.Draw(graphicsDevice, camera, Transform, skinningMatrices);

            foreach (var model in PhysicsModels)
                model.Draw(graphicsDevice, camera, animationPlayer.GetCurrentAbsoluteMatrix(model.Bone) * Transform, skinningMatrices);

            if ((animationPlayer != null) && ((SceneManager.ShowDebugBones)))
                debugSkeleton.Draw(animationPlayer.GetDebugBoneMatrices(), Skeleton.Bones, graphicsDevice, camera, Transform);

            if (animationPlayer != null)
                visualSkeleton.Draw(animationPlayer.GetDebugBoneMatrices(), Skeleton.Bones, camera, Transform);
        }

#endregion


        public Vector3 GetBoneCurrentAbsolutePosition(string name)
        {
            Vector3 pos = animationPlayer.GetCurrentAbsoluteMatrix(name).Translation;

            return pos + Transform.Translation;
        }
    
        public Xv2Character ConvertToXv2Character()
        {
            if (Moveset == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No moveset found.");
            if (characterData == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No characterData found.");

            characterData.MovesetFiles = Moveset.Files;
            return characterData;
        }
    }
}
