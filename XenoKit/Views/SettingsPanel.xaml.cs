using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xv2CoreLib.Resource.App;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public bool HideEmptryBacEntries
        {
            get
            {
                return SettingsManager.settings.XenoKit_HideEmptyBacEntries;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HideEmptyBacEntries != value)
                {
                    SettingsManager.settings.XenoKit_HideEmptyBacEntries = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }


        public SettingsPanel()
        {
            InitializeComponent();
            DataContext = this;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(HideEmptryBacEntries));
        }
    }
}
