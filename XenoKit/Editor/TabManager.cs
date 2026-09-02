using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace XenoKit.Editor
{
    public static class TabManager
    {
        private static MetroTabControl TabControl;
        private static int StartIndex = 0;

        private static List<DynamicTab> DynamicTabs = new List<DynamicTab>();

        internal static void SetTabContext(MetroTabControl tabControl)
        {
            if (TabControl != null)
                throw new Exception("TabManager.SetTabContext: tab context has already been set!");

            TabControl = tabControl;
            StartIndex = TabControl.Items.Count;
        }

        public static void AddTab(string name, object content, IDynamicTabObject context, object parentItem, string tooltip = null)
        {
            if (FocusTab(context))
                return;

            MetroTabItem tab = new MetroTabItem();
            tab.Style = Application.Current.Resources.FindName("UnderlinedTabControl") as Style;
            tab.Header = name;
            tab.ToolTip = tooltip != null ? tooltip : name;
            tab.MaxWidth = 150;
            tab.Content = content;
            tab.CloseButtonEnabled = true;
            tab.CloseTabCommand = TabCloseCommand;
            HeaderedControlHelper.SetHeaderFontSize(tab, 14.0);

            DynamicTab dynamicTab = new DynamicTab(context, tab, parentItem);
            DynamicTabs.Add(dynamicTab);

            TabControl.Items.Add(tab);
            TabControl.SelectedItem = tab;
        }

        public static void RemoveTab(IDynamicTabObject context)
        {
            if (context == null) return;

            for (int i = DynamicTabs.Count - 1; i >= 0; i--)
            {
                DynamicTab tab = DynamicTabs[i];

                if (tab.Context == context)
                {
                    if (TabControl.Items.Contains(tab.Tab))
                    {
                        TabControl.Items.Remove(tab.Tab);
                    }

                    DynamicTabs.Remove(tab);
                    return;
                }
            }
        }

        public static bool FocusTab(IDynamicTabObject context)
        {
            DynamicTab existingTab = DynamicTabs.FirstOrDefault(x => x.Context == context);

            if (existingTab != null)
            {
                if (TabControl.Items.Contains(existingTab.Tab))
                {
                    TabControl.SelectedItem = existingTab.Tab;
                    return true;
                }
                else
                {
                    //DynamicTabs and the tab control are out of sync: remove the existing dynamic tab and re-add it
                    //this shouln't happen, but just in case it does...
                    DynamicTabs.Remove(existingTab);
                }
            }

            return false;
        }

        public static void RemoveTabsForParent(object parentItem)
        {
            for (int i = DynamicTabs.Count - 1; i >= 0; i--)
            {
                if (DynamicTabs[i].ParentItem == parentItem)
                {
                    if (TabControl.Items.Contains(DynamicTabs[i].Tab))
                    {
                        TabControl.Items.Remove(DynamicTabs[i].Tab);
                    }

                    DynamicTabs.RemoveAt(i);
                }
            }
        }

        public static bool CanSelectedTabSave()
        {
            var tab = GetSelectedDynamicTab();
            return tab != null ? tab.Context.CanSave() : false;
        }

        public static void SaveSelectedTab()
        {
            var tab = GetSelectedDynamicTab();
            tab?.Context.Save();
        }

        public static DynamicTab GetSelectedDynamicTab()
        {
            var tab = TabControl.SelectedItem;
            var dynTab = DynamicTabs.FirstOrDefault(x => x.Tab == tab);

            return dynTab;
        }

        public static RelayCommand TabCloseCommand => new RelayCommand(TabClose);
        private static void TabClose()
        {
            RemoveTab(GetSelectedDynamicTab()?.Context);
            /*
            for (int i = DynamicTabs.Count - 1; i >= 0; i--)
            {
                if (!TabControl.Items.Contains(DynamicTabs[i].Tab))
                {
                    DynamicTabs.RemoveAt(i);
                }
            }
            */
        }
    }

    public class DynamicTab
    {
        public object ParentItem { get; private set; } //Could be the OutlinerItem parent or a InspectorEntity (for viewer mode). Used for determining if a tab should be automatically closed when items are removed from the outliner/viewer file list

        public IDynamicTabObject Context { get; private set; }
        public MetroTabItem Tab {  get; private set; }

        public DynamicTab(IDynamicTabObject context, MetroTabItem tab, object parent)
        {
            ParentItem = parent;
            Context = context;
            Tab = tab;
        }
    }

    public interface IDynamicTabObject
    {
        string GetSaveContextFileName();
        bool CanSave();
        void Save();
    }
}