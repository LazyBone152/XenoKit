using System;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Vfx;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public class ProjectileInstance : IDisposable
    {
        private const byte SpawnOrientationDefault = 0;
        private const byte SpawnOrientationUserDirection1 = 1;
        internal const byte SpawnOrientationUserDirectionValue = 3;

        private readonly Actor actor;
        private readonly Actor attachActor;
        private readonly Move move;
        private readonly BSA_File bsaFile;
        private readonly BSA_Entry bsaEntry;
        private readonly BacEntryInstance bacInstance;
        private readonly BAC_Type9 projectileType;
        private readonly ProjectileInstance parent;
        private readonly BsaPassReason spawnReason;
        private readonly bool allowBacConditionPassEntries;
        private readonly Matrix4x4 initialTransform;
        private readonly bool canFollowAttachTransform;
        private readonly Matrix4x4 initialMotionTransform;
        private readonly Matrix4x4 initialUserDirectionAttachRotation;
        private readonly List<MovementState> movements;
        private readonly HashSet<BSA_Type6> playedEffects = new HashSet<BSA_Type6>();
        private readonly HashSet<BSA_Type0> playedPassEntries = new HashSet<BSA_Type0>();
        private readonly List<ActiveProjectileEffect> activeEffects = new List<ActiveProjectileEffect>();
        private readonly List<ProjectileInstance> childProjectiles = new List<ProjectileInstance>();
        private readonly List<BsaHitboxPreview> hitboxPreviews;
        private readonly int expiryFrame;
        private readonly int endFrame;
        private readonly int passDepth;
        private const int MaxBsaPassDepth = 16;
        private bool expiryPassStarted;

        private float currentFrame;
        private Matrix4x4 transform;
        private Matrix4x4 motionTransform;
        private bool isAttachedToSource;
        private bool hasDetachedFromSource;
        private float detachFrame;
        private Matrix4x4 detachWorldTransform;

        public bool IsFinished => currentFrame >= endFrame && childProjectiles.Count == 0;
        public Matrix4x4 Transform => transform;
        public float CurrentFrame => currentFrame;

        public ProjectileInstance(BacEntryInstance bacInstance, BAC_Type9 projectileType, BSA_Entry bsaEntry)
            : this(bacInstance, null, bacInstance?.User, GetSpawnActor(bacInstance, projectileType), bacInstance?.SkillMove, null, bsaEntry, projectileType, CreateSpawnTransform(bacInstance, projectileType), 0, true, BsaPassReason.Root)
        {
        }

        public static ProjectileInstance CreatePreview(Actor actor, Move move, BSA_Entry bsaEntry, BSA_File bsaFile, Matrix4x4 spawnTransform)
        {
            return new ProjectileInstance(null, null, actor, actor, move, bsaFile, bsaEntry, null, spawnTransform, 0, false, BsaPassReason.Root);
        }

        private ProjectileInstance(BacEntryInstance bacInstance, ProjectileInstance parent, Actor actor, Actor attachActor, Move move, BSA_File bsaFile, BSA_Entry bsaEntry, BAC_Type9 projectileType, Matrix4x4 spawnTransform, int passDepth, bool allowBacConditionPassEntries, BsaPassReason spawnReason)
        {
            this.bacInstance = bacInstance;
            this.projectileType = projectileType;
            this.parent = parent;
            this.actor = actor;
            this.attachActor = attachActor;
            this.move = move;
            this.bsaFile = bsaFile;
            this.bsaEntry = bsaEntry;
            this.passDepth = passDepth;
            this.spawnReason = spawnReason;
            this.allowBacConditionPassEntries = allowBacConditionPassEntries;
            canFollowAttachTransform = ShouldFollowLiveAttachTransform(projectileType);
            isAttachedToSource = canFollowAttachTransform;
            initialUserDirectionAttachRotation = GetUserDirectionAttachRotation(attachActor, projectileType);
            movements = bsaEntry.IBsaTypes?
                .OfType<BSA_Type1>()
                .OrderBy(x => x.StartTime)
                .Select(x => new MovementState(x))
                .ToList() ?? new List<MovementState>();
            hitboxPreviews = bsaEntry.IBsaTypes?
                .OfType<BSA_Type3>()
                .Select(x => new BsaHitboxPreview(
                    x,
                    () => GetHitboxDrawTransform(x),
                    () => (int)Math.Floor(currentFrame),
                    () => GetHitboxMovementDelta(x)))
                .ToList() ?? new List<BsaHitboxPreview>();
            motionTransform = canFollowAttachTransform ? CreateProjectileLocalTransform(projectileType, attachActor, GetProjectileAttachTransform(attachActor, projectileType)) : spawnTransform;
            initialMotionTransform = motionTransform;
            transform = canFollowAttachTransform ? CreateWorldTransformFromMotion(motionTransform) : spawnTransform;
            initialTransform = transform;
            expiryFrame = Math.Max((int)bsaEntry.I_22, 1);
            endFrame = GetEndFrame();
        }

        public void Update(float frameStep)
        {
            float previousFrame = currentFrame;
            float targetFrame = currentFrame;

            if (frameStep > 0f)
            {
                targetFrame = currentFrame + frameStep;
                Move(previousFrame, targetFrame);
                currentFrame = targetFrame;
            }

            RefreshWorldTransform();

            PlayDueEffects(previousFrame, currentFrame);
            PlayDueBacConditionPassEntries(previousFrame, currentFrame);

            if (frameStep > 0f)
                TryStartPassEntry(BsaPassReason.Expires);

            UpdateActiveEffectTransforms();
            UpdateHitboxes();
            UpdateChildProjectiles(frameStep);
        }

        public void Dispose()
        {
            End(true);
        }

        public void Expire()
        {
            End(false);
        }

        private void End(bool force)
        {
            EndEffects(force);
            EndChildProjectiles(force);
            DisposeHitboxes();
            playedEffects.Clear();
            playedPassEntries.Clear();
        }

        private void EndEffects(bool force)
        {
            foreach (ActiveProjectileEffect effect in activeEffects)
                effect.Effect?.Terminate(force);

            activeEffects.Clear();
        }

        private void EndChildProjectiles(bool force)
        {
            foreach (ProjectileInstance projectile in childProjectiles)
            {
                if (force)
                    projectile.Dispose();
                else
                    projectile.Expire();
            }

            childProjectiles.Clear();
        }

        private void DisposeHitboxes()
        {
            foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                hitboxPreview.Dispose();

            hitboxPreviews.Clear();
        }

        public void Draw()
        {
            foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                hitboxPreview.Draw();

            foreach (ProjectileInstance projectile in childProjectiles)
                projectile.Draw();
        }

        private static Actor GetSpawnActor(BacEntryInstance bacInstance, BAC_Type9 projectileType)
        {
            if (bacInstance == null || projectileType == null)
                return null;

            if (projectileType.SpawnSource == 1 && SceneManager.Actors[1] != null)
                return SceneManager.Actors[1];

            return bacInstance.User;
        }

        private static Matrix4x4 CreateSpawnTransform(BacEntryInstance bacInstance, BAC_Type9 projectileType)
        {
            Actor spawnActor = GetSpawnActor(bacInstance, projectileType);
            return CreateProjectileWorldTransform(spawnActor, projectileType);
        }

        internal static Matrix4x4 CreateProjectileWorldTransform(Actor spawnActor, BAC_Type9 projectileType)
        {
            Matrix4x4 attachTransform = GetProjectileAttachTransform(spawnActor, projectileType);
            Matrix4x4 parentTransform = CreateProjectileParentTransform(spawnActor, projectileType, attachTransform);
            return CreateProjectileLocalTransform(projectileType, spawnActor, attachTransform) * parentTransform;
        }

        internal static Matrix4x4 CreateProjectileLocalTransform(BAC_Type9 projectileType, Actor spawnActor, Matrix4x4 attachTransform)
        {
            Matrix4x4 rotation = CreateProjectileRotation(projectileType, spawnActor, attachTransform);
            Matrix4x4 position = Matrix4x4.CreateTranslation(new SimdVector3(projectileType.PositionX, projectileType.PositionY, projectileType.PositionZ));

            return rotation * position;
        }

        internal static Matrix4x4 CreateProjectileRotation(BAC_Type9 projectileType)
        {
            return CreateProjectileRotation(projectileType, null, Matrix4x4.Identity);
        }

        private static Matrix4x4 CreateProjectileRotation(BAC_Type9 projectileType, Actor spawnActor, Matrix4x4 attachTransform)
        {
            if (projectileType == null)
                return Matrix4x4.Identity;

            if (IsUserDirection1SpawnOrientation(projectileType))
                return Matrix4x4.Identity;

            if (IsUserDirection3SpawnOrientation(projectileType))
                return CreateUserDirection3Rotation(projectileType, spawnActor, attachTransform);

            Matrix4x4 nonYRotation = Matrix4x4.CreateFromYawPitchRoll(
                MathHelper.ToRadians(projectileType.RotationX),
                0f,
                MathHelper.ToRadians(projectileType.RotationZ));
            Matrix4x4 yRotation = Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-projectileType.RotationY));

            return nonYRotation * yRotation;
        }

        private static Matrix4x4 CreateUserDirection3Rotation(BAC_Type9 projectileType, Actor spawnActor, Matrix4x4 attachTransform)
        {
            Matrix4x4 localRotation = Matrix4x4.CreateFromYawPitchRoll(
                MathHelper.ToRadians(projectileType.RotationY),
                MathHelper.ToRadians(projectileType.RotationX),
                MathHelper.ToRadians(projectileType.RotationZ));

            if (!HasProjectileRotation(projectileType) || spawnActor == null)
                return localRotation;

            Matrix4x4 attachRelative = GetUserDirectionAttachRotation(spawnActor, attachTransform);

            if (Matrix4x4.Invert(attachRelative, out Matrix4x4 inverseAttachRelative))
                return attachRelative * localRotation * inverseAttachRelative;

            Log.Add("Could not apply BAC Type 9 User Direction 3 attach-bone rotation because the attach rotation matrix could not be inverted.", LogType.Warning);
            return Matrix4x4.Identity;
        }

        internal static Matrix4x4 CreateProjectileParentTransform(Actor spawnActor, BAC_Type9 projectileType, Matrix4x4 attachTransform)
        {
            if (IsUserDirectionSpawnOrientation(projectileType))
                return GetUserDirectionParentTransform(spawnActor, attachTransform);

            if (projectileType.SpawnOrientation != SpawnOrientationDefault)
                Log.Add($"Unsupported BSA projectile spawn orientation {projectileType.SpawnOrientation}. Using default orientation.", LogType.Warning);

            return attachTransform;
        }

        private static bool ShouldFollowLiveAttachTransform(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   (IsDefaultSpawnOrientation(projectileType) ||
                    IsUserDirectionSpawnOrientation(projectileType));
        }

        private static bool IsDefaultSpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null && projectileType.SpawnOrientation == SpawnOrientationDefault;
        }

        private static bool IsUserDirectionSpawnOrientation(BAC_Type9 projectileType)
        {
            return IsUserDirection1SpawnOrientation(projectileType) ||
                   IsUserDirection3SpawnOrientation(projectileType);
        }

        private static bool IsUserDirection1SpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   projectileType.SpawnOrientation == SpawnOrientationUserDirection1;
        }

        private static bool IsUserDirection3SpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   projectileType.SpawnOrientation == SpawnOrientationUserDirectionValue;
        }

        private static bool HasProjectileRotation(BAC_Type9 projectileType)
        {
            return !MathHelpers.FloatEquals(projectileType.RotationX, 0f) ||
                   !MathHelpers.FloatEquals(projectileType.RotationY, 0f) ||
                   !MathHelpers.FloatEquals(projectileType.RotationZ, 0f);
        }

        private static Matrix4x4 GetProjectileAttachTransform(Actor spawnActor, BAC_Type9 projectileType)
        {
            Matrix4x4 attachTransform = spawnActor?.Transform ?? Matrix4x4.Identity;

            if (spawnActor == null)
                return attachTransform;

            int boneIdx = spawnActor.Skeleton.GetBoneIndex(projectileType.BoneLink.ToString(), true);

            if (boneIdx != -1)
                attachTransform = spawnActor.GetAbsoluteBoneMatrix(boneIdx);

            return attachTransform;
        }

        private static Matrix4x4 GetUserDirectionParentTransform(Actor spawnActor, Matrix4x4 attachTransform)
        {
            if (spawnActor == null)
                return attachTransform;

            Matrix4x4 parentTransform = GetRotationOnly(spawnActor.Transform);
            parentTransform.Translation = attachTransform.Translation;
            return parentTransform;
        }

        private static Matrix4x4 GetRotationOnly(Matrix4x4 transform)
        {
            if (Matrix4x4.Decompose(transform, out _, out System.Numerics.Quaternion rotation, out _))
                return Matrix4x4.CreateFromQuaternion(rotation);

            transform.Translation = SimdVector3.Zero;
            return transform;
        }

        private Matrix4x4 GetCurrentProjectileParentTransform()
        {
            if (projectileType == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachTransform = GetProjectileAttachTransform(attachActor, projectileType);

            if (IsUserDirectionSpawnOrientation(projectileType))
                return GetCurrentUserDirectionParentTransform(attachTransform);

            return CreateProjectileParentTransform(attachActor, projectileType, attachTransform);
        }

        private Matrix4x4 GetCurrentUserDirectionParentTransform(Matrix4x4 attachTransform)
        {
            if (attachActor == null)
                return attachTransform;

            Matrix4x4 currentAttachRotation = GetUserDirectionAttachRotation(attachActor, attachTransform);
            Matrix4x4 attachRotationDelta = GetRotationDelta(initialUserDirectionAttachRotation, currentAttachRotation);
            Matrix4x4 parentTransform = attachRotationDelta * GetRotationOnly(attachActor.Transform);
            parentTransform.Translation = attachTransform.Translation;
            return parentTransform;
        }

        private static Matrix4x4 GetUserDirectionAttachRotation(Actor actor, BAC_Type9 projectileType)
        {
            if (actor == null || projectileType == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachTransform = GetProjectileAttachTransform(actor, projectileType);
            return GetUserDirectionAttachRotation(actor, attachTransform);
        }

        private static Matrix4x4 GetUserDirectionAttachRotation(Actor actor, Matrix4x4 attachTransform)
        {
            if (actor == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachRotation = GetRotationOnly(attachTransform);
            Matrix4x4 actorRotation = GetRotationOnly(actor.Transform);

            if (Matrix4x4.Invert(actorRotation, out Matrix4x4 inverseActorRotation))
                return attachRotation * inverseActorRotation;

            return attachRotation;
        }

        private static Matrix4x4 GetRotationDelta(Matrix4x4 startRotation, Matrix4x4 currentRotation)
        {
            if (Matrix4x4.Invert(startRotation, out Matrix4x4 inverseStartRotation))
                return inverseStartRotation * currentRotation;

            return Matrix4x4.Identity;
        }

        private Matrix4x4 CreateWorldTransformFromMotion(Matrix4x4 localMotionTransform)
        {
            return localMotionTransform * GetCurrentProjectileParentTransform();
        }

        private void RefreshWorldTransform()
        {
            if (isAttachedToSource)
                transform = CreateWorldTransformFromMotion(motionTransform);
        }

        private void Move(float startFrame, float targetFrame)
        {
            // BSA movement entries are state changes. Duration is ignored because the latest started movement row stays active until another one starts.
            float frame = startFrame;

            while (frame < targetFrame)
            {
                float nextFrame = GetNextMovementBoundary(frame, targetFrame);
                MovementState movement = movements.LastOrDefault(x => x.IsActive(frame));

                if (movement != null)
                {
                    float frameStep = nextFrame - frame;
                    movement.StartIfNeeded();

                    if (isAttachedToSource && ShouldDetachFromAttach(movement))
                        DetachFromSource(frame);

                    ApplyMovement(movement, frameStep);
                }

                frame = nextFrame;
            }
        }

        private void DetachFromSource(float frame)
        {
            detachWorldTransform = CreateWorldTransformFromMotion(motionTransform);
            detachFrame = frame;
            hasDetachedFromSource = true;
            transform = detachWorldTransform;
            isAttachedToSource = false;
        }

        private static bool ShouldDetachFromAttach(MovementState movement)
        {
            return movement != null && movement.HasMovement;
        }

        private SimdVector3 GetHitboxMovementDelta(BSA_Type3 hitbox)
        {
            if (hitbox == null || currentFrame <= hitbox.StartTime)
                return SimdVector3.Zero;

            float startFrame = hitbox.StartTime;
            float endFrame = currentFrame;

            if (hitbox.Duration > 0)
                endFrame = Math.Min(endFrame, hitbox.StartTime + hitbox.Duration);

            if (endFrame <= startFrame)
                return SimdVector3.Zero;

            return GetLocalMovementDelta(startFrame, endFrame);
        }

        private Matrix4x4 GetHitboxDrawTransform(BSA_Type3 hitbox)
        {
            if (UsesGrowBounds(hitbox))
                return GetProjectileTransformAtFrame(hitbox.StartTime);

            return transform;
        }

        private static bool UsesGrowBounds(BSA_Type3 hitbox)
        {
            return hitbox != null &&
                   (BAC_Type1.BoundingBoxTypeEnum)(hitbox.I_00 & 0x000F) == BAC_Type1.BoundingBoxTypeEnum.MinMax &&
                   hitbox.I_04 != 0;
        }

        private SimdVector3 GetLocalMovementDelta(float startFrame, float endFrame)
        {
            List<MovementState> replayMovements = movements.Select(x => x.Clone()).ToList();
            SimdVector3 movementDelta = SimdVector3.Zero;
            float frame = 0f;

            while (frame < endFrame)
            {
                float nextFrame = GetNextMovementBoundary(replayMovements, frame, endFrame);
                MovementState movement = replayMovements.LastOrDefault(x => x.IsActive(frame));

                if (movement != null)
                {
                    float frameStep = nextFrame - frame;
                    movement.StartIfNeeded();

                    if (nextFrame > startFrame)
                    {
                        float sweepStart = Math.Max(frame, startFrame);
                        float sweepStep = nextFrame - sweepStart;

                        if (sweepStep > 0f)
                            movementDelta += movement.Velocity * (sweepStep / 60f);
                    }

                    movement.AdvanceVelocity(frameStep);
                }

                frame = nextFrame;
            }

            return movementDelta;
        }

        private float GetNextMovementBoundary(float frame, float targetFrame)
        {
            return GetNextMovementBoundary(movements, frame, targetFrame);
        }

        private static float GetNextMovementBoundary(IEnumerable<MovementState> movementStates, float frame, float targetFrame)
        {
            float boundary = targetFrame;

            foreach (MovementState movement in movementStates)
            {
                if (movement.IsIgnoredBySimulation)
                    continue;

                if (movement.StartTime > frame && movement.StartTime < boundary)
                    boundary = movement.StartTime;
            }

            return boundary;
        }

        private void ApplyMovement(MovementState movement, float frameStep)
        {
            if (isAttachedToSource)
            {
                Matrix4x4 parentTransform = GetCurrentProjectileParentTransform();
                ApplyMovementToFollowedTransform(ref motionTransform, parentTransform, movement, frameStep);
                transform = motionTransform * parentTransform;
                return;
            }

            ApplyMovementToTransform(ref transform, movement, frameStep);
        }

        private static void ApplyMovementToTransform(ref Matrix4x4 targetTransform, MovementState movement, float frameStep)
        {
            movement.StartIfNeeded();

            SimdVector3 worldVelocity = movement.UseWorldSpaceVelocity
                ? movement.Velocity
                : SimdVector3.TransformNormal(movement.Velocity, targetTransform);
            targetTransform.Translation += worldVelocity * (frameStep / 60f);
            movement.Velocity += movement.Acceleration * (frameStep / 60f);
        }

        private static void ApplyMovementToFollowedTransform(ref Matrix4x4 localMotionTransform, Matrix4x4 parentTransform, MovementState movement, float frameStep)
        {
            movement.StartIfNeeded();

            float seconds = frameStep / 60f;

            if (movement.UseWorldSpaceVelocity)
            {
                Matrix4x4 worldTransform = localMotionTransform * parentTransform;
                worldTransform.Translation += movement.Velocity * seconds;

                if (Matrix4x4.Invert(parentTransform, out Matrix4x4 inverseParent))
                    localMotionTransform = worldTransform * inverseParent;
                else
                    localMotionTransform.Translation += movement.Velocity * seconds;
            }
            else
            {
                SimdVector3 localVelocity = SimdVector3.TransformNormal(movement.Velocity, localMotionTransform);
                localMotionTransform.Translation += localVelocity * seconds;
            }

            movement.Velocity += movement.Acceleration * seconds;
        }

        private Matrix4x4 GetProjectileTransformAtFrame(float frame)
        {
            frame = Math.Max(0f, frame);

            if (!canFollowAttachTransform)
                return MoveTransform(initialTransform, 0f, frame);

            if (hasDetachedFromSource && frame >= detachFrame)
                return MoveTransform(detachWorldTransform, detachFrame, frame);

            float firstMovementFrame = GetFirstMovementFrame(frame);

            if (firstMovementFrame < 0f)
            {
                Matrix4x4 localMotion = MoveMotionTransform(initialMotionTransform, 0f, frame);
                return CreateWorldTransformFromMotion(localMotion);
            }

            Matrix4x4 detachMotion = MoveMotionTransform(initialMotionTransform, 0f, firstMovementFrame);
            Matrix4x4 detachTransform = CreateWorldTransformFromMotion(detachMotion);
            return MoveTransform(detachTransform, firstMovementFrame, frame);
        }

        private float GetFirstMovementFrame(float maxFrame)
        {
            MovementState movement = movements
                .Where(x => !x.IsIgnoredBySimulation && x.HasMovement && x.StartTime <= maxFrame)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            return movement?.StartTime ?? -1f;
        }

        private Matrix4x4 MoveTransform(Matrix4x4 startTransform, float startFrame, float targetFrame)
        {
            Matrix4x4 replayTransform = startTransform;
            List<MovementState> replayMovements = movements.Select(x => x.Clone()).ToList();
            float frame = startFrame;

            while (frame < targetFrame)
            {
                float nextFrame = GetNextMovementBoundary(replayMovements, frame, targetFrame);
                MovementState movement = replayMovements.LastOrDefault(x => x.IsActive(frame));

                if (movement != null)
                    ApplyMovementToTransform(ref replayTransform, movement, nextFrame - frame);

                frame = nextFrame;
            }

            return replayTransform;
        }

        private Matrix4x4 MoveMotionTransform(Matrix4x4 startMotionTransform, float startFrame, float targetFrame)
        {
            Matrix4x4 replayMotionTransform = startMotionTransform;
            List<MovementState> replayMovements = movements.Select(x => x.Clone()).ToList();
            float frame = startFrame;

            while (frame < targetFrame)
            {
                float nextFrame = GetNextMovementBoundary(replayMovements, frame, targetFrame);
                MovementState movement = replayMovements.LastOrDefault(x => x.IsActive(frame));

                if (movement != null)
                    ApplyMovementToFollowedTransform(ref replayMotionTransform, GetCurrentProjectileParentTransform(), movement, nextFrame - frame);

                frame = nextFrame;
            }

            return replayMotionTransform;
        }

        private int GetEndFrame()
        {
            int lastFrame = ShouldUseEntryLifetimeForEndFrame() ? expiryFrame : 1;

            foreach (IBsaType type in bsaEntry.IBsaTypes ?? Enumerable.Empty<IBsaType>())
            {
                if (!ShouldCountTypeForEndFrame(type))
                    continue;

                if (type.Duration == 0)
                    continue;

                lastFrame = Math.Max(lastFrame, (int)type.StartTime + type.Duration);
            }

            return lastFrame <= 1 ? expiryFrame : lastFrame;
        }

        private bool ShouldUseEntryLifetimeForEndFrame()
        {
            return parent == null || spawnReason != BsaPassReason.SystemPass;
        }

        private bool ShouldCountTypeForEndFrame(IBsaType type)
        {
            if (type is BSA_Type1)
                return false;

            if (ShouldUseEntryLifetimeForEndFrame())
                return true;

            return type is BSA_Type3 || type is BSA_Type6;
        }

        private void PlayDueEffects(float previousFrame, float targetFrame)
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;
            if (actor == null) return;

            foreach (BSA_Type6 effect in bsaEntry.IBsaTypes?.OfType<BSA_Type6>() ?? Enumerable.Empty<BSA_Type6>())
            {
                if (playedEffects.Contains(effect) || !IsEffectDue(effect, previousFrame, targetFrame))
                    continue;

                if (effect.I_08 == Switch.Off)
                    StopEffect(effect);
                else
                    PlayEffect(effect, GetProjectileTransformAtFrame(effect.StartTime));

                playedEffects.Add(effect);
            }
        }

        private void PlayEffect(BSA_Type6 effect, Matrix4x4 eventProjectileTransform)
        {
            Matrix4x4 offset = CreateEffectOffset(effect);
            Matrix4x4 effectTransform = ApplyEffectOffset(eventProjectileTransform, offset);
            VfxEffect vfxEffect = actor.VfxManager.PlayProjectileEffect(effect, move, actor, effectTransform);

            if (vfxEffect != null)
            {
                activeEffects.Add(new ActiveProjectileEffect(effect, vfxEffect, offset, currentFrame));
            }
        }

        private static Matrix4x4 CreateEffectOffset(BSA_Type6 effect)
        {
            return Matrix4x4.CreateTranslation(new SimdVector3(effect.F_12, effect.F_16, effect.F_20));
        }

        private static Matrix4x4 ApplyEffectOffset(Matrix4x4 projectileTransform, Matrix4x4 effectOffset)
        {
            return effectOffset * projectileTransform;
        }

        private void StopEffect(BSA_Type6 effect)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (!activeEffects[i].Matches(effect))
                    continue;

                activeEffects[i].Effect?.Terminate(false);
                activeEffects.RemoveAt(i);
            }

            parent?.StopEffect(effect);
        }

        private void PlayDueBacConditionPassEntries(float previousFrame, float targetFrame)
        {
            if (!allowBacConditionPassEntries)
                return;

            foreach (BSA_Type0 passEntry in bsaEntry.IBsaTypes?.OfType<BSA_Type0>() ?? Enumerable.Empty<BSA_Type0>())
            {
                if (playedPassEntries.Contains(passEntry) || !IsPassEntryActive(passEntry, previousFrame, targetFrame) || !HasMatchingBacPassCondition(passEntry))
                    continue;

                StartPassEntry(passEntry.BSA_EntryID, BsaPassReason.SystemPass);
                playedPassEntries.Add(passEntry);
            }
        }

        private bool IsEffectDue(BSA_Type6 effect, float previousFrame, float targetFrame)
        {
            return IsTimedTypeDue(effect, previousFrame, targetFrame);
        }

        private bool IsPassEntryActive(BSA_Type0 passEntry, float previousFrame, float targetFrame)
        {
            if (passEntry.Duration == 0)
                return passEntry.StartTime >= previousFrame && passEntry.StartTime <= targetFrame;

            float passStart = passEntry.StartTime;
            float passEnd = passStart + passEntry.Duration;

            if (targetFrame == 0f)
                return passStart == 0f;

            return targetFrame >= passStart && previousFrame < passEnd;
        }

        private bool HasMatchingBacPassCondition(BSA_Type0 passEntry)
        {
            return bacInstance?.HasActiveBsaPassCondition(passEntry.F_08) == true;
        }

        private bool IsTimedTypeDue(IBsaType type, float previousFrame, float targetFrame)
        {
            if (targetFrame == 0f)
                return type.StartTime == 0;

            if (previousFrame <= 0f && type.StartTime == 0)
                return true;

            return type.StartTime > previousFrame && type.StartTime <= targetFrame;
        }

        private void UpdateActiveEffectTransforms()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Effect == null || activeEffects[i].Effect.IsDestroyed)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (activeEffects[i].SkipTransformUpdateOnCreatedFrame && activeEffects[i].CreatedFrame == currentFrame)
                {
                    activeEffects[i].SkipTransformUpdateOnCreatedFrame = false;
                    continue;
                }

                Matrix4x4 effectTransform = ApplyEffectOffset(transform, activeEffects[i].Offset);
                activeEffects[i].Effect.SetExternalTransform(effectTransform);
            }
        }

        private void UpdateHitboxes()
        {
            foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                hitboxPreview.Update();
        }

        private void UpdateChildProjectiles(float frameStep)
        {
            for (int i = childProjectiles.Count - 1; i >= 0; i--)
            {
                childProjectiles[i].Update(frameStep);

                if (!childProjectiles[i].IsFinished)
                    continue;

                childProjectiles[i].Expire();
                childProjectiles.RemoveAt(i);
            }
        }

        private void TryStartPassEntry(BsaPassReason reason)
        {
            if (reason == BsaPassReason.Expires)
            {
                if (expiryPassStarted || currentFrame < expiryFrame)
                    return;

                expiryPassStarted = true;
                StartPassEntry(bsaEntry.Expires, reason);
            }
        }

        private void StartPassEntry(ushort entryId, BsaPassReason reason)
        {
            if (entryId == ushort.MaxValue || passDepth >= MaxBsaPassDepth)
                return;

            if (!TryGetPassEntry(entryId, out BSA_Entry entry))
                return;

            entry.InitializeIBsaTypes();
            childProjectiles.Add(new ProjectileInstance(bacInstance, this, actor, attachActor, move, bsaFile, entry, null, transform, passDepth + 1, allowBacConditionPassEntries, reason));
        }

        private bool TryGetPassEntry(ushort entryId, out BSA_Entry entry)
        {
            entry = GetBsaEntries()?.FirstOrDefault(bsaEntry => bsaEntry.SortID == entryId);
            return entry != null;
        }

        private IEnumerable<BSA_Entry> GetBsaEntries()
        {
            return bsaFile?.BSA_Entries ?? move?.Files?.BsaFile?.File?.BSA_Entries;
        }

        private enum BsaPassReason
        {
            Root,
            Expires,
            ImpactProjectile,
            ImpactEnemy,
            ImpactGround,
            SystemPass
        }

        private class ActiveProjectileEffect
        {
            public BSA_Type6 Source { get; }
            public VfxEffect Effect { get; }
            public Matrix4x4 Offset { get; }
            public float CreatedFrame { get; }
            public bool SkipTransformUpdateOnCreatedFrame { get; set; }

            public ActiveProjectileEffect(BSA_Type6 source, VfxEffect effect, Matrix4x4 offset, float createdFrame)
            {
                Source = source;
                Effect = effect;
                Offset = offset;
                CreatedFrame = createdFrame;
                SkipTransformUpdateOnCreatedFrame = true;
            }

            public bool Matches(BSA_Type6 effect)
            {
                return Source.EepkType == effect.EepkType &&
                       Source.SkillID == effect.SkillID &&
                       Source.EffectID == effect.EffectID;
            }
        }

        private class MovementState
        {
            private readonly BSA_Type1 movement;
            private readonly SimdVector3 startVelocity;
            private bool hasStarted;
            private const int IgnoredOption1Unknown2Flag = 0x00000002;
            private const int FreeMovementFlag = 0x00200000;

            public SimdVector3 Velocity { get; set; }
            public SimdVector3 Acceleration { get; }
            public int RawMotionFlags => movement.I_00;
            public int SimulationMotionFlags => GetSimulationMotionFlags(RawMotionFlags);
            public bool IsIgnoredBySimulation => (RawMotionFlags & IgnoredOption1Unknown2Flag) == IgnoredOption1Unknown2Flag;
            public bool UseWorldSpaceVelocity => (SimulationMotionFlags & FreeMovementFlag) == FreeMovementFlag;
            public bool HasMovement
            {
                get
                {
                    return !MathHelpers.FloatEquals(movement.F_04, 0f) ||
                           !MathHelpers.FloatEquals(movement.F_08, 0f) ||
                           !MathHelpers.FloatEquals(movement.F_12, 0f);
                }
            }
            public ushort StartTime => movement.StartTime;

            public MovementState(BSA_Type1 movement)
            {
                this.movement = movement;
                startVelocity = new SimdVector3(movement.F_08, movement.F_12, -movement.F_04);
                Acceleration = new SimdVector3(movement.F_24, movement.F_28, -movement.F_20);
            }

            private static int GetSimulationMotionFlags(int flags)
            {
                return flags & ~IgnoredOption1Unknown2Flag;
            }

            public bool IsActive(float frame)
            {
                if (IsIgnoredBySimulation)
                    return false;

                if (frame < movement.StartTime)
                    return false;

                return true;
            }

            public void StartIfNeeded()
            {
                if (hasStarted)
                    return;

                Velocity = startVelocity;
                hasStarted = true;
            }

            public void AdvanceVelocity(float frameStep)
            {
                Velocity += Acceleration * (frameStep / 60f);
            }

            public MovementState Clone()
            {
                return new MovementState(movement);
            }
        }
    }
}
