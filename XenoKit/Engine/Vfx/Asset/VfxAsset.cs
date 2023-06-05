using Microsoft.Xna.Framework;
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;

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
        private Matrix BacSpawnSource;
        private Matrix InitialPosition;
        private Matrix InitialRotation;
        protected bool DrawThisFrame = true;

        //Asset Type
        private AssetType AssetType;
        public bool AssetTypeChanged { get; private set; }

        public VfxAsset(Matrix startWorld, EffectPart effectPart, Actor actor, GameBase gameBase) : base(gameBase)
        {
            EffectPart = effectPart;
            Actor = actor;
            AssetType = EffectPart.AssetType;
            BacSpawnSource = startWorld;

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

            //Set Transform to selected bone if on bone attachment, else use the StartingTransform (from BAC)
            if (EffectPart.AttachementType == EffectPart.Attachment.Bone)
            {
                Transform = BoneIdx != -1 && Actor != null ? Actor.GetAbsoluteBoneMatrix(BoneIdx) : Matrix.Identity;
            }
            else
            {
                Transform = BacSpawnSource;
            }

            //Set initial position and rotation matrices. This is needed to properly support the Update Pos/Update Rot flags.
            InitialPosition = Matrix.CreateTranslation(Transform.Translation);
            InitialRotation = Transform * Matrix.Invert(InitialPosition);

            //Apply Initial Position XYZ offsets
            Transform *= Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));

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
            if(Actor != null && BoneIdx != -1 && EffectPart.AttachementType == EffectPart.Attachment.Bone)
            {
                //TODO: implement BoneDirection

                if (EffectPart.PositionUpdate && EffectPart.RotateUpdate)
                {
                    Transform = Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ)) * Actor.GetAbsoluteBoneMatrix(BoneIdx);
                }
                else if (EffectPart.PositionUpdate)
                {
                    Transform = InitialRotation * Matrix.CreateTranslation(Actor.GetAbsoluteBoneMatrix(BoneIdx).Translation) * Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));
                }
                else if (EffectPart.RotateUpdate)
                {
                    Transform = Actor.GetAbsoluteBoneMatrix(BoneIdx) * Matrix.Invert(Matrix.CreateTranslation(Actor.GetAbsoluteBoneMatrix(BoneIdx).Translation)) * InitialPosition;
                }
            }

            //Near and Far fade distance
            if (MathHelpers.FloatEquals(EffectPart.FarFadeDistance, 0))
            {
                DrawThisFrame = true;
            }
            else
            {
                float distanceToCamera = System.Math.Abs(Vector3.Distance(GameBase.ActiveCameraBase.CameraState.Position, Transform.Translation));
                DrawThisFrame = distanceToCamera >= EffectPart.NearFadeDistance && distanceToCamera < EffectPart.FarFadeDistance;
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

        protected Matrix GetAdjustedTransform()
        {
            //Unsure on AttachmentBone and User. They seem to get different rotations... so something is wrong

            switch (EffectPart.Orientation)
            {
                case EffectPart.OrientationType.None:
                    //Just uses position and no orientation
                    //The game seems to always rotate it by 90 degrees on Y for some reason
                    return Matrix.CreateRotationY(MathHelpers.Radians90Degrees) * Matrix.CreateTranslation(Transform.Translation);
                case EffectPart.OrientationType.User:
                    if (Actor == null) return Transform;
                    //Effect Position + Orientation of base bone of actor, with an extra rotation on Y
                    return Matrix.CreateRotationY(MathHelpers.Radians90Degrees) * Matrix.CreateRotationX(-MathHelpers.Radians90Degrees) * Matrix.CreateTranslation(Transform.Translation) * (Actor.Transform * Matrix.Invert(Matrix.CreateTranslation(Actor.Transform.Translation)));
                case EffectPart.OrientationType.Camera:
                    //Effect Position + rotate to face camera.
                    return Matrix.CreateBillboard(Transform.Translation, SceneManager.MainCamera.CameraBase.CameraState.ActualPosition, Vector3.Up, Vector3.Forward) * Matrix.CreateTranslation(Transform.Translation);
                case EffectPart.OrientationType.AttachmentBone:
                default:
                    //Use full rotation of the attachment bone
                    return Matrix.CreateRotationX(MathHelpers.Radians90Degrees) * Transform;

            }

        }
    }
}
