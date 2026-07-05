using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xv2CoreLib.BCM;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views.BCM
{
    public partial class BcmEntryEditor : UserControl
    {
        public event EventHandler EntryEdited;

        // Textbox fields commit on LostFocus. Switching entry rebuilds the editor and tears those
        // textboxes down before they commit, so any in-progress edit is flushed here against the
        // entry it was built for before the panels are cleared.
        private readonly List<Action> pendingCommits = new List<Action>();

        public static readonly DependencyProperty SelectedEntryProperty = DependencyProperty.Register(
            nameof(SelectedEntry), typeof(BCM_Entry), typeof(BcmEntryEditor), new PropertyMetadata(null, EntryPropertyChanged));

        public static readonly DependencyProperty IsRootEntryProperty = DependencyProperty.Register(
            nameof(IsRootEntry), typeof(bool), typeof(BcmEntryEditor), new PropertyMetadata(false, EntryPropertyChanged));

        public static readonly DependencyProperty SiblingIndexProperty = DependencyProperty.Register(
            nameof(SiblingIndex), typeof(string), typeof(BcmEntryEditor), new PropertyMetadata("0", EntryPropertyChanged));

        public static readonly DependencyProperty ChildIndexProperty = DependencyProperty.Register(
            nameof(ChildIndex), typeof(string), typeof(BcmEntryEditor), new PropertyMetadata("0", EntryPropertyChanged));

        public BCM_Entry SelectedEntry
        {
            get => (BCM_Entry)GetValue(SelectedEntryProperty);
            set => SetValue(SelectedEntryProperty, value);
        }

        public bool IsRootEntry
        {
            get => (bool)GetValue(IsRootEntryProperty);
            set => SetValue(IsRootEntryProperty, value);
        }

        public string SiblingIndex
        {
            get => (string)GetValue(SiblingIndexProperty);
            set => SetValue(SiblingIndexProperty, value);
        }

        public string ChildIndex
        {
            get => (string)GetValue(ChildIndexProperty);
            set => SetValue(ChildIndexProperty, value);
        }

        public BcmEntryEditor()
        {
            InitializeComponent();
            BuildEditor();
        }

        private static void EntryPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((BcmEntryEditor)dependencyObject).BuildEditor();
        }

        private void BuildEditor()
        {
            FlushPendingCommits();

            inputsPanel.Children.Clear();
            activatorPanel.Children.Clear();
            bacPanel.Children.Clear();
            miscPanel.Children.Clear();
            unknownPanel.Children.Clear();

            bool canEdit = SelectedEntry != null && !IsRootEntry;
            emptyTextBlock.Visibility = canEdit ? Visibility.Collapsed : Visibility.Visible;
            tabControl.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

            if (!canEdit) return;

            BuildInputsTab();
            BuildActivatorTab();
            BuildBacTab();
            BuildMiscTab();
            BuildUnknownTab();
        }

        private void FlushPendingCommits()
        {
            if (pendingCommits.Count == 0) return;

            List<Action> commits = new List<Action>(pendingCommits);
            pendingCommits.Clear();
            foreach (Action commit in commits)
                commit();
        }














        private Panel AddEditorGroup(Panel panel, string title)
        {
            GroupBox groupBox = new GroupBox
            {
                Header = title,
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(8)
            };
            groupBox.SetResourceReference(ForegroundProperty, "MahApps.Brushes.Text");
            groupBox.SetResourceReference(BorderBrushProperty, "MahApps.Brushes.Gray.SemiTransparent");

            StackPanel stackPanel = new StackPanel();
            groupBox.Content = stackPanel;
            panel.Children.Add(groupBox);
            return stackPanel;
        }














    }
}
