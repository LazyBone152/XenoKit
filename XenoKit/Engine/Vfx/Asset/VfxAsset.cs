using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Asset
{
    public abstract class VfxAsset : RenderObject, IDisposable
    {
        public float CurrentFrame { get; protected set; }
        public bool HasStarted { get; protected set; }
        public bool IsFinished { get; protected set; }
        public bool IsTerminating { get; protected set; }
        protected readonly EffectPart EffectPart;
        protected readonly Actor Actor;
        protected readonly bool SpawnedByProjectile;

        protected virtual bool FinishAnimationBeforeTerminating => false;
        private int BoneIdx = -1;
        public float Scale { get; protected set; } = -1f;
        private Matrix4x4 BacSpawnSource;
        private Matrix4x4 InitialPosition;
        private Matrix4x4 InitialRotation;
        private Matrix4x4 CurrentRotation;

        //Asset Type
        private AssetType AssetType;
        public bool AssetTypeChanged { get; private set; }

        public VfxAsset(Matrix4x4 startWorld, EffectPart effectPart, Actor actor, bool spawnedByProjectile = false)
        {
            EffectPart = effectPart;
            Actor = actor;
            SpawnedByProjectile = spawnedByProjectile;
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
            if (EffectPart.AttachementType == EffectPart.Attachment.Bone && !UsesExternalSpawn())
            {
                Transform = BoneIdx != -1 && Actor != null ? Actor.GetAbsoluteBoneMatrix(BoneIdx) : Matrix4x4.Identity;
            }
            else
            {
                Transform = BacSpawnSource;
            }

            //Set initial position and rotation matrices. This is needed to properly support the Update Pos/Update Rot flags.
            InitialPosition = Matrix4x4.CreateTranslation(Transform.Translation);
            InitialRotation = Transform * InitialPosition.Invert();

            //Apply Initial Position XYZ offsets
            Transform *= Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));

            //Apply CurrentRotation, if enabled
            if (EffectPart.EnableRotationValues)
            {
                float rotX = MathHelper.ToRadians(Xv2CoreLib.Random.Range(EffectPart.RotationX_Min, EffectPart.RotationX_Max));
                float rotY = MathHelper.ToRadians(Xv2CoreLib.Random.Range(EffectPart.RotationY_Min, EffectPart.RotationY_Max));
                float rotZ = MathHelper.ToRadians(Xv2CoreLib.Random.Range(EffectPart.RotationZ_Min, EffectPart.RotationZ_Max));

                CurrentRotation = Matrix4x4.CreateFromYawPitchRoll(rotX, rotY, rotZ);
            }
            else
            {
                CurrentRotation = Matrix4x4.Identity;
            }

            Scale = Xv2CoreLib.Random.Range(EffectPart.ScaleMin, EffectPart.ScaleMax);

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

        public virtual void Dispose()
        {
            EffectPart.PropertyChanged -= EffectPart_PropertyChanged;
        }

        public void SetExternalTransform(Matrix4x4 transform)
        {
            BacSpawnSource = transform;
            OnExternalTransformChanged();
        }

        protected Matrix4x4 GetExternalSpawnTransform()
        {
            return BacSpawnSource;
        }

        protected virtual void OnExternalTransformChanged()
        {
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

            if(Actor != null && BoneIdx != -1 && EffectPart.AttachementType == EffectPart.Attachment.Bone && !UsesExternalSpawn())
            {
                //TODO: implement BoneDirection

                if (EffectPart.PositionUpdate && EffectPart.RotateUpdate)
                {
                    Transform = Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ)) * Actor.GetAbsoluteBoneMatrix(BoneIdx);
                }
                else if (EffectPart.PositionUpdate)
                {
                    Transform = InitialRotation * Matrix4x4.CreateTranslation(Actor.GetAbsoluteBoneMatrix(BoneIdx).Translation) * Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));
                }
                else if (EffectPart.RotateUpdate)
                {
                    Transform = Actor.GetAbsoluteBoneMatrix(BoneIdx) * MathHelpers.Invert(Matrix4x4.CreateTranslation(Actor.GetAbsoluteBoneMatrix(BoneIdx).Translation)) * InitialPosition * Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));
                }
                else
                {
                    //Use starting position and rotation
                    Transform = Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ)) * InitialPosition * InitialRotation;
                }
            }
            else if(UsesExternalSpawn())
            {
                Matrix4x4 offset = Matrix4x4.CreateTranslation(new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ));
                Matrix4x4 spawnPosition = Matrix4x4.CreateTranslation(BacSpawnSource.Translation);
                Matrix4x4 spawnRotation = BacSpawnSource * MathHelpers.Invert(spawnPosition);

                if (EffectPart.PositionUpdate && EffectPart.RotateUpdate)
                {
                    Transform = offset * BacSpawnSource;
                }
                else if (EffectPart.PositionUpdate)
                {
                    Transform = InitialRotation * spawnPosition * offset;
                }
                else if (EffectPart.RotateUpdate)
                {
                    Transform = spawnRotation * InitialPosition * offset;
                }
            }

            //Near and Far fade distance
            if (MathHelpers.FloatEquals(EffectPart.FarFadeDistance, 0))
            {
                DrawThisFrame = true;
            }
            else
            {
                float distanceToCamera = System.Math.Abs(Vector3.Distance(ViewportInstance.Camera.CameraState.Position, Transform.Translation));
                DrawThisFrame = distanceToCamera >= EffectPart.NearFadeDistance && distanceToCamera < EffectPart.FarFadeDistance;
            }

            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
                DrawThisFrame = false;
        }

        protected bool UsesExternalSpawn()
        {
            return EffectPart.AttachementType == EffectPart.Attachment.External ||
                   (EffectPart.AttachementType == EffectPart.Attachment.Bone && string.Equals(EffectPart.ESK, "TRS", StringComparison.OrdinalIgnoreCase)) ||
                   (SpawnedByProjectile && string.IsNullOrWhiteSpace(EffectPart.ESK));
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

        protected Matrix4x4 GetAdjustedTransform()
        {
            //Unsure on AttachmentBone and User. They seem to get different rotations... so something is wrong

            Matrix4x4 transform = Transform;

            if (EffectPart.AttachementType == EffectPart.Attachment.Camera)
            {
                //Place transform directly in front of the camera
                SimdVector3 direction = ViewportInstance.Camera.CameraState.TargetPosition - ViewportInstance.Camera.CameraState.Position;
                SimdVector3 cameraForward = SimdVector3.Normalize(direction);
                SimdVector3 positionInFrontOfCamera = ViewportInstance.Camera.CameraState.Position + (cameraForward * 1f);

                transform.Translation = positionInFrontOfCamera;
            }

            switch (EffectPart.Orientation)
            {
                case EffectPart.OrientationType.None:
                    //Just uses position and no orientation
                    //The game seems to always rotate it by 90 degrees on Y for some reason
                    transform = Matrix4x4.CreateRotationY(MathHelper.PiOver2) * CurrentRotation * Matrix4x4.CreateTranslation(transform.Translation);
                    break;
                case EffectPart.OrientationType.User:
                    if (Actor == null) return Transform;
                    //Effect Position/Rotation + Base Bone of actor, with an additional rotation based on EffectPart.Direction (I_06)
                    Matrix4x4 userMatrix = Matrix4x4.CreateTranslation(transform.Translation) * (Actor.Transform * MathHelpers.Invert(Matrix4x4.CreateTranslation(Actor.Transform.Translation)));

                    //If I_06 was 2, there is no rotation (default direction)
                    if (EffectPart.I_06 == 0)
                        userMatrix = Matrix4x4.CreateRotationY(MathHelper.PiOver2) * userMatrix;
                    else if (EffectPart.I_06 == 1)
                        userMatrix = Matrix4x4.CreateRotationX(MathHelper.PiOver2) * userMatrix;

                    transform = CurrentRotation * userMatrix;
                    break;
                case EffectPart.OrientationType.Camera:
                    //Effect Position + rotate to face camera.
                    transform = CurrentRotation * Matrix4x4.CreateBillboard(transform.Translation, Viewport.Instance.Camera.CameraState.Position, MathHelpers.Up, MathHelpers.Forward);
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
