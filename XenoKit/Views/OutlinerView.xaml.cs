using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using GalaSoft.MvvmLight.CommandWpf;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for OutlinerView.xaml
    /// </summary>
    public partial class OutlinerView : UserControl
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
            return files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character;
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

        private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CanSetActor())
                SetActorPrimary();

            if (CanSetStage())
            {
                if (SceneManager.MainGameInstance.ActiveStage == files.SelectedItem.ManualFiles)
                {
                    SceneManager.MainGameInstance.ActiveStage = null;
                }
                else
                {
                    SceneManager.MainGameInstance.ActiveStage = files.SelectedItem.ManualFiles;
                }
            }
        }
    }
}
