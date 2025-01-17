using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.View;
using XenoKit.Engine.Objects;
using XenoKit.Editor;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource.App;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EAN;
using Xv2CoreLib.BAC;
using System.Linq;

namespace XenoKit.Engine.Animation
{
    public class VisualSkeleton : Entity
    {
        public static event EventHandler SelectedBoneChanged;

        private static readonly string[] ForbiddenBones =
        {
            "b_R_ArmHelper",
            "b_L_ArmHelper",
            "b_R_LegHelper",
            "b_L_LegHelper",
            "g_x_CAM",
            "g_x_LND"
        };

        private Actor character;
        public List<VisualBone> visualBones = new List<VisualBone>();

        //Bone Name settings:
        private Color BoneColor = Color.DarkBlue;
        private Color BoneNameColor = Color.BlueViolet;
        private const float FullAlphaDistance = 1f;
        private const float NameRenderDistance = 4f;

        //Visibility context
        private ESK_Skeleton CurrentEanSkeleton = null;
        private EditorTabs CurrentEditorTab = EditorTabs.Animation;
        private IBacBone CurrentBacBoneEntry = null;
        private bool hideBones = SettingsManager.settings.XenoKit_HideLessImportantBones;

        public VisualSkeleton(Actor chara, GameBase gameBase) : base(gameBase)
        {
            character = chara;
            Input.LeftDoubleClick += Input_LeftDoubleClick;
        }

        private void Input_LeftDoubleClick(object sender, EventArgs e)
        {
            if (SceneManager.ShowVisualSkeleton && SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.Action, EditorTabs.BCS_Bodies))
            {
                int selectedBone = GetBoneMouseIsOver();

                if (selectedBone != -1 && SelectedBoneChanged != null)
                {
                    SelectedBoneChanged.Invoke(selectedBone, null);
                }
            }
        }

        public int GetBoneMouseIsOver()
        {
            for(int i = 0; i < visualBones.Count; i++)
            {
                if (visualBones[i].IsMouseOver())
                {
                    //string name = character.Skeleton.Bones[i].Name;
                    //Log.Add($"{name} selected", LogType.Info);
                    return i;
                }
            }

            return -1;
        }

        public void Update(Xv2Bone[] bones)
        {
            if (SceneManager.ShowVisualSkeleton)
            {
                for (int i = visualBones.Count; i < bones.Length; i++)
                    visualBones.Add(new VisualBone(GameBase));

                UpdateVisibilities();
            }
        }

        public void Draw(Xv2Bone[] bones, Xv2Bone[] Bones, Matrix transform)
        {
            if (SceneManager.ShowVisualSkeleton && SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.BCS_Bodies, EditorTabs.Action))
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    Matrix newWorld = bones[i].AbsoluteAnimationMatrix * transform;
                    bool selected = SceneManager.MainGameInstance.CurrentGizmo.IsEnabledOnBone(i);

                    visualBones[i].Draw(newWorld, selected);

                    //Render Bone names
                    if (SettingsManager.Instance.Settings.XenoKit_RenderBoneNames && visualBones[i].IsVisible)
                    {
                        float distance = GameBase.ActiveCameraBase.DistanceFromCamera(newWorld.Translation);

                        if (distance < NameRenderDistance && ((SettingsManager.Instance.Settings.XenoKit_RenderBoneNamesMouseOverOnly && visualBones[i].IsMouseOver()) || selected || !SettingsManager.Instance.Settings.XenoKit_RenderBoneNamesMouseOverOnly))
                        {
                            Vector2 screenSpace = GameBase.ActiveCameraBase.ProjectToScreenPosition(newWorld.Translation);
                            screenSpace = new Vector2(screenSpace.X, screenSpace.Y + 5); //Text must go below the bone, not over

                            if (selected || distance < FullAlphaDistance)
                            {
                                TextRenderer.DrawOnScreenText(character.Skeleton.Bones[i].Name, screenSpace, BoneNameColor);
                            }
                            else
                            {
                                //Text gradually fades with camera distance
                                TextRenderer.DrawOnScreenText(character.Skeleton.Bones[i].Name, screenSpace, new Color(BoneNameColor, (1f - (distance / NameRenderDistance))));
                            }
                        }
                    }
                }
            }
        }

        private void UpdateVisibilities()
        {
            if (SceneManager.Actors[0] == null) return;

            //Update visibilties when:
            //-The editor tab changes
            //-The currently selected EAN file changes
            //-The currently selected bac type changes (for types that implement IBacBone)
            //-The hide bones setting is changed
            if (SceneManager.CurrentSceneState != CurrentEditorTab ||
                (SceneManager.CurrentSceneState == EditorTabs.Animation && Files.Instance.SelectedItem?.SelectedEanFile?.File?.Skeleton != CurrentEanSkeleton) ||
                (SceneManager.CurrentSceneState == EditorTabs.Action && CurrentBacBoneEntry != Controls.BacTab.SelectedIBacBone) ||
                hideBones != SettingsManager.settings.XenoKit_HideLessImportantBones)
            {
                //Set all invisible
                SetAllVisibilities(false);

                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Animation:
                        {
                            ESK_Skeleton eanSkeleton = Files.Instance.SelectedItem?.SelectedEanFile?.File?.Skeleton;

                            if(eanSkeleton != null)
                            {
                                for (int i = 0; i < character.Skeleton.Bones.Length; i++)
                                {
                                    //Hide less important bones
                                    if((ForbiddenBones.Contains(character.Skeleton.Bones[i].Name) || character.Skeleton.Bones[i].Name.Contains("g_C_") || character.Skeleton.Bones[i].Name.Contains("g_R_") || character.Skeleton.Bones[i].Name.Contains("g_R_")) && 
                                        SettingsManager.settings.XenoKit_HideLessImportantBones)
                                    {
                                        visualBones[i].IsVisible = false;
                                        continue;
                                    }

                                    visualBones[i].IsVisible = (eanSkeleton == null) ? false : eanSkeleton.Exists(character.Skeleton.Bones[i].Name);
                                }
                            }

                            CurrentEanSkeleton = eanSkeleton;
                        }
                        break;
                    case EditorTabs.BCS_Bodies:
                        SetAllVisibilities(true);
                        break;
                    case EditorTabs.Action:
                        {
                            if(Controls.BacTab.SelectedIBacBone is IBacBone)
                            {
                                foreach (var bone in Enum.GetValues(typeof(BoneLinks)))
                                {
                                    string boneName = bone.ToString();

                                    for (int i = 0; i < visualBones.Count; i++)
                                    {
                                        if (SceneManager.Actors[0].Skeleton.Bones[i].Name == boneName)
                                        {
                                            visualBones[i].IsVisible = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }

                CurrentEditorTab = SceneManager.CurrentSceneState;
                CurrentBacBoneEntry = Controls.BacTab.SelectedIBacBone;
                hideBones = SettingsManager.settings.XenoKit_HideLessImportantBones;
            }
        }

        private void SetAllVisibilities(bool isVisible)
        {
            foreach (var bone in visualBones)
                bone.IsVisible = isVisible;
        }
    }
}
