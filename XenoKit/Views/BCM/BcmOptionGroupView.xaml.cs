using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace XenoKit.Views.BCM
{
    public partial class BcmOptionGroupView : UserControl
    {
        public event EventHandler ValueChanged;

        private readonly List<BcmOptionGroup> groups = new List<BcmOptionGroup>();
        private bool isRefreshing;

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(BcmOptionGroupView), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(uint), typeof(BcmOptionGroupView), new PropertyMetadata(0u, ValuePropertyChanged));

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

        public BcmOptionGroupView()
        {
            InitializeComponent();
        }

        public void SetGroups(IEnumerable<BcmOptionGroup> newGroups)
        {
            groups.Clear();
            groups.AddRange(newGroups ?? Enumerable.Empty<BcmOptionGroup>());
            BuildGroups();
            RefreshValue();
        }

        private static void ValuePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((BcmOptionGroupView)dependencyObject).RefreshValue();
        }

        private void BuildGroups()
        {
            groupsPanel.Children.Clear();

            foreach (BcmOptionGroup group in groups)
            {
                bool skipInnerHeader = groups.Count == 1 && string.Equals(group.Title, Title, StringComparison.OrdinalIgnoreCase);
                FrameworkElement groupContainer;
                Panel stackPanel = new StackPanel();

                if (skipInnerHeader)
                {
                    groupContainer = stackPanel;
                    groupContainer.Margin = new Thickness(0, 0, 10, 8);
                }
                else
                {
                    GroupBox groupBox = new GroupBox
                    {
                        Header = group.Title,
                        Tag = group,
                        Width = 210,
                        Margin = new Thickness(0, 0, 10, 8),
                        Padding = new Thickness(6)
                    };
                    groupBox.SetResourceReference(ForegroundProperty, "MahApps.Brushes.Text");
                    groupBox.SetResourceReference(BorderBrushProperty, "MahApps.Brushes.Gray.SemiTransparent");
                    groupBox.Content = stackPanel;
                    groupContainer = groupBox;
                }

                groupContainer.Tag = group;

                string groupName = $"{Title}_{group.Title}_{Guid.NewGuid():N}";
                foreach (BcmChoice choice in group.Choices)
                {
                    RadioButton radioButton = new RadioButton
                    {
                        GroupName = groupName,
                        Tag = choice,
                        Margin = new Thickness(0, 1, 0, 1),
                        VerticalContentAlignment = VerticalAlignment.Top,
                        Content = new TextBlock
                        {
                            Text = choice.Label,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 178
                        }
                    };
                    radioButton.Checked += RadioButtonChecked;
                    stackPanel.Children.Add(radioButton);
                }

                groupsPanel.Children.Add(groupContainer);
            }
        }

        private void RefreshValue()
        {
            isRefreshing = true;

            foreach (FrameworkElement groupElement in groupsPanel.Children.OfType<FrameworkElement>())
            {
                if (!(groupElement.Tag is BcmOptionGroup group)) continue;
                Panel panel = groupElement is GroupBox groupBox ? groupBox.Content as Panel : groupElement as Panel;
                if (panel == null) continue;

                uint groupValue = (Value & group.Mask) >> group.Shift;
                foreach (RadioButton radioButton in panel.Children.OfType<RadioButton>())
                {
                    if (radioButton.Tag is BcmChoice choice)
                        radioButton.IsChecked = choice.Value == groupValue;
                }
            }

            isRefreshing = false;
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (isRefreshing) return;
            if (!(sender is RadioButton radioButton) || !(radioButton.Tag is BcmChoice choice)) return;
            FrameworkElement groupElement = FindParentGroupElement(radioButton);
            if (!(groupElement?.Tag is BcmOptionGroup group)) return;

            uint newValue = Value & ~group.Mask;
            newValue |= (choice.Value << group.Shift) & group.Mask;
            SetValueFromControl(newValue);
        }

        private static FrameworkElement FindParentGroupElement(DependencyObject child)
        {
            DependencyObject current = child;
            while (current != null)
            {
                if (current is GroupBox groupBox) return groupBox;
                if (current is StackPanel stackPanel && stackPanel.Tag is BcmOptionGroup) return stackPanel;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void SetValueFromControl(uint value)
        {
            if (Value == value) return;

            Value = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class BcmOptionGroup
    {
        public string Title { get; set; }
        public uint Mask { get; set; }
        public int Shift { get; set; }
        public IList<BcmChoice> Choices { get; set; }

        public BcmOptionGroup(string title, uint mask, int shift, params BcmChoice[] choices)
        {
            Title = title;
            Mask = mask;
            Shift = shift;
            Choices = choices;
        }
    }
}
