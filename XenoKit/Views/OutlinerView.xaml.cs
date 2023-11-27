using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;
using System.Windows;
using Xceed.Wpf.Toolkit.Primitives;
using Xv2CoreLib.EffectContainer;
using System.Linq;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for OutlinerView.xaml
    /// </summary>
    public partial class OutlinerView : UserControl, IDropTarget
    {
        public Files files { get { return Files.Instance; } }


        public OutlinerView()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region Commands
        public RelayCommand PropertiesCommand => new RelayCommand(Properties, CanLoadProperties);
        private void Properties()
        {
            OutlinerItemProperties window = new OutlinerItemProperties(Files.Instance.SelectedItem);
            window.ShowDialog();
        }

        public RelayCommand SetActorPrimaryCommand => new RelayCommand(SetActorPrimary, CanSetActor);
        private void SetActorPrimary()
        {
            SceneManager.SetActor(files.SelectedItem.character, 0);
        }

        public RelayCommand SetActorTargetCommand => new RelayCommand(SetActorTarget, CanSetActor);
        private void SetActorTarget()
        {
            SceneManager.SetActor(files.SelectedItem.character, 1);
        }

        public RelayCommand FocusActorCommand => new RelayCommand(FocusActor, IsActorSelected);
        private void FocusActor()
        {
            SceneManager.FocusActor(files.SelectedItem.character);
        }

        public RelayCommand RemoveCommand => new RelayCommand(Remove, CanRemove);
        private void Remove()
        {
            Files.Instance.RemoveSelectedItem(listBox.SelectedItems.Cast<OutlinerItem>().ToList());
        }


        private bool CanRemove()
        {
            if (Files.Instance.SelectedItem != null)
            {
                return Files.Instance.SelectedItem.CanDelete;
            }
            return false;
        }

        private bool CanLoadProperties()
        {
            if (listBox.SelectedItem is OutlinerItem outlinerItem)
            {
                return (outlinerItem.Type == OutlinerItem.OutlinerItemType.Skill || outlinerItem.Type == OutlinerItem.OutlinerItemType.Moveset || outlinerItem.Type == OutlinerItem.OutlinerItemType.Character);
            }
            return false;
        }

        private bool CanSetActor()
        {
            if (files.SelectedItem == null) return false;
            return files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CaC;
        }

        private bool CanSetStage()
        {
            if (files.SelectedItem == null) return false;
            return files.SelectedItem.Type == OutlinerItem.OutlinerItemType.STAGE_MANUAL;
        }

        private bool IsActorSelected()
        {
            if (listBox.SelectedItem is OutlinerItem outlinerItem)
            {
                return outlinerItem.Type == OutlinerItem.OutlinerItemType.Character;
            }
            return false;
        }
        #endregion

        #region Drop

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data)
            {
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    dropInfo.Effects = DragDropEffects.Copy;
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                }
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data)
            {
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedFilePaths = data.GetData(DataFormats.FileDrop, true) as string[];
                    Files.Instance.ProcessFileDrop(droppedFilePaths);
                }
            }
        }

        void IDropTarget.DragEnter(IDropInfo dropInfo)
        {
        }

        void IDropTarget.DragLeave(IDropInfo dropInfo)
        {

        }

        #endregion

        private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CanSetActor())
                SetActorPrimary();

            if (CanSetStage())
            {

                if (SceneManager.MainGameInstance.ActiveStage == files.SelectedItem.ManualFiles)
                {
                    SceneManager.MainGameInstance.SetActiveStage(null);
                }
                else
                {
                    SceneManager.MainGameInstance.SetActiveStage(files.SelectedItem.ManualFiles);
                }
            }
        }
    }
}
