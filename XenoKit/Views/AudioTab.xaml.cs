using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Forms;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Editor;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for AudioTab.xaml
    /// </summary>
    public partial class AudioTab : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public Files files { get { return Files.Instance; } }

        public AudioTab()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region SeFileSelection
        public RelayCommand AddSeFileCommand => new RelayCommand(AddSeFile, CanAddSeFile);
        private void AddSeFile()
        {
            //New SE files are only possible on characters.

            string result = StringInput.Show("New SE", "Costume IDs (comma-separated):", null, null, "Enter the costume ID (or IDs) that the SE is for (if entering multiple costumes, separate them by a comma):");

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            List<int> costumes = GetCostumes(result);

            if(costumes == null)
            {
                MessagePrompt.Show("The entered costumes could not be parsed.", "New SE", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            if (Xv2File<ACB_Wrapper>.HasCostume(files.SelectedMove.Files.SeAcbFile, costumes))
            {
                MessagePrompt.Show("One or more of the entered costumes are already in use on another SE ACB.", "New SE", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            var seFile = new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), costumes, Xenoverse2.MoveFileTypes.SE_ACB, files.SelectedItem.GetMoveType(), false, false);
            files.SelectedMove.Files.SeAcbFile.Add(seFile);
            files.SelectedItem.SelectedSeAcbFile = seFile;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListAdd<Xv2File<ACB_Wrapper>>(files.SelectedMove.Files.SeAcbFile, seFile));
            
            UndoManager.Instance.AddCompositeUndo(undos, $"New SE ({result})");
        }

        public RelayCommand DeleteSeFileCommand => new RelayCommand(DeleteSeFile, CanDeleteSeFile);
        private void DeleteSeFile()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<Xv2File<ACB_Wrapper>>(files.SelectedMove.Files.SeAcbFile, files.SelectedItem.SelectedSeAcbFile, $"Remove SE ({files.SelectedItem.SelectedSeAcbFile.GetCostumesString()})"));
            files.SelectedMove.Files.SeAcbFile.Remove(files.SelectedItem.SelectedSeAcbFile);
        }

        public RelayCommand RenameSeFileCommand => new RelayCommand(RenameSeFile, CanRenameSeFile);
        private void RenameSeFile()
        {
            List<int> originalCostumes = files.SelectedItem.SelectedSeAcbFile.Costumes;
            int originalIndex = files.SelectedMove.Files.SeAcbFile.IndexOf(files.SelectedItem.SelectedSeAcbFile);

            string result = StringInput.Show("Edit Costumes", "Costumes (comma-separated):", files.SelectedItem.SelectedSeAcbFile.GetCostumesString(), null, "Enter the costume ID (or IDs) that the SE is for (if entering multiple costumes, separate them by a comma):");

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            List<int> costumes = GetCostumes(result);

            if (costumes == null)
            {
                MessagePrompt.Show("The entered costumes could not be parsed.", "Edit Costumes", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            if (Xv2File<ACB_Wrapper>.HasCostume(files.SelectedMove.Files.SeAcbFile, costumes, originalIndex))
            {
                MessagePrompt.Show("One or more of the entered costumes are already in use on another SE ACB.", "Edit Costumes", MessagePromptButtons.OK, MessagePromptIcon.Error);
                return;
            }

            files.SelectedItem.SelectedSeAcbFile.Costumes = costumes;
            UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<ACB_Wrapper>>(nameof(Xv2File<ACB_Wrapper>.Costumes), files.SelectedItem.SelectedSeAcbFile, originalCostumes, files.SelectedItem.SelectedSeAcbFile.Costumes, $"Edit SE Costumes"));

        }


        private bool CanDeleteSeFile()
        {
            if (files.SelectedItem?.SelectedSeAcbFile != null)
            {
                return files.SelectedItem.SelectedSeAcbFile.IsNotDefault;
            }

            return false;
        }

        private bool CanAddSeFile()
        {
            if (files.SelectedItem != null)
            {
                return files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character;
            }

            return false;
        }

        private bool CanRenameSeFile()
        {
            if (files.SelectedItem != null)
            {
                if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character && (files.SelectedItem.SelectedSeAcbFile != null) ? files.SelectedItem.SelectedSeAcbFile.IsNotDefault : false)
                    return true;
            }

            return false;
        }
        #endregion


        #region VoxFileSelection
        public RelayCommand AddEnVoxFileCommand => new RelayCommand(AddEnVoxFile, CanAddVoxFile);
        private void AddEnVoxFile()
        {
            AddVoxFile_Base(true);
        }

        public RelayCommand AddJpVoxFileCommand => new RelayCommand(AddJpVoxFile, CanAddVoxFile);
        private void AddJpVoxFile()
        {
            AddVoxFile_Base(false);
        }

        private void AddVoxFile_Base(bool isEnglish)
        {
            //New VOX on characters will use Costume
            //New VOX on skills will use CharaCode

            string charaCode = string.Empty;
            List<int> costumes = null;

            if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset)
            {
                string result = StringInput.Show("New Voice", "Costume IDs (comma-separated):", null, null, "Enter the costume ID (or IDs) that the voice file is for (if entering multiple costumes, separate them by a comma):");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return;
                }

                costumes = GetCostumes(result);

                if (costumes == null)
                {
                    MessagePrompt.Show("The entered costumes could not be parsed.", "New Voice", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                if (Xv2File<ACB_Wrapper>.HasCostume(files.SelectedMove.Files.SeAcbFile, costumes))
                {
                    MessagePrompt.Show("One or more of the entered costumes are already in use on another VOX ACB.", "New Voice", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }
            }
            else if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill)
            {
                string result = StringInput.Show("New Voice", "Character ID (3-letter code):", null, null, "Enter the character ID (3-letter code) that the new voice is for.");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return;
                }

                if (result.Length != 3)
                {
                    MessagePrompt.Show("The entered ID contained too many or too few letters.", "New Voice", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                if (Xv2File<ACB_Wrapper>.IsCharaCodeUsed(files.SelectedMove.Files.VoxAcbFile, result.ToUpper()))
                {
                    MessagePrompt.Show("The entered ID is already used in a voice for this skill.", "New Voice", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                charaCode = result;
            }

            var voxFile = new Xv2File<ACB_Wrapper>(ACB_Wrapper.NewXv2Acb(), costumes, Xenoverse2.MoveFileTypes.VOX_ACB, files.SelectedItem.GetMoveType(), isEnglish, false);
            voxFile.CharaCode = charaCode;
            voxFile.Costumes = costumes;
            files.SelectedMove.Files.VoxAcbFile.Add(voxFile);
            files.SelectedItem.SelectedVoxAcbFile = voxFile;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableListAdd<Xv2File<ACB_Wrapper>>(files.SelectedMove.Files.VoxAcbFile, voxFile));

            UndoManager.Instance.AddCompositeUndo(undos, $"New Voice");
        }

        public RelayCommand DeleteVoxFileCommand => new RelayCommand(DeleteVoxFile, CanDeleteVoxFile);
        private void DeleteVoxFile()
        {
            //Skill: always can delete
            //Character: only if not default

            UndoManager.Instance.AddUndo(new UndoableListRemove<Xv2File<ACB_Wrapper>>(files.SelectedMove.Files.VoxAcbFile, files.SelectedItem.SelectedVoxAcbFile, $"Remove Voice"));
            files.SelectedMove.Files.VoxAcbFile.Remove(files.SelectedItem.SelectedVoxAcbFile);
        }

        public RelayCommand RenameVoxFileCommand => new RelayCommand(RenameVoxFile, CanRenameVoxFile);
        private void RenameVoxFile()
        {
            List<int> originalCostumes = files.SelectedItem.SelectedVoxAcbFile.Costumes;
            string originalCharaCode = files.SelectedItem.SelectedVoxAcbFile.CharaCode;
            bool isEnglish = files.SelectedItem.SelectedVoxAcbFile.IsEnglish;
            int originalIndex = files.SelectedMove.Files.VoxAcbFile.IndexOf(files.SelectedItem.SelectedVoxAcbFile);

            string charaCode = string.Empty;
            List<int> costumes = null;

            if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Moveset)
            {
                string result = StringInput.Show("Edit Costumes", "Costumes (comma-separated):", files.SelectedItem.SelectedVoxAcbFile.GetCostumesString(), null, "Enter the costume ID (or IDs) that the voice file is for (if entering multiple costumes, separate them by a comma):");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return;
                }

                costumes = GetCostumes(result);

                if (costumes == null)
                {
                    MessagePrompt.Show("The entered costumes could not be parsed.", "Costume for this voice file...", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                if (Xv2File<ACB_Wrapper>.HasCostume(files.SelectedMove.Files.VoxAcbFile, costumes, originalIndex, isEnglish))
                {
                    MessagePrompt.Show("One or more of the entered costumes are already in use on a VOX ACB.", "Costume for this voice file...", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                files.SelectedItem.SelectedVoxAcbFile.Costumes = costumes;
                UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<ACB_Wrapper>>(nameof(Xv2File<ACB_Wrapper>.Costumes), files.SelectedItem.SelectedVoxAcbFile, originalCostumes, files.SelectedItem.SelectedVoxAcbFile.Costumes, $"Edit Voice Costumes"));

            }
            else if (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Skill)
            {
                string result = StringInput.Show("Character code for this voice file...", "Character ID (3-letter code):", files.SelectedItem.SelectedVoxAcbFile.CharaCode, null, "Enter the character ID (3-letter code) that the new voice is for.");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return;
                }

                if (result.Length != 3)
                {
                    MessagePrompt.Show("The entered ID contained too many or too few letters.", "Character code for this voice file...", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                if (Xv2File<ACB_Wrapper>.IsCharaCodeUsed(files.SelectedMove.Files.VoxAcbFile, result.ToUpper(), originalIndex, isEnglish))
                {
                    MessagePrompt.Show("The entered ID is already used in a voice for this skill.", "Character code for this voice file...", MessagePromptButtons.OK, MessagePromptIcon.Error);
                    return;
                }

                files.SelectedItem.SelectedVoxAcbFile.CharaCode = result;
                UndoManager.Instance.AddUndo(new UndoableProperty<Xv2File<ACB_Wrapper>>(nameof(Xv2File<ACB_Wrapper>.CharaCode), files.SelectedItem.SelectedVoxAcbFile, originalCharaCode, result, $"Edit Voice Chara Code"));

            }
        }


        private bool IsCac()
        {
            return files.SelectedItem?.character?.CharacterData?.IsCaC == true;
        }

        private bool CanDeleteVoxFile()
        {
            if (IsCac()) return false;
            if(files.SelectedItem?.GetMoveType() == Xenoverse2.MoveType.Skill)
            {
                return true;
            }
            else if (files.SelectedItem?.GetMoveType() == Xenoverse2.MoveType.Character)
            {
                if (files.SelectedItem?.SelectedVoxAcbFile != null)
                {
                    return !files.SelectedItem.SelectedVoxAcbFile.IsDefault;
                }
            }

            return false;
        }

        private bool CanAddVoxFile()
        {
            if (IsCac()) return false;
            if (files.SelectedItem?.GetMoveType() == Xenoverse2.MoveType.Common || files.SelectedItem == null)
                return false;

            return true;
        }

        private bool CanRenameVoxFile()
        {
            if (IsCac()) return false;
            if (files.SelectedItem?.GetMoveType() == Xenoverse2.MoveType.Skill)
            {
                return true;
            }
            else if (files.SelectedItem?.GetMoveType() == Xenoverse2.MoveType.Character)
            {
                if (files.SelectedItem?.SelectedVoxAcbFile != null)
                {
                    return !files.SelectedItem.SelectedVoxAcbFile.IsDefault;
                }
            }

            return false;
        }
        #endregion


        public List<int> GetCostumes(string costumes)
        {
            string[] values = costumes.Trim().Split(',');
            List<int> intValues = new List<int>();

            foreach (var value in values)
            {
                int intValue;
                if (!int.TryParse(value, out intValue)) return null;

                intValues.Add(intValue);
            }

            return intValues;
        }
    }
}