using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Controls;
using XenoKit.Engine;
using XenoKit.Engine.Animation;
using Xv2CoreLib.EMO;
using Xv2CoreLib.ESK;
using Xv2CoreLib.NSK;
using Xv2CoreLib.Resource;

namespace XenoKit.Inspector.InspectorEntities
{
    public class SkinnedInspectorEntity : InspectorEntity, ISkinned
    {
        public override string FileType => "Skeleton";
        public string DisplayName => System.IO.Path.GetFileName(Path);

        public ESK_File EskFile { get; private set; }
        public NSK_File NskFile { get; private set; }
        public EMO_File EmoFile { get; private set; }

        public AnimationTabView AnimationViewInstance => AnimationTabView.InspectorInstance;
        public Xv2Skeleton Skeleton { get; set; }
        public AnimationPlayer AnimationPlayer { get; set; }

        public float[] EyeIrisLeft_UV { get; set; } = new float[4];
        public float[] EyeIrisRight_UV { get; set; } = new float[4];

        private int[] ScdBoneIndices;
        private Xv2Skeleton cachedParentSkeleton;
        private Xv2Skeleton cachedScdSkeleton;

        public SkinnedInspectorEntity(string path) : base(path)
        {
            Path = path;
            Load();
        }

        public override bool Load()
        {
            switch (System.IO.Path.GetExtension(Path).ToLower())
            {
                case ".esk":
                    EskFile = ESK_File.Load(Path);
                    Skeleton = new Xv2Skeleton(EskFile);
                    break;
                case ".nsk":
                    NskFile = NSK_File.Load(Path);
                    Skeleton = new Xv2Skeleton(NskFile.EskFile);
                    ChildEntities.Add(new MeshInspectorEntity(NskFile, Path));
                    break;
                case ".emo":
                    EmoFile = EMO_File.Load(Path);
                    Skeleton = new Xv2Skeleton(EmoFile.Skeleton);
                    ChildEntities.Add(new MeshInspectorEntity(EmoFile, Path));
                    break;
                default:
                    throw new InvalidOperationException($"SkinnedInspectorEntity.Load: File type \"{System.IO.Path.GetExtension(Path)}\" is not a valid skeleton file.");
            }

            //Create the animation player
            AnimationPlayer?.Dispose();
            AnimationPlayer = new AnimationPlayer(Skeleton, this);

            return true;
        }

        public override bool Save()
        {
            switch (System.IO.Path.GetExtension(Path).ToLower())
            {
                case ".esk":
                    EskFile.Save(Path);
                    break;
                case ".nsk":
                    NskFile.SaveFile(Path);
                    break;
                case ".emo":
                    EmoFile.SaveFile(Path);
                    break;
            }

            return true;
        }

        public override void Update()
        {
            //Reset eye positions to default
            EyeIrisLeft_UV[0] = EyeIrisLeft_UV[1] = EyeIrisRight_UV[0] = EyeIrisRight_UV[1] = 0;

            AnimationPlayer.Update(Matrix.Identity);

            base.Update();

            //Update the all children SCD skeletons
            foreach (InspectorEntity child in ChildEntities)
            {
                if(child is SkinnedInspectorEntity skinnedChild)
                {
                    skinnedChild.ScdUpdate(this);
                }
            }
        }

        public void ScdUpdate(SkinnedInspectorEntity parentSkeleton)
        {
            if(cachedScdSkeleton != Skeleton && cachedParentSkeleton != parentSkeleton.Skeleton)
            {
                SetupScdSkeletonCache(parentSkeleton.Skeleton);
            }

            if(parentSkeleton.Skeleton != null)
            {
                Transform = parentSkeleton.Transform;
                Skeleton.ScdUpdate(parentSkeleton.Skeleton, ScdBoneIndices);
            }
        }

        public void SetupScdSkeletonCache(Xv2Skeleton parentSkeleton)
        {
            ScdBoneIndices = Skeleton.ScdGetBoneIndices(parentSkeleton);
            cachedScdSkeleton = Skeleton;
            cachedParentSkeleton = Skeleton;
        }

        public Matrix GetAbsoluteBoneMatrix(int index)
        {
            return Skeleton.Bones[index].AbsoluteAnimationMatrix * Transform;
        }

    }
}
