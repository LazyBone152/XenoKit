using System.Timers;
using Xv2CoreLib.Resource.UndoRedo;
using LB_Common.Mvvm;
using XenoKit.Editor;

namespace EEPK_Organiser.Forms
{
    public partial class RecolorAll : AutoObservableWindow
    {
        public bool EnableLivePreview { get; set; } = true;

        private Timer UpdateTimer;

		private void XenoKitInit() 
		{
			if (LocalSettings.Instance.RealTimeRecolorUpdate && EnableLivePreview)
            {
				UpdateTimer = new Timer(10);
                Closed += RecolorAll_Closed;
                UpdateTimer.Elapsed += UpdateTimer_Elapsed;
                UpdateTimer.Start();
            }
		}

        private void RecolorAll_Closed(object sender, System.EventArgs e)
		{
            UpdateTimer.Stop();
			UpdateTimer.Elapsed -= UpdateTimer_Elapsed;
			UpdateTimer = null;
		}

        private async void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isRecoloring) return;

            if (HaveRecolorValuesChanged())
			{
				try
				{
					_isRecoloring = true;
					await PerformRecolorOperation();
					UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, GetContext());
				}
				finally
				{
					_isRecoloring = false;
				}
			}
        }

    }
}