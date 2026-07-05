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
        private const uint OpponentSizeFamilyMask = 0x000F0000;
        private const uint OpponentSizeUnknownMask = 0x0000000F;
        private const uint OpponentSizeUpgradeMask = 0xFF000000;

        private void AddOpponentSizeEditor(Panel panel)
        {
            PropertyInfo property = GetProperty(nameof(BCM_Entry.OpponentSizeConditions));
            if (property == null) return;

            BCM_Entry entry = SelectedEntry;
            uint currentValue = ToUInt32(property.GetValue(entry, null));
            uint currentSize = currentValue & OpponentSizeFamilyMask;
            uint currentUnknown = currentValue & OpponentSizeUnknownMask;
            BcmValueOption[] sizeOptions =
            {
                new BcmValueOption("All sizes (0x0)", 0x0),
                new BcmValueOption("Small characters (0x20000)", 0x20000),
                new BcmValueOption("Default size (0x40000)", 0x40000),
                new BcmValueOption("Medium (0x50000)", 0x50000),
                new BcmValueOption("Medium Large (0x60000)", 0x60000),
                new BcmValueOption("Large (0x70000)", 0x70000),
                new BcmValueOption("Great Ape (0x80000)", 0x80000)
            };
            BcmValueOption[] unknownOptions =
            {
                new BcmValueOption("None (0x0)", 0x0),
                new BcmValueOption("Unknown (0x1)", 0x1),
                new BcmValueOption("Unknown (0x3)", 0x3)
            };

            if (!HasOption(sizeOptions, currentSize))
            {
                Array.Resize(ref sizeOptions, sizeOptions.Length + 1);
                sizeOptions[sizeOptions.Length - 1] = new BcmValueOption($"Unknown size (0x{currentSize:X})", currentSize);
            }

            if (!HasOption(unknownOptions, currentUnknown))
            {
                Array.Resize(ref unknownOptions, unknownOptions.Length + 1);
                unknownOptions[unknownOptions.Length - 1] = new BcmValueOption($"Unknown (0x{currentUnknown:X})", currentUnknown);
            }

            AddOpponentSizeCombo(panel, entry, "Opponent Size", sizeOptions, currentSize, OpponentSizeFamilyMask, "Requirements based on character CMS size entries.");
            AddOpponentSizeCombo(panel, entry, "Size Unknown", unknownOptions, currentUnknown, OpponentSizeUnknownMask, "Low unknown bits for opponent size conditions.");
        }

        private void AddOpponentSizeCombo(Panel panel, BCM_Entry entry, string label, BcmValueOption[] options, uint selectedValue, uint mask, string toolTip)
        {
            Grid row = CreateEditorRow(label);
            ComboBox comboBox = new ComboBox
            {
                ItemsSource = options,
                DisplayMemberPath = nameof(BcmValueOption.Label),
                SelectedValuePath = nameof(BcmValueOption.Value),
                SelectedValue = selectedValue,
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ToolTip = toolTip
            };
            comboBox.SelectionChanged += (sender, args) =>
            {
                if (comboBox.SelectedValue is uint comboValue)
                    SetOpponentSizeBits(entry, mask, comboValue);
            };

            Grid.SetColumn(comboBox, 1);
            row.Children.Add(comboBox);
            panel.Children.Add(row);
        }

        private static bool HasOption(BcmValueOption[] options, uint value)
        {
            foreach (BcmValueOption option in options)
            {
                if (option.Value == value)
                    return true;
            }

            return false;
        }

        private void SetOpponentSizeBits(BCM_Entry entry, uint mask, uint value)
        {
            PropertyInfo property = GetProperty(nameof(BCM_Entry.OpponentSizeConditions));
            if (property == null) return;

            uint currentValue = ToUInt32(property.GetValue(entry, null));
            uint newValue = (currentValue & ~mask) | (value & mask);
            SetProperty(entry, property, newValue);
        }

        private void AddReceiverLinkEditor(Panel panel)
        {
            PropertyInfo property = GetProperty(nameof(BCM_Entry.ReceiverLinkID));
            if (property == null) return;

            BCM_Entry entry = SelectedEntry;
            uint currentValue = ToUInt32(property.GetValue(entry, null));
            BcmValueOption[] options =
            {
                new BcmValueOption("None (0x0)", 0x0),
                new BcmValueOption("Combos (0x1)", 0x1),
                new BcmValueOption("Supers (0x2)", 0x2),
                new BcmValueOption("Ultimate / Awoken / Evasive (0x4)", 0x4),
                new BcmValueOption("Z-Vanish (0x8)", 0x8),
                new BcmValueOption("Ki Blasts (0x10)", 0x10),
                new BcmValueOption("Jump (0x20)", 0x20),
                new BcmValueOption("Guard (0x40)", 0x40),
                new BcmValueOption("Flying / Step Dash (0x80)", 0x80)
            };

            bool hasCurrentValue = false;
            foreach (BcmValueOption option in options)
            {
                if (option.Value == currentValue)
                {
                    hasCurrentValue = true;
                    break;
                }
            }

            if (!hasCurrentValue)
            {
                Array.Resize(ref options, options.Length + 1);
                options[options.Length - 1] = new BcmValueOption($"Unknown (0x{currentValue:X})", currentValue);
            }

            Grid row = CreateEditorRow("Receiver Link Id");
            ComboBox comboBox = new ComboBox
            {
                ItemsSource = options,
                DisplayMemberPath = nameof(BcmValueOption.Label),
                SelectedValuePath = nameof(BcmValueOption.Value),
                SelectedValue = currentValue,
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ToolTip = "Matches the BCM Callback link value. Combos = 0x1, Supers = 0x2, Ultimate / Awoken / Evasive = 0x4."
            };
            comboBox.SelectionChanged += (sender, args) =>
            {
                if (comboBox.SelectedValue is uint selectedValue)
                    SetProperty(entry, property, selectedValue);
            };

            Grid.SetColumn(comboBox, 1);
            row.Children.Add(comboBox);
            panel.Children.Add(row);
        }

        private void AddLinkEditor(Panel panel, string labelText, string propertyName, string structuralIndex)
        {
            PropertyInfo property = GetProperty(propertyName);
            if (property == null) return;

            BCM_Entry entry = SelectedEntry;
            // The shown value starts from the real tree link. A loop override is only written when the user changes it.
            string loopIndex = property.GetValue(entry, null) as string;
            string fallbackIndex = NormalizeLinkIndex(structuralIndex);
            string displayValue = NormalizeLinkIndex(loopIndex);
            if (displayValue == "0")
                displayValue = fallbackIndex;

            Grid row = CreateEditorRow(labelText);

            TextBox textBox = new TextBox
            {
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = displayValue,
                ToolTip = labelText
            };
            textBox.LostFocus += (sender, args) => ApplyLinkValue(textBox, entry, property, fallbackIndex);
            textBox.TextChanged += (sender, args) => ApplyLinkValue(textBox, entry, property, fallbackIndex, false);
            textBox.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    ApplyLinkValue(textBox, entry, property, fallbackIndex);
                    args.Handled = true;
                }
            };

            pendingCommits.Add(() => ApplyLinkValue(textBox, entry, property, fallbackIndex, false));

            Grid.SetColumn(textBox, 1);
            row.Children.Add(textBox);
            panel.Children.Add(row);
        }

        private void ApplyLinkValue(TextBox textBox, BCM_Entry entry, PropertyInfo property, string structuralIndex, bool updateText = true)
        {
            string value = NormalizeLinkIndex(textBox.Text);
            string fallbackIndex = NormalizeLinkIndex(structuralIndex);
            string loopValue = value == fallbackIndex ? null : value;
            object oldValue = property.GetValue(entry, null);

            if (Equals(oldValue, loopValue)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(property.Name, entry, oldValue, loopValue, property.Name));
            property.SetValue(entry, loopValue, null);
            EntryEdited?.Invoke(this, EventArgs.Empty);

            if (updateText)
                textBox.Text = value;
        }

        private static string NormalizeLinkIndex(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "0";
            return value.Trim();
        }

        private void AddUpgradeLevelEditor(Panel panel)
        {
            BCM_Entry entry = SelectedEntry;
            Grid row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 9),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new TextBlock
            {
                Text = "Upgrade Level",
                ToolTip = "Opponent Size Conditions mapped as 0x1000000 per level.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 12, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            label.SetResourceReference(ForegroundProperty, "MahApps.Brushes.Text");
            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            TextBox textBox = new TextBox
            {
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = GetOpponentSizeUpgradeLevel(entry).ToString(CultureInfo.InvariantCulture),
                ToolTip = "Level 0 = 0x0, Level 4 = 0x4000000"
            };
            textBox.LostFocus += (sender, args) => ApplyUpgradeLevel(textBox, entry);
            textBox.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    ApplyUpgradeLevel(textBox, entry);
                    args.Handled = true;
                }
            };

            pendingCommits.Add(() => ApplyUpgradeLevel(textBox, entry, false));

            Grid.SetColumn(textBox, 1);
            row.Children.Add(textBox);
            panel.Children.Add(row);
        }

        private void ApplyUpgradeLevel(TextBox textBox, BCM_Entry entry, bool updateText = true)
        {
            if (!uint.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint level))
            {
                textBox.Text = GetOpponentSizeUpgradeLevel(entry).ToString(CultureInfo.InvariantCulture);
                return;
            }

            SetOpponentSizeBits(entry, OpponentSizeUpgradeMask, (level << 24) & OpponentSizeUpgradeMask);
            if (updateText)
                textBox.Text = level.ToString(CultureInfo.InvariantCulture);
        }

        private uint GetOpponentSizeUpgradeLevel(BCM_Entry entry)
        {
            return (entry.OpponentSizeConditions & OpponentSizeUpgradeMask) >> 24;
        }

    }
}
