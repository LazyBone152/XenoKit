using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;

namespace XenoKit.Views.SimulationParameters
{
    /// <summary>
    /// Interaction logic for InputParameters.xaml
    /// </summary>
    public partial class InputParameters : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        public InputParameters()
        {
            DataContext = this;
            InitializeComponent();
        }

    }
}
