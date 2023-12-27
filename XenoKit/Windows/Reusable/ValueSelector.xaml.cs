using MahApps.Metro.Controls;
using System.Windows;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for ValueSelector.xaml
    /// </summary>
    public partial class ValueSelector : MetroWindow
    {
        public bool IsFinished { get; private set; }

        public string ParameterName { get; set; }
        public string ParameterToolTip { get; set; }
        public int Parameter { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }

        public string BooleanParameterName { get; set; }
        public string BooleanParameterToolTip { get; set; }
        public bool BooleanParameter { get; set; }

        public ValueSelector(string windowName, string parameterName, string parameterToolTip, int initialValue = 0, int minValue = 0, int maxValue = int.MaxValue)
        {
            ParameterName = parameterName;
            ParameterToolTip = parameterToolTip;
            Parameter = initialValue;
            MaxValue = maxValue;
            MinValue = minValue;
            DataContext = this;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = windowName;
        }

        public void SetBooleanParameter(string name, string tooltip)
        {
            BooleanParameterName = name;
            BooleanParameterToolTip = tooltip;
            checkbox.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IsFinished = true;
            Close();
        }
    }
}
