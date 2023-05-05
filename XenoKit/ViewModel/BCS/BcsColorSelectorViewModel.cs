using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using XenoKit.Engine;

namespace XenoKit.ViewModel.BCS
{
    public class BcsColorSelectorViewModel : ObservableObject
    {
        PartSet partSet;
        ColorSelector colorSelector;
        BCS_File bcsFile;
        Part parentPart;

        public ushort PartColorGroup
        {
            get
            {
                return colorSelector.PartColorGroup;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(colorSelector.PartColorGroup), colorSelector, colorSelector.PartColorGroup, value, "BCS PartColorGroup"), UndoGroup.BCS, "COLOR", undoContext: partSet);
                colorSelector.PartColorGroup = value;
                RaisePropertyChanged(() => PartColorGroup);
                RaisePropertyChanged(() => SelectedColorSelectorPreview);
                ReapplyCustomColors();
            }
        }
        public ushort ColorIndex
        {
            get
            {
                return colorSelector.ColorIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<ColorSelector>(nameof(colorSelector.ColorIndex), colorSelector, colorSelector.ColorIndex, value, "BCS ColorIndex"), UndoGroup.BCS, "COLOR", undoContext: partSet);
                colorSelector.ColorIndex = value;
                RaisePropertyChanged(() => ColorIndex);
                RaisePropertyChanged(() => SelectedColorSelectorPreview);
                ReapplyCustomColors();
            }
        }

        public Colors SelectedColorSelectorPreview => bcsFile.GetColor(PartColorGroup, ColorIndex);


        public BcsColorSelectorViewModel(ColorSelector colSel, PartSet partSet, BCS_File bcsFile)
        {
            colorSelector = colSel;
            this.bcsFile = bcsFile;
            this.partSet = partSet;
            parentPart = bcsFile?.GetParentPart(colSel);

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
            //ReapplyCustomColors();
        }

        public void ReleaseEvents()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => PartColorGroup);
            RaisePropertyChanged(() => ColorIndex);
            RaisePropertyChanged(() => SelectedColorSelectorPreview);
        }

        public void ReapplyCustomColors()
        {
            if (SceneManager.Actors[0]?.PartSet != null)
            {
                if (SceneManager.Actors[0].PartSet.IsPartSet(partSet))
                {
                    SceneManager.Actors[0].PartSet.ReapplyCustomColors();
                }
            }
        }
    }

}
