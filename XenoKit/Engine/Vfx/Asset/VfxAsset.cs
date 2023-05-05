using Microsoft.Xna.Framework;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Vfx.Asset
{
    public abstract class VfxAsset : Entity
    {
        public bool IsFinished { get; protected set; }
        protected bool IsTerminating;
        protected readonly EffectPart EffectPart;
        protected readonly Actor Actor;

        protected virtual bool FinishAnimationBeforeTerminating => false;
        private int BoneIdx = -1;
        protected float Scale = 1f;
        private Matrix StartingTransform;
        protected bool DrawThisFrame = true;

        //Asset Type
        private AssetType AssetType;
        public bool AssetTypeChanged { get; private set; }

        public VfxAsset(EffectPart effectPart, Actor actor, GameBase gameBase) : base(gameBase)
        {
            EffectPart = effectPart;
            Actor = actor;
            AssetType = EffectPart.AssetType;

            Initialize();
            EffectPart.PropertyChanged += EffectPart_PropertyChanged;
        }

        private void EffectPart_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: Set up properties in EffectPart for relevant values! This does nothing right now...
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (AssetType != EffectPart.AssetType)
            {
                AssetTypeChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(EffectPart.ESK) && Actor != null)
            {
                BoneIdx = Actor.Skeleton.GetBoneIndex(EffectPart.ESK);
            }

            StartingTransform = BoneIdx != -1 ? Actor.Skeleton.Bones[BoneIdx].SkinningMatrix : Matrix.Identity;

            Scale = Random.Range(EffectPart.ScaleMin, EffectPart.ScaleMax);
        }

        /// <summary>
        /// Deactivates the effect according to the Deactivation Mode.
        /// </summary>
        public void Terminate()
        {
            if (EffectPart.Deactivation == EffectPart.DeactivationMode.Always || (EffectPart.Deactivation == EffectPart.DeactivationMode.AfterAnimLoop && !FinishAnimationBeforeTerminating))
            {
                IsFinished = true;
            }
            else if (EffectPart.Deactivation == EffectPart.DeactivationMode.AfterAnimLoop)
            {
                IsTerminating = true;
            }
        }

        public override void Dispose()
        {
            EffectPart.PropertyChanged -= EffectPart_PropertyChanged;
        }


        public override void Update()
        {
            //TODO: Handle BoneDirection
            Transform = Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));

            /*
            if (Actor != null && BoneIdx != -1)
            {
                if(EffectPart.PositionUpdate)
                    Transform *= Matrix.CreateTranslation(Actor.Skeleton.Bones[BoneIdx].SkinningMatrix.Translation);
                else
                    Transform *= Matrix.CreateTranslation(StartingTransform.Translation);


                if (EffectPart.RotateUpdate)
                    Transform *= Actor.Skeleton.Bones[BoneIdx].SkinningMatrix * Matrix.Invert(Matrix.CreateTranslation(Actor.Skeleton.Bones[BoneIdx].SkinningMatrix.Translation));
                else
                    Transform *= StartingTransform * Matrix.Invert(Matrix.CreateTranslation(StartingTransform.Translation));
            }
            */

            //Near and Far fade distance
            if (EffectPart.FarFadeDistance != 0)
            {
                float distanceToCamera = Vector3.Distance(GameBase.ActiveCameraBase.CameraState.Position, Transform.Translation);
                DrawThisFrame = distanceToCamera > EffectPart.NearFadeDistance && distanceToCamera < EffectPart.FarFadeDistance;
            }
            else
            {
                DrawThisFrame = true;
            }

        }

        public virtual void Simulate()
        {

        }

        public virtual void SeekNextFrame()
        {

        }

        public virtual void SeekPrevFrame()
        {

        }
    }
}
