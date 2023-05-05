using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Editor;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine.Animation
{
    public class VisualSkeleton
    {
        public static event EventHandler SelectedBoneChanged;

        private Character character;
        public List<VisualBone> visualBones = new List<VisualBone>();

        private bool refreshVisibile = false;

        public VisualSkeleton(Character chara)
        {
            character = chara;
            Input.LeftDoubleClick += Input_LeftDoubleClick;
        }

        private void Input_LeftDoubleClick(object sender, EventArgs e)
        {
            if (SceneManager.ShowVisualSkeleton && SceneManager.CurrentSceneState == EditorTabs.Animation)
            {
                if (SceneManager.CurrentSceneState != EditorTabs.Animation) return;

                int selectedBone = GetBoneMouseIsOver();

                if (selectedBone != -1 && SelectedBoneChanged != null)
                {
                    SelectedBoneChanged.Invoke(selectedBone, null);
                }
            }
        }

        public void SetInvisible()
        {
            foreach (var bone in visualBones)
                bone.IsVisible = false;
        }

        private void SetVisible()
        {
            ESK_Skeleton eanSkeleton = Files.Instance.SelectedItem?.SelectedEanFile?.File?.Skeleton;

            for(int i = 0; i < character.Skeleton.Bones.Length; i++)
            {
                visualBones[i].IsVisible = (eanSkeleton == null) ? false : eanSkeleton.Exists(character.Skeleton.Bones[i].Name);
            }
        }

        public int GetBoneMouseIsOver()
        {
            for(int i = 0; i < visualBones.Count; i++)
            {
                if (visualBones[i].IsMouseOver())
                {
                    string name = character.Skeleton.Bones[i].Name;
                    //Log.Add($"{name} selected", LogType.Info);
                    return i;
                }
            }

            return -1;
        }

        public void Update(Matrix[] boneMatrices)
        {
            if (SceneManager.ShowVisualSkeleton)
            {
                for (int i = visualBones.Count; i < boneMatrices.Length; i++)
                    visualBones.Add(new VisualBone());

                if (refreshVisibile)
                {
                    SetVisible();
                    refreshVisibile = false;
                }
            }
        }

        public void Draw(Matrix[] boneMatrices, Bone[] Bones, Camera camera, Matrix transform)
        {
            if (SceneManager.ShowVisualSkeleton && SceneManager.CurrentSceneState == EditorTabs.Animation)
            {
                for (int i = 0; i < boneMatrices.Length; i++)
                {
                    if (SceneManager.ResolveLeftHandSymetry)
                    {
                        visualBones[i].Draw(camera, (boneMatrices[i] * transform) * Matrix.CreateScale(-1f, 1f, 1f), SceneManager.AnimatorGizmo.IsEnabledOnBone(i));
                    }
                    else
                    {
                        visualBones[i].Draw(camera, (boneMatrices[i] * transform), SceneManager.AnimatorGizmo.IsEnabledOnBone(i));
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the visibility of each bone on the next frame. Bones not in the currently selected ean file will be made invisible.
        /// </summary>
        public void RefreshVisibilities()
        {
            refreshVisibile = true;
        }
    }
}
