using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Helper;
using XenoKit.ViewModel.BCS;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for BcsPartSetView.xaml
    /// </summary>
    public partial class BcsPartSetView : UserControl, INotifyPropertyChanged
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

        public BCS_File BcsFile => Files.Instance.SelectedItem?.character?.CharacterData?.BcsFile?.File;
        public Xv2Character Character => Files.Instance.SelectedItem?.character?.CharacterData;
        public IEnumerable<Xv2PartSetFile> EmdFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EMD && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;
        public IEnumerable<Xv2PartSetFile> EmbFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EMB && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;
        public IEnumerable<Xv2PartSetFile> EmmFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EMM && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;
        public IEnumerable<Xv2PartSetFile> DytFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.DYT_EMB && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;
        public IEnumerable<Xv2PartSetFile> ScdFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.SCD && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;
        public IEnumerable<Xv2PartSetFile> EanFiles => Character != null ? Character.PartSetFiles.Where(x => x.FileType == Xv2PartSetFile.Type.EAN && (x.PartTypes.HasFlag(CurrentPartFlag) || x.PartTypes == 0)) : null;

        //Selected Items
        public object SelectedItem
        {
            get => treeView.SelectedItem;
        }

        public PartSet SelectedPartSet => SelectedItem as PartSet;
        public Part SelectedPart => SelectedItem as Part;
        public PhysicsPart SelectedPhysicsPart => SelectedItem as PhysicsPart;
        public ColorSelector SelectedColorSelector => SelectedItem as ColorSelector;
        private PartTypeFlags CurrentPartFlag { get; set; }


        //ViewModels
        public BcsPartViewModel PartViewModel { get; private set; }
        public BcsPhysicsPartViewModel PhysicsPartViewModel { get; private set; }
        public BcsColorSelectorViewModel ColorSelectorViewModel { get; private set; }

        //PartSetID
        public int SelectedPartSetID
        {
            get => SelectedPartSet != null ? SelectedPartSet.ID : -1;
            set
            {
                if (SelectedPartSet?.ID != value)
                {
                    SetPartSetID(value);
                }
            }
        }

        //Visibilities
        public Visibility PartSetVisibility => SelectedItem is PartSet ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PartVisibility => SelectedItem is Part ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PhysicsPartVisibility => SelectedItem is PhysicsPart ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorSelectorVisibility => SelectedItem is ColorSelector ? Visibility.Visible : Visibility.Collapsed;

        public List<string> AddPartTypes
        {
            get
            {
                if (BcsFile == null) return null;
                PartSet partSet = SelectedPartSet != null ? SelectedPartSet : BcsFile.GetParentPartSet(SelectedPart);

                if (partSet == null) return null;

                List<string> types = new List<string>();

                if (!partSet.HasPart((PartType)0)) types.Add(((PartType)0).ToString());
                if (!partSet.HasPart((PartType)1)) types.Add(((PartType)1).ToString());
                if (!partSet.HasPart((PartType)2)) types.Add(((PartType)2).ToString());
                if (!partSet.HasPart((PartType)3)) types.Add(((PartType)3).ToString());
                if (!partSet.HasPart((PartType)4)) types.Add(((PartType)4).ToString());
                if (!partSet.HasPart((PartType)5)) types.Add(((PartType)5).ToString());
                if (!partSet.HasPart((PartType)6)) types.Add(((PartType)6).ToString());
                if (!partSet.HasPart((PartType)7)) types.Add(((PartType)7).ToString());
                if (!partSet.HasPart((PartType)8)) types.Add(((PartType)8).ToString());
                if (!partSet.HasPart((PartType)9)) types.Add(((PartType)9).ToString());

                return types;
            }
        }

        public BcsPartSetView()
        {
            DataContext = this;
            InitializeComponent();
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            //Selectively reload active part / part set, if its involved in the undo event
            if (SceneManager.Actors[0] != null)
            {
                if(e.UndoContext is PartSet partSetContext)
                {
                    if (SceneManager.Actors[0].PartSet?.IsPartSet(partSetContext) == true)
                    {
                        if(e.UndoArg == "COLOR")
                            SceneManager.Actors[0].PartSet.ApplyCustomColors();
                        else
                            SceneManager.Actors[0].PartSet.LoadPartSet();
                    }
                }
                else if (e.UndoContext is Part partContext)
                {
                    if (SceneManager.Actors[0].PartSet?.IsPartSet(partContext) == true)
                    {
                        SceneManager.Actors[0].PartSet.LoadPart((int)partContext.PartType);
                        SceneManager.Actors[0].PartSet.ApplyCustomColors();
                    }
                }
                else if (e.UndoContext is PhysicsPart physicsPartContext)
                {
                    Part part = BcsFile?.GetParentPart(physicsPartContext);

                    if (SceneManager.Actors[0].PartSet?.IsPartSet(part) == true && part != null)
                        SceneManager.Actors[0].PartSet.LoadPart((int)part.PartType);
                }
            }

            if (BcsFile != null)
            {
                //View needs to be resorted
                if (e.UndoContext == BcsFile.PartSets)
                {
                    treeView.Items.SortDescriptions.Clear();
                    treeView.Items.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));
                }

                foreach (var partSet in BcsFile.PartSets)
                {
                    partSet.RefreshValues();
                }
            }
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(BcsFile));
            NotifyPropertyChanged(nameof(EmdFiles));
            NotifyPropertyChanged(nameof(EmbFiles));
            NotifyPropertyChanged(nameof(EmmFiles));
            NotifyPropertyChanged(nameof(DytFiles));
            NotifyPropertyChanged(nameof(ScdFiles));
            NotifyPropertyChanged(nameof(EanFiles));
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            NotifyPropertyChanged(nameof(SelectedItem));
            NotifyPropertyChanged(nameof(SelectedPartSet));
            NotifyPropertyChanged(nameof(SelectedPart));
            NotifyPropertyChanged(nameof(SelectedPhysicsPart));
            NotifyPropertyChanged(nameof(PartSetVisibility));
            NotifyPropertyChanged(nameof(PartVisibility));
            NotifyPropertyChanged(nameof(PhysicsPartVisibility));
            NotifyPropertyChanged(nameof(ColorSelectorVisibility));
            NotifyPropertyChanged(nameof(IsActorCurrent));

            if (SelectedItem is Part part)
            {
                PartViewModel?.ReleaseEvents();
                PartViewModel = new BcsPartViewModel(part, BcsFile.GetParentPartSet(part));
                CurrentPartFlag = part.GetPartTypeFlags();
            }
            else if (PartViewModel != null)
            {
                PartViewModel.ReleaseEvents();
                PartViewModel = null;
            }

            if (SelectedItem is PhysicsPart physicsPart)
            {
                PhysicsPartViewModel?.ReleaseEvents();
                PhysicsPartViewModel = new BcsPhysicsPartViewModel(physicsPart, BcsFile.GetParentPartSet(physicsPart));

                Part parentPart = GetRootSelectedPart();
                CurrentPartFlag = parentPart != null ? parentPart.GetPartTypeFlags() : 0;
            }
            else if (PhysicsPartViewModel != null)
            {
                PhysicsPartViewModel.ReleaseEvents();
                PhysicsPartViewModel = null;
            }

            if (SelectedItem is ColorSelector colorSelector)
            {
                ColorSelectorViewModel?.ReleaseEvents();
                ColorSelectorViewModel = new BcsColorSelectorViewModel(colorSelector, BcsFile.GetParentPartSet(colorSelector), BcsFile);
            }
            else if (ColorSelectorViewModel != null)
            {
                ColorSelectorViewModel.ReleaseEvents();
                ColorSelectorViewModel = null;
            }

            NotifyPropertyChanged(nameof(SelectedPartSetID));
            NotifyPropertyChanged(nameof(PartViewModel));
            NotifyPropertyChanged(nameof(PhysicsPartViewModel));
            NotifyPropertyChanged(nameof(ColorSelectorViewModel));
            NotifyPropertyChanged(nameof(EmdFiles));
            NotifyPropertyChanged(nameof(EmbFiles));
            NotifyPropertyChanged(nameof(EmmFiles));
            NotifyPropertyChanged(nameof(DytFiles));
            NotifyPropertyChanged(nameof(ScdFiles));
            NotifyPropertyChanged(nameof(EanFiles));
        }

        public async void SetPartSetID(int newId)
        {
            if (SelectedPartSet != null)
            {
                if (BcsFile.PartSets.Any(x => x.ID == newId && x != SelectedPartSet))
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "ID Already Used", $"Another Part Set already has ID \"{newId}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedPartSet.ID), SelectedPartSet, SelectedPartSet.ID, newId, "PartSet ID"), UndoGroup.BCS, undoContext: BcsFile?.PartSets);
                SelectedPartSet.ID = newId;

                NotifyPropertyChanged(nameof(SelectedPartSetID));
                SelectedPartSet.RefreshValues();

                treeView.Items.SortDescriptions.Clear();
                treeView.Items.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));
            }
        }


        #region PartSetCommands
        public RelayCommand EquipPartSetCommand => new RelayCommand(EquipPartSet, CanEquipPartSet);
        private void EquipPartSet()
        {
            if (SelectedPartSet != null)
                Files.Instance.SelectedItem.character.PartSet = new CharaPartSet(SceneManager.MainGameBase, Files.Instance.SelectedItem?.character, SelectedPartSet.ID);
        }

        public RelayCommand ApplyTransformationCommand => new RelayCommand(ApplyTransformation, CanEquipPartSet);
        private void ApplyTransformation()
        {
            if (SelectedPartSet != null)
                Files.Instance.SelectedItem.character.PartSet.ApplyTransformation(SelectedPartSet.ID);
        }

        public RelayCommand RemoveTransformationCommand => new RelayCommand(RemoveTransformation, CanEquipPartSet);
        private void RemoveTransformation()
        {
            if (SelectedPartSet != null)
            {
                Files.Instance.SelectedItem.character.PartSet.ResetTransformation();
                SceneManager.Actors[0].ForceDytOverride = -1;
            }
        }

        public RelayCommand<int> ApplyTransformationDytCommand => new RelayCommand<int>(ApplyTransDyt);
        private void ApplyTransDyt(int dyt)
        {
            if (SceneManager.Actors[0] != null)
                SceneManager.Actors[0].ForceDytOverride = dyt;
        }

        public RelayCommand AddPartSetCommand => new RelayCommand(AddPartSet, IsBcsFileLoaded);
        private void AddPartSet()
        {
            PartSet partSet = new PartSet();
            partSet.ID = BcsFile.NewPartSetID();
            BcsFile.PartSets.Add(partSet);

            UndoManager.Instance.AddUndo(new UndoableListAdd<PartSet>(BcsFile.PartSets, partSet, $"Add Part Set {partSet.ID}"), UndoGroup.BCS);

            Log.Add($"Added new PartSet: {partSet.ID}");

        }

        public RelayCommand RemovePartSetCommand => new RelayCommand(RemovePartSet, IsPartSetSelected);
        private void RemovePartSet()
        {
            UndoManager.Instance.AddUndo(new UndoableListRemove<PartSet>(BcsFile.PartSets, SelectedPartSet, BcsFile.PartSets.IndexOf(SelectedPartSet), $"Remove Part Set {SelectedPartSet.ID}"), UndoGroup.BCS);
            BcsFile.PartSets.Remove(SelectedPartSet);
        }

        public RelayCommand CopyPartSetCommand => new RelayCommand(CopyPartSet, IsPartSetSelected);
        private void CopyPartSet()
        {
            Clipboard.SetData(ClipboardConstants.BcsPartSet, SelectedPartSet);
        }

        public RelayCommand PastePartSetCommand => new RelayCommand(PastePartSet, CanPastePartSet);
        private void PastePartSet()
        {
            PartSet partSet = (PartSet)Clipboard.GetData(ClipboardConstants.BcsPartSet);

            partSet.ID = BcsFile.NewPartSetID();
            BcsFile.PartSets.Add(partSet);

            UndoManager.Instance.AddUndo(new UndoableListAdd<PartSet>(BcsFile.PartSets, partSet, $"Paste Part Set {partSet.ID}"));

            Log.Add($"Pasted PartSet, new assigned ID: {partSet.ID}");
        }

        public RelayCommand DuplicatePartSetCommand => new RelayCommand(DuplicatePartSet, IsPartSetSelected);
        private void DuplicatePartSet()
        {
            PartSet partSet = SelectedPartSet.Copy();

            partSet.ID = BcsFile.NewPartSetID();
            BcsFile.PartSets.Add(partSet);

            UndoManager.Instance.AddUndo(new UndoableListAdd<PartSet>(BcsFile.PartSets, partSet, $"Duplicate Part Set {partSet.ID}"));

            Log.Add($"Duplicated PartSet, new assigned ID: {partSet.ID}");
        }


        private bool CanPastePartSet()
        {
            return Clipboard.ContainsData(ClipboardConstants.BcsPartSet);
        }

        private bool IsPartSetSelected()
        {
            return SelectedPartSet != null;
        }

        private bool IsBcsFileLoaded()
        {
            return BcsFile != null;
        }

        private bool CanEquipPartSet()
        {
            if (Files.Instance.SelectedItem == null || SceneManager.Actors[0] == null) return false;
            return IsActorCurrent;
        }

        public bool IsActorCurrent => SceneManager.Actors[0] == Files.Instance.SelectedItem?.character;
        #endregion

        #region PartCommands
        public RelayCommand RemovePartCommand => new RelayCommand(RemovePart, IsPartSelected);
        private void RemovePart()
        {
            PartType partType = SelectedPart.PartType;

            var partSet = BcsFile.GetParentPartSet(SelectedPart);
            UndoManager.Instance.AddUndo(new UndoableListRemove<Part>(partSet.Parts, SelectedPart, partSet.Parts.IndexOf(SelectedPart), "Remove Part"), UndoGroup.BCS, undoContext: partSet);
            partSet.Parts.Remove(SelectedPart);

            ReloadEquippedPart(partType);
        }

        public RelayCommand CopyPartCommand => new RelayCommand(CopyPart, IsPartSelected);
        private void CopyPart()
        {
            Clipboard.SetData(ClipboardConstants.BcsPart, SelectedPart);
        }

        public RelayCommand PastePartCommand => new RelayCommand(PastePart, CanPastePart);
        private void PastePart()
        {
            Part part = (Part)Clipboard.GetData(ClipboardConstants.BcsPart);
            PartSet parentPartSet = SelectedPartSet != null ? SelectedPartSet : BcsFile.GetParentPartSet(SelectedPart);

            List<IUndoRedo> undos = new List<IUndoRedo>();
            parentPartSet.SetPart(part, part.PartType, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Part", UndoGroup.BCS, undoContext: parentPartSet);

            ReloadEquippedPart(part.PartType);
        }


        private bool CanPastePart()
        {
            return (Clipboard.ContainsData(ClipboardConstants.BcsPart)) && (SelectedPartSet != null || SelectedPart != null);
        }

        private bool IsPartSelected()
        {
            return SelectedPart != null;
        }

        //Events:
        private void AddPartTypes_Click(object sender, RoutedEventArgs e)
        {
            PartSet partSet = SelectedPartSet != null ? SelectedPartSet : BcsFile.GetParentPartSet(SelectedPart);
            if (partSet == null) return;

            string partTypeStr = ViewHelpers.GetMenuItemString(e);
            PartType partType;

            if (Enum.TryParse(partTypeStr, out partType))
            {
                if (!partSet.HasPart(partType))
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    Part part = new Part();
                    part.Model = -1;
                    part.Model2 = -1;
                    part.PartType = partType;

                    partSet.SetPart(part, partType, undos);

                    UndoManager.Instance.AddCompositeUndo(undos, "Add Part", UndoGroup.BCS, undoContext: partSet);
                }
            }
        }

        private void AddPartType_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(AddPartTypes));
        }

        #endregion

        #region PhysicsPartCommands
        public RelayCommand AddPhysicsPartCommand => new RelayCommand(AddPhysicsPart, CanAddPartChild);
        private void AddPhysicsPart()
        {
            Part parentPart = SelectedPart != null ? SelectedPart : BcsFile.GetParentPart(SelectedPhysicsPart);

            if (parentPart == null)
                parentPart = BcsFile.GetParentPart(SelectedColorSelector);

            PhysicsPart newPhysicsPart = new PhysicsPart();
            newPhysicsPart.Model1 = -1;
            newPhysicsPart.Model2 = -1;

            UndoManager.Instance.AddUndo(new UndoableListAdd<PhysicsPart>(parentPart.PhysicsParts, newPhysicsPart, "Add Physics Part"));
            parentPart.PhysicsParts.Add(newPhysicsPart);
        }

        public RelayCommand RemovePhysicsPartCommand => new RelayCommand(RemovePhysicsPart, IsPhysicsPartSelected);
        private void RemovePhysicsPart()
        {
            PartSet partSet = BcsFile.GetParentPartSet(SelectedPhysicsPart);
            Part parentPart = BcsFile.GetParentPart(SelectedPhysicsPart);
            PartType partType = parentPart.PartType;

            UndoManager.Instance.AddUndo(new UndoableListRemove<PhysicsPart>(parentPart.PhysicsParts, SelectedPhysicsPart, parentPart.PhysicsParts.IndexOf(SelectedPhysicsPart), "Remove PhysicsPart"), UndoGroup.BCS, undoContext: SelectedPhysicsPart);
            parentPart.PhysicsParts.Remove(SelectedPhysicsPart);

            ReloadEquippedPart(partType, partSet);
        }

        public RelayCommand CopyPhysicsPartCommand => new RelayCommand(CopyPhysicsPart, IsPhysicsPartSelected);
        private void CopyPhysicsPart()
        {
            Clipboard.SetData(ClipboardConstants.BcsPhysicsPart, SelectedPhysicsPart);
        }

        public RelayCommand PastePhysicsPartCommand => new RelayCommand(PastePhysicsPart, CanPastePhysicsPart);
        private void PastePhysicsPart()
        {
            PhysicsPart physicsPart = (PhysicsPart)Clipboard.GetData(ClipboardConstants.BcsPhysicsPart);
            Part parentPart = SelectedPart != null ? SelectedPart : BcsFile.GetParentPart(SelectedPhysicsPart);

            parentPart.PhysicsParts.Add(physicsPart);
            UndoManager.Instance.AddUndo(new UndoableListAdd<PhysicsPart>(parentPart.PhysicsParts, physicsPart, "Paste Physics Part"), UndoGroup.BCS, undoContext: physicsPart);

            ReloadEquippedPart(parentPart.PartType);
        }

        public RelayCommand DuplicatePhysicsPartCommand => new RelayCommand(DuplicatePhysicsPart, IsPhysicsPartSelected);
        private void DuplicatePhysicsPart()
        {
            PhysicsPart physicsPart = SelectedPhysicsPart.Copy();
            Part parentPart = SelectedPart != null ? SelectedPart : BcsFile.GetParentPart(SelectedPhysicsPart);

            parentPart.PhysicsParts.Add(physicsPart);
            UndoManager.Instance.AddUndo(new UndoableListAdd<PhysicsPart>(parentPart.PhysicsParts, physicsPart, "Duplicate Physics Part"), UndoGroup.BCS, undoContext: physicsPart);

            ReloadEquippedPart(parentPart.PartType);
        }

        private bool CanAddPartChild()
        {
            return SelectedPhysicsPart != null || SelectedPart != null || SelectedColorSelector != null;
        }

        private bool IsPhysicsPartSelected()
        {
            return SelectedPhysicsPart != null;
        }

        private bool CanPastePhysicsPart()
        {
            return (Clipboard.ContainsData(ClipboardConstants.BcsPhysicsPart)) && (SelectedPart != null || SelectedPhysicsPart != null);
        }
        #endregion

        #region ColorSelectorCommands
        public RelayCommand AddColorSelectorCommand => new RelayCommand(AddColorSelector, CanAddPartChild);
        private void AddColorSelector()
        {
            Part parentPart = SelectedPart != null ? SelectedPart : BcsFile.GetParentPart(SelectedColorSelector);

            if(parentPart == null)
                parentPart = BcsFile.GetParentPart(SelectedPhysicsPart);

            ColorSelector newColorSelector = new ColorSelector();

            UndoManager.Instance.AddUndo(new UndoableListAdd<ColorSelector>(parentPart.ColorSelectors, newColorSelector, "Add Color Selector"), UndoGroup.BCS, "COLOR", undoContext: BcsFile.GetParentPartSet(parentPart));
            parentPart.ColorSelectors.Add(newColorSelector);

            ReloadEquippedPart(parentPart.PartType, null, true);
        }

        public RelayCommand RemoveColorSelectorCommand => new RelayCommand(RemoveColorSelector, IsColorSelectorSelected);
        private void RemoveColorSelector()
        {
            PartSet partSet = BcsFile.GetParentPartSet(SelectedColorSelector);
            Part parentPart = BcsFile.GetParentPart(SelectedColorSelector);
            PartType partType = parentPart.PartType;

            UndoManager.Instance.AddUndo(new UndoableListRemove<ColorSelector>(parentPart.ColorSelectors, SelectedColorSelector, parentPart.ColorSelectors.IndexOf(SelectedColorSelector), "Remove Color Selector"), UndoGroup.BCS, "COLOR", undoContext: partSet);
            parentPart.ColorSelectors.Remove(SelectedColorSelector);

            ReloadEquippedPart(partType, partSet, true);
        }

        public RelayCommand CopyColorSelectorCommand => new RelayCommand(CopyColorSelector, IsColorSelectorSelected);
        private void CopyColorSelector()
        {
            Clipboard.SetData(ClipboardConstants.BcsColorSelector, SelectedColorSelector);
        }

        public RelayCommand PasteColorSelectorCommand => new RelayCommand(PasteColorSelector, CanPasteColorSelector);
        private void PasteColorSelector()
        {
            ColorSelector colorSelector = (ColorSelector)Clipboard.GetData(ClipboardConstants.BcsColorSelector);
            Part parentPart = SelectedPart != null ? SelectedPart : BcsFile.GetParentPart(SelectedColorSelector);
            PartSet partSet = BcsFile.GetParentPartSet(parentPart);

            parentPart.ColorSelectors.Add(colorSelector);
            UndoManager.Instance.AddUndo(new UndoableListAdd<ColorSelector>(parentPart.ColorSelectors, colorSelector, "Paste Color Selector"), UndoGroup.BCS, "COLOR", undoContext: partSet);

            ReloadEquippedPart(parentPart.PartType, partSet, true);
        }

        public RelayCommand PasteColorSelectorOrPhysicsPartCommand => new RelayCommand(PasteColorSelectorOrPhysicsPart, CanPasteColorSelectorOrPhysicsPart);
        private void PasteColorSelectorOrPhysicsPart()
        {
            if(CanPastePhysicsPart())
            {
                PastePhysicsPart();
            }
            else
            {
                PasteColorSelector();
            }

        }

        private bool CanPasteColorSelectorOrPhysicsPart()
        {
            return CanPasteColorSelector() || CanPastePhysicsPart();
        }

        private bool IsColorSelectorSelected()
        {
            return SelectedColorSelector != null;
        }

        private bool CanPasteColorSelector()
        {
            return (Clipboard.ContainsData(ClipboardConstants.BcsColorSelector)) && (SelectedPart != null || SelectedPhysicsPart != null);
        }
        #endregion

        #region InputBindingCommands
        public RelayCommand RemoveInputCommand => new RelayCommand(RemoveInput, IsAnythingSelected);
        private void RemoveInput()
        {
            if (SelectedPartSet != null)
                RemovePartSet();
            else if (SelectedPart != null)
                RemovePart();
            else if (SelectedPhysicsPart != null)
                RemovePhysicsPart();
            else if (SelectedColorSelector != null)
                RemoveColorSelector();
        }

        public RelayCommand CopyInputCommand => new RelayCommand(CopyInput, IsAnythingSelected);
        private void CopyInput()
        {
            if (SelectedPartSet != null)
                CopyPartSet();
            else if (SelectedPart != null)
                CopyPart();
            else if (SelectedPhysicsPart != null)
                CopyPhysicsPart();
            else if (SelectedColorSelector != null)
                CopyColorSelector();
        }

        public RelayCommand PasteInputCommand => new RelayCommand(PasteInput, CanPasteAnything);
        private void PasteInput()
        {
            if (SelectedPartSet != null)
                PastePartSet();
            else if (SelectedPart != null)
                PastePart();
            else if (SelectedPhysicsPart != null)
                PastePhysicsPart();
            else if (SelectedColorSelector != null)
                PasteColorSelector();
        }

        public RelayCommand DuplicateInputCommand => new RelayCommand(DuplicateInput, IsAnythingSelected);
        private void DuplicateInput()
        {
            if (SelectedPart != null || SelectedColorSelector != null) return;

            if (SelectedPartSet != null)
                DuplicatePartSet();
            else if (SelectedPhysicsPart != null)
                DuplicatePhysicsPart();
        }

        private bool CanPasteAnything()
        {
            return CanPastePartSet() || CanPastePart() || CanPasteColorSelector() || CanPastePhysicsPart();
        }

        private bool IsAnythingSelected()
        {
            return IsPartSetSelected() || IsPartSelected() || IsPhysicsPartSelected() || IsColorSelectorSelected();
        }
        #endregion

        #region ColorCommands
        public RelayCommand GetColorCommand => new RelayCommand(GetBcsColor, CanGetBcsColor);
        private void GetBcsColor()
        {
            Windows.BcsColorSelector colorSelector = new Windows.BcsColorSelector(BcsFile, SelectedColorSelector.PartColorGroup);
            colorSelector.ShowDialog();

            if (colorSelector.Finished && ColorSelectorViewModel != null)
            {
                ColorSelectorViewModel.ColorIndex = (ushort)colorSelector.SelectedValue;
            }
        }

        private bool CanGetBcsColor()
        {
            if (SelectedColorSelector == null) return false;
            return BcsFile.PartColors.Any(x => x.ID == SelectedColorSelector.PartColorGroup);
        }
        #endregion

        private void treeView_Loaded(object sender, RoutedEventArgs e)
        {
            treeView.Items.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));
        }

        private void ReloadEquippedPart(PartType partType, PartSet parentPartSet = null, bool justColors = false)
        {
            if (SceneManager.Actors[0]?.PartSet != null)
            {
                PartSet partSet = parentPartSet != null ? parentPartSet : GetRootSelectedPartSet();

                if (SceneManager.Actors[0].PartSet.IsPartSet(partSet) && partSet != null)
                {
                    if(!justColors)
                        SceneManager.Actors[0]?.PartSet.LoadPart((int)partType);

                    SceneManager.Actors[0].PartSet.ApplyCustomColors();
                }
            }
        }

        private void ReloadEquippedPartSet()
        {
            if (SceneManager.Actors[0]?.PartSet != null)
            {
                PartSet partSet = GetRootSelectedPartSet();

                if (SceneManager.Actors[0].PartSet.IsPartSet(partSet) && partSet != null)
                {
                    SceneManager.Actors[0].PartSet.LoadPartSet();
                    SceneManager.Actors[0].PartSet.ApplyCustomColors();
                }
            }
        }

        private PartSet GetRootSelectedPartSet()
        {
            if (BcsFile == null) return null;
            if (SelectedPartSet != null) return SelectedPartSet;
            if (SelectedPart != null) return BcsFile.GetParentPartSet(SelectedPart);
            if (SelectedPhysicsPart != null) return BcsFile.GetParentPartSet(SelectedPhysicsPart);
            if (SelectedColorSelector != null) return BcsFile.GetParentPartSet(SelectedColorSelector);

            return null;
        }
        
        private Part GetRootSelectedPart()
        {
            if (BcsFile == null) return null;
            if (SelectedPart != null) return SelectedPart;
            if (SelectedPhysicsPart != null) return BcsFile.GetParentPart(SelectedPhysicsPart);
            if (SelectedColorSelector != null) return BcsFile.GetParentPart(SelectedColorSelector);

            return null;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi == null || e.Handled) return;

            if (!tvi.IsExpanded)
            {
                tvi.IsExpanded = true;

                //Close all other items
                foreach(var item in treeView.ItemContainerGenerator.Items)
                {
                    if(item is PartSet)
                    {
                        var container = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                        if (container != null && container != tvi)
                            container.IsExpanded = false;
                    }
                }
            }

            e.Handled = true;
        }
    }
}
