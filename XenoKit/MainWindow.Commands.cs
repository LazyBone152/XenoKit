using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Forms;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SAV;

namespace XenoKit
{
    public partial class MainWindow
    {
        public RelayCommand LoadSuperSkillCommand => new RelayCommand(LoadSuperSkill);
        public RelayCommand LoadUltimateSkillCommand => new RelayCommand(LoadUltimateSkill);
        public RelayCommand LoadEvasiveSkillCommand => new RelayCommand(LoadEvasiveSkill);
        public RelayCommand LoadBlastSkillCommand => new RelayCommand(LoadBlastSkill);
        public RelayCommand LoadAwokenSkillCommand => new RelayCommand(LoadAwokenSkill);
        public RelayCommand LoadMovesetCommand => new RelayCommand(LoadMoveset);
        public RelayCommand LoadCharacterCommand => new RelayCommand(LoadCharacter);
        public RelayCommand LoadCacCommand => new RelayCommand(LoadCac);
        public RelayCommand LoadStageCommand => new RelayCommand(LoadStage);
        public RelayCommand SaveCurrentCommand => new RelayCommand(SaveCurrent, CanSaveCurrent);
        public RelayCommand SaveAllCommand => new RelayCommand(SaveAll, CanSaveAll);

        private void LoadSuperSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Super);
        }

        private void LoadUltimateSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Ultimate);
        }

        private void LoadEvasiveSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Evasive);
        }

        private void LoadBlastSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Blast);
        }

        private void LoadAwokenSkill()
        {
            Files.Instance.AsyncLoadSkill(Xv2CoreLib.CUS.CUS_File.SkillType.Awoken);
        }

        private void LoadMoveset()
        {
            Files.Instance.LoadMoveset();
        }

        private void LoadCharacter()
        {
            Files.Instance.AsyncLoadCharacter();
        }

        private async void LoadCac()
        {
            if (!File.Exists(SettingsManager.settings.SaveFile))
            {
                MessagePrompt.Show("A save file must be set in the settings to use this feature.", "No Save File", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            SAV_File savFile = SAV_File.Load(SettingsManager.settings.SaveFile, false);
            List<Xv2Item> items = new List<Xv2Item>();

            for (int i = 0; i < savFile.Characters.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(savFile.Characters[i].Name))
                    items.Add(new Xv2Item(i, savFile.Characters[i].Name));
            }

            EntitySelector itemSelector = new EntitySelector(items, "CaC");
            itemSelector.ShowDialog();

            if (itemSelector.SelectedItem != null)
            {
                await Files.Instance.AsyncLoadCac(itemSelector.SelectedItem.ID, savFile.Characters[itemSelector.SelectedItem.ID]);
            }
        }

        private void LoadStage()
        {
            Files.Instance.AsyncLoadStage();
        }

        private void SaveCurrent()
        {
            Files.Instance.SaveItem(Files.Instance.SelectedItem);
        }

        private void SaveAll()
        {
            var result = MessagePrompt.Show("Save all files currently loaded in the outliner (except those marked as \"Read Only\"?", "Save All", MessagePromptButtons.YesNo, MessagePromptIcon.Question);

            if (result == MessagePromptResult.Yes)
                Files.Instance.SaveAll();
        }

        private bool CanSaveAll()
        {
            return Files.Instance.OutlinerItems.Any(x => !x.ReadOnly);
        }

        private bool CanSaveCurrent()
        {
            if (Files.Instance.SelectedItem != null)
                return !Files.Instance.SelectedItem.ReadOnly;

            return false;
        }
    }
}
