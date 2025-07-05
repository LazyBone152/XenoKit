using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine.Stage;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for StageView.xaml
    /// </summary>
    public partial class StageView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public Files files { get { return Files.Instance; } }

        private StageObject _selectedStageObject = null;
        private StageColliderInstance _selectedStageCollider = null;

        public StageObject SelectedStageObject
        {
            get => _selectedStageObject;
            set
            {
                if (_selectedStageObject != value)
                {
                    _selectedStageObject = value;
                    NotifyPropertyChanged(nameof(SelectedStageObject));
                }
            }
        }
        public StageColliderInstance SelectedStageCollider
        {
            get => _selectedStageCollider;
            set
            {
                if (_selectedStageCollider != value)
                {
                    _selectedStageCollider = value;
                    NotifyPropertyChanged(nameof(SelectedStageCollider));
                }
            }
        }
        
        public StageView()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(e.NewValue is StageColliderInstance instance)
            {
                if (_selectedStageCollider != null)
                    _selectedStageCollider.IsEnabled = false;

                SelectedStageCollider = instance;
                if(SelectedStageCollider != null)
                    SelectedStageCollider.IsEnabled = true;
            }
        }
    }
}
