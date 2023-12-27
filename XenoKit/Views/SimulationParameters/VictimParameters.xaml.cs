using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;

namespace XenoKit.Views.SimulationParameters
{
    /// <summary>
    /// Interaction logic for VictimParameters.xaml
    /// </summary>
    public partial class VictimParameters : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public bool VictimEnabled
        {
            get => SceneManager.VictimEnabled;
            set
            {
                if(value == true && SceneManager.Actors[1] == null)
                {
                    if(MessageBox.Show("Do you want to load the default victim character?\n\nIf you select no, you may manually load a character as usual, and then right-click on it in the outliner and set it as the Victim, but until you do so no victim will be available.", "No Victim Set", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        SceneManager.EnsureActorIsSet(1);
                    }
                    else
                    {
                        return;
                    }
                }

                SceneManager.VictimEnabled = value;
            }
        }

        public float VictimDistance
        {
            get => SceneManager.VictimDistance;
            set => SceneManager.VictimDistance = value;
        }
        public bool VictimIsGuarding
        {
            get => SceneManager.VictimIsGuarding;
            set => SceneManager.VictimIsGuarding = value;
        }

        public VictimParameters()
        {
            DataContext = this;
            InitializeComponent();
            SceneManager.ActorChanged += SceneManager_ActorChanged;
            victimDirectionComboBox.SelectedIndex = 0;
        }

        private void SceneManager_ActorChanged(object source, ActorChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(VictimEnabled));
        }

        private void victimDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SceneManager.VictimIsFacingPrimary = victimDirectionComboBox.SelectedIndex == 0;
        }
    }
}
