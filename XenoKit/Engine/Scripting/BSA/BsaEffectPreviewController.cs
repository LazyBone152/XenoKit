using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XenoKit.Editor;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Scripting.BSA
{
    // The BSA tab uses ProjectileInstance so movement, hitboxes, and effects follow the same transform path as BAC Type9 projectiles.
    public class BsaEffectPreviewController
    {
        private ProjectileInstance projectile;
        private BSA_Entry entry;
        private BSA_File bsaFile;
        private Move move;
        private int duration;
        private bool isActive;
        private int playRequestId;

        public int CurrentFrame => projectile != null ? (int)Math.Floor(projectile.CurrentFrame) : 0;
        public int Duration => duration;

        public static BsaEffectPreviewController Instance { get; } = new BsaEffectPreviewController();

        private BsaEffectPreviewController()
        {
        }

        public async void Play(BSA_Entry bsaEntry, Move selectedMove, BSA_File selectedBsaFile)
        {
            Stop();
            int requestId = ++playRequestId;

            if (bsaEntry == null || selectedMove == null || selectedBsaFile?.BSA_Entries?.Contains(bsaEntry) != true)
                return;

            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation && !SettingsManager.Instance.Settings.XenoKit_HitboxSimulation)
                return;

            await SceneManager.AsyncEnsureActorIsSet(0);

            if (requestId != playRequestId ||
                !SceneManager.IsOnTab(EditorTabs.Projectile) ||
                SceneManager.Actors[0] == null ||
                !ReferenceEquals(Files.Instance.SelectedItem?.SelectedBsaFile?.File, selectedBsaFile) ||
                selectedBsaFile.BSA_Entries?.Contains(bsaEntry) != true)
                return;

            entry = bsaEntry;
            bsaFile = selectedBsaFile;
            move = selectedMove;
            duration = GetPreviewDuration(entry);
            projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);
            isActive = true;
        }

        public void Stop()
        {
            playRequestId++;
            isActive = false;
            projectile?.Dispose();
            projectile = null;
            entry = null;
            bsaFile = null;
            move = null;
            duration = 0;
            Viewport.Instance?.VfxManager.StopEffects();
        }

        public void Update()
        {
            if (!isActive)
                return;

            if (entry == null || bsaFile == null || move == null || !SceneManager.IsOnTab(EditorTabs.Projectile))
            {
                Stop();
                return;
            }

            if (!Viewport.Instance.IsPlaying)
            {
                projectile?.Update(0f);
                return;
            }

            projectile?.Update(1f);

            if (projectile == null || projectile.IsFinished)
            {
                Viewport.Instance?.VfxManager.StopEffects();
                projectile?.Dispose();
                projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);
            }
        }

        public void SeekNextFrame()
        {
            if (!isActive)
                return;

            if (CurrentFrame < duration)
                Seek(CurrentFrame + 1);
            else
                Seek(0);
        }

        public void SeekPrevFrame()
        {
            if (!isActive)
                return;

            if (CurrentFrame > 0)
                Seek(CurrentFrame - 1);
            else
                Seek(duration);
        }

        public void Seek(int frame)
        {
            if (!isActive || entry == null || bsaFile == null || move == null || SceneManager.Actors[0] == null)
                return;

            int targetFrame = Math.Max(0, Math.Min(frame, duration));

            if (projectile != null && targetFrame >= CurrentFrame)
            {
                while (CurrentFrame < targetFrame)
                    AdvanceOneFrame();

                return;
            }

            Viewport.Instance?.VfxManager.StopEffects();
            projectile?.Dispose();
            projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);

            for (int replayFrame = 0; replayFrame < targetFrame; replayFrame++)
                AdvanceOneFrame();
        }

        private void AdvanceOneFrame()
        {
            projectile?.Update(1f);
            Viewport.Instance?.VfxManager.Simulate();
        }

        public void Draw()
        {
            projectile?.Draw();
        }

        private static int GetPreviewDuration(BSA_Entry bsaEntry)
        {
            int effectDuration = bsaEntry.IBsaTypes?
                .OfType<BSA_Type6>()
                .Select(effect => (int)effect.StartTime + effect.Duration)
                .DefaultIfEmpty(0)
                .Max() ?? 0;

            int hitboxDuration = bsaEntry.IBsaTypes?
                .OfType<BSA_Type3>()
                .Select(hitbox => (int)hitbox.StartTime + hitbox.Duration)
                .DefaultIfEmpty(0)
                .Max() ?? 0;

            return Math.Max(Math.Max(Math.Max((int)bsaEntry.I_22, effectDuration), hitboxDuration), 1);
        }
    }
}
