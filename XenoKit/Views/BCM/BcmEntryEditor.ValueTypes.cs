using System.Windows.Controls;

namespace XenoKit.Views.BCM
{
    public partial class BcmEntryEditor : UserControl
    {
        private enum EditorValueMode
        {
            Decimal,
            Hex,
            Text
        }

        private class BcmValueOption
        {
            public string Label { get; }
            public uint Value { get; }

            public BcmValueOption(string label, uint value)
            {
                Label = label;
                Value = value;
            }
        }
    }
}
