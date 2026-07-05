using System;
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
        private void AddBitGroup(Panel panel, string title, string propertyName, params BcmChoiceGroup[] groups)
        {
            PropertyInfo property = GetProperty(propertyName);
            if (property == null) return;

            BCM_Entry entry = SelectedEntry;
            BcmBitGroupView view = new BcmBitGroupView
            {
                Title = title,
                Value = ToUInt32(property.GetValue(entry, null))
            };
            view.SetGroups(groups);
            view.ValueChanged += (sender, args) =>
            {
                SetProperty(entry, property, FromUInt32(property.PropertyType, view.Value));
                if (property.Name == nameof(BCM_Entry.I_00))
                    BuildEditor();
            };
            panel.Children.Add(view);
        }

        private void AddOptionGroup(Panel panel, string title, string propertyName, params BcmOptionGroup[] groups)
        {
            PropertyInfo property = GetProperty(propertyName);
            if (property == null) return;

            BCM_Entry entry = SelectedEntry;
            BcmOptionGroupView view = new BcmOptionGroupView
            {
                Title = title,
                Value = ToUInt32(property.GetValue(entry, null))
            };
            view.SetGroups(groups);
            view.ValueChanged += (sender, args) =>
            {
                SetProperty(entry, property, FromUInt32(property.PropertyType, view.Value));
                if (property.Name == nameof(BCM_Entry.I_00))
                    BuildEditor();
            };
            panel.Children.Add(view);
        }

        private Grid CreateEditorRow(string labelText)
        {
            Grid row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 9),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new TextBlock
            {
                Text = labelText,
                ToolTip = labelText,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 12, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            label.SetResourceReference(ForegroundProperty, "MahApps.Brushes.Text");
            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            return row;
        }

        private static BcmChoice Choice(string label, object value)
        {
            return new BcmChoice(label, Convert.ToUInt32(value, CultureInfo.InvariantCulture));
        }

    }
}
