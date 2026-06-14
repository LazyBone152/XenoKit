using System;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Vfx;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public class ProjectileInstance : IDisposable
    {
        private const byte SpawnOrientationDefault = 0;
        internal const byte SpawnOrientationUserDirectionValue = 3;

        private readonly Actor actor;
        private readonly Actor attachActor;
        private readonly Move move;
        private readonly BSA_File bsaFile;
        private readonly BSA_Entry bsaEntry;
        private readonly BacEntryInstance bacInstance;
        private readonly BAC_Type9 projectileType;
        private readonly ProjectileInstance parent;
        private readonly bool allowBacConditionPassEntries;
        private readonly Matrix4x4 initialTransform;
        private readonly bool followsLiveAttachTransform;
        private readonly Matrix4x4 initialMotionTransform;
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
        private SimdVector3 currentLocalMovementFrameDelta;

        public bool IsFinished => currentFrame >= endFrame && childProjectiles.Count == 0;
        public Matrix4x4 Transform => transform;
        public float CurrentFrame => currentFrame;

        public ProjectileInstance(BacEntryInstance bacInstance, BAC_Type9 projectileType, BSA_Entry bsaEntry)
            : this(bacInstance, null, bacInstance?.User, GetSpawnActor(bacInstance, projectileType), bacInstance?.SkillMove, null, bsaEntry, projectileType, CreateSpawnTransform(bacInstance, projectileType), 0, true)
        {
        }

        public static ProjectileInstance CreatePreview(Actor actor, Move move, BSA_Entry bsaEntry, BSA_File bsaFile, Matrix4x4 spawnTransform)
        {
            return new ProjectileInstance(null, null, actor, actor, move, bsaFile, bsaEntry, null, spawnTransform, 0, false);
        }

        private ProjectileInstance(BacEntryInstance bacInstance, ProjectileInstance parent, Actor actor, Actor attachActor, Move move, BSA_File bsaFile, BSA_Entry bsaEntry, BAC_Type9 projectileType, Matrix4x4 spawnTransform, int passDepth, bool allowBacConditionPassEntries)
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
            this.allowBacConditionPassEntries = allowBacConditionPassEntries;
            followsLiveAttachTransform = projectileType != null && projectileType.SpawnOrientation == SpawnOrientationDefault;
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
                    () => GetHitboxSweepDelta(x)))
                .ToList() ?? new List<BsaHitboxPreview>();
            motionTransform = followsLiveAttachTransform ? CreateProjectileLocalTransform(projectileType) : spawnTransform;
            initialMotionTransform = motionTransform;
            transform = followsLiveAttachTransform ? CreateWorldTransformFromMotion(motionTransform) : spawnTransform;
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

            if (frameStep <= 0f)
                currentLocalMovementFrameDelta = SimdVector3.Zero;

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
            return CreateProjectileLocalTransform(projectileType) * parentTransform;
        }

        internal static Matrix4x4 CreateProjectileLocalTransform(BAC_Type9 projectileType)
        {
            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(
                MathHelper.ToRadians(projectileType.RotationX),
                MathHelper.ToRadians(projectileType.RotationY),
                MathHelper.ToRadians(projectileType.RotationZ));
            Matrix4x4 position = Matrix4x4.CreateTranslation(new SimdVector3(projectileType.PositionX, projectileType.PositionY, projectileType.PositionZ));

            return rotation * position;
        }

        internal static Matrix4x4 CreateProjectileParentTransform(Actor spawnActor, BAC_Type9 projectileType, Matrix4x4 attachTransform)
        {
            if (projectileType.SpawnOrientation == SpawnOrientationUserDirectionValue)
                return GetUserDirectionParentTransform(spawnActor, attachTransform);

            if (projectileType.SpawnOrientation != SpawnOrientationDefault)
                Log.Add($"Unsupported BSA projectile spawn orientation {projectileType.SpawnOrientation}. Using default orientation.", LogType.Warning);

            return attachTransform;
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
            return CreateProjectileParentTransform(attachActor, projectileType, attachTransform);
        }

        private Matrix4x4 CreateWorldTransformFromMotion(Matrix4x4 localMotionTransform)
        {
            return localMotionTransform * GetCurrentProjectileParentTransform();
        }

        private void RefreshWorldTransform()
        {
            if (followsLiveAttachTransform)
                transform = CreateWorldTransformFromMotion(motionTransform);
        }

        private void Move(float startFrame, float targetFrame)
        {
            // BSA movement entries are state changes. Duration is ignored because the latest started movement row stays active until another one starts.
            float frame = startFrame;
            currentLocalMovementFrameDelta = SimdVector3.Zero;

            while (frame < targetFrame)
            {
                float nextFrame = GetNextMovementBoundary(frame, targetFrame);
                MovementState movement = movements.LastOrDefault(x => x.IsActive(frame));

                if (movement != null)
                {
                    float frameStep = nextFrame - frame;
                    movement.StartIfNeeded();
                    AddHitboxSweepDelta(movement, frameStep);
                    ApplyMovement(movement, frameStep);
                }

                frame = nextFrame;
            }
        }

        private void AddHitboxSweepDelta(MovementState movement, float frameStep)
        {
            movement.StartIfNeeded();
            currentLocalMovementFrameDelta += movement.HitboxVelocity * (frameStep / 60f);
            movement.AdvanceHitboxVelocity(frameStep);
        }

        private SimdVector3 GetHitboxSweepDelta(BSA_Type3 hitbox)
        {
            if (hitbox == null || currentFrame <= hitbox.StartTime)
                return SimdVector3.Zero;

            float startFrame = hitbox.StartTime;
            float endFrame = currentFrame;

            if (hitbox.Duration > 0)
                endFrame = Math.Min(endFrame, hitbox.StartTime + hitbox.Duration);

            if (endFrame <= startFrame)
                return SimdVector3.Zero;

            return GetLocalMovementSweepDelta(startFrame, endFrame);
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

        private SimdVector3 GetLocalMovementSweepDelta(float startFrame, float endFrame)
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
                            movementDelta += movement.HitboxVelocity * (sweepStep / 60f);
                    }

                    movement.AdvanceHitboxVelocity(frameStep);
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
            if (followsLiveAttachTransform)
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
            if (followsLiveAttachTransform)
            {
                Matrix4x4 localMotion = MoveMotionTransform(initialMotionTransform, 0f, Math.Max(0f, frame));
                return CreateWorldTransformFromMotion(localMotion);
            }

            return MoveTransform(initialTransform, 0f, Math.Max(0f, frame));
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
            int lastFrame = expiryFrame;

            foreach (IBsaType type in bsaEntry.IBsaTypes ?? Enumerable.Empty<IBsaType>())
            {
                if (type is BSA_Type1)
                    continue;

                if (type.Duration == 0)
                    continue;

                lastFrame = Math.Max(lastFrame, (int)type.StartTime + type.Duration);
            }

            return lastFrame;
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
            childProjectiles.Add(new ProjectileInstance(bacInstance, this, actor, attachActor, move, bsaFile, entry, null, transform, passDepth + 1, allowBacConditionPassEntries));
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
            private readonly SimdVector3 startHitboxVelocity;
            private readonly SimdVector3 hitboxAcceleration;
            private bool hasStarted;
            private const int IgnoredOption1Unknown2Flag = 0x00000002;
            private const int FreeMovementFlag = 0x00200000;

            public SimdVector3 Velocity { get; set; }
            public SimdVector3 HitboxVelocity { get; private set; }
            public SimdVector3 Acceleration { get; }
            public int RawMotionFlags => movement.I_00;
            public int SimulationMotionFlags => GetSimulationMotionFlags(RawMotionFlags);
            public bool IsIgnoredBySimulation => (RawMotionFlags & IgnoredOption1Unknown2Flag) == IgnoredOption1Unknown2Flag;
            public bool UseWorldSpaceVelocity => (SimulationMotionFlags & FreeMovementFlag) == FreeMovementFlag;
            public ushort StartTime => movement.StartTime;

            public MovementState(BSA_Type1 movement)
            {
                this.movement = movement;
                startVelocity = new SimdVector3(movement.F_08, movement.F_12, -movement.F_04);
                startHitboxVelocity = new SimdVector3(movement.F_08, movement.F_12, movement.F_04);
                Acceleration = new SimdVector3(movement.F_24, movement.F_28, -movement.F_20);
                hitboxAcceleration = new SimdVector3(movement.F_24, movement.F_28, movement.F_20);
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
                HitboxVelocity = startHitboxVelocity;
                hasStarted = true;
            }

            public void AdvanceHitboxVelocity(float frameStep)
            {
                HitboxVelocity += hitboxAcceleration * (frameStep / 60f);
            }

            public MovementState Clone()
            {
                return new MovementState(movement);
            }
        }
    }
}
