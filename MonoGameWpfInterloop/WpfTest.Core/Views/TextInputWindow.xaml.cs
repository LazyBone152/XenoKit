using System;
using System.Threading;
using System.Windows.Threading;

namespace WpfTest.Views
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow
    {
        public TextInputWindow()
        {
            InitializeComponent();

            // manual timer that runs every 50ms to update UI based on game state
            var timer = new Timer(TimerTick, null, 0, 50);
            Closed += (sender, args) =>
            {
                timer?.Dispose();
                timer = null;
            };
        }

        private void TimerTick(object state)
        {
            TextFromGame.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                TextFromGame.Text = Game.EnteredMessage;
            }));
        }
    }
}