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
        private void AddEditor(Panel panel, string labelText, string propertyName, EditorValueMode valueMode, bool commitOnTextChanged = false)
        {
            PropertyInfo property = GetProperty(propertyName);
            if (property == null) return;

            Grid row = CreateEditorRow(labelText);

            TextBox textBox = new TextBox
            {
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = FormatValue(property.GetValue(SelectedEntry, null), valueMode)
            };
            textBox.ToolTip = labelText;
            textBox.LostFocus += (sender, args) => ApplyEditorValue(textBox, property, valueMode);
            if (commitOnTextChanged)
                textBox.TextChanged += (sender, args) => ApplyEditorValue(textBox, property, valueMode, false);
            textBox.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    ApplyEditorValue(textBox, property, valueMode);
                    args.Handled = true;
                }
            };

            Grid.SetColumn(textBox, 1);
            row.Children.Add(textBox);
            panel.Children.Add(row);
        }

        private void ApplyEditorValue(TextBox textBox, PropertyInfo property, EditorValueMode valueMode, bool updateText = true)
        {
            if (TryConvertText(textBox.Text, property.PropertyType, valueMode, out object value))
            {
                SetProperty(property, value);
                if (updateText)
                    textBox.Text = FormatValue(property.GetValue(SelectedEntry, null), valueMode);
                return;
            }

            textBox.Text = FormatValue(property.GetValue(SelectedEntry, null), valueMode);
        }

        private void SetProperty(PropertyInfo property, object value)
        {
            object oldValue = property.GetValue(SelectedEntry, null);
            if (Equals(oldValue, value)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(property.Name, SelectedEntry, oldValue, value, property.Name));
            property.SetValue(SelectedEntry, value, null);
            EntryEdited?.Invoke(this, EventArgs.Empty);
        }

        private PropertyInfo GetProperty(string propertyName)
        {
            // The editor uses concrete field groups. Reflection here only keeps UndoManager wiring consistent for simple scalar fields.
            return typeof(BCM_Entry).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        }

        private static uint ToUInt32(object value)
        {
            if (value == null) return 0;
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        private static object FromUInt32(Type type, uint value)
        {
            if (type.IsEnum) return Enum.ToObject(type, value);
            if (type == typeof(uint)) return value;
            if (type == typeof(ushort)) return (ushort)value;
            if (type == typeof(short)) return unchecked((short)value);
            if (type == typeof(int)) return unchecked((int)value);
            return value;
        }

        private static bool TryConvertText(string text, Type type, EditorValueMode valueMode, out object value)
        {
            text = (text ?? string.Empty).Trim();

            if (type == typeof(string))
            {
                value = string.IsNullOrWhiteSpace(text) ? null : text;
                return true;
            }

            bool isHex = valueMode == EditorValueMode.Hex || text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            string numberText = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2) : text;

            if (type == typeof(float))
            {
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = floatValue;
                    return true;
                }

                value = null;
                return false;
            }

            if (isHex)
            {
                if (!uint.TryParse(numberText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexValue))
                {
                    value = null;
                    return false;
                }

                value = FromUInt32(type, hexValue);
                return true;
            }

            if (type == typeof(uint) && uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintValue))
            {
                value = uintValue;
                return true;
            }

            if (type == typeof(ushort) && ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortValue))
            {
                value = ushortValue;
                return true;
            }

            if (type == typeof(short) && short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortValue))
            {
                value = shortValue;
                return true;
            }

            if (type == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                value = intValue;
                return true;
            }

            value = null;
            return false;
        }

        private static string FormatValue(object value, EditorValueMode valueMode)
        {
            if (value == null) return string.Empty;

            if (valueMode == EditorValueMode.Hex)
                return $"0x{ToDisplayUInt32(value):X}";

            if (value is float floatValue)
                return floatValue.ToString("0.#######", CultureInfo.InvariantCulture);

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static uint ToDisplayUInt32(object value)
        {
            if (value is short shortValue) return unchecked((ushort)shortValue);
            if (value is int intValue) return unchecked((uint)intValue);
            if (value is ushort ushortValue) return ushortValue;
            if (value is byte byteValue) return byteValue;
            if (value is sbyte sbyteValue) return unchecked((byte)sbyteValue);
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

    }
}
