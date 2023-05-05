using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;

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

        public RelayCommand PropertiesCommand => new RelayCommand(Properties, CanLoadProperties);
        private void Properties()
        {
            OutlinerItemProperties window = new OutlinerItemProperties(Files.Instance.SelectedMove.Files);
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

        private bool CanLoadProperties()
        {
            if(listBox.SelectedItem is OutlinerItem outlinerItem)
            {
                return (outlinerItem.Type == OutlinerItem.OutlinerItemType.Skill || outlinerItem.Type == OutlinerItem.OutlinerItemType.Moveset || outlinerItem.Type == OutlinerItem.OutlinerItemType.Character);
            }
            return false;
        }

        private bool CanSetActor()
        {
            return files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character;
        }
    }
}
