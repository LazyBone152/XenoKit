using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib.BCS;
using XenoKit.ViewModel.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsColorView.xaml
    /// </summary>
    public partial class BcsColorView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private PartColor _selectedColorGroup = null;
        private Colors _selectedColor = null;

        public BCS_File BcsFile => Files.Instance.SelectedItem?.character?.CharacterData?.BcsFile?.File;
        public PartColor SelectedColorGroup
        {
            get => _selectedColorGroup;
            set
            {
                _selectedColorGroup = value;
                NotifyPropertyChanged(nameof(SelectedColorGroup));
                NotifyPropertyChanged(nameof(ColorVisibility));
            }
        }
        public Colors SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                NotifyPropertyChanged(nameof(SelectedColor));
            }
        }
        public string SelectedColorGroupName
        {
            get => SelectedColorGroup?.Name;
            set
            {
                if(SelectedColorGroupName != value && SelectedColorGroup != null)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<PartColor>(nameof(PartColor.Name), SelectedColorGroup, SelectedColorGroup.Name, value, "PartColorGroup Name"));
                    SelectedColorGroup.Name = value;
                    NotifyPropertyChanged(nameof(SelectedColorGroupName));
                }
            }
        }
        public int SelectedColorGroupID
        {
            get => SelectedColorGroup != null ? SelectedColorGroup.SortID : 0;
            set
            {
                if (SelectedColorGroupID != value && SelectedColorGroup != null)
                {
                    EditColorGroupID(value);
                }
            }
        }

        public Visibility ColorVisibility => SelectedColorGroup != null ? Visibility.Visible : Visibility.Collapsed;

        public BcsColorView()
        {
            DataContext = this;
            InitializeComponent();
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;

            NotifyPropertyChanged(nameof(ColorVisibility));
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            NotifyPropertyChanged(nameof(SelectedColorGroupName));
            
            if(BcsFile?.PartColors != null)
            {
                foreach (var colorGroup in BcsFile.PartColors)
                {
                    colorGroup.RefreshValues();
                }
            }

            if(SelectedColor != null)
            {
                SelectedColor.RefreshColorValues();
            }
        }

        #region ColorGroupCommands
        public RelayCommand AddColorGroupCommand => new RelayCommand(AddColorGroup, IsBcsFilePresent);
        private void AddColorGroup()
        {
            PartColor newColorGroup = new PartColor();
            newColorGroup.Index = BcsFile.NewPartColorGroupID().ToString();
            newColorGroup.Name = "NewColorGroup";
            BcsFile.PartColors.Add(newColorGroup);

            UndoManager.Instance.AddUndo(new UndoableListAdd<PartColor>(BcsFile.PartColors, newColorGroup, "New Color Group"));
        }

        public RelayCommand RemoveColorGroupCommand => new RelayCommand(RemoveColorGroup, IsColorGroupSelected);
        private void RemoveColorGroup()
        {
            var selectedItems = dataGrid.SelectedItems.Cast<PartColor>().ToList();

            if(selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var item in selectedItems)
                {
                    undos.Add(new UndoableListRemove<PartColor>(BcsFile.PartColors, item));
                    BcsFile.PartColors.Remove(item);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Remove Color Group", UndoGroup.BCS);
            }
        }

        public RelayCommand CopyColorGroupCommand => new RelayCommand(CopyColorGroup, IsColorGroupSelected);
        private void CopyColorGroup()
        {
            var selectedItems = dataGrid.SelectedItems.Cast<PartColor>().ToList();

            if (selectedItems?.Count > 0)
            {
                Clipboard.SetData(ClipboardConstants.BcsColorGroup, selectedItems);
            }
        }

        public RelayCommand PasteColorGroupCommand => new RelayCommand(PasteColorGroup, CanPasteBcsColorGroup);
        private void PasteColorGroup()
        {
            List<PartColor> partColorGroups = (List<PartColor>)Clipboard.GetData(ClipboardConstants.BcsColorGroup);

            if(partColorGroups != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var colorGroup in partColorGroups)
                {
                    //Assign new ID if already used
                    if(BcsFile.PartColors.Any(x => x.ID == colorGroup.ID))
                    {
                        colorGroup.ID = (ushort)BcsFile.NewPartColorGroupID();
                    }

                    BcsFile.PartColors.Add(colorGroup);
                    undos.Add(new UndoableListAdd<PartColor>(BcsFile.PartColors, colorGroup));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Color Group", UndoGroup.BCS);
            }

        }

        public RelayCommand DuplicateColorGroupCommand => new RelayCommand(DuplicateColorGroup, IsColorGroupSelected);
        private void DuplicateColorGroup()
        {
            var selectedItems = dataGrid.SelectedItems.Cast<PartColor>().ToList();

            if (selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                for (int i = 0; i < selectedItems.Count; i++)
                {
                    selectedItems[i] = selectedItems[i].Copy();
                    selectedItems[i].ID = (ushort)BcsFile.NewPartColorGroupID();
                    BcsFile.PartColors.Add(selectedItems[i]);

                    undos.Add(new UndoableListAdd<PartColor>(BcsFile.PartColors, selectedItems[i]));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Color Group", UndoGroup.BCS);
            }
        }


        private bool IsBcsFilePresent()
        {
            return BcsFile != null;
        }

        private bool IsColorGroupSelected()
        {
            return SelectedColorGroup != null && IsBcsFilePresent();
        }
        
        private bool CanPasteBcsColorGroup()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsColorGroup);
        }

        #endregion

        #region ColorCommands
        public RelayCommand AddColorCommand => new RelayCommand(AddColor, IsColorGroupSelected);
        private void AddColor()
        {
            Colors newColor = new Colors();
            newColor.ID = SelectedColorGroup.NewColorID();
            newColor.Color1.A = 1f;
            newColor.Color2.A = 1f;
            newColor.Color3.A = 1f;
            newColor.Color4.A = 1f;
            SelectedColorGroup.ColorsList.Add(newColor);

            UndoManager.Instance.AddUndo(new UndoableListAdd<Colors>(SelectedColorGroup.ColorsList, newColor, "New BCS Color"));
        }

        public RelayCommand RemoveColorCommand => new RelayCommand(RemoveColor, IsColorSelected);
        private void RemoveColor()
        {
            var selectedItems = colorListBox.SelectedItems.Cast<Colors>().ToList();

            if (selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var item in selectedItems)
                {
                    undos.Add(new UndoableListRemove<Colors>(SelectedColorGroup.ColorsList, item, SelectedColorGroup.ColorsList.IndexOf(item)));
                    SelectedColorGroup.ColorsList.Remove(item);
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Remove BCS Color", UndoGroup.BCS);
            }
        }

        public RelayCommand CopyColorCommand => new RelayCommand(CopyColor, IsColorSelected);
        private void CopyColor()
        {
            var selectedItems = colorListBox.SelectedItems.Cast<Colors>().ToList();

            if (selectedItems?.Count > 0)
            {
                Clipboard.SetData(ClipboardConstants.BcsColor, selectedItems);
            }
        }

        public RelayCommand PasteColorCommand => new RelayCommand(PasteColor, CanPasteBcsColor);
        private void PasteColor()
        {
            List<Colors> colors = (List<Colors>)Clipboard.GetData(ClipboardConstants.BcsColor);

            if (colors != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var color in colors)
                {
                    //Assign new ID if already used
                    if (SelectedColorGroup.ColorsList.Any(x => x.ID == color.ID))
                    {
                        color.ID = SelectedColorGroup.NewColorID();
                    }

                    SelectedColorGroup.ColorsList.Add(color);
                    undos.Add(new UndoableListAdd<Colors>(SelectedColorGroup.ColorsList, color));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Paste BCS Color", UndoGroup.BCS);
            }

        }

        public RelayCommand PasteColorValuesCommand => new RelayCommand(PasteColorValues, CanPasteBcsColor);
        private void PasteColorValues()
        {
            List<Colors> colors = (List<Colors>)Clipboard.GetData(ClipboardConstants.BcsColor);

            if (colors?.Count == 1)
            {
                List<IUndoRedo> undos = SelectedColor.PasteValues(colors[0]);

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Color Values", UndoGroup.BCS);
            }
            else
            {
                Log.Add("Can only paste color values when only 1 color was copied!");
            }

        }

        public RelayCommand DuplicateColorCommand => new RelayCommand(DuplicateColor, IsColorSelected);
        private void DuplicateColor()
        {
            var selectedItems = colorListBox.SelectedItems.Cast<Colors>().ToList();

            if (selectedItems?.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                for(int i = 0; i < selectedItems.Count; i++)
                {
                    selectedItems[i] = selectedItems[i].Copy();
                    selectedItems[i].ID = SelectedColorGroup.NewColorID();
                    SelectedColorGroup.ColorsList.Add(selectedItems[i]);

                    undos.Add(new UndoableListAdd<Colors>(SelectedColorGroup.ColorsList, selectedItems[i]));
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate BCS Color", UndoGroup.BCS);
            }
        }

        public RelayCommand RegenAllColorIDsCommand => new RelayCommand(RegenAllColorIDs, IsColorGroupSelected);
        private void RegenAllColorIDs()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = 0; i < SelectedColorGroup.ColorsList.Count; i++)
            {
                undos.Add(new UndoableProperty<Colors>(nameof(Colors.ID), SelectedColorGroup.ColorsList[i], SelectedColorGroup.ColorsList[i].ID, i));
                SelectedColorGroup.ColorsList[i].ID = i;
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Regen Color IDs", UndoGroup.BCS);
        }


        private bool IsColorSelected()
        {
            return SelectedColor != null;
        }

        private bool CanPasteBcsColor()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsColor) && SelectedColorGroup != null;
        }
        #endregion

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(BcsFile));
        }

        private async void EditColorGroupID(int newId)
        {
            if(BcsFile.PartColors.Any(x => x.SortID == newId && x != SelectedColorGroup))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", "The entered ID is already used by another Color Group.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return;
            }
            string newIdStr = newId.ToString();

            UndoManager.Instance.AddUndo(new UndoableProperty<PartColor>(nameof(PartColor.Index), SelectedColorGroup, SelectedColorGroup.Index, newIdStr, "PartColorGroup ID"));
            SelectedColorGroup.Index = newIdStr;
            NotifyPropertyChanged(nameof(SelectedColorGroupID));
        }

        private void CustomColor_ColorChangedEvent(object sender, EventArgs e)
        {
            SelectedColor?.RefreshPreview();
        }
    }
}
