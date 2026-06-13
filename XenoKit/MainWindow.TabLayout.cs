using AutoUpdater;
using ControlzEx.Theming;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SAV;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace XenoKit
{
    public partial class MainWindow
    {
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedTab();
            Files.Instance.SelectedItemOrTabChanged(sender, e);
        }

        private async void UpdateSelectedTab()
        {
            UpdateStateTabLayout();

            bool changed = await SceneManager.SetSceneState(mainTabControl.SelectedIndex, bcsTabControl.SelectedIndex, audioControl.audioTabControl.SelectedIndex, eepkEditor.tabControl.SelectedIndex);

            //Auto play bac entry if nothing is active
            if (SceneManager.CurrentSceneState == EditorTabs.Action)
            {
                bacControlView.AutoPlayBacEntry();
            }

            if (!changed) return;

            if (SceneManager.CurrentSceneState == EditorTabs.Camera)
            {
                SceneManager.CameraSelectionChanged(cameraTabView.SelectedEanFile, cameraTabView.SelectedAnimation);
            }
        }

        private void UpdateStateTabLayout()
        {
            bool stateTabSelected = mainTabControl.SelectedItem is TabItem tabItem && string.Equals(tabItem.Header as string, "State", StringComparison.Ordinal);
            Visibility sceneVisibility = stateTabSelected ? Visibility.Collapsed : Visibility.Visible;

            Grid.SetColumnSpan(mainTabControl, stateTabSelected ? 3 : 1);
            monoGameView.Visibility = sceneVisibility;
            sceneTab.Visibility = sceneVisibility;
            editorSceneSplitter.Visibility = sceneVisibility;
            sceneLogSplitter.Visibility = sceneVisibility;
        }

    }
}
