using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace XenoKit.Views.BCM
{
    public partial class BcmBitGroupView : UserControl
    {
        public event EventHandler ValueChanged;

        private readonly List<BcmChoiceGroup> groups = new List<BcmChoiceGroup>();
        private bool isRefreshing;

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(BcmBitGroupView), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(uint), typeof(BcmBitGroupView), new PropertyMetadata(0u, ValuePropertyChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public uint Value
        {
            get => (uint)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public BcmBitGroupView()
        {
            InitializeComponent();
        }

        public void SetGroups(IEnumerable<BcmChoiceGroup> newGroups)
        {
            groups.Clear();
            groups.AddRange(newGroups ?? Enumerable.Empty<BcmChoiceGroup>());
            BuildGroups();
            RefreshValue();
        }

        private static void ValuePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((BcmBitGroupView)dependencyObject).RefreshValue();
        }

        private void BuildGroups()
        {
            groupsPanel.Children.Clear();

            foreach (BcmChoiceGroup group in groups)
            {
                bool skipInnerHeader = groups.Count == 1 && string.IsNullOrWhiteSpace(group.Title);
                FrameworkElement groupElement;
                Panel choicePanel = skipInnerHeader ? (Panel)new WrapPanel() : new StackPanel();

                if (skipInnerHeader)
                {
                    groupElement = choicePanel;
                    groupElement.Margin = new Thickness(0, 0, 10, 8);
                }
                else
                {
                    GroupBox groupBox = new GroupBox
                    {
                        Header = group.Title,
                        Width = 210,
                        Margin = new Thickness(0, 0, 10, 8),
                        Padding = new Thickness(6)
                    };
                    groupBox.SetResourceReference(ForegroundProperty, "MahApps.Brushes.Text");
                    groupBox.SetResourceReference(BorderBrushProperty, "MahApps.Brushes.Gray.SemiTransparent");
                    groupBox.Content = choicePanel;
                    groupElement = groupBox;
                }

                groupElement.Tag = group;

                foreach (BcmChoice choice in group.Choices)
                {
                    double labelWidth = skipInnerHeader ? 138 : 178;
                    CheckBox checkBox = new CheckBox
                    {
                        Tag = choice,
                        Width = skipInnerHeader ? 166 : double.NaN,
                        Margin = skipInnerHeader ? new Thickness(0, 2, 12, 2) : new Thickness(0, 1, 0, 1),
                        VerticalContentAlignment = skipInnerHeader ? VerticalAlignment.Center : VerticalAlignment.Top,
                        Content = new TextBlock
                        {
                            Text = choice.Label,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = labelWidth,
                            VerticalAlignment = skipInnerHeader ? VerticalAlignment.Center : VerticalAlignment.Top
                        }
                    };
                    checkBox.ToolTip = string.IsNullOrWhiteSpace(choice.ToolTip) ? choice.Label : choice.ToolTip;
                    checkBox.Checked += CheckBoxChanged;
                    checkBox.Unchecked += CheckBoxChanged;
                    choicePanel.Children.Add(checkBox);
                }

                groupsPanel.Children.Add(groupElement);
            }
        }

        private void RefreshValue()
        {
            isRefreshing = true;

            foreach (FrameworkElement groupElement in groupsPanel.Children.OfType<FrameworkElement>())
            {
                Panel panel = groupElement is GroupBox groupBox ? groupBox.Content as Panel : groupElement as Panel;
                if (panel == null) continue;

                foreach (CheckBox checkBox in panel.Children.OfType<CheckBox>())
                {
                    if (checkBox.Tag is BcmChoice choice)
                        checkBox.IsChecked = (Value & choice.Value) == choice.Value && choice.Value != 0;
                }
            }

            isRefreshing = false;
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (isRefreshing) return;

            uint knownMask = 0;
            foreach (BcmChoice choice in groups.SelectMany(group => group.Choices))
                knownMask |= choice.Value;

            uint newValue = Value & ~knownMask;

            foreach (FrameworkElement groupElement in groupsPanel.Children.OfType<FrameworkElement>())
            {
                Panel panel = groupElement is GroupBox groupBox ? groupBox.Content as Panel : groupElement as Panel;
                if (panel == null) continue;

                foreach (CheckBox checkBox in panel.Children.OfType<CheckBox>())
                {
                    if (checkBox.IsChecked == true && checkBox.Tag is BcmChoice choice)
                        newValue |= choice.Value;
                }
            }

            SetValueFromControl(newValue);
        }

        private void SetValueFromControl(uint value)
        {
            if (Value == value) return;

            Value = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class BcmChoice
    {
        public string Label { get; set; }
        public uint Value { get; set; }
        public string ToolTip { get; set; }

        public BcmChoice(string label, uint value, string toolTip = null)
        {
            Label = label;
            Value = value;
            ToolTip = toolTip;
        }
    }

    public class BcmChoiceGroup
    {
        public string Title { get; set; }
        public IList<BcmChoice> Choices { get; set; }

        public BcmChoiceGroup(string title, params BcmChoice[] choices)
        {
            Title = title;
            Choices = choices;
        }
    }
}
