using LB_Common.Forms;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Model;
using XenoKit.Views;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;

namespace EEPK_Organiser.View
{
    public partial class EepkEditor
    {
        partial void XenoKit_Init()
        {
            XenoKitAvailable = true;
            XenoKitVisible = Visibility.Visible;
            NotifyPropertyChanged(nameof(XenoKitVisible));

            pbindDataGrid.SelectionChanged += PbindDataGrid_SelectionChanged;
            tbindDataGrid.SelectionChanged += TbindDataGrid_SelectionChanged;
            cbindDataGrid.SelectionChanged += CbindDataGrid_SelectionChanged;
            emoDataGrid.SelectionChanged += EmoDataGrid_SelectionChanged;
            lightDataGrid.SelectionChanged += LightDataGrid_SelectionChanged;
        }

        partial void XenoKit_OnSelectedTabChanged(Tabs tab)
        {
            Viewport.Instance?.VfxPreview.ClearPreview();

            if (effectContainerFile == null)
                return;

            switch (tab)
            {
                case Tabs.Effect:
                    PlaySelectedEffect();
                    SetEffectPartGizmo();
                    break;
                case Tabs.Pbind:
                    PlayAsset(GetSelectedAsset(AssetType.PBIND));
                    break;
                case Tabs.Tbind:
                    PlayAsset(GetSelectedAsset(AssetType.TBIND));
                    break;
                case Tabs.Cbind:
                    PlayAsset(GetSelectedAsset(AssetType.CBIND));
                    break;
                case Tabs.Emo:
                    PlayAsset(GetSelectedAsset(AssetType.EMO));
                    break;
                case Tabs.Light:
                    PlayAsset(GetSelectedAsset(AssetType.LIGHT));
                    break;
            }
        }

        partial void XenoKit_OnEffectPartSelectionChange()
        {
            SetEffectPartGizmo();
        }

        partial void XenoKit_OnEffectSelectionChange()
        {
            PlaySelectedEffect();
            SetEffectPartGizmo();
        }


        private void SetEffectPartGizmo()
        {
            if (Viewport.Instance == null) return;

            EffectPart effectPart = SelectedEffectPart;

            if (effectPart != null)
            {
                Viewport.Instance.EffectPartGizmo.SetContext(effectPart);
            }
            else
            {
                Viewport.Instance.EffectPartGizmo.RemoveContext();
            }

        }

        partial void PlaySelectedEffect()
        {
            if (SelectedEffect != null && Viewport.Instance != null)
            {
                Viewport.Instance.VfxPreview.PreviewEffect(SelectedEffect);
            }
        }

        partial void PlayAsset(Asset asset)
        {
            if (Viewport.Instance != null && asset != null)
                Viewport.Instance.VfxPreview.PreviewAsset(asset);
        }

        partial void AssetEmoOpenEditor(Asset asset)
        {
            EffectFile emb = asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMB);
            EffectFile emm = asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMM);
            EffectFile emo = asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMO);

            if(emo == null || emo.EmoFile == null)
            {
                MessagePrompt.Show("No emo file found.", "No Model File", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            if (emb == null || emb.EmbFile == null)
            {
                MessagePrompt.Show("The Model Editor requires a .emb file.", "No Textures", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            if (emm == null || emm.EmmFile == null)
            {
                MessagePrompt.Show("The Model Editor requires a .emm file.", "No Materials", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            Xv2ModelFile compiledModel = Viewport.Instance.CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(emo.EmoFile);
            ModelScene modelScene = Viewport.Instance.CompiledObjectManager.GetCompiledObject<ModelScene>(compiledModel);

            if (!TabManager.FocusTab(modelScene))
            {
                modelScene.SetFiles(XenoKit.Engine.Shader.ShaderType.Chara, emb.EmbFile, emm.EmmFile);
                modelScene.SetPaths(true, $"{effectContainerFile.Directory}/{emo.FullFileName}", $"{effectContainerFile.Directory}/{emb.FullFileName}", $"{effectContainerFile.Directory}/{emm.FullFileName}", null);

                ModelSceneView modelSceneView = new ModelSceneView(modelScene);

                TabManager.AddTab($"{Path.GetFileName(emo.FullFileName)}", modelSceneView, modelScene, Files.Instance.SelectedItem, null);
            }
        }

        private void PbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(GetSelectedAsset(AssetType.PBIND));
        }

        private void TbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(GetSelectedAsset(AssetType.TBIND));
        }

        private void CbindDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(GetSelectedAsset(AssetType.CBIND));
        }

        private void EmoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(GetSelectedAsset(AssetType.EMO));
        }

        private void LightDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayAsset(GetSelectedAsset(AssetType.LIGHT));
        }
    }
}
