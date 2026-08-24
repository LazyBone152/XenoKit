using Microsoft.Xna.Framework;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using static Xv2CoreLib.ValuesDictionary.BAC;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.View
{
    public class CameraAnimInstance
    {
        bool hasBacData;

        public readonly bool AutoTerminate;
        private float _currentFrame = 0;
        public int StartFrame = 0;
        public int EndFrame = 0;
        public EAN_File EanFile;
        public readonly EAN_Animation Animation;
        public readonly CameraTarget cameraTarget;
        public readonly BacCameraSettings bacCameraSettings;

        public Actor Actor;

        public float CurrentFrame
        {
            get
            {
                return _currentFrame;
            }
            set
            {
                _currentFrame = value;
                SceneManager.InvokeCameraCurrentFrameChangedEvent();
            }
        }
        public int CurrentAnimDuration
        {
            get
            {
                if (Animation != null)
                    return Animation.FrameCount - 1;
                return 0;
            }
        }

        public CameraAnimInstance(EAN_File eanFile, EAN_Animation anim, BAC_Type10 bacCamEntry, bool autoTerminate, int targetCharacterIndex, Actor actor)
        {
            EanFile = eanFile;
            AutoTerminate = autoTerminate;
            Animation = anim;
            StartFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame : 0;
            EndFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame + bacCamEntry.Duration - 1 : anim.FrameCount - 1;
            _currentFrame = StartFrame;
            hasBacData = bacCamEntry != null;
            Actor = actor;

            if (bacCamEntry != null)
            {
                bacCameraSettings = new BacCameraSettings(this, bacCamEntry);
                cameraTarget = new CameraTarget(targetCharacterIndex, bacCamEntry.BoneLink);
            }
            else
            {
                bacCameraSettings = new BacCameraSettings(this);
                cameraTarget = new CameraTarget(targetCharacterIndex, 0);
            }

        }

        public void UpdateValues()
        {
            if (!hasBacData)
                EndFrame = Animation.FrameCount - 1;
        }
    }

    public struct CameraTarget
    {
        public readonly int CharacterIndex;
        public readonly string _bone;
        public string Bone
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_bone)) return "b_C_Base";
                return _bone;
            }
        }

        public CameraTarget(int charIndex, BoneLinks bone)
        {
            CharacterIndex = charIndex;
            if (!BoneNames.TryGetValue(bone, out _bone))
                _bone = "b_C_Base";
        }
    }

    public struct BacCameraSettings
    {
        private const float BacModifierHorizontalOffsetScale = 1f / 6f;

        private readonly CameraAnimInstance ParentInstance;
        public bool Enabled;
        public ushort GlobalDuration;
        public ushort PosXDuration;
        public ushort PosYDuration;
        public ushort PosZDuration;
        public ushort RotXDuration;
        public ushort RotYDuration;
        public ushort RotZDuration;
        public ushort DispXZDuration;
        public ushort DispZYDuration;
        public ushort FovDuration;

        public float PosX;
        public float PosY;
        public float PosZ;
        public float RotX;
        public float RotY;
        public float RotZ; //Roll
        public float FoV;
        public float DispXZ;
        public float DispZY;

        //Interpolated Values
        public float CurrentFoV
        {
            get
            {
                return FoV * GetFactor(FovDuration);
            }
        }
        public float CurrentPosX
        {
            get
            {
                return PosX * GetFactor(PosXDuration);
            }
        }
        public float CurrentPosY
        {
            get
            {
                return PosY * GetFactor(PosYDuration);
            }
        }
        public float CurrentPosZ
        {
            get
            {
                return PosZ * GetFactor(PosZDuration);
            }
        }
        public float CurrentDispXZ => DispXZ * GetFactor(DispXZDuration);
        public float CurrentDispZY => DispZY * GetFactor(DispZYDuration);
        public float CurrentRotX
        {
            get
            {
                return RotX * GetFactor(RotXDuration);
            }
        }
        public float CurrentRotY
        {
            get
            {
                return RotY * GetFactor(RotYDuration);
            }
        }
        public float CurrentRotZ
        {
            get
            {
                return RotZ * GetFactor(RotZDuration);
            }
        }

        public BacCameraSettings(CameraAnimInstance camera)
        {
            ParentInstance = camera;
            Enabled = false;
            PosX = 0;
            PosY = 0;
            PosZ = 0;
            RotX = 0;
            RotY = 0;
            RotZ = 0;
            DispXZ = 0;
            DispZY = 0;
            FoV = 0;
            GlobalDuration = 0;
            PosXDuration = 0;
            PosYDuration = 0;
            PosZDuration = 0;
            RotXDuration = 0;
            RotYDuration = 0;
            RotZDuration = 0;
            FovDuration = 0;
            DispXZDuration = 0;
            DispZYDuration = 0;
        }

        public BacCameraSettings(CameraAnimInstance camera, BAC_Type10 bacCameraEntry)
        {
            ParentInstance = camera;
            Enabled = bacCameraEntry.EnableTransformModifiers;
            PosX = bacCameraEntry.PositionX;
            PosY = bacCameraEntry.PositionY;
            PosZ = bacCameraEntry.PositionZ;
            RotX = bacCameraEntry.RotationX;
            RotY = bacCameraEntry.RotationY;
            RotZ = bacCameraEntry.RotationZ;
            DispXZ = bacCameraEntry.DisplacementXZ;
            DispZY = bacCameraEntry.DisplacementZY;
            FoV = bacCameraEntry.FieldOfView;
            GlobalDuration = bacCameraEntry.GlobalModiferDuration;
            PosXDuration = bacCameraEntry.PositionX_Duration;
            PosYDuration = bacCameraEntry.PositionY_Duration;
            PosZDuration = bacCameraEntry.PositionZ_Duration;
            RotXDuration = bacCameraEntry.RotationX_Duration;
            RotYDuration = bacCameraEntry.RotationY_Duration;
            RotZDuration = bacCameraEntry.RotationZ_Duration;
            FovDuration = bacCameraEntry.FieldOfView_Duration;
            DispXZDuration = bacCameraEntry.DisplacementXZ_Duration;
            DispZYDuration = bacCameraEntry.DisplacementZY_Duration;
        }

        public void ApplyTo(ref SimdVector3 position, ref SimdVector3 targetPosition)
        {
            SimdVector3 backward = position - targetPosition;
            float originalDistance = backward.Length();

            if (originalDistance <= float.Epsilon)
                return;

            backward /= originalDistance;

            float modifiedDistance = originalDistance + CurrentPosZ;
            float currentDispXZ = CurrentDispXZ;
            float currentDispZY = CurrentDispZY;

            SimdVector3 modifiedBackward = RotateCameraDirection(backward, CurrentRotY, CurrentRotX);

            SimdVector3 modifiedPosition = targetPosition + modifiedBackward * modifiedDistance;
            SimdVector3 modifiedTargetPosition = targetPosition;

            SimdVector3 positionOffset = GetCameraRight(modifiedBackward) * (CurrentPosX * BacModifierHorizontalOffsetScale);
            modifiedPosition += positionOffset;
            modifiedTargetPosition += positionOffset;

            if (currentDispXZ != 0f || currentDispZY != 0f)
            {
                SimdVector3 aim = RotateCameraDirection(-modifiedBackward, currentDispXZ, currentDispZY);
                modifiedTargetPosition = modifiedPosition + aim * modifiedDistance;
            }

            //PosY raises the whole shot, so it moves the target as well
            SimdVector3 heightOffset = MathHelpers.Up * CurrentPosY;
            modifiedPosition += heightOffset;
            modifiedTargetPosition += heightOffset;

            float factor = CurrentGlobalFactor();
            position = SimdVector3.Lerp(position, modifiedPosition, factor);
            targetPosition = SimdVector3.Lerp(targetPosition, modifiedTargetPosition, factor);

        }

        public float GetCurrentFoV()
        {
            return CurrentFoV * CurrentGlobalFactor();
        }

        public float GetCurrentRoll()
        {
            return CurrentRotZ * CurrentGlobalFactor();
        }

        private SimdVector3 GetCameraRight(SimdVector3 backward)
        {
            SimdVector3 right = SimdVector3.Cross(MathHelpers.Up, backward);
            float rightLength = right.Length();

            return rightLength > float.Epsilon ? right / rightLength : right;
        }

        //Yaw comes first, because the pitch axis is the right vector that the yaw produces
        private SimdVector3 RotateCameraDirection(SimdVector3 direction, float yawDegrees, float pitchDegrees)
        {
            SimdVector3 yawed = direction;

            if (yawDegrees != 0f)
            {
                SimdQuaternion yawRotation = SimdQuaternion.CreateFromAxisAngle(MathHelpers.Up, MathHelper.ToRadians(yawDegrees));
                yawed = SimdVector3.Transform(yawed, yawRotation);
            }

            if (pitchDegrees == 0f)
                return yawed;

            SimdQuaternion pitchRotation = SimdQuaternion.CreateFromAxisAngle(GetCameraRight(yawed), MathHelper.ToRadians(pitchDegrees));

            return SimdVector3.Normalize(SimdVector3.Transform(yawed, pitchRotation));
        }

        private float CurrentGlobalFactor()
        {
            return GetFactor(GlobalDuration);
        }

        private float GetFactor(float duration)
        {
            if (ParentInstance.CurrentFrame - ParentInstance.StartFrame > duration || duration == 0) return 1f;
            return (ParentInstance.CurrentFrame - ParentInstance.StartFrame) / duration;
        }
    }

}
