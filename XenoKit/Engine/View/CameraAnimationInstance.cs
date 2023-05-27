using Microsoft.Xna.Framework;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using static Xv2CoreLib.ValuesDictionary.BAC;

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

        public CameraAnimInstance(EAN_File eanFile, EAN_Animation anim, BAC_Type10 bacCamEntry, bool autoTerminate, int targetCharacterIndex)
        {
            EanFile = eanFile;
            AutoTerminate = autoTerminate;
            Animation = anim;
            StartFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame : 0;
            EndFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame + bacCamEntry.Duration - 1 : anim.FrameCount - 1;
            _currentFrame = StartFrame;
            hasBacData = bacCamEntry != null;

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
        public float DispXZ; //Not implementing
        public float DispZY; //Not implementing

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
                return (PosX) * GetFactor(PosXDuration);
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

        public bool IsRotationReversed;

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

            EAN_AnimationComponent pos = camera.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Position);
            IsRotationReversed = pos.GetKeyframeValue(0, Axis.Z) > 0f;
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

            EAN_AnimationComponent pos = camera.Animation.GetNode("Node").GetComponent(EAN_AnimationComponent.ComponentType.Position);
            IsRotationReversed = pos.GetKeyframeValue(0, Axis.Z) > 0f;
        }

        public Vector3 GetCurrentPosition(Vector3 position, Vector3 targetPosition)
        {
            Vector3 forward = targetPosition - position;
            forward.Normalize();

            Vector3 posToMove = new Vector3(CurrentPosX, CurrentPosY, CurrentPosZ);
            posToMove = Vector3.Transform(posToMove, Matrix.CreateWorld(position, forward, Vector3.Up));

            return (posToMove - position) * CurrentGlobalFactor();
        }

        public float GetCurrentFoV()
        {
            return CurrentFoV * CurrentGlobalFactor();
        }

        public float GetCurrentRoll(Vector3 cameraPosition)
        {
            return IsRotationReversed ? (-CurrentRotZ) * CurrentGlobalFactor() : CurrentRotZ * CurrentGlobalFactor();
        }

        public Vector3 GetCurrentRotation(Vector3 position, Vector3 targetPosition)
        {
            //Calculate the extra position needed to perform the rotation
            //Calculate the WHOLE thing first, then multiply it by CurrentFactor
            //This is how it is done in XV2. The result is multiplied, not the RotX,Y,Z values. (which results in the interpolation not actually rotating... just goes from point a to point b)

            Vector3 newPosition;

            //Rotate Y
            Vector3 temp = position - targetPosition;
            temp = Vector3.Transform(temp, Matrix.CreateRotationY(MathHelper.ToRadians(CurrentRotY)));
            newPosition = targetPosition + temp;

            //Rotate X
            temp = newPosition - targetPosition;

            //CurrentRotX needs to be inverted based on the cameras Z position, else the rotation will happen in the wrong direction if the camera is behind the target
            temp = IsRotationReversed ? Vector3.Transform(temp, Matrix.CreateRotationX(MathHelper.ToRadians(CurrentRotX))) : Vector3.Transform(temp, Matrix.CreateRotationX(MathHelper.ToRadians(-CurrentRotX)));
            newPosition = targetPosition + temp;

            return (newPosition - position) * CurrentGlobalFactor();
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
