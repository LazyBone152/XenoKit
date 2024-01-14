using Microsoft.Xna.Framework;
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Vfx.Asset
{
    public abstract class VfxAsset : Entity
    {
        public float CurrentFrame { get; protected set; }
        public bool HasStarted { get; protected set; }
        public bool IsFinished { get; protected set; }
        public bool IsTerminating { get; protected set; }
        protected readonly EffectPart EffectPart;
        protected readonly Actor Actor;

        protected virtual bool FinishAnimationBeforeTerminating => false;
        private int BoneIdx = -1;
        public float Scale { get; protected set; } = -1f;
        private Matrix BacSpawnSource;
        private Matrix InitialPosition;
        private Matrix InitialRotation;
        private Matrix CurrentRotation;

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
                BoneIdx = Actor.Skeleton.GetBoneIndex(EffectPart.ESK, true);
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

            //Apply CurrentRotation, if enabled
            if (EffectPart.EnableRotationValues)
            {
                float rotX = MathHelper.ToRadians(Random.Range(EffectPart.RotationX_Min, EffectPart.RotationX_Max));
                float rotY = MathHelper.ToRadians(Random.Range(EffectPart.RotationY_Min, EffectPart.RotationY_Max));
                float rotZ = MathHelper.ToRadians(Random.Range(EffectPart.RotationZ_Min, EffectPart.RotationZ_Max));

                CurrentRotation = Matrix.CreateFromYawPitchRoll(rotX, rotY, rotZ);
            }
            else
            {
                CurrentRotation = Matrix.Identity;
            }

            Scale = Random.Range(EffectPart.ScaleMin, EffectPart.ScaleMax);

            //Reset start state
            CurrentFrame = 0f;
            HasStarted = false;
        }

        /// <summary>
        /// Deactivates the effect according to the Deactivation Mode.
        /// </summary>
        public virtual void Terminate()
        {
            if (EffectPart.Deactivation == EffectPart.DeactivationMode.Immediate || (EffectPart.Deactivation == EffectPart.DeactivationMode.LoopCancel && !FinishAnimationBeforeTerminating))
            {
                IsFinished = true;
            }
            else if (EffectPart.Deactivation == EffectPart.DeactivationMode.LoopCancel)
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
            if (!HasStarted)
            {
                if(CurrentFrame >= EffectPart.StartTime)
                {
                    CurrentFrame = 0f;
                    HasStarted = true;
                }
                else
                {
                    CurrentFrame += EffectPart.UseTimeScale ? Actor.ActiveTimeScale : 1f;
                    DrawThisFrame = false;
                    return;
                }
            }

            DrawThisFrame = true;

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
                    Transform = Actor.GetAbsoluteBoneMatrix(BoneIdx) * Matrix.Invert(Matrix.CreateTranslation(Actor.GetAbsoluteBoneMatrix(BoneIdx).Translation)) * InitialPosition * Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));
                }
                else
                {
                    //Use starting position and rotation
                    Transform = Matrix.CreateTranslation(new Vector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ)) * InitialPosition * InitialRotation;
                }
            }
            else if(EffectPart.AttachementType == EffectPart.Attachment.External)
            {
                Transform = BacSpawnSource;
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

            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
                DrawThisFrame = false;
        }

        public virtual void Simulate()
        {
            //Update();
        }

        public virtual void SeekNextFrame()
        {
            //Update();
        }

        public virtual void SeekPrevFrame()
        {

        }

        protected Matrix GetAdjustedTransform()
        {
            //Unsure on AttachmentBone and User. They seem to get different rotations... so something is wrong

            Matrix transform = Transform;

            if (EffectPart.AttachementType == EffectPart.Attachment.Camera)
            {
                //Place transform directly in front of the camera
                Vector3 direction = GameBase.ActiveCameraBase.CameraState.ActualTargetPosition - GameBase.ActiveCameraBase.CameraState.ActualPosition;
                Vector3 cameraForward = Vector3.Normalize(direction);
                Vector3 positionInFrontOfCamera = GameBase.ActiveCameraBase.CameraState.ActualPosition + (cameraForward * 1f);

                transform.Translation = positionInFrontOfCamera;
            }

            switch (EffectPart.Orientation)
            {
                case EffectPart.OrientationType.None:
                    //Just uses position and no orientation
                    //The game seems to always rotate it by 90 degrees on Y for some reason
                    transform = Matrix.CreateRotationY(MathHelper.PiOver2) * CurrentRotation * Matrix.CreateTranslation(transform.Translation);
                    break;
                case EffectPart.OrientationType.User:
                    if (Actor == null) return Transform;
                    //Effect Position/Rotation + Base Bone of actor, with an additional rotation based on EffectPart.Direction (I_06)
                    Matrix userMatrix = Matrix.CreateTranslation(transform.Translation) * (Actor.Transform * Matrix.Invert(Matrix.CreateTranslation(Actor.Transform.Translation)));

                    //If I_06 was 2, there is no rotation (default direction)
                    if (EffectPart.I_06 == 0)
                        userMatrix = Matrix.CreateRotationY(MathHelper.PiOver2) * userMatrix;
                    else if (EffectPart.I_06 == 1)
                        userMatrix = Matrix.CreateRotationX(MathHelper.PiOver2) * userMatrix;

                    transform = CurrentRotation * userMatrix;
                    break;
                case EffectPart.OrientationType.Camera:
                    //Effect Position + rotate to face camera.
                    transform = CurrentRotation * Matrix.CreateBillboard(transform.Translation, SceneManager.MainCamera.CameraBase.CameraState.ActualPosition, Vector3.Up, Vector3.Forward);
                    break;
                case EffectPart.OrientationType.RotateMovement:
                    //This rotates the effect by 45 degrees if there is active movement going on.
                    //transform = Matrix.CreateRotationX(MathHelper.PiOver4) * Transform;
                    transform = CurrentRotation * Transform;
                    break;
                case EffectPart.OrientationType.AttachmentBone:
                default:
                    //Use full rotation of the attachment bone
                    transform = CurrentRotation * Transform;
                    break;

            }

            return transform;
        }
    }
}
